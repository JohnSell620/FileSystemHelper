using System;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.WindowsAPICodePack.Shell;

namespace ImageCaptionerPlugin
{
    public class ImageFile
    {
        public string? Name { get; set; }
        public string? FullPath { get; set; }
        public string? Extension { get; set; }
        public BitmapImage? ImageBmp { get; set; }
        public string? Caption { get; set; }

        // Windows file properties
        public string[]? Title { get; set; }
        public string[]? Tags { get; set; }
        public string? Subject { get; set; }
        public int? Height { get; set; }
        public int? Width { get; set; }

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

        public byte[] GetByteArray()
        {
            byte[]? iba = null;
            FileStream fileStream = new(FullPath!, FileMode.Open, FileAccess.Read);
            using (BinaryReader reader = new(fileStream))
            {
                iba = new byte[reader.BaseStream.Length];
                for (int i = 0; i < reader.BaseStream.Length; i++)
                    iba[i] = reader.ReadByte();
            }
            return iba;
        }

        public string UpdateFileProperties()
        {
            try
            {
                if (Tags != null && Tags.Length > 0)
                {
                    // TODO Prompt to elevate privileges...
                    var shellProperties = ShellFile.FromFilePath(FullPath).Properties;
                    shellProperties.System.Keywords.Value = Tags;
                    shellProperties.System.Subject.Value = Caption;
                }
            }
            catch (Exception ex)
            {
                if (ex.Source != null)
                {
                    Console.WriteLine(ex.Source.ToString());
                }
            }

            return "File property updates successful.";
        }
    }
}
