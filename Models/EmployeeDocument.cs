using System;
using System.ComponentModel.DataAnnotations.Schema;
namespace hrms.Models
{
    [Table("employee_documents")]
    public class EmployeeDocument
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("employee_id")]
        public int EmployeeId { get; set; }

        [Column("document_name")]
        public string DocumentName { get; set; }

        [Column("file_path")]
        public string FilePath { get; set; }

        [Column("uploaded_at")]
        public DateTime UploadedAt { get; set; }

        public Employee Employee { get; set; }
    }
}