﻿using System;
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
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Globalization;

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
                Console.WriteLine(ex.Message); //throw ex;
            }            
        }

        private void ProcessSelectedFiles(string[] fileNames)
        {
            /* 
             * TODO handle improper file types
             */ 
            DragAndDrop.Visibility = Visibility.Hidden;
            Copy_Button.Visibility = Visibility.Visible;
            Clear_Button.Visibility = Visibility.Visible;

            if (fileNames.Length == 1)
            {
                Regen_Button.Visibility = Visibility.Visible;
                string fileExt = System.IO.Path.GetExtension(fileNames[0]);

                // Txt file properties cannot be updated.
                if (fileExt == ".docx" || fileExt == ".odt" || fileExt == ".pdf")
                {
                    AddProperties_Button.Visibility = Visibility.Visible;
                }
                else
                {
                    AddProperties_Button.Visibility = Visibility.Hidden;
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
        
        private async void Settings_ClickAsync(object sender, RoutedEventArgs e)
        {
            object result = await MaterialDesignThemes.Wpf.DialogHost.Show(new ComboBoxViewModel());
        }

        private void BrowseLocal_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".docx",
                InitialDirectory = @"C:\Users\jsell\source\repos\FileSystemHelper\Dev",
                Filter = @"All Files (*.*)|*.*|Text Files (.txt)|*.txt|" +
                         @"Word Documents (.docx)|*.docx|Word Template (.dotx)|*.dotx|" +
                         @"OpenDocument Text Files (.odt)|*.odt|PDF Files (.pdf)|*.pdf",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            {
                ProcessSelectedFiles(dlg.FileNames);
            }
        }

        private void RegenerateSummary_Click(object sender, RoutedEventArgs e)
        {
            SummaryText.Text = _activeTextFile.Summary;
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
            Regen_Button.Visibility = Visibility.Hidden;
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

    public class ComboBoxViewModel : INotifyPropertyChanged
    {
        private int? _selectedValueOne;
        private string _selectedTextTwo;
        private string _selectedValidationOutlined;
        private string _selectedValidationFilled;

        public ComboBoxViewModel()
        {
            LongListToTestComboVirtualization = new List<int>(Enumerable.Range(0, 1000));
            ShortStringList = new[]
            {
                "1",
                "2",
                "3",
                "4 (default)",
                "5",
                "6",
                "7",
                "8",
                "9",
                "10"
            };

            SelectedValueOne = LongListToTestComboVirtualization.Skip(2).First();
            SelectedTextTwo = null;
        }

        public int? SelectedValueOne
        {
            get => _selectedValueOne;
            set => this.MutateVerbose(ref _selectedValueOne, value, RaisePropertyChanged());
        }

        public string SelectedTextTwo
        {
            get => _selectedTextTwo;
            set => NotifyPropertyChangedExtension.MutateVerbose(this, ref _selectedTextTwo, value, RaisePropertyChanged());
        }

        public string SelectedValidationFilled
        {
            get => _selectedValidationFilled;
            set => NotifyPropertyChangedExtension.MutateVerbose(this, ref _selectedValidationFilled, value, RaisePropertyChanged());
        }

        public string SelectedValidationOutlined
        {
            get => _selectedValidationOutlined;
            set => NotifyPropertyChangedExtension.MutateVerbose(this, ref _selectedValidationOutlined, value, RaisePropertyChanged());
        }

        public IList<int> LongListToTestComboVirtualization { get; }
        public IList<string> ShortStringList { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        private System.Action<PropertyChangedEventArgs> RaisePropertyChanged()
        {
            return args => PropertyChanged?.Invoke(this, args);
        }
    }

    public static class NotifyPropertyChangedExtension
    {
        public static bool MutateVerbose<TField>(this INotifyPropertyChanged _, ref TField field, TField newValue, Action<PropertyChangedEventArgs> raise, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<TField>.Default.Equals(field, newValue)) return false;
            field = newValue;
            raise?.Invoke(new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }

    public class NotEmptyValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return string.IsNullOrWhiteSpace((value ?? "").ToString())
                ? new ValidationResult(false, "Field is required.")
                : ValidationResult.ValidResult;
        }
    }
}
