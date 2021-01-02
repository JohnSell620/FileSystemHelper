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

namespace SummarizerPlugin
{
    public partial class SummarizerControl : UserControl
    {
        public SummarizerControl()
        {
            InitializeComponent();
        }

        //private void OnExecute()
        //{
        //    string text = @"Historically, the world of data and the world of objects" +
        //     @" have not been well integrated. Programmers work in C# or Visual Basic" +
        //     @" and also in SQL or XQuery. On the one side are concepts such as classes," +
        //     @" objects, fields, inheritance, and .NET APIs. On the other side" +
        //     @" are tables, columns, rows, nodes, and separate languages for dealing with" +
        //     @" them. Data types often require translation between the two worlds; there are" +
        //     @" different standard functions. Because the object world has no notion of query, a" +
        //     @" query can only be represented as a string without compile-time type checking or" +
        //     @" IntelliSense support in the IDE. Transferring data from SQL tables or XML trees to" +
        //     @" objects in memory is often tedious and error-prone.";

        //    this.SummaryText.Text = SummarizerPlugin.Summarizer.SummarizeByLSA(text);
        //}

        private void BrowseLocal_Click(object sender, RoutedEventArgs e)
        {

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = "txt";
            dlg.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.InitialDirectory = @"C:\Users\jsell\source\repos\";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    // Clear previous summary.
                    if (_StackPanel.Children.GetType() == typeof(TextBlock) || _StackPanel.Children.Count > 0)
                    {
                        _StackPanel.Children.Clear();
                    }

                    string textToSummarize = File.ReadAllText(dlg.FileName);
                    string[] textLines = File.ReadAllLines(dlg.FileName);
                    string textSummary = Summarizer.SummarizeByLSA(textToSummarize);
                    //string textSummary = @"Historically, the world of data and the world of objects" +
                    //    @" have not been well integrated. Programmers work in C# or Visual Basic" +
                    //    @" and also in SQL or XQuery. On the one side are concepts such as classes,";

                    TextBlock textBlock = new TextBlock();
                    textBlock.Name = "SummaryText";
                    textBlock.Style = (Style)Application.Current.Resources["MaterialDesignHeadline6TextBlock"];
                    textBlock.Text = textSummary;
                    _StackPanel.Children.Add(textBlock);

                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
        
        private void Ellipse_MouseMove(object sender, MouseEventArgs e)
        {
            Ellipse ellipse = sender as Ellipse;
            if (ellipse != null && e.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(ellipse,
                                    ellipse.Fill.ToString(),
                                    DragDropEffects.Copy);
            }
        }

        private Brush _previousFill = null;
        private void Ellipse_DragEnter(object sender, DragEventArgs e)
        {
            Ellipse ellipse = sender as Ellipse;
            if (ellipse != null)
            {
                // Save the current Fill brush so that you can revert back to this value in DragLeave.
                _previousFill = ellipse.Fill;

                // If the DataObject contains string data, extract it.
                if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    string dataString = (string)e.Data.GetData(DataFormats.StringFormat);

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

        private void Ellipse_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;

            // If the DataObject contains string data, extract it.
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string dataString = (string)e.Data.GetData(DataFormats.StringFormat);

                // If the string can be converted into a Brush, allow copying.
                BrushConverter converter = new BrushConverter();
                if (converter.IsValid(dataString))
                {
                    e.Effects = DragDropEffects.Copy | DragDropEffects.Move;
                }
            }
        }

        private void Ellipse_DragLeave(object sender, DragEventArgs e)
        {
            Ellipse ellipse = sender as Ellipse;
            if (ellipse != null)
            {
                ellipse.Fill = _previousFill;
            }
        }

        private void Ellipse_Drop(object sender, DragEventArgs e)
        {
            Ellipse ellipse = sender as Ellipse;
            if (ellipse != null)
            {
                // If the DataObject contains string data, extract it.
                if (e.Data.GetDataPresent(DataFormats.StringFormat))
                {
                    string dataString = (string)e.Data.GetData(DataFormats.StringFormat);

                    // If the string can be converted into a Brush,
                    // convert it and apply it to the ellipse.
                    BrushConverter converter = new BrushConverter();
                    if (converter.IsValid(dataString))
                    {
                        Brush newFill = (Brush)converter.ConvertFromString(dataString);
                        ellipse.Fill = newFill;
                    }
                }
            }
        }
    }
}
