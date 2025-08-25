using System;
using System.ComponentModel.DataAnnotations;

namespace hrms.Models
{
    // This model represents ONLY the data on the form
    public class LeaveRequestViewModel
    {
        [Required(ErrorMessage = "Please select a leave type.")]
        [Display(Name = "Leave Type")]
        public string LeaveType { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        public string? Reason { get; set; }
    }
}