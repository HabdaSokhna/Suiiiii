using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database
{
    public class TbCitizen
    {
        //Primary Key
        [Key]
        public int Citizen_ID { get; set; }
        //Uniqe and Committed to the rules Egyptian national number format and Required
        [Required(ErrorMessage = "الرقم القومي مطلوب")]
        [RegularExpression(@"^[23]\d{13}$", ErrorMessage = "الرقم القومي المصري يجب أن يبدأ بـ 2 أو 3 ومكون من 14 رقمًا")]
        public string Citizen_National_Id {  get; set; } = string.Empty;
        //Required and Max Length = 150 and Mini Length = 3
        [Required(ErrorMessage = "يجب ادخل اسمك")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "الاسم يجب أن يكون بين 3 و 150 حرفاً")]
        public string? Citizen_Name { get; set; } = string.Empty;
        //Required and Strictly follows the rules Email style and Uniqe
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [StringLength(100, ErrorMessage = "الإيميل طويل جداً")]
        public string? Citizen_Email { get; set; } = string.Empty;

        //RelationShip (Citizen_Phone , Report);

        //One for Citizen To Many for Citizen_Phone
        public virtual ICollection<TbCitizen_Phone> LstPhone { get; set; }
        //One for Citizen To Many for Reports
        public virtual ICollection<TbReport> LstReport { get; set; }
        public TbCitizen()
        {
            LstPhone = new HashSet<TbCitizen_Phone>();
            LstReport = new HashSet<TbReport>();
        }
    }
}
