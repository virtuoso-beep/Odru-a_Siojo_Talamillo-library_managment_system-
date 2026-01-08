using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LMS_Library_Management_System.Helper;

namespace LMS_Library_Management_System.Forms.Dashboard
{
    public partial class DeactivateUserDialog : Form
    {
        public bool DeactivateUser { get; private set; }

        public DeactivateUserDialog(string userName)
        {
            InitializeComponent();
            this.Text = "Deactivate User";
            this.Size = new Size(500, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(248, 247, 242);
            this.Padding = new Padding(0);
            
            SetupDialog(userName);
        }

        private void SetupDialog(string userName)
        {
            // Main container panel
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 247, 242), // Light off-white
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            // Content panel
            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 247, 242),
                Padding = new Padding(30, 30, 30, 20)
            };

            // Title
            Label lblTitle = new Label
            {
                Text = "Deactivate User",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, 0),
                AutoSize = true
            };

            // Message
            Label lblMessage = new Label
            {
                Text = $"Are you sure you want to deactivate {userName}'s account? They will no longer be able to access the system.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, 40),
                Size = new Size(440, 50),
                AutoSize = false
            };

            contentPanel.Controls.AddRange(new Control[] { lblTitle, lblMessage });

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
                DeactivateUser = false;
                this.DialogResult = DialogResult.Cancel;
            };

            // Deactivate button (red)
            Button btnDeactivate = new Button
            {
                Text = "Deactivate",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(220, 53, 69), // Red color
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(120, 40),
                Location = new Point(footerPanel.Width - 130, 15),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnDeactivate.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnDeactivate.Width - 1, btnDeactivate.Height - 1), 6))
                {
                    e.Graphics.FillPath(new SolidBrush(btnDeactivate.BackColor), path);
                }
                TextRenderer.DrawText(e.Graphics, btnDeactivate.Text, btnDeactivate.Font,
                    new Rectangle(0, 0, btnDeactivate.Width, btnDeactivate.Height),
                    btnDeactivate.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnDeactivate.MouseEnter += (s, e) =>
            {
                btnDeactivate.BackColor = Color.FromArgb(200, 35, 51);
                btnDeactivate.Invalidate();
            };
            btnDeactivate.MouseLeave += (s, e) =>
            {
                btnDeactivate.BackColor = Color.FromArgb(220, 53, 69);
                btnDeactivate.Invalidate();
            };
            btnDeactivate.Click += (s, e) =>
            {
                DeactivateUser = true;
                this.DialogResult = DialogResult.OK;
            };

            footerPanel.Controls.AddRange(new Control[] { btnCancel, btnDeactivate });

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


