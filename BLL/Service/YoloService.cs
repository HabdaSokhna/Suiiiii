using BLL.DTO.Ai_Model;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BLL.Service
{
    public class YoloService : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _serviceTag;
        private const int ModelInputSize = 640;
        private readonly string[] _inputNames;

        public YoloService(string modelPath, string serviceTag)
        {
            
            var options = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                ExecutionMode = ExecutionMode.ORT_SEQUENTIAL
            };

            _session = new InferenceSession(modelPath, options);
            _serviceTag = serviceTag;
            _inputNames = _session.InputMetadata.Keys.ToArray();
        }

        public PredictionResult_Dto AnalyzeImage(byte[] imageBytes)
        {
            using var image = Image.Load<Rgb24>(imageBytes);
            int orgWidth = image.Width;
            int orgHeight = image.Height;

            
            var (letterboxed, scale, padX, padY) = LetterboxImage(image, ModelInputSize);

            
            var inputTensor = Preprocess(letterboxed);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(_inputNames[0], inputTensor)
            };

    
            using var results = _session.Run(inputs);
            var output = results.First().AsTensor<float>();

            // 4. Post-process - معالجة المخرجات ورسم الصناديق
            var boxes = ParseOutput(output, orgWidth, orgHeight, scale, padX, padY);
            var finalBoxes = ApplyNMS(boxes, 0.45f);

            if (finalBoxes.Any())
            {
                // نأخذ أعلى نتيجة ثقة
                var best = finalBoxes.OrderByDescending(b => b.Confidence).First();
                return new PredictionResult_Dto
                {
                    Tag = _serviceTag,
                    Confidence = (float)Math.Round(best.Confidence, 2)
                };
            }

            return new PredictionResult_Dto { Tag = "None", Confidence = 0 };
        }

        private (Image<Rgb24> image, float scale, int padX, int padY) LetterboxImage(Image<Rgb24> image, int targetSize)
        {
           
            float scale = Math.Min((float)targetSize / image.Width, (float)targetSize / image.Height);

            int newWidth = (int)Math.Round(image.Width * scale);
            int newHeight = (int)Math.Round(image.Height * scale);

            
            int padX = (targetSize - newWidth) / 2;
            int padY = (targetSize - newHeight) / 2;

            
            var result = new Image<Rgb24>(targetSize, targetSize, new Rgb24(114, 114, 114));

            
            var resized = image.Clone(x => x.Resize(new ResizeOptions
            {
                Size = new Size(newWidth, newHeight),
                Sampler = KnownResamplers.Triangle
            }));

            
            result.Mutate(x => x.DrawImage(resized, new Point(padX, padY), 1f));

            return (result, scale, padX, padY);
        }

        private DenseTensor<float> Preprocess(Image<Rgb24> image)
        {
            var tensor = new DenseTensor<float>(new[] { 1, 3, ModelInputSize, ModelInputSize });

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < ModelInputSize; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < ModelInputSize; x++)
                    {
                        
                        tensor[0, 0, y, x] = row[x].R / 255f;
                        tensor[0, 1, y, x] = row[x].G / 255f;
                        tensor[0, 2, y, x] = row[x].B / 255f;
                    }
                }
            });

            return tensor;
        }

        private List<BoxPrediction> ParseOutput(Tensor<float> output, int orgWidth, int orgHeight, float scale, int padX, int padY)
        {
            var boxes = new List<BoxPrediction>();

            
            int numAnchors = output.Dimensions[2];
            int numClasses = output.Dimensions[1] - 4;

            for (int i = 0; i < numAnchors; i++)
            {
                
                float maxClassScore = 0f;
                int classId = 0;

                for (int c = 0; c < numClasses; c++)
                {
                    float score = output[0, 4 + c, i];
                    if (score > maxClassScore)
                    {
                        maxClassScore = score;
                        classId = c;
                    }
                }

                if (maxClassScore < 0.25f) continue;

                
                float cx = output[0, 0, i];
                float cy = output[0, 1, i];
                float w = output[0, 2, i];
                float h = output[0, 3, i];

                
                float x1 = (cx - w / 2f - padX) / scale;
                float y1 = (cy - h / 2f - padY) / scale;
                float x2 = (cx + w / 2f - padX) / scale;
                float y2 = (cy + h / 2f - padY) / scale;

                boxes.Add(new BoxPrediction
                {
                    X = Math.Clamp(x1, 0, orgWidth),
                    Y = Math.Clamp(y1, 0, orgHeight),
                    Width = Math.Clamp(x2 - x1, 0, orgWidth),
                    Height = Math.Clamp(y2 - y1, 0, orgHeight),
                    Confidence = maxClassScore,
                    ClassId = classId
                });
            }

            return boxes;
        }

        private List<BoxPrediction> ApplyNMS(List<BoxPrediction> boxes, float iouThreshold)
        {
            var sorted = boxes.OrderByDescending(b => b.Confidence).ToList();
            var results = new List<BoxPrediction>();

            while (sorted.Count > 0)
            {
                var best = sorted[0];
                results.Add(best);
                sorted.RemoveAt(0);

                for (int i = sorted.Count - 1; i >= 0; i--)
                {
                    if (CalculateIoU(best, sorted[i]) > iouThreshold)
                    {
                        sorted.RemoveAt(i);
                    }
                }
            }

            return results;
        }

        private float CalculateIoU(BoxPrediction a, BoxPrediction b)
        {
            float x1 = Math.Max(a.X, b.X);
            float y1 = Math.Max(a.Y, b.Y);
            float x2 = Math.Min(a.X + a.Width, b.X + b.Width);
            float y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

            float intersection = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
            float areaA = a.Width * a.Height;
            float areaB = b.Width * b.Height;
            float union = areaA + areaB - intersection;

            return union <= 0 ? 0 : intersection / union;
        }

        public void Dispose() => _session?.Dispose();
    }

    public class BoxPrediction
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Confidence { get; set; }
        public int ClassId { get; set; }
    }
}