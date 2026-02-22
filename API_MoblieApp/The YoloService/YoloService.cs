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

        // الآن سيعمل هذا السطر لأن الأسماء أصبحت input و output
        _predictor = YoloV8Predictor.Create(modelPath, labels);
    }

    public PredictionResult_Dto AnalyzeImage(byte[] imageBytes)
    {
        using var image = Image.Load<Rgb24>(imageBytes);

        // تأكد من الحجم
        image.Mutate(x => x.Resize(640, 640));

        var predictions = _predictor.Predict(image);

        // سطر التشخيص (مهم جداً)
        System.Diagnostics.Debug.WriteLine($"[AI Debug] Predictions Count: {predictions.Count()}");

        foreach (var res in predictions)
        {
            System.Diagnostics.Debug.WriteLine($"[AI Debug] Found: {res.Label.Name} | Score: {res.Score}");
        }

        var best = predictions.OrderByDescending(p => p.Score).FirstOrDefault();

        if (best == null) return new PredictionResult_Dto { Tag = "No Detection", Confidence = 0 };

        return new PredictionResult_Dto
        {
            Tag = best.Label.Name,
            Confidence = best.Score
        };
    }
}