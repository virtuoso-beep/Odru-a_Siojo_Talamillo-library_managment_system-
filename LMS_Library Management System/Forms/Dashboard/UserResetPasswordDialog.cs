using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LMS_Library_Management_System.Helper;

namespace LMS_Library_Management_System.Forms.Dashboard
{
    public partial class UserResetPasswordDialog : Form
    {
        public bool SendResetLink { get; private set; }

        public UserResetPasswordDialog(string email)
        {
            InitializeComponent();
            this.Text = "Reset Password";
            this.Size = new Size(500, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.White;
            this.Padding = new Padding(0);
            
            SetupDialog(email);
        }

        private void SetupDialog(string email)
        {
            // Main container panel
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            // Header panel
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(30, 20, 30, 10)
            };

            Label lblTitle = new Label
            {
                Text = "Reset Password",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = ThemeConstants.PrimaryMaroon,
                Location = new Point(30, 20),
                AutoSize = true
            };

            headerPanel.Controls.Add(lblTitle);

            // Content panel
            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(30, 20, 30, 20)
            };

            Label lblMessage = new Label
            {
                Text = $"A password reset link will be sent to {email}. The user will be required to create a new password.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, 0),
                Size = new Size(440, 60),
                AutoSize = false
            };

            contentPanel.Controls.Add(lblMessage);

            // Footer panel with buttons
            Panel footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.White,
                Padding = new Padding(30, 15, 30, 15)
            };

            // Cancel button
            Button btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = ThemeConstants.TextDark,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(220, 220, 220) },
                Size = new Size(100, 40),
                Location = new Point(footerPanel.Width - 240, 15),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnCancel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnCancel.Width - 1, btnCancel.Height - 1), 6))
                {
                    e.Graphics.FillPath(new SolidBrush(btnCancel.BackColor), path);
                    e.Graphics.DrawPath(new Pen(btnCancel.FlatAppearance.BorderColor, 1), path);
                }
                TextRenderer.DrawText(e.Graphics, btnCancel.Text, btnCancel.Font,
                    new Rectangle(0, 0, btnCancel.Width, btnCancel.Height),
                    btnCancel.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnCancel.Click += (s, e) =>
            {
                SendResetLink = false;
                this.DialogResult = DialogResult.Cancel;
            };

            // Send Reset Link button
            Button btnSend = new Button
            {
                Text = "Send Reset Link",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ThemeConstants.PrimaryMaroon,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(140, 40),
                Location = new Point(footerPanel.Width - 130, 15),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnSend.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnSend.Width - 1, btnSend.Height - 1), 6))
                {
                    e.Graphics.FillPath(new SolidBrush(btnSend.BackColor), path);
                }
                TextRenderer.DrawText(e.Graphics, btnSend.Text, btnSend.Font,
                    new Rectangle(0, 0, btnSend.Width, btnSend.Height),
                    btnSend.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnSend.Click += (s, e) =>
            {
                SendResetLink = true;
                this.DialogResult = DialogResult.OK;
            };

            footerPanel.Controls.AddRange(new Control[] { btnCancel, btnSend });

            mainPanel.Controls.Add(headerPanel);
            mainPanel.Controls.Add(contentPanel);
            mainPanel.Controls.Add(footerPanel);

            this.Controls.Add(mainPanel);

            // Add border (same style as reservation dialog)
            this.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(Color.FromArgb(200, 200, 200), 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);
                }
            };
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

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);
        }
    }
}

