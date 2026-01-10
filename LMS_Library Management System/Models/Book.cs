using System;

namespace LMS_Library_Management_System.Models
{
    /// <summary>
    /// Represents book data structure
    /// </summary>
    public class Book
    {
        public int BookId { get; set; }
        public string ISBN { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Publisher { get; set; }
        public int? PublicationYear { get; set; }
        public string Category { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

