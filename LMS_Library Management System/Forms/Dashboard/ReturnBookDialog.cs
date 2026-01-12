using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using LMS_Library_Management_System.Helper;
using LMS_Library_Management_System.Interfaces;
using LMS_Library_Management_System.Service;

namespace LMS_Library_Management_System.Forms.Dashboard
{
    public class ReturnBookDialog : Form
    {
        public int SelectedBorrowingId { get; private set; }

        private Panel pnlTransactions;
        private List<BorrowingTransaction> _activeBorrowings;
        private BorrowingTransaction _selectedTransaction;
        private CirculationService _circulationService;

        public ReturnBookDialog()
        {
            this.Text = "Return Book";
            this.Size = new Size(520, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(248, 247, 242); // Beige background
            this.Padding = new Padding(0);

            _circulationService = new CirculationService();
            _activeBorrowings = new List<BorrowingTransaction>();

            SetupDialog();
            LoadTransactions();
        }

        private void SetupDialog()
        {
            // Main container panel
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 247, 242), // Beige background
                Padding = new Padding(0)
            };

            // Header panel
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.FromArgb(248, 247, 242), // Beige background
                Padding = new Padding(30, 20, 30, 10)
            };

            Label lblTitle = new Label
            {
                Text = "Return Book",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Location = new Point(30, 20),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Select a transaction to process return",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(30, 52),
                AutoSize = true
            };

