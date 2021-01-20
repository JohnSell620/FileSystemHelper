using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using DocumentFormat.OpenXml.Packaging;
using System.Xml;
using AODL.Document.TextDocuments;
using AODL.Document.Content;
using System.Xml.Linq;

namespace SummarizerPlugin
{
    public class TextFile
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public string Extension { get; set; }
        public string RawText { get; set; }
        public int DocumentLength { get; set; }
        public string[] DocumentConcepts { get; set; }
        public string Summary { get; set; }

        public TextFile() { }
        public TextFile(string fullPath)
        {
            Name = Path.GetFileName(fullPath);
            FullPath = fullPath;
            Extension = Path.GetExtension(fullPath);
            if (Extension == ".docx" || Extension == ".pdf")
            {
                try
                {
                    RawText = GetTextFromWordOrPDF(fullPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Data);
                }
            }
            else if (Extension == ".odt")
            {
                try
                {
                    RawText = GetTextFromOdt(fullPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Data);
                }
            }
            else /* Extension is .txt */
            {
                RawText = File.ReadAllText(fullPath);
            }
        }

        public void UpdateFileProperties()
        {
            try
            {
                var shellFile = ShellFile.FromFilePath(FullPath);
                ShellPropertyWriter spw = shellFile.Properties.GetPropertyWriter();
                spw.WriteProperty(SystemProperties.System.Keywords, DocumentConcepts);
                spw.WriteProperty(SystemProperties.System.Subject, Summary);
                spw.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Source.ToString());
            }
        }

        public string GetTextFromWordOrPDF(string file /* SPFile file */)
        {
            const string wordmlNamespace = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            StringBuilder textBuilder = new StringBuilder();
            using (WordprocessingDocument wdDoc = WordprocessingDocument.Open(file /* file.OpenBinaryStream() */, false))
            {
                // Manage namespaces to perform XPath queries.  
                NameTable nt = new NameTable();
                XmlNamespaceManager nsManager = new XmlNamespaceManager(nt);
                nsManager.AddNamespace("w", wordmlNamespace);

                // Get the document part from the package.  
                // Load the XML in the document part into an XmlDocument instance.  
                XmlDocument xdoc = new XmlDocument(nt);
                xdoc.Load(wdDoc.MainDocumentPart.GetStream());

                XmlNodeList paragraphNodes = xdoc.SelectNodes("//w:p", nsManager);
                foreach (XmlNode paragraphNode in paragraphNodes)
                {
                    XmlNodeList textNodes = paragraphNode.SelectNodes(".//w:t", nsManager);
                    foreach (System.Xml.XmlNode textNode in textNodes)
                    {
                        textBuilder.Append(textNode.InnerText);
                    }
                    textBuilder.Append(Environment.NewLine);
                }

            }
            return textBuilder.ToString();
        }

        public string GetTextFromOdt(string path)
        {
            var sb = new StringBuilder();
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
