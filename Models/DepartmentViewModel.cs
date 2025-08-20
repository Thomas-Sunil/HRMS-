using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace hrms.Models
{
    public class DepartmentViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Department Name is required.")]
        [Display(Name = "Department Name")]
        public string Name { get; set; }

        public string Description { get; set; }

        [Display(Name = "Head of Department")]
        public int? HeadOfDepartmentId { get; set; } // Only need the ID

        // This property is not for the form, but for populating the dropdown
        public IEnumerable<SelectListItem> ManagerList { get; set; }
    }
}