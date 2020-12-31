using PluginBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
