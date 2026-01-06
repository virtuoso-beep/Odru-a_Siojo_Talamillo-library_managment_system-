using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using Library_Management_System.Helper;

namespace Library_Management_System.Service
{
    public class EmailService
    {
        private string _smtpServer;
        private int _smtpPort;
        private string _smtpUsername;
        private string _smtpPassword;
        private bool _enableSsl;
        private string _fromEmail;
        private string _fromName;

        public EmailService()
        {
            LoadEmailSettings();
        }

        private void LoadEmailSettings()
        {
            try
            {
                var settingsService = new SettingsService();
                var libraryInfo = settingsService.GetLibraryInfo();
                var notificationSettings = settingsService.GetNotificationSettings();

                // Get SMTP settings from LibrarySettings table
                _smtpServer = GetSetting("SMTPServer", "smtp.gmail.com");
                _smtpPort = int.TryParse(GetSetting("SMTPPort", "587"), out int port) ? port : 587;
                _smtpUsername = GetSetting("SMTPUsername", "");
                _smtpPassword = GetSetting("SMTPPassword", "");
                _enableSsl = GetSetting("SMTPEnableSSL", "true").ToLower() == "true";
                _fromEmail = libraryInfo.Email ?? GetSetting("FromEmail", "library@umindanao.edu.ph");
                _fromName = libraryInfo.LibraryName ?? "Library Management System";
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "EmailService.LoadEmailSettings");
                // Use defaults
                _smtpServer = "smtp.gmail.com";
                _smtpPort = 587;
                _enableSsl = true;
                _fromEmail = "library@umindanao.edu.ph";
                _fromName = "Library Management System";
            }
        }

        private string GetSetting(string key, string defaultValue)
        {
            try
            {
                var settingsService = new SettingsService();
                using (var connection = new MySql.Data.MySqlClient.MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    string query = "SELECT SettingValue FROM LibrarySettings WHERE SettingKey = @key";
                    using (var command = new MySql.Data.MySqlClient.MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@key", key);
                        var result = command.ExecuteScalar();
                        return result != null ? result.ToString() : defaultValue;
                    }
                }
            }
            catch
            {
                return defaultValue;
            }
        }

        public bool SendEmail(string toEmail, string subject, string body, bool isHtml = true)
        {
            if (string.IsNullOrWhiteSpace(toEmail) || string.IsNullOrWhiteSpace(_smtpServer))
            {
                ErrorHandler.LogError(new Exception("Email configuration incomplete"), "EmailService.SendEmail");
                return false;
            }

            try
            {
                using (var client = new SmtpClient(_smtpServer, _smtpPort))
                {
                    client.EnableSsl = _enableSsl;
                    client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                    client.Timeout = 30000;

                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(_fromEmail, _fromName);
                        message.To.Add(toEmail);
                        message.Subject = subject;
                        message.Body = body;
                        message.IsBodyHtml = isHtml;
                        message.BodyEncoding = Encoding.UTF8;
                        message.SubjectEncoding = Encoding.UTF8;

                        client.Send(message);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "EmailService.SendEmail");
                return false;
            }
        }

        public bool SendOverdueReminder(string memberEmail, string memberName, string bookTitle, DateTime dueDate, decimal fineAmount)
        {
            if (!IsEmailNotificationsEnabled())
                return false;

            string subject = "Overdue Book Reminder";
            string body = EmailTemplates.GetOverdueReminderTemplate(memberName, bookTitle, dueDate, fineAmount);
            return SendEmail(memberEmail, subject, body);
        }

        public bool SendDueDateReminder(string memberEmail, string memberName, string bookTitle, DateTime dueDate, int daysUntilDue)
        {
            if (!IsEmailNotificationsEnabled())
                return false;

            string subject = $"Book Due Soon - {daysUntilDue} Day(s) Remaining";
            string body = EmailTemplates.GetDueDateReminderTemplate(memberName, bookTitle, dueDate, daysUntilDue);
            return SendEmail(memberEmail, subject, body);
        }

