using System;
using System.Drawing;
using System.Windows.Forms;
using Library_Management_System.Helper;
using Library_Management_System.Service;
using static Library_Management_System.Helper.PlaceholderTextHelper;

namespace Library_Management_System.Forms.Authentication
{
    public partial class ResetPasswordForm : Form
    {
        private AuthenticationService _authService;
        private string _resetToken;
        private int? _userId;
        private string _userEmail;

        public ResetPasswordForm(string resetToken)
        {
            InitializeComponent();
            _authService = new AuthenticationService();
            _resetToken = resetToken;
            ValidateToken();
            SetupForm();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Size = new Size(450, 450);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Reset Password";
            this.BackColor = Color.White;
            this.ResumeLayout(false);
        }

        private void ValidateToken()
        {
            var (isValid, userId, email) = _authService.ValidateResetToken(_resetToken);
            if (!isValid || !userId.HasValue)
            {
                MessageBox.Show("Invalid or expired reset token.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return;
            }

            _userId = userId;
            _userEmail = email;
        }

        private void SetupForm()
        {
            if (_userId == null) return;

            this.Controls.Clear();

            Label lblTitle = new Label
            {
                Text = "Reset Your Password",
                Font = new Font("Georgia", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(128, 0, 0),
                AutoSize = true,
                Location = new Point(30, 30)
            };

            Label lblSubtitle = new Label
            {
                Text = $"Enter a new password for {_userEmail}",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(30, 70),
                Width = 380
            };

            Label lblNewPassword = new Label
            {
                Text = "New Password",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(30, 120),
                AutoSize = true
            };

            TextBox txtNewPassword = new TextBox
            {
                Location = new Point(30, 145),
                Size = new Size(380, 35),
                Font = new Font("Segoe UI", 11F),
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true
            };
            txtNewPassword.SetPlaceholder("Enter new password");

            Label lblConfirmPassword = new Label
            {
                Text = "Confirm Password",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(30, 200),
                AutoSize = true
            };

            TextBox txtConfirmPassword = new TextBox
            {
                Location = new Point(30, 225),
                Size = new Size(380, 35),
                Font = new Font("Segoe UI", 11F),
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true
            };
            txtConfirmPassword.SetPlaceholder("Confirm new password");

            Label lblPasswordRequirements = new Label
            {
                Text = "Password must be at least 8 characters and contain:\n• Uppercase letter\n• Lowercase letter\n• Number\n• Special character",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.DarkGray,
                Location = new Point(30, 280),
                Size = new Size(380, 80),
                AutoSize = false
            };

            Button btnReset = new Button
            {
                Text = "Reset Password",
                Location = new Point(30, 370),
                Size = new Size(380, 40),
                BackColor = Color.FromArgb(128, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnReset.FlatAppearance.BorderSize = 0;

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(30, 420),
                Size = new Size(380, 35),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(128, 0, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(128, 0, 0);
            btnCancel.FlatAppearance.BorderSize = 1;

            btnReset.Click += (s, e) =>
            {
                string newPassword = txtNewPassword.GetActualText();
                string confirmPassword = txtConfirmPassword.GetActualText();

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    MessageBox.Show("Please enter a new password.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtNewPassword.Focus();
                    return;
                }

                if (newPassword != confirmPassword)
                {
                    MessageBox.Show("Passwords do not match.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtConfirmPassword.Focus();
                    return;
                }

                try
                {
                    bool success = _authService.ResetPassword(_resetToken, newPassword);
                    if (success)
                    {
                        MessageBox.Show("Your password has been reset successfully. Please login with your new password.", 
                            "Password Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Failed to reset password. The token may be invalid or expired.", 
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(ex.Message, "Password Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.AddRange(new Control[] 
            { 
                lblTitle, lblSubtitle, lblNewPassword, txtNewPassword,
                lblConfirmPassword, txtConfirmPassword, lblPasswordRequirements,
                btnReset, btnCancel
            });
        }
    }
}

