using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LMS_Library_Management_System.Helper;

namespace LMS_Library_Management_System.Forms.Dashboard
{
    public partial class EditUserDialog : Form
    {
        public string FullName { get; private set; }
        public string Email { get; private set; }
        public string PhoneNumber { get; private set; }
        public string Department { get; private set; }

        private TextBox txtFullName;
        private TextBox txtEmail;
        private TextBox txtPhoneNumber;
        private TextBox txtDepartment;

        public EditUserDialog(string fullName = "", string email = "", string phoneNumber = "", string department = "")
        {
            InitializeComponent();
            this.Text = "Edit User";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.White;
            this.Padding = new Padding(0);
            
            SetupDialog(fullName, email, phoneNumber, department);
        }

        private void SetupDialog(string fullName, string email, string phoneNumber, string department)
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
                Height = 80,
                BackColor = Color.White,
                Padding = new Padding(30, 25, 30, 15)
            };

            Label lblTitle = new Label
            {
                Text = "Edit User",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = ThemeConstants.PrimaryMaroon,
                Location = new Point(30, 25),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Update user information",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Gray,
                Location = new Point(30, 60),
                AutoSize = true
            };

            // Close button
            Button btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 16F),
                ForeColor = Color.Gray,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(40, 40),
                Location = new Point(headerPanel.Width - 50, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            headerPanel.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, btnClose });

            // Content panel
            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(30, 20, 30, 20)
            };

            int yPos = 20;
            int labelWidth = 120;
            int inputWidth = 400;
            int spacing = 30;

            // Full Name
            Label lblFullName = new Label
            {
                Text = "Full Name *",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, yPos),
                Size = new Size(labelWidth, 25),
                AutoSize = false
            };

            Panel pnlFullName = new Panel
            {
                Location = new Point(0, yPos + 25),
                Size = new Size(inputWidth, 40),
                BackColor = Color.White
            };
            pnlFullName.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlFullName.Width - 1, pnlFullName.Height - 1), 6))
                using (Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            txtFullName = new TextBox
            {
                Location = new Point(10, 8),
                Size = new Size(inputWidth - 20, 24),
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                Text = fullName
            };
            pnlFullName.Controls.Add(txtFullName);

            yPos += 25 + 40 + spacing;

            // Email
            Label lblEmail = new Label
            {
                Text = "Email Address *",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, yPos),
                Size = new Size(labelWidth, 25),
                AutoSize = false
            };

            Panel pnlEmail = new Panel
            {
                Location = new Point(0, yPos + 25),
                Size = new Size(inputWidth, 40),
                BackColor = Color.White
            };
            pnlEmail.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlEmail.Width - 1, pnlEmail.Height - 1), 6))
                using (Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            txtEmail = new TextBox
            {
                Location = new Point(10, 8),
                Size = new Size(inputWidth - 20, 24),
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                Text = email
            };
            pnlEmail.Controls.Add(txtEmail);

            yPos += 25 + 40 + spacing;

            // Phone Number
            Label lblPhone = new Label
            {
                Text = "Phone Number",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, yPos),
                Size = new Size(labelWidth, 25),
                AutoSize = false
            };

            Panel pnlPhone = new Panel
            {
                Location = new Point(0, yPos + 25),
                Size = new Size(inputWidth, 40),
                BackColor = Color.White
            };
            pnlPhone.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlPhone.Width - 1, pnlPhone.Height - 1), 6))
                using (Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            txtPhoneNumber = new TextBox
            {
                Location = new Point(10, 8),
                Size = new Size(inputWidth - 20, 24),
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                Text = phoneNumber
            };
            pnlPhone.Controls.Add(txtPhoneNumber);

            yPos += 25 + 40 + spacing;

            // Department
            Label lblDepartment = new Label
            {
                Text = "Department",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, yPos),
                Size = new Size(labelWidth, 25),
                AutoSize = false
            };

            Panel pnlDepartment = new Panel
            {
                Location = new Point(0, yPos + 25),
                Size = new Size(inputWidth, 40),
                BackColor = Color.White
            };
            pnlDepartment.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlDepartment.Width - 1, pnlDepartment.Height - 1), 6))
                using (Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            txtDepartment = new TextBox
            {
                Location = new Point(10, 8),
                Size = new Size(inputWidth - 20, 24),
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                Text = department
            };
            pnlDepartment.Controls.Add(txtDepartment);

            contentPanel.Controls.AddRange(new Control[] 
            { 
                lblFullName, pnlFullName, 
                lblEmail, pnlEmail, 
                lblPhone, pnlPhone, 
                lblDepartment, pnlDepartment 
            });

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
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            // Save Changes button
            Button btnSave = new Button
            {
                Text = "✏ Save Changes",
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
            btnSave.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnSave.Width - 1, btnSave.Height - 1), 6))
                {
                    e.Graphics.FillPath(new SolidBrush(btnSave.BackColor), path);
                }
                TextRenderer.DrawText(e.Graphics, btnSave.Text, btnSave.Font,
                    new Rectangle(0, 0, btnSave.Width, btnSave.Height),
                    btnSave.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtFullName.Text) || string.IsNullOrWhiteSpace(txtEmail.Text))
                {
                    MessageBox.Show("Full Name and Email Address are required fields.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                FullName = txtFullName.Text;
                Email = txtEmail.Text;
                PhoneNumber = txtPhoneNumber.Text;
                Department = txtDepartment.Text;
                this.DialogResult = DialogResult.OK;
            };

            footerPanel.Controls.AddRange(new Control[] { btnCancel, btnSave });

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


