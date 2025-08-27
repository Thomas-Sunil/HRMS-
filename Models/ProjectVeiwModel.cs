using hrms.Models;
using System.Collections.Generic;

namespace hrms.ViewModels
{
    // A ViewModel for when an employee is viewing their project details
    public class EmployeeProjectViewModel
    {
        public Project Project { get; set; }
        // Only the tasks assigned to the CURRENT employee
        public IEnumerable<ProjectTask> MyTasks { get; set; }
    }
}