            // Close button
            Button btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 14F),
                ForeColor = Color.FromArgb(150, 150, 150),
                BackColor = Color.Transparent,
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

            // Content panel
            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 247, 242), // Beige background
                Padding = new Padding(30, 20, 30, 20),
                AutoScroll = true
            };

            // Transactions container
            pnlTransactions = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 247, 242), // Beige background
                AutoScroll = true
            };

            contentPanel.Controls.Add(pnlTransactions);

            // Buttons panel
            Panel buttonsPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Color.FromArgb(248, 247, 242), // Beige background
                Padding = new Padding(30, 10, 30, 15)
            };

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(128, 0, 32), // Maroon color to match other buttons
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(90, 38),
                Location = new Point(buttonsPanel.Width - 120, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            btnCancel.Paint += (s, e) =>
            {
                Button btn = s as Button;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 6))
                {
                    e.Graphics.FillPath(new SolidBrush(btn.BackColor), path);
                }
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(btn.Text, btn.Font, new SolidBrush(btn.ForeColor), new RectangleF(0, 0, btn.Width, btn.Height), sf);
            };
            btnCancel.MouseEnter += (s, e) => btnCancel.BackColor = Color.FromArgb(100, 0, 25); // Darker maroon on hover
            btnCancel.MouseLeave += (s, e) => btnCancel.BackColor = Color.FromArgb(128, 0, 32); // Maroon color

            buttonsPanel.Controls.Add(btnCancel);

            // Add panels to main panel
            mainPanel.Controls.AddRange(new Control[] { headerPanel, contentPanel, buttonsPanel });

            // Add main panel to form
            this.Controls.Add(mainPanel);

            // Add rounded corners to form
            this.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, this.Width - 1, this.Height - 1), 10))
                {
                    this.Region = new Region(path);
                    using (Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };
        }

        private void LoadTransactions()
        {
            try
            {
                _activeBorrowings = _circulationService.GetActiveBorrowings();
                DisplayTransactions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading transactions: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplayTransactions()
        {
            pnlTransactions.Controls.Clear();

            if (_activeBorrowings.Count == 0)
            {
                Label lblNoTransactions = new Label
                {
                    Text = "No active borrowings found.",
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = Color.FromArgb(120, 120, 120),
                    Location = new Point(0, 120), // Adjusted for yPos = 100
                    AutoSize = true
                };
                pnlTransactions.Controls.Add(lblNoTransactions);
                return;
            }

            int yPos = 100; // Added more top margin for center items
            int cardHeight = 80;
            int spacing = 12;

            foreach (var transaction in _activeBorrowings)
            {
                Panel transactionCard = CreateTransactionCard(transaction, yPos);
                pnlTransactions.Controls.Add(transactionCard);
                yPos += cardHeight + spacing;
            }

            pnlTransactions.Height = yPos;
        }

        private Panel CreateTransactionCard(BorrowingTransaction transaction, int yPos)
        {
            bool isOverdue = transaction.IsOverdue;
            Color cardBackColor = Color.White;
            Color borderColor = isOverdue ? Color.FromArgb(255, 200, 200) : Color.FromArgb(200, 220, 255);
            Color iconColor = isOverdue ? Color.FromArgb(220, 53, 69) : Color.FromArgb(59, 130, 246);
            string statusText = isOverdue ? "Overdue" : "Active";
            Color statusBgColor = isOverdue ? Color.FromArgb(255, 230, 230) : Color.FromArgb(220, 240, 255);
            Color statusTextColor = isOverdue ? Color.FromArgb(200, 50, 50) : Color.FromArgb(50, 120, 200);

            Panel card = new Panel
            {
                Location = new Point(0, yPos),
                Size = new Size(430, 80),
                BackColor = cardBackColor,
                Tag = transaction
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8))
                {
                    e.Graphics.FillPath(new SolidBrush(cardBackColor), path);
                    using (Pen pen = new Pen(borderColor, 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            // Book icon
            Label lblBookIcon = new Label
            {
                Text = "📖",
                Font = new Font("Segoe UI Emoji", 20F),
                Location = new Point(18, 22),
                Size = new Size(40, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                ForeColor = iconColor
            };
            card.Controls.Add(lblBookIcon);

            // Book title
            Label lblBookTitle = new Label
            {
                Text = transaction.BookTitle,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Location = new Point(70, 20),
                Size = new Size(260, 25),
                AutoSize = false
            };
            card.Controls.Add(lblBookTitle);

            // Member name
            Label lblMemberName = new Label
            {
                Text = transaction.MemberName,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(70, 45),
                Size = new Size(260, 20),
                AutoSize = false
            };
            card.Controls.Add(lblMemberName);

            // Status badge
            Panel pnlStatus = new Panel
            {
                Location = new Point(card.Width - 85, 20),
                Size = new Size(70, 26),
                BackColor = statusBgColor
            };
            pnlStatus.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlStatus.Width - 1, pnlStatus.Height - 1), 6))
                {
                    e.Graphics.FillPath(new SolidBrush(pnlStatus.BackColor), path);
                }
            };

            Label lblStatus = new Label
            {
                Text = statusText,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = statusTextColor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlStatus.Controls.Add(lblStatus);
            card.Controls.Add(pnlStatus);

            // Fine amount (if overdue)
            if (isOverdue && transaction.FineAmount > 0)
            {
                Label lblFine = new Label
                {
                    Text = $"Fine: ${transaction.FineAmount:F0}",
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                    ForeColor = Color.FromArgb(200, 50, 50),
                    Location = new Point(card.Width - 85, 50),
                    Size = new Size(70, 18),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                card.Controls.Add(lblFine);
            }

            // Make card clickable
            card.Cursor = Cursors.Hand;
            card.Click += (s, e) => SelectTransaction(transaction, card);
            card.MouseEnter += (s, e) =>
            {
                card.BackColor = Color.FromArgb(250, 250, 250);
                card.Invalidate();
            };
            card.MouseLeave += (s, e) =>
            {
                card.BackColor = cardBackColor;
                card.Invalidate();
            };

            // Make all child controls clickable
            foreach (Control ctrl in card.Controls)
            {
                ctrl.Click += (s, e) => SelectTransaction(transaction, card);
                ctrl.Cursor = Cursors.Hand;
            }

            return card;
        }

        private void SelectTransaction(BorrowingTransaction transaction, Panel card)
        {
            // Deselect all cards
            foreach (Control ctrl in pnlTransactions.Controls)
            {
                if (ctrl is Panel p && p.Tag is BorrowingTransaction)
                {
                    p.BackColor = Color.White;
                    p.Invalidate();
                }
            }

            // Select this card
            card.BackColor = Color.FromArgb(240, 245, 255);
            card.Invalidate();
            _selectedTransaction = transaction;
            SelectedBorrowingId = transaction.BorrowingId;

            // Show return button or process return
            ProcessReturn();
        }

        private void ProcessReturn()
        {
            if (_selectedTransaction == null) return;

            try
            {
                decimal fineAmount;
                string errorMessage;

                bool success = _circulationService.ReturnBook(_selectedTransaction.BorrowingId, out fineAmount, out errorMessage);

                if (success)
                {
                    string message = "Book returned successfully.";
                    if (fineAmount > 0)
                    {
                        message += $"\nFine amount: ${fineAmount:F2}";
                    }

                    MessageBox.Show(message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                }
                else
                {
                    MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing return: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
    }
}


