using System.Collections.Generic;
using LMS_Library_Management_System.Models;

namespace LMS_Library_Management_System.Interfaces
{
    /// <summary>
    /// Interface for book management operations
    /// </summary>
    public interface IBookService
    {
        int AddBook(string isbn, string title, string author, string publisher, int? publicationYear, 
            string category, int totalCopies, string description);
        
        List<Book> GetAllBooks();
        
        Book GetBookById(int bookId);
        
        bool UpdateBook(int bookId, string isbn, string title, string author, string publisher, 
            int? publicationYear, string category, int totalCopies, int availableCopies, string description);
        
        bool DeleteBook(int bookId);
        
        bool AddCopies(int bookId, int copiesToAdd);
        
        List<string> GetAllCategories();
        
        List<Book> SearchBooks(string searchTerm, string category = null);
        
        Dictionary<string, int> GetBookStatistics();
    }
}

