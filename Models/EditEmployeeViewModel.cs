using System.ComponentModel.DataAnnotations;

namespace hrms.Models
{
    public class EditEmployeeViewModel
    {
        public int Id { get; set; }

        // Personal
        [Required] public string FirstName { get; set; }
        [Required] public string LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AddressLine1 { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }

        // Professional
        [Required][EmailAddress] public string Email { get; set; }
        [Required] public string Position { get; set; }
        public string? HighestQualification { get; set; }

        // Assignment
        [Display(Name = "Department")]
        public int? DepartmentId { get; set; }
        [Display(Name = "Reporting HR")]
        public int? ReportingHrId { get; set; }
    }
}