namespace BLL.DTO.Report
{
    /// <summary>
    /// Detailed data transfer object for reading report information, 
    /// typically used in administrative views including reporter details.
    /// </summary>
    public class ReportRead_Dto
    {
        /// <summary>
        /// Unique primary key for the report.
        /// </summary>
        /// <example>5012</example>
        public int Report_ID { get; set; }

        /// <summary>
        /// The full content of the report including user-provided details.
        /// </summary>
        /// <example>Severe flooding detected at the main intersection.</example>
        public string Report_Description { get; set; } = string.Empty;

        /// <summary>
        /// Exact geographic coordinates (latitude, longitude).
        /// </summary>
        /// <example>30.0444, 31.2357</example>
        public string Report_GeoLocation { get; set; } = string.Empty;

        /// <summary>
        /// The date and time the report was officially submitted.
        /// </summary>
        public DateTime Report_Submit { get; set; }

        /// <summary>
        /// The category assigned by the citizen.
        /// </summary>
        public string? Report_Category { get; set; }

        /// <summary>
        /// The category suggested by the AI analysis system.
        /// </summary>
        /// <example>Natural Disaster</example>
        public string? Report_PredictedCategory { get; set; }

        /// <summary>
        /// The relative or absolute path to the stored incident image.
        /// </summary>
        public string? PhotoPath { get; set; }

        /// <summary>
        /// The AI's certainty level regarding the predicted category (0.0 to 1.0).
        /// </summary>
        /// <example>0.89</example>
        public float Confidence_Score { get; set; }

        /// <summary>
        /// System timestamp when the record was created in the database.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The full name of the citizen who filed the report.
        /// </summary>
        /// <example>Ahmed Mohamed</example>
        public string CitizenName { get; set; } = string.Empty;

        /// <summary>
        /// The email address of the citizen for contact or identification.
        /// </summary>
        /// <example>citizen@example.com</example>
        public string CitizenEmail { get; set; } = string.Empty;
    }
}