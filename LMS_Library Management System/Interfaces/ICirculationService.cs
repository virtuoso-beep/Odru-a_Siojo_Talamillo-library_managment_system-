using System;
using System.Collections.Generic;
using LMS_Library_Management_System.Models;

namespace LMS_Library_Management_System.Interfaces
{
    /// <summary>
    /// Interface for circulation operations (borrow, return, renew)
    /// </summary>
    public interface ICirculationService
    {
        /// <summary>
        /// Borrows a book to a member
        /// </summary>
        bool BorrowBook(int memberId, int bookId, DateTime dueDate, out string errorMessage);

        /// <summary>
        /// Returns a borrowed book
        /// </summary>
        bool ReturnBook(int borrowingId, out decimal fineAmount, out string errorMessage);

        /// <summary>
        /// Renews a borrowed book
        /// </summary>
        bool RenewBook(int borrowingId, DateTime newDueDate, out string errorMessage);

        /// <summary>
        /// Gets all active borrowings for a member
        /// </summary>
        List<BorrowingTransaction> GetActiveBorrowingsByMember(int memberId);

        /// <summary>
        /// Gets all active borrowings (for return dialog)
        /// </summary>
        List<BorrowingTransaction> GetActiveBorrowings();

        /// <summary>
        /// Gets a borrowing transaction by ID
        /// </summary>
        BorrowingTransaction GetBorrowingById(int borrowingId);

        /// <summary>
        /// Validates if a member can borrow books
        /// </summary>
        bool ValidateMember(int memberId, out string errorMessage);

        /// <summary>
        /// Calculates fine for overdue book
        /// </summary>
        decimal CalculateFine(int borrowingId);

        /// <summary>
        /// Gets borrowing statistics
        /// </summary>
        BorrowingStatistics GetBorrowingStatistics();

        /// <summary>
        /// Gets all borrowing transactions for display (including returned)
        /// </summary>
        List<BorrowingTransaction> GetAllBorrowingsForDisplay();
    }

    /// <summary>
    /// Extended borrowing transaction with member and book details
    /// </summary>
    public class BorrowingTransaction
    {
        public int BorrowingId { get; set; }
        public int MemberId { get; set; }
        public int BookId { get; set; }
        public string MemberName { get; set; }
        public string MemberNumber { get; set; }
        public string BookTitle { get; set; }
        public string Author { get; set; }
        public string ISBN { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } // Active, Returned, Overdue
        public decimal FineAmount { get; set; }
        public bool IsOverdue { get; set; }
    }

    /// <summary>
    /// Borrowing statistics
    /// </summary>
    public class BorrowingStatistics
    {
        public int CurrentlyBorrowed { get; set; }
        public int Overdue { get; set; }
        public int ReturnedToday { get; set; }
    }
}


