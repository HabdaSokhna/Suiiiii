using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Database
{
    public class TbCitizen_Phone
    {
        //Primary Key
        [Key]
        public int Phone_Id { get; set; } // إضافة ID تلقائي أفضل كمفتاح أساسي

        //Phone Number for Ciltizen
        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [StringLength(11, MinimumLength = 11)]
        [RegularExpression(@"^01[0125]\d{8}$", ErrorMessage = "يجب أن يكون رقم موبايل مصري صحيح")]
        public string Phone_Number { get; set; } = string.Empty;
        //RelationShip

        //One Cilizen Many Phone_Numbers
        public int Citizen_ID { get; set; }

        [ForeignKey("Citizen_ID")]
        public virtual TbCitizen Citizen { get; set; } = null!;
    }
}
