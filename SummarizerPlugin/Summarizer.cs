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

namespace SummarizerPlugin
{
    public class Summarizer : IComponent
    {
        public string Name { get => "Summarizer"; }
        public string Description { get => "Summarize image or text files in batches or one-by-one."; }
        public string Function { get => "Summarize"; }
        public string Author { get => "JS"; }
        public string Version { get => "1.0.0"; }
        public int Execute()
        {
            Console.WriteLine("Hello, File Summarizer.");
            return 0;
        }

        public static string SummarizeByLSA(string input)
        {
            string[] sentences = Regex.Split(input, @"(?<=[\.!\?])\s+");
            for (int i = 0; i < sentences.Length; ++i)
            {
                var sb = new StringBuilder();
                foreach (char c in sentences[i])
                {
                    if (!char.IsPunctuation(c))
                        sb.Append(c);
                }
                sentences[i] = sb.ToString();
            }

            // Remove stop words--e.g., the, and, a, etc.
            string[] stopwords = File.ReadAllLines(@"./stopwords.txt");
            for (int i = 0; i < sentences.Count(); ++i)
            {
                for (int j = 0; j < stopwords.Count(); ++j)
                {
                    sentences[i] = sentences[i].Replace(stopwords[j], "");
                }
            }

            // Reduce words to their stem.
            var stemmer = new PorterStemmer();
            for (int i = 0; i < sentences.Count(); ++i)
            {
                sentences[i] = stemmer.StemWord(sentences[i]);
            }
                        
            Dictionary<string, int> wordFrequencies = new Dictionary<string, int>();
            foreach (string s in sentences)
            {
                if (wordFrequencies.ContainsKey(s))
                {
                    wordFrequencies[s]++;
                }
                else
                {
                    wordFrequencies[s] = 1;
                }
            }

            foreach (KeyValuePair<string, int> kvp in wordFrequencies)
            {
                Console.WriteLine(kvp.Key + ", " + kvp.Value);
            }
            // Top N words with highest frequencies will serve as document concepts.
            int N = 5;
            string[] concepts = (from kvp in wordFrequencies
                            orderby kvp.Value descending
                            select kvp)
                            .ToDictionary(pair => pair.Key, pair => pair.Value).Take(N)
                            .Select(k => k.Key).ToArray();
            //string[] concepts = conceptQuery.Select(k => k.Key).ToArray();

            //foreach (string s in concepts)
            //{
            //    Console.WriteLine(s);
            //}

            //int documentLength = sentences.Length;
            //var X = DenseMatrix.Create(concepts.Length, documentLength, (i, j) => 0.0);
            //for (int i = 0; i < N; ++i)
            //{
            //    int sentencesWithConcept = 0;
            //    for (int j = 0; j < documentLength; ++j)
            //    {
            //        var matchQuery = from word in sentences[j]
            //                         where word == concepts[i]
            //                         select word;
            //        int wordCount = matchQuery.Count();
            //        if (wordCount > 0)
            //        {
            //            sentencesWithConcept += 1;
            //        }

            //        X[i, j] = wordCount / sentences[j].Split(' ').Length;
            //    }
            //    if (sentencesWithConcept == 0)
            //    {
            //        Console.WriteLine("No sentences with concept " + concepts[i]);
            //    }
            //    double inverseDocumentFreq = Math.Log(documentLength/(sentencesWithConcept + 0.0001), 2.0);
            //    for (int k = 0; k < N; ++k)
            //    {
            //        X[i, k] = X[i, k] * inverseDocumentFreq;
            //    }
            //}

            //// Compute SVD of the topic representation matrix, X.
            //var svd = X.Svd();

            //// Select sentences via the cross method.
            //Matrix<double> Vh = svd.VT.PointwiseAbs();
            //for (int i = 0; i < Vh.RowCount; ++i)
            //{
            //    double averageSentenceScore = Vh.Row(i).Average();
            //    for (int j = 0; j < Vh.ColumnCount; ++j)
            //    {
            //        if (Vh[i, j] <= averageSentenceScore) Vh[i, j] = 0;
            //    }
            //}
            //var sentenceLengths = Vh.RowSums();
            //int[] summaryIndices = new int[concepts.Length];
            //for (int i = 0; i < Vh.RowCount; ++i)
            //{
            //    double max = 0;
            //    for (int j = 0; j < Vh.ColumnCount; ++j)
            //    {
            //        if (Vh[i, j] > max)
            //        {
            //            max = Vh[i, j];
            //            summaryIndices[i] = j;
            //        }

            //    }
            //}

            //string summary = "";
            //foreach (int i in summaryIndices)
            //{
            //    summary += sentences[i];
            //}

            return "summary";
        }
    }
}
