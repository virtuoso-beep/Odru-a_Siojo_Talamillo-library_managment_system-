using System;
using System.Collections.Generic;
using System.Data;
using Library_Management_System.Helper;
using Library_Management_System.Interfaces;
using MySql.Data.MySqlClient;
namespace Library_Management_System.Service
{
    public class CirculationService : ICirculationService
    {
        public class BorrowingInfo
        {
            public string BorrowingId { get; set; }
            public string MemberId { get; set; }
            public string MemberNumber { get; set; }
            public string MemberName { get; set; }
            public string BookId { get; set; }
            public string BookTitle { get; set; }
            public string BookAuthor { get; set; }
            public string BookISBN { get; set; }
            public DateTime BorrowDate { get; set; }
            public DateTime DueDate { get; set; }
            public DateTime? ReturnDate { get; set; }
            public string Status { get; set; }
            public int DaysOverdue { get; set; }
            public decimal FineAmount { get; set; }
        }
        public List<BorrowingInfo> GetBorrowings(string searchText = "", string statusFilter = "All")
        {
            var borrowings = new List<BorrowingInfo>();
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    using (var command = StoredProcedureHelper.CreateCommand("SP_GetAllBorrowings", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText);
                        StoredProcedureHelper.AddParameter(command, "p_StatusFilter", statusFilter == "All" ? null : statusFilter);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                borrowings.Add(new BorrowingInfo
                                {
                                    BorrowingId = reader["BorrowingId"].ToString(),
                                    MemberId = reader["MemberId"].ToString(),
                                    MemberNumber = reader["MemberNumber"].ToString(),
                                    MemberName = reader["MemberName"].ToString(),
                                    BookId = reader["BookId"].ToString(),
                                    BookTitle = reader["BookTitle"].ToString(),
                                    BookAuthor = reader["BookAuthor"].ToString(),
                                    BorrowDate = Convert.ToDateTime(reader["BorrowDate"]),
                                    DueDate = Convert.ToDateTime(reader["DueDate"]),
                                    ReturnDate = reader["ReturnDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["ReturnDate"]) : null,
                                    Status = reader["Status"].ToString(),
                                    DaysOverdue = Convert.ToInt32(reader["DaysOverdue"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting borrowings: {ex.Message}");
            }
            return borrowings;
        }
        public bool CheckoutBook(string memberId, string bookId, int loanPeriodDays = 14)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    DateTime dueDate = DateTime.Now.AddDays(loanPeriodDays);
                    using (var command = StoredProcedureHelper.CreateCommand("SP_CheckoutBook", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_MemberId", int.Parse(memberId));
                        StoredProcedureHelper.AddParameter(command, "p_BookId", int.Parse(bookId));
                        StoredProcedureHelper.AddParameter(command, "p_DueDate", dueDate);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int borrowingId = Convert.ToInt32(reader["BorrowingId"]);
                                System.Diagnostics.Debug.WriteLine($"Book checked out. Borrowing ID: {borrowingId}");
                                return true;
                            }
                        }
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking out book: {ex.Message}");
                throw;
            }
        }
        public bool ReturnBook(string borrowingId)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    using (var command = StoredProcedureHelper.CreateCommand("SP_ReturnBook", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_BorrowingId", int.Parse(borrowingId));
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int daysOverdue = Convert.ToInt32(reader["DaysOverdue"]);
                                decimal fineAmount = Convert.ToDecimal(reader["FineAmount"]);
                                System.Diagnostics.Debug.WriteLine($"Book returned. Days overdue: {daysOverdue}, Fine: ${fineAmount}");
                                return true;
                            }
                        }
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error returning book: {ex.Message}");
                throw;
            }
        }
        public bool RenewBook(string borrowingId, int extensionDays = 7)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    DateTime newDueDate;
                    using (var getCmd = StoredProcedureHelper.CreateCommand("SP_GetBorrowingById", connection))
                    {
                        StoredProcedureHelper.AddParameter(getCmd, "p_BorrowingId", int.Parse(borrowingId));
                        using (var reader = getCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                DateTime currentDueDate = Convert.ToDateTime(reader["DueDate"]);
                                newDueDate = currentDueDate.AddDays(extensionDays);
                            }
                            else
                            {
                                throw new Exception("Borrowing record not found");
                            }
                        }
                    }
                    using (var command = StoredProcedureHelper.CreateCommand("SP_RenewBook", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_BorrowingId", int.Parse(borrowingId));
                        StoredProcedureHelper.AddParameter(command, "p_NewDueDate", newDueDate);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int rowsAffected = Convert.ToInt32(reader["RowsAffected"]);
                                return rowsAffected > 0;
                            }
                        }
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error renewing book: {ex.Message}");
                throw;
            }
        }
        public BorrowingInfo GetBorrowingById(string borrowingId)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    using (var command = StoredProcedureHelper.CreateCommand("SP_GetBorrowingById", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_BorrowingId", int.Parse(borrowingId));
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new BorrowingInfo
                                {
                                    BorrowingId = reader["BorrowingId"].ToString(),
                                    MemberId = reader["MemberId"].ToString(),
                                    MemberNumber = reader["MemberNumber"].ToString(),
                                    MemberName = reader["MemberName"].ToString(),
                                    BookId = reader["BookId"].ToString(),
                                    BookTitle = reader["BookTitle"].ToString(),
                                    BookAuthor = reader["BookAuthor"].ToString(),
                                    BorrowDate = Convert.ToDateTime(reader["BorrowDate"]),
                                    DueDate = Convert.ToDateTime(reader["DueDate"]),
                                    ReturnDate = reader["ReturnDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["ReturnDate"]) : null,
                                    Status = reader["Status"].ToString(),
                                    DaysOverdue = Convert.ToInt32(reader["DaysOverdue"])
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting borrowing: {ex.Message}");
            }
            return null;
        }
    }
}