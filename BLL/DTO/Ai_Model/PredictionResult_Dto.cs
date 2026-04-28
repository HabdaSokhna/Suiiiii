namespace BLL.DTO.Ai_Model
{
    public class PredictionResult_Dto
    {
        public string Tag { get; set; } = "No Detection";
        public float Confidence { get; set; }
    }
}
