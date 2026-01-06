using System;
using System.Collections.Generic;
using System.Data;
using Library_Management_System.Helper;
using MySql.Data.MySqlClient;
namespace Library_Management_System.Service
{
    public class SettingsService
    {
        public class LibraryInfo
        {
            public string LibraryName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string Address { get; set; }
        }
        public class NotificationSettings
        {
            public bool EmailNotifications { get; set; } = true;
            public bool OverdueReminders { get; set; } = true;
            public bool ReservationAlerts { get; set; } = true;
            public bool DueDateReminders { get; set; } = true;
            public int ReminderDaysBeforeDue { get; set; } = 2;
        }
        public class BorrowingSettings
        {
            public int StudentLoanPeriod { get; set; } = 14;
            public int FacultyLoanPeriod { get; set; } = 30;
            public int StaffLoanPeriod { get; set; } = 21;
            public int GuestLoanPeriod { get; set; } = 7;
            public int StudentBorrowLimit { get; set; } = 5;
            public int FacultyBorrowLimit { get; set; } = 10;
            public int StaffBorrowLimit { get; set; } = 7;
            public int GuestBorrowLimit { get; set; } = 3;
            public int RenewalDays { get; set; } = 7;
            public int MaxRenewals { get; set; } = 2;
        }
        public class FinesSettings
        {
            public decimal FineRatePerDay { get; set; } = 5.00m;
            public int GracePeriodDays { get; set; } = 0;
            public decimal MaxFineAmount { get; set; } = 100.00m;
            public decimal LostBookFee { get; set; } = 50.00m;
        }
        private void SaveSetting(string key, string value, string description = null)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    string query = @"
                        INSERT INTO LibrarySettings (SettingKey, SettingValue, Description, UpdatedDate)
                        VALUES (@key, @value, @description, NOW())
                        ON DUPLICATE KEY UPDATE 
                            SettingValue = @value,
                            Description = COALESCE(@description, Description),
                            UpdatedDate = NOW()";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@key", key);
                        command.Parameters.AddWithValue("@value", value);
                        command.Parameters.AddWithValue("@description", description ?? (object)DBNull.Value);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.ErrorHandler.LogError(ex, $"SaveSetting-{key}");
                System.Diagnostics.Debug.WriteLine($"Error saving setting {key}: {ex.Message}");
                throw;
            }
        }
        private string GetSetting(string key, string defaultValue = null)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    string query = "SELECT SettingValue FROM LibrarySettings WHERE SettingKey = @key";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@key", key);
                        var result = command.ExecuteScalar();
                        return result != null ? result.ToString() : defaultValue;
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.ErrorHandler.LogError(ex, $"GetSetting-{key}");
                System.Diagnostics.Debug.WriteLine($"Error getting setting {key}: {ex.Message}");
                return defaultValue;
            }
        }
        // Library Information
        public LibraryInfo GetLibraryInfo()
        {
            return new LibraryInfo
            {
                LibraryName = GetSetting("LibraryName", "University of Mindanao Visayan Campus"),
                Email = GetSetting("LibraryEmail", "library@umindanao.edu.ph"),
                Phone = GetSetting("LibraryPhone", "+63 900-000-0000"),
                Address = GetSetting("LibraryAddress", "Tagum City, Visayan")
            };
        }
        public bool SaveLibraryInfo(LibraryInfo info)
        {
            try
            {
                SaveSetting("LibraryName", info.LibraryName, "Library name");
                SaveSetting("LibraryEmail", info.Email, "Library email address");
                SaveSetting("LibraryPhone", info.Phone, "Library phone number");
                SaveSetting("LibraryAddress", info.Address, "Library address");
                return true;
            }
            catch
            {
                return false;
            }
        }
        // Notification Settings
        public NotificationSettings GetNotificationSettings()
        {
            return new NotificationSettings
            {
                EmailNotifications = GetSetting("EmailNotifications", "true").ToLower() == "true",
                OverdueReminders = GetSetting("OverdueReminders", "true").ToLower() == "true",
                ReservationAlerts = GetSetting("ReservationAlerts", "true").ToLower() == "true",
                DueDateReminders = GetSetting("DueDateReminders", "true").ToLower() == "true",
                ReminderDaysBeforeDue = int.TryParse(GetSetting("ReminderDaysBeforeDue", "2"), out int days) ? days : 2
            };
        }
        public bool SaveNotificationSettings(NotificationSettings settings)
        {
            try
            {
                SaveSetting("EmailNotifications", settings.EmailNotifications.ToString(), "Enable email notifications");
                SaveSetting("OverdueReminders", settings.OverdueReminders.ToString(), "Send overdue reminders");
                SaveSetting("ReservationAlerts", settings.ReservationAlerts.ToString(), "Send reservation alerts");
                SaveSetting("DueDateReminders", settings.DueDateReminders.ToString(), "Send due date reminders");
                SaveSetting("ReminderDaysBeforeDue", settings.ReminderDaysBeforeDue.ToString(), "Days before due date to send reminder");
                return true;
            }
            catch
            {
                return false;
            }
        }
        // Borrowing Settings
        public BorrowingSettings GetBorrowingSettings()
        {
            return new BorrowingSettings
            {
                StudentLoanPeriod = int.TryParse(GetSetting("StudentLoanPeriod", "14"), out int sp) ? sp : 14,
                FacultyLoanPeriod = int.TryParse(GetSetting("FacultyLoanPeriod", "30"), out int fp) ? fp : 30,
                StaffLoanPeriod = int.TryParse(GetSetting("StaffLoanPeriod", "21"), out int stp) ? stp : 21,
                GuestLoanPeriod = int.TryParse(GetSetting("GuestLoanPeriod", "7"), out int gp) ? gp : 7,
                StudentBorrowLimit = int.TryParse(GetSetting("StudentBorrowLimit", "5"), out int sbl) ? sbl : 5,
                FacultyBorrowLimit = int.TryParse(GetSetting("FacultyBorrowLimit", "10"), out int fbl) ? fbl : 10,
                StaffBorrowLimit = int.TryParse(GetSetting("StaffBorrowLimit", "7"), out int stbl) ? stbl : 7,
                GuestBorrowLimit = int.TryParse(GetSetting("GuestBorrowLimit", "3"), out int gbl) ? gbl : 3,
                RenewalDays = int.TryParse(GetSetting("RenewalDays", "7"), out int rd) ? rd : 7,
                MaxRenewals = int.TryParse(GetSetting("MaxRenewals", "2"), out int mr) ? mr : 2
            };
        }
        public bool SaveBorrowingSettings(BorrowingSettings settings)
        {
            try
            {
                SaveSetting("StudentLoanPeriod", settings.StudentLoanPeriod.ToString(), "Loan period for students (days)");
                SaveSetting("FacultyLoanPeriod", settings.FacultyLoanPeriod.ToString(), "Loan period for faculty (days)");
                SaveSetting("StaffLoanPeriod", settings.StaffLoanPeriod.ToString(), "Loan period for staff (days)");
                SaveSetting("GuestLoanPeriod", settings.GuestLoanPeriod.ToString(), "Loan period for guests (days)");
                SaveSetting("StudentBorrowLimit", settings.StudentBorrowLimit.ToString(), "Maximum books students can borrow");
                SaveSetting("FacultyBorrowLimit", settings.FacultyBorrowLimit.ToString(), "Maximum books faculty can borrow");
                SaveSetting("StaffBorrowLimit", settings.StaffBorrowLimit.ToString(), "Maximum books staff can borrow");
                SaveSetting("GuestBorrowLimit", settings.GuestBorrowLimit.ToString(), "Maximum books guests can borrow");
                SaveSetting("RenewalDays", settings.RenewalDays.ToString(), "Days to extend loan on renewal");
                SaveSetting("MaxRenewals", settings.MaxRenewals.ToString(), "Maximum number of renewals allowed");
                return true;
            }
            catch
            {
                return false;
            }
        }
        // Fines Settings
        public FinesSettings GetFinesSettings()
        {
            return new FinesSettings
            {
                FineRatePerDay = decimal.TryParse(GetSetting("FineRatePerDay", "5.00"), out decimal fr) ? fr : 5.00m,
                GracePeriodDays = int.TryParse(GetSetting("GracePeriodDays", "0"), out int gpd) ? gpd : 0,
                MaxFineAmount = decimal.TryParse(GetSetting("MaxFineAmount", "100.00"), out decimal mfa) ? mfa : 100.00m,
                LostBookFee = decimal.TryParse(GetSetting("LostBookFee", "50.00"), out decimal lbf) ? lbf : 50.00m
            };
        }
        public bool SaveFinesSettings(FinesSettings settings)
        {
            try
            {
                SaveSetting("FineRatePerDay", settings.FineRatePerDay.ToString("F2"), "Fine amount per day overdue");
                SaveSetting("GracePeriodDays", settings.GracePeriodDays.ToString(), "Grace period before fines apply (days)");
                SaveSetting("MaxFineAmount", settings.MaxFineAmount.ToString("F2"), "Maximum fine amount");
                SaveSetting("LostBookFee", settings.LostBookFee.ToString("F2"), "Fee for lost books");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

