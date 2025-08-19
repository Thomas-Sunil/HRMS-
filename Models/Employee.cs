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

        [Column("department")]
        public string Department { get; set; }

        [Column("position")]
        public string Position { get; set; }

        [Column("date_of_joining")]
        public DateTime DateOfJoining { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        // This is a "navigation property" and does not map to a database column,
        // so it does NOT get a [Column] attribute.
        public User User { get; set; }
    }
}