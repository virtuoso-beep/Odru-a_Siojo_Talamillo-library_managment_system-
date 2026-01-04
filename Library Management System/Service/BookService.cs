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
            public string Location { get; set; }
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
                    using (var command = StoredProcedureHelper.CreateCommand("SP_GetAllBooks", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText);
                        StoredProcedureHelper.AddParameter(command, "p_CategoryFilter", categoryFilter == "All Categories" ? null : categoryFilter);
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
                                    Location = reader["Location"]?.ToString(),
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
                    using (var command = StoredProcedureHelper.CreateCommand("SP_GetBookCategories", connection))
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
        private void EnsureBooksTableExists(MySqlConnection connection)
        {
        }
        public bool AddBook(string isbn, string title, string author, string publisher,
                           int? publicationYear, string category, int totalCopies, string description)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    using (var command = StoredProcedureHelper.CreateCommand("SP_AddBook", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_Title", title?.Trim());
                        StoredProcedureHelper.AddParameter(command, "p_Author", author?.Trim());
                        StoredProcedureHelper.AddParameter(command, "p_ISBN", string.IsNullOrWhiteSpace(isbn) ? null : isbn.Trim());
                        StoredProcedureHelper.AddParameter(command, "p_Publisher", string.IsNullOrWhiteSpace(publisher) ? null : publisher.Trim());
                        StoredProcedureHelper.AddParameter(command, "p_PublicationYear", publicationYear);
                        StoredProcedureHelper.AddParameter(command, "p_Category", string.IsNullOrWhiteSpace(category) ? null : category.Trim());
                        StoredProcedureHelper.AddParameter(command, "p_TotalCopies", totalCopies);
                        StoredProcedureHelper.AddParameter(command, "p_Location", "");
                        StoredProcedureHelper.AddParameter(command, "p_Description", string.IsNullOrWhiteSpace(description) ? null : description.Trim());
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int bookId = Convert.ToInt32(reader["BookId"]);
                                System.Diagnostics.Debug.WriteLine($"Book added with ID: {bookId}");
                            }
                        }
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
                    using (var command = StoredProcedureHelper.CreateCommand("SP_UpdateBook", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_BookId", int.Parse(bookId));
                        StoredProcedureHelper.AddParameter(command, "p_Title", title?.Trim());
                        StoredProcedureHelper.AddParameter(command, "p_Author", author?.Trim());
                        StoredProcedureHelper.AddParameter(command, "p_ISBN", string.IsNullOrWhiteSpace(isbn) ? null : isbn.Trim());
                        StoredProcedureHelper.AddParameter(command, "p_Publisher", string.IsNullOrWhiteSpace(publisher) ? null : publisher.Trim());
                        StoredProcedureHelper.AddParameter(command, "p_PublicationYear", publicationYear);
                        StoredProcedureHelper.AddParameter(command, "p_Category", string.IsNullOrWhiteSpace(category) ? null : category.Trim());
                        StoredProcedureHelper.AddParameter(command, "p_TotalCopies", totalCopies);
                        StoredProcedureHelper.AddParameter(command, "p_Location", "");
                        StoredProcedureHelper.AddParameter(command, "p_Description", string.IsNullOrWhiteSpace(description) ? null : description.Trim());
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int rowsAffected = Convert.ToInt32(reader["RowsAffected"]);
                                return rowsAffected > 0;
                            }
                        }
                        return false;
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
                    using (var command = StoredProcedureHelper.CreateCommand("SP_DeleteBook", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_BookId", int.Parse(bookId));
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int rowsAffected = Convert.ToInt32(reader["RowsAffected"]);
                                return rowsAffected > 0;
                            }
                        }
                        return false;
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
                    using (var command = StoredProcedureHelper.CreateCommand("SP_GetBookById", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_BookId", int.Parse(bookId));
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
                                    Location = reader["Location"]?.ToString(),
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
