using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using LMS_Library_Management_System.Helper;

namespace LMS_Library_Management_System.Forms.Dashboard
{
    public class EditBookCopyDialog : Form
    {
        private int _copyId;
        private int _bookId;
        private string _bookTitle;
        private int _copyNumber;
        private bool _isStaff;
        private ComboBox _cmbStatus;
        private TextBox _txtLocation;
        private Button _btnSave;
        private Button _btnCancel;

        public bool ChangesSaved { get; private set; }

        public EditBookCopyDialog(int copyId, int bookId, string bookTitle, int copyNumber, string currentStatus, string currentLocation, bool isStaff = false)
        {
            _copyId = copyId;
            _bookId = bookId;
            _bookTitle = bookTitle;
            _copyNumber = copyNumber;
            _isStaff = isStaff;

            this.Text = "Edit Book Copy";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Beige;
            this.Padding = new Padding(0);

            SetupDialog(currentStatus, currentLocation);
        }

        private void SetupDialog(string currentStatus, string currentLocation)
        {
            // Main container
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Beige,
                Padding = new Padding(0)
            };

            // Header
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.Beige,
                Padding = new Padding(30, 20, 30, 10)
            };

            Label lblTitle = new Label
            {
                Text = "Edit Book Copy",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Location = new Point(30, 20),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Update copy status and location",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(30, 50),
                AutoSize = true
            };

            // Close button
            Button btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 14F),
                ForeColor = Color.FromArgb(150, 150, 150),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(35, 35),
                Location = new Point(headerPanel.Width - 45, 15),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.FromArgb(80, 80, 80);
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.FromArgb(150, 150, 150);

            headerPanel.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, btnClose });

            // Book Info Section
            Panel infoPanel = new Panel
            {
                Location = new Point(30, 100),
                Size = new Size(440, 100),
                BackColor = Color.FromArgb(248, 248, 248)
            };
            infoPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, infoPanel.Width - 1, infoPanel.Height - 1), 8))
                {
                    e.Graphics.FillPath(new SolidBrush(Color.FromArgb(248, 248, 248)), path);
                    e.Graphics.DrawPath(new Pen(Color.FromArgb(220, 220, 220), 1), path);
                }
            };

            // Book icon
            Panel iconPanel = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(60, 60),
                BackColor = Color.FromArgb(255, 240, 240)
            };
            iconPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, iconPanel.Width - 1, iconPanel.Height - 1), 8))
                {
                    e.Graphics.FillPath(new SolidBrush(Color.FromArgb(255, 240, 240)), path);
                }
                // Draw book icon (📖)
                TextRenderer.DrawText(e.Graphics, "📖", new Font("Segoe UI Emoji", 24F), 
                    new Rectangle(0, 0, iconPanel.Width, iconPanel.Height), 
                    Color.FromArgb(200, 20, 40), 
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            // Book title and copy number
            Label lblBookTitle = new Label
            {
                Text = _bookTitle,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Location = new Point(90, 25),
                Size = new Size(330, 25),
                AutoSize = false
            };

            Label lblCopyNumber = new Label
            {
                Text = $"Copy #{_copyNumber}",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(90, 50),
                Size = new Size(330, 20),
                AutoSize = false
            };

            infoPanel.Controls.AddRange(new Control[] { iconPanel, lblBookTitle, lblCopyNumber });

            // Status Section
            Label lblStatus = new Label
            {
                Text = "Status",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Location = new Point(30, 220),
                AutoSize = true
            };

            Panel statusContainer = new Panel
            {
                Location = new Point(30, 250),
                Size = new Size(440, 40),
                BackColor = Color.White
            };
            statusContainer.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, statusContainer.Width - 1, statusContainer.Height - 1), 6))
                {
                    e.Graphics.DrawPath(new Pen(Color.FromArgb(220, 220, 220), 1), path);
                }
            };

            _cmbStatus = new ComboBox
            {
                Location = new Point(5, 5),
                Size = new Size(430, 30),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            
            // Staff cannot mark as Lost
            if (_isStaff)
            {
                _cmbStatus.Items.AddRange(new string[] { "Available", "Borrowed", "Damaged", "For Repair" });
            }
            else
            {
                _cmbStatus.Items.AddRange(new string[] { "Available", "Borrowed", "Damaged", "Lost", "For Repair" });
            }
            
            // If current status is Lost and user is staff, default to Available
            if (_isStaff && (currentStatus == "Lost" || currentStatus?.Contains("Lost") == true))
            {
                _cmbStatus.SelectedItem = "Available";
            }
            else
            {
                _cmbStatus.SelectedItem = currentStatus ?? "Available";
            }
            
            statusContainer.Controls.Add(_cmbStatus);

            // Location Section
            Label lblLocation = new Label
            {
                Text = "Location",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Location = new Point(30, 310),
                AutoSize = true
            };

            Panel locationContainer = new Panel
            {
                Location = new Point(30, 340),
                Size = new Size(440, 40),
                BackColor = Color.White
            };
            locationContainer.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, locationContainer.Width - 1, locationContainer.Height - 1), 6))
                {
                    e.Graphics.DrawPath(new Pen(Color.FromArgb(220, 220, 220), 1), path);
                }
            };

            _txtLocation = new TextBox
            {
                Location = new Point(10, 8),
                Size = new Size(420, 24),
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                Text = currentLocation ?? ""
            };
            locationContainer.Controls.Add(_txtLocation);

            // Buttons
            Panel buttonPanel = new Panel
            {
                Location = new Point(30, 400),
                Size = new Size(440, 40),
                BackColor = Color.White
            };

            _btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(100, 100, 100),
                BackColor = Color.FromArgb(240, 240, 240),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(100, 35),
                Location = new Point(240, 0),
                Cursor = Cursors.Hand
            };
            _btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            _btnCancel.Paint += (s, e) =>
            {
                Button btn = s as Button;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 6))
                {
                    e.Graphics.FillPath(new SolidBrush(btn.BackColor), path);
                    TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), btn.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            };

            _btnSave = new Button
            {
                Text = "Save Changes",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(200, 20, 40),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(180, 35),
                Location = new Point(350, 0),
                Cursor = Cursors.Hand
            };
            _btnSave.Click += BtnSave_Click;
            _btnSave.Paint += (s, e) =>
            {
                Button btn = s as Button;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 6))
                {
                    e.Graphics.FillPath(new SolidBrush(btn.BackColor), path);
                    TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), btn.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            };

            buttonPanel.Controls.AddRange(new Control[] { _btnCancel, _btnSave });

            mainPanel.Controls.AddRange(new Control[] { headerPanel, infoPanel, lblStatus, statusContainer, lblLocation, locationContainer, buttonPanel });
            this.Controls.Add(mainPanel);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string newStatus = _cmbStatus.SelectedItem?.ToString() ?? "Available";
            string newLocation = _txtLocation.Text.Trim();

            if (string.IsNullOrWhiteSpace(newLocation))
            {
                MessageBox.Show("Please enter a location for this book copy.", "Location Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    using (var updateCmd = new MySqlCommand(
                        @"UPDATE BookCopies 
                          SET Status = @Status, 
                              Location = @Location
                          WHERE CopyId = @CopyId",
                        connection))
                    {
                        updateCmd.Parameters.AddWithValue("@CopyId", _copyId);
                        updateCmd.Parameters.AddWithValue("@Status", newStatus);
                        updateCmd.Parameters.AddWithValue("@Location", newLocation);
                        updateCmd.ExecuteNonQuery();
                    }

                    ChangesSaved = true;
                    MessageBox.Show("Book copy updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating book copy: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

