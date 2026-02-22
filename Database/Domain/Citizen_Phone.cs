using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database
{
    public class Citizen_Phone
    {
        [Key]
        public int Phone_Id { get; set; }

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [StringLength(11, MinimumLength = 11)]
        [RegularExpression(@"^01[0125]\d{8}$", ErrorMessage = "يجب أن يكون رقم موبايل مصري صحيح")]
        public string Phone_Number { get; set; } = string.Empty;

        //RelationShip
        public int Citizen_ID { get; set; }
        [ForeignKey("Citizen_ID")]
        public virtual Citizen Citizen { get; set; } = null!;
    }
}