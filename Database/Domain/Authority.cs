

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Database
{
    public class Authority
    {


        #region Properties
        //Primary Key
        [Key]
        public int Authority_ID { get; set; }

        //Max Length = 100 , Min Length = 3 , Required
        [Required(ErrorMessage = "ادخل اسم سلطه المعنية")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "الاسم يجب أن يكون بين 3 و 100 حرف")]
        
        public string Authority_Name { get; set; } = string.Empty;

        //Max Length = 100 , Min Length = 3 , Required
        [Required(ErrorMessage = "ادخل اسم قسم")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "الاسم يجب أن يكون بين 3 و 100 حرف")]
        public string Department_Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "يجب تحديد تخصص الجهة")]
        public string Category { get; set; } = string.Empty;
        //RelationShip

        //One Authority Many AuthorityContacts
        public virtual ICollection<Authority_Contact> LstAuthorityContacts { get; set; }

        //Mane Authority Many Reports ( Handle Between Reports and Authority ) 
        //One Authority Many Handle
        public virtual ICollection<Handle> LstHandle { get; set; }
        #endregion


        #region Constractors
        public Authority()
        {
            LstAuthorityContacts = new HashSet<Authority_Contact>();
            LstHandle = new HashSet<Handle>();
        } 
        #endregion

    }
}
