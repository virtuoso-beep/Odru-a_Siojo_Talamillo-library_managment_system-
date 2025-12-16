using System.Collections.Generic;
using Library_Management_System.Service;

namespace Library_Management_System.Interfaces
{
    public interface IBookService
    {
        List<BookService.BookInfo> GetBooks(string searchText = "", string categoryFilter = "All Categories");
        List<string> GetCategories();
        bool AddBook(string title, string author, string isbn, string publisher, int? publicationYear, string category, int totalCopies, string description = "");
        bool UpdateBook(string bookId, string title, string author, string isbn, string publisher, int? publicationYear, string category, int totalCopies, string description = "");
        bool DeleteBook(string bookId);
        BookService.BookInfo GetBookById(string bookId);
    }
}
