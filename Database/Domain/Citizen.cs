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
     
        public string? DeviceToken { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;


        [ForeignKey("ApplicationUserId")]
        public virtual ApplicationUser User { get; set; } = default!;

        
        public virtual ICollection<Citizen_Phone> LstPhone { get; set; } = default!;
        public virtual ICollection<Report> LstReport { get; set; } = default!;


        public Citizen()
        {
           
            LstPhone = new HashSet<Citizen_Phone>();
            LstReport = new HashSet<Report>();
        }
    }
}
