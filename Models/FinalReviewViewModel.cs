using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace hrms.Models
{
    public class FinalReviewViewModel
    {
        // This holds the ProjectId and Name for the form
        public Project Project { get; set; }

        // This holds the list of items to display on the form
        public List<PerformanceReviewItem> Reviews { get; set; } = new List<PerformanceReviewItem>();
    }

    public class PerformanceReviewItem
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }
        public string? Feedback { get; set; }
    }
}