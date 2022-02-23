using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Win32;
using AODL.Document.Content;
using BitMiracle.Docotic.Pdf;
using AODL.Document.TextDocuments;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;

namespace SummarizerPlugin
{
    public class TextFile
    {
        public string? Name { get; set; }
        public string? FullPath { get; set; }
        public string? Extension { get; set; }
        public string? RawText { get; set; }
        public int? DocumentLength { get; set; }
        public string[]? DocumentConcepts { get; set; }
        public string? Summary { get; set; }
        public int DesiredSummaryLength { get; set; }

        public TextFile() { }
        public TextFile(string _fullPath)
        {
            Name = Path.GetFileName(_fullPath);
            FullPath = _fullPath;
            Extension = Path.GetExtension(_fullPath);
            DesiredSummaryLength = 4;

            try
            {
                if (Extension == ".docx")
                {
                    RawText = GetTextFromWord(_fullPath);
                }
                else if (Extension == ".pdf")
                {
                    RawText = GetTextFromPdf(_fullPath);
                }
                else if (Extension == ".odt")
                {
                    RawText = GetTextFromOdt(_fullPath);
                }
                else /* Extension is .txt */
                {
                    RawText = File.ReadAllText(_fullPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Data);
            }
        }

        public string UpdateFileProperties()
        {
            try
            {
                // Check whether Microsoft Word is installed on this computer,
                // by searching the HKEY_CLASSES_ROOT\Word.Application key.
                using (var regWord = Registry.ClassesRoot.OpenSubKey("Word.Application"))
                {
                    if (regWord == null)
                    {
                        return "Microsoft Word is not installed";
                    }
                    else
                    {
                        if (DocumentConcepts != null && DocumentConcepts.Length > 0)
                        {
                            // TODO Prompt to elevate privileges...
                            var shellProperties = ShellFile.FromFilePath(FullPath).Properties;
                            shellProperties.System.Keywords.Value = DocumentConcepts;
                            shellProperties.System.Subject.Value = Summary;

                            using ShellPropertyWriter spw = shellProperties.GetPropertyWriter();
                            spw.WriteProperty(SystemProperties.System.Keywords, DocumentConcepts);
                            spw.WriteProperty(SystemProperties.System.Subject, Summary);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Source != null)
                    Console.WriteLine(ex.Source.ToString());
            }

            return "File property updates successful.";
        }

        private string GetTextFromPdf(string filePath)
        {
            string text = "";
            using (var pdf = new PdfDocument(filePath))
            {
                var options = new PdfTextExtractionOptions
                {
                    SkipInvisibleText = true,
                    WithFormatting = false
                };
                text = pdf.GetText(options);
            }
            return text;
        }

        private string GetTextFromWord(string file /* SPFile file */)
        {
            /* 
             * From https://bit.ly/2XZi9CF 
             */
            const string wordmlNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            System.Text.StringBuilder textBuilder = new System.Text.StringBuilder();
            using (WordprocessingDocument? wdDoc = WordprocessingDocument.Open(file /* file.OpenBinaryStream() */, false))
            {
                // Manage namespaces to perform XPath queries.  
                NameTable nt = new NameTable();
                XmlNamespaceManager nsManager = new XmlNamespaceManager(nt);
                nsManager.AddNamespace("w", wordmlNamespace);

                // Get the document part from the package.  
                // Load the XML in the document part into an XmlDocument instance.  
                XmlDocument xdoc = new XmlDocument(nt);
                xdoc.Load(wdDoc.MainDocumentPart.GetStream());

                XmlNodeList? paragraphNodes = xdoc.SelectNodes("//w:p", nsManager);
                if (paragraphNodes != null)
                {
                    foreach (XmlNode paragraphNode in paragraphNodes)
                    {
                        XmlNodeList? textNodes = paragraphNode.SelectNodes(".//w:t", nsManager);
                        if (textNodes != null)
                        {
                            foreach (System.Xml.XmlNode textNode in textNodes)
                            {
                                textBuilder.Append(textNode.InnerText);
                            }
                            textBuilder.Append(System.Environment.NewLine);
                        }
                    }
                }

            }
            return textBuilder.ToString();
        }

        private string GetTextFromOdt(string path)
        {
            /* 
             * From https://bit.ly/3qIktKA 
             */
            var sb = new System.Text.StringBuilder();
            using (var doc = new TextDocument())
            {
                doc.Load(path);

                //The header and footer are in the DocumentStyles part. Grab the XML of this part
                XElement stylesPart = XElement.Parse(doc.DocumentStyles.Styles.OuterXml);
                //Take all headers and footers text, concatenated with return carriage
                string stylesText = string.Join("\r\n", stylesPart.Descendants()
                    .Where(x => x.Name.LocalName == "header" || x.Name.LocalName == "footer")
                    .Select(y => y.Value));

                //Main content
                var mainPart = doc.Content.Cast<IContent>();
                var mainText = String.Join("\r\n", mainPart.Select(x => x.Node.InnerText));

                //Append both text variables
                sb.Append(stylesText + "\r\n");
                sb.Append(mainText);
            }

            return sb.ToString();
        }
    }
}
