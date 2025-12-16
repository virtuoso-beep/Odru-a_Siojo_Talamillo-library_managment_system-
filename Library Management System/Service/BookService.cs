using System;
using System.Collections.Generic;
using System.Data;
using Library_Management_System.Helper;
using Library_Management_System.Interfaces;
using MySql.Data.MySqlClient;

namespace Library_Management_System.Service
{
    public class BookService : IBookService
    {
        public class BookInfo
        {
            public string BookId { get; set; }
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

        public List<BookInfo> GetBooks(string searchText = "", string categoryFilter = "All Categories")
        {
            var books = new List<BookInfo>();

            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    string query = @"
                        SELECT BookId, ISBN, Title, Author, Publisher, PublicationYear,
                               Category, TotalCopies, AvailableCopies, Description, CreatedDate
                        FROM Books WHERE 1=1";

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        query += " AND (Title LIKE @search OR Author LIKE @search OR ISBN LIKE @search)";
                    }

                    if (categoryFilter != "All Categories")
                    {
                        query += " AND Category = @category";
                    }

                    query += " ORDER BY Title";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        if (!string.IsNullOrEmpty(searchText))
                        {
                            command.Parameters.AddWithValue("@search", $"%{searchText}%");
                        }
                        if (categoryFilter != "All Categories")
                        {
                            command.Parameters.AddWithValue("@category", categoryFilter);
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                books.Add(new BookInfo
                                {
                                    BookId = reader["BookId"].ToString(),
                                    ISBN = reader["ISBN"]?.ToString(),
                                    Title = reader["Title"].ToString(),
                                    Author = reader["Author"].ToString(),
                                    Publisher = reader["Publisher"]?.ToString(),
                                    PublicationYear = reader["PublicationYear"] != DBNull.Value ? (int?)reader["PublicationYear"] : null,
                                    Category = reader["Category"]?.ToString(),
                                    TotalCopies = Convert.ToInt32(reader["TotalCopies"]),
                                    AvailableCopies = Convert.ToInt32(reader["AvailableCopies"]),
                                    Description = reader["Description"]?.ToString(),
                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting books: {ex.Message}");
            }

            return books;
        }

        public List<string> GetCategories()
        {
            var categories = new List<string>();

            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    string query = "SELECT DISTINCT Category FROM Books WHERE Category IS NOT NULL ORDER BY Category";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                categories.Add(reader["Category"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting categories: {ex.Message}");
            }

            return categories;
        }

        public bool AddBook(string isbn, string title, string author, string publisher,
                           int? publicationYear, string category, int totalCopies, string description)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    string query = @"
                        INSERT INTO Books (ISBN, Title, Author, Publisher, PublicationYear, Category, TotalCopies, AvailableCopies, Description)
                        VALUES (@isbn, @title, @author, @publisher, @publicationYear, @category, @totalCopies, @totalCopies, @description)";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@isbn", string.IsNullOrWhiteSpace(isbn) ? (object)DBNull.Value : isbn.Trim());
                        command.Parameters.AddWithValue("@title", title.Trim());
                        command.Parameters.AddWithValue("@author", author.Trim());
                        command.Parameters.AddWithValue("@publisher", string.IsNullOrWhiteSpace(publisher) ? (object)DBNull.Value : publisher.Trim());
                        command.Parameters.AddWithValue("@publicationYear", publicationYear ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@category", string.IsNullOrWhiteSpace(category) ? (object)DBNull.Value : category.Trim());
                        command.Parameters.AddWithValue("@totalCopies", totalCopies);
                        command.Parameters.AddWithValue("@description", string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description.Trim());

                        command.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding book: {ex.Message}");
                throw;
            }
        }

        public bool UpdateBook(string bookId, string isbn, string title, string author, string publisher,
                              int? publicationYear, string category, int totalCopies, string description)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    // First get current available copies
                    string getCurrentQuery = "SELECT AvailableCopies FROM Books WHERE BookId = @bookId";
                    int currentAvailable = 0;

                    using (var getCommand = new MySqlCommand(getCurrentQuery, connection))
                    {
                        getCommand.Parameters.AddWithValue("@bookId", bookId);
                        var result = getCommand.ExecuteScalar();
                        if (result != null)
                        {
                            currentAvailable = Convert.ToInt32(result);
                        }
                    }

                    // Calculate new available copies (don't let it go below 0 or above total)
                    int newAvailable = System.Math.Min(System.Math.Max(0, currentAvailable), totalCopies);

                    string query = @"
                        UPDATE Books
                        SET ISBN = @isbn, Title = @title, Author = @author, Publisher = @publisher,
                            PublicationYear = @publicationYear, Category = @category,
                            TotalCopies = @totalCopies, AvailableCopies = @availableCopies,
                            Description = @description
                        WHERE BookId = @bookId";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@bookId", bookId);
                        command.Parameters.AddWithValue("@isbn", string.IsNullOrWhiteSpace(isbn) ? (object)DBNull.Value : isbn.Trim());
                        command.Parameters.AddWithValue("@title", title.Trim());
                        command.Parameters.AddWithValue("@author", author.Trim());
                        command.Parameters.AddWithValue("@publisher", string.IsNullOrWhiteSpace(publisher) ? (object)DBNull.Value : publisher.Trim());
                        command.Parameters.AddWithValue("@publicationYear", publicationYear ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@category", string.IsNullOrWhiteSpace(category) ? (object)DBNull.Value : category.Trim());
                        command.Parameters.AddWithValue("@totalCopies", totalCopies);
                        command.Parameters.AddWithValue("@availableCopies", newAvailable);
                        command.Parameters.AddWithValue("@description", string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description.Trim());

                        command.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating book: {ex.Message}");
                throw;
            }
        }

        public bool DeleteBook(string bookId)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    // Check if book has active borrowings
                    string checkBorrowingsQuery = "SELECT COUNT(*) FROM Borrowings WHERE BookId = @bookId AND ReturnDate IS NULL";
                    using (var checkCommand = new MySqlCommand(checkBorrowingsQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@bookId", bookId);
                        int activeBorrowings = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (activeBorrowings > 0)
                        {
                            throw new Exception("Cannot delete book with active borrowings.");
                        }
                    }

                    string query = "DELETE FROM Books WHERE BookId = @bookId";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@bookId", bookId);
                        command.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting book: {ex.Message}");
                throw;
            }
        }

        public BookInfo GetBookById(string bookId)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    string query = @"
                        SELECT BookId, ISBN, Title, Author, Publisher, PublicationYear,
                               Category, TotalCopies, AvailableCopies, Description, CreatedDate
                        FROM Books WHERE BookId = @bookId";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@bookId", bookId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new BookInfo
                                {
                                    BookId = reader["BookId"].ToString(),
                                    ISBN = reader["ISBN"]?.ToString(),
                                    Title = reader["Title"].ToString(),
                                    Author = reader["Author"].ToString(),
                                    Publisher = reader["Publisher"]?.ToString(),
                                    PublicationYear = reader["PublicationYear"] != DBNull.Value ? (int?)reader["PublicationYear"] : null,
                                    Category = reader["Category"]?.ToString(),
                                    TotalCopies = Convert.ToInt32(reader["TotalCopies"]),
                                    AvailableCopies = Convert.ToInt32(reader["AvailableCopies"]),
                                    Description = reader["Description"]?.ToString(),
                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting book: {ex.Message}");
            }

            return null;
        }
    }
}
