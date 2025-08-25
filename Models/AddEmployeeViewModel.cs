using System.ComponentModel.DataAnnotations;

namespace hrms.Models
{
    // This ViewModel is now simple, only for HIRING
    public class AddEmployeeViewModel
    {
        [Required] public string FirstName { get; set; }
        [Required] public string LastName { get; set; }
        [Required][EmailAddress] public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        [Required] public string Position { get; set; }
        [Required][DataType(DataType.Date)] public System.DateTime DateOfJoining { get; set; }
        [Required] public string Username { get; set; }
        [Required][DataType(DataType.Password)] public string Password { get; set; }
        [Required] public string Role { get; set; }

        // DepartmentId and ReportingHrId have been completely removed.
    }
}