        public bool SendReservationAlert(string memberEmail, string memberName, string bookTitle)
        {
            if (!IsEmailNotificationsEnabled())
                return false;

            string subject = "Reserved Book Available";
            string body = EmailTemplates.GetReservationAlertTemplate(memberName, bookTitle);
            return SendEmail(memberEmail, subject, body);
        }

        public bool SendPasswordResetEmail(string userEmail, string resetToken, string resetLink)
        {
            string subject = "Password Reset Request";
            string body = EmailTemplates.GetPasswordResetTemplate(userEmail, resetToken, resetLink);
            return SendEmail(userEmail, subject, body);
        }

        private bool IsEmailNotificationsEnabled()
        {
            try
            {
                var settingsService = new SettingsService();
                var notificationSettings = settingsService.GetNotificationSettings();
                return notificationSettings.EmailNotifications;
            }
            catch
            {
                return false;
            }
        }
    }

    public static class EmailTemplates
    {
        public static string GetOverdueReminderTemplate(string memberName, string bookTitle, DateTime dueDate, decimal fineAmount)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #800000; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 20px; }}
        .alert {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Library Management System</h2>
        </div>
        <div class='content'>
            <h3>Overdue Book Reminder</h3>
            <p>Dear {memberName},</p>
            <p>This is a reminder that you have an overdue book:</p>
            <div class='alert'>
                <strong>Book Title:</strong> {bookTitle}<br>
                <strong>Due Date:</strong> {dueDate:MMMM dd, yyyy}<br>
                <strong>Fine Amount:</strong> ₱{fineAmount:F2}
            </div>
            <p>Please return this book as soon as possible to avoid additional fines.</p>
            <p>If you have already returned this book, please ignore this message.</p>
            <p>Thank you for your attention.</p>
        </div>
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        public static string GetDueDateReminderTemplate(string memberName, string bookTitle, DateTime dueDate, int daysUntilDue)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #800000; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 20px; }}
        .info {{ background-color: #d1ecf1; border-left: 4px solid #0c5460; padding: 15px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Library Management System</h2>
        </div>
        <div class='content'>
            <h3>Book Due Soon Reminder</h3>
            <p>Dear {memberName},</p>
            <p>This is a friendly reminder that you have a book due soon:</p>
            <div class='info'>
                <strong>Book Title:</strong> {bookTitle}<br>
                <strong>Due Date:</strong> {dueDate:MMMM dd, yyyy}<br>
                <strong>Days Remaining:</strong> {daysUntilDue} day(s)
            </div>
            <p>Please return this book by the due date to avoid late fees.</p>
            <p>Thank you for using our library services.</p>
        </div>
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        public static string GetReservationAlertTemplate(string memberName, string bookTitle)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #800000; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 20px; }}
        .success {{ background-color: #d4edda; border-left: 4px solid #28a745; padding: 15px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Library Management System</h2>
        </div>
        <div class='content'>
            <h3>Reserved Book Available</h3>
            <p>Dear {memberName},</p>
            <p>Great news! The book you reserved is now available:</p>
            <div class='success'>
                <strong>Book Title:</strong> {bookTitle}
            </div>
            <p>Please visit the library within the next 3 days to borrow this book. After this period, your reservation will expire.</p>
            <p>We look forward to seeing you soon!</p>
        </div>
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        public static string GetPasswordResetTemplate(string userEmail, string resetToken, string resetLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #800000; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 20px; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #800000; color: white; text-decoration: none; border-radius: 4px; margin: 20px 0; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Library Management System</h2>
        </div>
        <div class='content'>
            <h3>Password Reset Request</h3>
            <p>Hello,</p>
            <p>We received a request to reset the password for your account: <strong>{userEmail}</strong></p>
            <p>Click the button below to reset your password:</p>
            <p style='text-align: center;'>
                <a href='{resetLink}' class='button'>Reset Password</a>
            </p>
            <p>Or copy and paste this link into your browser:</p>
            <p style='word-break: break-all;'>{resetLink}</p>
            <div class='warning'>
                <strong>Reset Token:</strong> {resetToken}<br>
                <strong>Note:</strong> This link will expire in 24 hours. If you did not request a password reset, please ignore this email.
            </div>
        </div>
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}

