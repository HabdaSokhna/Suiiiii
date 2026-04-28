using System.Text.Json.Serialization;

namespace BLL.DTO.Notification
{
    public class NotificationResponse_Dto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("is_read")] 
        public bool IsRead { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
       
        [JsonPropertyName("time_ago")]
        public string TimeAgo => CalculateTimeAgo(CreatedAt);

        private string CalculateTimeAgo(DateTime dateTime)
        {
            var span = DateTime.UtcNow.AddHours(3) - dateTime; 
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            return dateTime.ToString("dd/MM/yyyy");
        }
    }
}
