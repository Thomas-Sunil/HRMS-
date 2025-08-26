using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace hrms.Models
{
    public class CreateMeetingViewModel
    {
        [Required] public string Title { get; set; }
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        public DateTime EndTime { get; set; }

        // This will hold the IDs of the employees the manager checks
        public List<int> InvitedEmployeeIds { get; set; } = new List<int>();

        // This will be used to populate the checklist on the form
        public List<Employee>? AvailableTeamMembers { get; set; }
    }
}