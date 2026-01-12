using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using LMS_Library_Management_System.Helper;

namespace LMS_Library_Management_System.Forms.Dashboard
{
    public class RecordPaymentDialog : Form
    {
        private int _fineId;
        private decimal _totalFine;
        private decimal _alreadyPaid;
        private decimal _balanceDue;
        private string _memberName;
        private string _bookTitle;
        private NumericUpDown _nudPaymentAmount;
        private Button _btnProcessPayment;
        private Button _btnCancel;

        public bool PaymentProcessed { get; private set; }

        public RecordPaymentDialog(int fineId, string memberName, string bookTitle, decimal totalFine, decimal alreadyPaid)
        {
            _fineId = fineId;
            _memberName = memberName;
            _bookTitle = bookTitle;
            _totalFine = totalFine;
            _alreadyPaid = alreadyPaid;
            _balanceDue = totalFine - alreadyPaid;

            this.Text = "Record Payment";
            this.Size = new Size(520, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.White;
            this.Padding = new Padding(0);

            SetupDialog();
        }

        private void SetupDialog()
        {
            // Main container
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0)
            };

            // Header
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.White,
                Padding = new Padding(30, 20, 30, 10)
            };

            Label lblTitle = new Label
            {
                Text = "Record Payment",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Location = new Point(30, 20),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Process fine payment for member",
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

            // Payment Details Section
            Panel detailsPanel = new Panel
            {
                Location = new Point(30, 100),
                Size = new Size(460, 180),
                BackColor = Color.FromArgb(248, 248, 248)
            };
            detailsPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, detailsPanel.Width - 1, detailsPanel.Height - 1), 8))
                {
                    e.Graphics.FillPath(new SolidBrush(Color.FromArgb(248, 248, 248)), path);
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
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(40, 40, 40),
                Location = new Point(140, yPos),
                Size = new Size(valueWidth, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            detailsPanel.Controls.AddRange(new Control[] { lblMember, lblMemberValue });
            yPos += 30;

            // Book
            Label lblBook = new Label
            {
                Text = "Book:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Label lblBookValue = new Label
            {
                Text = _bookTitle,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(40, 40, 40),
                Location = new Point(140, yPos),
                Size = new Size(valueWidth, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            detailsPanel.Controls.AddRange(new Control[] { lblBook, lblBookValue });
            yPos += 30;

            // Total Fine
            Label lblTotalFine = new Label
            {
                Text = "Total Fine:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Label lblTotalFineValue = new Label
            {
                Text = $"₱{_totalFine:N2}",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(40, 40, 40),
                Location = new Point(140, yPos),
                Size = new Size(valueWidth, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            detailsPanel.Controls.AddRange(new Control[] { lblTotalFine, lblTotalFineValue });
            yPos += 30;

            // Already Paid
            Label lblAlreadyPaid = new Label
            {
                Text = "Already Paid:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Label lblAlreadyPaidValue = new Label
            {
                Text = $"₱{_alreadyPaid:N2}",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(76, 175, 80),
                Location = new Point(140, yPos),
                Size = new Size(valueWidth, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            detailsPanel.Controls.AddRange(new Control[] { lblAlreadyPaid, lblAlreadyPaidValue });
            yPos += 30;

            // Balance Due
            Label lblBalanceDue = new Label
            {
                Text = "Balance Due:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Label lblBalanceDueValue = new Label
            {
                Text = $"₱{_balanceDue:N2}",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(244, 67, 54),
                Location = new Point(140, yPos),
                Size = new Size(valueWidth, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };
            detailsPanel.Controls.AddRange(new Control[] { lblBalanceDue, lblBalanceDueValue });

            // Payment Amount Input
            Label lblPaymentAmount = new Label
            {
                Text = "Payment Amount",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Location = new Point(30, 300),
                AutoSize = true
            };

            Panel paymentInputPanel = new Panel
            {
                Location = new Point(30, 330),
                Size = new Size(460, 45),
                BackColor = Color.White
            };
            paymentInputPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, paymentInputPanel.Width - 1, paymentInputPanel.Height - 1), 6))
                {
                    e.Graphics.DrawPath(new Pen(Color.FromArgb(200, 20, 40), 2), path);
                }
            };

            Label lblPesoSign = new Label
            {
                Text = "₱",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 20, 40),
                Location = new Point(15, 12),
                Size = new Size(20, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _nudPaymentAmount = new NumericUpDown
            {
                Location = new Point(35, 10),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 12F),
                BorderStyle = BorderStyle.None,
                Minimum = 0.01m,
                Maximum = _balanceDue,
                Value = _balanceDue,
                DecimalPlaces = 2,
                Increment = 0.01m
            };

            paymentInputPanel.Controls.AddRange(new Control[] { lblPesoSign, _nudPaymentAmount });

            // Buttons
            Panel buttonPanel = new Panel
            {
                Location = new Point(30, 390),
                Size = new Size(460, 40),
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

            _btnProcessPayment = new Button
            {
                Text = "₱ Process Payment",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(200, 20, 40),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(180, 35),
                Location = new Point(350, 0),
                Cursor = Cursors.Hand
            };
            _btnProcessPayment.Click += BtnProcessPayment_Click;
            _btnProcessPayment.Paint += (s, e) =>
            {
                Button btn = s as Button;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 6))
                {
                    e.Graphics.FillPath(new SolidBrush(btn.BackColor), path);
                    TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), btn.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            };

            buttonPanel.Controls.AddRange(new Control[] { _btnCancel, _btnProcessPayment });

            mainPanel.Controls.AddRange(new Control[] { headerPanel, detailsPanel, lblPaymentAmount, paymentInputPanel, buttonPanel });
            this.Controls.Add(mainPanel);
        }

        private void BtnProcessPayment_Click(object sender, EventArgs e)
        {
            decimal paymentAmount = _nudPaymentAmount.Value;

            if (paymentAmount <= 0)
            {
                MessageBox.Show("Payment amount must be greater than zero.", "Invalid Amount", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (paymentAmount > _balanceDue)
            {
                MessageBox.Show($"Payment amount cannot exceed the balance due of ₱{_balanceDue:N2}.", "Invalid Amount", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Ensure PaidAmount column exists
                    EnsurePaidAmountColumnExists(connection);

                    // Get current paid amount
                    decimal currentPaid = 0;
                    using (var getCmd = new MySqlCommand("SELECT COALESCE(PaidAmount, 0) AS PaidAmount FROM Fines WHERE FineId = @FineId", connection))
                    {
                        getCmd.Parameters.AddWithValue("@FineId", _fineId);
                        object result = getCmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            currentPaid = Convert.ToDecimal(result);
                        }
                    }

                    decimal newPaidAmount = currentPaid + paymentAmount;
                    bool isFullPayment = newPaidAmount >= _totalFine;
                    string newStatus = isFullPayment ? "Paid" : "Unpaid";

                    // Update fine with paid amount and status
                    using (var updateCmd = new MySqlCommand(
                        @"UPDATE Fines 
                          SET PaidAmount = @PaidAmount,
                              Status = @Status, 
                              PaidDate = CASE WHEN @Status = 'Paid' THEN NOW() ELSE PaidDate END
                          WHERE FineId = @FineId",
                        connection))
                    {
                        updateCmd.Parameters.AddWithValue("@FineId", _fineId);
                        updateCmd.Parameters.AddWithValue("@PaidAmount", newPaidAmount);
                        updateCmd.Parameters.AddWithValue("@Status", newStatus);
                        updateCmd.ExecuteNonQuery();
                    }

                    PaymentProcessed = true;
                    string message = isFullPayment 
                        ? $"Full payment of ₱{paymentAmount:N2} processed successfully! Fine is now marked as Paid."
                        : $"Partial payment of ₱{paymentAmount:N2} recorded. Total paid: ₱{newPaidAmount:N2}. Remaining balance: ₱{(_totalFine - newPaidAmount):N2}";
                    MessageBox.Show(message, "Payment Processed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing payment: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EnsurePaidAmountColumnExists(MySqlConnection connection)
        {
            try
            {
                // Check if PaidAmount column exists
                string checkColumnQuery = @"
                    SELECT COUNT(*) 
                    FROM information_schema.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() 
                    AND TABLE_NAME = 'Fines' 
                    AND COLUMN_NAME = 'PaidAmount'";
                
                using (var checkCmd = new MySqlCommand(checkColumnQuery, connection))
                {
                    int columnExists = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (columnExists == 0)
                    {
                        // Add PaidAmount column
                        using (var addColumnCmd = new MySqlCommand(
                            "ALTER TABLE Fines ADD COLUMN PaidAmount DECIMAL(10,2) DEFAULT 0", 
                            connection))
                        {
                            addColumnCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If column already exists or other error, continue
                System.Diagnostics.Debug.WriteLine($"Error ensuring PaidAmount column: {ex.Message}");
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

