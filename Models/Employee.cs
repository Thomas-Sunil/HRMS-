using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace hrms.Models
{
    [Table("employees")]
    public class Employee
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("first_name")]
        public string FirstName { get; set; }

        [Column("last_name")]
        public string LastName { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("phone_number")]
        public string? PhoneNumber { get; set; }

        [Column("department_id")]
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        [Column("position")]
        public string Position { get; set; }

        [Column("date_of_joining")]
        public DateTime DateOfJoining { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }
        public User User { get; set; }

        [Column("reporting_hr_id")]
        public int? ReportingHrId { get; set; }
        [ForeignKey("ReportingHrId")]
        public Employee? ReportingHr { get; set; }
        [Column("photo_path")]
        public string? PhotoPath { get; set; }

        [Column("address_line_1")]
        public string? AddressLine1 { get; set; }

        [Column("address_line_2")]
        public string? AddressLine2 { get; set; }

        [Column("city")]
        public string? City { get; set; }

        [Column("state")]
        public string? State { get; set; }

        [Column("postal_code")]
        public string? PostalCode { get; set; }

        [Column("country")]
        public string? Country { get; set; }

        [Column("personal_email")]
        public string? PersonalEmail { get; set; }

        // --- NEW PROFESSIONAL DETAIL FIELDS ---
        [Column("highest_qualification")]
        public string? HighestQualification { get; set; }

        [Column("previous_company")]
        public string? PreviousCompany { get; set; }

        [Column("previous_experience_years")]
        public int? PreviousExperienceYears { get; set; }

        // --- Navigation Property for Documents ---
        public ICollection<EmployeeDocument> Documents { get; set; } = new List<EmployeeDocument>();

        public ICollection<Project> Projects { get; set; } = new List<Project>();
        [InverseProperty("Manager")]
        public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();

        [InverseProperty("Creator")]
        public ICollection<Meeting> CreatedMeetings { get; set; } = new List<Meeting>();
        // --- END FIX ---

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}