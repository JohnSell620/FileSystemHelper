using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummarizerPlugin
{
    public class TextFile
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public string Extension { get; set; }
        public string RawText { get; set; }
        public int DocumentLength { get; set; }

        // Can be added to document properties. (TODO)
        public string[] DocumentConcepts { get; set; }

        public TextFile(string fullPath)
        {
            Name = Path.GetFileName(fullPath);
            FullPath = fullPath;
            Extension = Path.GetExtension(fullPath);
            RawText = File.ReadAllText(fullPath);
        }
    }
}
