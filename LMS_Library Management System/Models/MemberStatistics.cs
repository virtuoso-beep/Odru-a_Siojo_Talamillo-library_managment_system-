using System;

namespace LMS_Library_Management_System.Models
{
    /// <summary>
    /// Data class for member statistics
    /// </summary>
    public class MemberStatistics
    {
        public int CurrentBooksCount { get; set; }
        public int MaxBooks { get; set; }
        public int TotalBorrowed { get; set; }
        public decimal UnpaidFines { get; set; }
        public DateTime? MembershipExpiry { get; set; }
    }
}

