using hrms.Models;
using System.Collections.Generic;

namespace hrms.ViewModels
{
    public class ProjectDetailViewModel
    {
        public Project Project { get; set; }
        // Team members already ON the project
        public IEnumerable<Employee> AssignedTeamMembers { get; set; }
        // Team members in the dept but NOT on this project
        public IEnumerable<Employee> AvailableTeamMembers { get; set; }
    }
}