
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Database
{
    public class TbAuthority
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
        //RelationShip

        //One Authority Many AuthorityContacts
        public virtual ICollection<TbAuthority_Contact> LstAuthorityContacts { get; set; }

        //Mane Authority Many Reports ( Handle Between Reports and Authority ) 
        //One Authority Many Handle
        public virtual ICollection<TbHandle> LstHandle { get; set; }
        #endregion


        #region Constractors
        public TbAuthority()
        {
            LstAuthorityContacts = new HashSet<TbAuthority_Contact>();
            LstHandle = new HashSet<TbHandle>();
        } 
        #endregion

    }
}
