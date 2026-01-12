using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using LMS_Library_Management_System.Helper;
using LMS_Library_Management_System.Models;
using LMS_Library_Management_System.Service;

namespace LMS_Library_Management_System.Forms.Dashboard
{
    public class CheckOutBookDialog : Form
    {
        public int SelectedMemberId { get; private set; }
        public int SelectedBookId { get; private set; }
        public DateTime DueDate { get; private set; }

        private TextBox txtMemberSearch;
        private TextBox txtBookSearch;
        private Panel pnlMemberDropdown;
        private Panel pnlBookDropdown;
        private Panel pnlMemberContainer;
        private Panel pnlBookContainer;
        private ListBox lstMembers;
        private ListBox lstBooks;
        private List<MemberData> _allMembers;
        private List<Book> _allBooks;
        private List<MemberData> _filteredMembers;
        private List<Book> _filteredBooks;
        private MemberData _selectedMember;
        private Book _selectedBook;
        private MemberService _memberService;
        private BookService _bookService;

        public CheckOutBookDialog()
        {
            this.Text = "Check Out Book";
            this.Size = new Size(520, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Beige;
            this.Padding = new Padding(0);

            _memberService = new MemberService();
            _bookService = new BookService();
            _allMembers = new List<MemberData>();
            _allBooks = new List<Book>();
            _filteredMembers = new List<MemberData>();
            _filteredBooks = new List<Book>();

            SetupDialog();
            LoadData();
        }

        private void SetupDialog()
        {
            // Main container panel
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Beige,
                Padding = new Padding(0)
            };

            // Header panel
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.Beige,
                Padding = new Padding(30, 20, 30, 10)
            };

            Label lblTitle = new Label
            {
                Text = "Check Out Book",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Location = new Point(30, 20),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Issue a book to a library member",
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
                BackColor = Color.Beige,
                Padding = new Padding(30, 15, 30, 20),
                AutoScroll = true
            };

            int yPos = 0;
            int inputWidth = 458;

            // Select Member section
            Label lblMember = new Label
            {
                Text = "Select Member",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(0, yPos),
                Size = new Size(inputWidth, 22),
                AutoSize = false
            };

            yPos += 28;

