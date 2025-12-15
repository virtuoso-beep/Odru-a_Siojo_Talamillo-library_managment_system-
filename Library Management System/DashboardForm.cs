namespace Library_Management_System.Forms
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Windows.Forms;
    using Library_Management_System.Helper;
    using Library_Management_System.Models;
    using Library_Management_System.Services;
    using Library_Management_System;

    partial class DashboardForm : Form
    {
        private bool _isLoadingMembersData = false;
        private bool _isProcessingAction = false;

        public DashboardForm()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing dashboard components: {ex.Message}\n\n{ex.StackTrace}", 
                    "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
            
            try
            {
                InitializeDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting up dashboard: {ex.Message}\n\n{ex.StackTrace}", 
                    "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeDashboard()
        {
            this.WindowState = FormWindowState.Normal;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1200, 650);
            this.MinimumSize = new Size(1200, 650);
            this.MaximumSize = new Size(1200, 650);
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            
            SetFormIcon();
            
            ApplyFormRoundedCorners();
            
            this.Resize += DashboardForm_Resize;

            ResetMenuHighlights();

            LoadLogoImage();

            LoadUserInfo();

            LoadDashboardData();
            
            SetupCardStyling();
            
            SetupMenuButtonHoverEffects();
            
            SetupSidebarStyling();
            
            SetupMembersView();
        }

        private void SetupMembersView()
        {
            txtSearchMembers.Text = "🔍 Search members...";
            txtSearchMembers.ForeColor = Color.Gray;
            txtSearchMembers.Enter += TxtSearchMembers_Enter;
            txtSearchMembers.Leave += TxtSearchMembers_Leave;
            
            pnlSearchFilter.BackColor = Color.Transparent;
            pnlSearchFilter.Padding = new Padding(15, 10, 15, 10);
            pnlSearchFilter.Paint += PnlSearchFilter_Paint;
            
            SetupMemberCardIcons();
        }

        private void PnlSearchFilter_Paint(object sender, PaintEventArgs e)
        {
            if (!(sender is Panel panel)) return;

            using (Pen borderPen = new Pen(Color.FromArgb(220, 220, 220), 1))
            {
                Rectangle borderRect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                e.Graphics.DrawRectangle(borderPen, borderRect);
            }
        }

        private void SetupMemberCardIcons()
        {
        }

        private void TxtSearchMembers_Enter(object sender, EventArgs e)
        {
            if (txtSearchMembers.Text == "🔍 Search members..." || txtSearchMembers.Text == "Search members...")
            {
                txtSearchMembers.Text = "";
                txtSearchMembers.ForeColor = Color.Black;
            }
        }

        private void TxtSearchMembers_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearchMembers.Text))
            {
                txtSearchMembers.Text = "🔍 Search members...";
                txtSearchMembers.ForeColor = Color.Gray;
            }
        }

        private void SetupMenuButtonHoverEffects()
        {
            Button[] menuButtons = { btnDashboard, btnMembers, btnCatalog, btnCirculation, 
                btnReservations, btnFines, btnInventory, btnReports, btnSearch, btnSettings };

            foreach (var button in menuButtons)
            {
                button.MouseEnter += MenuButton_MouseEnter;
                button.MouseLeave += MenuButton_MouseLeave;
                button.Paint += MenuButton_Paint;
                button.Cursor = Cursors.Hand;
            }
        }

        private void MenuButton_Paint(object sender, PaintEventArgs e)
        {
            if (!(sender is Button button)) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            bool isActive = button.BackColor == Color.FromArgb(150, 0, 0);
            bool isHover = button.BackColor == Color.FromArgb(140, 0, 0);
            
            if (isActive || isHover)
            {
                using (Pen borderPen = new Pen(Color.White, 4))
                {
                    e.Graphics.DrawLine(borderPen, 0, 0, 0, button.Height);
                }
            }

            using (Pen separatorPen = new Pen(Color.FromArgb(100, 0, 0), 1))
            {
                e.Graphics.DrawLine(separatorPen, 0, button.Height - 1, button.Width, button.Height - 1);
            }
        }

        private void MenuButton_MouseEnter(object sender, EventArgs e)
        {
            if (!(sender is Button button)) return;

            if (button.BackColor != Color.FromArgb(150, 0, 0))
            {
                button.BackColor = Color.FromArgb(140, 0, 0);
                button.Invalidate();
            }
        }

        private void MenuButton_MouseLeave(object sender, EventArgs e)
        {
            if (!(sender is Button button)) return;

            if (button.BackColor != Color.FromArgb(150, 0, 0))
            {
                button.BackColor = Color.FromArgb(128, 0, 0);
                button.Invalidate();
            }
        }

        private void SetupSidebarStyling()
        {
            pnlSidebar.Paint += Sidebar_Paint;
            
            pnlAdministrator.Paint += AdministratorPanel_Paint;
        }

        private void Sidebar_Paint(object sender, PaintEventArgs e)
        {
            if (!(sender is Panel sidebar)) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int separatorY = 575;
            using (Pen separatorPen = new Pen(Color.FromArgb(100, 0, 0), 1))
            {
                e.Graphics.DrawLine(separatorPen, 20, separatorY, sidebar.Width - 20, separatorY);
            }
        }

        private void AdministratorPanel_Paint(object sender, PaintEventArgs e)
        {
            if (!(sender is Panel panel)) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (Pen borderPen = new Pen(Color.FromArgb(120, 0, 0), 1))
            {
                e.Graphics.DrawLine(borderPen, 0, 0, panel.Width, 0);
            }
        }

        private void SetupCardStyling()
        {
            pnlCardTotalBooks.Paint += Card_Paint;
            pnlCardActiveMembers.Paint += Card_Paint;
            pnlCardBooksBorrowed.Paint += Card_Paint;
            pnlCardOverdueBooks.Paint += Card_Paint;
            pnlCardTodaysBorrowings.Paint += Card_Paint;
            pnlCardTodaysReturns.Paint += Card_Paint;
            pnlCardPendingFines.Paint += Card_Paint;
            pnlCardReservations.Paint += Card_Paint;
            pnlWeeklyCirculation.Paint += Card_Paint;
            pnlCollectionCategory.Paint += Card_Paint;
            
            pnlCardTotalMembers.Paint += Card_Paint;
            pnlCardActiveMembersStat.Paint += Card_Paint;
            pnlCardSuspendedMembers.Paint += Card_Paint;
            pnlCardExpiredMembers.Paint += Card_Paint;
        }

        private void Card_Paint(object sender, PaintEventArgs e)
        {
            if (!(sender is Panel card)) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            for (int i = 0; i < 3; i++)
            {
                Rectangle shadowRect = new Rectangle(2 + i, 2 + i, card.Width - 4, card.Height - 4);
                using (GraphicsPath shadowPath = CreateRoundedRectangle(shadowRect, 12))
                {
                    int alpha = 8 - (i * 2);
                    using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0)))
                    {
                        e.Graphics.FillPath(shadowBrush, shadowPath);
                    }
                }
            }

            Rectangle cardRect = new Rectangle(0, 0, card.Width - 2, card.Height - 2);
            using (GraphicsPath path = CreateRoundedRectangle(cardRect, 12))
            {
                using (SolidBrush brush = new SolidBrush(card.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
                
                if (card.BackColor != Color.FromArgb(128, 0, 0))
                {
                    using (Pen pen = new Pen(Color.FromArgb(245, 245, 245), 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
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

        private void LoadLogoImage()
        {
            try
            {
                string logoPath = System.IO.Path.Combine(Application.StartupPath, "..", "..", "Resources", "images-removebg-preview.png");
                if (System.IO.File.Exists(logoPath))
                {
                    picLogo.Image = Image.FromFile(logoPath);
                }
                else
                {
                    logoPath = System.IO.Path.Combine(Application.StartupPath, "Resources", "images-removebg-preview.png");
                    if (System.IO.File.Exists(logoPath))
                    {
                        picLogo.Image = Image.FromFile(logoPath);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load logo: {ex.Message}");
            }
        }

        private void SetFormIcon()
        {
            try
            {
                System.ComponentModel.ComponentResourceManager resources = 
                    new System.ComponentModel.ComponentResourceManager(typeof(SiginForm));
                Icon formIcon = ((Icon)(resources.GetObject("$this.Icon")));
                if (formIcon != null)
                {
                    this.Icon = formIcon;
                }
            }
            catch (Exception ex)
            {
                try
                {
                    SiginForm signInForm = Application.OpenForms.Count > 0 ? Application.OpenForms[0] as SiginForm : null;
                    if (signInForm != null)
                    {
                        this.Icon = signInForm.Icon;
                    }
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load form icon: {ex.Message}");
                }
            }
        }

        private void ApplyFormRoundedCorners()
        {
            GraphicsPath path = new GraphicsPath();
            int radius = 20;
            Rectangle rect = this.ClientRectangle;
            
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseAllFigures();
            
            this.Region = new Region(path);
        }

        private void LoadUserInfo()
        {
            if (CurrentUser.IsLoggedIn)
            {
                User currentUser = CurrentUser.User;
                lblWelcome.Text = $"Welcome back, {currentUser.FirstName}. Here's what's happening today.";
                lblAdminName.Text = currentUser.FirstName;
            }
            else
            {
                lblWelcome.Text = "Welcome back, Administrator. Here's what's happening today.";
                lblAdminName.Text = "Admin";
            }

            lblDate.Text = DateTime.Now.ToString("MMM d, yyyy");
        }

        private void LoadDashboardData()
        {
            try
            {
                var dashboardService = new DashboardService();
                var stats = dashboardService.GetDashboardStatistics();

                lblTotalBooks.Text = stats.TotalBooks.ToString();
                lblTotalBooksChange.Text = dashboardService.CalculatePercentageChange(stats.TotalBooks, stats.TotalBooksLastWeek);
                AddCardIcon(pnlCardTotalBooks, "📚");

                lblActiveMembers.Text = stats.ActiveMembers.ToString();
                lblActiveMembersChange.Text = dashboardService.CalculatePercentageChange(stats.ActiveMembers, stats.ActiveMembersLastWeek);
                AddCardIcon(pnlCardActiveMembers, "👥");

                lblBooksBorrowed.Text = stats.BooksBorrowed.ToString();
                lblBooksBorrowedChange.Text = dashboardService.CalculatePercentageChange(stats.BooksBorrowed, stats.BooksBorrowedLastWeek);
                AddCardIcon(pnlCardBooksBorrowed, "⇄");

                lblOverdueBooks.Text = stats.OverdueBooks.ToString();
                lblOverdueBooksChange.Text = dashboardService.CalculatePercentageChange(stats.OverdueBooks, stats.OverdueBooksLastWeek);
                AddCardIcon(pnlCardOverdueBooks, "⚠");

                lblTodaysBorrowings.Text = stats.TodaysBorrowings.ToString();
                AddCardIcon(pnlCardTodaysBorrowings, "📚");

                lblTodaysReturns.Text = stats.TodaysReturns.ToString();
                AddCardIcon(pnlCardTodaysReturns, "👥");

                lblPendingFines.Text = $"₱{stats.PendingFines:F2}";
                AddCardIcon(pnlCardPendingFines, "₱");

                lblReservations.Text = stats.Reservations.ToString();
                AddCardIcon(pnlCardReservations, "⏰");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard data: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                
                lblTotalBooks.Text = "0";
                lblActiveMembers.Text = "0";
                lblBooksBorrowed.Text = "0";
                lblOverdueBooks.Text = "0";
                lblTodaysBorrowings.Text = "0";
                lblTodaysReturns.Text = "0";
                lblPendingFines.Text = "₱0.00";
                lblReservations.Text = "0";
            }
        }

        private void AddCardIcon(Panel cardPanel, string icon)
        {
            for (int i = cardPanel.Controls.Count - 1; i >= 0; i--)
            {
                if (cardPanel.Controls[i] is Label label && label.Tag?.ToString() == "CardIcon")
                {
                    cardPanel.Controls.RemoveAt(i);
                }
            }

            Label lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 22F, FontStyle.Regular),
                ForeColor = cardPanel.BackColor == Color.FromArgb(128, 0, 0) ? Color.White : Color.FromArgb(128, 0, 0),
                Location = new Point(cardPanel.Width - 55, 18),
                Size = new Size(40, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "CardIcon"
            };
            cardPanel.Controls.Add(lblIcon);
        }

        private void ResetMenuHighlights()
        {
            btnDashboard.BackColor = Color.FromArgb(128, 0, 0);
            btnMembers.BackColor = Color.FromArgb(128, 0, 0);
            btnCatalog.BackColor = Color.FromArgb(128, 0, 0);
            btnCirculation.BackColor = Color.FromArgb(128, 0, 0);
            btnReservations.BackColor = Color.FromArgb(128, 0, 0);
            btnFines.BackColor = Color.FromArgb(128, 0, 0);
            btnInventory.BackColor = Color.FromArgb(128, 0, 0);
            btnReports.BackColor = Color.FromArgb(128, 0, 0);
            btnSearch.BackColor = Color.FromArgb(128, 0, 0);
            btnSettings.BackColor = Color.FromArgb(128, 0, 0);

            btnDashboard.BackColor = Color.FromArgb(150, 0, 0);
        }

        private void DashboardForm_Resize(object sender, EventArgs e)
        {
            ApplyFormRoundedCorners();
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Are you sure you want to exit?",
                "Confirm Exit",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
            
            if (result == DialogResult.Yes)
            {
                CurrentUser.Clear();
                Application.Exit();
            }
        }

        private void BtnClose_MouseEnter(object sender, EventArgs e)
        {
            btnClose.ForeColor = Color.White;
        }

        private void BtnClose_MouseLeave(object sender, EventArgs e)
        {
            btnClose.ForeColor = Color.FromArgb(60, 60, 60);
        }

        private void BtnSignOut_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Are you sure you want to sign out?",
                "Confirm Sign Out",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                CurrentUser.Clear();
                this.Close();
            }
        }

        private void MenuItem_Click(object sender, EventArgs e)
        {
            if (!(sender is Button menuButton) || menuButton.Tag == null) return;

            ResetMenuHighlights();
            menuButton.BackColor = Color.FromArgb(150, 0, 0);

            string menuItem = menuButton.Tag.ToString();

            switch (menuItem)
            {
                case "Dashboard":
                    ShowDashboardView();
                    break;

                case "Members":
                    ShowMembersView();
                    break;

                case "Catalog":
                    ShowFeatureMessage("Book Catalog", 
                        "The Book Catalog module will allow you to:\n" +
                        "• Browse the library collection\n" +
                        "• Add new books\n" +
                        "• Edit book information\n" +
                        "• Manage book categories");
                    break;

                case "Circulation":
                    ShowFeatureMessage("Circulation Management", 
                        "The Circulation module will allow you to:\n" +
                        "• Process book borrowings\n" +
                        "• Process book returns\n" +
                        "• View borrowing history\n" +
                        "• Manage due dates");
                    break;

                case "Reservations":
                    ShowFeatureMessage("Reservations", 
                        "The Reservations module will allow you to:\n" +
                        "• View book reservations\n" +
                        "• Manage reservation requests\n" +
                        "• Process reservation confirmations");
                    break;

                case "Fines":
                    ShowFeatureMessage("Fines Management", 
                        "The Fines module will allow you to:\n" +
                        "• View pending fines\n" +
                        "• Process fine payments\n" +
                        "• Generate fine reports");
                    break;

                case "Inventory":
                    ShowFeatureMessage("Inventory Management", 
                        "The Inventory module will allow you to:\n" +
                        "• Track book inventory\n" +
                        "• Manage book copies\n" +
                        "• View stock levels");
                    break;

                case "Reports":
                    ShowFeatureMessage("Reports", 
                        "The Reports module will provide:\n" +
                        "• Borrowing reports\n" +
                        "• Member activity reports\n" +
                        "• Financial reports\n" +
                        "• Statistical analysis");
                    break;

                case "Search":
                    ShowFeatureMessage("Search", 
                        "The Search module will allow you to:\n" +
                        "• Search for books\n" +
                        "• Search for members\n" +
                        "• Advanced search options");
                    break;

                case "Settings":
                    ShowFeatureMessage("Settings", 
                        "The Settings module will allow you to:\n" +
                        "• Configure system settings\n" +
                        "• Manage user accounts\n" +
                        "• Set borrowing rules\n" +
                        "• Customize preferences");
                    break;

                default:
                    break;
            }
        }

        private void ShowFeatureMessage(string title, string message)
        {
            MessageBox.Show(message, 
                $"{title} - Coming Soon", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Information);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (CurrentUser.IsLoggedIn)
                {
                    DialogResult result = MessageBox.Show(
                        "Are you sure you want to exit?",
                        "Confirm Exit",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                    if (result == DialogResult.No)
                    {
                        e.Cancel = true;
                    }
                    else
                    {
                        CurrentUser.Clear();
                        Application.Exit();
                    }
                }
            }
        }

        private void ShowDashboardView()
        {
            pnlMembersView.Visible = false;

            ShowDashboardControls(true);
            
            LoadDashboardData();
        }

        private void ShowMembersView()
        {
            ShowDashboardControls(false);

            pnlMembersView.Visible = true;

            LoadMembersData();
        }

        private void ShowDashboardControls(bool show)
        {
            lblWelcome.Visible = show;
            lblDate.Visible = show;
            pnlCardTotalBooks.Visible = show;
            pnlCardActiveMembers.Visible = show;
            pnlCardBooksBorrowed.Visible = show;
            pnlCardOverdueBooks.Visible = show;
            pnlCardTodaysBorrowings.Visible = show;
            pnlCardTodaysReturns.Visible = show;
            pnlCardPendingFines.Visible = show;
            pnlCardReservations.Visible = show;
            pnlWeeklyCirculation.Visible = show;
            pnlCollectionCategory.Visible = show;
        }

        private void LoadMembersData()
        {
            if (_isLoadingMembersData) return;
            
            try
            {
                _isLoadingMembersData = true;
                
                var membersService = new MembersService();
                
                var stats = membersService.GetMemberStatistics();
                lblTotalMembers.Text = stats.TotalMembers.ToString();
                lblActiveMembersStat.Text = stats.ActiveMembers.ToString();
                lblSuspendedMembers.Text = stats.SuspendedMembers.ToString();
                lblExpiredMembers.Text = stats.ExpiredMembers.ToString();

                string searchText = txtSearchMembers.Text;
                if (searchText == "🔍 Search members..." || searchText == "Search members...")
                {
                    searchText = "";
                }
                string statusFilter = cmbStatusFilter.SelectedItem?.ToString() ?? "All Status";
                string typeFilter = cmbTypeFilter.SelectedItem?.ToString() ?? "All Types";

                var members = membersService.GetMembers(searchText, statusFilter, typeFilter);

                dgvMembers.Rows.Clear();
                dgvMembers.Columns.Clear();

                dgvMembers.Columns.Add("MemberId", "Member ID");
                dgvMembers.Columns.Add("Name", "Name");
                dgvMembers.Columns.Add("Type", "Type");
                dgvMembers.Columns.Add("Email", "Email");
                dgvMembers.Columns.Add("Status", "Status");
                dgvMembers.Columns.Add("Books", "Books");
                dgvMembers.Columns.Add("Fines", "Fines");
                dgvMembers.Columns.Add("Actions", "Actions");

                dgvMembers.Columns["MemberId"].Width = 120;
                dgvMembers.Columns["Name"].Width = 150;
                dgvMembers.Columns["Type"].Width = 80;
                dgvMembers.Columns["Email"].Width = 200;
                dgvMembers.Columns["Status"].Width = 100;
                dgvMembers.Columns["Books"].Width = 80;
                dgvMembers.Columns["Fines"].Width = 100;
                dgvMembers.Columns["Actions"].Width = 180;
                
                dgvMembers.Columns["Actions"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                foreach (var member in members)
                {
                    int rowIndex = dgvMembers.Rows.Add(
                        member.MemberId,
                        member.Name,
                        member.Type,
                        member.Email,
                        member.Status,
                        $"{member.BooksBorrowed}/{member.BooksLimit}",
                        $"₱{member.Fines:F2}",
                        "" 
                    );

                    if (member.Status == "Active")
                    {
                        dgvMembers.Rows[rowIndex].Cells["Status"].Style.ForeColor = Color.Green;
                    }
                    else if (member.Status == "Suspended")
                    {
                        dgvMembers.Rows[rowIndex].Cells["Status"].Style.ForeColor = Color.Red;
                    }
                    else if (member.Status == "Expired")
                    {
                        dgvMembers.Rows[rowIndex].Cells["Status"].Style.ForeColor = Color.Orange;
                    }

                    dgvMembers.Rows[rowIndex].Cells["Actions"].Value = "👁️ View  |  ✏️ Edit  |  🗑️ Delete";
                    dgvMembers.Rows[rowIndex].Cells["Actions"].Tag = member.MemberId;
                }

                dgvMembers.CellClick -= DgvMembers_CellClick;
                dgvMembers.CellClick += DgvMembers_CellClick;
                dgvMembers.CellFormatting -= DgvMembers_CellFormatting;
                dgvMembers.CellFormatting += DgvMembers_CellFormatting;

                cmbStatusFilter.SelectedIndexChanged -= CmbStatusFilter_SelectedIndexChanged;
                if (cmbStatusFilter.SelectedIndex == -1)
                {
                    cmbStatusFilter.SelectedIndex = 0;
                }
                cmbStatusFilter.SelectedIndexChanged += CmbStatusFilter_SelectedIndexChanged;
                
                cmbTypeFilter.SelectedIndexChanged -= CmbTypeFilter_SelectedIndexChanged;
                if (cmbTypeFilter.SelectedIndex == -1)
                {
                    cmbTypeFilter.SelectedIndex = 0;
                }
                cmbTypeFilter.SelectedIndexChanged += CmbTypeFilter_SelectedIndexChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading members data: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _isLoadingMembersData = false;
            }
        }

        private void TxtSearchMembers_TextChanged(object sender, EventArgs e)
        {
            if (_isLoadingMembersData) return;
            string text = txtSearchMembers.Text;
            if (text == "🔍 Search members..." || text == "Search members...") return;
            
            LoadMembersData();
        }

        private void CmbStatusFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isLoadingMembersData) return;
            
            LoadMembersData();
        }

        private void CmbTypeFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isLoadingMembersData) return;
            
            LoadMembersData();
        }

        private void BtnAddMember_Click(object sender, EventArgs e)
        {
            ShowRegisterMemberDialog();
        }

        private void DgvMembers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProcessingAction) return;
            
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            DataGridView dgv = sender as DataGridView;
            if (dgv == null) return;

            if (e.RowIndex >= dgv.Rows.Count) return;

            if (dgv.Columns[e.ColumnIndex].Name == "Actions")
            {
                string memberId = null;
                
                if (dgv.Rows[e.RowIndex].Cells["Actions"].Tag != null)
                {
                    memberId = dgv.Rows[e.RowIndex].Cells["Actions"].Tag.ToString();
                }
                else if (dgv.Columns.Contains("MemberId") && e.RowIndex < dgv.Rows.Count)
                {
                    var memberIdCell = dgv.Rows[e.RowIndex].Cells["MemberId"];
                    if (memberIdCell != null)
                    {
                        memberId = memberIdCell.Value?.ToString();
                    }
                }
                
                if (string.IsNullOrEmpty(memberId)) return;

                Rectangle cellRect = dgv.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                Point clickPoint = dgv.PointToClient(Control.MousePosition);
                
                int relativeX = clickPoint.X - cellRect.X;
                int cellWidth = cellRect.Width;

                int thirdWidth = cellWidth / 3;
                if (relativeX < thirdWidth)
                {
                    ViewMember(memberId);
                }
                else if (relativeX < thirdWidth * 2)
                {
                    EditMember(memberId);
                }
                else
                {
                    DeleteMember(memberId);
                }
            }
        }

        private void DgvMembers_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex >= 0 && e.RowIndex >= 0)
            {
                DataGridView dgv = sender as DataGridView;
                if (dgv != null && dgv.Columns[e.ColumnIndex].Name == "Actions")
                {
                    e.CellStyle.BackColor = Color.FromArgb(250, 250, 250);
                    e.CellStyle.ForeColor = Color.FromArgb(128, 0, 0);
                    e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                    e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    e.CellStyle.SelectionBackColor = Color.FromArgb(240, 240, 240);
                    e.CellStyle.SelectionForeColor = Color.FromArgb(128, 0, 0);
                }
            }
        }

        private void ViewMember(string memberId)
        {
            if (_isProcessingAction) return;
            
            try
            {
                _isProcessingAction = true;
                
                var membersService = new MembersService();
                var member = membersService.GetMemberByMemberId(memberId);
                
                if (member == null)
                {
                    MessageBox.Show("Member not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                ShowMemberPreviewDialog(member);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading member details: {ex.Message}\n\n{ex.StackTrace}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isProcessingAction = false;
            }
        }

        private void ShowMemberPreviewDialog(MembersService.MemberInfo member)
        {
            Form previewForm = new Form
            {
                Text = "Member Preview",
                Size = new Size(650, 800),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                BackColor = Color.White
            };

            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.White,
                Padding = new Padding(30, 20, 30, 15)
            };

            Label titleLabel = new Label
            {
                Text = member.Name,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                Location = new Point(30, 15),
                Size = new Size(500, 35),
                ForeColor = Color.FromArgb(40, 40, 40),
                AutoSize = false
            };

            Label memberIdLabel = new Label
            {
                Text = member.MemberId,
                Font = new Font("Segoe UI", 11F),
                Location = new Point(30, 50),
                Size = new Size(500, 25),
                ForeColor = Color.FromArgb(120, 120, 120),
                AutoSize = false
            };

            Button btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                Location = new Point(600, 15),
                Size = new Size(35, 35),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(128, 0, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => previewForm.Close();
            btnClose.MouseEnter += (s, e) => { btnClose.ForeColor = Color.FromArgb(180, 0, 0); };
            btnClose.MouseLeave += (s, e) => { btnClose.ForeColor = Color.FromArgb(128, 0, 0); };

            headerPanel.Controls.AddRange(new Control[] { titleLabel, memberIdLabel, btnClose });

            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(30, 20, 30, 30),
                BackColor = Color.White
            };

            int yPos = 0;

            Panel userInfoPanel = new Panel
            {
                Location = new Point(0, yPos),
                Size = new Size(590, 110),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            Panel avatarPanel = new Panel
            {
                Location = new Point(0, 10),
                Size = new Size(70, 70),
                BackColor = Color.FromArgb(128, 0, 0)
            };
            avatarPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                string initials = "";
                string[] nameParts = member.Name.Split(' ');
                if (nameParts.Length >= 2)
                {
                    initials = nameParts[0].Substring(0, 1).ToUpper() + nameParts[1].Substring(0, 1).ToUpper();
                }
                else if (nameParts.Length == 1 && nameParts[0].Length >= 2)
                {
                    initials = nameParts[0].Substring(0, 2).ToUpper();
                }
                else if (member.Name.Length >= 2)
                {
                    initials = member.Name.Substring(0, 2).ToUpper();
                }
                else
                {
                    initials = member.Name.ToUpper();
                }

                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(0, 0, avatarPanel.Width, avatarPanel.Height);
                    e.Graphics.FillPath(new SolidBrush(Color.FromArgb(128, 0, 0)), path);
                }

                using (Font font = new Font("Segoe UI", 22F, FontStyle.Bold))
                {
                    SizeF textSize = e.Graphics.MeasureString(initials, font);
                    PointF textPos = new PointF(
                        (avatarPanel.Width - textSize.Width) / 2,
                        (avatarPanel.Height - textSize.Height) / 2
                    );
                    e.Graphics.DrawString(initials, font, Brushes.White, textPos);
                }
            };

            Label statusTag = new Label
            {
                Text = member.Status,
                Location = new Point(85, 15),
                Size = new Size(75, 28),
                BackColor = member.Status == "Active" ? Color.FromArgb(76, 175, 80) : 
                           member.Status == "Suspended" ? Color.FromArgb(244, 67, 54) : 
                           Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(8, 0, 8, 0)
            };
            statusTag.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, statusTag.Width, statusTag.Height), 6))
                {
                    e.Graphics.FillPath(new SolidBrush(statusTag.BackColor), path);
                }
                TextRenderer.DrawText(e.Graphics, statusTag.Text, statusTag.Font, statusTag.ClientRectangle, 
                    statusTag.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            Label typeTag = new Label
            {
                Text = member.Type,
                Location = new Point(170, 15),
                Size = new Size(75, 28),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(8, 0, 8, 0)
            };
            typeTag.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, typeTag.Width, typeTag.Height), 6))
                {
                    e.Graphics.FillPath(new SolidBrush(typeTag.BackColor), path);
                }
                TextRenderer.DrawText(e.Graphics, typeTag.Text, typeTag.Font, typeTag.ClientRectangle, 
                    typeTag.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            Label emailLabel = new Label
            {
                Text = member.Email,
                Location = new Point(85, 50),
                Size = new Size(500, 25),
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = false
            };

            Label phoneLabel = new Label
            {
                Text = member.Phone ?? "Not provided",
                Location = new Point(85, 75),
                Size = new Size(500, 25),
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = false
            };

            userInfoPanel.Controls.AddRange(new Control[] { avatarPanel, statusTag, typeTag, emailLabel, phoneLabel });
            yPos += 120;

            // Membership Details Card
            Panel membershipCard = CreateInfoCard("Membership Details", yPos, 130);
            int cardY = 45;

            Label registeredLabel = new Label
            {
                Text = "Registered:",
                Location = new Point(25, cardY),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(120, 120, 120),
                AutoSize = false
            };

            Label registeredValue = new Label
            {
                Text = member.RegistrationDate?.ToString("MMM d, yyyy") ?? "N/A",
                Location = new Point(150, cardY),
                Size = new Size(410, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(40, 40, 40),
                AutoSize = false
            };
            membershipCard.Controls.AddRange(new Control[] { registeredLabel, registeredValue });
            cardY += 30;

            Label expiresLabel = new Label
            {
                Text = "Expires:",
                Location = new Point(25, cardY),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(120, 120, 120),
                AutoSize = false
            };

            Label expiresValue = new Label
            {
                Text = member.ExpirationDate?.ToString("MMM d, yyyy") ?? "N/A",
                Location = new Point(150, cardY),
                Size = new Size(410, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(40, 40, 40),
                AutoSize = false
            };
            membershipCard.Controls.AddRange(new Control[] { expiresLabel, expiresValue });
            cardY += 30;

            Label addressLabel = new Label
            {
                Text = "Address:",
                Location = new Point(25, cardY),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(120, 120, 120),
                AutoSize = false
            };

            Label addressValue = new Label
            {
                Text = member.Address ?? "Not provided",
                Location = new Point(150, cardY),
                Size = new Size(410, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(40, 40, 40),
                AutoSize = false
            };
            membershipCard.Controls.AddRange(new Control[] { addressLabel, addressValue });
            yPos += 140;

            Panel borrowingCard = CreateInfoCard("Borrowing Summary", yPos, 120);
            cardY = 45;

            Label currentBorrowedLabel = new Label
            {
                Text = "Currently Borrowed:",
                Location = new Point(25, cardY),
                Size = new Size(170, 25),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(120, 120, 120),
                AutoSize = false
            };

            Label currentBorrowedValue = new Label
            {
                Text = $"{member.BooksBorrowed} books",
                Location = new Point(200, cardY),
                Size = new Size(360, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                AutoSize = false
            };
            borrowingCard.Controls.AddRange(new Control[] { currentBorrowedLabel, currentBorrowedValue });
            cardY += 30;

            Label totalBorrowedLabel = new Label
            {
                Text = "Total Borrowed:",
                Location = new Point(25, cardY),
                Size = new Size(170, 25),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(120, 120, 120),
                AutoSize = false
            };

            Label totalBorrowedValue = new Label
            {
                Text = $"{member.TotalBorrowed} books",
                Location = new Point(200, cardY),
                Size = new Size(360, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(40, 40, 40),
                AutoSize = false
            };
            borrowingCard.Controls.AddRange(new Control[] { totalBorrowedLabel, totalBorrowedValue });
            cardY += 30;

            Label finesLabel = new Label
            {
                Text = "Unpaid Fines:",
                Location = new Point(25, cardY),
                Size = new Size(170, 25),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(120, 120, 120),
                AutoSize = false
            };

            Label finesValue = new Label
            {
                Text = $"₱{member.Fines:F2}",
                Location = new Point(200, cardY),
                Size = new Size(360, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = member.Fines > 0 ? Color.FromArgb(244, 67, 54) : Color.FromArgb(76, 175, 80),
                AutoSize = false
            };
            borrowingCard.Controls.AddRange(new Control[] { finesLabel, finesValue });
            yPos += 130;

            Panel privilegesCard = CreateInfoCard($"Privileges ({member.Type.ToLower()})", yPos, 135);
            
            int cardWidth = 590;
            int padding = 25;
            int spacing = (cardWidth - (padding * 2)) / 4;
            int startX = padding;
            int metricY = 50;
            int labelY = 95;

            Label maxBooksValue = new Label
            {
                Text = member.BooksLimit.ToString(),
                Location = new Point(startX, metricY),
                Size = new Size(spacing - 5, 40),
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };
            Label maxBooksLabel = new Label
            {
                Text = "Max Books",
                Location = new Point(startX, labelY),
                Size = new Size(spacing - 5, 25),
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(120, 120, 120),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };
            startX += spacing;

            Label daysAllowedValue = new Label
            {
                Text = "14",
                Location = new Point(startX, metricY),
                Size = new Size(spacing - 5, 40),
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };
            Label daysAllowedLabel = new Label
            {
                Text = "Days Allowed",
                Location = new Point(startX, labelY),
                Size = new Size(spacing - 5, 25),
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(120, 120, 120),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };
            startX += spacing;

            Label renewalsValue = new Label
            {
                Text = "2",
                Location = new Point(startX, metricY),
                Size = new Size(spacing - 5, 40),
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };
            Label renewalsLabel = new Label
            {
                Text = "Renewals",
                Location = new Point(startX, labelY),
                Size = new Size(spacing - 5, 25),
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(120, 120, 120),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };
            startX += spacing;

            Label finePerDayValue = new Label
            {
                Text = "₱5",
                Location = new Point(startX, metricY),
                Size = new Size(spacing - 5, 40),
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };
            Label finePerDayLabel = new Label
            {
                Text = "Fine/Day",
                Location = new Point(startX, labelY),
                Size = new Size(spacing - 5, 25),
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(120, 120, 120),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };

            privilegesCard.Controls.AddRange(new Control[] { 
                maxBooksValue, maxBooksLabel,
                daysAllowedValue, daysAllowedLabel,
                renewalsValue, renewalsLabel,
                finePerDayValue, finePerDayLabel
            });

            contentPanel.Controls.AddRange(new Control[] { userInfoPanel, membershipCard, borrowingCard, privilegesCard });

            previewForm.Controls.Add(headerPanel);
            previewForm.Controls.Add(contentPanel);

            previewForm.ShowDialog(this);
        }

        private Panel CreateInfoCard(string title, int yPosition, int height = 100)
        {
            Panel card = new Panel
            {
                Location = new Point(0, yPosition),
                Size = new Size(590, height),
                BackColor = Color.FromArgb(250, 250, 250),
                Padding = new Padding(0)
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle cardRect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using (GraphicsPath path = CreateRoundedRectangle(cardRect, 10))
                {
                    using (SolidBrush brush = new SolidBrush(card.BackColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (Pen pen = new Pen(Color.FromArgb(230, 230, 230), 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            Label titleLabel = new Label
            {
                Text = title,
                Location = new Point(25, 18),
                Size = new Size(540, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                AutoSize = false
            };
            card.Controls.Add(titleLabel);

            return card;
        }

        private void EditMember(string memberId)
        {
            if (_isProcessingAction) return;
            
            try
            {
                _isProcessingAction = true;
                
                var membersService = new MembersService();
                var member = membersService.GetMemberByMemberId(memberId);
                
                if (member == null)
                {
                    MessageBox.Show("Member not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                ShowEditMemberDialog(member);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading member details: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isProcessingAction = false;
            }
        }

        private void DeleteMember(string memberId)
        {
            if (_isProcessingAction) return;
            
            DialogResult result = MessageBox.Show(
                $"Are you sure you want to delete member {memberId}?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    _isProcessingAction = true;
                    
                    var membersService = new MembersService();
                    membersService.DeleteMember(memberId);

                    MessageBox.Show("Member deleted successfully.", 
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    LoadMembersData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting member: {ex.Message}", 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    _isProcessingAction = false;
                }
            }
        }

        private void ShowEditMemberDialog(MembersService.MemberInfo member)
        {
            string memberId = member.MemberId;
            
            string[] nameParts = member.Name.Split(new[] { ' ' }, 2);
            string firstName = nameParts.Length > 0 ? nameParts[0] : "";
            string lastName = nameParts.Length > 1 ? nameParts[1] : "";

            Form editForm = new Form
            {
                Text = "Edit Member",
                Size = new Size(520, 650),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false
            };

            Label titleLabel = new Label
            {
                Text = "Edit Member",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(440, 40),
                ForeColor = Color.FromArgb(40, 40, 40)
            };

            Label subtitleLabel = new Label
            {
                Text = "Update member information",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(20, 60),
                Size = new Size(440, 25),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            Label lblFirstName = new Label { Text = "First Name", Location = new Point(20, 110), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtFirstName = new TextBox { Location = new Point(20, 135), Size = new Size(440, 30), Font = new Font("Segoe UI", 10F), Text = firstName };

            Label lblLastName = new Label { Text = "Last Name", Location = new Point(20, 175), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtLastName = new TextBox { Location = new Point(20, 200), Size = new Size(440, 30), Font = new Font("Segoe UI", 10F), Text = lastName };

            Label lblEmail = new Label { Text = "Email", Location = new Point(20, 240), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtEmail = new TextBox 
            { 
                Location = new Point(20, 265), 
                Size = new Size(440, 30), 
                Font = new Font("Segoe UI", 10F), 
                Text = member.Email
            };

            Label lblPhone = new Label { Text = "Phone", Location = new Point(20, 305), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtPhone = new TextBox 
            { 
                Location = new Point(20, 330), 
                Size = new Size(440, 30), 
                Font = new Font("Segoe UI", 10F), 
                Text = member.Phone ?? ""
            };
            Label lblAddress = new Label { Text = "Address", Location = new Point(20, 370), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtAddress = new TextBox 
            { 
                Location = new Point(20, 395), 
                Size = new Size(440, 30), 
                Font = new Font("Segoe UI", 10F), 
                Text = member.Address ?? ""
            };

            Label lblMemberType = new Label { Text = "Member Type", Location = new Point(20, 435), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            ComboBox cmbMemberType = new ComboBox
            {
                Location = new Point(20, 460),
                Size = new Size(440, 30),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbMemberType.Items.AddRange(new[] { "Student", "Faculty", "Staff", "Guest" });
            
            if (cmbMemberType.Items.Contains(member.Type))
            {
                cmbMemberType.SelectedItem = member.Type;
            }
            else
            {
                cmbMemberType.SelectedIndex = 0;
            }

            Label lblStatus = new Label { Text = "Status", Location = new Point(20, 500), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            ComboBox cmbStatus = new ComboBox
            {
                Location = new Point(20, 525),
                Size = new Size(440, 30),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStatus.Items.AddRange(new[] { "Active", "Inactive", "Suspended", "Expired" });
            
            if (cmbStatus.Items.Contains(member.Status))
            {
                cmbStatus.SelectedItem = member.Status;
            }
            else
            {
                cmbStatus.SelectedIndex = 0;
            }

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(270, 570),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.Click += (s, e) => editForm.Close();

            Button btnSave = new Button
            {
                Text = "Save Changes",
                Location = new Point(360, 570),
                Size = new Size(110, 35),
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(128, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;

            // Apply rounded corners to Save button
            int radius = 8;
            using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnSave.Width, btnSave.Height), radius))
            {
                btnSave.Region = new Region(path);
            }

            btnSave.Paint += (s, e) =>
            {
                if (!(s is Button btn)) return;
                
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, btn.Width, btn.Height);
                
                Color backColor = btn.BackColor;
                if (btn.ClientRectangle.Contains(btn.PointToClient(Control.MousePosition)))
                {
                    if (Control.MouseButtons == MouseButtons.Left)
                    {
                        backColor = Color.FromArgb(100, 0, 0);
                    }
                    else
                    {
                        backColor = Color.FromArgb(150, 0, 0);
                    }
                }
                
                using (GraphicsPath path = CreateRoundedRectangle(rect, radius))
                {
                    using (SolidBrush brush = new SolidBrush(backColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }
                
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, rect, btn.ForeColor, 
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            btnSave.MouseEnter += (s, e) => btnSave.Invalidate();
            btnSave.MouseLeave += (s, e) => btnSave.Invalidate();
            btnSave.MouseDown += (s, e) => btnSave.Invalidate();
            btnSave.MouseUp += (s, e) => btnSave.Invalidate();

            editForm.Controls.AddRange(new Control[]
            {
                titleLabel, subtitleLabel,
                lblFirstName, txtFirstName,
                lblLastName, txtLastName,
                lblEmail, txtEmail,
                lblPhone, txtPhone,
                lblAddress, txtAddress,
                lblMemberType, cmbMemberType,
                lblStatus, cmbStatus,
                btnCancel, btnSave
            });
            
            txtEmail.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtPhone.Focus(); };
            txtPhone.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtAddress.Focus(); };
            txtAddress.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) cmbMemberType.Focus(); };
            cmbMemberType.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) cmbStatus.Focus(); };
            cmbStatus.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnSave.PerformClick(); };

            btnSave.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtFirstName.Text) ||
                    string.IsNullOrWhiteSpace(txtLastName.Text) ||
                    string.IsNullOrWhiteSpace(txtEmail.Text))
                {
                    MessageBox.Show("Please fill in all required fields.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string email = txtEmail.Text.Trim().ToLower();
                
                if (email == "firstname.lastname.idnumber.tc@umindanao.edu.ph")
                {
                    MessageBox.Show("Please enter a valid educational email address.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }
                
                if (!IsValidEducationalEmail(email))
                {
                    MessageBox.Show("Please ensure:\n\n• It must be institutional email\n• Format: firstname.lastname.idnumber.tc@umindanao.edu.ph\n\nExample: t.odruna.142275.tc@umindanao.edu.ph",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }

                try
                {
                    btnSave.Enabled = false;
                    btnSave.Text = "Saving...";

                    string memberType = cmbMemberType.SelectedItem?.ToString();
                    if (string.IsNullOrEmpty(memberType))
                    {
                        MessageBox.Show("Please select a member type.",
                            "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        btnSave.Enabled = true;
                        btnSave.Text = "Save Changes";
                        return;
                    }

                    string status = cmbStatus.SelectedItem?.ToString();
                    if (string.IsNullOrEmpty(status))
                    {
                        MessageBox.Show("Please select a status.",
                            "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        btnSave.Enabled = true;
                        btnSave.Text = "Save Changes";
                        return;
                    }

                    var membersService = new MembersService();
                    membersService.UpdateMember(
                        memberId,
                        txtFirstName.Text.Trim(),
                        txtLastName.Text.Trim(),
                        email, 
                        memberType,
                        status,
                        txtPhone.Text.Trim(),
                        txtAddress.Text.Trim()
                    );

                    MessageBox.Show("Member updated successfully!",
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    editForm.DialogResult = DialogResult.OK;
                    editForm.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating member:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                        "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    
                    btnSave.Enabled = true;
                    btnSave.Text = "Save Changes";
                }
            };

            editForm.Load += (s, e) => txtFirstName.Focus();
            editForm.FormClosed += (s, e) =>
            {
                if (editForm.DialogResult == DialogResult.OK)
                {
                    LoadMembersData();
                }
            };

            DialogResult result = editForm.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                LoadMembersData();
            }
        }

        private void ShowRegisterMemberDialog()
        {
            Form registerForm = new Form
            {
                Text = "Register New Member",
                Size = new Size(520, 600),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false
            };

            Label titleLabel = new Label
            {
                Text = "Register New Member",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(440, 40),
                ForeColor = Color.FromArgb(40, 40, 40)
            };

            Label subtitleLabel = new Label
            {
                Text = "Add a new member to the library system",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(20, 60),
                Size = new Size(440, 25),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            Label lblFirstName = new Label { Text = "First Name", Location = new Point(20, 110), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtFirstName = new TextBox { Location = new Point(20, 135), Size = new Size(440, 30), Font = new Font("Segoe UI", 10F) };

            Label lblLastName = new Label { Text = "Last Name", Location = new Point(20, 175), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtLastName = new TextBox { Location = new Point(20, 200), Size = new Size(440, 30), Font = new Font("Segoe UI", 10F) };

            Label lblEmail = new Label { Text = "Email", Location = new Point(20, 240), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtEmail = new TextBox { Location = new Point(20, 265), Size = new Size(440, 30), Font = new Font("Segoe UI", 10F) };
            
            txtEmail.Text = "firstname.lastname.IDnumber.tc@umindanao.edu.ph";
            txtEmail.ForeColor = Color.Gray;
            txtEmail.Enter += (s, e) =>
            {
                if (txtEmail.Text == "firstname.lastname.IDnumber.tc@umindanao.edu.ph")
                {
                    txtEmail.Text = "";
                    txtEmail.ForeColor = Color.Black;
                }
            };
            txtEmail.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtEmail.Text))
                {
                    txtEmail.Text = "firstname.lastname.IDnumber.tc@umindanao.edu.ph";
                    txtEmail.ForeColor = Color.Gray;
                }
            };

            Label lblPhone = new Label { Text = "Phone", Location = new Point(20, 305), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtPhone = new TextBox { Location = new Point(20, 330), Size = new Size(440, 30), Font = new Font("Segoe UI", 10F) };

            Label lblAddress = new Label { Text = "Address", Location = new Point(20, 370), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtAddress = new TextBox { Location = new Point(20, 395), Size = new Size(440, 30), Font = new Font("Segoe UI", 10F) };

            Label lblMemberType = new Label { Text = "Member Type", Location = new Point(20, 435), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            ComboBox cmbMemberType = new ComboBox
            {
                Location = new Point(20, 460),
                Size = new Size(440, 30),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbMemberType.Items.AddRange(new[] { "Student", "Faculty", "Staff", "Guest" });
            cmbMemberType.SelectedIndex = 0;

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(270, 510),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                DialogResult = DialogResult.Cancel
            };

            Button btnRegister = new Button
            {
                Text = "Register Member",
                Location = new Point(360, 510),
                Size = new Size(110, 35),
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(128, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRegister.FlatAppearance.BorderSize = 0;

            int radius = 8;
            using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnRegister.Width, btnRegister.Height), radius))
            {
                btnRegister.Region = new Region(path);
            }
            
            btnRegister.Paint += (s, e) =>
            {
                if (!(s is Button btn)) return;
                
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, btn.Width, btn.Height);
                
                Color backColor = btn.BackColor;
                if (btn.ClientRectangle.Contains(btn.PointToClient(Control.MousePosition)))
                {
                    if (Control.MouseButtons == MouseButtons.Left)
                    {
                        backColor = Color.FromArgb(100, 0, 0);
                    }
                    else
                    {
                        backColor = Color.FromArgb(150, 0, 0);
                    }
                }
                
                using (GraphicsPath path = CreateRoundedRectangle(rect, radius))
                {
                    using (SolidBrush brush = new SolidBrush(backColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }
                
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, rect, btn.ForeColor, 
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            
            txtFirstName.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtLastName.Focus(); };
            txtLastName.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtEmail.Focus(); };
            txtEmail.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtPhone.Focus(); };
            txtPhone.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtAddress.Focus(); };
            txtAddress.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) cmbMemberType.Focus(); };
            cmbMemberType.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnRegister.PerformClick(); };
            
            registerForm.AcceptButton = btnRegister;
            registerForm.CancelButton = btnCancel;
            
            btnCancel.Click += (s, e) => registerForm.Close();
            
            btnRegister.MouseEnter += (s, e) => btnRegister.Invalidate();
            btnRegister.MouseLeave += (s, e) => btnRegister.Invalidate();
            btnRegister.MouseDown += (s, e) => btnRegister.Invalidate();
            btnRegister.MouseUp += (s, e) => btnRegister.Invalidate();

            registerForm.Controls.AddRange(new Control[]
            {
                titleLabel, subtitleLabel,
                lblFirstName, txtFirstName,
                lblLastName, txtLastName,
                lblEmail, txtEmail,
                lblPhone, txtPhone,
                lblAddress, txtAddress,
                lblMemberType, cmbMemberType,
                btnCancel, btnRegister
            });

            btnRegister.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtFirstName.Text))
                {
                    MessageBox.Show("Please enter a first name.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtFirstName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtLastName.Text))
                {
                    MessageBox.Show("Please enter a last name.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtLastName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtEmail.Text))
                {
                    MessageBox.Show("Please enter an email address.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }
                string email = txtEmail.Text.Trim();
                
                if (email == "firstname.lastname.IDnumber.tc@umindanao.edu.ph" || 
                    email == "firstname.lastname.idnumber.tc@umindanao.edu.ph" ||
                    string.IsNullOrWhiteSpace(email))
                {
                    MessageBox.Show("Please ensure:\n\n• It must be institutional email\n• Format: firstname.lastname.idnumber.tc@umindanao.edu.ph\n\nExample: t.odruna.142275.tc@umindanao.edu.ph",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }
                
                email = email.ToLower();
                
                if (!IsValidEducationalEmail(email))
                {
                    MessageBox.Show("Please ensure:\n\n• It must be institutional email\n• Format: firstname.lastname.idnumber.tc@umindanao.edu.ph\n\nExample: t.odruna.142275.tc@umindanao.edu.ph",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }

                if (cmbMemberType.SelectedItem == null)
                {
                    MessageBox.Show("Please select a member type.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbMemberType.Focus();
                    return;
                }

                try
                {
                    btnRegister.Enabled = false;
                    btnRegister.Text = "Registering...";

                    var membersService = new MembersService();
                    membersService.RegisterMember(
                        txtFirstName.Text.Trim(),
                        txtLastName.Text.Trim(),
                        email, 
                        txtPhone.Text.Trim(),
                        txtAddress.Text.Trim(),
                        cmbMemberType.SelectedItem.ToString()
                    );

                    MessageBox.Show($"Member registered successfully!\n\nMember will be able to login with:\nEmail: {email}\nDefault Password: Member123!",
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    registerForm.DialogResult = DialogResult.OK;
                    registerForm.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error registering member:\n\n{ex.Message}",
                        "Registration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    
                    btnRegister.Enabled = true;
                    btnRegister.Text = "Register Member";
                }
            };

            registerForm.Load += (s, e) => txtFirstName.Focus();

            registerForm.FormClosed += (s, e) =>
            {
                if (registerForm.DialogResult == DialogResult.OK)
                {
                    LoadMembersData();
                }
            };

            if (registerForm.ShowDialog(this) == DialogResult.OK)
            {
                LoadMembersData();
            }
        }

        private bool IsValidEducationalEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            email = email.Trim().ToLower();

            if (!email.EndsWith("@umindanao.edu.ph"))
                return false;

            string[] emailParts = email.Split('@');
            if (emailParts.Length != 2)
                return false;
                
            string localPart = emailParts[0];
            string[] parts = localPart.Split('.');
            
            if (parts.Length != 4)
                return false;
            
            if (string.IsNullOrWhiteSpace(parts[0]) || !System.Text.RegularExpressions.Regex.IsMatch(parts[0], @"^[a-z]+$"))
                return false;
            
            if (string.IsNullOrWhiteSpace(parts[1]) || !System.Text.RegularExpressions.Regex.IsMatch(parts[1], @"^[a-z]+$"))
                return false;
            
            if (string.IsNullOrWhiteSpace(parts[2]) || !System.Text.RegularExpressions.Regex.IsMatch(parts[2], @"^[0-9]+$"))
                return false;
            
            if (parts[3] != "tc")
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}

