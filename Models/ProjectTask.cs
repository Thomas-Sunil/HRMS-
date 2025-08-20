using System.ComponentModel.DataAnnotations.Schema;

namespace hrms.Models
{
    [Table("project_tasks")]
    public class ProjectTask
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("is_completed")]
        public bool IsCompleted { get; set; }

        [Column("project_id")]
        public int ProjectId { get; set; }
        public Project Project { get; set; }
    }
}