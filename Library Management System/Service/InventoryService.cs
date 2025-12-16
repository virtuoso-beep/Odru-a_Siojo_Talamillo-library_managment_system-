using System;
using System.Collections.Generic;
using System.Data;
using Library_Management_System.Helper;
using MySql.Data.MySqlClient;

namespace Library_Management_System.Service
{
    public class InventoryService
    {
        public class InventoryInfo
        {
            public string BookId { get; set; }
            public string ISBN { get; set; }
            public string Title { get; set; }
            public string Author { get; set; }
            public string Category { get; set; }
            public int TotalCopies { get; set; }
            public int AvailableCopies { get; set; }
            public int BorrowedCopies { get { return TotalCopies - AvailableCopies; } }
            public string Location { get; set; }
            public string Condition { get; set; }
            public DateTime LastInventoryCheck { get; set; }
        }

        public List<InventoryInfo> GetInventory(string searchText = "", string categoryFilter = "All Categories", string statusFilter = "All Status")
        {
            var inventory = new List<InventoryInfo>();

            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    string query = @"
                        SELECT BookId, ISBN, Title, Author, Category, TotalCopies, AvailableCopies,
                               CreatedDate as LastInventoryCheck
                        FROM Books WHERE 1=1";

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        query += " AND (Title LIKE @search OR Author LIKE @search OR ISBN LIKE @search)";
                    }

                    if (categoryFilter != "All Categories")
                    {
                        query += " AND Category = @category";
                    }

                    if (statusFilter != "All Status")
                    {
                        switch (statusFilter)
                        {
                            case "Available":
                                query += " AND AvailableCopies > 0";
                                break;
                            case "Out of Stock":
                                query += " AND AvailableCopies = 0";
                                break;
                            case "Low Stock":
                                query += " AND AvailableCopies > 0 AND AvailableCopies <= 2";
                                break;
                        }
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
                                inventory.Add(new InventoryInfo
                                {
                                    BookId = reader["BookId"].ToString(),
                                    ISBN = reader["ISBN"]?.ToString(),
                                    Title = reader["Title"].ToString(),
                                    Author = reader["Author"].ToString(),
                                    Category = reader["Category"]?.ToString(),
                                    TotalCopies = Convert.ToInt32(reader["TotalCopies"]),
                                    AvailableCopies = Convert.ToInt32(reader["AvailableCopies"]),
                                    Location = "Main Library", // Default location
                                    Condition = "Good", // Default condition
                                    LastInventoryCheck = Convert.ToDateTime(reader["LastInventoryCheck"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting inventory: {ex.Message}");
            }

            return inventory;
        }

        public Dictionary<string, int> GetInventorySummary()
        {
            var summary = new Dictionary<string, int>();

            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    // Total books
                    string totalQuery = "SELECT SUM(TotalCopies) FROM Books";
                    using (var cmd = new MySqlCommand(totalQuery, connection))
                    {
                        summary["TotalCopies"] = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                    }

                    // Available books
                    string availableQuery = "SELECT SUM(AvailableCopies) FROM Books";
                    using (var cmd = new MySqlCommand(availableQuery, connection))
                    {
                        summary["AvailableCopies"] = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                    }

                    // Borrowed books
                    summary["BorrowedCopies"] = summary["TotalCopies"] - summary["AvailableCopies"];

                    // Out of stock books
                    string outOfStockQuery = "SELECT COUNT(*) FROM Books WHERE AvailableCopies = 0";
                    using (var cmd = new MySqlCommand(outOfStockQuery, connection))
                    {
                        summary["OutOfStock"] = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                    }

                    // Low stock books (2 or fewer available)
                    string lowStockQuery = "SELECT COUNT(*) FROM Books WHERE AvailableCopies > 0 AND AvailableCopies <= 2";
                    using (var cmd = new MySqlCommand(lowStockQuery, connection))
                    {
                        summary["LowStock"] = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting inventory summary: {ex.Message}");
            }

            return summary;
        }

        public bool UpdateInventory(string bookId, int newTotalCopies)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    // Get current available copies
                    string getCurrentQuery = "SELECT AvailableCopies FROM Books WHERE BookId = @bookId";
                    int currentAvailable = 0;

                    using (var getCmd = new MySqlCommand(getCurrentQuery, connection))
                    {
                        getCmd.Parameters.AddWithValue("@bookId", bookId);
                        var result = getCmd.ExecuteScalar();
                        if (result != null)
                        {
                            currentAvailable = Convert.ToInt32(result);
                        }
                    }

                    // Calculate new available copies (can't exceed total, can't go below 0)
                    int newAvailable = System.Math.Min(System.Math.Max(0, currentAvailable), newTotalCopies);

                    string updateQuery = @"
                        UPDATE Books
                        SET TotalCopies = @totalCopies, AvailableCopies = @availableCopies
                        WHERE BookId = @bookId";

                    using (var command = new MySqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@bookId", bookId);
                        command.Parameters.AddWithValue("@totalCopies", newTotalCopies);
                        command.Parameters.AddWithValue("@availableCopies", newAvailable);
                        command.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating inventory: {ex.Message}");
                throw;
            }
        }

        public List<string> GetLowStockAlerts()
        {
            var alerts = new List<string>();

            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    string query = @"
                        SELECT Title, AvailableCopies
                        FROM Books
                        WHERE AvailableCopies > 0 AND AvailableCopies <= 2
                        ORDER BY AvailableCopies";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string title = reader["Title"].ToString();
                                int available = Convert.ToInt32(reader["AvailableCopies"]);
                                alerts.Add($"\"{title}\" - Only {available} copies remaining");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting low stock alerts: {ex.Message}");
            }

            return alerts;
        }

        public List<string> GetOutOfStockItems()
        {
            var outOfStock = new List<string>();

            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    string query = @"
                        SELECT Title
                        FROM Books
                        WHERE AvailableCopies = 0
                        ORDER BY Title";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                outOfStock.Add(reader["Title"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting out of stock items: {ex.Message}");
            }

            return outOfStock;
        }

        public bool PerformInventoryAudit()
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    // Update last inventory check date for all books
                    string updateQuery = "UPDATE Books SET CreatedDate = NOW()";

                    using (var command = new MySqlCommand(updateQuery, connection))
                    {
                        command.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error performing inventory audit: {ex.Message}");
                throw;
            }
        }
    }
}