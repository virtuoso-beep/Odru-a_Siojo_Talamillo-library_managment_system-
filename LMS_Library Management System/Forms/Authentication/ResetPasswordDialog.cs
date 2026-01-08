using System;
using System.Windows.Forms;
using LMS_Library_Management_System.Models;
using LMS_Library_Management_System.Helper;
using static LMS_Library_Management_System.Helper.PlaceholderTextHelper;

namespace LMS_Library_Management_System.Forms.Authentication
{
    public partial class ResetPasswordDialog : Form
    {
        private User _user;
        public string NewPassword { get; private set; }

        public ResetPasswordDialog(User user)
        {
            InitializeComponent();
            _user = user;
        }

        private void ResetPasswordDialog_Load(object sender, EventArgs e)
        {
            this.StartPosition = FormStartPosition.CenterParent;
            lblUserInfo.Text = $"Resetting password for:\n{_user.FullName} ({_user.Email})";
            
            // Initialize password char to show placeholder clearly
            txtNewPassword.PasswordChar = '\0';
            txtConfirmPassword.PasswordChar = '\0';
            
            txtNewPassword.SetPlaceholder("Enter new password");
            txtConfirmPassword.SetPlaceholder("Confirm new password");
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            try
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

                if (newPassword.Length < 6)
                {
                    MessageBox.Show("Password must be at least 6 characters long.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtNewPassword.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(confirmPassword))
                {
                    MessageBox.Show("Please confirm your new password.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtConfirmPassword.Focus();
                    return;
                }

                if (newPassword != confirmPassword)
                {
                    MessageBox.Show("Passwords do not match.\n\nPlease enter the same password in both fields.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtConfirmPassword.Focus();
                    return;
                }

                NewPassword = newPassword;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}

