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
using Library_Management_System.Helper;
using static Library_Management_System.Helper.PlaceholderTextHelper;
using static Library_Management_System.Helper.MYSqlHelper;
using PlaceholderData = Library_Management_System.Helper.PlaceholderTextHelper.PlaceholderData;
using Library_Management_System.Interfaces;
using Library_Management_System.Models;
using Library_Management_System.Service;
namespace Library_Management_System
{
    public partial class SiginForm : Form
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
        public SiginForm()
        {
            InitializeComponent();
            _authenticationService = new Library_Management_System.Service.AuthenticationService();
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
            txtEmail.Text = "";
            txtPassword.Text = "";
            cmbLoginAs.SelectedIndex = -1;
            txtEmail.Focus();
            txtEmail.SetPlaceholder("john.doe.123456.tc@umindanao.edu.ph");
            txtPassword.PasswordChar = '\0';
            txtPassword.SetPlaceholder("Enter your password");
            btnTogglePassword.Text = "👁";
            ApplyFormRoundedCorners();
            ApplyRoundedTextBoxStyling();
            ApplyTransparentComboBoxStyling();
            string logoPath = System.IO.Path.Combine(Application.StartupPath, "Resources", "images-removebg-preview.png");
            if (System.IO.File.Exists(logoPath))
            {
                picLogo.Image = Image.FromFile(logoPath);
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
        private void SiginForm_Resize(object sender, EventArgs e)
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
            cmbLoginAs.BackColor = Color.FromArgb(240, 240, 240);
            cmbLoginAs.FlatStyle = FlatStyle.Flat;
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
        private void btnTogglePassword_Click(object sender, EventArgs e)
        {
            if (txtPassword.Tag is PlaceholderData data && txtPassword.Text == data.PlaceholderText)
            {
                return;
            }
            passwordVisible = !passwordVisible;
            if (passwordVisible)
            {
                txtPassword.PasswordChar = '\0';
                btnTogglePassword.Text = "👁️";
            }
            else
            {
                txtPassword.PasswordChar = '●';
                btnTogglePassword.Text = "👁";
            }
        }
        private void btnSignIn_Click(object sender, EventArgs e)
        {
            try
            {
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
                    Library_Management_System.Helper.AuditLogger.LogLogin(email, true);
                }
                else
                {
                    Library_Management_System.Helper.AuditLogger.LogLogin(email, false);
                }
                if (authenticatedUser != null)
                {
                    Library_Management_System.Helper.AuditLogger.LogLogin(email, true);
                    SiginForm.SetCurrentUser(authenticatedUser);
                    MessageBox.Show($"Welcome, {authenticatedUser.FullName}!\n{authenticatedUser.GetRoleDescription()}",
                        "Login Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    try
                    {
                        Form dashboardForm;
                        if (authenticatedUser.Role == UserRole.Staff)
                        {
                            dashboardForm = new Forms.staff.StaffDashboard();
                        }
                        else
                        {
                            dashboardForm = new DashboardForm();
                        }
                        dashboardForm.FormClosed += (s, args) => 
                        {
                            SiginForm.ClearCurrentUser();
                            this.Show();
                            this.txtEmail.SetActualText("");
                            this.txtPassword.SetActualText("");
                            this.cmbLoginAs.SelectedIndex = -1;
                            btnSignIn.Enabled = true;
                            btnSignIn.Text = "Sign In";
                        };
                        this.Hide();
                        dashboardForm.Show();
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during authentication:\n{ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
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
            if (string.IsNullOrWhiteSpace(txtPassword.GetActualText()))
            {
                MessageBox.Show("Please enter your password.\n\nFor admin login, use: Admin123!", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return false;
            }
            if (txtPassword.GetActualText().Length < 6)
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
            string email = txtEmail.Text.Trim().ToLower();
            if (!IsValidEducationalEmail(email))
            {
                MessageBox.Show("Please ensure:\n\n� It must be institutional email\n� Format: firstname.lastname.IDnumber.tc@umindanao.edu.ph\n\nExample: john.doe.123456.tc@umindanao.edu.ph",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEmail.Focus();
                return false;
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
    }
}
