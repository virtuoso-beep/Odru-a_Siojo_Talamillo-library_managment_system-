using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using LMS_Library_Management_System.Helper;
using LMS_Library_Management_System.Models;
using LMS_Library_Management_System.Interfaces;
using LMS_Library_Management_System.Service;
using static LMS_Library_Management_System.Helper.PlaceholderTextHelper;
using PlaceholderData = LMS_Library_Management_System.Helper.PlaceholderTextHelper.PlaceholderData;

namespace LMS_Library_Management_System.Forms.Authentication
{
    public partial class SignInForm : Form
    {
        private static User _currentUser;
        private bool passwordVisible = false;
        private readonly IAuthenticationService _authenticationService;

        public static User CurrentUser => _currentUser;
        public static bool IsLoggedIn => _currentUser != null;

        public static void SetCurrentUser(User user)
        {
            _currentUser = user;
        }

        public static void ClearCurrentUser()
        {
            _currentUser = null;
        }

        public SignInForm()
        {
            InitializeComponent();
            _authenticationService = new AuthenticationService();
        }

        private void SignAndSignUpForms_Load(object sender, EventArgs e)
        {
            this.StartPosition = FormStartPosition.Manual;
            int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
            int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
            this.Location = new Point(
                (screenWidth - this.Width) / 2,
                50
            );
            this.Height = 700;

            // Ensure form controls are properly initialized
            txtEmail.Text = "";
            txtPassword.Text = "";
            cmbLoginAs.SelectedIndex = -1;
            txtEmail.Focus();

            // Set up placeholder text
            txtEmail.SetPlaceholder("Enter your email");
            // Password placeholder - ensure it shows properly
            txtPassword.PasswordChar = '\0'; // Clear password char initially to show placeholder
            txtPassword.SetPlaceholder("Enter your password");
            
            // Ensure password field clears placeholder and requires input when focused
            txtPassword.Enter += TxtPassword_Enter;
            txtPassword.GotFocus += TxtPassword_GotFocus;

            ApplyFormRoundedCorners();
            
            // Apply rounded corners and styling to textbox containers
            ApplyRoundedTextBoxStyling();
            
            // Apply transparent border styling to combobox
            ApplyTransparentComboBoxStyling();

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
        }


        private void ApplyFormRoundedCorners()
        {
            GraphicsPath path = new GraphicsPath();
            int radius = 20;
            Rectangle rect = this.ClientRectangle;

            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseAllFigures();

            this.Region = new Region(path);
        }

        private void SignInForm_Resize(object sender, EventArgs e)
        {
            ApplyFormRoundedCorners();
        }

        private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseAllFigures();
            return path;
        }

