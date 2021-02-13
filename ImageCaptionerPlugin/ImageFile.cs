using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ImageCaptionerPlugin
{
    public class ImageFile
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public string Extension { get; set; }
        public BitmapImage ImageBmp { get; set; }
        public string Caption { get; set; }

        // Windows properties
        public string[] Title { get; set; }
        public string[] Tags { get; set; }
        public string Subject { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }

        public ImageFile() { }
        public ImageFile(string _fullPath)
        {
            try
            {
                Name = Path.GetFileName(_fullPath);
                FullPath = _fullPath;
                Extension = Path.GetExtension(_fullPath);
                ImageBmp = new BitmapImage(new Uri(_fullPath));
                Height = Convert.ToInt32(ImageBmp.Height);
                Width = Convert.ToInt32(ImageBmp.Width);
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
                if (Tags.Length > 0)
                {
                    // TODO Prompt to elevate privileges...
                    var shellProperties = ShellFile.FromFilePath(FullPath).Properties;
                    shellProperties.System.Keywords.Value = Tags;
                    shellProperties.System.Subject.Value = Caption;

                    //// using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
                    //ShellPropertyWriter spw = shellFile.Properties.GetPropertyWriter();
                    //spw.WriteProperty(SystemProperties.System.Keywords, DocumentConcepts);
                    //spw.WriteProperty(SystemProperties.System.Subject, Summary);
                    //spw.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Source.ToString());
            }

            return "File property updates successful.";
        }
    }
}
