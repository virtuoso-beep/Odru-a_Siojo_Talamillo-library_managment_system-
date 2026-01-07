using System;
using System.Drawing;
using System.Windows.Forms;
using Library_Management_System.Helper;
using Library_Management_System.Service;
using static Library_Management_System.Helper.PlaceholderTextHelper;

namespace Library_Management_System.Forms.Authentication
{
    public partial class ForgotPasswordForm : Form
    {
        private AuthenticationService _authService;
        private EmailService _emailService;

        public ForgotPasswordForm()
        {
            InitializeComponent();
            _authService = new AuthenticationService();
            _emailService = new EmailService();
            SetupForm();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Size = new Size(450, 350);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Forgot Password";
            this.BackColor = Color.White;
            this.ResumeLayout(false);
        }

        private void SetupForm()
        {
            this.Controls.Clear();

            Label lblTitle = new Label
            {
                Text = "Reset Password",
                Font = new Font("Georgia", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(128, 0, 0),
                AutoSize = true,
                Location = new Point(30, 30)
            };

            Label lblSubtitle = new Label
            {
                Text = "Enter your email address and we'll send you a password reset link.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(30, 70),
                Width = 380
            };

            Label lblEmail = new Label
            {
                Text = "Email Address",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(30, 120),
                AutoSize = true
            };

            TextBox txtEmail = new TextBox
            {
                Location = new Point(30, 145),
                Size = new Size(380, 35),
                Font = new Font("Segoe UI", 11F),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtEmail.SetPlaceholder("Enter your email address");

            Button btnSubmit = new Button
            {
                Text = "Send Reset Link",
                Location = new Point(30, 210),
                Size = new Size(380, 40),
                BackColor = Color.FromArgb(128, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSubmit.FlatAppearance.BorderSize = 0;

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(30, 260),
                Size = new Size(380, 35),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(128, 0, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(128, 0, 0);
            btnCancel.FlatAppearance.BorderSize = 1;

            btnSubmit.Click += (s, e) =>
            {
                string email = txtEmail.GetActualText();
                if (string.IsNullOrWhiteSpace(email))
                {
                    MessageBox.Show("Please enter your email address.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }

                if (!IsValidEmail(email))
                {
                    MessageBox.Show("Please enter a valid email address.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }

                btnSubmit.Enabled = false;
                btnSubmit.Text = "Sending...";

                try
                {
                    string token = _authService.RequestPasswordReset(email);
                    if (token != null)
                    {
                        // Generate reset link (in a real app, this would be a web URL)
                        string resetLink = $"library://reset?token={token}";
                        
                        bool emailSent = _emailService.SendPasswordResetEmail(email, token, resetLink);
                        
                        if (emailSent)
                        {
                            MessageBox.Show(
                                $"Password reset link has been sent to {email}.\n\n" +
                                $"Reset Token: {token}\n\n" +
                                $"Note: In a production environment, this would be sent via email. " +
                                $"Please use this token to reset your password.",
                                "Reset Link Sent",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information
                            );
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show(
                                $"Password reset token generated but email could not be sent.\n\n" +
                                $"Reset Token: {token}\n\n" +
                                $"Please contact administrator or use this token to reset your password.",
                                "Token Generated",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning
                            );
                        }
                    }
                    else
                    {
                        // Don't reveal if user exists for security
                        MessageBox.Show(
                            "If an account with that email exists, a password reset link has been sent.",
                            "Reset Link Sent",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    btnSubmit.Enabled = true;
                    btnSubmit.Text = "Send Reset Link";
                }
            };

            btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.AddRange(new Control[] 
            { 
                lblTitle, lblSubtitle, lblEmail, txtEmail, btnSubmit, btnCancel 
            });
        }

        private bool IsValidEmail(string email)
        {
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
    }
}

