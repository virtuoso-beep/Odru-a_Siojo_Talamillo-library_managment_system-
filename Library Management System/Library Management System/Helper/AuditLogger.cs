using System;
using System.IO;
using System.Text;

namespace Library_Management_System.Helper
{
    public static class AuditLogger
    {
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LibraryManagementSystem",
            "audit.log");

        static AuditLogger()
        {
            // Ensure directory exists
            string directory = Path.GetDirectoryName(LogFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public static void Log(string action, string userEmail, string details = "", string ipAddress = "")
        {
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}|{userEmail}|{action}|{details}|{ipAddress}";

                using (StreamWriter writer = new StreamWriter(LogFilePath, true, Encoding.UTF8))
                {
                    writer.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                // Fallback to debug output if file logging fails
                System.Diagnostics.Debug.WriteLine($"Audit logging failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Original log entry: {action}|{userEmail}|{details}");
            }
        }

        public static void LogLogin(string userEmail, bool success, string ipAddress = "")
        {
            string action = success ? "LOGIN_SUCCESS" : "LOGIN_FAILED";
            Log(action, userEmail, "", ipAddress);
        }

        public static void LogLogout(string userEmail, string reason = "USER_INITIATED")
        {
            Log("LOGOUT", userEmail, reason);
        }

        public static void LogMemberRegistration(string adminEmail, string memberEmail)
        {
            Log("MEMBER_REGISTERED", adminEmail, $"New member: {memberEmail}");
        }

        public static void LogBookCheckout(string userEmail, string memberId, string bookId, string bookTitle)
        {
            Log("BOOK_CHECKOUT", userEmail, $"Member: {memberId}, Book: {bookId} ({bookTitle})");
        }

        public static void LogBookReturn(string userEmail, string borrowingId, string bookTitle)
        {
            Log("BOOK_RETURN", userEmail, $"Borrowing ID: {borrowingId}, Book: {bookTitle}");
        }

        public static void LogBookAdded(string userEmail, string bookId, string title)
        {
            Log("BOOK_ADDED", userEmail, $"Book ID: {bookId}, Title: {title}");
        }

        public static void LogBookDeleted(string userEmail, string bookId, string title)
        {
            Log("BOOK_DELETED", userEmail, $"Book ID: {bookId}, Title: {title}");
        }

        public static void LogMemberUpdated(string userEmail, string memberId, string changes)
        {
            Log("MEMBER_UPDATED", userEmail, $"Member ID: {memberId}, Changes: {changes}");
        }

        public static void LogFineProcessed(string userEmail, string fineId, string action, decimal amount)
        {
            Log("FINE_PROCESSED", userEmail, $"Fine ID: {fineId}, Action: {action}, Amount: {amount:C}");
        }
    }
}
