using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace hrms.Models
{
    public class AddEmployeeViewModel
    {
        // Personal Details
        [Required] public string FirstName { get; set; }
        [Required] public string LastName { get; set; }
        public string? AddressLine1 { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public IFormFile? Photo { get; set; }
        public string? PhoneNumber { get; set; } // This property was missing

        // Professional Details
        [Required][EmailAddress] public string Email { get; set; }
        [Required] public string Position { get; set; }
        [Required][DataType(DataType.Date)] public System.DateTime DateOfJoining { get; set; }
        public string? HighestQualification { get; set; }
        public List<IFormFile> Certificates { get; set; } = new List<IFormFile>();

        // Login Details
        [Required] public string Username { get; set; }
        [Required][DataType(DataType.Password)] public string Password { get; set; }
        [Required] public string Role { get; set; }
    }
}