using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace hrms.Models
{
    [Table("meetings")]
    public class Meeting
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("start_time")]
        public DateTime StartTime { get; set; }

        [Column("end_time")]
        public DateTime EndTime { get; set; }

        // --- THIS IS THE FIX ---
        // We are explicitly telling EF which property holds the key
        [Column("created_by_id")]
        public int? CreatedById { get; set; }

        // This attribute links the 'Creator' object to the 'CreatedById' key
        [ForeignKey("CreatedById")]
        public Employee Creator { get; set; }
        // --- END FIX ---

        public ICollection<MeetingInvitation> Invitations { get; set; } = new List<MeetingInvitation>();
    }
}