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
using Library_Management_System.Helpers;
using Library_Management_System.Interface;
using Library_Management_System.Models;
using Library_Management_System.Services;
using Library_Management_System.Forms;

namespace Library_Management_System
{
    public partial class SiginForm : Form
    {
        private bool passwordVisible = false;
        private readonly IAuthenticationService _authenticationService;

        public SiginForm()
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
            
            ApplyFormRoundedCorners();
            
            string logoPath = System.IO.Path.Combine(Application.StartupPath, "..", "..", "Resources", "images-removebg-preview.png");
            if (System.IO.File.Exists(logoPath))
            {
                picLogo.Image = Image.FromFile(logoPath);
            }
            else
            {
                logoPath = System.IO.Path.Combine(Application.StartupPath, "Resources", "images-removebg-preview.png");
                if (System.IO.File.Exists(logoPath))
                {
                    picLogo.Image = Image.FromFile(logoPath);
                }
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

        private void pnlMainCard_Paint(object sender, PaintEventArgs e)
        {
            GraphicsPath path = new GraphicsPath();
            int radius = 20;
            Rectangle rect = pnlMainCard.ClientRectangle;
            
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseAllFigures();
            
            pnlMainCard.Region = new Region(path);
        }


        private void DrawRoundedBorder(Panel panel, PaintEventArgs e, Color borderColor, int borderWidth, int radius = 8)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            Rectangle rect = panel.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;
            
            using (Pen borderPen = new Pen(borderColor, borderWidth))
            {
                GraphicsPath path = new GraphicsPath();
                path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
                path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
                path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
                path.CloseAllFigures();
                
                g.DrawPath(borderPen, path);
                path.Dispose();
            }
        }

        private void pnlEmailContainer_Paint(object sender, PaintEventArgs e)
        {
            Panel panel = sender as Panel;
            if (panel != null)
            {
                DrawRoundedBorder(panel, e, Color.FromArgb(220, 220, 220), 2, 8);
            }
        }

        private void pnlPasswordContainer_Paint(object sender, PaintEventArgs e)
        {
            Panel panel = sender as Panel;
            if (panel != null)
            {
                DrawRoundedBorder(panel, e, Color.FromArgb(220, 220, 220), 2, 8);
            }
        }

        private void pnlComboContainer_Paint(object sender, PaintEventArgs e)
        {
            Panel panel = sender as Panel;
            if (panel != null)
            {
                DrawRoundedBorder(panel, e, Color.FromArgb(220, 220, 220), 2, 8);
            }
        }

        private void btnTogglePassword_Click(object sender, EventArgs e)
        {
            passwordVisible = !passwordVisible;
            txtPassword.PasswordChar = passwordVisible ? '\0' : '●';
            btnTogglePassword.Text = passwordVisible ? "👁️" : "👁";
        }

        private void btnSignIn_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateInput())
                {
                    return;
                }

                string email = txtEmail.Text.Trim();
                string password = txtPassword.Text;
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
                    CurrentUser.SetCurrentUser(authenticatedUser);

                    MessageBox.Show($"Welcome, {authenticatedUser.FullName}!\n{authenticatedUser.GetRoleDescription()}", 
                        "Login Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    try
                    {
                        DashboardForm dashboardForm = new DashboardForm();
                        
                        dashboardForm.FormClosed += (s, args) => 
                        {
                            CurrentUser.Clear();
                            this.Show();
                            this.txtEmail.Clear();
                            this.txtPassword.Clear();
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
                    
                    txtPassword.Clear();
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
            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Please enter your email address.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEmail.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Please enter your password.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return false;
            }

            if (cmbLoginAs.SelectedItem == null)
            {
                MessageBox.Show("Please select a role.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbLoginAs.Focus();
                return false;
            }

            string email = txtEmail.Text.Trim().ToLower();
            if (!IsValidEducationalEmail(email))
            {
                MessageBox.Show("Please ensure:\n\n• It must be institutional email\n• Format: firstname.lastname.idnumber.tc@umindanao.edu.ph\n\nExample: t.odruna.142275.tc@umindanao.edu.ph", 
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
