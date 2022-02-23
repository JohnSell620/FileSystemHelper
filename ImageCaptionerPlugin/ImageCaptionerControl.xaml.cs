using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Threading;

namespace ImageCaptionerPlugin
{
    public partial class ImageCaptionerControl : System.Windows.Controls.UserControl
    {
        private ImageFile? _activeImageFile;
        private bool _autoUpdateFileProperties;

        public ImageCaptionerControl()
        {
            InitializeComponent();
            _autoUpdateFileProperties = false;
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

        private void GenerateAndPrintCaption(ImageFile _imageFile)
        {
            try
            {
                SelectedImage.Source = _imageFile.ImageBmp;
                CaptionText.Text = ImageCaptioner.CaptionImage(_imageFile);

                Run r1 = new("File name: " + _imageFile.Name);
                string tags = "Tags: ";
                if (_imageFile.Tags != null)
                {
                    foreach (string tag in _imageFile.Tags)
                    {
                        tags += tag + ", ";
                    }
                    tags = tags.Remove(tags.Length - 2);
                }
                Run r2 = new(tags);
                Run r3 = new("Location: " + _imageFile.FullPath);

                FileInfo.Inlines.Clear();
                FileInfo.Inlines.Add(r1);
                FileInfo.Inlines.Add(new LineBreak());
                FileInfo.Inlines.Add(r2);
                FileInfo.Inlines.Add(new LineBreak());
                FileInfo.Inlines.Add(r3);

                MDCardCaption.Visibility = Visibility.Visible;
                MDCardFileInfo.Visibility = Visibility.Visible;
                MDCardImage.Visibility = Visibility.Visible;
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

                if (fileExt == ".jpg" || fileExt == ".png" || fileExt == ".bmp")
                {
                    AddProperties_Button.Visibility = Visibility.Visible;
                }
                else
                {
                    string message = "Unsupported file type selected: " + fileExt +
                        "\nType must be .jpg, .bmp, or .png";
                    string caption = "Unsupported file type...";
                    System.Windows.Forms.MessageBox.Show(wf, message, caption);
                    return;
                }

                _activeImageFile = new ImageFile(fileNames[0]);
                GenerateAndPrintCaption(_activeImageFile);

                DragAndDrop.Visibility = Visibility.Hidden;
                Copy_Button.Visibility = Visibility.Visible;
                Clear_Button.Visibility = Visibility.Visible;
            }
            else if (fileNames.Length > 1)
            {
                string overallCaption = "";
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
                    if (!"jpg|png|bmp".Contains(fileExt))
                    {
                        continue;
                    }

                    _activeImageFile = new ImageFile(filePath);
                    overallCaption += ImageCaptioner.CaptionImage(_activeImageFile);
                }

                if (overallCaption.Length == 0)
                {
                    string message = "No supported file types were selected. " +
                        "\nType must be .docx, .pdf, .odt, or .txt";
                    string caption = "Unsupported file types...";
                    System.Windows.Forms.MessageBox.Show(wf, message, caption);
                    return;
                }

                _activeImageFile = new ImageFile
                {
                    Caption = overallCaption,
                    Name = overallPath,
                    FullPath = overallPath,
                    Extension = null
                };

                GenerateAndPrintCaption(_activeImageFile);
            }
        }

        private async void Settings_ClickAsync(object sender, RoutedEventArgs e)
        {
            ToggleButtonViewModel tbvm = new(_autoUpdateFileProperties);
            var result = await MaterialDesignThemes.Wpf.DialogHost.Show(tbvm);

            if (result != null && (bool)result)
            {
                _autoUpdateFileProperties = tbvm.IsEnabled;
            }
        }

        private void BrowseLocal_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new()
            {
                InitialDirectory = @"C:\Users\jsell\source\repos\FileSystemHelper\Dev\Images",
                Filter = @"Image files(*.bmp, *.jpg) | *.bmp; *.jpg", // | All files(*.*) | *.* 
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            {
                ProcessSelectedFiles(dlg.FileNames);
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Clipboard.SetText(CaptionText.Text);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            CaptionText.Text = "";
            MDCardCaption.Visibility = Visibility.Hidden;
            MDCardFileInfo.Visibility = Visibility.Hidden;
            MDCardImage.Visibility = Visibility.Hidden;
            DragAndDrop.Visibility = Visibility.Visible;
            Copy_Button.Visibility = Visibility.Hidden;
            Clear_Button.Visibility = Visibility.Hidden;
            AddProperties_Button.Visibility = Visibility.Hidden;
        }

        private void UpdateFileProperties_Click(object sender, RoutedEventArgs e)
        {
            if (_activeImageFile == null) return;
            if (!(bool)_autoUpdateFileProperties)
            {
                if (System.Windows.MessageBox.Show(
                    "Update file's properties with summary and concepts?",
                    "Update file properties", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    UpdateStatusMessage(_activeImageFile.UpdateFileProperties(), 3);
                }
            }
            else
            {
                UpdateStatusMessage(_activeImageFile.UpdateFileProperties(), 3);
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

    public class ToggleButtonViewModel : INotifyPropertyChanged
    {
        private bool _isEnabled;
        private string? _selectedValidationOutlined;
        private string? _selectedValidationFilled;

        public IList<int>? LongListToTestComboVirtualization { get; }
        public IList<string>? ShortStringList { get; }
        public event PropertyChangedEventHandler? PropertyChanged;

        public ToggleButtonViewModel(bool autoUpdateFileProperties)
        {
            _isEnabled = autoUpdateFileProperties;
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => this.MutateVerbose(ref _isEnabled, value, RaisePropertyChanged());
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
            return 0;
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
