using System.ComponentModel.DataAnnotations.Schema;

namespace hrms.Models
{
    [Table("Users")] // This tells EF to map this class to the case-sensitive "Users" table
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
    }
}