        private void ApplyRoundedTextBoxStyling()
        {
            // Email container with rounded corners
            pnlEmailContainer.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, pnlEmailContainer.Width - 1, pnlEmailContainer.Height - 1);
                using (GraphicsPath path = CreateRoundedRectangle(rect, 10))
                {
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (Pen pen = new Pen(Color.FromArgb(200, 200, 200), 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };
            pnlEmailContainer.Region = new Region(CreateRoundedRectangle(new Rectangle(0, 0, pnlEmailContainer.Width, pnlEmailContainer.Height), 10));

            // Password container with rounded corners
            pnlPasswordContainer.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, pnlPasswordContainer.Width - 1, pnlPasswordContainer.Height - 1);
                using (GraphicsPath path = CreateRoundedRectangle(rect, 10))
                {
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (Pen pen = new Pen(Color.FromArgb(200, 200, 200), 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };
            pnlPasswordContainer.Region = new Region(CreateRoundedRectangle(new Rectangle(0, 0, pnlPasswordContainer.Width, pnlPasswordContainer.Height), 10));

            // Combo container with rounded corners
            pnlComboContainer.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, pnlComboContainer.Width - 1, pnlComboContainer.Height - 1);
                using (GraphicsPath path = CreateRoundedRectangle(rect, 10))
                {
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (Pen pen = new Pen(Color.FromArgb(200, 200, 200), 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };
            pnlComboContainer.Region = new Region(CreateRoundedRectangle(new Rectangle(0, 0, pnlComboContainer.Width, pnlComboContainer.Height), 10));
        }

        private void ApplyTransparentComboBoxStyling()
        {
            // Make combobox background match container to hide border
            cmbLoginAs.BackColor = Color.FromArgb(240, 240, 240);
            cmbLoginAs.FlatStyle = FlatStyle.Flat;
            
            // The dropdown arrow will remain visible as it's part of the native control
            // The border is hidden by matching the background color with the container
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Are you sure you want to exit the application?",
                "Confirm Exit",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
            
            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
        }


        private void TxtPassword_Enter(object sender, EventArgs e)
        {
            // Clear placeholder when user clicks on password field
            if (txtPassword.Tag is PlaceholderData data && txtPassword.Text == data.PlaceholderText)
            {
                txtPassword.Text = "";
                txtPassword.ForeColor = data.OriginalForeColor;
                txtPassword.PasswordChar = '●'; // Set password char immediately
            }
        }

        private void TxtPassword_GotFocus(object sender, EventArgs e)
        {
            // Ensure placeholder is cleared when password field gets focus
            if (txtPassword.Tag is PlaceholderData data && txtPassword.Text == data.PlaceholderText)
            {
                txtPassword.Text = "";
                txtPassword.ForeColor = data.OriginalForeColor;
                txtPassword.PasswordChar = '●'; // Set password char immediately
            }
        }

        private void btnTogglePassword_Click(object sender, EventArgs e)
        {
            passwordVisible = !passwordVisible;
            
            // Only set password char if there's actual text (not placeholder)
            if (txtPassword.Tag is PlaceholderData data && txtPassword.Text == data.PlaceholderText)
            {
                // Don't toggle if showing placeholder
                return;
            }
            
            txtPassword.PasswordChar = passwordVisible ? '\0' : '●';
            btnTogglePassword.Text = passwordVisible ? "👁️" : "👁";
        }

        private void btnSignIn_Click(object sender, EventArgs e)
        {
            // Always reset button state at the start to ensure clean state
            btnSignIn.Enabled = true;
            btnSignIn.Text = "Sign In";
            
            try
            {
                // Debug: Check form state
                System.Diagnostics.Debug.WriteLine($"Email: '{txtEmail.GetActualText()}'");
                System.Diagnostics.Debug.WriteLine($"Password: '{txtPassword.GetActualText()}' (Length: {txtPassword.GetActualText().Length})");
                System.Diagnostics.Debug.WriteLine($"Role: '{cmbLoginAs.SelectedItem}'");

                if (!ValidateInput())
                {
                    return;
                }

                string email = txtEmail.GetActualText().Trim();
                string password = txtPassword.GetActualText();
                string loginAs = cmbLoginAs.SelectedItem?.ToString();

                UserRole expectedRole = MapRoleFromString(loginAs);
                if (expectedRole == 0)
                {
                    MessageBox.Show("Please select a valid role.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                btnSignIn.Enabled = false;
                btnSignIn.Text = "Signing in...";
                Application.DoEvents();

                User authenticatedUser = _authenticationService.Authenticate(email, password, expectedRole);

                if (authenticatedUser != null)
                {
                    SignInForm.SetCurrentUser(authenticatedUser);

                    // Update last login timestamp
                    try
                    {
                        var userManagementService = new Service.UserManagementService();
                        userManagementService.UpdateLastLogin(email);
                    }
                    catch (Exception loginEx)
                    {
                        // Log but don't prevent login if last login update fails
                        System.Diagnostics.Debug.WriteLine($"Failed to update last login: {loginEx.Message}");
                    }

                    MessageBox.Show($"Welcome, {authenticatedUser.FullName}!\n{authenticatedUser.GetRoleDescription()}",
                        "Login Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    try
                    {
                        Form dashboard = GetDashboardForRole(authenticatedUser.Role);
                        
                        dashboard.FormClosed += (s, args) => 
                        {
                            SignInForm.ClearCurrentUser();
                            this.Show();
                            this.txtEmail.SetActualText("");
                            this.txtPassword.SetActualText("");
                            this.cmbLoginAs.SelectedIndex = -1;
                            btnSignIn.Enabled = true;
                            btnSignIn.Text = "Sign In";
                        };
                        
                        this.Hide();
                        dashboard.Show();
                    }
                    catch (Exception ex)
                    {
                        string errorDetails = $"Error: {ex.Message}";
                        if (ex.InnerException != null)
                        {
                            errorDetails += $"\n\nInner Exception: {ex.InnerException.Message}";
                        }
                        errorDetails += $"\n\nStack Trace: {ex.StackTrace}";
                        
                        MessageBox.Show($"Error opening dashboard:\n\n{errorDetails}", 
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        
                        this.Show();
                        btnSignIn.Enabled = true;
                        btnSignIn.Text = "Sign In";
                    }
                }
                else
                {
                    MessageBox.Show("Invalid email, password, or role mismatch.\nPlease check your credentials and try again.",
                        "Authentication Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    txtPassword.SetActualText("");
                    txtPassword.Focus();
                    
                    // Reset button state
                    btnSignIn.Enabled = true;
                    btnSignIn.Text = "Sign In";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during authentication:\n{ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // Reset button state
                btnSignIn.Enabled = true;
                btnSignIn.Text = "Sign In";
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtEmail.GetActualText()))
            {
                MessageBox.Show("Please enter your email address.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEmail.Focus();
                return false;
            }

            string password = txtPassword.GetActualText();
            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Password is required.\n\nPlease enter your password to sign in.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                // Clear password field and set focus
                if (txtPassword.Tag is PlaceholderData data)
                {
                    txtPassword.Text = "";
                    txtPassword.PasswordChar = '●';
                    txtPassword.ForeColor = data.OriginalForeColor;
                }
                txtPassword.Focus();
                return false;
            }

            if (password.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters long.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return false;
            }

            if (cmbLoginAs.SelectedItem == null || cmbLoginAs.SelectedIndex == -1)
            {
                MessageBox.Show("Please select your role (Administrator, Staff, or Member).",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbLoginAs.Focus();
                return false;
            }

            // Only validate umindanao email format for Members
            UserRole selectedRole = MapRoleFromString(cmbLoginAs.SelectedItem?.ToString());
            if (selectedRole == UserRole.Member)
            {
                string email = txtEmail.Text.Trim().ToLower();
                if (!IsValidEducationalEmail(email))
                {
                    MessageBox.Show("Please ensure:\n\n• It must be institutional email\n• Format: firstname.lastname.IDnumber.tc@umindanao.edu.ph\n\nExample: john.doe.123456.tc@umindanao.edu.ph",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return false;
                }
            }
            else
            {
                // For Admin and Staff, just validate it's a valid email format
                string email = txtEmail.Text.Trim().ToLower();
                try
                {
                    var addr = new System.Net.Mail.MailAddress(email);
                    if (addr.Address != email)
                    {
                        MessageBox.Show("Please enter a valid email address.",
                            "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtEmail.Focus();
                        return false;
                    }
                }
                catch
                {
                    MessageBox.Show("Please enter a valid email address.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return false;
                }
            }

            return true;
        }

        private bool IsValidEducationalEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            email = email.Trim().ToLower();
            if (!email.EndsWith("@umindanao.edu.ph"))
                return false;

            string[] emailParts = email.Split('@');
            if (emailParts.Length != 2)
                return false;
                
            string localPart = emailParts[0];

            string[] parts = localPart.Split('.');
            
            if (parts.Length != 4)
                return false;
            
            if (string.IsNullOrWhiteSpace(parts[0]) || !Regex.IsMatch(parts[0], @"^[a-z]+$"))
                return false;
            
            if (string.IsNullOrWhiteSpace(parts[1]) || !Regex.IsMatch(parts[1], @"^[a-z]+$"))
                return false;
            
            if (string.IsNullOrWhiteSpace(parts[2]) || !Regex.IsMatch(parts[2], @"^[0-9]+$"))
                return false;
            
            if (parts[3] != "tc")
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private UserRole MapRoleFromString(string roleString)
        {
            if (string.IsNullOrWhiteSpace(roleString))
                return 0;

            switch (roleString.ToLower())
            {
                case "administrator":
                    return UserRole.Administrator;
                case "staff":
                    return UserRole.Staff;
                case "member":
                    return UserRole.Member;
                default:
                    return 0;
            }
        }

        private Form GetDashboardForRole(UserRole role)
        {
            switch (role)
            {
                case UserRole.Administrator:
                    return new Forms.Dashboard.AdminDashboardForm();
                case UserRole.Staff:
                    return new Forms.Dashboard.StaffDashboardForm();
                case UserRole.Member:
                    // For now, show a message - Member dashboard can be implemented later
                    throw new NotImplementedException("Member dashboard is not yet implemented");
                default:
                    throw new ArgumentException("Invalid user role");
            }
        }

        private void lnkForgotPassword_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using (ForgotPasswordForm forgotPasswordForm = new ForgotPasswordForm())
            {
                forgotPasswordForm.ShowDialog(this);
            }
        }
    }
}

