using System.ComponentModel.DataAnnotations;

namespace hrms.Models
{
    public class AddEmployeeViewModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; }

        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Position is required.")]
        public string Position { get; set; }

        [Required(ErrorMessage = "Date of joining is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Joining")]
        public DateTime DateOfJoining { get; set; }

        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "A role must be selected.")]
        public string Role { get; set; }
    }
}