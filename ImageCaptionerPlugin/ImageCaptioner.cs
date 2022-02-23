using System;
using System.IO;
using PluginBase;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Http;

namespace ImageCaptionerPlugin
{
    public class ImageCaptioner : IComponent
    {
        public string Name { get => "ImageCaptioner"; }
        public string Description { get => "Generate image captions in batches or one-by-one."; }
        public string Function { get => "Caption"; }
        public string Author { get => "JS"; }
        public string Version { get => "2.0.0"; }
        public Type Control => typeof(ImageCaptionerControl);
        public int Execute()
        {
            Console.WriteLine("Hello, Image Captioner.");
            return 0;
        }
        public static string? CaptionImage(ImageFile imageFile)
        {
            // Post image byte array to image-captioner model server
            var image = imageFile.GetByteArray();
            Console.WriteLine(image.Length);
            ByteArrayContent bac = new(image, 0, image.Length);
            bac.Headers.Add("Content-Type", "application/form-data");
            MultipartFormDataContent form = new()
            {
                { bac, "image", imageFile.Name! }
            };
            string url = $"http://127.0.0.1:5000/caption/{imageFile.Name}";
            Task<HttpResponseMessage> response = new HttpClient().PostAsync(url, form);

            // Parse server response to obtain image caption
            string? caption = null;
            if (response.Result.StatusCode == HttpStatusCode.OK)
            {
                string? temp = response.Result.Content.ReadAsStringAsync().Result.ToString();
                Regex rx = new(@"(?<=caption"": "")(.*)(?= <end>)");
                var matches = rx.Matches(temp!);
                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        caption += match.Value;
                    }
                }
            }
            imageFile.Caption = caption;
            return caption;
        }
        public static async Task<string> CaptionImageLocal(ImageFile imageFile)
        {
            return await Task.Run(() =>
            {
                var processStartInfo = new ProcessStartInfo()
                {
                    FileName = "%LOCALAPPDATA%\\Progams\\Python\\Python310\\python.exe",
                    Arguments = "caption.py " + imageFile.FullPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                Process process = new();
                process.StartInfo = processStartInfo;
                process.Start();
                StreamReader sr = process.StandardOutput;
                string? captionString;
                captionString = sr.ReadLine();
                return captionString!;
            });
        }
    }
}
