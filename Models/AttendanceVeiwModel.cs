using System;

namespace hrms.Models
{
    public class AttendanceWidgetViewModel
    {
        public bool CanClockIn { get; set; }
        public bool CanClockOut { get; set; }
        public bool IsCompleted { get; set; }
        public TimeSpan? ClockInTime { get; set; }
        public string TodaysDate => DateTime.Today.ToString("D"); // e.g., "Saturday, 21 August 2025"
    }
}