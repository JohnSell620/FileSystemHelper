using PluginBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Poseidon.Analysis;
using System.IO;
using System.Linq;
using System.Text;

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

        public static string summarizeByLSA(string text)
        {
            // Compute N-most occurring words.
            string[] source = text.ToLower().Split(new char[] { '.', '?', '!', ' ', ';', ':', ',' },
                StringSplitOptions.RemoveEmptyEntries);

            string[] stopwords = File.ReadAllLines(@"./stopwords.txt");
            for (int i = 0; i < source.Count(); ++i)
            {
                for (int j = 0; j < stopwords.Count(); ++j)
                {
                    source[i] = source[i].Replace(stopwords[j], "");
                }
            }

            var stemmer = new PorterStemmer();
            for (int i = 0; i < source.Count(); ++i)
            {
                source[i] = stemmer.StemWord(source[i]);
            }

            // Top N words with the highest frequencies will serve as the document concepts.
            var wordFrequencies = new SortedDictionary<string, int>();
            foreach (string s in source)
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

            return "Summary sentence.";
        }
    }
}
