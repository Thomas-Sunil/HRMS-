using System.ComponentModel.DataAnnotations.Schema;

namespace hrms.Models
{
    [Table("meeting_invitations")]
    public class MeetingInvitation
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("meeting_id")]
        public int MeetingId { get; set; }
        public Meeting Meeting { get; set; }

        [Column("employee_id")]
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }

        [Column("status")]
        public string Status { get; set; } // "Pending", "Accepted", "Declined"
    }
}