using System.Collections.Generic;
namespace hrms.Models
{
    public class ManagerDashboardViewModel
    {
        public Employee Manager { get; set; }
        public IEnumerable<Employee> TeamMembers { get; set; }
        public IEnumerable<Employee> UnassignedEmployees { get; set; }
    }
}