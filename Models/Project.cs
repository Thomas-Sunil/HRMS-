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

        // --- THIS IS THE FIX ---
        // We add the ForeignKey attribute to be explicit.
        [Column("manager_id")]
        public int? ManagerId { get; set; }
        [ForeignKey("ManagerId")]
        public Employee Manager { get; set; }
        // --- END FIX ---

        public ICollection<Employee> AssignedEmployees { get; set; } = new List<Employee>();
        public ICollection<ProjectTask> ProjectTasks { get; set; } = new List<ProjectTask>();
        public ICollection<PerformanceReview> PerformanceReviews { get; set; } = new List<PerformanceReview>();
    }
}