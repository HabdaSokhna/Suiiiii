using System.ComponentModel.DataAnnotations;

namespace SIRS_API.DTO.Report
{
    /// <summary>
    /// Data Transfer Object for creating a new incident report.
    /// Supports multipart/form-data for image uploads.
    /// </summary>
    public class ReportCreate_Dto
    {
        /// <summary>
        /// A brief, descriptive title of the incident.
        /// </summary>
        /// <example>Large Pothole on Main St.</example>
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 100 characters")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed information about the report, including observations or specific concerns.
        /// </summary>
        /// <example>There is a deep pothole in the middle of the road causing traffic delays.</example>
        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 1000 characters")]
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// Geographic coordinates of the incident in (latitude, longitude) format.
        /// </summary>
        /// <example>30.0444, 31.2357</example>
        [Required(ErrorMessage = "Geographic location is required")]
        [RegularExpression(@"^-?\d+\.?\d*,\s*-?\d+\.?\d*$",
               ErrorMessage = "Location format must be: latitude,longitude")]
        public string Location { get; set; } = string.Empty;
        /// <summary>
        /// Optional: The user-selected category for the report.
        /// If omitted, the system will use AI to predict the category.
        /// </summary>
        /// <example>Infrastructure</example>
        [StringLength(50)]
        public string? Category { get; set; }

        /// <summary>
        /// Optional: An image file (JPEG/PNG) providing visual evidence of the incident.
        /// </summary>
        public IFormFile? Photo { get; set; }
        /// <summary>
        /// New: To store the AI confidence level in the database if needed.
        /// </summary>
        public float? AiConfidence { get; set; }
    }
}