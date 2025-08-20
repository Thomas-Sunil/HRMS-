using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace hrms.Models
{
    [Table("performance_reviews")]
    public class PerformanceReview
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("rating")]
        [Range(1, 5)]
        public int Rating { get; set; }

        [Column("feedback")]
        public string Feedback { get; set; }

        [Column("review_date")]
        public DateTime ReviewDate { get; set; }

        [Column("project_id")]
        public int? ProjectId { get; set; }
        public Project Project { get; set; }

        [Column("employee_id")]
        public int? EmployeeId { get; set; }
        public Employee Employee { get; set; }

        [Column("manager_id")]
        public int? ManagerId { get; set; }
        public Employee Manager { get; set; }
    }
}