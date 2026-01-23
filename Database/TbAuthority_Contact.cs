using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Database
{
    public class TbAuthority_Contact
    {
        //PrimaryKey
        [Key]
        public int Contact_Id { get; set; }

        //Required and MinLength = 5 and MaxLength = 500 
        [Required]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "بيانات التواصل يجب أن تكون بين 5 و 200 حرف")]
        public string Contact_Info { get; set; } = string.Empty;

        //RelationShip
        //One Authority Many Contact_Info
        public int Authority_ID { get; set; }
        [ForeignKey("Authority_ID")]
        public virtual TbAuthority? Authority { get; set; }
    }
}
