using System.Collections.Generic; // Required for ICollection
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
        public string PhoneNumber { get; set; }

        [Column("department_id")]
        public int? DepartmentId { get; set; }
        public Department Department { get; set; }

        [Column("position")]
        public string Position { get; set; }

        [Column("date_of_joining")]
        public System.DateTime DateOfJoining { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }
        public User User { get; set; }

        // This represents the many-to-many relationship for projects an employee is assigned to.
        public ICollection<Project> Projects { get; set; } = new List<Project>();

        // --- THIS IS THE MISSING PIECE ---
        // This represents the one-to-many relationship for projects this employee MANAGES.
        [InverseProperty("Manager")]
        public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
        // --- END FIX ---

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}