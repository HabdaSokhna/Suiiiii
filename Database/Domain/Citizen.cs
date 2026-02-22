using Database.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database
{
    public class Citizen
    {
        [Key]
        public int Citizen_ID { get; set; }

        [Required(ErrorMessage = "الرقم القومي مطلوب")]
        [RegularExpression(@"^[23]\d{13}$", ErrorMessage = "الرقم القومي المصري يجب أن يبدأ بـ 2 أو 3 ومكون من 14 رقمًا")]
        [StringLength(14, MinimumLength = 14)]
        public string Citizen_National_Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "يجب ادخل اسمك")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "الاسم يجب أن يكون بين 3 و 150 حرفاً")]
        public string Citizen_Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [StringLength(100, ErrorMessage = "الإيميل طويل جداً")]
        public string Citizen_Email { get; set; } = string.Empty;

        // ✅ حقول التدقيق
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsDeleted { get; set; } = false;

        // 🔗 الربط مع الحساب (Foreign Key)
        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        // ✅ تم إضافة = default! لحل مشكلة التحذير في الـ Constructor
        [ForeignKey("ApplicationUserId")]
        public virtual ApplicationUser User { get; set; } = default!;

        // 🔘 العلاقات (Navigation Properties)
        // تم إضافة = default! لأن EF Core سيهتم بتهيئتها عند جلب البيانات
        public virtual ICollection<Citizen_Phone> LstPhone { get; set; } = default!;
        public virtual ICollection<Report> LstReport { get; set; } = default!;

        public Citizen()
        {
            // نترك الـ HashSets لضمان عدم وجود NullReference عند إضافة عناصر جديدة يدوياً
            LstPhone = new HashSet<Citizen_Phone>();
            LstReport = new HashSet<Report>();
        }
    }
}
