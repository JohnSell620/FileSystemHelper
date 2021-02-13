using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PluginBase;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats;
using System.IO;
using SixLabors.ImageSharp.Processing;
//using Tensorflow;
//using Tensorflow.Keras;
//using static Tensorflow.Binding;
//using static Tensorflow.KerasApi;
//using NumSharp;
//using TensorFlow;

namespace ImageCaptionerPlugin
{
    public class ImageCaptioner : IComponent
    {
        public string Name { get => "Captioner"; }
        public string Description { get => "Generate image captions in batches or one-by-one."; }
        public string Function { get => "Caption"; }
        public string Author { get => "JS"; }
        public string Version { get => "1.0.0"; }
        public Type Control => typeof(CaptionerControl);
        public int Execute()
        {
            Console.WriteLine("Hello, Image Captioner.");
            return 0;
        }

        public static string CaptionImage(ImageFile imageFile)
        {
            //var image = Image.Load<Rgb24>(imageFile.FullPath, out IImageFormat format);
            //image.Mutate(x =>
            //{
            //    x.Resize(new ResizeOptions
            //    {
            //        Size = new Size(299, 299),
            //        Mode = ResizeMode.Crop
            //    });
            //});

            //using (var graph = new TFGraph())
            //{
            //    var imgFtEx_model = File.ReadAllBytes("C:\\Users\\jsell\\source\\repos\\FileSystemHelper\\ImageCaptionerPlugin\\Resources\\trained_model\\image_feature_extract_model\\save_model.pb");
            //    graph.Import(new TFBuffer(imgFtEx_model));

            //    Console.WriteLine(graph.CurrentNameScope);

            //    using (var session = new TFSession(graph))
            //    {
            //        var tensor = ImageUtil.CreateTensorFromImageFile(imageFile.FullPath, TFDataType.Float);
            //        var runner = session.GetRunner();
            //        runner.AddInput(graph["image_tensor"][0], tensor);
            //    }

            //}
            return "caption";
        }

        public static string CaptionImageOnnx(ImageFile imageFile)
        {
            //var image = Image.Load<Rgb24>(imageFile.FullPath, out IImageFormat format);

            ////image.Mutate(x =>
            ////{
            ////    x.Resize(new ResizeOptions
            ////    {
            ////        Size = new Size(299, 299),
            ////        Mode = ResizeMode.Crop
            ////    });
            ////});

            //using (Stream imageStream = new MemoryStream())
            //{
            //    image.Mutate(x =>
            //    {
            //        x.Resize(new ResizeOptions
            //        {
            //            Size = new Size(299, 299),
            //            Mode = ResizeMode.Crop
            //        });
            //    });
            //    image.Save(imageStream, format);
            //};

            //Tensor<float> input = new DenseTensor<float>(new[] { 1, 3, 299, 299 });
            //var mean = new[] { 0.485f, 0.456f, 0.406f };
            //var stddev = new[] { 0.229f, 0.224f, 0.225f };
            //for (int y = 0; y < image.Height; y++)
            //{
            //    Span<Rgb24> pixelSpan = image.GetPixelRowSpan(y);
            //    for (int x = 0; x < image.Width; x++)
            //    {
            //        input[0, 0, y, x] = ((pixelSpan[x].R / 255f) - mean[0]) / stddev[0];
            //        input[0, 1, y, x] = ((pixelSpan[x].G / 255f) - mean[1]) / stddev[1];
            //        input[0, 2, y, x] = ((pixelSpan[x].B / 255f) - mean[2]) / stddev[2];
            //    }
            //}
            
            //var inputs1 = new List<NamedOnnxValue>()
            //{
            //    NamedOnnxValue.CreateFromTensor<float>("image", input)
            //};

            //var imgFtEx_session = new InferenceSession("C:\\Users\\jsell\\source\\repos\\FileSystemHelper\\ImageCaptionerPlugin\\Resources\\model\\ifem.onnx");
            ////var encoder_session = new InferenceSession("C:\\Users\\jsell\\source\\repos\\FileSystemHelper\\ImageCaptionerPlugin\\Resources\\model\\encoder.onnx");
            ////var decoder_session = new InferenceSession("C:\\Users\\jsell\\source\\repos\\FileSystemHelper\\ImageCaptionerPlugin\\Resources\\model\\decoder.onnx");

            //// image feature extraction layer
            //using (var outputs1 = imgFtEx_session.Run(inputs1))
            //{
            //    var input2 = outputs1.First();
            //    //input2.Name = "imageFeatures";
            //    //var inputs2 = new List<NamedOnnxValue>() { input2 };
            //    //Console.WriteLine(outputs1.Count);

            //    //// encoder
            //    //using (var outputs2 = encoder_session.Run(inputs2))
            //    //{

            //    //}
            //}

            return "caption";
        }
    }
}
