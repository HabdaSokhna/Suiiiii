using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Database.Domain
{
    public class Authority_Login
    {
        [Key]
        public int Login_ID { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty; 

        public int Authority_ID { get; set; }

        [ForeignKey("Authority_ID")]
        public virtual Authority Authority { get; set; }
    }
}
