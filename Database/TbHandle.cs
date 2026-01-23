using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Database
{
    public class TbHandle
    {
        #region Composite Primary Key
        public int Report_ID { get; set; }
        public int Authority_ID { get; set; }
        #endregion

        //Status Data processing 
        public string Status { get; set; } = string.Empty;
        //Time remaining for AI analysis
        public DateTime Update_Report {  get; set; }
        //RelationShip
        //One Handle Many Report
        [ForeignKey("Report_ID")]
        public virtual TbReport? Report { get; set; }
        //One Handle Mant Authority
        [ForeignKey("Authority_ID")]
        public virtual TbAuthority? Authority { get; set; }
    }
}
