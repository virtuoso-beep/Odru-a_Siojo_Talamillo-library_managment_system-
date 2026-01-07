using System;
using System.Windows.Forms;
namespace Library_Management_System.Helper
{
    public static class ErrorHandler
    {
        public static void ShowUserFriendlyError(Exception ex, string context = "operation")
        {
            string userMessage = GetUserFriendlyMessage(ex, context);
            string technicalMessage = ex.Message;
            if (ex.InnerException != null)
            {
                technicalMessage += $"\n\nTechnical Details: {ex.InnerException.Message}";
            }
            System.Diagnostics.Debug.WriteLine($"[{context}] Error: {technicalMessage}\nStack Trace: {ex.StackTrace}");
            MessageBox.Show(
                userMessage,
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        public static void ShowWarning(string message, string title = "Warning")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        public static void ShowInfo(string message, string title = "Information")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        public static bool ShowConfirm(string message, string title = "Confirm")
        {
            return MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }
        private static string GetUserFriendlyMessage(Exception ex, string context)
        {
            string baseMessage = $"An error occurred while {context}.";
            if (ex is MySql.Data.MySqlClient.MySqlException mysqlEx)
            {
                switch (mysqlEx.Number)
                {
                    case 1045:
                        return "Database access denied. Please check your database credentials.";
                    case 1049:
                        return "Database not found. Please ensure the database exists.";
                    case 2006:
                        return "Database connection lost. Please try again.";
                    case 1062:
                        return "This record already exists. Please check for duplicates.";
                    case 1451:
                        return "Cannot delete this record because it is being used by other records.";
                    case 1452:
                        return "Invalid reference. The related record does not exist.";
                    default:
                        return $"{baseMessage}\n\nDatabase Error: {mysqlEx.Message}";
                }
            }
            if (ex is System.Data.SqlClient.SqlException)
            {
                return $"{baseMessage}\n\nDatabase connection error. Please check your connection settings.";
            }
            if (ex is UnauthorizedAccessException)
            {
                return "You do not have permission to perform this action.";
            }
            if (ex is System.IO.FileNotFoundException)
            {
                return "Required file not found. Please ensure all files are in place.";
            }
            if (ex is System.IO.IOException)
            {
                return "File access error. Please ensure the file is not in use by another program.";
            }
            if (ex is ArgumentException || ex is ArgumentNullException)
            {
                return $"{baseMessage}\n\nInvalid input provided. Please check your data and try again.";
            }
            if (ex is InvalidOperationException)
            {
                return $"{baseMessage}\n\nThe operation cannot be performed in the current state.";
            }
            if (ex is TimeoutException)
            {
                return "The operation timed out. Please try again.";
            }
            return $"{baseMessage}\n\nError: {ex.Message}\n\nIf this problem persists, please contact the system administrator.";
        }
        public static void LogError(Exception ex, string context = "Application")
        {
            try
            {
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{context}] {ex.GetType().Name}: {ex.Message}\nStack Trace: {ex.StackTrace}\n\n";
                string logPath = System.IO.Path.Combine(Application.StartupPath, "error_log.txt");
                System.IO.File.AppendAllText(logPath, logMessage);
            }
            catch
            {
            }
        }
        public static void HandleErrorWithLogging(Exception ex, string context = "operation")
        {
            LogError(ex, context);
            ShowUserFriendlyError(ex, context);
        }
    }
}

