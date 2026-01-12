using System;

namespace LMS_Library_Management_System.Models
{
    /// <summary>
    /// Data class for borrowing information
    /// </summary>
    public class Borrowing
    {
        public int BorrowingId { get; set; }
        public int MemberId { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; }
        public string Author { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } // Borrowed, Returned, Overdue
        public decimal FineAmount { get; set; }
    }
}

