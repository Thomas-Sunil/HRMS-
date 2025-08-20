using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace hrms.Models
{
    // A single view model for handling project pages
    public class EmployeeProjectsViewModel
    {
        public Employee Employee { get; set; }
        public IEnumerable<Project> Projects { get; set; }

        // Properties for the "Create New Project" form
        [Required]
        [Display(Name = "Project Name")]
        public string NewProjectName { get; set; }
        public string NewProjectDescription { get; set; }
        [DataType(DataType.Date)]
        public DateTime? NewProjectDeadline { get; set; }
    }
}