using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LMS_Library_Management_System.Helper;

namespace LMS_Library_Management_System.Forms.Dashboard
{
    public partial class AssignRoleDialog : Form
    {
        public string SelectedRole { get; private set; }

        private ComboBox cmbRole;

        public AssignRoleDialog(string userName, string currentRole = "Librarian/Admin")
        {
            InitializeComponent();
            this.Text = "Assign Role";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(248, 247, 242);
            this.Padding = new Padding(0);
            
            SetupDialog(userName, currentRole);
        }

        private void SetupDialog(string userName, string currentRole)
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
                BackColor = ThemeConstants.PrimaryMaroon,
                Padding = new Padding(30, 25, 30, 15)
            };

            Label lblTitle = new Label
            {
                Text = "Assign Role",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(30, 25),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = $"Change the role for {userName}",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(240, 240, 240),
                Location = new Point(30, 60),
                AutoSize = true
            };

            // Close button
            Button btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 16F),
                ForeColor = Color.White,
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
                BackColor = Color.FromArgb(248, 247, 242), // Light beige
                Padding = new Padding(30, 30, 30, 20)
            };

            // Select Role label
            Label lblSelectRole = new Label
            {
                Text = "Select Role",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, 0),
                AutoSize = true
            };

            // Role dropdown
            Panel pnlRole = new Panel
            {
                Location = new Point(0, 30),
                Size = new Size(440, 40),
                BackColor = Color.White
            };
            pnlRole.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlRole.Width - 1, pnlRole.Height - 1), 6))
                using (Pen pen = new Pen(ThemeConstants.PrimaryMaroon, 2))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            cmbRole = new ComboBox
            {
                Location = new Point(10, 5),
                Size = new Size(420, 30),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbRole.Items.AddRange(new string[] { "Librarian/Admin", "Staff", "Member" });
            cmbRole.SelectedItem = currentRole;
            if (cmbRole.SelectedIndex == -1) cmbRole.SelectedIndex = 0;
            pnlRole.Controls.Add(cmbRole);

            // Role Permissions label
            Label lblPermissions = new Label
            {
                Text = "Role Permissions:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, 90),
                AutoSize = true
            };

            // Permissions list
            Panel pnlPermissions = new Panel
            {
                Location = new Point(0, 120),
                Size = new Size(440, 150),
                BackColor = Color.Transparent
            };

            string[] permissions = new string[]
            {
                "• Full system access",
                "• Manage all modules",
                "• User management",
                "• System settings",
                "• Reports & analytics"
            };

            int permY = 0;
            foreach (string perm in permissions)
            {
                Label lblPerm = new Label
                {
                    Text = perm,
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = ThemeConstants.TextDark,
                    Location = new Point(0, permY),
                    AutoSize = true
                };
                pnlPermissions.Controls.Add(lblPerm);
                permY += 25;
            }

            contentPanel.Controls.AddRange(new Control[] 
            { 
                lblSelectRole, pnlRole, 
                lblPermissions, pnlPermissions 
            });

            // Footer panel with buttons
            Panel footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.FromArgb(248, 247, 242),
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
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(200, 200, 200) },
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

            // Assign Role button
            Button btnAssign = new Button
            {
                Text = "🛡 Assign Role",
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
            btnAssign.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnAssign.Width - 1, btnAssign.Height - 1), 6))
                {
                    e.Graphics.FillPath(new SolidBrush(btnAssign.BackColor), path);
                }
                TextRenderer.DrawText(e.Graphics, btnAssign.Text, btnAssign.Font,
                    new Rectangle(0, 0, btnAssign.Width, btnAssign.Height),
                    btnAssign.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnAssign.Click += (s, e) =>
            {
                SelectedRole = cmbRole.SelectedItem?.ToString() ?? "";
                this.DialogResult = DialogResult.OK;
            };

            footerPanel.Controls.AddRange(new Control[] { btnCancel, btnAssign });

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


