using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;

namespace SummarizerPlugin
{
    public partial class SummarizerControl : System.Windows.Controls.UserControl
    {
        private TextFile _activeTextFile;
        public SummarizerControl()
        {
            InitializeComponent();
        }
        
        private void GenerateAndPrintSummary(TextFile textFile)
        {
            try
            {
                string summaryText = Summarizer.SummarizeByLSA(textFile);
                textFile.Summary = summaryText;
                
                SummaryText.Text = summaryText;
                MDCardSummary.Visibility = Visibility.Visible;

                Run r1 = new Run("File name: " + textFile.Name);
                string documentConcepts = "";
                foreach (string s in textFile.DocumentConcepts)
                {
                    documentConcepts += s + ", ";
                }
                Run r2 = new Run("Concepts: " + documentConcepts.Remove(documentConcepts.Length - 2));
                Run r3 = new Run("Location: " + textFile.FullPath);

                FileInfo.Inlines.Clear();
                FileInfo.Inlines.Add(r1);
                FileInfo.Inlines.Add(new LineBreak());
                FileInfo.Inlines.Add(r2);
                FileInfo.Inlines.Add(new LineBreak());
                FileInfo.Inlines.Add(r3);
                MDCardFileInfo.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                //throw ex;
                Console.WriteLine(ex.Data);
            }
            
        }

        private void ProcessSelectedFiles(string[] fileNames)
        {
            DragAndDrop.Visibility = Visibility.Hidden;
            Copy_Button.Visibility = Visibility.Visible;
            Clear_Button.Visibility = Visibility.Visible;

            if (fileNames.Length == 1)
            {
                string fileExt = System.IO.Path.GetExtension(fileNames[0]);

                // Txt file properties cannot be updated.
                if (fileExt == ".docx" || fileExt == ".odt" || fileExt == ".pdf")
                {
                    AddProperties_Button.Visibility = Visibility.Visible;
                }
                _activeTextFile = new TextFile(fileNames[0]);
                GenerateAndPrintSummary(_activeTextFile);
            }
            if (fileNames.Length > 1)
            {
                string overallSummary = "";
                string overallPath = System.IO.Path.GetDirectoryName(fileNames[0]);
                foreach (string filePath in fileNames)
                {
                    _activeTextFile = new TextFile(filePath);
                    overallSummary += Summarizer.SummarizeByLSA(_activeTextFile);
                }
                _activeTextFile = new TextFile
                {
                    RawText = overallSummary,
                    Name = overallPath,
                    FullPath = overallPath,
                    Extension = null
                };
                GenerateAndPrintSummary(_activeTextFile);
            }
        }

        private void BrowseLocal_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".docx",
                Filter = @"Text Files (.txt)|*.txt|Word Documents (.docx)|*.docx|" +
                    @"Word Template (.dotx)|*.dotx|ODT Files (.odt)|*.odt|" +
                    @"PDF Files (.pdf)|*.pdf|All Files (*.*)|*.*",
                InitialDirectory = @"C:\Users\jsell\source\repos\",
                Multiselect = true,
                RestoreDirectory = true
            };

            if (dlg.ShowDialog() == true)
            {
                //string[] fileNames = dlg.FileNames;
                ProcessSelectedFiles(dlg.FileNames);
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Clipboard.SetText(SummaryText.Text);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _activeTextFile = null;
            SummaryText.Text = "";
            MDCardSummary.Visibility = Visibility.Hidden;
            MDCardFileInfo.Visibility = Visibility.Hidden;
            DragAndDrop.Visibility = Visibility.Visible;
            Copy_Button.Visibility = Visibility.Hidden;
            Clear_Button.Visibility = Visibility.Hidden;
            AddProperties_Button.Visibility = Visibility.Hidden;
        }

        private void AddSummaryToFileProperties_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.MessageBox.Show(
                "Update file's properties with summary and concepts?",
                "Update file properties", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (_activeTextFile != null)
                {
                    _activeTextFile.UpdateFileProperties();
                }
            }
        }
        
        private void Ellipse_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Ellipse ellipse && e.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(ellipse,
                                    ellipse.Fill.ToString(),
                                    System.Windows.DragDropEffects.Copy);
            }
        }

        private Brush _previousFill = null;
        private void Ellipse_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (sender is Ellipse ellipse)
            {
                // Save the current Fill brush so that you can revert back to this value in DragLeave.
                _previousFill = ellipse.Fill;

                // If the DataObject contains string data, extract it.
                if (e.Data.GetDataPresent(System.Windows.DataFormats.StringFormat))
                {
                    string dataString = (string)e.Data.GetData(System.Windows.DataFormats.StringFormat);

                    // If the string can be converted into a Brush, convert it.
                    BrushConverter converter = new BrushConverter();
                    if (converter.IsValid(dataString))
                    {
                        Brush newFill = (Brush)converter.ConvertFromString(dataString);
                        ellipse.Fill = newFill;
                    }
                }
            }
        }

        private void Ellipse_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Effects = System.Windows.DragDropEffects.None;

            // If the DataObject contains string data, extract it.
            if (e.Data.GetDataPresent(System.Windows.Forms.DataFormats.StringFormat))
            {
                string dataString = (string)e.Data.GetData(System.Windows.Forms.DataFormats.StringFormat);

                // If the string can be converted into a Brush, allow copying.
                BrushConverter converter = new BrushConverter();
                if (converter.IsValid(dataString))
                {
                    e.Effects = System.Windows.DragDropEffects.Copy | System.Windows.DragDropEffects.Move;
                }
            }
        }

        private void Ellipse_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            if (sender is Ellipse ellipse)
            {
                ellipse.Fill = _previousFill;
            }
        }

        private void Ellipse_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (sender is Ellipse ellipse)
            {
                Console.WriteLine("Ellipse_Drop entered...");
                // If the DataObject contains string data, extract it.
                if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                {
                    string[] fileNames = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                    ProcessSelectedFiles(fileNames);

                    // If the string can be converted into a Brush,
                    // convert it and apply it to the ellipse.
                    BrushConverter converter = new BrushConverter();
                    string firstFileName = fileNames[0].ToString();
                    if (converter.IsValid(firstFileName))
                    {
                        Brush newFill = (Brush)converter.ConvertFromString(firstFileName);
                        ellipse.Fill = newFill;
                        e.Effects = System.Windows.DragDropEffects.Copy;
                    }
                }
            }
        }
    }
}
