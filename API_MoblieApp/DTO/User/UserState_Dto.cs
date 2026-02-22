namespace SIRS_API.DTO.User
{
    /// <summary>
    /// Comprehensive DTO representing the full state of a user, 
    /// combining identity profile and activity statistics.
    /// </summary>
    public class UserState_Dto
    {
        /// <summary>
        /// Unique identifier for the user in the Identity system.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// User's full legal name.
        /// </summary>
        /// <example>John Doe</example>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Primary email address used for login and notifications.
        /// </summary>
        /// <example>user@example.com</example>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Optional: User's contact phone number.
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// The date and time when the account was first created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Indicates if the user account has been soft-deleted.
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// List of security roles assigned to the user (e.g., Citizen, Admin).
        /// </summary>
        public List<string> Roles { get; set; } = new();

        /// <summary>
        /// Aggregated statistics regarding the user's reporting activity.
        /// </summary>
        public UserStatistics Statistics { get; set; } = new();
    }

    /// <summary>
    /// Calculated metrics for a specific user's interaction with the reporting system.
    /// </summary>
    public class UserStatistics
    {
        /// <summary>
        /// Cumulative count of all reports submitted by the user.
        /// </summary>
        public int TotalReports { get; set; }

        /// <summary>
        /// Number of reports currently awaiting review or action.
        /// </summary>
        public int PendingReports { get; set; }

        /// <summary>
        /// Number of reports that have been successfully addressed.
        /// </summary>
        public int ResolvedReports { get; set; }

        /// <summary>
        /// Number of reports that were dismissed or rejected by authorities.
        /// </summary>
        public int RejectedReports { get; set; }

        /// <summary>
        /// Timestamp of the most recently submitted report. Returns null if no reports exist.
        /// </summary>
        public DateTime? LastReportDate { get; set; }
    }
}