namespace SIRS_API.DTO.Report
{
    /// <summary>
    /// Represents the detailed information of a report for display purposes.
    /// </summary>
    public class Report_Dto
    {
        /// <summary>
        /// The unique identifier for the report.
        /// </summary>
        /// <example>1024</example>
        public int Report_ID { get; set; }

        /// <summary>
        /// The headline or brief summary of the incident.
        /// </summary>
        /// <example>Water Leakage</example>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed explanation provided by the citizen.
        /// </summary>
        /// <example>There is a major pipe burst at the corner of the street.</example>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Geographic location or address where the incident occurred.
        /// </summary>
        /// <example>30.0444, 31.2357</example>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// The category manually selected by the user.
        /// </summary>
        /// <example>Infrastructure</example>
        public string? Category { get; set; }

        /// <summary>
        /// The category suggested by the AI model after analyzing the report content or photo.
        /// </summary>
        /// <example>Utilities</example>
        public string? PredictedCategory { get; set; }

        /// <summary>
        /// Absolute URL to the uploaded incident photo.
        /// </summary>
        /// <example>https://api.sirs.com/uploads/reports/img_2024.jpg</example>
        public string? Photo { get; set; }

        /// <summary>
        /// AI confidence level in the predicted category (Value between 0 and 1).
        /// </summary>
        /// <example>0.95</example>
        public float ConfidenceScore { get; set; }

        /// <summary>
        /// The exact date and time when the report was submitted.
        /// </summary>
        public DateTime SubmittedAt { get; set; }

        /// <summary>
        /// The current global status of the report (e.g., Pending, Resolved, Rejected).
        /// </summary>
        /// <example>Pending</example>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// A list of processing details from various authorities handling this report.
        /// </summary>
        public List<HandleInfo> Handles { get; set; } = new();
    }

    /// <summary>
    /// Contains information about the authority or department handling the report.
    /// </summary>
    public class HandleInfo
    {
        /// <summary>
        /// Name of the governmental or organizational body in charge.
        /// </summary>
        /// <example>Ministry of Public Works</example>
        public string AuthorityName { get; set; } = string.Empty;

        /// <summary>
        /// Specific department within the authority.
        /// </summary>
        /// <example>Water Sewage Dept</example>
        public string Department { get; set; } = string.Empty;

        /// <summary>
        /// Status of the report within this specific department.
        /// </summary>
        /// <example>In Progress</example>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// The last time this department updated the report status.
        /// </summary>
        public DateTime LastUpdated { get; set; }
    }
}