            pnlMemberContainer = new Panel
            {
                Location = new Point(0, yPos),
                Size = new Size(inputWidth, 44),
                BackColor = Color.White,
                Tag = false // Track focus state
            };
            pnlMemberContainer.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool isFocused = (bool)pnlMemberContainer.Tag;
                Color borderColor = isFocused ? Color.FromArgb(128, 0, 32) : Color.FromArgb(200, 200, 200);
                int borderWidth = isFocused ? 2 : 1;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlMemberContainer.Width - 1, pnlMemberContainer.Height - 1), 6))
                using (Pen pen = new Pen(borderColor, borderWidth))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            txtMemberSearch = new TextBox
            {
                Location = new Point(12, 12),
                Size = new Size(inputWidth - 50, 20),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            txtMemberSearch.SetPlaceholder("Search and select member...");
            txtMemberSearch.TextChanged += TxtMemberSearch_TextChanged;
            txtMemberSearch.Enter += (s, e) => 
            { 
                pnlMemberContainer.Tag = true;
                pnlMemberContainer.Invalidate();
                ShowMemberDropdown(); 
            };
            txtMemberSearch.Leave += (s, e) => 
            { 
                pnlMemberContainer.Tag = false;
                pnlMemberContainer.Invalidate();
                System.Threading.Thread.Sleep(200); 
                HideMemberDropdown(); 
            };

            Label lblMemberDropdown = new Label
            {
                Text = "▼",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(inputWidth - 35, 13),
                Size = new Size(25, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            lblMemberDropdown.Click += (s, e) => { txtMemberSearch.Focus(); ShowMemberDropdown(); };

            pnlMemberContainer.Controls.AddRange(new Control[] { txtMemberSearch, lblMemberDropdown });

            // Member dropdown
            pnlMemberDropdown = new Panel
            {
                Location = new Point(0, yPos + 48),
                Size = new Size(inputWidth, 0),
                BackColor = Color.White,
                Visible = false,
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlMemberDropdown.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                
                // Draw subtle shadow
                using (GraphicsPath shadowPath = CreateRoundedRectangle(new Rectangle(2, 2, pnlMemberDropdown.Width - 3, pnlMemberDropdown.Height - 3), 6))
                {
                    using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                    {
                        e.Graphics.FillPath(shadowBrush, shadowPath);
                    }
                }
                
                // Draw main border
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlMemberDropdown.Width - 1, pnlMemberDropdown.Height - 1), 6))
                {
                    e.Graphics.FillPath(Brushes.White, path);
                    using (Pen pen = new Pen(Color.FromArgb(200, 200, 200), 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            lstMembers = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5F),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60),
                ItemHeight = 22
            };
            lstMembers.SelectedIndexChanged += LstMembers_SelectedIndexChanged;
            lstMembers.MouseDown += (s, e) =>
            {
                int index = lstMembers.IndexFromPoint(e.Location);
                if (index >= 0)
                {
                    lstMembers.SelectedIndex = index;
                }
            };
            pnlMemberDropdown.Controls.Add(lstMembers);

            yPos += 60;

            // Select Book section
            Label lblBook = new Label
            {
                Text = "Select Book",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(0, yPos),
                Size = new Size(inputWidth, 22),
                AutoSize = false
            };

            yPos += 28;

            pnlBookContainer = new Panel
            {
                Location = new Point(0, yPos),
                Size = new Size(inputWidth, 44),
                BackColor = Color.White,
                Tag = false // Track focus state
            };
            pnlBookContainer.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool isFocused = (bool)pnlBookContainer.Tag;
                Color borderColor = isFocused ? Color.FromArgb(128, 0, 32) : Color.FromArgb(200, 200, 200);
                int borderWidth = isFocused ? 2 : 1;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlBookContainer.Width - 1, pnlBookContainer.Height - 1), 6))
                using (Pen pen = new Pen(borderColor, borderWidth))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            txtBookSearch = new TextBox
            {
                Location = new Point(12, 12),
                Size = new Size(inputWidth - 50, 20),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            txtBookSearch.SetPlaceholder("Search and select book...");
            txtBookSearch.TextChanged += TxtBookSearch_TextChanged;
            txtBookSearch.Enter += (s, e) => 
            { 
                pnlBookContainer.Tag = true;
                pnlBookContainer.Invalidate();
                ShowBookDropdown(); 
            };
            txtBookSearch.Leave += (s, e) => 
            { 
                pnlBookContainer.Tag = false;
                pnlBookContainer.Invalidate();
                System.Threading.Thread.Sleep(200); 
                HideBookDropdown(); 
            };

            Label lblBookDropdown = new Label
            {
                Text = "▼",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(inputWidth - 35, 13),
                Size = new Size(25, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            lblBookDropdown.Click += (s, e) => { txtBookSearch.Focus(); ShowBookDropdown(); };

            pnlBookContainer.Controls.AddRange(new Control[] { txtBookSearch, lblBookDropdown });

            // Book dropdown
            pnlBookDropdown = new Panel
            {
                Location = new Point(0, yPos + 48),
                Size = new Size(inputWidth, 0),
                BackColor = Color.White,
                Visible = false,
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlBookDropdown.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                
                // Draw subtle shadow
                using (GraphicsPath shadowPath = CreateRoundedRectangle(new Rectangle(2, 2, pnlBookDropdown.Width - 3, pnlBookDropdown.Height - 3), 6))
                {
                    using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                    {
                        e.Graphics.FillPath(shadowBrush, shadowPath);
                    }
                }
                
                // Draw main border
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlBookDropdown.Width - 1, pnlBookDropdown.Height - 1), 6))
                {
                    e.Graphics.FillPath(Brushes.White, path);
                    using (Pen pen = new Pen(Color.FromArgb(200, 200, 200), 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            lstBooks = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5F),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60),
                ItemHeight = 22
            };
            lstBooks.SelectedIndexChanged += LstBooks_SelectedIndexChanged;
            lstBooks.MouseDown += (s, e) =>
            {
                int index = lstBooks.IndexFromPoint(e.Location);
                if (index >= 0)
                {
                    lstBooks.SelectedIndex = index;
                }
            };
            pnlBookDropdown.Controls.Add(lstBooks);

            yPos += 65;

            // Buttons panel
            Panel buttonsPanel = new Panel
            {
                Location = new Point(0, yPos),
                Size = new Size(inputWidth, 55),
                BackColor = Color.Beige,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(80, 80, 80),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(90, 38),
                Location = new Point(inputWidth - 230, 8),
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
                    using (Pen borderPen = new Pen(Color.FromArgb(220, 220, 220), 1))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }
                }
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(btn.Text, btn.Font, new SolidBrush(btn.ForeColor), new RectangleF(0, 0, btn.Width, btn.Height), sf);
            };
            btnCancel.MouseEnter += (s, e) => btnCancel.BackColor = Color.FromArgb(250, 250, 250);
            btnCancel.MouseLeave += (s, e) => btnCancel.BackColor = Color.White;

            Button btnIssue = new Button
            {
                Text = "  📖  Issue Book",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(128, 0, 32), // Maroon/red color to match theme
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(130, 38),
                Location = new Point(inputWidth - 130, 8),
                Cursor = Cursors.Hand
            };
            btnIssue.Click += BtnIssue_Click;
            btnIssue.Paint += (s, e) =>
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
            btnIssue.MouseEnter += (s, e) => btnIssue.BackColor = Color.FromArgb(100, 0, 25);
            btnIssue.MouseLeave += (s, e) => btnIssue.BackColor = Color.FromArgb(128, 0, 32);

            buttonsPanel.Controls.AddRange(new Control[] { btnCancel, btnIssue });

            // Add all controls to content panel in proper order
            contentPanel.Controls.Add(lblMember);
            contentPanel.Controls.Add(pnlMemberContainer);
            contentPanel.Controls.Add(pnlMemberDropdown);
            contentPanel.Controls.Add(lblBook);
            contentPanel.Controls.Add(pnlBookContainer);
            contentPanel.Controls.Add(pnlBookDropdown);
            contentPanel.Controls.Add(buttonsPanel);
            
            // Set z-order to ensure dropdowns appear on top
            pnlMemberDropdown.BringToFront();
            pnlBookDropdown.BringToFront();

            // Add panels to main panel
            mainPanel.Controls.AddRange(new Control[] { headerPanel, contentPanel });

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

            // Focus on member search
            this.Shown += (s, e) => txtMemberSearch.Focus();
        }

        private void LoadData()
        {
            try
            {
                // Load all active members directly from database (Status = 1 means active)
                // This ensures we only show members that exist in the database
                var allMembersFromDb = _memberService.GetAllMembers();
                if (allMembersFromDb == null || allMembersFromDb.Count == 0)
                {
                    _allMembers = new List<MemberData>();
                    _filteredMembers = new List<MemberData>();
                    UpdateMemberList(_filteredMembers);
                    System.Diagnostics.Debug.WriteLine("No members found in database");
                }
                else
                {
                    _allMembers = allMembersFromDb.Where(m => m.Status == 1).ToList();
                    _filteredMembers = new List<MemberData>(_allMembers);
                    UpdateMemberList(_filteredMembers);
                    System.Diagnostics.Debug.WriteLine($"Loaded {_allMembers.Count} active members from database");
                }
                
                // Load only available books directly from database (AvailableCopies > 0)
                // This ensures we only show books that are available in the database
                var allBooksFromDb = _bookService.GetAllBooks();
                if (allBooksFromDb == null || allBooksFromDb.Count == 0)
                {
                    _allBooks = new List<Book>();
                    _filteredBooks = new List<Book>();
                    UpdateBookList(_filteredBooks);
                    System.Diagnostics.Debug.WriteLine("No books found in database");
                }
                else
                {
                    _allBooks = allBooksFromDb.Where(b => b.AvailableCopies > 0).ToList();
                    _filteredBooks = new List<Book>(_allBooks);
                    UpdateBookList(_filteredBooks);
                    System.Diagnostics.Debug.WriteLine($"Loaded {_allBooks.Count} available books from database");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data from database: {ex.Message}\n\nPlease check your database connection.", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"Error in LoadData: {ex.Message}\n{ex.StackTrace}");
                
                // Initialize empty lists on error
                _allMembers = new List<MemberData>();
                _allBooks = new List<Book>();
                _filteredMembers = new List<MemberData>();
                _filteredBooks = new List<Book>();
            }
        }

        private void TxtMemberSearch_TextChanged(object sender, EventArgs e)
        {
            string searchText = txtMemberSearch.Text.ToLower();
            
            // Get actual text (remove placeholder)
            if (txtMemberSearch.Tag is PlaceholderTextHelper.PlaceholderData placeholderData)
            {
                if (searchText == placeholderData.PlaceholderText.ToLower())
                {
                    searchText = "";
                }
            }
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredMembers = new List<MemberData>(_allMembers);
                UpdateMemberList(_filteredMembers);
                return;
            }

            _filteredMembers = _allMembers.Where(m =>
                m.MemberNumber.ToLower().Contains(searchText) ||
                m.FirstName.ToLower().Contains(searchText) ||
                m.LastName.ToLower().Contains(searchText) ||
                m.Email.ToLower().Contains(searchText)
            ).ToList();

            UpdateMemberList(_filteredMembers);
        }

        private void UpdateMemberList(List<MemberData> members)
        {
            lstMembers.Items.Clear();
            if (members == null || members.Count == 0)
            {
                lstMembers.Items.Add("No active members found in database");
                return;
            }
            
            foreach (var member in members)
            {
                // Display format: FirstName LastName (MemberNumber)
                lstMembers.Items.Add($"{member.FirstName} {member.LastName} ({member.MemberNumber})");
            }
        }

        private void ShowMemberDropdown()
        {
            // Refresh data from database when dropdown is opened
            try
            {
                RefreshMemberData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing member data: {ex.Message}");
            }
            
            if (lstMembers.Items.Count > 0)
            {
                int dropdownHeight = Math.Min(150, lstMembers.Items.Count * 22 + 10);
                pnlMemberDropdown.Height = dropdownHeight;
                pnlMemberDropdown.Location = new Point(0, pnlMemberContainer.Bottom + 4);
                pnlMemberDropdown.Visible = true;
                pnlMemberDropdown.BringToFront();
            }
            else
            {
                pnlMemberDropdown.Height = 40;
                pnlMemberDropdown.Location = new Point(0, pnlMemberContainer.Bottom + 4);
                pnlMemberDropdown.Visible = true;
                pnlMemberDropdown.BringToFront();
            }
        }
        
        private void RefreshMemberData()
        {
            // Reload members directly from database to ensure fresh data
            // This ensures dropdown always shows current members from database
            try
            {
                var allMembersFromDb = _memberService.GetAllMembers();
                if (allMembersFromDb != null && allMembersFromDb.Count > 0)
                {
                    _allMembers = allMembersFromDb.Where(m => m.Status == 1).ToList();
                }
                else
                {
                    _allMembers = new List<MemberData>();
                }
            
            // Update filtered list if search is active
            string searchText = txtMemberSearch.Text?.ToLower() ?? "";
            if (txtMemberSearch.Tag is PlaceholderTextHelper.PlaceholderData placeholderData)
            {
                if (searchText == placeholderData.PlaceholderText.ToLower())
                {
                    searchText = "";
                }
            }
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredMembers = new List<MemberData>(_allMembers);
            }
            else
            {
                _filteredMembers = _allMembers.Where(m =>
                    m.MemberNumber.ToLower().Contains(searchText) ||
                    m.FirstName.ToLower().Contains(searchText) ||
                    m.LastName.ToLower().Contains(searchText) ||
                    m.Email.ToLower().Contains(searchText)
                ).ToList();
            }
            
            UpdateMemberList(_filteredMembers);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing members from database: {ex.Message}");
                // Keep existing data if refresh fails
            }
        }

        private void HideMemberDropdown()
        {
            pnlMemberDropdown.Visible = false;
        }

        private void LstMembers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstMembers.SelectedIndex >= 0 && lstMembers.SelectedIndex < _filteredMembers.Count)
            {
                _selectedMember = _filteredMembers[lstMembers.SelectedIndex];
                txtMemberSearch.Text = $"{_selectedMember.FirstName} {_selectedMember.LastName} ({_selectedMember.MemberNumber})";
                txtMemberSearch.ForeColor = Color.Black;
                HideMemberDropdown();
            }
        }

        private void TxtBookSearch_TextChanged(object sender, EventArgs e)
        {
            string searchText = txtBookSearch.Text.ToLower();
            
            // Get actual text (remove placeholder)
            if (txtBookSearch.Tag is PlaceholderTextHelper.PlaceholderData placeholderData)
            {
                if (searchText == placeholderData.PlaceholderText.ToLower())
                {
                    searchText = "";
                }
            }
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredBooks = new List<Book>(_allBooks);
                UpdateBookList(_filteredBooks);
                return;
            }

            _filteredBooks = _allBooks.Where(b =>
                b.Title.ToLower().Contains(searchText) ||
                b.Author.ToLower().Contains(searchText) ||
                (b.ISBN != null && b.ISBN.ToLower().Contains(searchText))
            ).ToList();

            UpdateBookList(_filteredBooks);
        }

        private void UpdateBookList(List<Book> books)
        {
            lstBooks.Items.Clear();
            if (books == null || books.Count == 0)
            {
                lstBooks.Items.Add("No available books found in database");
                return;
            }
            
            foreach (var book in books)
            {
                // Display format: Title by Author (Available: X)
                lstBooks.Items.Add($"{book.Title} by {book.Author} (Available: {book.AvailableCopies})");
            }
        }

        private void ShowBookDropdown()
        {
            // Refresh data from database when dropdown is opened
            try
            {
                RefreshBookData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing book data: {ex.Message}");
            }
            
            if (lstBooks.Items.Count > 0)
            {
                int dropdownHeight = Math.Min(150, lstBooks.Items.Count * 22 + 10);
                pnlBookDropdown.Height = dropdownHeight;
                pnlBookDropdown.Location = new Point(0, pnlBookContainer.Bottom + 4);
                pnlBookDropdown.Visible = true;
                pnlBookDropdown.BringToFront();
            }
            else
            {
                pnlBookDropdown.Height = 40;
                pnlBookDropdown.Location = new Point(0, pnlBookContainer.Bottom + 4);
                pnlBookDropdown.Visible = true;
                pnlBookDropdown.BringToFront();
            }
        }
        
        private void RefreshBookData()
        {
            // Reload books directly from database to ensure fresh data (only available books)
            // This ensures dropdown always shows current available books from database
            try
            {
                var allBooksFromDb = _bookService.GetAllBooks();
                if (allBooksFromDb != null && allBooksFromDb.Count > 0)
                {
                    _allBooks = allBooksFromDb.Where(b => b.AvailableCopies > 0).ToList();
                }
                else
                {
                    _allBooks = new List<Book>();
                }
            
            // Update filtered list if search is active
            string searchText = txtBookSearch.Text?.ToLower() ?? "";
            if (txtBookSearch.Tag is PlaceholderTextHelper.PlaceholderData placeholderData)
            {
                if (searchText == placeholderData.PlaceholderText.ToLower())
                {
                    searchText = "";
                }
            }
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredBooks = new List<Book>(_allBooks);
            }
            else
            {
                _filteredBooks = _allBooks.Where(b =>
                    b.Title.ToLower().Contains(searchText) ||
                    b.Author.ToLower().Contains(searchText) ||
                    (b.ISBN != null && b.ISBN.ToLower().Contains(searchText))
                ).ToList();
            }
            
            UpdateBookList(_filteredBooks);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing books from database: {ex.Message}");
                // Keep existing data if refresh fails
            }
        }

        private void HideBookDropdown()
        {
            pnlBookDropdown.Visible = false;
        }

        private void LstBooks_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstBooks.SelectedIndex >= 0 && lstBooks.SelectedIndex < _filteredBooks.Count)
            {
                _selectedBook = _filteredBooks[lstBooks.SelectedIndex];
                txtBookSearch.Text = $"{_selectedBook.Title} by {_selectedBook.Author}";
                txtBookSearch.ForeColor = Color.Black;
                HideBookDropdown();
            }
        }

        private void BtnIssue_Click(object sender, EventArgs e)
        {
            if (_selectedMember == null)
            {
                MessageBox.Show("Please select a member.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMemberSearch.Focus();
                return;
            }

            if (_selectedBook == null)
            {
                MessageBox.Show("Please select a book.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBookSearch.Focus();
                return;
            }

            SelectedMemberId = _selectedMember.MemberId;
            SelectedBookId = _selectedBook.BookId;
            DueDate = DateTime.Now.AddDays(14); // Default 14 days

            this.DialogResult = DialogResult.OK;
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
