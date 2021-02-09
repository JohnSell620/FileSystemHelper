using PluginBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Poseidon.Analysis;
using System.IO;
using System.Text.RegularExpressions;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Configuration;

namespace SummarizerPlugin
{
    public class Summarizer : IComponent
    {
        public string Name { get => "Summarizer"; }
        public string Description { get => "Summarize image or text files in batches or one-by-one."; }
        public string Function { get => "Summarize"; }
        public string Author { get => "JS"; }
        public string Version { get => "1.0.0"; }
        public Type Control => typeof(SummarizerControl);
        public int Execute()
        {
            Console.WriteLine("Hello, File Summarizer.");
            return 0;
        }

        public static string SummarizeByLSA(TextFile textFile)
        {
            string input = textFile.RawText;
            string[] sentences = input.Split(new char[] { '.', '!', '?', ':', '…', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < sentences.Length; ++i)
            {
                var sb = new StringBuilder();
                string sentence = sentences[i].Trim();
                foreach (char c in sentence)
                {
                    if (!char.IsPunctuation(c))
                        sb.Append(c);
                }
                sentences[i] = sb.ToString().ToLower();
            }

            // Remove stop words--e.g., the, and, a, etc.
            string[] stopwords = File.ReadAllLines(@"Resources/stopwords.txt");
            for (int i = 0; i < sentences.Count(); ++i)
            {
                string sentence = sentences[i];
                for (int j = 0; j < stopwords.Count(); ++j)
                {
                    sentences[i] = string.Join(" ", sentence.Split(' ').Where(wrd => !stopwords.Contains(wrd)));
                }
            }

            // Reduce words to their stem.
            PorterStemmer stemmer = new PorterStemmer();
            for (int i = 0; i < sentences.Count(); ++i)
            {
                sentences[i] = stemmer.StemWord(sentences[i]);
            }

            Dictionary<string, int> wordFrequencies = new Dictionary<string, int>();
            foreach (string s in sentences)
            {
                string[] words = s.Split(' ');
                foreach (string w in words)
                {
                    if (wordFrequencies.ContainsKey(w))
                    {
                        wordFrequencies[w] += 1;
                    }
                    else
                    {
                        wordFrequencies[w] = 1;
                    }
                }
            }

            // Top N words with highest frequencies will serve as document concepts.
            string summarySentenceCount = ConfigurationManager.AppSettings.Get("SummarySentenceCount");
            Console.WriteLine("\nSummarySentenceCount = " + summarySentenceCount);
            int N = 4;
            string[] concepts = (from kvp in wordFrequencies
                                 orderby kvp.Value descending
                                 select kvp)
                                .ToDictionary(pair => pair.Key, pair => pair.Value).Take(N)
                                .Select(k => k.Key).ToArray();

            // Add concepts to TextFile instance properties.
            textFile.DocumentConcepts = concepts;

            int documentLength = sentences.Length;
            var X = DenseMatrix.Create(N, documentLength, (i, j) => 0.0);
            for (int i = 0; i < X.RowCount; ++i)
            {
                int sentencesWithConcept = 0;
                string concept = concepts[i];
                for (int j = 0; j < X.ColumnCount; ++j)
                {
                    string[] sentenceWords = sentences[j].Split(' ');
                    int wordCount = (from word in sentenceWords
                                     where word == concept
                                     select word)
                                     .Count();
                    if (wordCount > 0)
                    {
                        sentencesWithConcept += 1;
                    }

                    X[i, j] = wordCount / sentenceWords.Length;
                }
                if (sentencesWithConcept == 0)
                {
                    Console.WriteLine("No sentences with concept " + concepts[i]);
                }
                double inverseDocumentFreq = Math.Log(documentLength / (sentencesWithConcept + 0.0001), 2.0);
                for (int k = 0; k < X.ColumnCount; ++k)
                {
                    X[i, k] = X[i, k] * inverseDocumentFreq;
                }
            }

            // Compute SVD of the topic representation matrix, X.
            var svd = X.Svd();

            // Cross method to select summary sentences.
            int columnCount = svd.VT.ColumnCount;
            Matrix<double> Vh = svd.VT.SubMatrix(0, concepts.Length, 0, columnCount).PointwiseAbs();
            for (int i = 0; i < Vh.RowCount; ++i)
            {
                double averageSentenceScore = Vh.Row(i).Average();
                for (int j = 0; j < Vh.ColumnCount; ++j)
                {
                    if (Vh[i, j] <= averageSentenceScore)
                    {
                        Vh[i, j] = 0;
                    }
                }
            }

            var sentenceLengths = Vh.RowSums();
            int[] summaryIndices = new int[Vh.RowCount];
            Console.Write("Vh.RowCnt = ", Vh.RowCount);
            Console.Write("concepts.Length = ", concepts.Length);
            for (int i = 0; i < Vh.RowCount; ++i)
            {
                double max = 0;
                for (int j = 0; j < Vh.ColumnCount; ++j)
                {
                    if (Vh[i, j] > max)
                    {
                        max = Vh[i, j];
                        summaryIndices[i] = j;
                    }
                }
            }
            
            string[] sourceSentences = Regex.Split(input, @"(?<=[\.!\?])\s+");
            textFile.DocumentLength = sourceSentences.Length;
            string summary = "";
            foreach (int i in summaryIndices)
            {
                summary += sourceSentences[i] + " ";
            }

            /* From https://bit.ly/3ogjy2l */
            return summary.Replace("\r\n", string.Empty)
                        .Replace("\n", string.Empty)
                        .Replace("\r", string.Empty)
                        .Replace("\t", string.Empty)
                        .Replace(((char)0x2028).ToString(), string.Empty)
                        .Replace(((char)0x2029).ToString(), string.Empty);
        }
    }
}
