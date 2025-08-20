using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hrms.Models
{
    [Table("departments")]
    public class Department
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [Required]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Display(Name = "Head of Department")]
        [Column("head_of_department_id")]
        public int? HeadOfDepartmentId { get; set; }

        [ForeignKey("HeadOfDepartmentId")]
        public Employee HeadOfDepartment { get; set; }

        // --- THIS IS THE FIX ---
        // The InverseProperty attribute tells EF that the "Department" property
        // on the Employee model is the other side of this relationship.
        [InverseProperty("Department")]
        public ICollection<Employee> Employees { get; set; }
        // --- END FIX ---
    }
}