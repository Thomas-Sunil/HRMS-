using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace hrms.Models
{
    [Table("projects")]
    public class Project
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("deadline")]
        public DateTime? Deadline { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("manager_id")]
        public int? ManagerId { get; set; }
        public Employee Manager { get; set; }

        [Column("employee_id")]
        public int? EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public ICollection<ProjectTask> ProjectTasks { get; set; }
        public PerformanceReview PerformanceReview { get; set; }
    }
}