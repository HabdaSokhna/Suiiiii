namespace BLL.DTO.Report
{
    /// <summary>
    /// النسخة الخفيفة للبلاغ - تستخدم في قائمة الـ History (قائمة البلاغات)
    /// </summary>
    public class ReportSummary_Dto
    {
        public int Report_ID { get; set; }
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// التصنيف النهائي الجاهز للعرض (سواء المختار يدوياً أو المكتشف بالذكاء الاصطناعي)
        /// </summary>
        public string DisplayCategory { get; set; } = string.Empty;

        /// <summary>
        /// الرابط الكامل للصورة (Absolute URL)
        /// </summary>
        public string? PhotoUrl { get; set; }

        public string Status { get; set; } = "Pending";
        public DateTime SubmittedAt { get; set; }
    }

    /// <summary>
    /// النسخة الكاملة للبلاغ - تستخدم في صفحة تفاصيل البلاغ فقط
    /// </summary>
    public class Report_Dto : ReportSummary_Dto
    {
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        // تفاصيل إضافية للشفافية مع المستخدم
        public string? UserSelectedCategory { get; set; }
        public string? AiPredictedCategory { get; set; }
        public float ConfidenceScore { get; set; }

        /// <summary>
        /// قائمة الجهات التي تعاملت مع البلاغ
        /// </summary>
        public List<HandleInfo> Handles { get; set; } = new();
    }

    public class HandleInfo
    {
        public string AuthorityName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }

    public class ReportFilterDto
    {
        public string? Category { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PagedResult<T>
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<T> Data { get; set; } = new();
    }
}