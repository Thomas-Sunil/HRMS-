using System.ComponentModel.DataAnnotations;

namespace hrms.Models
{
    // This ViewModel now ONLY contains data that comes FROM the form.
    public class DepartmentViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Department Name is required.")]
        [Display(Name = "Department Name")]
        public string Name { get; set; }

        public string Description { get; set; }

        [Display(Name = "Head of Department")]
        public int? HeadOfDepartmentId { get; set; }
    }
}