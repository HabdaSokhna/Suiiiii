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

        /// <summary>
        /// Type of notification: 'report', 'system', or 'update'
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        // الربط بالبلاغ اختياري (Nullable)
        public int? Report_ID { get; set; }
        [ForeignKey("Report_ID")]
        public virtual Report? Report { get; set; }

        // الربط بالمواطن إجباري
        [Required]
        public int Citizen_ID { get; set; }

        [ForeignKey("Citizen_ID")]
        public virtual Citizen? Citizen { get; set; } // تأكد من اسم الكلاس عندك TbCitizen أو Citizen

        public bool IsRead { get; set; } = false;

        // نصيحة: استخدم DateTime.UtcNow لتوحيد التوقيت بين الموبايل والسيرفر
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}