using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Threading;

namespace SummarizerPlugin
{
    public partial class SummarizerControl : System.Windows.Controls.UserControl
    {
        private TextFile? _activeTextFile;
        private int _desiredSummaryLength;

        public SummarizerControl()
        {
            InitializeComponent();
            _desiredSummaryLength = 4;
        }

        private async void UpdateStatusMessage(string message, int duration = 3)
        {
            StatusMessage.Visibility = Visibility.Visible;
            StatusMessage.Text = message;
            StatusMessage.Foreground = new SolidColorBrush(Colors.MediumSeaGreen);
            if (message.ToLower().Contains("error"))
            {
                StatusMessage.Foreground = new SolidColorBrush(Colors.PaleVioletRed);
            }

            // Keep message display duration \in [3, 10] seconds
            await Task.Run(() => Thread.Sleep(
                (duration > 10 ? 10 : duration < 3 ? 3 : duration) * 1000));

            StatusMessage.Visibility = Visibility.Hidden;
        }

        private void GenerateAndPrintSummary(TextFile textFile)
        {
            try
            {
                string summaryText = Summarizer.SummarizeByLSA(textFile!);
                textFile.Summary = summaryText;
                SummaryText.Text = summaryText;

                string documentConcepts = "";
                if (textFile.DocumentConcepts != null)
                {
                    foreach (string s in textFile.DocumentConcepts)
                    {
                        documentConcepts += s + ", ";
                    }
                }

                Run r1 = new("File name: " + textFile.Name);
                Run r2 = new("Concepts: " + documentConcepts.Remove(documentConcepts.Length - 2));
                Run r3 = new("Location: " + textFile.FullPath);

                FileInfo.Inlines.Clear();
                FileInfo.Inlines.Add(r1);
                FileInfo.Inlines.Add(new LineBreak());
                FileInfo.Inlines.Add(r2);
                FileInfo.Inlines.Add(new LineBreak());
                FileInfo.Inlines.Add(r3);

                MDCardSummary.Visibility = Visibility.Visible;
                MDCardFileInfo.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ProcessSelectedFiles(string[] fileNames)
        {
            /* 
             * Handle improper file types
             */
            Form wf = new() { Size = new System.Drawing.Size(0, 0) };
            Task.Delay(TimeSpan.FromSeconds(7))
                .ContinueWith((t) => wf.Close(), TaskScheduler.FromCurrentSynchronizationContext());

            if (fileNames.Length == 1)
            {
                string fileExt = System.IO.Path.GetExtension(fileNames[0]);

                if (fileExt == ".docx" || fileExt == ".odt" || fileExt == ".pdf")
                {
                    AddProperties_Button.Visibility = Visibility.Visible;
                }
                else if (fileExt == ".txt" /* Txt file properties cannot be updated */)
                {
                    AddProperties_Button.Visibility = Visibility.Hidden;
                }
                else
                {
                    string message = "Unsupported file type selected: " + fileExt +
                        "\nType must be .docx, .pdf, .odt, or .txt";
                    string caption = "Unsupported file type...";
                    System.Windows.Forms.MessageBox.Show(wf, message, caption);
                    return;
                }

                _activeTextFile = new(fileNames[0]);
                _activeTextFile.DesiredSummaryLength = _desiredSummaryLength;
                GenerateAndPrintSummary(_activeTextFile);

                Regen_Button.Visibility = Visibility.Visible;
                DragAndDrop.Visibility = Visibility.Hidden;
                Copy_Button.Visibility = Visibility.Visible;
                Clear_Button.Visibility = Visibility.Visible;
            }
            else if (fileNames.Length > 1)
            {
                string overallSummary = "";
                string? overallPath = System.IO.Path.GetDirectoryName(fileNames[0]);
                foreach (string filePath in fileNames)
                {
                    string fileExt = "";
                    try
                    {
                        fileExt = System.IO.Path.GetExtension(filePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    // Handle invalid file types
                    if (!"docx|odt|pdf|txt".Contains(fileExt))
                    {
                        continue;
                    }

                    _activeTextFile = new(filePath);
                    overallSummary += Summarizer.SummarizeByLSA(_activeTextFile);
                }

                if (overallSummary.Length == 0)
                {
                    string message = "No supported file types were selected. " +
                        "\nType must be .docx, .pdf, .odt, or .txt";
                    string caption = "Unsupported file types...";
                    System.Windows.Forms.MessageBox.Show(wf, message, caption);
                    return;
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
            int? activeTextFileLength = _activeTextFile == null || _activeTextFile.DocumentLength == null ?
                                        10 : _activeTextFile.DocumentLength.Value;
            ComboBoxViewModel cbvm = new(activeTextFileLength.Value);
            var result = await MaterialDesignThemes.Wpf.DialogHost.Show(cbvm);
            if (result != null && (bool)result && cbvm.SelectedValueOne != null)
            {
                _desiredSummaryLength = cbvm.SelectedValueOne.Value;
            }
        }

        private void BrowseLocal_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new()
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
            Clear_Click(sender, e);
            GenerateAndPrintSummary(_activeTextFile!);
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
            Regen_Button.Visibility = Visibility.Hidden;
            Copy_Button.Visibility = Visibility.Hidden;
            Clear_Button.Visibility = Visibility.Hidden;
            AddProperties_Button.Visibility = Visibility.Hidden;
        }

        private void UpdateFileProperties_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTextFile == null) return;
            if (System.Windows.MessageBox.Show(
                "Update file's properties with summary and concepts?",
                "Update file properties", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                UpdateStatusMessage(_activeTextFile.UpdateFileProperties(), 3);
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

        private Brush? _previousFill = null;
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
                    BrushConverter converter = new();
                    if (converter.IsValid(dataString))
                    {
                        Brush? newFill = converter.ConvertFromString(dataString) as Brush;
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
                BrushConverter converter = new();
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
                    BrushConverter converter = new();
                    string firstFileName = fileNames[0].ToString();
                    if (converter.IsValid(firstFileName))
                    {
                        Brush? newFill = converter.ConvertFromString(firstFileName) as Brush;
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
        private string? _selectedTextTwo;
        private string? _selectedValidationOutlined;
        private string? _selectedValidationFilled;

        public IList<int> LongListToTestComboVirtualization { get; }
        public IList<string> ShortStringList { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ComboBoxViewModel(int activeDocLength)
        {
            //int activeDocLength = InstancePipeline.GetActiveDocumentLength();
            activeDocLength = activeDocLength > 0 ? activeDocLength : 1;
            int maxSummaryLength = activeDocLength > 10 ? 10 : activeDocLength;
            ShortStringList = new string[maxSummaryLength];
            for (int i = 1; i <= maxSummaryLength; ++i)
            {
                ShortStringList[i - 1] = i.ToString();
            }
            LongListToTestComboVirtualization = new List<int>(Enumerable.Range(2, maxSummaryLength + 1));
            SelectedValueOne = LongListToTestComboVirtualization.Skip(2).First();
            SelectedTextTwo = null;
        }

        public int? SelectedValueOne
        {
            get => _selectedValueOne;
            set => this.MutateVerbose(ref _selectedValueOne, value, RaisePropertyChanged());
        }

        public string? SelectedTextTwo
        {
            get => _selectedTextTwo;
            set => NotifyPropertyChangedExtension.MutateVerbose(this, ref _selectedTextTwo, value, RaisePropertyChanged());
        }

        public string? SelectedValidationFilled
        {
            get => _selectedValidationFilled;
            set => NotifyPropertyChangedExtension.MutateVerbose(this, ref _selectedValidationFilled, value, RaisePropertyChanged());
        }

        public string? SelectedValidationOutlined
        {
            get => _selectedValidationOutlined;
            set => NotifyPropertyChangedExtension.MutateVerbose(this, ref _selectedValidationOutlined, value, RaisePropertyChanged());
        }


        private System.Action<PropertyChangedEventArgs> RaisePropertyChanged()
        {
            return args => PropertyChanged?.Invoke(this, args);
        }
    }

    public class InstancePipeline
    {
        public static int GetActiveDocumentLength()
        {
            return 10;
        }
    }

    public static class NotifyPropertyChangedExtension
    {
        public static bool MutateVerbose<TField>(this INotifyPropertyChanged _, ref TField field, TField newValue, Action<PropertyChangedEventArgs> raise, [CallerMemberName] string? propertyName = null)
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
