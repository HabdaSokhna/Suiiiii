using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database
{
    public class Report
    {
        [Key]
        public int Report_ID { get; set; }

        [Required(ErrorMessage = "احكي اللي حصل")]
        [StringLength(1000)]
        public string Report_Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "الموقع الجغرافي مطلوب")]
        [StringLength(150)]
        public string Report_GeoLocation { get; set; } = string.Empty;

        public DateTime Report_Submit { get; set; } = DateTime.Now;

        public string? Report_Category { get; set; }

        public string? PhotoPath { get; set; }

        [Range(0.0, 1.0, ErrorMessage = "نسبة الثقة يجب أن تكون بين 0 و 1")]
        public float Confidence_Score { get; set; }

        public string AI_Category { get; set; }
        public string? AI_Scores { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;  //Time Pending
        public int UpdatedStatus { get; set; } = 1;
        public DateTime? Solved  { get; set; }  //Time Solved
        public bool IsDeleted { get; set; }

        //RelationShip
        public int Citizen_ID { get; set; }
        [ForeignKey("Citizen_ID")]
        public virtual Citizen? Citizen { get; set; }

        public virtual ICollection<Handle> LstHandle { get; set; }

        public Report()
        {
            LstHandle = new HashSet<Handle>();
        }
    }
}