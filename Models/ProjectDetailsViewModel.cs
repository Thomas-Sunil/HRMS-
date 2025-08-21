using System.Collections.Generic;
namespace hrms.Models
{
    public class ProjectDetailsViewModel
    {
        public Project Project { get; set; }
        // Team members who are NOT yet assigned to ANY project
        public IEnumerable<Employee> AvailableTeamMembers { get; set; }
    }
}