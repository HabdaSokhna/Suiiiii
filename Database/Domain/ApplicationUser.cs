using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace Database.Domain
{
    public class ApplicationUser : IdentityUser
    {
        // مسار الصورة الشخصية (يُخزن هنا بناءً على طلبك)
        [MaxLength(255)]
        public string? ProfilePhotoPath { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public bool IsDeleted { get; set; } = false;
       
        public virtual Citizen CitizenProfile { get; set; } = default!;
        public string? DeviceToken { get; set; }
        public string? TwoFactorSecret { get; set; }
        public bool TwoFactorEnabled { get; set; }
    }
}