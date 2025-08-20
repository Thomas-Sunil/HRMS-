using System.ComponentModel.DataAnnotations;

namespace hrms.Models
{
    public class EditEmployeeViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Department")]
        public int? DepartmentId { get; set; }

        [Required]
        public string Position { get; set; }
    }
}