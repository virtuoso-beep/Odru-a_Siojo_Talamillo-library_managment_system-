using System;
using System.Collections.Generic;
using System.Data;
using Library_Management_System.Helper;
using MySql.Data.MySqlClient;
namespace Library_Management_System.Service
{
    public class SearchService
    {
        public class SearchResult
        {
            public string BookId { get; set; }
            public string ISBN { get; set; }
            public string Title { get; set; }
            public string Author { get; set; }
            public string Publisher { get; set; }
            public int? PublicationYear { get; set; }
            public string Category { get; set; }
            public string Description { get; set; }
            public int TotalCopies { get; set; }
            public int AvailableCopies { get; set; }
            public bool IsAvailable { get { return AvailableCopies > 0; } }
            public string AvailabilityStatus { get { return IsAvailable ? "Available" : "Unavailable"; } }
        }
        public class SearchFilters
        {
            public string Category { get; set; } = "All Categories";
            public bool AvailableOnly { get; set; } = false;
            public int? MinYear { get; set; }
            public int? MaxYear { get; set; }
            public string Publisher { get; set; }
        }
        public List<SearchResult> SearchBooks(string searchText, SearchFilters filters = null)
        {
            var results = new List<SearchResult>();
            if (filters == null)
            {
                filters = new SearchFilters();
            }
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            b.BookId,
                            b.ISBN,
                            b.Title,
                            b.Author,
                            b.Publisher,
                            b.PublicationYear,
                            COALESCE(c.CategoryName, 'Uncategorized') AS Category,
                            b.Description,
                            b.TotalCopies,
                            b.AvailableCopies
                        FROM Books b
                        LEFT JOIN Categories c ON b.CategoryId = c.CategoryId
                        WHERE 1=1";
                    var parameters = new List<MySqlParameter>();
                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        query += @" AND (
                            b.Title LIKE @searchText OR
                            b.Author LIKE @searchText OR
                            b.ISBN LIKE @searchText OR
                            b.Description LIKE @searchText OR
                            b.Publisher LIKE @searchText OR
                            c.CategoryName LIKE @searchText
                        )";
                        parameters.Add(new MySqlParameter("@searchText", $"%{searchText}%"));
                    }
                    if (filters.Category != null && filters.Category != "All Categories")
                    {
                        query += " AND c.CategoryName = @category";
                        parameters.Add(new MySqlParameter("@category", filters.Category));
                    }
                    if (filters.AvailableOnly)
                    {
                        query += " AND b.AvailableCopies > 0";
                    }
                    if (filters.MinYear.HasValue)
                    {
                        query += " AND b.PublicationYear >= @minYear";
                        parameters.Add(new MySqlParameter("@minYear", filters.MinYear.Value));
                    }
                    if (filters.MaxYear.HasValue)
                    {
                        query += " AND b.PublicationYear <= @maxYear";
                        parameters.Add(new MySqlParameter("@maxYear", filters.MaxYear.Value));
                    }
                    if (!string.IsNullOrWhiteSpace(filters.Publisher))
                    {
                        query += " AND b.Publisher LIKE @publisher";
                        parameters.Add(new MySqlParameter("@publisher", $"%{filters.Publisher}%"));
                    }
                    query += " ORDER BY b.Title ASC";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.Add(param);
                        }
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(new SearchResult
                                {
                                    BookId = reader["BookId"].ToString(),
                                    ISBN = reader["ISBN"]?.ToString(),
                                    Title = reader["Title"].ToString(),
                                    Author = reader["Author"].ToString(),
                                    Publisher = reader["Publisher"]?.ToString(),
                                    PublicationYear = reader["PublicationYear"] != DBNull.Value ? (int?)Convert.ToInt32(reader["PublicationYear"]) : null,
                                    Category = reader["Category"].ToString(),
                                    Description = reader["Description"]?.ToString(),
                                    TotalCopies = Convert.ToInt32(reader["TotalCopies"]),
                                    AvailableCopies = Convert.ToInt32(reader["AvailableCopies"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.ErrorHandler.LogError(ex, "SearchBooks");
                System.Diagnostics.Debug.WriteLine($"Error searching books: {ex.Message}");
            }
            return results;
        }
        public List<SearchResult> SearchByTitle(string title)
        {
            return SearchBooks(title, new SearchFilters());
        }
        public List<SearchResult> SearchByAuthor(string author)
        {
            return SearchBooks(author, new SearchFilters());
        }
        public List<SearchResult> SearchByISBN(string isbn)
        {
            return SearchBooks(isbn, new SearchFilters());
        }
        public List<SearchResult> SearchByCategory(string category)
        {
            return SearchBooks("", new SearchFilters { Category = category });
        }
        public List<string> GetAvailableCategories()
        {
            var categories = new List<string>();
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    string query = @"
                        SELECT DISTINCT c.CategoryName
                        FROM Categories c
                        INNER JOIN Books b ON c.CategoryId = b.CategoryId
                        ORDER BY c.CategoryName";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                categories.Add(reader["CategoryName"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.ErrorHandler.LogError(ex, "GetAvailableCategories");
                System.Diagnostics.Debug.WriteLine($"Error getting categories: {ex.Message}");
            }
            return categories;
        }
        public List<string> GetAvailablePublishers()
        {
            var publishers = new List<string>();
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    string query = @"
                        SELECT DISTINCT Publisher
                        FROM Books
                        WHERE Publisher IS NOT NULL AND Publisher != ''
                        ORDER BY Publisher";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                publishers.Add(reader["Publisher"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.ErrorHandler.LogError(ex, "GetAvailablePublishers");
                System.Diagnostics.Debug.WriteLine($"Error getting publishers: {ex.Message}");
            }
            return publishers;
        }
        public SearchResult GetBookDetails(string bookId)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            b.BookId,
                            b.ISBN,
                            b.Title,
                            b.Author,
                            b.Publisher,
                            b.PublicationYear,
                            COALESCE(c.CategoryName, 'Uncategorized') AS Category,
                            b.Description,
                            b.TotalCopies,
                            b.AvailableCopies
                        FROM Books b
                        LEFT JOIN Categories c ON b.CategoryId = c.CategoryId
                        WHERE b.BookId = @bookId";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@bookId", bookId);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new SearchResult
                                {
                                    BookId = reader["BookId"].ToString(),
                                    ISBN = reader["ISBN"]?.ToString(),
                                    Title = reader["Title"].ToString(),
                                    Author = reader["Author"].ToString(),
                                    Publisher = reader["Publisher"]?.ToString(),
                                    PublicationYear = reader["PublicationYear"] != DBNull.Value ? (int?)Convert.ToInt32(reader["PublicationYear"]) : null,
                                    Category = reader["Category"].ToString(),
                                    Description = reader["Description"]?.ToString(),
                                    TotalCopies = Convert.ToInt32(reader["TotalCopies"]),
                                    AvailableCopies = Convert.ToInt32(reader["AvailableCopies"])
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.ErrorHandler.LogError(ex, "GetBookDetails");
                System.Diagnostics.Debug.WriteLine($"Error getting book details: {ex.Message}");
            }
            return null;
        }
        public int GetSearchResultCount(string searchText, SearchFilters filters = null)
        {
            if (filters == null)
            {
                filters = new SearchFilters();
            }
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    string query = @"
                        SELECT COUNT(*) as ResultCount
                        FROM Books b
                        LEFT JOIN Categories c ON b.CategoryId = c.CategoryId
                        WHERE 1=1";
                    var parameters = new List<MySqlParameter>();
                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        query += @" AND (
                            b.Title LIKE @searchText OR
                            b.Author LIKE @searchText OR
                            b.ISBN LIKE @searchText OR
                            b.Description LIKE @searchText OR
                            b.Publisher LIKE @searchText OR
                            c.CategoryName LIKE @searchText
                        )";
                        parameters.Add(new MySqlParameter("@searchText", $"%{searchText}%"));
                    }
                    if (filters.Category != null && filters.Category != "All Categories")
                    {
                        query += " AND c.CategoryName = @category";
                        parameters.Add(new MySqlParameter("@category", filters.Category));
                    }
                    if (filters.AvailableOnly)
                    {
                        query += " AND b.AvailableCopies > 0";
                    }
                    if (filters.MinYear.HasValue)
                    {
                        query += " AND b.PublicationYear >= @minYear";
                        parameters.Add(new MySqlParameter("@minYear", filters.MinYear.Value));
                    }
                    if (filters.MaxYear.HasValue)
                    {
                        query += " AND b.PublicationYear <= @maxYear";
                        parameters.Add(new MySqlParameter("@maxYear", filters.MaxYear.Value));
                    }
                    if (!string.IsNullOrWhiteSpace(filters.Publisher))
                    {
                        query += " AND b.Publisher LIKE @publisher";
                        parameters.Add(new MySqlParameter("@publisher", $"%{filters.Publisher}%"));
                    }
                    using (var command = new MySqlCommand(query, connection))
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.Add(param);
                        }
                        var result = command.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.ErrorHandler.LogError(ex, "GetSearchResultCount");
                System.Diagnostics.Debug.WriteLine($"Error getting search result count: {ex.Message}");
            }
            return 0;
        }
    }
}
