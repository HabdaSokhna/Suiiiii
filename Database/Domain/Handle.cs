using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Database
{
    public class Handle
    {
        #region Composite Primary Key
        [Key, Column(Order = 0)]
        public int Report_ID { get; set; }

        [Key, Column(Order = 1)]
        public int Authority_ID { get; set; }
        #endregion

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        //RelationShip
        [ForeignKey("Report_ID")]
        public virtual Report? Report { get; set; }

        [ForeignKey("Authority_ID")]
        public virtual Authority? Authority { get; set; }
    }
}