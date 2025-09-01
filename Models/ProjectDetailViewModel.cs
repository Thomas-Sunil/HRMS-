using hrms.Models;
using System.Collections.Generic;

// Note the namespace is hrms.Models
namespace hrms.Models
{
    public class ProjectDetailViewModel
    {
        public Project Project { get; set; }
        public IEnumerable<Employee> AssignedTeamMembers { get; set; }
        public IEnumerable<Employee> AvailableTeamMembers { get; set; }
    }
}