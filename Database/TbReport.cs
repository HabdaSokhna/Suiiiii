using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Database
{
    public class TbReport
    {
        //Primary Key
        [Key]
        public int Report_ID { get; set; }

        [Required(ErrorMessage = "احكي اللي حصل")]
        [StringLength(1000)]
        //Description For Report
        public string Report_Description { get; set; } = string.Empty;

        //Location 
        [Required(ErrorMessage = "الموقع الجغرافي مطلوب")]
        [StringLength(150)] // وسعنا المساحة للإحداثيات
        public string Report_GeoLocation { get; set; } = string.Empty;
       
        public DateTime Report_Submit { get; set; }

        [Required(ErrorMessage = "يجب تحديد فئة البلاغ")]
        //Category From user input
        public string Report_Category { get; set; } = string.Empty;

        //Category From Ai After Processing
        public string Report_PredictedCategory { get; set; } = string.Empty;
        //Path Photo 
        public string PhotoPath { get; set; } = string.Empty;

        //Accuracy ratio 
        [Range(0, 100)]
        public decimal Confidence_Score { get; set; } 

        //Status For Report Get Data For Api ( Ai Agent )
        public string Status { get; set; } = "In Progress"; 
        //Time For Data processing 
        public DateTime AiTime { get; set; }
        //RelationShip
        //Cilizen One Report Many
        public int Citizen_ID { get; set; }
        [ForeignKey("Citizen_ID")]
        public virtual TbCitizen? Citizen { get; set; }
        //One handle Many Reports
        public virtual ICollection<TbHandle> LstHandle { get; set; }

        public TbReport()
        {
            LstHandle = new HashSet<TbHandle>();
        }
    }

}
