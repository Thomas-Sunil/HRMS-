using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace hrms.Models
{
    [Table("attendances")]
    public class Attendance
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("employee_id")]
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }

        [Column("date")]
        public DateTime Date { get; set; }

        [Column("clock_in_time")]
        public TimeSpan? ClockInTime { get; set; }

        [Column("clock_out_time")]
        public TimeSpan? ClockOutTime { get; set; }

        [Column("status")]
        public string Status { get; set; }
    }
}