using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hrms.Models
{
    [Table("leave_requests")]
    public class LeaveRequest
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("employee_id")]
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }

        [Display(Name = "Leave Type")]
        [Column("leave_type")]
        [Required]
        public string LeaveType { get; set; }

        [Display(Name = "Start Date")]
        [Column("start_date")]
        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "End Date")]
        [Column("end_date")]
        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Column("reason")]
        public string? Reason { get; set; }

        [Column("request_date")]
        public DateTime RequestDate { get; set; }

        [Column("status")]
        public string Status { get; set; }

        // --- THESE PROPERTIES WERE MISSING ---
        [Column("manager_approved_by_id")]
        public int? ManagerApprovedById { get; set; }

        [Column("hr_approved_by_id")]
        public int? HrApprovedById { get; set; }
        // --- END FIX ---
        // ---- Add this new property inside your LeaveRequest model ----
        [Column("duration_type")]
        public string DurationType { get; set; } // "Full Day", "Half Day - First Half", "Half Day - Second Half"
    }
}