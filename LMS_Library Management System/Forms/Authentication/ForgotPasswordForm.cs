using System;
using System.Drawing;
using System.Windows.Forms;
using LMS_Library_Management_System.Helper;
using LMS_Library_Management_System.Interfaces;
using LMS_Library_Management_System.Models;
using LMS_Library_Management_System.Service;
using MySql.Data.MySqlClient;
using static LMS_Library_Management_System.Helper.PlaceholderTextHelper;

namespace LMS_Library_Management_System.Forms.Authentication
{
    public partial class ForgotPasswordForm : Form
    {
        private readonly IAuthenticationService _authenticationService;

        public ForgotPasswordForm()
        {
            InitializeComponent();
            _authenticationService = new AuthenticationService();
        }

        private void ForgotPasswordForm_Load(object sender, EventArgs e)
        {
            this.StartPosition = FormStartPosition.Manual;
            int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
            int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
            int formWidth = this.Width;
            int formHeight = this.Height;
            this.Location = new Point(
                (screenWidth - formWidth) / 2,
                (screenHeight - formHeight) / 2
            );
            
            // Apply rounded corners to main card
            ApplyRoundedCorners();
            
            // Load logo - try multiple paths
            string[] logoPaths = new string[]
            {
                // Check in Resources folder relative to executable
                System.IO.Path.Combine(Application.StartupPath, "Resources", "logo.png"),
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.png"),
                // Check in current directory Resources
                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Resources", "logo.png"),
                // Check directly in executable directory
                System.IO.Path.Combine(Application.StartupPath, "logo.png"),
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png"),
                // Check in project source directory (for development)
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Resources", "logo.png"),
                System.IO.Path.Combine(Application.StartupPath, "..", "..", "Resources", "logo.png")
            };

            bool logoLoaded = false;
            foreach (string logoPath in logoPaths)
            {
                try
                {
                    string fullPath = System.IO.Path.GetFullPath(logoPath);
                    if (System.IO.File.Exists(fullPath))
                    {
                        picLogo.Image = Image.FromFile(fullPath);
                        logoLoaded = true;
                        System.Diagnostics.Debug.WriteLine($"Logo loaded from: {fullPath}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load logo from {logoPath}: {ex.Message}");
                    // Continue to next path if this one fails
                }
            }

            if (!logoLoaded)
            {
                System.Diagnostics.Debug.WriteLine("Logo file not found in any of the checked paths.");
            }
            
            txtEmail.SetPlaceholder("Enter your email address");
            txtEmail.Focus();
        }

        private void ForgotPasswordForm_Resize(object sender, EventArgs e)
        {
            ApplyRoundedCorners();
        }

        private void ApplyRoundedCorners()
        {
            int radius = 20;
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
            path.AddArc(pnlMainCard.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
            path.AddArc(pnlMainCard.Width - radius * 2, pnlMainCard.Height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(0, pnlMainCard.Height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseAllFigures();
            pnlMainCard.Region = new Region(path);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnResetPassword_Click(object sender, EventArgs e)
        {
            try
            {
                string email = txtEmail.GetActualText().Trim().ToLower();

                if (string.IsNullOrWhiteSpace(email))
                {
                    MessageBox.Show("Please enter your email address.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }

                // Validate email format
                try
                {
                    var addr = new System.Net.Mail.MailAddress(email);
                    if (addr.Address != email)
                    {
                        MessageBox.Show("Please enter a valid email address.", "Validation Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtEmail.Focus();
                        return;
                    }
                }
                catch
                {
                    MessageBox.Show("Please enter a valid email address.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }

                // Check if user exists
                if (!_authenticationService.UserExists(email))
                {
                    MessageBox.Show("No account found with this email address.\n\nPlease check your email and try again.",
                        "Account Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtEmail.Focus();
                    return;
                }

                // Get user from database to verify
                User user = GetUserByEmail(email);
                if (user == null)
                {
                    MessageBox.Show("Unable to retrieve account information.\n\nPlease contact system administrator.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Show reset password dialog
                using (ResetPasswordDialog resetDialog = new ResetPasswordDialog(user))
                {
                    if (resetDialog.ShowDialog() == DialogResult.OK)
                    {
                        string newPassword = resetDialog.NewPassword;
                        
                        // Update password in database
                        if (UpdateUserPassword(email, newPassword))
                        {
                            MessageBox.Show("Password has been reset successfully!\n\nYou can now sign in with your new password.",
                                "Password Reset Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Failed to reset password.\n\nPlease try again or contact system administrator.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private User GetUserByEmail(string email)
        {
            try
            {
                using (var connection = new MySqlConnection(Helper.MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    
                    using (var command = new MySqlCommand("sp_GetUserByEmail", connection))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_Email", email.Trim().ToLower());

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int roleValue = Convert.ToInt32(reader["Role"]);
                                UserRole userRole = (UserRole)roleValue;

                                User user = CreateUserFromRole(userRole);
                                user.UserId = Convert.ToInt32(reader["UserId"]);
                                user.Email = reader["Email"].ToString();
                                user.FirstName = reader["FirstName"].ToString();
                                user.LastName = reader["LastName"].ToString();
                                user.IsActive = Convert.ToBoolean(reader["IsActive"]);

                                return user;
                            }
                        }
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        private User CreateUserFromRole(UserRole role)
        {
            switch (role)
            {
                case UserRole.Administrator:
                    return new Models.Librarian();
                case UserRole.Staff:
                    return new Models.LibraryStaff();
                case UserRole.Member:
                    return new Models.Member();
                default:
                    throw new ArgumentException("Invalid user role");
            }
        }

        private bool UpdateUserPassword(string email, string newPassword)
        {
            try
            {
                string hashedPassword = _authenticationService.HashPassword(newPassword);
                
                using (var connection = new MySqlConnection(Helper.MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    
                    using (var command = new MySqlCommand("sp_UpdateUserPassword", connection))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_Email", email.Trim().ToLower());
                        command.Parameters.AddWithValue("p_PasswordHash", hashedPassword);
                        
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
            catch
            {
                return false;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}

