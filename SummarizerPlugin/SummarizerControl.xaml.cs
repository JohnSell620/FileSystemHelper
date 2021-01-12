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
        public SummarizerControl()
        {
            InitializeComponent();
        }
        
        private void GenerateAndPrintSummary(string file)
        {
            try
            {
                TextFile textFile = new TextFile(file);
                string summaryText = Summarizer.SummarizeByLSA(textFile);

                // Clear previous summary.
                if (_StackPanel.Children.GetType() == typeof(TextBlock) || _StackPanel.Children.Count > 0)
                {
                    //_StackPanel.Children.Clear();
                    SummaryText.Text = "";
                }

                SummaryText.Text = summaryText;
                MDCardSummary.Visibility = Visibility.Visible;

                Run r1 = new Run("File name: " + textFile.Name);
                string documentConcepts = "";
                foreach (string s in textFile.DocumentConcepts) {
                    documentConcepts += s + ", ";
                }
                Run r2 = new Run("Concepts: " + documentConcepts.Remove(documentConcepts.Length - 2));
                Run r3 = new Run("Location: " + textFile.FullPath);

                FileInfo.Inlines.Add(r1);
                FileInfo.Inlines.Add(new LineBreak());
                FileInfo.Inlines.Add(r2);
                FileInfo.Inlines.Add(new LineBreak());
                FileInfo.Inlines.Add(r3);
                MDCardFileInfo.Visibility = Visibility.Visible;

                //string textToSummarize = File.ReadAllText(dlg.FileName);
                //string[] textLines = File.ReadAllLines(dlg.FileName);
                //string textSummary = @"Historically, the world of data and the world of objects" +
                //    @" have not been well integrated. Programmers work in C# or Visual Basic" +
                //    @" and also in SQL or XQuery. On the one side are concepts such as classes,";

                //TextBlock textBlock = new TextBlock();
                //SummaryText.Name = "SummaryText";
                //SummaryText.Style = (Style)Application.Current.Resources["MaterialDesignHeadline6TextBlock"];
                //SummaryText.Text = summaryText;
                //
                //textBlock.Style ==> materialDesign:TextFieldAssist.Hint="Please copy and/or browse again..."
                //
                //_StackPanel.Children.Add(textBlock);
                //MDCard.Visibility = Visibility.Visible;

            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private void BrowseLocal_Click(object sender, RoutedEventArgs e)
        {

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = "txt",
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
                InitialDirectory = @"C:\Users\jsell\source\repos\",
                RestoreDirectory = true
            };

            if (dlg.ShowDialog() == true)
            {
                DragAndDrop.Visibility = Visibility.Hidden;
                Copy_Button.Visibility = Visibility.Visible;
                Clear_Button.Visibility = Visibility.Visible;
                AddProperties_Button.Visibility = Visibility.Visible;
                GenerateAndPrintSummary(dlg.FileName);
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Clipboard.SetText(SummaryText.Text);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
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

        }
        
        private void Ellipse_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Ellipse ellipse = sender as Ellipse;
            if (ellipse != null && e.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(ellipse,
                                    ellipse.Fill.ToString(),
                                    System.Windows.DragDropEffects.Copy);
            }
        }

        private Brush _previousFill = null;
        private void Ellipse_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            Ellipse ellipse = sender as Ellipse;
            if (ellipse != null)
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
            Ellipse ellipse = sender as Ellipse;
            if (ellipse != null)
            {
                ellipse.Fill = _previousFill;
            }
        }

        private void Ellipse_Drop(object sender, System.Windows.DragEventArgs e)
        {
            Ellipse ellipse = sender as Ellipse;
            if (ellipse != null)
            {
                Console.WriteLine("Ellipse_Drop entered...");
                // If the DataObject contains string data, extract it.
                if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                    string firstFileName = files[0].ToString();

                    DragAndDrop.Visibility = Visibility.Hidden;
                    Copy_Button.Visibility = Visibility.Visible;
                    Clear_Button.Visibility = Visibility.Visible;
                    AddProperties_Button.Visibility = Visibility.Visible;

                    GenerateAndPrintSummary(firstFileName);

                    // If the string can be converted into a Brush,
                    // convert it and apply it to the ellipse.
                    BrushConverter converter = new BrushConverter();
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
