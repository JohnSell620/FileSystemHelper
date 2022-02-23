using System;
using System.Diagnostics;
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
using Tensorflow;
using Tensorflow.Keras;
using static Tensorflow.Binding;
using static Tensorflow.KerasApi;
using NumSharp;

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

        public static string CaptionImageOnnx(ImageFile imageFile)
        {
            var image = Image.Load<Rgb24>(imageFile.FullPath, out IImageFormat format);

            bool modifyImageFile = false;
            if (!modifyImageFile)
            {
                image.Mutate(x =>
                {
                    x.Resize(new ResizeOptions
                    {
                        Size = new Size(299, 299),
                        Mode = ResizeMode.Crop
                    });
                });
            }
            else
            {
                using (Stream imageStream = new MemoryStream())
                {
                    image.Mutate(x =>
                    {
                        x.Resize(new ResizeOptions
                        {
                            Size = new Size(299, 299),
                            Mode = ResizeMode.Crop
                        });
                    });
                    image.Save(imageStream, format);
                };
            }

            Tensor<float> input = new DenseTensor<float>(new[] { 1, 3, 299, 299 });
            var mean = new[] { 0.485f, 0.456f, 0.406f };
            var stddev = new[] { 0.229f, 0.224f, 0.225f };
            for (int y = 0; y < image.Height; y++)
            {
                Span<Rgb24> pixelSpan = image.GetPixelRowSpan(y);
                for (int x = 0; x < image.Width; x++)
                {
                    input[0, 0, y, x] = ((pixelSpan[x].R / 255f) - mean[0]) / stddev[0];
                    input[0, 1, y, x] = ((pixelSpan[x].G / 255f) - mean[1]) / stddev[1];
                    input[0, 2, y, x] = ((pixelSpan[x].B / 255f) - mean[2]) / stddev[2];
                }
            }

            var inputs1 = new List<NamedOnnxValue>()
            {
                NamedOnnxValue.CreateFromTensor<float>("image", input)
            };

            var imgFtEx_session = new InferenceSession("C:\\Users\\jsell\\source\\repos\\FileSystemHelper\\ImageCaptionerPlugin\\Resources\\model\\ifem.onnx");
            var encoder_session = new InferenceSession("C:\\Users\\jsell\\source\\repos\\FileSystemHelper\\ImageCaptionerPlugin\\Resources\\model\\encoder.onnx");
            var decoder_session = new InferenceSession("C:\\Users\\jsell\\source\\repos\\FileSystemHelper\\ImageCaptionerPlugin\\Resources\\model\\decoder.onnx");

            // image feature extraction layer
            using (var outputs1 = imgFtEx_session.Run(inputs1))
            {
                var input2 = outputs1.First();
                input2.Name = "imageFeatures";
                var inputs2 = new List<NamedOnnxValue>() { input2 };
                Debug.WriteLine(outputs1.Count);

                // encoder
                using (var outputs2 = encoder_session.Run(inputs2))
                {
                    // TODO
                    var input3 = outputs2.First();
                    var inputs3 = new List<NamedOnnxValue>() { input3 };
                    using (var outputs3 = decoder_session.Run(inputs3))
                    {
                        // TODO
                    }
                }
            }

            return "Caption";
        }

        private static Graph ImportGraph(string modelName)
        {
            var graph = new Graph().as_default();
            graph.Import(Path.Combine($"Resources\\model\\data\\{modelName}\\", "saved_model.pb"));
            return graph;
        }

        public static NDArray ReadTensorFromImageFile(string file_name,
                                int input_height = 299,
                                int input_width = 299,
                                int input_mean = 0,
                                int input_std = 255)
        {
            return tf_with<Graph, NDArray>(tf.Graph(), graph =>
            {
                var file_reader = tf.io.read_file(file_name, "file_reader");
                var image_reader = tf.image.decode_jpeg(file_reader, channels: 3, name: "jpeg_reader");
                var caster = tf.cast(image_reader, tf.float32);
                var dims_expander = tf.expand_dims(caster, 0);
                var resize = tf.constant(new int[] { input_height, input_width });
                var bilinear = tf.image.resize_bilinear(dims_expander, resize);
                var sub = tf.subtract(bilinear, new float[] { input_mean });
                var normalized = tf.divide(sub, new float[] { input_std });

                return tf_with<Session, NDArray>(tf.Session(graph), sess => sess.run(normalized));
            });
        }

        public static string CaptionImage(ImageFile imageFile)
        {
            tf.compat.v1.disable_eager_execution();
            string caption = "Caption.";
            using (var ifemGraph = ImportGraph("image_feature_extract_model"))
            {
                var image = ReadTensorFromImageFile(imageFile.FullPath);
                var inputOperationIfem = ifemGraph.get_operation_by_name("input_1");
                var outputOperationIfem = ifemGraph.get_operation_by_name("mixed10");
                var ifemOutput = tf_with<Session, NDArray>(tf.Session(ifemGraph),
                    sess => sess.run(outputOperationIfem.outputs[0],
                    new FeedItem(inputOperationIfem.outputs[0], image)));

                using (var encoderGraph = ImportGraph("encoder"))
                {
                    var inputOperationEncoder = encoderGraph.get_operation_by_name("input_name");
                    var outputOperationEncoder = encoderGraph.get_operation_by_name("output");
                    var encoderOutput = tf_with<Session, NDArray>(tf.Session(encoderGraph),
                        sess => sess.run(outputOperationEncoder.outputs[0],
                        new FeedItem(inputOperationEncoder.outputs[0], image)));

                    using (var decoderGraph = ImportGraph("encoder"))
                    {
                        var inputOperationDecoder = decoderGraph.get_operation_by_name("input_name");
                        var outputOperationDecoder = decoderGraph.get_operation_by_name("output");
                        var decoderOutput = tf_with<Session, NDArray>(tf.Session(decoderGraph),
                            sess => sess.run(outputOperationEncoder.outputs[0],
                            new FeedItem(inputOperationEncoder.outputs[0], image)));

                        caption = decoderOutput.ToString();
                    }
                }
            }
            return caption;
        }
    }
}
