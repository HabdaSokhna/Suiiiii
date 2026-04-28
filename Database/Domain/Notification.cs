using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Domain
{
    public class Notification
    {
        [Key]
        public int Notification_ID { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        
        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        
        public int? Report_ID { get; set; }
        [ForeignKey("Report_ID")]
        public virtual Report? Report { get; set; }

        
        [Required]
        public int Citizen_ID { get; set; }

        [ForeignKey("Citizen_ID")]
        public virtual Citizen? Citizen { get; set; } 

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}