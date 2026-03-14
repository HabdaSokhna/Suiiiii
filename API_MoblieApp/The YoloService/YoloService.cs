using SIRS_API.DTO.Ai_Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Yolov8Net;

public class YoloService
{
    private readonly IPredictor _predictor;

    public YoloService(string modelPath)
    {
        // الترتيب الصفر هو smoke والواحد هو fire كما في تدريبك
        var labels = new[] { "smoke", "fire" };
        _predictor = YoloV8Predictor.Create(modelPath, labels);
    }

    public PredictionResult_Dto AnalyzeImage(byte[] imageBytes)
    {
        // سطر تشخيصي ضروري: لو الرقم ده 0 يبقى العيب مش في الكود ده
        System.Diagnostics.Debug.WriteLine($"[AI Debug] Bytes received: {imageBytes?.Length ?? 0}");

        if (imageBytes == null || imageBytes.Length == 0)
            return new PredictionResult_Dto { Tag = "No Data", Confidence = 0 };

        try
        {
            using var image = Image.Load<Rgb24>(imageBytes);

            // الكود القديم اللي كنت شغال بيه
            image.Mutate(x => x.Resize(640, 640));

            var predictions = _predictor.Predict(image);

            // سطر التشخيص (مهم جداً)
            System.Diagnostics.Debug.WriteLine($"[AI Debug] Predictions Count: {predictions.Count()}");

            var best = predictions.OrderByDescending(p => p.Score).FirstOrDefault();

            if (best == null) return new PredictionResult_Dto { Tag = "No Detection", Confidence = 0 };

            return new PredictionResult_Dto
            {
                Tag = best.Label.Name,
                Confidence = best.Score
            };
        }
        catch (Exception ex)
        {
            // عشان لو ضرب Pointer يقولك السبب بدل ما يوقع السيرفر
            System.Diagnostics.Debug.WriteLine($"[AI Error] {ex.Message}");
            return new PredictionResult_Dto { Tag = "Error", Confidence = 0 };
        }
    }
}