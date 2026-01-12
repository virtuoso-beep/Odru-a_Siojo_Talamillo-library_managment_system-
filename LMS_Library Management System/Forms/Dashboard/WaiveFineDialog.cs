using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using LMS_Library_Management_System.Helper;
using static LMS_Library_Management_System.Helper.PlaceholderTextHelper;

namespace LMS_Library_Management_System.Forms.Dashboard
{
    public class WaiveFineDialog : Form
    {
        private int _fineId;
        private decimal _amountToWaive;
        private string _memberName;
        private TextBox _txtReason;
        private Button _btnWaiveFine;
        private Button _btnCancel;

        public bool FineWaived { get; private set; }

        public WaiveFineDialog(int fineId, string memberName, decimal amountToWaive)
        {
            _fineId = fineId;
            _memberName = memberName;
            _amountToWaive = amountToWaive;

            this.Text = "Waive Fine";
            this.Size = new Size(520, 470);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(250, 245, 240);
            this.Padding = new Padding(0);

            SetupDialog();
        }

        private void SetupDialog()
        {
            // Main container
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(250, 245, 240),
                Padding = new Padding(0)
            };

            // Header
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(250, 245, 240),
                Padding = new Padding(30, 20, 30, 10)
            };

            Label lblTitle = new Label
            {
                Text = "Waive Fine",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Location = new Point(30, 20),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Provide a reason for waiving this fine",
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
                BackColor = Color.FromArgb(250, 245, 240),
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

            // Details Section
            Panel detailsPanel = new Panel
            {
                Location = new Point(30, 100),
                Size = new Size(460, 80),
                BackColor = Color.FromArgb(240, 235, 230)
            };
            detailsPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, detailsPanel.Width - 1, detailsPanel.Height - 1), 8))
                {
                    e.Graphics.FillPath(new SolidBrush(Color.FromArgb(240, 235, 230)), path);
                    e.Graphics.DrawPath(new Pen(Color.FromArgb(220, 220, 220), 1), path);
                }
            };

            int yPos = 20;
            int labelWidth = 120;
            int valueWidth = 280;

            // Member
            Label lblMember = new Label
            {
                Text = "Member:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Label lblMemberValue = new Label
            {
                Text = _memberName,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Location = new Point(140, yPos),
                Size = new Size(valueWidth, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            detailsPanel.Controls.AddRange(new Control[] { lblMember, lblMemberValue });
            yPos += 30;

            // Amount to Waive
            Label lblAmount = new Label
            {
                Text = "Amount to Waive:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Label lblAmountValue = new Label
            {
                Text = $"₱{_amountToWaive:N2}",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Location = new Point(140, yPos),
                Size = new Size(valueWidth, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            detailsPanel.Controls.AddRange(new Control[] { lblAmount, lblAmountValue });

            // Reason Section
            Label lblReason = new Label
            {
                Text = "Reason for Waiver *",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Location = new Point(30, 200),
                AutoSize = true
            };

            Label lblReasonDescription = new Label
            {
                Text = "Please provide a detailed reason for waiving this fine. This information will be recorded for audit purposes.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(30, 220),
                Size = new Size(460, 30),
                AutoSize = false
            };

            // Create a container panel for the text box with border
            Panel reasonContainer = new Panel
            {
                Location = new Point(28, 248),
                Size = new Size(464, 104),
                BackColor = Color.Transparent
            };
            reasonContainer.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, reasonContainer.Width - 1, reasonContainer.Height - 1), 4))
                {
                    e.Graphics.DrawPath(new Pen(Color.FromArgb(200, 20, 40), 2), path);
                }
            };

            _txtReason = new TextBox
            {
                Location = new Point(2, 2),
                Size = new Size(460, 100),
                Font = new Font("Segoe UI", 10F),
                Multiline = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(40, 40, 40),
                ScrollBars = ScrollBars.Vertical,
                AcceptsReturn = true,
                AcceptsTab = true,
                WordWrap = true
            };
            _txtReason.SetPlaceholder("Enter the reason for waiving this fine...");
            
            // Add text box to container
            reasonContainer.Controls.Add(_txtReason);

            // Buttons
            Panel buttonPanel = new Panel
            {
                Location = new Point(30, 365),
                Size = new Size(460, 40),
                BackColor = Color.FromArgb(250, 245, 240)
            };

            _btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(100, 100, 100),
                BackColor = Color.FromArgb(240, 235, 230),
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

            _btnWaiveFine = new Button
            {
                Text = "🚫 Waive Fine",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(200, 20, 40),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(180, 35),
                Location = new Point(350, 0),
                Cursor = Cursors.Hand
            };
            _btnWaiveFine.Click += BtnWaiveFine_Click;
            _btnWaiveFine.Paint += (s, e) =>
            {
                Button btn = s as Button;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 6))
                {
                    e.Graphics.FillPath(new SolidBrush(btn.BackColor), path);
                    TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), btn.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            };

            buttonPanel.Controls.AddRange(new Control[] { _btnCancel, _btnWaiveFine });

            // Add controls
            mainPanel.Controls.Add(headerPanel);
            mainPanel.Controls.Add(detailsPanel);
            mainPanel.Controls.Add(lblReason);
            mainPanel.Controls.Add(lblReasonDescription);
            mainPanel.Controls.Add(reasonContainer);
            mainPanel.Controls.Add(buttonPanel);
            
            this.Controls.Add(mainPanel);
        }

        private void BtnWaiveFine_Click(object sender, EventArgs e)
        {
            string reason = _txtReason.GetActualText().Trim();
            
            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("Please provide a reason for waiving this fine.", "Reason Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Ensure WaivedDate and WaivedReason columns exist
                    EnsureWaiverColumnsExist(connection);

                    // Get current reason to preserve it
                    string currentReason = "";
                    using (var getCmd = new MySqlCommand("SELECT Reason FROM Fines WHERE FineId = @FineId", connection))
                    {
                        getCmd.Parameters.AddWithValue("@FineId", _fineId);
                        object result = getCmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            currentReason = result.ToString();
                        }
                    }

                    // Update fine status to Waived
                    string updatedReason = string.IsNullOrEmpty(currentReason) 
                        ? $"Waived: {reason}"
                        : $"{currentReason} | Waived: {reason}";

                    using (var updateCmd = new MySqlCommand(
                        @"UPDATE Fines 
                          SET Status = 'Waived', 
                              Reason = @Reason,
                              WaivedDate = NOW(),
                              WaivedReason = @WaivedReason
                          WHERE FineId = @FineId",
                        connection))
                    {
                        updateCmd.Parameters.AddWithValue("@FineId", _fineId);
                        updateCmd.Parameters.AddWithValue("@Reason", updatedReason);
                        updateCmd.Parameters.AddWithValue("@WaivedReason", reason);
                        updateCmd.ExecuteNonQuery();
                    }

                    FineWaived = true;
                    MessageBox.Show("Fine has been waived successfully!", "Fine Waived", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error waiving fine: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EnsureWaiverColumnsExist(MySqlConnection connection)
        {
            try
            {
                // Check if WaivedDate column exists
                string checkDateQuery = @"
                    SELECT COUNT(*) 
                    FROM information_schema.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'Fines' 
                    AND COLUMN_NAME = 'WaivedDate'";
                
                using (var checkCmd = new MySqlCommand(checkDateQuery, connection))
                {
                    int columnExists = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (columnExists == 0)
                    {
                        using (var addColumnCmd = new MySqlCommand(
                            "ALTER TABLE Fines ADD COLUMN WaivedDate DATETIME NULL", 
                            connection))
                        {
                            addColumnCmd.ExecuteNonQuery();
                        }
                    }
                }

                // Check if WaivedReason column exists
                string checkReasonQuery = @"
                    SELECT COUNT(*) 
                    FROM information_schema.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'Fines' 
                    AND COLUMN_NAME = 'WaivedReason'";
                
                using (var checkCmd = new MySqlCommand(checkReasonQuery, connection))
                {
                    int columnExists = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (columnExists == 0)
                    {
                        using (var addColumnCmd = new MySqlCommand(
                            "ALTER TABLE Fines ADD COLUMN WaivedReason VARCHAR(500) NULL", 
                            connection))
                        {
                            addColumnCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If columns already exist or other error, continue
                System.Diagnostics.Debug.WriteLine($"Error ensuring waiver columns: {ex.Message}");
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

