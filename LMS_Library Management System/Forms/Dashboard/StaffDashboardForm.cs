using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using LMS_Library_Management_System.Forms.Authentication;
using LMS_Library_Management_System.Helper;
using static LMS_Library_Management_System.Helper.PlaceholderTextHelper;

using System.Windows.Forms.DataVisualization.Charting;

namespace LMS_Library_Management_System.Forms.Dashboard
{
    public partial class StaffDashboardForm : Form
    {
        // Dynamic view panels

        private Panel pnlSearchView;
        private Panel pnlSearchContent;
        private TextBox txtSearchInput;
        private Button btnTriggerSearch;
        private Button btnSearchFilters;
        private List<Control> _originalMainContentControls = new List<Control>();
        private bool _isLoadingMembersData = false;
        private bool _isProcessingAction = false;
        public StaffDashboardForm()
        {
            InitializeComponent();
            this.Text = "Library Management System - Staff Dashboard";
            this.WindowState = FormWindowState.Maximized;
            this.Size = new Size(1400, 700);
            this.MinimumSize = new Size(1200, 600);
            InitializeDashboard();
        }

        private void InitializeDashboard()
        {
            StoreOriginalControls();
            SetupMenuButtonHoverEffects();
            SetupCardStyling();
            SetupSidebarStyling();
            ResetMenuHighlights();
            WireUpMenuButtons();
            ShowDashboardView();
        }

        private void StoreOriginalControls()
        {
            _originalMainContentControls.Clear();
            foreach (Control ctrl in pnlMainContent.Controls)
            {
                _originalMainContentControls.Add(ctrl);
            }
        }

        private void RestoreOriginalControls()
        {
            List<Control> toRemove = new List<Control>();
            foreach (Control ctrl in pnlMainContent.Controls)
            {
                if (!_originalMainContentControls.Contains(ctrl) && 

                    ctrl != pnlSearchView && 
                    ctrl != pnlMembersView)
                {
                    toRemove.Add(ctrl);
                }
            }
            foreach (Control ctrl in toRemove)
            {
                pnlMainContent.Controls.Remove(ctrl);
                ctrl.Dispose();
            }
            foreach (Control ctrl in _originalMainContentControls)
            {
                if (!pnlMainContent.Controls.Contains(ctrl))
                {
                    pnlMainContent.Controls.Add(ctrl);
                }
            }
        }

        private void ClearDynamicControls()
        {
            List<Control> toRemove = new List<Control>();
            foreach (Control ctrl in pnlMainContent.Controls)
            {
                if (!_originalMainContentControls.Contains(ctrl) && ctrl != pnlMembersView)
                {
                    toRemove.Add(ctrl);
                }
            }
            foreach (Control ctrl in toRemove)
            {
                if (ctrl is Panel panel)
                {
                    panel.Controls.Clear();
                }
                pnlMainContent.Controls.Remove(ctrl);
                ctrl.Dispose();
            }
        }

        private void ShowDashboardControls(bool show)
        {
            if (lblWelcome != null) lblWelcome.Visible = show;
            if (lblDate != null) lblDate.Visible = show;
            if (pnlCardTotalBooks != null) pnlCardTotalBooks.Visible = show;
            if (pnlCardActiveMembers != null) pnlCardActiveMembers.Visible = show;
            if (pnlCardBooksBorrowed != null) pnlCardBooksBorrowed.Visible = show;
            if (pnlCardOverdueBooks != null) pnlCardOverdueBooks.Visible = show;
            if (pnlCardTodaysBorrowings != null) pnlCardTodaysBorrowings.Visible = show;
            if (pnlCardTodaysReturns != null) pnlCardTodaysReturns.Visible = show;
            if (pnlCardPendingFines != null) pnlCardPendingFines.Visible = show;
            if (pnlWeeklyCirculation != null) pnlWeeklyCirculation.Visible = show;
            if (pnlCollectionCategory != null) pnlCollectionCategory.Visible = show;

            if (pnlSearchView != null && !pnlSearchView.IsDisposed) pnlSearchView.Visible = false;
            if (pnlMembersView != null && !pnlMembersView.IsDisposed) pnlMembersView.Visible = false;
        }

        private void WireUpMenuButtons()
        {
            btnReports.Click += (s, e) => ShowReportsView();
        }

        private void SetupMenuButtonHoverEffects()
        {
            Button[] menuButtons = { btnDashboard, btnMembers, btnCatalog, btnCirculation, 
                btnReservations, btnFines, btnInventory, btnReports, btnSearch };

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

            bool isActive = button.BackColor == ThemeConstants.AccentMaroon;
            bool isHover = button.BackColor == ThemeConstants.AccentMaroonHover;
            
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

            if (button.BackColor != ThemeConstants.AccentMaroon)
            {
                button.BackColor = ThemeConstants.AccentMaroonHover;
                button.Invalidate();
            }
        }

        private void MenuButton_MouseLeave(object sender, EventArgs e)
        {
            if (!(sender is Button button)) return;

            if (button.BackColor != ThemeConstants.AccentMaroon)
            {
                button.BackColor = ThemeConstants.SecondaryMaroon;
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

            int separatorY = 600;
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
            pnlWeeklyCirculation.Paint += Card_Paint;
            pnlCollectionCategory.Paint += Card_Paint;
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
                Color topColor, bottomColor;
                
                if (card.BackColor == ThemeConstants.PrimaryMaroon)
                {
                    topColor = ThemeConstants.PrimaryMaroonLight;
                    bottomColor = ThemeConstants.PrimaryMaroon;
                }
                else
                {
                    topColor = Color.FromArgb(Math.Min(255, card.BackColor.R + 30), Math.Min(255, card.BackColor.G + 30), Math.Min(255, card.BackColor.B + 30));
                    bottomColor = card.BackColor;
                }
                
                if (card.BackColor == ThemeConstants.PrimaryMaroon)
                {
                    using (LinearGradientBrush gradientBrush = new LinearGradientBrush(
                        cardRect,
                        ThemeConstants.PrimaryMaroonLight,
                        ThemeConstants.PrimaryMaroon,
                        LinearGradientMode.Vertical))
                    {
                        e.Graphics.FillPath(gradientBrush, path);
                    }
                }
                else
                {
                    using (LinearGradientBrush gradientBrush = new LinearGradientBrush(
                        cardRect,
                        topColor,
                        bottomColor,
                        LinearGradientMode.Vertical))
                    {
                        e.Graphics.FillPath(gradientBrush, path);
                    }
                }

                if (card.BackColor != ThemeConstants.PrimaryMaroon)
                {
                    using (Pen pen = new Pen(ThemeConstants.BorderLight, 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
                else
                {
                    using (Pen pen = new Pen(ThemeConstants.PrimaryMaroonLight, 2))
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

        private void ResetMenuHighlights()
        {
            btnDashboard.BackColor = ThemeConstants.SecondaryMaroon;
            btnMembers.BackColor = ThemeConstants.SecondaryMaroon;
            btnCatalog.BackColor = ThemeConstants.SecondaryMaroon;
            btnCirculation.BackColor = ThemeConstants.SecondaryMaroon;
            btnReservations.BackColor = ThemeConstants.SecondaryMaroon;
            btnFines.BackColor = ThemeConstants.SecondaryMaroon;
            btnInventory.BackColor = ThemeConstants.SecondaryMaroon;
            btnReports.BackColor = ThemeConstants.SecondaryMaroon;
            btnSearch.BackColor = ThemeConstants.SecondaryMaroon;
            btnDashboard.BackColor = ThemeConstants.AccentMaroon;
        }

        private void MenuItem_Click(object sender, EventArgs e)
        {
            if (!(sender is Button menuButton) || menuButton.Tag == null) return;

            ResetMenuHighlights();
            menuButton.BackColor = ThemeConstants.AccentMaroon;

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
                    ShowCatalogView();
                    break;

                case "Circulation":
                    ShowCirculationView();
                    break;

                case "Reservations":
                    ShowReservationsView();
                    break;

                case "Fines":
                    ShowFinesView();
                    break;

                case "Inventory":
                    ShowInventoryView();
                    break;

                case "Reports":
                    ShowReportsView();
                    break;

                case "Search":
                    ShowSearchView();
                    break;

                default:
                    break;
            }
        }

        private void ShowDashboardView()
        {
            RestoreOriginalControls();
            ShowDashboardControls(true);
            LoadDashboardData();
            ResetMenuHighlights();
            btnDashboard.BackColor = ThemeConstants.AccentMaroon;
            
            // Setup resize handler for dashboard cards
            pnlMainContent.Resize -= PnlMainContent_DashboardResize;
            pnlMainContent.Resize += PnlMainContent_DashboardResize;
            
            ArrangeDashboardCards();
        }

        private void PnlMainContent_DashboardResize(object sender, EventArgs e)
        {
            if (pnlCardTotalBooks != null && pnlCardTotalBooks.Visible)
            {
                ArrangeDashboardCards();
            }
        }

        private void ArrangeDashboardCards()
        {
            // Get the available width (accounting for padding)
            int availableWidth = pnlMainContent.Width - 60; // 30px padding on each side
            int gap = 20;
            
            // Calculate card sizes to fill available space better
            int cardWidth = (availableWidth - (3 * gap)) / 4; // 4 cards per row with gaps
            int cardHeight = 200; // Increased height
            int smallCardHeight = 170; // Increased height
            int chartHeight = 220; // Increased chart height
            
            // Ensure reasonable sizes - let cards grow but not too much
            if (cardWidth < 220) cardWidth = 220;
            if (cardWidth > 320) cardWidth = 320; // Max width for readability
            
            // Calculate starting X position to center the cards
            int totalCardsWidth = (4 * cardWidth) + (3 * gap);
            int startX = (pnlMainContent.Width - totalCardsWidth) / 2;
            
            // Row 1: Large cards (4 cards) - make them bigger
            int y1 = 110;
            pnlCardTotalBooks.Location = new Point(startX, y1);
            pnlCardTotalBooks.Size = new Size(cardWidth, cardHeight);
            pnlCardActiveMembers.Location = new Point(startX + cardWidth + gap, y1);
            pnlCardActiveMembers.Size = new Size(cardWidth, cardHeight);
            pnlCardBooksBorrowed.Location = new Point(startX + (cardWidth + gap) * 2, y1);
            pnlCardBooksBorrowed.Size = new Size(cardWidth, cardHeight);
            pnlCardOverdueBooks.Location = new Point(startX + (cardWidth + gap) * 3, y1);
            pnlCardOverdueBooks.Size = new Size(cardWidth, cardHeight);
            
            // Row 2: Small cards (3 cards) - make them bigger and fill width
            int y2 = y1 + cardHeight + gap;
            int smallCardWidth = (availableWidth - (2 * gap)) / 3; // 3 cards with gaps
            if (smallCardWidth < 220) smallCardWidth = 220;
            if (smallCardWidth > 360) smallCardWidth = 360;
            
            int totalSmallCardsWidth = (3 * smallCardWidth) + (2 * gap);
            int startXSmall = (pnlMainContent.Width - totalSmallCardsWidth) / 2;
            
            pnlCardTodaysBorrowings.Location = new Point(startXSmall, y2);
            pnlCardTodaysBorrowings.Size = new Size(smallCardWidth, smallCardHeight);
            pnlCardTodaysReturns.Location = new Point(startXSmall + smallCardWidth + gap, y2);
            pnlCardTodaysReturns.Size = new Size(smallCardWidth, smallCardHeight);
            pnlCardPendingFines.Location = new Point(startXSmall + (smallCardWidth + gap) * 2, y2);
            pnlCardPendingFines.Size = new Size(smallCardWidth, smallCardHeight);
            
            // Row 3: Chart panels (2 panels) - make them bigger and fill width
            int y3 = y2 + smallCardHeight + gap;
            int chartWidth = (availableWidth - gap) / 2;
            pnlWeeklyCirculation.Location = new Point(startX, y3);
            pnlWeeklyCirculation.Size = new Size(chartWidth, chartHeight);
            pnlCollectionCategory.Location = new Point(startX + chartWidth + gap, y3);
            pnlCollectionCategory.Size = new Size(chartWidth, chartHeight);
        }

        private void ShowMembersView()
        {
            RestoreOriginalControls();
            ShowDashboardControls(false);
            
            // Ensure pnlMembersView is in controls (in case it was removed)
            if (pnlMembersView != null && !pnlMainContent.Controls.Contains(pnlMembersView))
            {
                pnlMainContent.Controls.Add(pnlMembersView);
                // Ensure it fills the parent if needed, though it's likely docked or anchored in Designer
                pnlMembersView.Dock = DockStyle.Fill; 
                pnlMembersView.BringToFront();
            }
            
            if (pnlMembersView != null) pnlMembersView.Visible = true;
            SetupMembersViewStyle();
            LoadMembersData();
        }

        private void LoadMembersData()
        {
            if (_isLoadingMembersData) return;
            try
            {
                _isLoadingMembersData = true;
                
                // Initialize columns only once
                if (dgvMembers.Columns.Count == 0)
                {
                    dgvMembers.Columns.Add("MemberId", "Member ID");
                    dgvMembers.Columns.Add("Name", "Name");
                    dgvMembers.Columns.Add("Type", "Type");
                    dgvMembers.Columns.Add("Email", "Email");
                    dgvMembers.Columns.Add("Status", "Status");
                    dgvMembers.Columns.Add("Books", "Books");
                    dgvMembers.Columns.Add("Fines", "Fines");
                    dgvMembers.Columns.Add("Actions", "Actions");
                    
                    dgvMembers.Columns["MemberId"].Width = 130;
                    dgvMembers.Columns["Name"].Width = 200;
                    dgvMembers.Columns["Type"].Width = 100;
                    dgvMembers.Columns["Email"].Width = 220;
                    dgvMembers.Columns["Status"].Width = 100;
                    dgvMembers.Columns["Books"].Width = 80;
                    dgvMembers.Columns["Fines"].Width = 80;
                    dgvMembers.Columns["Actions"].Width = 100;
                    dgvMembers.RowTemplate.Height = 60; // Taller rows for multi-line text
                }
                
                string searchText = txtSearchMembers.Text;
                if (searchText == "ðŸ” Search members..." || searchText == "Search members...")
                {
                    searchText = "";
                }
                string statusFilter = cmbStatusFilter.SelectedItem?.ToString() ?? "All Status";
                string typeFilter = cmbTypeFilter.SelectedItem?.ToString() ?? "All Types";
                
                // Mock data for demonstration
                var mockMembers = new List<MemberInfo>
                {
                    new MemberInfo { MemberId = "MEM-2024-0001", Name = "John Smith", PhoneNumber = "+1 555-0101", Type = "Student", Email = "john.smith@university.edu", Status = "Active", BooksBorrowed = 3, BooksLimit = 5, Fines = 0.00m },
                    new MemberInfo { MemberId = "MEM-2024-0002", Name = "Emily Johnson", PhoneNumber = "+1 555-0102", Type = "Faculty", Email = "emily.johnson@university.edu", Status = "Active", BooksBorrowed = 7, BooksLimit = 10, Fines = 0.00m },
                    new MemberInfo { MemberId = "MEM-2024-0003", Name = "Michael Brown", PhoneNumber = "+1 555-0103", Type = "Staff", Email = "michael.brown@university.edu", Status = "Active", BooksBorrowed = 2, BooksLimit = 7, Fines = 15.00m },
                    new MemberInfo { MemberId = "MEM-2024-0004", Name = "Sarah Davis", PhoneNumber = "+1 555-0104", Type = "Guest", Email = "sarah.davis@email.com", Status = "Active", BooksBorrowed = 1, BooksLimit = 2, Fines = 0.00m },
                    new MemberInfo { MemberId = "MEM-2024-0005", Name = "David Wilson", PhoneNumber = "+1 555-0105", Type = "Student", Email = "david.wilson@university.edu", Status = "Suspended", BooksBorrowed = 0, BooksLimit = 5, Fines = 150.00m },
                    new MemberInfo { MemberId = "MEM-2024-0006", Name = "Diana Prince", PhoneNumber = "+1 555-0106", Type = "Faculty", Email = "diana.prince@email.com", Status = "Active", BooksBorrowed = 4, BooksLimit = 10, Fines = 0.00m },
                    new MemberInfo { MemberId = "MEM-2024-0007", Name = "Edward Lee", PhoneNumber = "+1 555-0107", Type = "Guest", Email = "edward.lee@email.com", Status = "Expired", BooksBorrowed = 1, BooksLimit = 3, Fines = 10.00m },
                    new MemberInfo { MemberId = "MEM-2024-0008", Name = "Fiona Chen", PhoneNumber = "+1 555-0108", Type = "Student", Email = "fiona.chen@email.com", Status = "Active", BooksBorrowed = 0, BooksLimit = 5, Fines = 0.00m }
                };
                
                // Apply filters
                IEnumerable<MemberInfo> filteredMembers = mockMembers;
                if (!string.IsNullOrEmpty(searchText))
                {
                    string searchLower = searchText.ToLower();
                    filteredMembers = filteredMembers.Where(m => 
                        m.MemberId.ToLower().StartsWith(searchLower) ||
                        m.Name.ToLower().StartsWith(searchLower) ||
                        m.Email.ToLower().StartsWith(searchLower));
                }
                if (statusFilter != "All Status")
                {
                    filteredMembers = filteredMembers.Where(m => m.Status == statusFilter);
                }
                if (typeFilter != "All Types")
                {
                    filteredMembers = filteredMembers.Where(m => m.Type == typeFilter);
                }
                
                // Calculate statistics
                var allMembers = mockMembers;
                lblTotalMembers.Text = allMembers.Count.ToString();
                
                // Statistics with labels
                lblActiveMembersStat.Text = allMembers.Count(m => m.Status == "Active").ToString();
                lblSuspendedMembers.Text = allMembers.Count(m => m.Status == "Suspended").ToString();
                lblExpiredMembers.Text = allMembers.Count(m => m.Status == "Expired").ToString();
                
                // Populate grid
                dgvMembers.Rows.Clear();
                foreach (var member in filteredMembers)
                {
                    int rowIndex = dgvMembers.Rows.Add(
                        member.MemberId,
                        member.Name, // Name column will hold Name, but painted with phone
                        member.Type,
                        member.Email,
                        member.Status,
                        $"{member.BooksBorrowed}/{member.BooksLimit}",
                        $"${member.Fines:F0}", // Format without cents as per image, or F2 if desired
                        "" // Actions column left empty for custom painting
                    );
                    
                    dgvMembers.Rows[rowIndex].Tag = member; // Store full object for painting access
                }
                
                // Wire up events
                dgvMembers.CellClick -= DgvMembers_CellClick;
                dgvMembers.CellClick += DgvMembers_CellClick;
                dgvMembers.CellFormatting -= DgvMembers_CellFormatting;
                dgvMembers.CellFormatting += DgvMembers_CellFormatting;
                dgvMembers.CellPainting -= DgvMembers_CellPainting;
                dgvMembers.CellPainting += DgvMembers_CellPainting;
                
                if (cmbStatusFilter.SelectedIndex == -1) cmbStatusFilter.SelectedIndex = 0;
                if (cmbTypeFilter.SelectedIndex == -1) cmbTypeFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading members data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _isLoadingMembersData = false;
            }
        }

        private void SetupMembersViewStyle()
        {
            // Grid Styling
            dgvMembers.BackgroundColor = Color.White;
            dgvMembers.BorderStyle = BorderStyle.None;
            dgvMembers.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvMembers.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvMembers.EnableHeadersVisualStyles = false;
            
            dgvMembers.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            dgvMembers.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(100, 100, 100);
            dgvMembers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            dgvMembers.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
            dgvMembers.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(100, 100, 100);
            dgvMembers.ColumnHeadersHeight = 40;
            
            dgvMembers.DefaultCellStyle.BackColor = Color.White;
            dgvMembers.DefaultCellStyle.ForeColor = Color.FromArgb(50, 50, 50);
            dgvMembers.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            dgvMembers.DefaultCellStyle.SelectionBackColor = Color.FromArgb(245, 245, 245); // Very light gray selection
            dgvMembers.DefaultCellStyle.SelectionForeColor = Color.FromArgb(50, 50, 50);
            dgvMembers.DefaultCellStyle.Padding = new Padding(10, 0, 0, 0); // Left padding
            dgvMembers.RowHeadersVisible = false;
            dgvMembers.GridColor = Color.FromArgb(240, 240, 240);
            dgvMembers.AllowUserToResizeRows = false;
            
            // Header
            lblMembersTitle.Font = new Font("Segoe UI", 24F, FontStyle.Bold); // Serif-like font in image? Use Segoe UI Bold for now
            lblMembersTitle.ForeColor = Color.FromArgb(30, 30, 30);
            lblMembersTitle.Text = "Members";
            
            lblMembersSubtitle.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            lblMembersSubtitle.ForeColor = Color.Gray;
            
            // Add Member Button
            btnAddMember.BackColor = Color.Maroon; // Matches Image
            btnAddMember.ForeColor = Color.White;
            btnAddMember.FlatStyle = FlatStyle.Flat;
            btnAddMember.FlatAppearance.BorderSize = 0;
            btnAddMember.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnAddMember.Text = "+ Add Member";
            
            // Remove existing handlers to prevent duplicates if called multiple times
            btnAddMember.Click -= BtnAddMember_Click;
            btnAddMember.Click += BtnAddMember_Click;
            
            // Need to set Region for rounded corners if not already capable, handled in Paint? 
            // Existing Paint handler might interfere. I'll rely on flat style for now or use region.
            
            // Stats Cards Styling
            StyleStatCard(pnlCardTotalMembers, lblTotalMembers, "Total Members", Color.Maroon, Color.White);
            StyleStatCard(pnlCardActiveMembersStat, lblActiveMembersStat, "Active", Color.White, Color.Green);
            StyleStatCard(pnlCardSuspendedMembers, lblSuspendedMembers, "Suspended", Color.White, Color.Red);
            StyleStatCard(pnlCardExpiredMembers, lblExpiredMembers, "Expired", Color.White, Color.Orange);

            // Filter Styling
            pnlSearchFilter.BackColor = Color.White; 
            // We need to ensure the panel background is white or transparent, 
            // and the search box has a rounded border.
            // Assuming standard WinForms controls, heavy styling requires painting.
        }

        private void StyleStatCard(Panel card, Label valueLabel, string subtitle, Color bgColor, Color accentColor)
        {
            card.BackColor = bgColor;
            
            // Find Icon and Subtitle labels inside the card
            // Assuming the structure is consistent with Designer
            foreach(Control c in card.Controls)
            {
                if(c is Label l)
                {
                    if(l == valueLabel)
                    {
                        l.ForeColor = (bgColor == Color.Maroon) ? Color.White : Color.Black;
                        l.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
                    }
                    else if(l.Name.Contains("Label") || l.Text == subtitle) // Subtitle
                    {
                        l.ForeColor = (bgColor == Color.Maroon) ? Color.FromArgb(200, 255, 255, 255) : Color.Gray;
                        l.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                    }
                    else if(l.Name.Contains("Icon")) // Icon
                    {
                        l.ForeColor = (bgColor == Color.Maroon) ? Color.White : accentColor;
                    }
                }
            }
        }

        private void DgvMembers_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
             if (e.RowIndex < 0) return; // Header
             
             // Custom Painting
             if (dgvMembers.Columns[e.ColumnIndex].Name == "Name")
             {
                 e.PaintBackground(e.CellBounds, true);
                 
                 if (dgvMembers.Rows[e.RowIndex].Tag is MemberInfo member)
                 {
                     // Draw Name
                     using (Brush nameBrush = new SolidBrush(Color.FromArgb(30, 30, 30)))
                     {
                         e.Graphics.DrawString(member.Name, new Font("Segoe UI", 9.5F, FontStyle.Bold), nameBrush, 
                             e.CellBounds.X + 10, e.CellBounds.Y + 12);
                     }
                     
                     // Draw Phone
                     using (Brush phoneBrush = new SolidBrush(Color.Gray))
                     {
                         e.Graphics.DrawString(member.PhoneNumber, new Font("Segoe UI", 8.5F, FontStyle.Regular), phoneBrush, 
                             e.CellBounds.X + 10, e.CellBounds.Y + 32);
                     }
                 }
                 e.Handled = true;
             }
             else if (dgvMembers.Columns[e.ColumnIndex].Name == "Type")
             {
                 e.PaintBackground(e.CellBounds, true);
                 if (e.Value != null)
                 {
                     string type = e.Value.ToString();
                     Color bgColor = Color.FromArgb(230, 240, 255); // Default Blue
                     Color textColor = Color.FromArgb(0, 100, 200);
                     
                     if(type == "Faculty") { bgColor = Color.FromArgb(250, 230, 230); textColor = Color.FromArgb(200, 50, 50); }
                     else if(type == "Staff") { bgColor = Color.FromArgb(230, 250, 230); textColor = Color.FromArgb(0, 150, 50); }
                     else if(type == "Guest") { bgColor = Color.FromArgb(255, 245, 230); textColor = Color.FromArgb(200, 120, 0); }
                     else if(type == "Student") { bgColor = Color.FromArgb(225, 245, 255); textColor = Color.FromArgb(0, 120, 215); }

                     using (SolidBrush bgBrush = new SolidBrush(bgColor))
                     using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(e.CellBounds.X + 5, e.CellBounds.Y + 15, 80, 25), 4))
                     {
                         e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                         e.Graphics.FillPath(bgBrush, path);
                     }
                     
                     TextRenderer.DrawText(e.Graphics, type, new Font("Segoe UI", 8F, FontStyle.Bold), 
                         new Rectangle(e.CellBounds.X + 5, e.CellBounds.Y + 15, 80, 25), textColor, 
                         TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                 }
                 e.Handled = true;
             }
             else if (dgvMembers.Columns[e.ColumnIndex].Name == "Status")
             {
                 e.PaintBackground(e.CellBounds, true);
                 if (e.Value != null)
                 {
                     string status = e.Value.ToString();
                     Color bgColor = Color.FromArgb(230, 250, 230);
                     Color textColor = Color.Green;
                     
                     if (status == "Suspended") { bgColor = Color.FromArgb(250, 230, 230); textColor = Color.Red; }
                     else if (status == "Expired") { bgColor = Color.FromArgb(255, 245, 230); textColor = Color.Orange; }
                     else if (status == "Active") { bgColor = Color.FromArgb(220, 255, 220); textColor = Color.FromArgb(0, 128, 0); }

                     using (SolidBrush bgBrush = new SolidBrush(bgColor))
                     using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(e.CellBounds.X + 10, e.CellBounds.Y + 15, 70, 25), 12))
                     {
                         e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                         e.Graphics.FillPath(bgBrush, path);
                     }
                     TextRenderer.DrawText(e.Graphics, status, new Font("Segoe UI", 8F, FontStyle.Bold), 
                         new Rectangle(e.CellBounds.X + 10, e.CellBounds.Y + 15, 70, 25), textColor, 
                         TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                 }
                 e.Handled = true;
             }
             else if (dgvMembers.Columns[e.ColumnIndex].Name == "Actions")
             {
                 e.PaintBackground(e.CellBounds, true);
                 // Draw Icons
                 TextRenderer.DrawText(e.Graphics, "ðŸ‘", new Font("Segoe UI Symbol", 12F), 
                     new Rectangle(e.CellBounds.X + 10, e.CellBounds.Y, 30, e.CellBounds.Height), Color.Gray, 
                     TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                     
                 TextRenderer.DrawText(e.Graphics, "âœ", new Font("Segoe UI Symbol", 12F), 
                     new Rectangle(e.CellBounds.X + 50, e.CellBounds.Y, 30, e.CellBounds.Height), Color.Gray, 
                     TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                 e.Handled = true;
             }
        }

        private void TxtSearchMembers_TextChanged(object sender, EventArgs e)
        {
            if (_isLoadingMembersData) return;
            string text = txtSearchMembers.Text;
            if (text == "ðŸ” Search members..." || text == "Search members...") return;
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

        private void ShowRegisterMemberDialog()
        {
            // Form Setup
            Form registerForm = new Form();
            registerForm.Size = new Size(550, 720);
            registerForm.FormBorderStyle = FormBorderStyle.None; // Custom chrome
            registerForm.StartPosition = FormStartPosition.CenterParent;
            registerForm.BackColor = Color.White;
            registerForm.ShowInTaskbar = false;

            // Paint Border
            registerForm.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(Color.FromArgb(200, 200, 200), 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, registerForm.Width - 1, registerForm.Height - 1);
                }
            };

            // Close Button (X)
            Label btnClose = new Label();
            btnClose.Text = "Ã—"; 
            btnClose.Font = new Font("Arial", 18, FontStyle.Regular);
            btnClose.ForeColor = Color.Gray;
            btnClose.Location = new Point(registerForm.Width - 40, 15);
            btnClose.Size = new Size(30, 30);
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += (s, e) => registerForm.Close();
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.Black;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.Gray;
            registerForm.Controls.Add(btnClose);

            // Title
            Label lblTitle = new Label();
            lblTitle.Text = "Register New Member";
            lblTitle.Font = new Font("Georgia", 18, FontStyle.Bold); 
            lblTitle.ForeColor = Color.FromArgb(40, 0, 0); // Dark Maroon
            lblTitle.Location = new Point(30, 30);
            lblTitle.AutoSize = true;
            registerForm.Controls.Add(lblTitle);

            // Subtitle
            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Add a new member to the library system";
            lblSubtitle.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            lblSubtitle.ForeColor = Color.FromArgb(120, 120, 120);
            lblSubtitle.Location = new Point(32, 65);
            lblSubtitle.AutoSize = true;
            registerForm.Controls.Add(lblSubtitle);

            // -- Controls Helpers --
            Action<string, int, int> AddLabel = (text, x, y) => {
                Label l = new Label();
                l.Text = text;
                l.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                l.ForeColor = Color.FromArgb(40, 40, 40);
                l.Location = new Point(x, y);
                l.AutoSize = true;
                registerForm.Controls.Add(l);
            };

            Func<string, int, int, int, TextBox> AddInput = (placeholder, x, y, w) => {
                Panel pnl = new Panel();
                pnl.Location = new Point(x, y + 25);
                pnl.Size = new Size(w, 42);
                pnl.BackColor = Color.White;
                pnl.Padding = new Padding(10, 10, 10, 5);
                
                TextBox tb = new TextBox();
                tb.BorderStyle = BorderStyle.None;
                tb.Font = new Font("Segoe UI", 11F, FontStyle.Regular);
                tb.Dock = DockStyle.Fill;
                tb.BackColor = Color.White;
                if(!string.IsNullOrEmpty(placeholder)) tb.SetPlaceholder(placeholder); 

                pnl.Controls.Add(tb);
                registerForm.Controls.Add(pnl);
                
                pnl.Paint += (s, e) => {
                     e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                     bool isFocused = (pnl.Tag as string == "Focused");
                     Color borderColor = isFocused ? Color.Maroon : Color.FromArgb(220, 220, 220);
                     float width = isFocused ? 2f : 1f;
                     
                     using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnl.Width - 3, pnl.Height - 3), 8))
                     using(Pen pen = new Pen(borderColor, width))
                     {
                         e.Graphics.DrawPath(pen, path);
                     }
                };

                tb.Enter += (s, e) => { pnl.Tag = "Focused"; pnl.Invalidate(); };
                tb.Leave += (s, e) => { pnl.Tag = ""; pnl.Invalidate(); };
                
                return tb;
            };

            // -- Layout Controls --
            int currentY = 110;
            int gap = 80;

            // Row 1
            AddLabel("First Name", 30, currentY);
            AddLabel("Last Name", 285, currentY);
            TextBox txtFirstName = AddInput("John", 30, currentY, 235);
            TextBox txtLastName = AddInput("Doe", 285, currentY, 235);
            
            // Set First Name as focused by default visually if needed, but Focus() below handles it.
            
            currentY += gap;

            // Row 2
            AddLabel("Email", 30, currentY);
            TextBox txtEmail = AddInput("john.doe@example.com", 30, currentY, 490);
            currentY += gap;

            // Row 3
            AddLabel("Phone", 30, currentY);
            TextBox txtPhone = AddInput("+1 555-0123", 30, currentY, 490);
            // Allow only numeric input for phone number (no letters or symbols)
            txtPhone.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
            };
            currentY += gap;

            // Row 4
            AddLabel("Address", 30, currentY);
            TextBox txtAddress = AddInput("123 Main Street", 30, currentY, 490);
            currentY += gap;

            // Row 5 - ComboBox
            AddLabel("Member Type", 30, currentY);
            Panel pnlCombo = new Panel();
            pnlCombo.Location = new Point(30, currentY + 25);
            pnlCombo.Size = new Size(490, 42);
            pnlCombo.BackColor = Color.White;
            
            ComboBox cmbMemberType = new ComboBox();
            cmbMemberType.FlatStyle = FlatStyle.Flat;
            cmbMemberType.Font = new Font("Segoe UI", 11F);
            cmbMemberType.Items.AddRange(new string[] { "Student", "Faculty", "Staff", "Guest" });
            cmbMemberType.SelectedIndex = 0;
            cmbMemberType.Location = new Point(10, 8);
            cmbMemberType.Width = 470;
            cmbMemberType.DropDownStyle = ComboBoxStyle.DropDownList;
            
            pnlCombo.Controls.Add(cmbMemberType);
            registerForm.Controls.Add(pnlCombo);
            
            pnlCombo.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnlCombo.Width - 3, pnlCombo.Height - 3), 8))
                 using(Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1))
                 {
                     e.Graphics.DrawPath(pen, path);
                 }
            };

            // Buttons
            Button btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(100, 38);
            btnCancel.Location = new Point(250, 640);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.BackColor = Color.White;
            btnCancel.ForeColor = Color.Black;
            btnCancel.FlatAppearance.BorderColor = Color.LightGray;
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.Font = ThemeConstants.FontButton;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.Click += (s, e) => registerForm.Close();

            // Round Cancel Button
            btnCancel.Paint += (s, e) => {
                // System creates default paint, we can just make sure region is rounded if we want
                 // Simple flat style with border is fine for Cancel
            };

            Button btnRegister = new Button();
            btnRegister.Text = "Register Member";
            btnRegister.Size = new Size(160, 38);
            btnRegister.Location = new Point(360, 640);
            btnRegister.FlatStyle = FlatStyle.Flat;
            btnRegister.BackColor = Color.Maroon;
            btnRegister.ForeColor = Color.White;
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.Font = ThemeConstants.FontButton;
            btnRegister.Cursor = Cursors.Hand;

            // Round Register Button
            btnRegister.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0,0, btnRegister.Width, btnRegister.Height);
                using(GraphicsPath path = CreateRoundedRectangle(r, 8))
                using(SolidBrush brush = new SolidBrush(Color.Maroon))
                {
                    e.Graphics.FillPath(brush, path);
                    TextRenderer.DrawText(e.Graphics, btnRegister.Text, btnRegister.Font, r, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            };

            registerForm.Controls.Add(btnCancel);
            registerForm.Controls.Add(btnRegister);
            registerForm.AcceptButton = btnRegister;
            registerForm.CancelButton = btnCancel;

            // Logic
            btnRegister.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtFirstName.GetActualText()) || string.IsNullOrWhiteSpace(txtLastName.GetActualText()))
                {
                    MessageBox.Show("Please enter valid names.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtEmail.GetActualText()))
                {
                    MessageBox.Show("Please enter an email.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        
                // Mock Success
                MessageBox.Show($"Member {txtFirstName.GetActualText()} {txtLastName.GetActualText()} registered successfully!", 
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                registerForm.DialogResult = DialogResult.OK;
                registerForm.Close();
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

        private bool IsValidRegularEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;
            email = email.Trim().ToLower();
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email && email.Contains("@");
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return true; 
            phone = phone.Trim();
            string digitsOnly = System.Text.RegularExpressions.Regex.Replace(phone, @"[^\d]", "");
            if (digitsOnly.Length < 7 || digitsOnly.Length > 12)
                return false;
            if (digitsOnly.Length >= 10)
            {
                if (!digitsOnly.StartsWith("09") && !digitsOnly.StartsWith("639") && !digitsOnly.StartsWith("63"))
                    return false;
            }
            return System.Text.RegularExpressions.Regex.IsMatch(phone, @"^[\d\s\-\(\)\+]+$");
        }

        private bool IsValidName(string name, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            name = name.Trim();
            if (name.Length < 2 || name.Length > 50)
                return false;
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z\s\-']+$"))
                return false;
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"[a-zA-Z]"))
                return false;
            return true;
        }

        private bool IsValidAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return true; 
            address = address.Trim();
            if (address.Length < 5 || address.Length > 200)
                return false;
            return System.Text.RegularExpressions.Regex.IsMatch(address, @"^[a-zA-Z0-9\s,.\-#]+$");
        }

        private bool IsValidIdNumber(string idNumber)
        {
            if (string.IsNullOrWhiteSpace(idNumber))
                return true; 
            idNumber = idNumber.Trim();
            if (idNumber.Length != 6)
                return false;
            return System.Text.RegularExpressions.Regex.IsMatch(idNumber, @"^[0-9]{6}$");
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
                    e.CellStyle.ForeColor = ThemeConstants.PrimaryMaroon;
                    e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                    e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    e.CellStyle.SelectionBackColor = Color.FromArgb(240, 240, 240);
                    e.CellStyle.SelectionForeColor = ThemeConstants.PrimaryMaroon;
                }
            }
        }

        private void ViewMember(string memberId)
        {
            if (_isProcessingAction) return;
            try
            {
                _isProcessingAction = true;
                
                // Load actual member data from database
                var memberService = new Service.MemberService();
                var memberData = memberService.GetMemberByNumber(memberId);
                
                if (memberData == null)
                {
                    MessageBox.Show("Member not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                // Get member statistics
                var stats = memberService.GetMemberStatistics(memberData.MemberId, memberData.MemberType);
                
                // Get borrowing history
                var borrowingHistory = memberService.GetMemberBorrowingHistory(memberData.MemberId);
                
                // Format data for display
                string memberName = memberData.FullName;
                string email = memberData.Email;
                string phone = memberData.Phone ?? "Not provided";
                string registeredDate = memberData.RegistrationDate.ToString("MMM dd, yyyy");
                string expiryDate = stats.MembershipExpiry?.ToString("MMM dd, yyyy") ?? "N/A";
                string address = memberData.Address ?? "Not provided";
                string status = memberData.StatusText;
                string memberType = memberData.MemberType;
                string department = memberData.Department ?? "Not specified";
                int currentBooks = stats.CurrentBooksCount;
                int maxBooks = stats.MaxBooks;
                int totalBorrowed = stats.TotalBorrowed;
                decimal unpaidFines = stats.UnpaidFines;
                
                using (Form viewForm = new Form())
                {
                    viewForm.Text = "";
                    viewForm.Size = new Size(700, 750);
                    viewForm.StartPosition = FormStartPosition.CenterParent;
                    viewForm.FormBorderStyle = FormBorderStyle.None;
                    viewForm.BackColor = Color.FromArgb(245, 240, 235); // Beige background
                    viewForm.Padding = new Padding(0);
                    
                    // Main container panel
                    Panel mainPanel = new Panel
                    {
                        Dock = DockStyle.Fill,
                        BackColor = Color.FromArgb(245, 240, 235), // Beige background
                        Padding = new Padding(0)
                    };
                    
                    // Header panel
                    Panel headerPanel = new Panel
                    {
                        Dock = DockStyle.Top,
                        Height = 90,
                        BackColor = Color.FromArgb(245, 240, 235), // Beige background
                        Padding = new Padding(30, 25, 30, 15)
                    };
                    
                    // Close button (X)
                    Button btnCloseX = new Button
                    {
                        Text = "âœ•",
                        Size = new Size(30, 30),
                        Location = new Point(640, 25),
                        Anchor = AnchorStyles.Top | AnchorStyles.Right,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.Transparent,
                        ForeColor = Color.Gray,
                        Font = new Font("Segoe UI", 12F),
                        Cursor = Cursors.Hand
                    };
                    btnCloseX.FlatAppearance.BorderSize = 0;
                    btnCloseX.Click += (s, e) => viewForm.Close();
                    headerPanel.Controls.Add(btnCloseX);
                    
                    // Title and ID
                    Label lblTitle = new Label
                    {
                        Text = memberName,
                        Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(40, 40, 40),
                        Location = new Point(0, 0),
                        AutoSize = true
                    };
                    headerPanel.Controls.Add(lblTitle);
                    
                    Label lblMemberId = new Label
                    {
                        Text = memberId,
                        Font = new Font("Segoe UI", 10F),
                        ForeColor = Color.Gray,
                        Location = new Point(0, 35),
                        AutoSize = true
                    };
                    headerPanel.Controls.Add(lblMemberId);
                    
                    // Content panel
                    Panel contentPanel = new Panel
                    {
                        Dock = DockStyle.Fill,
                        AutoScroll = true,
                        BackColor = Color.FromArgb(245, 240, 235), // Beige background
                        Padding = new Padding(30, 40, 30, 20)
                    };
                    
                    // Avatar and info section
                    Panel avatarSection = new Panel
                    {
                        Height = 120,
                        Dock = DockStyle.Top,
                        BackColor = Color.FromArgb(245, 240, 235) // Beige background
                    };
                    
                    // Avatar circle
                    Panel avatarPanel = new Panel
                    {
                        Size = new Size(80, 80),
                        Location = new Point(0, 0),
                        BackColor = Color.FromArgb(245, 240, 235)
                    };
                    avatarPanel.Paint += (s, e) =>
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, 79, 79), 40))
                        using (SolidBrush brush = new SolidBrush(Color.FromArgb(245, 240, 235)))
                            e.Graphics.FillPath(brush, path);
                        using (Font font = new Font("Segoe UI", 24F, FontStyle.Bold))
                        using (SolidBrush brush = new SolidBrush(ThemeConstants.PrimaryMaroon))
                        {
                            SizeF textSize = e.Graphics.MeasureString("JS", font);
                            e.Graphics.DrawString("JS", font, brush, (80 - textSize.Width) / 2, (80 - textSize.Height) / 2);
                        }
                    };
                    avatarSection.Controls.Add(avatarPanel);
                    
                    // Status tags
                    // Status badge
                    Color statusBgColor = status == "Active" ? Color.FromArgb(220, 252, 231) : 
                                         status == "Suspended" ? Color.FromArgb(254, 226, 226) : 
                                         Color.FromArgb(255, 245, 230);
                    Color statusTextColor = status == "Active" ? Color.FromArgb(34, 197, 94) : 
                                           status == "Suspended" ? Color.FromArgb(239, 68, 68) : 
                                           Color.FromArgb(200, 120, 0);
                    
                    Panel tagStatus = new Panel
                    {
                        Size = new Size(80, 24),
                        Location = new Point(100, 0),
                        BackColor = statusBgColor
                    };
                    
                    string capturedStatus = status;
                    Color capturedStatusBg = statusBgColor;
                    Color capturedStatusText = statusTextColor;
                    
                    tagStatus.Paint += (s, e) =>
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, 79, 23), 12))
                        using (SolidBrush brush = new SolidBrush(capturedStatusBg))
                            e.Graphics.FillPath(brush, path);
                        using (Font font = new Font("Segoe UI", 8F, FontStyle.Bold))
                        using (SolidBrush brush = new SolidBrush(capturedStatusText))
                        {
                            SizeF textSize = e.Graphics.MeasureString(capturedStatus, font);
                            float x = (80 - textSize.Width) / 2;
                            e.Graphics.DrawString(capturedStatus, font, brush, x, 5);
                        }
                    };
                    avatarSection.Controls.Add(tagStatus);
                    
                    // Member type badge
                    Color typeBgColor = memberType == "Student" ? Color.FromArgb(219, 234, 254) : 
                                       memberType == "Faculty" ? Color.FromArgb(254, 226, 226) : 
                                       memberType == "Staff" ? Color.FromArgb(220, 252, 231) : 
                                       Color.FromArgb(255, 245, 230);
                    Color typeTextColor = memberType == "Student" ? Color.FromArgb(59, 130, 246) : 
                                         memberType == "Faculty" ? Color.FromArgb(200, 50, 50) : 
                                         memberType == "Staff" ? Color.FromArgb(0, 150, 50) : 
                                         Color.FromArgb(200, 120, 0);
                    
                    Panel tagMemberType = new Panel
                    {
                        Size = new Size(75, 24),
                        Location = new Point(190, 0),
                        BackColor = typeBgColor
                    };
                    
                    string capturedType = memberType;
                    Color capturedTypeBg = typeBgColor;
                    Color capturedTypeText = typeTextColor;
                    
                    tagMemberType.Paint += (s, e) =>
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, 74, 23), 12))
                        using (SolidBrush brush = new SolidBrush(capturedTypeBg))
                            e.Graphics.FillPath(brush, path);
                        using (Font font = new Font("Segoe UI", 8F, FontStyle.Bold))
                        using (SolidBrush brush = new SolidBrush(capturedTypeText))
                        {
                            SizeF textSize = e.Graphics.MeasureString(capturedType, font);
                            float x = (75 - textSize.Width) / 2;
                            e.Graphics.DrawString(capturedType, font, brush, x, 5);
                        }
                    };
                    avatarSection.Controls.Add(tagMemberType);
                    
                    // Contact info
                    Label lblEmail = new Label
                    {
                        Text = email,
                        Font = new Font("Segoe UI", 10F),
                        ForeColor = Color.FromArgb(60, 60, 60),
                        Location = new Point(100, 35),
                        AutoSize = true
                    };
                    avatarSection.Controls.Add(lblEmail);
                    
                    Label lblPhone = new Label
                    {
                        Text = phone,
                        Font = new Font("Segoe UI", 10F),
                        ForeColor = Color.FromArgb(60, 60, 60),
                        Location = new Point(100, 55),
                        AutoSize = true
                    };
                    avatarSection.Controls.Add(lblPhone);
                    
                    contentPanel.Controls.Add(avatarSection);
                    
                    // Two column cards
                    Panel cardsContainer = new Panel
                    {
                        Height = 180,
                        Dock = DockStyle.Top,
                        BackColor = Color.FromArgb(245, 240, 235), // Beige background
                        Padding = new Padding(0, 30, 0, 0)
                    };
                    
                    // Membership Details card
                    Panel cardMembership = new Panel
                    {
                        Size = new Size(300, 160),
                        Location = new Point(0, 0),
                        BackColor = Color.FromArgb(245, 240, 235) // Beige background
                    };
                    cardMembership.Paint += (s, e) =>
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, 299, 159), 8))
                        using (Pen pen = new Pen(Color.FromArgb(230, 230, 230)))
                            e.Graphics.DrawPath(pen, path);
                    };
                    
                    Label lblMembershipTitle = new Label
                    {
                        Text = "Membership Details",
                        Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(40, 40, 40),
                        Location = new Point(20, 20),
                        AutoSize = true
                    };
                    cardMembership.Controls.Add(lblMembershipTitle);
                    
                    Label lblRegistered = new Label
                    {
                        Text = $"Registered: {registeredDate}",
                        Font = new Font("Segoe UI", 9F),
                        ForeColor = Color.FromArgb(100, 100, 100),
                        Location = new Point(20, 50),
                        AutoSize = true
                    };
                    cardMembership.Controls.Add(lblRegistered);
                    
                    Label lblExpires = new Label
                    {
                        Text = $"Expires: {expiryDate}",
                        Font = new Font("Segoe UI", 9F),
                        ForeColor = Color.FromArgb(100, 100, 100),
                        Location = new Point(20, 75),
                        AutoSize = true
                    };
                    cardMembership.Controls.Add(lblExpires);
                    
                    Label lblAddress = new Label
                    {
                        Text = $"Address: {address}",
                        Font = new Font("Segoe UI", 9F),
                        ForeColor = Color.FromArgb(100, 100, 100),
                        Location = new Point(20, 100),
                        AutoSize = true,
                        MaximumSize = new Size(260, 0)
                    };
                    cardMembership.Controls.Add(lblAddress);
                    
                    cardsContainer.Controls.Add(cardMembership);
                    
                    // Borrowing Statistics card
                    Panel cardStatistics = new Panel
                    {
                        Size = new Size(300, 160),
                        Location = new Point(320, 0),
                        BackColor = Color.FromArgb(245, 240, 235) // Beige background
                    };
                    cardStatistics.Paint += (s, e) =>
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, 299, 159), 8))
                        using (Pen pen = new Pen(Color.FromArgb(230, 230, 230)))
                            e.Graphics.DrawPath(pen, path);
                    };
                    
                    Label lblStatsTitle = new Label
                    {
                        Text = "Borrowing Statistics",
                        Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(40, 40, 40),
                        Location = new Point(20, 20),
                        AutoSize = true
                    };
                    cardStatistics.Controls.Add(lblStatsTitle);
                    
                    Label lblCurrentBooks = new Label
                    {
                        Text = $"Current Books: {currentBooks}/{maxBooks}",
                        Font = new Font("Segoe UI", 9F),
                        ForeColor = Color.FromArgb(100, 100, 100),
                        Location = new Point(20, 50),
                        AutoSize = true
                    };
                    cardStatistics.Controls.Add(lblCurrentBooks);
                    
                    Label lblTotalBorrowed = new Label
                    {
                        Text = $"Total Borrowed: {totalBorrowed}",
                        Font = new Font("Segoe UI", 9F),
                        ForeColor = Color.FromArgb(100, 100, 100),
                        Location = new Point(20, 75),
                        AutoSize = true
                    };
                    cardStatistics.Controls.Add(lblTotalBorrowed);
                    
                    Label lblFines = new Label
                    {
                        Text = $"Unpaid Fines: ${unpaidFines:F2}",
                        Font = new Font("Segoe UI", 9F),
                        ForeColor = Color.FromArgb(100, 100, 100),
                        Location = new Point(20, 100),
                        AutoSize = true
                    };
                    cardStatistics.Controls.Add(lblFines);
                    
                    cardsContainer.Controls.Add(cardStatistics);
                    
                    // Resize handler for cards
                    cardsContainer.Resize += (s, e) =>
                    {
                        int cardWidth = (cardsContainer.Width - 20) / 2;
                        cardMembership.Width = cardWidth;
                        cardStatistics.Width = cardWidth;
                        cardStatistics.Left = cardWidth + 20;
                    };
                    
                    contentPanel.Controls.Add(cardsContainer);
                    
                    // Borrowing History section
                    Panel historySection = new Panel
                    {
                        Height = 200,
                        Dock = DockStyle.Top,
                        BackColor = Color.FromArgb(245, 240, 235), // Beige background
                        Padding = new Padding(0, 30, 0, 0)
                    };
                    historySection.Paint += (s, e) =>
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, historySection.Width - 1, historySection.Height - 1), 8))
                        using (Pen pen = new Pen(Color.FromArgb(230, 230, 230)))
                            e.Graphics.DrawPath(pen, path);
                    };
                    
                    Label lblHistoryTitle = new Label
                    {
                        Text = "ðŸ• Borrowing History",
                        Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(40, 40, 40),
                        Location = new Point(20, 20),
                        AutoSize = true
                    };
                    historySection.Controls.Add(lblHistoryTitle);
                    
                    // Display real borrowing history from database
                    int historyCount = 0;
                    foreach (var borrowing in borrowingHistory.Take(5)) // Show last 5 borrowings
                    {
                        Panel historyItem = new Panel
                        {
                            Height = 50,
                            Dock = DockStyle.Top,
                            BackColor = Color.Transparent,
                            Padding = new Padding(20, 10, 20, 0)
                        };
                        
                        Label lblBook = new Label
                        {
                            Text = borrowing.BookTitle,
                            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                            ForeColor = Color.FromArgb(40, 40, 40),
                            Location = new Point(0, 5),
                            AutoSize = true,
                            MaximumSize = new Size(400, 0)
                        };
                        historyItem.Controls.Add(lblBook);
                        
                        Label lblDate = new Label
                        {
                            Text = borrowing.BorrowDate.ToString("MMM dd, yyyy"),
                            Font = new Font("Segoe UI", 9F),
                            ForeColor = Color.Gray,
                            Location = new Point(0, 25),
                            AutoSize = true
                        };
                        historyItem.Controls.Add(lblDate);
                        
                        // Status tag
                        string borrowStatusText = borrowing.Status;
                        Color borrowBgColor = Color.FromArgb(243, 244, 246);
                        Color borrowTextColor = Color.FromArgb(107, 114, 128);
                        
                        // Determine if overdue
                        if (borrowing.Status == "Borrowed" && borrowing.DueDate < DateTime.Now)
                        {
                            borrowStatusText = "Overdue";
                            borrowBgColor = Color.FromArgb(254, 226, 226);
                            borrowTextColor = Color.FromArgb(239, 68, 68);
                        }
                        else if (borrowing.Status == "Returned")
                        {
                            borrowStatusText = "Returned";
                            borrowBgColor = Color.FromArgb(243, 244, 246);
                            borrowTextColor = Color.FromArgb(107, 114, 128);
                        }
                        else if (borrowing.Status == "Borrowed")
                        {
                            borrowStatusText = "Borrowed";
                            borrowBgColor = Color.FromArgb(219, 234, 254);
                            borrowTextColor = Color.FromArgb(37, 99, 235);
                        }
                        
                        Panel tagBorrowStatus = new Panel
                        {
                            Size = new Size(75, 22),
                            Location = new Point(historyItem.Width - 95, 14),
                            Anchor = AnchorStyles.Top | AnchorStyles.Right,
                            BackColor = borrowBgColor
                        };
                        
                        Color capturedBorrowBg = borrowBgColor;
                        Color capturedBorrowText = borrowTextColor;
                        string capturedBorrowStatus = borrowStatusText;
                        
                        tagBorrowStatus.Paint += (s, e) =>
                        {
                            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                            using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, 74, 21), 11))
                            using (SolidBrush brush = new SolidBrush(capturedBorrowBg))
                                e.Graphics.FillPath(brush, path);
                            using (Font font = new Font("Segoe UI", 8F, FontStyle.Bold))
                            using (SolidBrush brush = new SolidBrush(capturedBorrowText))
                            {
                                SizeF textSize = e.Graphics.MeasureString(capturedBorrowStatus, font);
                                float x = (75 - textSize.Width) / 2;
                                e.Graphics.DrawString(capturedBorrowStatus, font, brush, x, 4);
                            }
                        };
                        historyItem.Controls.Add(tagBorrowStatus);
                        historySection.Controls.Add(historyItem);
                        
                        historyCount++;
                    }
                    
                    // If no borrowing history
                    if (historyCount == 0)
                    {
                        Label lblNoBorrowings = new Label
                        {
                            Text = "No borrowing history yet",
                            Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                            ForeColor = Color.Gray,
                            Location = new Point(20, 60),
                            AutoSize = true
                        };
                        historySection.Controls.Add(lblNoBorrowings);
                    }
                    
                    contentPanel.Controls.Add(historySection);
                    
                    // Quick Actions section
                    Panel actionsSection = new Panel
                    {
                        Height = 100,
                        Dock = DockStyle.Top,
                        BackColor = Color.FromArgb(245, 240, 235), // Beige background
                        Padding = new Padding(0, 30, 0, 0)
                    };
                    actionsSection.Paint += (s, e) =>
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, actionsSection.Width - 1, actionsSection.Height - 1), 8))
                        using (Pen pen = new Pen(Color.FromArgb(230, 230, 230)))
                            e.Graphics.DrawPath(pen, path);
                    };
                    
                    Label lblActionsTitle = new Label
                    {
                        Text = "Quick Actions",
                        Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(40, 40, 40),
                        Location = new Point(20, 20),
                        AutoSize = true
                    };
                    actionsSection.Controls.Add(lblActionsTitle);
                    
                    // Dynamic suspend/activate button based on current status
                    string suspendBtnText = status == "Suspended" ? "Activate Member" : "Suspend Member";
                    Color suspendBtnBg = status == "Suspended" ? Color.FromArgb(220, 252, 231) : Color.FromArgb(254, 226, 226);
                    Color suspendBtnForeColor = status == "Suspended" ? Color.FromArgb(22, 163, 74) : Color.FromArgb(239, 68, 68);
                    
                    Button btnSuspend = new Button
                    {
                        Text = suspendBtnText,
                        Size = new Size(150, 35),
                        Location = new Point(20, 50),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = suspendBtnBg,
                        ForeColor = suspendBtnForeColor,
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                        Cursor = Cursors.Hand
                    };
                    btnSuspend.FlatAppearance.BorderSize = 0;
                    
                    Color capturedSuspendBg = suspendBtnBg;
                    Color capturedSuspendText = suspendBtnForeColor;
                    string capturedSuspendBtnText = suspendBtnText;
                    
                    btnSuspend.Paint += (s, e) =>
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnSuspend.Width - 1, btnSuspend.Height - 1), 6))
                        using (SolidBrush brush = new SolidBrush(capturedSuspendBg))
                            e.Graphics.FillPath(brush, path);
                        TextRenderer.DrawText(e.Graphics, capturedSuspendBtnText, btnSuspend.Font, new Rectangle(0, 0, btnSuspend.Width, btnSuspend.Height), capturedSuspendText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    };
                    
                    btnSuspend.Click += (s, e) =>
                    {
                        _isProcessingAction = false; // Reset flag to allow SuspendMember to execute
                        SuspendMember(memberId);
                        // Refresh the view after suspension
                        viewForm.Close();
                        if (!string.IsNullOrEmpty(memberId))
                        {
                            ViewMember(memberId);
                        }
                    };
                    
                    actionsSection.Controls.Add(btnSuspend);
                    
                    contentPanel.Controls.Add(actionsSection);
                    
                    // Footer buttons
                    Panel footerPanel = new Panel
                    {
                        Dock = DockStyle.Bottom,
                        Height = 70,
                        BackColor = Color.FromArgb(245, 240, 235), // Beige background
                        Padding = new Padding(30, 15, 30, 15)
                    };
                    
                    Button btnClose = new Button
                    {
                        Text = "Close",
                        Size = new Size(100, 40),
                        Location = new Point(580, 15),
                        Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.FromArgb(243, 244, 246),
                        ForeColor = Color.FromArgb(40, 40, 40),
                        Font = new Font("Segoe UI", 9F),
                        Cursor = Cursors.Hand,
                        DialogResult = DialogResult.Cancel
                    };
                    btnClose.FlatAppearance.BorderSize = 0;
                    btnClose.Paint += (s, e) =>
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnClose.Width - 1, btnClose.Height - 1), 6))
                        using (SolidBrush brush = new SolidBrush(Color.FromArgb(243, 244, 246)))
                            e.Graphics.FillPath(brush, path);
                        TextRenderer.DrawText(e.Graphics, btnClose.Text, btnClose.Font, new Rectangle(0, 0, btnClose.Width, btnClose.Height), Color.FromArgb(40, 40, 40), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    };
                    footerPanel.Controls.Add(btnClose);
                    
                    mainPanel.Controls.Add(footerPanel);
                    mainPanel.Controls.Add(contentPanel);
                    mainPanel.Controls.Add(headerPanel);
                    
                    // Form border/shadow effect
                    viewForm.Paint += (s, e) =>
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, viewForm.Width - 1, viewForm.Height - 1), 12))
                        using (Pen pen = new Pen(Color.FromArgb(200, 200, 200), 1))
                            e.Graphics.DrawPath(pen, path);
                    };
                    
                    viewForm.Controls.Add(mainPanel);
                    viewForm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load member details: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isProcessingAction = false;
            }
        }

        private void EditMember(string memberId)
        {
            if (_isProcessingAction) return;
            try
            {
                _isProcessingAction = true;
                
                using (Form editForm = new Form())
                {
                    editForm.Text = $"Edit Member - {memberId}";
                    editForm.Size = new Size(500, 600);
                    editForm.StartPosition = FormStartPosition.CenterParent;
                    editForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    editForm.MaximizeBox = false;
                    editForm.MinimizeBox = false;
                    
                    Label lblTitle = new Label
                    {
                        Text = $"Edit Member: {memberId}",
                        Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                        ForeColor = ThemeConstants.PrimaryMaroon,
                        Location = new Point(20, 20),
                        AutoSize = true
                    };
                    
                    Label lblMemberId = new Label { Text = "Member ID:", Location = new Point(20, 70), AutoSize = true };
                    TextBox txtMemberId = new TextBox { Text = memberId, Location = new Point(20, 95), Size = new Size(440, 30), Font = new Font("Segoe UI", 10F), ReadOnly = true, BackColor = Color.LightGray };
                    
                    Label lblName = new Label { Text = "Full Name:", Location = new Point(20, 140), AutoSize = true };
                    TextBox txtName = new TextBox { Text = "[Member Name]", Location = new Point(20, 165), Size = new Size(440, 30), Font = new Font("Segoe UI", 10F) };
                    
                    Label lblType = new Label { Text = "Member Type:", Location = new Point(20, 210), AutoSize = true };
                    ComboBox cmbType = new ComboBox { Location = new Point(20, 235), Size = new Size(440, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F) };
                    cmbType.Items.AddRange(new[] { "Student", "Faculty", "Staff", "Guest" });
                    cmbType.SelectedIndex = 0;
                    
                    Label lblEmail = new Label { Text = "Email:", Location = new Point(20, 280), AutoSize = true };
                    TextBox txtEmail = new TextBox { Text = "[Email Address]", Location = new Point(20, 305), Size = new Size(440, 30), Font = new Font("Segoe UI", 10F) };
                    
                    Label lblPhone = new Label { Text = "Phone:", Location = new Point(20, 350), AutoSize = true };
                    TextBox txtPhone = new TextBox { Text = "[Phone Number]", Location = new Point(20, 375), Size = new Size(440, 30), Font = new Font("Segoe UI", 10F) };
                    
                    Label lblStatus = new Label { Text = "Status:", Location = new Point(20, 410), AutoSize = true };
                    ComboBox cmbStatus = new ComboBox { Location = new Point(20, 435), Size = new Size(440, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F) };
                    cmbStatus.Items.AddRange(new[] { "Active", "Suspended", "Expired", "Inactive" });
                    cmbStatus.SelectedIndex = 0;
                    
                    Button btnSave = new Button
                    {
                        Text = "Save Changes",
                        Location = new Point(300, 480),
                        Size = new Size(100, 35),
                        BackColor = ThemeConstants.PrimaryMaroon,
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        DialogResult = DialogResult.OK
                    };
                    
                    Button btnCancel = new Button
                    {
                        Text = "Cancel",
                        Location = new Point(410, 480),
                        Size = new Size(75, 35),
                        DialogResult = DialogResult.Cancel
                    };
                    
                    editForm.Controls.AddRange(new Control[] { lblTitle, lblMemberId, txtMemberId, lblName, txtName, 
                        lblType, cmbType, lblEmail, txtEmail, lblPhone, txtPhone, lblStatus, cmbStatus, btnSave, btnCancel });
                    
                    if (editForm.ShowDialog() == DialogResult.OK)
                    {
                        if (string.IsNullOrWhiteSpace(txtName.Text))
                        {
                            MessageBox.Show("Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        
                        // TODO: Update database
                        MessageBox.Show($"Member {txtName.Text} has been updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadMembersData();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to edit member: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isProcessingAction = false;
            }
        }

        private void SuspendMember(string memberId)
        {
            if (_isProcessingAction) return;
            try
            {
                _isProcessingAction = true;
                
                // Load member data
                var memberService = new Service.MemberService();
                var memberData = memberService.GetMemberByNumber(memberId);
                
                if (memberData == null)
                {
                    MessageBox.Show("Member not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                string currentStatus = memberData.StatusText;
                string newStatus = currentStatus == "Suspended" ? "Active" : "Suspended";
                string action = currentStatus == "Suspended" ? "activate" : "suspend";
                
                // If suspending, show dialog for reason and duration
                if (newStatus == "Suspended")
                {
                    using (Form suspendForm = new Form())
                    {
                        suspendForm.Text = "Suspend Member";
                        suspendForm.Size = new Size(500, 350);
                        suspendForm.StartPosition = FormStartPosition.CenterParent;
                        suspendForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                        suspendForm.MaximizeBox = false;
                        suspendForm.MinimizeBox = false;
                        suspendForm.BackColor = Color.FromArgb(245, 240, 235);
                        
                        Label lblTitle = new Label
                        {
                            Text = $"Suspend {memberData.FirstName} {memberData.LastName}",
                            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                            ForeColor = ThemeConstants.PrimaryMaroon,
                            Location = new Point(20, 20),
                            AutoSize = true
                        };
                        suspendForm.Controls.Add(lblTitle);
                        
                        Label lblReason = new Label
                        {
                            Text = "Reason for Suspension:",
                            Font = new Font("Segoe UI", 10F),
                            Location = new Point(20, 60),
                            AutoSize = true
                        };
                        suspendForm.Controls.Add(lblReason);
                        
                        TextBox txtReason = new TextBox
                        {
                            Location = new Point(20, 85),
                            Size = new Size(440, 80),
                            Multiline = true,
                            Font = new Font("Segoe UI", 10F),
                            ScrollBars = ScrollBars.Vertical
                        };
                        suspendForm.Controls.Add(txtReason);
                        
                        Label lblDuration = new Label
                        {
                            Text = "Suspension Duration (days):",
                            Font = new Font("Segoe UI", 10F),
                            Location = new Point(20, 180),
                            AutoSize = true
                        };
                        suspendForm.Controls.Add(lblDuration);
                        
                        NumericUpDown numDuration = new NumericUpDown
                        {
                            Location = new Point(20, 205),
                            Size = new Size(150, 25),
                            Minimum = 1,
                            Maximum = 365,
                            Value = 30,
                            Font = new Font("Segoe UI", 10F)
                        };
                        suspendForm.Controls.Add(numDuration);
                        
                        Label lblDays = new Label
                        {
                            Text = "days (Leave blank for indefinite)",
                            Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                            ForeColor = Color.Gray,
                            Location = new Point(180, 208),
                            AutoSize = true
                        };
                        suspendForm.Controls.Add(lblDays);
                        
                        Button btnCancel = new Button
                        {
                            Text = "Cancel",
                            Size = new Size(100, 35),
                            Location = new Point(250, 260),
                            DialogResult = DialogResult.Cancel,
                            Font = new Font("Segoe UI", 9F)
                        };
                        suspendForm.Controls.Add(btnCancel);
                        
                        Button btnSuspend = new Button
                        {
                            Text = "Suspend",
                            Size = new Size(100, 35),
                            Location = new Point(360, 260),
                            BackColor = ThemeConstants.PrimaryMaroon,
                            ForeColor = Color.White,
                            FlatStyle = FlatStyle.Flat,
                            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                            DialogResult = DialogResult.OK
                        };
                        btnSuspend.FlatAppearance.BorderSize = 0;
                        suspendForm.Controls.Add(btnSuspend);
                        
                        suspendForm.AcceptButton = btnSuspend;
                        suspendForm.CancelButton = btnCancel;
                        
                        if (suspendForm.ShowDialog() == DialogResult.OK)
                        {
                            string reason = txtReason.Text.Trim();
                            if (string.IsNullOrEmpty(reason))
                            {
                                MessageBox.Show("Please provide a reason for suspension.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            
                            int duration = (int)numDuration.Value;
                            
                            // TODO: Store reason and duration in database
                            // For now, just update the status
                            int statusValue = 2; // Suspended
                            
                            bool success = memberService.UpdateMember(
                                memberId,
                                memberData.FirstName,
                                memberData.LastName,
                                memberData.Email,
                                memberData.Phone,
                                memberData.Address,
                                memberData.Department,
                                memberData.MemberType,
                                statusValue
                            );
                            
                            if (success)
                            {
                                MessageBox.Show($"Member has been suspended for {duration} days.\nReason: {reason}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                LoadMembersData();
                            }
                            else
                            {
                                MessageBox.Show("Failed to suspend member. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                else
                {
                    // Activating - simple confirmation
                    DialogResult result = MessageBox.Show(
                        $"Are you sure you want to activate {memberData.FirstName} {memberData.LastName}?",
                        "Activate Member",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Yes)
                    {
                        int statusValue = 1; // Active
                        
                        bool success = memberService.UpdateMember(
                            memberId,
                            memberData.FirstName,
                            memberData.LastName,
                            memberData.Email,
                            memberData.Phone,
                            memberData.Address,
                            memberData.Department,
                            memberData.MemberType,
                            statusValue
                        );
                        
                        if (success)
                        {
                            MessageBox.Show("Member has been activated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadMembersData();
                        }
                        else
                        {
                            MessageBox.Show("Failed to activate member. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update member status: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isProcessingAction = false;
            }
        }

        private void DeleteMember(string memberId)
        {
            if (_isProcessingAction) return;
            try
            {
                _isProcessingAction = true;
                DialogResult result = MessageBox.Show(
                    $"Are you sure you want to delete member {memberId}?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    // TODO: Delete member from database
                    LoadMembersData();
                    MessageBox.Show("Member deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete member: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isProcessingAction = false;
            }
        }

        private void ShowCatalogView()
        {
            RestoreOriginalControls();
            ShowDashboardControls(false);
            ClearDynamicControls();
            Label titleLabel = new Label
            {
                Text = "Catalog",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(300, 70),
                ForeColor = Color.FromArgb(40, 40, 40),
                BackColor = Color.Transparent
            };
            Label subtitleLabel = new Label
            {
                Text = "Browse and manage library books and resources",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Location = new Point(30, 95),
                Size = new Size(500, 25),
                ForeColor = Color.FromArgb(100, 100, 100),
                BackColor = Color.Transparent
            };
            Button btnAddBookHeader = new Button
            {
                Text = "+ Add Book",
                Location = new Point(pnlMainContent.Width - 150, 20),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = ThemeConstants.PrimaryMaroon,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnAddBookHeader.FlatAppearance.BorderSize = 0;
            btnAddBookHeader.Click += (s, e) => ShowAddBookDialog();
            Panel statsPanel = new Panel
            {
                Location = new Point(30, 150),
                Size = new Size(pnlMainContent.Width - 60, 80),
                BackColor = Color.Transparent,
                Tag = "CatalogStatsPanel"
            };
            Panel cardTotalTitles = CreateCatalogStatCard("ðŸ“š", "0", "Total Titles", ThemeConstants.PrimaryMaroon, new Point(0, 0));
            Panel cardAvailableCopies = CreateCatalogStatCard("ðŸ“–", "0", "Available Copies", Color.FromArgb(76, 175, 80), new Point(200, 0));
            Panel cardTotalCopies = CreateCatalogStatCard("ðŸ“š", "0", "Total Copies", Color.FromArgb(33, 150, 243), new Point(400, 0));
            Panel cardCategories = CreateCatalogStatCard("ðŸ”–", "0", "Categories", Color.FromArgb(255, 152, 0), new Point(600, 0));
            statsPanel.Controls.AddRange(new Control[] { cardTotalTitles, cardAvailableCopies, cardTotalCopies, cardCategories });
            Panel searchPanel = new Panel
            {
                Location = new Point(30, 240),
                Size = new Size(pnlMainContent.Width - 60, 70),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            searchPanel.Paint += (s, e) =>
            {
                using (var shadowBrush = new SolidBrush(Color.FromArgb(15, 0, 0, 0)))
                {
                    e.Graphics.FillRectangle(shadowBrush, 3, 3, searchPanel.Width - 3, searchPanel.Height - 3);
                }
                using (var bgBrush = new SolidBrush(Color.FromArgb(252, 252, 252)))
                {
                    e.Graphics.FillRectangle(bgBrush, 0, 0, searchPanel.Width, searchPanel.Height);
                }
                using (var borderPen = new Pen(Color.FromArgb(220, 220, 220), 1))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, searchPanel.Width - 1, searchPanel.Height - 1);
                }
            };
            Panel searchContainer = new Panel
            {
                Location = new Point(20, 15),
                Size = new Size(320, 40),
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.None
            };
            Label lblSearchIcon = new Label
            {
                Text = "ðŸ”",
                Location = new Point(10, 8),
                Size = new Size(25, 24),
                Font = new Font("Segoe UI", 12F),
                ForeColor = Color.FromArgb(150, 150, 150),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            TextBox txtSearchBooks = new TextBox
            {
                Location = new Point(40, 8),
                Size = new Size(270, 24),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Gray,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(245, 245, 245)
            };
            txtSearchBooks.SetPlaceholder("Search by title, author, or ISBN...");
            Label lblCategoryFilter = new Label
            {
                Text = "Category:",
                Location = new Point(340, 18),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 10F)
            };
            ComboBox cmbCategoryFilter = new ComboBox
            {
                Location = new Point(420, 15),
                Size = new Size(150, 30),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCategoryFilter.Items.Add("All Categories");
            cmbCategoryFilter.SelectedIndex = 0;
            DataGridView dgvBooks = new DataGridView
            {
                Location = new Point(30, 320),
                Size = new Size(pnlMainContent.Width - 60, pnlMainContent.Height - 350),
                BackgroundColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(230, 230, 230),
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            dgvBooks.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 249, 250),
                ForeColor = Color.FromArgb(33, 37, 41),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                SelectionBackColor = Color.FromArgb(248, 249, 250)
            };
            dgvBooks.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(33, 37, 41),
                Font = new Font("Segoe UI", 9F),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0),
                SelectionBackColor = Color.FromArgb(33, 150, 243, 20),
                SelectionForeColor = Color.FromArgb(33, 37, 41)
            };
            dgvBooks.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(252, 252, 252),
                ForeColor = Color.FromArgb(33, 37, 41),
                Font = new Font("Segoe UI", 9F),
                SelectionBackColor = Color.FromArgb(33, 150, 243, 20),
                SelectionForeColor = Color.FromArgb(33, 37, 41)
            };
            dgvBooks.Columns.Add("BookId", "ID");
            dgvBooks.Columns.Add("Title", "Title");
            dgvBooks.Columns.Add("Author", "Author");
            dgvBooks.Columns.Add("ISBN", "ISBN");
            dgvBooks.Columns.Add("Category", "Category");
            dgvBooks.Columns.Add("TotalCopies", "Total");
            dgvBooks.Columns.Add("AvailableCopies", "Available");
            dgvBooks.Columns["BookId"].Width = 60;
            dgvBooks.Columns["Title"].Width = 200;
            dgvBooks.Columns["Author"].Width = 150;
            dgvBooks.Columns["ISBN"].Width = 120;
            dgvBooks.Columns["Category"].Width = 100;
            dgvBooks.Columns["TotalCopies"].Width = 70;
            dgvBooks.Columns["AvailableCopies"].Width = 80;
            dgvBooks.Columns["BookId"].Visible = false;
            searchContainer.Controls.AddRange(new Control[] { lblSearchIcon, txtSearchBooks });
            searchPanel.Controls.AddRange(new Control[] { searchContainer, lblCategoryFilter, cmbCategoryFilter });
            pnlMainContent.Controls.AddRange(new Control[] { titleLabel, subtitleLabel, btnAddBookHeader, statsPanel, searchPanel, dgvBooks });
        }

        private void ShowCirculationView()
        {
            RestoreOriginalControls();
            ShowDashboardControls(false);
            ClearDynamicControls();
            Label titleLabel = new Label
            {
                Text = "Circulation",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(300, 70),
                ForeColor = Color.FromArgb(40, 40, 40),
                BackColor = Color.Transparent
            };
            Label subtitleLabel = new Label
            {
                Text = "Manage book borrowings, returns, and renewals",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Location = new Point(30, 95),
                Size = new Size(500, 25),
                ForeColor = Color.FromArgb(100, 100, 100),
                BackColor = Color.Transparent
            };
            Panel statsPanel = new Panel
            {
                Location = new Point(30, 150),
                Size = new Size(pnlMainContent.Width - 60, 80),
                BackColor = Color.Transparent,
                Tag = "CirculationStatsPanel"
            };
            Panel cardCurrentlyBorrowed = CreateCatalogStatCard("ðŸ“š", "0", "Currently Borrowed", Color.FromArgb(33, 150, 243), new Point(0, 0));
            Panel cardOverdue = CreateCatalogStatCard("âš ï¸", "0", "Overdue", Color.FromArgb(244, 67, 54), new Point(200, 0));
            Panel cardReturnedToday = CreateCatalogStatCard("âœ“", "0", "Returned Today", Color.FromArgb(76, 175, 80), new Point(400, 0));
            statsPanel.Controls.AddRange(new Control[] { cardCurrentlyBorrowed, cardOverdue, cardReturnedToday });
            Panel searchPanel = new Panel
            {
                Location = new Point(30, 250),
                Size = new Size(pnlMainContent.Width - 60, 70),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            searchPanel.Paint += (s, e) =>
            {
                using (var shadowBrush = new SolidBrush(Color.FromArgb(15, 0, 0, 0)))
                {
                    e.Graphics.FillRectangle(shadowBrush, 3, 3, searchPanel.Width - 3, searchPanel.Height - 3);
                }
                using (var bgBrush = new SolidBrush(Color.FromArgb(252, 252, 252)))
                {
                    e.Graphics.FillRectangle(bgBrush, 0, 0, searchPanel.Width, searchPanel.Height);
                }
                using (var borderPen = new Pen(Color.FromArgb(220, 220, 220), 1))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, searchPanel.Width - 1, searchPanel.Height - 1);
                }
            };
            Panel searchContainer = new Panel
            {
                Location = new Point(20, 15),
                Size = new Size(320, 40),
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.None
            };
            TextBox txtSearchBorrowings = new TextBox
            {
                Location = new Point(40, 8),
                Size = new Size(260, 24),
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(245, 245, 245)
            };
            txtSearchBorrowings.SetPlaceholder("Search transactions...");
            Label lblStatusFilter = new Label
            {
                Text = "Status:",
                Location = new Point(searchPanel.Width - 200, 18),
                Size = new Size(60, 25),
                Font = new Font("Segoe UI", 10F),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            ComboBox cmbStatusFilter = new ComboBox
            {
                Location = new Point(searchPanel.Width - 140, 15),
                Size = new Size(120, 30),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            cmbStatusFilter.Items.AddRange(new[] { "All Status", "Active", "Returned", "Overdue" });
            cmbStatusFilter.SelectedIndex = 0;
            Label searchIcon = new Label
            {
                Text = "ðŸ”",
                Font = new Font("Segoe UI", 12F),
                Location = new Point(10, 10),
                Size = new Size(25, 20),
                ForeColor = Color.FromArgb(150, 150, 150),
                TextAlign = ContentAlignment.MiddleCenter
            };
            searchContainer.Controls.Add(searchIcon);
            searchContainer.Controls.Add(txtSearchBorrowings);
            Button btnCheckout = new Button
            {
                Text = "Check Out",
                Location = new Point(pnlMainContent.Width - 250, 20),
                Size = new Size(110, 35),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = ThemeConstants.PrimaryMaroon,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnCheckout.FlatAppearance.BorderSize = 0;
            btnCheckout.Click += (s, e) => ShowCheckOutDialog();
            Button btnReturn = new Button
            {
                Text = "Return",
                Location = new Point(pnlMainContent.Width - 130, 20),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = ThemeConstants.PrimaryMaroon,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnReturn.FlatAppearance.BorderSize = 1;
            btnReturn.FlatAppearance.BorderColor = ThemeConstants.PrimaryMaroon;
            btnReturn.Click += (s, e) => ShowFeatureMessage("Return", "Return functionality will be implemented.");
            DataGridView dgvBorrowings = new DataGridView
            {
                Location = new Point(30, 340),
                Size = new Size(pnlMainContent.Width - 60, pnlMainContent.Height - 370),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            dgvBorrowings.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(40, 40, 40),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgvBorrowings.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(40, 40, 40),
                Font = new Font("Segoe UI", 9F),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                SelectionBackColor = ThemeConstants.PrimaryMaroon,
                SelectionForeColor = Color.White
            };
            dgvBorrowings.Columns.Add("BorrowingId", "ID");
            dgvBorrowings.Columns.Add("AccessionNumber", "Accession #");
            dgvBorrowings.Columns.Add("BookTitle", "Book Title");
            dgvBorrowings.Columns.Add("MemberName", "Member");
            dgvBorrowings.Columns.Add("BorrowDate", "Borrow Date");
            dgvBorrowings.Columns.Add("DueDate", "Due Date");
            dgvBorrowings.Columns.Add("Status", "Status");
            dgvBorrowings.Columns.Add("Fine", "Fine");
            dgvBorrowings.Columns["BorrowingId"].Width = 80;
            dgvBorrowings.Columns["AccessionNumber"].Width = 120;
            dgvBorrowings.Columns["BookTitle"].Width = 200;
            dgvBorrowings.Columns["MemberName"].Width = 150;
            dgvBorrowings.Columns["BorrowDate"].Width = 100;
            dgvBorrowings.Columns["DueDate"].Width = 100;
            dgvBorrowings.Columns["Status"].Width = 100;
            dgvBorrowings.Columns["Fine"].Width = 80;
            dgvBorrowings.Columns["BorrowingId"].Visible = false;
            searchContainer.Controls.AddRange(new Control[] { searchIcon, txtSearchBorrowings });
            searchPanel.Controls.AddRange(new Control[] { searchContainer, lblStatusFilter, cmbStatusFilter });
            pnlMainContent.Controls.AddRange(new Control[] { titleLabel, subtitleLabel, btnCheckout, btnReturn, statsPanel, searchPanel, dgvBorrowings });
        }

        private void ShowReservationsView()
        {
            RestoreOriginalControls();
            ShowDashboardControls(false);
            ClearDynamicControls();
            Label titleLabel = new Label
            {
                Text = "Reservations",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(200, 40),
                ForeColor = Color.FromArgb(40, 40, 40),
                BackColor = Color.Transparent
            };
            Label subtitleLabel = new Label
            {
                Text = "Manage book reservations and pickup notifications",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Location = new Point(30, 60),
                Size = new Size(400, 25),
                ForeColor = Color.FromArgb(100, 100, 100),
                BackColor = Color.Transparent
            };
            Button btnNewReservation = new Button
            {
                Text = "New Reservation",
                Location = new Point(pnlMainContent.Width - 180, 20),
                Size = new Size(150, 35),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = ThemeConstants.PrimaryMaroon,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnNewReservation.FlatAppearance.BorderSize = 0;
            btnNewReservation.Click += (s, e) => ShowReservationDialog();
            Panel statsPanel = new Panel
            {
                Location = new Point(30, 100),
                Size = new Size(pnlMainContent.Width - 60, 80),
                BackColor = Color.Transparent,
                Tag = "ReservationsStatsPanel"
            };
            Panel cardPending = CreateCatalogStatCard("ðŸ•’", "0", "Pending", Color.FromArgb(255, 193, 7), new Point(0, 0));
            Panel cardReady = CreateCatalogStatCard("ðŸ””", "0", "Ready for Pickup", Color.FromArgb(76, 175, 80), new Point(200, 0));
            Panel cardFulfilled = CreateCatalogStatCard("âœ“", "0", "Fulfilled", Color.FromArgb(33, 150, 243), new Point(400, 0));
            Panel cardExpired = CreateCatalogStatCard("âœ—", "0", "Expired", Color.FromArgb(158, 158, 158), new Point(600, 0));
            statsPanel.Controls.AddRange(new Control[] { cardPending, cardReady, cardFulfilled, cardExpired });
            Panel searchPanel = new Panel
            {
                Location = new Point(30, 190),
                Size = new Size(pnlMainContent.Width - 60, 70),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            searchPanel.Paint += (s, e) =>
            {
                using (var shadowBrush = new SolidBrush(Color.FromArgb(15, 0, 0, 0)))
                {
                    e.Graphics.FillRectangle(shadowBrush, 3, 3, searchPanel.Width - 3, searchPanel.Height - 3);
                }
                using (var bgBrush = new SolidBrush(Color.FromArgb(252, 252, 252)))
                {
                    e.Graphics.FillRectangle(bgBrush, 0, 0, searchPanel.Width, searchPanel.Height);
                }
                using (var borderPen = new Pen(Color.FromArgb(220, 220, 220), 1))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, searchPanel.Width - 1, searchPanel.Height - 1);
                }
            };
            Panel searchContainer = new Panel
            {
                Location = new Point(20, 15),
                Size = new Size(320, 40),
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.None
            };
            Label searchIcon = new Label
            {
                Text = "ðŸ”",
                Font = new Font("Segoe UI", 12F),
                Location = new Point(10, 10),
                Size = new Size(25, 20),
                ForeColor = Color.FromArgb(150, 150, 150),
                TextAlign = ContentAlignment.MiddleCenter
            };
            TextBox txtSearchReservations = new TextBox
            {
                Location = new Point(40, 8),
                Size = new Size(260, 24),
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(245, 245, 245)
            };
            txtSearchReservations.SetPlaceholder("Search reservations...");
            searchContainer.Controls.AddRange(new Control[] { searchIcon, txtSearchReservations });
            DataGridView dgvReservations = new DataGridView
            {
                Location = new Point(30, 280),
                Size = new Size(pnlMainContent.Width - 60, pnlMainContent.Height - 310),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            dgvReservations.Columns.Add("ReservationId", "ID");
            dgvReservations.Columns.Add("BookTitle", "Book Title");
            dgvReservations.Columns.Add("MemberName", "Member");
            dgvReservations.Columns.Add("ReservationDate", "Reservation Date");
            dgvReservations.Columns.Add("Status", "Status");
            dgvReservations.Columns["ReservationId"].Width = 100;
            dgvReservations.Columns["BookTitle"].Width = 250;
            dgvReservations.Columns["MemberName"].Width = 200;
            dgvReservations.Columns["ReservationDate"].Width = 150;
            dgvReservations.Columns["Status"].Width = 120;
            dgvReservations.Columns["ReservationId"].Visible = false;
            searchPanel.Controls.Add(searchContainer);
            pnlMainContent.Controls.AddRange(new Control[] { titleLabel, subtitleLabel, btnNewReservation, statsPanel, searchPanel, dgvReservations });
        }

        private void ShowFinesView()
        {
            RestoreOriginalControls();
            ShowDashboardControls(false);
            ClearDynamicControls();
            Label titleLabel = new Label
            {
                Text = "Fines & Penalties",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(300, 70),
                ForeColor = Color.FromArgb(40, 40, 40),
                BackColor = Color.Transparent
            };
            Label subtitleLabel = new Label
            {
                Text = "Manage member fines, payments, and waivers",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Location = new Point(30, 95),
                Size = new Size(500, 25),
                ForeColor = Color.FromArgb(100, 100, 100),
                BackColor = Color.Transparent
            };
            Button btnAddFine = new Button
            {
                Text = "Add Fine",
                Location = new Point(pnlMainContent.Width - 150, 20),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = ThemeConstants.PrimaryMaroon,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnAddFine.FlatAppearance.BorderSize = 0;
            btnAddFine.Click += (s, e) => ShowAddFineDialog();
            Panel statsPanel = new Panel
            {
                Location = new Point(30, 150),
                Size = new Size(pnlMainContent.Width - 60, 80),
                BackColor = Color.Transparent,
                Tag = "FinesStatsPanel"
            };
            Panel cardPendingFines = CreateCatalogStatCard("âš ï¸", "â‚±0.00", "Pending Fines", ThemeConstants.PrimaryMaroon, new Point(0, 0));
            Panel cardCollected = CreateCatalogStatCard("âœ“", "â‚±0.00", "Collected", Color.FromArgb(76, 175, 80), new Point(200, 0));
            Panel cardWaived = CreateCatalogStatCard("âœ—", "â‚±0.00", "Waived", Color.FromArgb(33, 150, 243), new Point(400, 0));
            Panel cardPendingCases = CreateCatalogStatCard("â‚±", "0", "Pending Cases", Color.FromArgb(255, 193, 7), new Point(600, 0));
            statsPanel.Controls.AddRange(new Control[] { cardPendingFines, cardCollected, cardWaived, cardPendingCases });
            Panel searchPanel = new Panel
            {
                Location = new Point(30, 250),
                Size = new Size(pnlMainContent.Width - 60, 70),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            searchPanel.Paint += (s, e) =>
            {
                using (var shadowBrush = new SolidBrush(Color.FromArgb(15, 0, 0, 0)))
                {
                    e.Graphics.FillRectangle(shadowBrush, 3, 3, searchPanel.Width - 3, searchPanel.Height - 3);
                }
                using (var bgBrush = new SolidBrush(Color.FromArgb(252, 252, 252)))
                {
                    e.Graphics.FillRectangle(bgBrush, 0, 0, searchPanel.Width, searchPanel.Height);
                }
                using (var borderPen = new Pen(Color.FromArgb(220, 220, 220), 1))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, searchPanel.Width - 1, searchPanel.Height - 1);
                }
            };
            Panel searchContainer = new Panel
            {
                Location = new Point(20, 15),
                Size = new Size(320, 40),
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.None
            };
            Label searchIcon = new Label
            {
                Text = "ðŸ”",
                Font = new Font("Segoe UI", 12F),
                Location = new Point(10, 10),
                Size = new Size(25, 20),
                ForeColor = Color.FromArgb(150, 150, 150),
                TextAlign = ContentAlignment.MiddleCenter
            };
            searchContainer.Controls.Add(searchIcon);
            TextBox txtSearchFines = new TextBox
            {
                Location = new Point(40, 8),
                Size = new Size(260, 24),
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(245, 245, 245)
            };
            txtSearchFines.SetPlaceholder("Search fines...");
            searchContainer.Controls.Add(txtSearchFines);
            DataGridView dgvFines = new DataGridView
            {
                Location = new Point(30, 340),
                Size = new Size(pnlMainContent.Width - 60, pnlMainContent.Height - 370),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            dgvFines.Columns.Add("FineId", "ID");
            dgvFines.Columns.Add("MemberName", "Member");
            dgvFines.Columns.Add("BookTitle", "Book");
            dgvFines.Columns.Add("Amount", "Amount");
            dgvFines.Columns.Add("Status", "Status");
            dgvFines.Columns.Add("DueDate", "Due Date");
            dgvFines.Columns["FineId"].Width = 80;
            dgvFines.Columns["MemberName"].Width = 200;
            dgvFines.Columns["BookTitle"].Width = 250;
            dgvFines.Columns["Amount"].Width = 100;
            dgvFines.Columns["Status"].Width = 120;
            dgvFines.Columns["DueDate"].Width = 120;
            dgvFines.Columns["FineId"].Visible = false;
            searchPanel.Controls.Add(searchContainer);
            pnlMainContent.Controls.AddRange(new Control[] { titleLabel, subtitleLabel, btnAddFine, statsPanel, searchPanel, dgvFines });
        }

        private void ShowInventoryView()
        {
            RestoreOriginalControls();
            ShowDashboardControls(false);
            ClearDynamicControls();
            Label titleLabel = new Label
            {
                Text = "Inventory Management",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(300, 70),
                ForeColor = Color.FromArgb(40, 40, 40),
                BackColor = Color.Transparent
            };
            Label subtitleLabel = new Label
            {
                Text = "Track and manage book copies, locations, and conditions",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Location = new Point(30, 95),
                Size = new Size(500, 25),
                ForeColor = Color.FromArgb(100, 100, 100),
                BackColor = Color.Transparent
            };
            Button btnExportInventory = new Button
            {
                Text = "Export Inventory",
                Location = new Point(pnlMainContent.Width - 180, 20),
                Size = new Size(150, 35),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = ThemeConstants.PrimaryMaroon,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnExportInventory.FlatAppearance.BorderSize = 0;
            btnExportInventory.Click += (s, e) => ShowFeatureMessage("Export", "Export functionality will be implemented.");
            Panel statsPanel = new Panel
            {
                Location = new Point(30, 150),
                Size = new Size(pnlMainContent.Width - 60, 160),
                BackColor = Color.Transparent,
                Tag = "InventoryStatsPanel"
            };
            Panel cardTotalTitles = CreateCatalogStatCard("ðŸ“š", "0", "Total Titles", ThemeConstants.PrimaryMaroon, new Point(0, 0));
            Panel cardTotalCopies = CreateCatalogStatCard("ðŸ“–", "0", "Total Copies", Color.White, new Point(250, 0));
            Panel cardAvailable = CreateCatalogStatCard("âœ“", "0", "Available", Color.FromArgb(76, 175, 80), new Point(500, 0));
            Panel cardBorrowed = CreateCatalogStatCard("ðŸ“—", "0", "Borrowed", Color.FromArgb(33, 150, 243), new Point(0, 80));
            Panel cardDamaged = CreateCatalogStatCard("âš ", "0", "Damaged", Color.FromArgb(255, 193, 7), new Point(250, 80));
            Panel cardLost = CreateCatalogStatCard("âŒ", "0", "Lost", Color.FromArgb(244, 67, 54), new Point(500, 80));
            statsPanel.Controls.AddRange(new Control[] { cardTotalTitles, cardTotalCopies, cardAvailable, cardBorrowed, cardDamaged, cardLost });
            Panel searchPanel = new Panel
            {
                Location = new Point(30, 330),
                Size = new Size(pnlMainContent.Width - 60, 70),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            searchPanel.Paint += (s, e) =>
            {
                using (var shadowBrush = new SolidBrush(Color.FromArgb(15, 0, 0, 0)))
                {
                    e.Graphics.FillRectangle(shadowBrush, 3, 3, searchPanel.Width - 3, searchPanel.Height - 3);
                }
                using (var bgBrush = new SolidBrush(Color.White))
                {
                    e.Graphics.FillRectangle(bgBrush, 0, 0, searchPanel.Width, searchPanel.Height);
                }
                using (var borderPen = new Pen(Color.FromArgb(220, 220, 220), 1))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, searchPanel.Width - 1, searchPanel.Height - 1);
                }
            };
            Panel searchContainer = new Panel
            {
                Location = new Point(20, 15),
                Size = new Size(320, 40),
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.None
            };
            Label searchIcon = new Label
            {
                Text = "ðŸ”",
                Font = new Font("Segoe UI", 12F),
                Location = new Point(10, 10),
                Size = new Size(25, 20),
                ForeColor = Color.FromArgb(150, 150, 150),
                TextAlign = ContentAlignment.MiddleCenter
            };
            TextBox txtSearchInventory = new TextBox
            {
                Location = new Point(40, 8),
                Size = new Size(260, 24),
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(245, 245, 245)
            };
            txtSearchInventory.SetPlaceholder("Search inventory...");
            searchContainer.Controls.AddRange(new Control[] { searchIcon, txtSearchInventory });
            DataGridView dgvInventory = new DataGridView
            {
                Location = new Point(30, 420),
                Size = new Size(pnlMainContent.Width - 60, pnlMainContent.Height - 450),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            dgvInventory.Columns.Add("AccessionNumber", "Accession #");
            dgvInventory.Columns.Add("BookTitle", "Book Title");
            dgvInventory.Columns.Add("Location", "Location");
            dgvInventory.Columns.Add("Condition", "Condition");
            dgvInventory.Columns.Add("Status", "Status");
            dgvInventory.Columns["AccessionNumber"].Width = 150;
            dgvInventory.Columns["BookTitle"].Width = 300;
            dgvInventory.Columns["Location"].Width = 200;
            dgvInventory.Columns["Condition"].Width = 120;
            dgvInventory.Columns["Status"].Width = 120;
            searchPanel.Controls.Add(searchContainer);
            pnlMainContent.Controls.AddRange(new Control[] { titleLabel, subtitleLabel, btnExportInventory, statsPanel, searchPanel, dgvInventory });
        }

        private FlowLayoutPanel flpReportsContent;
        private Panel pnlReportsTabs;

        private void ShowReportsView()
        {
            RestoreOriginalControls();
            ShowDashboardControls(false);
            ClearDynamicControls();

            // Main Container
            Panel pnlReportsMain = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(30, 20, 30, 20)
            };
            pnlMainContent.Controls.Add(pnlReportsMain);

            // 1. Header Section
            Panel pnlHeader = new Panel { Height = 80, Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 20) };
            
            Label lblTitle = new Label 
            { 
                Text = "Reports & Analytics", 
                Font = new Font("Georgia", 24, FontStyle.Bold), 
                Location = new Point(0, 0), 
                AutoSize = true,
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            Label lblSub = new Label 
            { 
                Text = "Generate and view library statistics and reports", 
                Font = new Font("Segoe UI", 10), 
                Location = new Point(2, 40), 
                AutoSize = true,
                ForeColor = Color.Gray 
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            // Header Buttons
             Button btnExport = new Button 
            { 
                Text = "ðŸ“¥  Export", 
                Size = new Size(100, 36), 
                Location = new Point(pnlHeader.Width - 100, 5), 
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.FromArgb(120, 20, 40), 
                ForeColor = Color.White, 
                Font = ThemeConstants.FontButton,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnExport.Width, btnExport.Height), 6))
                 using(SolidBrush brush = new SolidBrush(Color.FromArgb(120, 20, 40))) 
                 {
                     e.Graphics.FillPath(brush, path);
                     TextRenderer.DrawText(e.Graphics, btnExport.Text, btnExport.Font, new Rectangle(0,0,btnExport.Width,btnExport.Height), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                 }
            };

            Button btnPrint = new Button 
            { 
                Text = "ðŸ–¨  Print", 
                Size = new Size(90, 36), 
                Location = new Point(pnlHeader.Width - 200, 5), 
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60), 
                Font = ThemeConstants.FontButton,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnPrint.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnPrint.Width-1, btnPrint.Height-1), 6))
                 using(Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1)) 
                 {
                     e.Graphics.DrawPath(pen, path);
                     TextRenderer.DrawText(e.Graphics, btnPrint.Text, btnPrint.Font, new Rectangle(0,0,btnPrint.Width,btnPrint.Height), Color.FromArgb(60,60,60), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                 }
            };
            
            Panel pnlDate = new Panel { Size = new Size(140, 36), Location = new Point(pnlHeader.Width - 350, 5), BackColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            ComboBox cmbDate = new ComboBox { FlatStyle = FlatStyle.Flat, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbDate.Items.Add("ðŸ“…  Last 7 days");
            cmbDate.SelectedIndex = 0;
            pnlDate.Controls.Add(cmbDate);
            pnlDate.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlDate.Width-1, pnlDate.Height-1), 6))
                 using(Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1)) e.Graphics.DrawPath(pen, path);
            };

            pnlHeader.Controls.Add(btnExport);
            pnlHeader.Controls.Add(btnPrint);
            pnlHeader.Controls.Add(pnlDate);
            pnlReportsMain.Controls.Add(pnlHeader);

            // 2. Tabs Section
            pnlReportsTabs = new Panel { Height = 40, Dock = DockStyle.Top, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 20) };
            pnlReportsMain.Controls.Add(pnlReportsTabs);

            // 3. Content Section (Scrollable)
            flpReportsContent = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 20, 0, 20)
            };
            pnlReportsMain.Controls.Add(flpReportsContent);
            flpReportsContent.BringToFront();

            // Initialize Tabs
            SetupReportsTabs();
            
            // Layout Fixes
            pnlReportsMain.Resize += (s, e) => {
                 if(flpReportsContent.Controls.Count > 0)
                 {
                     foreach(Control c in flpReportsContent.Controls)
                         c.Width = flpReportsContent.ClientSize.Width - 20;
                 }
            };
        }

        private void SetupReportsTabs()
        {
            string[] tabs = { "Circulation", "Members", "Collection", "Fines" };
            pnlReportsTabs.Controls.Clear();
            int tabW = 120; // Fixed width for consistent look
            int tabX = 0;

            foreach(var tab in tabs)
            {
                Button btnTab = new Button 
                { 
                    Text = tab, 
                    TextAlign = ContentAlignment.MiddleCenter, 
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Color.Gray,
                    Size = new Size(tabW, 40),
                    Location = new Point(tabX, 0),
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Tag = tab
                };
                btnTab.FlatAppearance.BorderSize = 0;
                btnTab.FlatAppearance.MouseDownBackColor = Color.Transparent;
                btnTab.FlatAppearance.MouseOverBackColor = Color.Transparent;
                
                btnTab.Click += (s, e) => SwitchReportTab(tab, btnTab);

                pnlReportsTabs.Controls.Add(btnTab);
                tabX += tabW;
            }
            
            // Draw bottom line
            Panel pnlLine = new Panel { Height = 2, Dock = DockStyle.Bottom, BackColor = Color.FromArgb(230,230,230) };
            pnlReportsTabs.Controls.Add(pnlLine); // Add first so it's behind

            // Default
            SwitchReportTab("Circulation", pnlReportsTabs.Controls[0] as Button);
        }

        private void SwitchReportTab(string tabName, Button activeBtn)
        {
            // Reset styles
            foreach(Control c in pnlReportsTabs.Controls)
            {
                if(c is Button b)
                {
                    b.Font = new Font("Segoe UI", 10, FontStyle.Regular);
                    b.ForeColor = Color.Gray;
                    // b.Invalidate(); // To remove custom paint if any
                }
            }

            // Set Active
            if(activeBtn != null)
            {
                activeBtn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                activeBtn.ForeColor = ThemeConstants.PrimaryMaroon;
                
                // Add active indicator (custom paint on the container or the button)
                // Simplified: Just Bold/Color for now, or add a line panel
            }

            // Load Content
            flpReportsContent.Controls.Clear();
            switch(tabName)
            {
                case "Circulation": ShowReportsCirculation(); break;
                case "Members": ShowReportsMembers(); break;
                case "Collection": ShowReportsCollection(); break;
                case "Fines": ShowReportsFines(); break;
            }
        }

        private void ShowReportsCirculation()
        {
             // 1. KPI Section
            TableLayoutPanel tlpKPI = new TableLayoutPanel
            {
                Height = 100, 
                ColumnCount = 5,
                RowCount = 1,
                BackColor = Color.Transparent,
                Width = flpReportsContent.ClientSize.Width - 10,
                Margin = new Padding(0, 0, 0, 20)
            };
            tlpKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tlpKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tlpKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tlpKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tlpKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            
            tlpKPI.Controls.Add(CreateReportStatCard("Total Collection", "2,450", "ðŸ“–", Color.White, Color.Black, false), 0, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Active Members", "142", "ðŸ‘¥", Color.White, Color.Black, false), 1, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Transactions", "38", "â†—", Color.White, Color.Black, false), 2, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Fines Collected", "$1,250", "ðŸ’²", Color.White, Color.Black, false), 3, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Overdue", "12", "ðŸ“…", Color.White, Color.Black, false), 4, 0);
            flpReportsContent.Controls.Add(tlpKPI);

            // 2. Charts Section (Daily Circulation + Top Books)
            TableLayoutPanel tlpCharts = new TableLayoutPanel
            {
                Height = 350,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Width = flpReportsContent.ClientSize.Width - 10,
                Margin = new Padding(0, 0, 0, 20)
            };
            tlpCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tlpCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            Panel pnlChart = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 20, 0) };
            SetupChart(pnlChart, "Daily Circulation Trends");
            tlpCharts.Controls.Add(pnlChart, 0, 0);

            Panel pnlTopBooks = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 0, 0) };
            SetupDistributionChart(pnlTopBooks, "Most Borrowed Books", new Dictionary<string, int> { 
                {"The Great Gatsby", 45}, {"1984", 32}, {"To Kill a Mockingbird", 28}, {"Pride and Prejudice", 25}, {"The Hobbit", 20} 
            }); 
            tlpCharts.Controls.Add(pnlTopBooks, 1, 0);
            flpReportsContent.Controls.Add(tlpCharts);

            // 3. Overdue Grid
            Panel pnlGrid = new Panel { Height = 300, Width = flpReportsContent.ClientSize.Width - 10, BackColor = Color.White, Margin = new Padding(0, 0, 0, 30) };
             Label lblGridTitle = new Label { Text = "Overdue Books", Font = new Font("Georgia", 14, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true };
             pnlGrid.Controls.Add(lblGridTitle);
             
             DataGridView dgv = CreateReportGrid();
             dgv.Columns.Add("Book", "Book");
             dgv.Columns.Add("Member", "Member");
             dgv.Columns.Add("BorrowDate", "Borrow Date");
             dgv.Columns.Add("DueDate", "Due Date");
             dgv.Columns.Add("Status", "Status");
             
             dgv.Rows.Add("Pride and Prejudice", "Michael Brown", "Oct 15, 2024", "Nov 1, 2024", "Overdue (45 days)");
             dgv.Rows.Add("The Catcher in the Rye", "Sarah Wilson", "Oct 20, 2024", "Nov 5, 2024", "Overdue (40 days)");
             dgv.Rows.Add("Brave New World", "David Lee", "Nov 1, 2024", "Nov 15, 2024", "Overdue (30 days)");
             
             pnlGrid.Controls.Add(dgv);
             flpReportsContent.Controls.Add(pnlGrid);
        }

        private void ShowReportsMembers()
        {
             // 1. KPI Section
            TableLayoutPanel tlpKPI = new TableLayoutPanel
            {
                Height = 100, 
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Width = flpReportsContent.ClientSize.Width - 10,
                Margin = new Padding(0, 0, 0, 20)
            };
            tlpKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            
            tlpKPI.Controls.Add(CreateReportStatCard("Total Members", "524", "ðŸ‘¥", Color.White, Color.Black, false), 0, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("New This Month", "32", "âž•", Color.White, Color.Green, false), 1, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Active Users", "412", "âš¡", Color.White, Color.Blue, false), 2, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Suspended", "8", "â›”", Color.White, Color.Red, false), 3, 0);
            flpReportsContent.Controls.Add(tlpKPI);

            // 2. Charts Section
            TableLayoutPanel tlpCharts = new TableLayoutPanel
            {
                Height = 350,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Width = flpReportsContent.ClientSize.Width - 10,
                Margin = new Padding(0, 0, 0, 20)
            };
            tlpCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            Panel pnlTypeChart = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 20, 0) };
            SetupDistributionChart(pnlTypeChart, "Member Distribution by Type", new Dictionary<string, int> { 
                {"Student", 350}, {"Faculty", 80}, {"Staff", 50}, {"Guest", 44} 
            });
            tlpCharts.Controls.Add(pnlTypeChart, 0, 0);

            Panel pnlStatusChart = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 0, 0) };
             SetupDistributionChart(pnlStatusChart, "Membership Status", new Dictionary<string, int> { 
                {"Active", 412}, {"Expired", 80}, {"Suspended", 8}, {"Pending", 24} 
            });
            tlpCharts.Controls.Add(pnlStatusChart, 1, 0);
            flpReportsContent.Controls.Add(tlpCharts);

             // 3. Grid
            Panel pnlGrid = new Panel { Height = 300, Width = flpReportsContent.ClientSize.Width - 10, BackColor = Color.White, Margin = new Padding(0, 0, 0, 30) };
             Label lblGridTitle = new Label { Text = "Recent Member Activity", Font = new Font("Georgia", 14, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true };
             pnlGrid.Controls.Add(lblGridTitle);
             
             DataGridView dgv = CreateReportGrid();
             dgv.Columns.Add("MemberId", "ID");
             dgv.Columns.Add("Name", "Name");
             dgv.Columns.Add("Activity", "Activity");
             dgv.Columns.Add("Date", "Date");
             
             dgv.Rows.Add("MEM-2024-001", "John Doe", "Borrowed 'Clean Code'", "Just now");
             dgv.Rows.Add("MEM-2024-045", "Alice Smith", "Returned 'Design Patterns'", "1 hour ago");
             dgv.Rows.Add("MEM-2023-112", "Bob Jones", "Paid Fine ($5.00)", "2 hours ago");
             
             pnlGrid.Controls.Add(dgv);
             flpReportsContent.Controls.Add(pnlGrid);
        }

        private void ShowReportsCollection()
        {
             // 1. KPI Section
            TableLayoutPanel tlpKPI = new TableLayoutPanel
            {
                Height = 100, 
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Width = flpReportsContent.ClientSize.Width - 10,
                Margin = new Padding(0, 0, 0, 20)
            };
            tlpKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            
            tlpKPI.Controls.Add(CreateReportStatCard("Total Titles", "12,450", "ðŸ“š", Color.White, Color.Black, false), 0, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Total Copies", "15,200", "ðŸ“–", Color.White, Color.Black, false), 1, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Available", "11,500", "âœ…", Color.White, Color.Green, false), 2, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Lost/Damaged", "45", "âŒ", Color.White, Color.Red, false), 3, 0);
            flpReportsContent.Controls.Add(tlpKPI);

            // 2. Charts Section
            TableLayoutPanel tlpCharts = new TableLayoutPanel
            {
                Height = 350,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Width = flpReportsContent.ClientSize.Width - 10,
                Margin = new Padding(0, 0, 0, 20)
            };
            tlpCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tlpCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            Panel pnlCatChart = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 20, 0) };
             SetupDistributionChart(pnlCatChart, "Collection by Category", new Dictionary<string, int> { 
                {"Fiction", 4500}, {"Science", 2500}, {"History", 1500}, {"Technology", 2000}, {"Arts", 1000}, {"Others", 950}
            });
            tlpCharts.Controls.Add(pnlCatChart, 0, 0);

            Panel pnlTopChart = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 0, 0) };
             SetupDistributionChart(pnlTopChart, "Most Popular Categories", new Dictionary<string, int> { 
                 {"Fiction", 120}, {"Technology", 95}, {"Science", 80}, {"History", 45}
            });
            tlpCharts.Controls.Add(pnlTopChart, 1, 0);
            flpReportsContent.Controls.Add(tlpCharts);

             // 3. Grid
            Panel pnlGrid = new Panel { Height = 300, Width = flpReportsContent.ClientSize.Width - 10, BackColor = Color.White, Margin = new Padding(0, 0, 0, 30) };
             Label lblGridTitle = new Label { Text = "Recent Acquisitions", Font = new Font("Georgia", 14, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true };
             pnlGrid.Controls.Add(lblGridTitle);
             
             DataGridView dgv = CreateReportGrid();
             dgv.Columns.Add("Id", "Accession #");
             dgv.Columns.Add("Title", "Title");
             dgv.Columns.Add("Category", "Category");
             dgv.Columns.Add("DateAdded", "Date Added");
             
             dgv.Rows.Add("ACC-2024-501", "Advanced AI Algorithms", "Technology", "Dec 10, 2024");
             dgv.Rows.Add("ACC-2024-502", "Modern History of Europe", "History", "Dec 12, 2024");
             dgv.Rows.Add("ACC-2024-503", "Quick Recipes", "Lifestyle", "Dec 14, 2024");
             
             pnlGrid.Controls.Add(dgv);
             flpReportsContent.Controls.Add(pnlGrid);
        }

        private void ShowReportsFines()
        {
             // 1. KPI Section
            TableLayoutPanel tlpKPI = new TableLayoutPanel
            {
                Height = 100, 
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Width = flpReportsContent.ClientSize.Width - 10,
                Margin = new Padding(0, 0, 0, 20)
            };
            tlpKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKPI.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            
            tlpKPI.Controls.Add(CreateReportStatCard("Total Collected", "$12,450", "ðŸ’°", Color.White, Color.Green, false), 0, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Pending Fines", "$2,100", "âš ï¸", Color.White, Color.Orange, false), 1, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Waived", "$540", "ðŸ‘‹", Color.White, Color.Gray, false), 2, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Overdue Cases", "45", "âš–ï¸", Color.White, Color.Red, false), 3, 0);
            flpReportsContent.Controls.Add(tlpKPI);

            // 2. Charts Section
            TableLayoutPanel tlpCharts = new TableLayoutPanel
            {
                Height = 350,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Width = flpReportsContent.ClientSize.Width - 10,
                Margin = new Padding(0, 0, 0, 20)
            };
            tlpCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tlpCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            Panel pnlTrendChart = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 20, 0) };
             SetupChart(pnlTrendChart, "Daily Revenue (Fines)"); // Reusing the spline chart for now
            tlpCharts.Controls.Add(pnlTrendChart, 0, 0);

            Panel pnlTypeChart = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 0, 0) };
             SetupDistributionChart(pnlTypeChart, "Fines by Reason", new Dictionary<string, int> { 
                 {"Late Return", 120}, {"Lost Book", 15}, {"Damaged Book", 8}, {"Other", 5}
            });
            tlpCharts.Controls.Add(pnlTypeChart, 1, 0);
            flpReportsContent.Controls.Add(tlpCharts);

             // 3. Grid
            Panel pnlGrid = new Panel { Height = 300, Width = flpReportsContent.ClientSize.Width - 10, BackColor = Color.White, Margin = new Padding(0, 0, 0, 30) };
             Label lblGridTitle = new Label { Text = "Unpaid Fines", Font = new Font("Georgia", 14, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true };
             pnlGrid.Controls.Add(lblGridTitle);
             
             DataGridView dgv = CreateReportGrid();
             dgv.Columns.Add("Member", "Member");
             dgv.Columns.Add("Reason", "Reason");
             dgv.Columns.Add("Amount", "Amount");
             dgv.Columns.Add("Status", "Status");
             
             dgv.Rows.Add("John Doe", "Late Return - Clean Code", "$5.00", "Pending");
             dgv.Rows.Add("Jane Smith", "Damaged - History 101", "$15.00", "Pending");
             dgv.Rows.Add("Mike Ross", "Lost - Law Basics", "$50.00", "Overdue");
             
             pnlGrid.Controls.Add(dgv);
             flpReportsContent.Controls.Add(pnlGrid);
        }

        private DataGridView CreateReportGrid()
        {
            DataGridView dgv = new DataGridView 
            { 
                Location = new Point(0, 60), 
                Size = new Size(1100, 240),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 40 },
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle {
                BackColor = Color.White,
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                SelectionBackColor = Color.White,
                SelectionForeColor = Color.DimGray,
                Padding = new Padding(10)
            };
            dgv.DefaultCellStyle = new DataGridViewCellStyle {
                BackColor = Color.White,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 10),
                Padding = new Padding(10),
                SelectionBackColor = Color.FromArgb(250, 240, 240),
                SelectionForeColor = Color.Black
            };
            dgv.GridColor = Color.FromArgb(240, 240, 240);
            return dgv;
        }

        private void SetupDistributionChart(Panel pnl, string title, Dictionary<string, int> data)
        {
             // Header
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.Transparent };
            Label lblTitle = new Label { Text = title, Font = new Font("Georgia", 14, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true };
            pnlHeader.Controls.Add(lblTitle);
            pnl.Controls.Add(pnlHeader);

            Chart chart = new Chart();
            chart.Dock = DockStyle.Fill;
            
            ChartArea ca = new ChartArea();
            ca.Name = "MainArea";
            ca.BackColor = Color.White;
            chart.ChartAreas.Add(ca);

            Series s1 = new Series
            {
                Name = "Data",
                ChartType = SeriesChartType.Doughnut
            };
            
            foreach(var kvp in data)
            {
                s1.Points.AddXY(kvp.Key, kvp.Value);
            }
            
            chart.Series.Add(s1);
            pnl.Controls.Add(chart);
            chart.BringToFront();

            pnl.Paint += (s, e) => {
                 using(Pen borderPen = new Pen(Color.FromArgb(230, 230, 230))) 
                    e.Graphics.DrawRectangle(borderPen, 0, 0, pnl.Width - 1, pnl.Height - 1);
            };
        }




        private void ShowSearchView()
        {
            RestoreOriginalControls();
            ShowDashboardControls(false);
            if (pnlSearchView == null || pnlSearchView.IsDisposed)
            {
                SetupSearchView();
            }
            if (!pnlMainContent.Controls.Contains(pnlSearchView))
            {
                pnlMainContent.Controls.Add(pnlSearchView);
            }
            pnlSearchView.Visible = true;
            pnlSearchView.BringToFront();
            pnlSearchView.Dock = DockStyle.Fill;
        }

        private void SetupSearchView()
        {
            if (pnlSearchView != null && !pnlSearchView.IsDisposed) return;
            pnlSearchView = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeConstants.BackgroundLight,
                Padding = new Padding(30)
            };
            Label lblTitle = new Label
            {
                Text = "Search & Discovery",
                Font = new Font("Georgia", 20F, FontStyle.Bold),
                ForeColor = ThemeConstants.PrimaryMaroon,
                AutoSize = true,
                Location = new Point(0, 0),
                BackColor = Color.Transparent
            };
            Label lblSubtitle = new Label
            {
                Text = "Find books, resources, and check availability",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(2, 35),
                BackColor = Color.Transparent
            };
            Panel pnlSearchBar = new Panel
            {
                Size = new Size(pnlSearchView.Width - 60, 60),
                Location = new Point(0, 70),
                BackColor = Color.White,
                Padding = new Padding(10),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlSearchBar.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Color.LightGray))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlSearchBar.Width - 1, pnlSearchBar.Height - 1);
                }
            };
            btnTriggerSearch = new Button
            {
                Text = "ðŸ” Search",
                Size = new Size(100, 40),
                BackColor = ThemeConstants.PrimaryMaroon,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTriggerSearch.FlatAppearance.BorderSize = 0;
            btnSearchFilters = new Button
            {
                Text = "All Categories  â–¼",
                Size = new Size(100, 40),
                BackColor = Color.White,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
            btnSearchFilters.FlatAppearance.BorderSize = 0;
            txtSearchInput = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 12F),
                ForeColor = Color.Gray,
                Text = "Search by title, author, ISBN, subject...",
                Location = new Point(40, 18),
                Width = 500,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            Label lblSearchIcon = new Label { Text = "ðŸ”", Font = new Font("Segoe UI", 12F), Location = new Point(10, 15), AutoSize = true, ForeColor = Color.Gray, BackColor = Color.Transparent };
            pnlSearchBar.Resize += (s, e) =>
            {
                txtSearchInput.Width = pnlSearchBar.Width - 260;
            };
            txtSearchInput.Enter += (s, e) =>
            {
                if (txtSearchInput.Text == "Search by title, author, ISBN, subject...")
                {
                    txtSearchInput.Text = "";
                    txtSearchInput.ForeColor = Color.Black;
                }
            };
            txtSearchInput.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSearchInput.Text))
                {
                    txtSearchInput.Text = "Search by title, author, ISBN, subject...";
                    txtSearchInput.ForeColor = Color.Gray;
                }
            };
            pnlSearchBar.Controls.Add(txtSearchInput);
            pnlSearchBar.Controls.Add(lblSearchIcon);
            pnlSearchBar.Controls.Add(btnSearchFilters);
            pnlSearchBar.Controls.Add(btnTriggerSearch);
            pnlSearchContent = new Panel
            {
                Location = new Point(0, 150),
                Size = new Size(pnlSearchView.Width - 60, 500),
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlSearchContent.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Color.LightGray))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlSearchContent.Width - 1, pnlSearchContent.Height - 1);
                }
            };
            Label lblEmptyIcon = new Label
            {
                Text = "ðŸ”",
                Font = new Font("Segoe UI", 60F),
                ForeColor = Color.LightGray,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Label lblEmptyTitle = new Label
            {
                Text = "Start your search",
                Font = new Font("Georgia", 16F, FontStyle.Bold),
                ForeColor = ThemeConstants.PrimaryMaroon,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            Label lblEmptySub = new Label
            {
                Text = "Enter a search term to find books in the library catalog",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Gray,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlSearchContent.Resize += (s, e) =>
            {
                int cx = pnlSearchContent.Width / 2;
                int cy = pnlSearchContent.Height / 2;
                lblEmptyIcon.Location = new Point(cx - lblEmptyIcon.Width / 2, cy - 80);
                lblEmptyTitle.Location = new Point(cx - lblEmptyTitle.Width / 2, cy + 20);
                lblEmptySub.Location = new Point(cx - lblEmptySub.Width / 2, cy + 50);
            };
            btnTriggerSearch.Click += (s, e) => ShowFeatureMessage("Search", "Search functionality will be implemented.");
            txtSearchInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    ShowFeatureMessage("Search", "Search functionality will be implemented.");
                }
            };
            pnlSearchContent.Controls.AddRange(new Control[] { lblEmptyIcon, lblEmptyTitle, lblEmptySub });
            pnlSearchView.Controls.Add(lblTitle);
            pnlSearchView.Controls.Add(lblSubtitle);
            pnlSearchView.Controls.Add(pnlSearchBar);
            pnlSearchView.Controls.Add(pnlSearchContent);
        }


        private Panel CreateCatalogStatCard(string icon, string value, string label, Color accentColor, Point location)
        {
            Panel card = new Panel
            {
                Location = location,
                Size = new Size(200, 70),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle shadowRect = new Rectangle(2, 2, card.Width - 2, card.Height - 2);
                Rectangle cardRect = new Rectangle(0, 0, card.Width - 2, card.Height - 2);
                using (var shadowBrush = new SolidBrush(Color.FromArgb(8, 0, 0, 0)))
                using (var shadowPath = CreateRoundedRectangle(shadowRect, 4))
                {
                    e.Graphics.FillPath(shadowBrush, shadowPath);
                }
                using (var bgBrush = new SolidBrush(Color.White))
                using (var cardPath = CreateRoundedRectangle(cardRect, 4))
                {
                    e.Graphics.FillPath(bgBrush, cardPath);
                }
                using (var borderPen = new Pen(Color.FromArgb(230, 230, 230), 1))
                using (var borderPath = CreateRoundedRectangle(cardRect, 4))
                {
                    e.Graphics.DrawPath(borderPen, borderPath);
                }
            };
            Label lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 20F),
                Location = new Point(15, 15),
                Size = new Size(40, 40),
                ForeColor = accentColor,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            Label lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                Location = new Point(65, 5),
                Size = new Size(125, 35),
                ForeColor = Color.FromArgb(40, 40, 40),
                TextAlign = ContentAlignment.MiddleLeft,
                Tag = "Value",
                BackColor = Color.Transparent
            };
            Label lblLabel = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 9F),
                Location = new Point(65, 42),
                Size = new Size(125, 20),
                ForeColor = Color.FromArgb(100, 100, 100),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            card.Controls.AddRange(new Control[] { lblIcon, lblValue, lblLabel });
            return card;
        }

        private void UpdateCatalogStatCard(Panel card, string newValue)
        {
            foreach (Control ctrl in card.Controls)
            {
                if (ctrl.Tag?.ToString() == "Value" && ctrl is Label lbl)
                {
                    lbl.Text = newValue;
                    break;
                }
            }
        }

        private void ShowFeatureMessage(string title, string message)
        {
            MessageBox.Show(message, 
                $"{title} - Coming Soon", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Information);
        }

        private void StaffDashboardForm_Load(object sender, EventArgs e)
        {
            LoadUserInfo();
            LoadLogo();
            LoadDashboardData();
            UpdateDate();
        }

        private void LoadUserInfo()
        {
            if (SignInForm.CurrentUser != null)
            {
                lblWelcome.Text = $"Welcome back, {SignInForm.CurrentUser.FullName}";
                lblStaffName.Text = SignInForm.CurrentUser.FullName;
            }
        }

        private void LoadLogo()
        {
            string logoPath = Path.Combine(Application.StartupPath, "Resources", "logo.png");
            if (File.Exists(logoPath))
            {
                picLogo.Image = Image.FromFile(logoPath);
            }
        }

        private void LoadDashboardData()
        {
            // TODO: Load actual data from database
            // For now, using placeholder values
            lblTotalBooks.Text = "0";
            lblTotalBooksChange.Text = "+0% from last week";
            lblActiveMembers.Text = "0";
            lblActiveMembersChange.Text = "+0% from last week";
            lblBooksBorrowed.Text = "0";
            lblBooksBorrowedChange.Text = "+0% from last week";
            lblOverdueBooks.Text = "0";
            lblOverdueBooksChange.Text = "+0% from last week";
            lblTodaysBorrowings.Text = "0";
            lblTodaysReturns.Text = "0";
            lblPendingFines.Text = "0";
        }

        private void UpdateDate()
        {
            lblDate.Text = DateTime.Now.ToString("MMM d, yyyy");
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Are you sure you want to sign out?",
                "Confirm Sign Out",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                SignInForm.ClearCurrentUser();
                this.Close();
            }
        }
        private void ShowAddBookDialog()
        {
            Form addBookForm = new Form();
            addBookForm.Size = new Size(600, 750); // Adjusted height
            addBookForm.FormBorderStyle = FormBorderStyle.None;
            addBookForm.StartPosition = FormStartPosition.CenterParent;
            addBookForm.BackColor = Color.White;
            addBookForm.ShowInTaskbar = false;

            // Border Point
            addBookForm.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 using (Pen p = new Pen(Color.FromArgb(200, 200, 200), 1))
                 {
                     e.Graphics.DrawRectangle(p, 0, 0, addBookForm.Width - 1, addBookForm.Height - 1);
                 }
            };

            // Close
            Label btnClose = new Label { Text = "Ã—", Font = new Font("Arial", 18), ForeColor = Color.Gray, Location = new Point(addBookForm.Width - 40, 10), Size = new Size(30, 30), Cursor = Cursors.Hand };
            btnClose.Click += (s, e) => addBookForm.Close();
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.Black;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.Gray;
            addBookForm.Controls.Add(btnClose);

            // Title
            Label lblTitle = new Label { Text = "Add New Book", Font = new Font("Georgia", 16, FontStyle.Bold), ForeColor = Color.FromArgb(40, 40, 40), Location = new Point(30, 30), AutoSize = true };
            addBookForm.Controls.Add(lblTitle);

            Label lblSubtitle = new Label { Text = "Enter the book details to add it to the catalog", Font = new Font("Segoe UI", 10), ForeColor = Color.Gray, Location = new Point(32, 60), AutoSize = true };
            addBookForm.Controls.Add(lblSubtitle);

            // Helper for Inputs
             Func<string, int, int, int, Control> AddInput = (placeholder, x, posY, w) => {
                Panel pnl = new Panel();
                pnl.Location = new Point(x, posY + 25);
                pnl.Size = new Size(w, 40); // Slightly compact
                pnl.BackColor = Color.White;
                pnl.Padding = new Padding(10, 8, 10, 5);
                
                TextBox tb = new TextBox();
                tb.BorderStyle = BorderStyle.None;
                tb.Font = new Font("Segoe UI", 10F);
                tb.Dock = DockStyle.Fill;
                tb.BackColor = Color.White;
                if(!string.IsNullOrEmpty(placeholder)) tb.SetPlaceholder(placeholder); 

                pnl.Controls.Add(tb);
                addBookForm.Controls.Add(pnl);
                
                pnl.Paint += (s, e) => {
                     e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                     bool isFocused = (pnl.Tag as string == "Focused");
                     Color borderColor = isFocused ? Color.Maroon : Color.FromArgb(220, 220, 220);
                     float width = isFocused ? 1.5f : 1f;
                     using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnl.Width - 3, pnl.Height - 3), 6))
                     using(Pen pen = new Pen(borderColor, width))
                     {
                         e.Graphics.DrawPath(pen, path);
                     }
                };
                tb.Enter += (s, e) => { pnl.Tag = "Focused"; pnl.Invalidate(); };
                tb.Leave += (s, e) => { pnl.Tag = ""; pnl.Invalidate(); };
                return tb;
            };

            // Helper for Numeric Inputs (Pages, Copies)
            Func<string, int, int, int, Control> AddNumericInput = (placeholder, x, posY, w) => {
                Panel pnl = new Panel();
                pnl.Location = new Point(x, posY + 25);
                pnl.Size = new Size(w, 40);
                pnl.BackColor = Color.White;
                pnl.Padding = new Padding(10, 8, 10, 5);

                TextBox tb = new TextBox();
                tb.BorderStyle = BorderStyle.None;
                tb.Font = new Font("Segoe UI", 10F);
                tb.Dock = DockStyle.Fill;
                tb.BackColor = Color.White;
                if(!string.IsNullOrEmpty(placeholder)) tb.SetPlaceholder(placeholder);

                // Restrict to numbers only
                tb.KeyPress += (s, e) => {
                    if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                    {
                        e.Handled = true;
                    }
                };

                pnl.Controls.Add(tb);
                addBookForm.Controls.Add(pnl);

                pnl.Paint += (s, e) => {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    bool isFocused = (pnl.Tag as string == "Focused");
                    Color borderColor = isFocused ? Color.Maroon : Color.FromArgb(220, 220, 220);
                    float width = isFocused ? 1.5f : 1f;
                    using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnl.Width - 3, pnl.Height - 3), 6))
                    using(Pen pen = new Pen(borderColor, width))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                };

                tb.Enter += (s, e) => { pnl.Tag = "Focused"; pnl.Invalidate(); };
                tb.Leave += (s, e) => { pnl.Tag = ""; pnl.Invalidate(); };

                return tb;
            };

            Action<string, int, int> AddLabel = (text, x, posY) => {
                Label l = new Label { Text = text, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 50), Location = new Point(x, posY), AutoSize = true };
                addBookForm.Controls.Add(l);
            };

            // Layout
            int y = 100;
            int col1 = 30;
            int col2 = 300;
            int w1 = 250;
            int wFull = 520;

            // Row 1
            AddLabel("Title *", col1, y);
            AddInput("Book title", col1, y, w1);
            AddLabel("Subtitle", col2, y);
            AddInput("Optional subtitle", col2, y, w1);
            y += 75;

            // Row 2
            AddLabel("ISBN *", col1, y);
            AddInput("978-0-00-000000-0", col1, y, w1);
            
            AddLabel("Category *", col2, y);
             // Custom Combo for Category
            Panel pnlCat = new Panel { Location = new Point(col2, y + 25), Size = new Size(w1, 40), BackColor = Color.White };
            ComboBox cmbCat = new ComboBox { FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10), Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCat.Items.Add("Enter Category");
            cmbCat.Items.AddRange(new string[] { "Fiction", "Non-Fiction", "Science", "Technology", "History", "Philosophy", "Arts", "Literature", "Business", "Education" });
            cmbCat.SelectedIndex = 0;
            cmbCat.ForeColor = Color.Gray;

            cmbCat.SelectedIndexChanged += (s, e) => {
                if (cmbCat.SelectedIndex == 0)
                {
                    cmbCat.ForeColor = Color.Gray;
                }
                else
                {
                    cmbCat.ForeColor = Color.Black;
                }
            };

            pnlCat.Controls.Add(cmbCat);
            addBookForm.Controls.Add(pnlCat);
             pnlCat.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnlCat.Width - 3, pnlCat.Height - 3), 6))
                 using(Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1)) e.Graphics.DrawPath(pen, path);
            };
            y += 75;

            // Row 3
            AddLabel("Author *", col1, y);
            AddInput("Author name", col1, y, w1);
            AddLabel("Publisher *", col2, y);
            AddInput("Publisher name", col2, y, w1);
            y += 75;

            // Row 4
            AddLabel("Publication Year", col1, y);
            AddNumericInput("2026", col1, y, 70);
            
            AddLabel("Pages", col1 + 90, y);
            AddNumericInput("0", col1 + 90, y, 70);

            AddLabel("Copies", col2, y);
            AddNumericInput("1", col2, y, w1);
            y += 75;

            // Row 5
            AddLabel("Language", col1, y);
            
            // Language Combo (allows typing)
            Panel pnlLanguage = new Panel { Location = new Point(col1, y + 25), Size = new Size(w1, 40), BackColor = Color.White };
            ComboBox cmbLanguage = new ComboBox { FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10), Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown };
            cmbLanguage.Items.AddRange(new string[] { "English", "Tagalog", "Cebuano", "Spanish" });
            cmbLanguage.Text = "English";
            pnlLanguage.Controls.Add(cmbLanguage);
            addBookForm.Controls.Add(pnlLanguage);
            pnlLanguage.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool isFocused = (pnlLanguage.Tag as string == "Focused");
                Color borderColor = isFocused ? Color.Maroon : Color.FromArgb(220, 220, 220);
                float width = isFocused ? 1.5f : 1f;
                using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnlLanguage.Width - 3, pnlLanguage.Height - 3), 6))
                using(Pen pen = new Pen(borderColor, width))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };
            cmbLanguage.Enter += (s, e) => { pnlLanguage.Tag = "Focused"; pnlLanguage.Invalidate(); };
            cmbLanguage.Leave += (s, e) => { pnlLanguage.Tag = ""; pnlLanguage.Invalidate(); };
            
            AddLabel("Location *", col2, y);
            AddInput("Section A, Shelf 1", col2, y, w1);
            y += 75;

            // Row 6
            AddLabel("Resource Type", col1, y);
            // Type Combo
            Panel pnlType = new Panel { Location = new Point(col1, y + 25), Size = new Size(wFull, 40), BackColor = Color.White };
            ComboBox cmbType = new ComboBox { FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10), Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbType.Items.Add("Enter Resource Type");
            cmbType.Items.AddRange(new string[] { "Book", "Periodical", "Thesis", "Audio-Visual", "Ebook" });
            cmbType.SelectedIndex = 0;
            cmbType.ForeColor = Color.Gray;

            cmbType.SelectedIndexChanged += (s, e) => {
                if (cmbType.SelectedIndex == 0)
                {
                    cmbType.ForeColor = Color.Gray;
                }
                else
                {
                    cmbType.ForeColor = Color.Black;
                }
            };

            pnlType.Controls.Add(cmbType);
            addBookForm.Controls.Add(pnlType);
            pnlType.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnlType.Width - 3, pnlType.Height - 3), 6))
                 using(Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1)) e.Graphics.DrawPath(pen, path);
            };
            y += 75;

             // Row 7 Description
            AddLabel("Description", col1, y);
            Panel pnlDesc = new Panel { Location = new Point(col1, y + 25), Size = new Size(wFull, 80), BackColor = Color.White, Padding = new Padding(10) };
            TextBox txtDesc = new TextBox { BorderStyle = BorderStyle.None, Multiline = true, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10) };
            txtDesc.SetPlaceholder("Brief description of the book...");
            pnlDesc.Controls.Add(txtDesc);
            addBookForm.Controls.Add(pnlDesc);
            pnlDesc.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 bool isFocused = (pnlDesc.Tag as string == "Focused");
                 Color borderColor = isFocused ? Color.Maroon : Color.FromArgb(220, 220, 220);
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnlDesc.Width - 3, pnlDesc.Height - 3), 6))
                 using(Pen pen = new Pen(borderColor, 1)) e.Graphics.DrawPath(pen, path);
            };
             txtDesc.Enter += (s, e) => { pnlDesc.Tag = "Focused"; pnlDesc.Invalidate(); };
             txtDesc.Leave += (s, e) => { pnlDesc.Tag = ""; pnlDesc.Invalidate(); };

            // Buttons
            Button btnCancel = new Button { Text = "Cancel", Size = new Size(100, 35), Location = new Point(addBookForm.Width - 240, addBookForm.Height - 50), FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Color.Black, Font = ThemeConstants.FontButton };
            btnCancel.Click += (s, e) => addBookForm.Close();

            Button btnAdd = new Button { Text = "Add Book", Size = new Size(120, 35), Location = new Point(addBookForm.Width - 130, addBookForm.Height - 50), FlatStyle = FlatStyle.Flat, BackColor = Color.Maroon, ForeColor = Color.White, Font = ThemeConstants.FontButton };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnAdd.Width, btnAdd.Height), 6))
                 using(SolidBrush brush = new SolidBrush(Color.Maroon))
                 {
                     e.Graphics.FillPath(brush, path);
                     TextRenderer.DrawText(e.Graphics, btnAdd.Text, btnAdd.Font, new Rectangle(0,0,btnAdd.Width,btnAdd.Height), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                 }
            };
            btnAdd.Click += (s, e) => { MessageBox.Show("Book added!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information); addBookForm.Close(); };

            addBookForm.Controls.Add(btnCancel);
            addBookForm.Controls.Add(btnAdd);
            addBookForm.ShowDialog(this);
        }

        private void ShowCheckOutDialog()
        {
            Form checkOutForm = new Form();
            checkOutForm.Size = new Size(450, 350);
            checkOutForm.FormBorderStyle = FormBorderStyle.None;
            checkOutForm.StartPosition = FormStartPosition.CenterParent;
            checkOutForm.BackColor = Color.White;
            checkOutForm.ShowInTaskbar = false;

             checkOutForm.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 using (Pen p = new Pen(Color.FromArgb(200, 200, 200), 1))
                 {
                     e.Graphics.DrawRectangle(p, 0, 0, checkOutForm.Width - 1, checkOutForm.Height - 1);
                 }
            };

            Label btnClose = new Label { Text = "Ã—", Font = new Font("Arial", 18), ForeColor = Color.Gray, Location = new Point(checkOutForm.Width - 40, 10), Size = new Size(30, 30), Cursor = Cursors.Hand };
            btnClose.Click += (s, e) => checkOutForm.Close();
            checkOutForm.Controls.Add(btnClose);

            Label lblTitle = new Label { Text = "Check Out Book", Font = new Font("Georgia", 16, FontStyle.Bold), ForeColor = Color.FromArgb(40, 40, 40), Location = new Point(30, 25), AutoSize = true };
            checkOutForm.Controls.Add(lblTitle);
             Label lblSubtitle = new Label { Text = "Issue a book to a library member", Font = new Font("Segoe UI", 10), ForeColor = Color.Gray, Location = new Point(32, 55), AutoSize = true };
            checkOutForm.Controls.Add(lblSubtitle);

            int y = 100;
            // Member Select
            Label lblMem = new Label { Text = "Select Member", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 50), Location = new Point(30, y), AutoSize = true };
            checkOutForm.Controls.Add(lblMem);
            
            Panel pnlMem = new Panel { Location = new Point(30, y + 25), Size = new Size(390, 45), BackColor = Color.White, Padding = new Padding(10, 8, 10, 5) };
            ComboBox cmbMem = new ComboBox { FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown }; 
            // Mock Data
            cmbMem.Items.Add("John Smith (MEM-001)");
            pnlMem.Controls.Add(cmbMem);
            checkOutForm.Controls.Add(pnlMem);
            
             pnlMem.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 bool isFocused = cmbMem.Focused;
                 Color borderColor = isFocused ? Color.Maroon : Color.FromArgb(120, 0, 0); // Solid Maroon Border as per image
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnlMem.Width - 3, pnlMem.Height - 3), 8))
                 using(Pen pen = new Pen(borderColor, 1.5f)) e.Graphics.DrawPath(pen, path);
            };
            cmbMem.Enter += (s, e) => pnlMem.Invalidate();
            cmbMem.Leave += (s, e) => pnlMem.Invalidate();
            
            y += 85;

            // Book Select
            Label lblBk = new Label { Text = "Select Book", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 50), Location = new Point(30, y), AutoSize = true };
            checkOutForm.Controls.Add(lblBk);
            
             Panel pnlBk = new Panel { Location = new Point(30, y + 25), Size = new Size(390, 45), BackColor = Color.White, Padding = new Padding(10, 8, 10, 5) };
            ComboBox cmbBk = new ComboBox { FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown }; 
             cmbBk.Items.Add("Intro to C# (ISBN-123)");
            pnlBk.Controls.Add(cmbBk);
            checkOutForm.Controls.Add(pnlBk);
            
             pnlBk.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnlBk.Width - 3, pnlBk.Height - 3), 8))
                 using(Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1)) e.Graphics.DrawPath(pen, path);
            };
            
            cmbBk.Enter += (s, e) => pnlBk.Invalidate();
            cmbBk.Leave += (s, e) => pnlBk.Invalidate();

            // Buttons
            Button btnCancel = new Button { Text = "Cancel", Size = new Size(100, 38), Location = new Point(190, 280), FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Color.Black, Font = ThemeConstants.FontButton };
            btnCancel.Click += (s, e) => checkOutForm.Close();

             Button btnConfirm = new Button { Text = "Confirm Checkout", Size = new Size(140, 38), Location = new Point(300, 280), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(180, 120, 120), ForeColor = Color.White, Font = ThemeConstants.FontButton };
            btnConfirm.FlatAppearance.BorderSize = 0;
             btnConfirm.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnConfirm.Width, btnConfirm.Height), 6))
                 using(SolidBrush brush = new SolidBrush(Color.FromArgb(180, 120, 120))) // Rose/Mauve color from image
                 {
                     e.Graphics.FillPath(brush, path);
                     TextRenderer.DrawText(e.Graphics, "Confirm Checkout", btnConfirm.Font, new Rectangle(0,0,btnConfirm.Width,btnConfirm.Height), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                 }
            };

             checkOutForm.Controls.Add(btnCancel);
             checkOutForm.Controls.Add(btnConfirm);
             checkOutForm.ShowDialog(this);
        }

        private void ShowReservationDialog()
        {
            Form rvForm = new Form();
            rvForm.Size = new Size(450, 400);
            rvForm.FormBorderStyle = FormBorderStyle.None;
            rvForm.StartPosition = FormStartPosition.CenterParent;
            rvForm.BackColor = Color.White;
            rvForm.ShowInTaskbar = false;

             rvForm.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 using (Pen p = new Pen(Color.FromArgb(200, 200, 200), 1))
                 {
                     e.Graphics.DrawRectangle(p, 0, 0, rvForm.Width - 1, rvForm.Height - 1);
                 }
            };

            Label btnClose = new Label { Text = "Ã—", Font = new Font("Arial", 18), ForeColor = Color.Gray, Location = new Point(rvForm.Width - 40, 10), Size = new Size(30, 30), Cursor = Cursors.Hand };
            btnClose.Click += (s, e) => rvForm.Close();
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.Black;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.Gray;
            rvForm.Controls.Add(btnClose);

            Label lblTitle = new Label { Text = "Create Reservation", Font = new Font("Georgia", 16, FontStyle.Bold), ForeColor = Color.FromArgb(40, 40, 40), Location = new Point(30, 25), AutoSize = true };
            rvForm.Controls.Add(lblTitle);
             Label lblSubtitle = new Label { Text = "Reserve a book for a library member", Font = new Font("Segoe UI", 10), ForeColor = Color.Gray, Location = new Point(32, 55), AutoSize = true };
            rvForm.Controls.Add(lblSubtitle);

            int y = 100;
            // Member Select
            Label lblMem = new Label { Text = "Select Member", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 50), Location = new Point(30, y), AutoSize = true };
            rvForm.Controls.Add(lblMem);
            
            Panel pnlMem = new Panel { Location = new Point(30, y + 25), Size = new Size(390, 45), BackColor = Color.White, Padding = new Padding(10, 8, 10, 5) };
            ComboBox cmbMem = new ComboBox { FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown }; 
            cmbMem.Items.Add("Choose a member...");
            cmbMem.SelectedIndex = 0;
            pnlMem.Controls.Add(cmbMem);
            rvForm.Controls.Add(pnlMem);
            
             pnlMem.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 bool isFocused = cmbMem.Focused;
                 Color borderColor = isFocused ? Color.Maroon : Color.FromArgb(120, 0, 0); 
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnlMem.Width - 3, pnlMem.Height - 3), 8))
                 using(Pen pen = new Pen(borderColor, 1.5f)) e.Graphics.DrawPath(pen, path);
            };
            cmbMem.Enter += (s, e) => pnlMem.Invalidate();
            cmbMem.Leave += (s, e) => pnlMem.Invalidate();
            
            y += 85;

            // Book Select
            Label lblBk = new Label { Text = "Select Book", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 50), Location = new Point(30, y), AutoSize = true };
            rvForm.Controls.Add(lblBk);
            
             Panel pnlBk = new Panel { Location = new Point(30, y + 25), Size = new Size(390, 45), BackColor = Color.White, Padding = new Padding(10, 8, 10, 5) };
            ComboBox cmbBk = new ComboBox { FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown }; 
             cmbBk.Items.Add("Choose a book...");
             cmbBk.SelectedIndex = 0;
            pnlBk.Controls.Add(cmbBk);
            rvForm.Controls.Add(pnlBk);
            
             pnlBk.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnlBk.Width - 3, pnlBk.Height - 3), 8))
                 using(Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1)) e.Graphics.DrawPath(pen, path);
            };
             cmbBk.Enter += (s, e) => pnlBk.Invalidate();
             cmbBk.Leave += (s, e) => pnlBk.Invalidate();

            // Buttons
            Button btnCancel = new Button { Text = "Cancel", Size = new Size(100, 38), Location = new Point(180, 330), FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Color.Black, Font = ThemeConstants.FontButton };
            btnCancel.Click += (s, e) => rvForm.Close();

             Button btnConfirm = new Button { Text = "Create Reservation", Size = new Size(150, 38), Location = new Point(290, 330), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(120, 20, 40), ForeColor = Color.White, Font = ThemeConstants.FontButton }; // Darker Maroon
            btnConfirm.FlatAppearance.BorderSize = 0;
             btnConfirm.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnConfirm.Width, btnConfirm.Height), 6))
                 using(SolidBrush brush = new SolidBrush(Color.FromArgb(120, 20, 40))) 
                 {
                     e.Graphics.FillPath(brush, path);
                     TextRenderer.DrawText(e.Graphics, "Create Reservation", btnConfirm.Font, new Rectangle(0,0,btnConfirm.Width,btnConfirm.Height), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                 }
            };

             rvForm.Controls.Add(btnCancel);
             rvForm.Controls.Add(btnConfirm);
             rvForm.ShowDialog(this);
        }


        private void ShowAddFineDialog()
        {
            Form fineForm = new Form();
            fineForm.Size = new Size(450, 600);
            fineForm.FormBorderStyle = FormBorderStyle.None;
            fineForm.StartPosition = FormStartPosition.CenterParent;
            fineForm.BackColor = Color.White;
            fineForm.ShowInTaskbar = false;

             fineForm.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 using (Pen p = new Pen(Color.FromArgb(200, 200, 200), 1))
                 {
                     e.Graphics.DrawRectangle(p, 0, 0, fineForm.Width - 1, fineForm.Height - 1);
                 }
            };

            Label btnClose = new Label { Text = "Ã—", Font = new Font("Arial", 18), ForeColor = Color.Gray, Location = new Point(fineForm.Width - 40, 10), Size = new Size(30, 30), Cursor = Cursors.Hand };
            btnClose.Click += (s, e) => fineForm.Close();
            fineForm.Controls.Add(btnClose);

            Label lblTitle = new Label { Text = "Add New Fine", Font = new Font("Georgia", 16, FontStyle.Bold), ForeColor = Color.FromArgb(40, 40, 40), Location = new Point(30, 25), AutoSize = true };
            fineForm.Controls.Add(lblTitle);
            Label lblSubtitle = new Label { Text = "Create a new fine for a member", Font = new Font("Segoe UI", 10), ForeColor = Color.Gray, Location = new Point(32, 55), AutoSize = true };
            fineForm.Controls.Add(lblSubtitle);

            int y = 90;
            // Member Select
            Label lblMem = new Label { Text = "Select Member", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 50), Location = new Point(30, y), AutoSize = true };
            fineForm.Controls.Add(lblMem);
            Panel pnlMem = new Panel { Location = new Point(30, y + 25), Size = new Size(390, 45), BackColor = Color.White, Padding = new Padding(10, 8, 10, 5) };
            ComboBox cmbMem = new ComboBox { FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown }; 
            cmbMem.Items.Add("Choose a member...");
            cmbMem.SelectedIndex = 0;
            pnlMem.Controls.Add(cmbMem);
            fineForm.Controls.Add(pnlMem);
             pnlMem.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 bool isFocused = cmbMem.Focused;
                 Color borderColor = isFocused ? Color.Maroon : Color.FromArgb(120, 0, 0); 
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnlMem.Width - 3, pnlMem.Height - 3), 8))
                 using(Pen pen = new Pen(borderColor, 1.5f)) e.Graphics.DrawPath(pen, path);
            };
            cmbMem.Enter += (s, e) => pnlMem.Invalidate();
            cmbMem.Leave += (s, e) => pnlMem.Invalidate();
            y += 80;

             // Fine Type
            Label lblType = new Label { Text = "Fine Type", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 50), Location = new Point(30, y), AutoSize = true };
            fineForm.Controls.Add(lblType);
            Panel pnlType = new Panel { Location = new Point(30, y + 25), Size = new Size(390, 45), BackColor = Color.White, Padding = new Padding(10, 8, 10, 5) };
            ComboBox cmbType = new ComboBox { FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList }; 
            cmbType.Items.AddRange(new string[] { "Late Return", "Lost Book", "Damaged Book", "Other" });
            cmbType.SelectedItem = "Other";
            pnlType.Controls.Add(cmbType);
            fineForm.Controls.Add(pnlType);
             pnlType.Paint += (s, e) => {
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnlType.Width - 3, pnlType.Height - 3), 8))
                 using(Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1)) e.Graphics.DrawPath(pen, path);
            };
            y += 80;

            // Amount
             Label lblAmt = new Label { Text = "Amount", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 50), Location = new Point(30, y), AutoSize = true };
            fineForm.Controls.Add(lblAmt);
             Panel pnlAmt = new Panel { Location = new Point(30, y + 25), Size = new Size(390, 45), BackColor = Color.White, Padding = new Padding(10, 10, 10, 5) };
             Label lblSign = new Label { Text = "$", Font = new Font("Segoe UI", 11), ForeColor = Color.Gray, AutoSize = true, Location = new Point(10, 8) };
             TextBox txtAmt = new TextBox { BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 11), Location = new Point(30, 8), Width = 340 };
             txtAmt.Text = "0";
             pnlAmt.Controls.Add(lblSign);
             pnlAmt.Controls.Add(txtAmt);
             fineForm.Controls.Add(pnlAmt);
              pnlAmt.Paint += (s, e) => {
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnlAmt.Width - 3, pnlAmt.Height - 3), 8))
                 using(Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1)) e.Graphics.DrawPath(pen, path);
            };
            y += 80;

            // Book Title (Opt)
             Label lblBk = new Label { Text = "Book Title (optional)", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 50), Location = new Point(30, y), AutoSize = true };
            fineForm.Controls.Add(lblBk);
             Panel pnlBk = new Panel { Location = new Point(30, y + 25), Size = new Size(390, 45), BackColor = Color.White, Padding = new Padding(10, 10, 10, 5) };
             TextBox txtBk = new TextBox { BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10), Dock = DockStyle.Fill };
             txtBk.SetPlaceholder("Related book title...");
             pnlBk.Controls.Add(txtBk);
             fineForm.Controls.Add(pnlBk);
              pnlBk.Paint += (s, e) => {
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnlBk.Width - 3, pnlBk.Height - 3), 8))
                 using(Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1)) e.Graphics.DrawPath(pen, path);
            };
             y += 80;

             // Notes
             Label lblNotes = new Label { Text = "Notes", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 50), Location = new Point(30, y), AutoSize = true };
            fineForm.Controls.Add(lblNotes);
             Panel pnlNotes = new Panel { Location = new Point(30, y + 25), Size = new Size(390, 90), BackColor = Color.White, Padding = new Padding(10) };
             TextBox txtNotes = new TextBox { BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10), Dock = DockStyle.Fill, Multiline = true };
             txtNotes.SetPlaceholder("Additional notes...");
             pnlNotes.Controls.Add(txtNotes);
             fineForm.Controls.Add(pnlNotes);
              pnlNotes.Paint += (s, e) => {
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnlNotes.Width - 3, pnlNotes.Height - 3), 8))
                 using(Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1)) e.Graphics.DrawPath(pen, path);
            };

             // Buttons
            Button btnCancel = new Button { Text = "Cancel", Size = new Size(100, 38), Location = new Point(230, 540), FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Color.Black, Font = ThemeConstants.FontButton };
            btnCancel.Click += (s, e) => fineForm.Close();

             Button btnConfirm = new Button { Text = "Add Fine", Size = new Size(100, 38), Location = new Point(340, 540), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(120, 20, 40), ForeColor = Color.White, Font = ThemeConstants.FontButton };
            btnConfirm.FlatAppearance.BorderSize = 0;
             btnConfirm.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnConfirm.Width, btnConfirm.Height), 6))
                 using(SolidBrush brush = new SolidBrush(Color.FromArgb(120, 20, 40))) 
                 {
                     e.Graphics.FillPath(brush, path);
                     TextRenderer.DrawText(e.Graphics, "Add Fine", btnConfirm.Font, new Rectangle(0,0,btnConfirm.Width,btnConfirm.Height), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                 }
            };

             fineForm.Controls.Add(btnCancel);
             fineForm.Controls.Add(btnConfirm);
             fineForm.ShowDialog(this);
        }

        private Panel CreateReportStatCard(string title, string value, string icon, Color bg, Color fg, bool isDark)
        {
            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = bg, Margin = new Padding(5) };
            
            // Icon at x=10
            Label lblIcon = new Label 
            { 
                Text = icon, 
                Font = new Font("Segoe UI", 20, FontStyle.Regular), // Increased font size slightly
                ForeColor = isDark ? Color.White : Color.FromArgb(40,40,40), 
                Location = new Point(10, 15), 
                AutoSize = true, 
                BackColor = Color.Transparent 
            };

            // Value at x=70 (giving 60px space for icon)
            Label lblVal = new Label 
            { 
                Text = value, 
                Font = new Font("Segoe UI", 16, FontStyle.Bold), 
                ForeColor = fg, 
                Location = new Point(70, 10), 
                AutoEllipsis = true,
                AutoSize = true
            };

            // Title below value at x=70
            Label lblTitle = new Label 
            { 
                Text = title, 
                Font = new Font("Segoe UI", 9), 
                ForeColor = isDark ? Color.FromArgb(200, 200, 200) : Color.Gray, 
                Location = new Point(70, 45), 
                AutoEllipsis = true,
                AutoSize = true
            };
            
             card.Controls.Add(lblIcon);
             card.Controls.Add(lblVal);
             card.Controls.Add(lblTitle);
             
            // Dynamic Resizing Logic
            card.Resize += (s, e) => {
                 int maxW = card.Width - 80; // 70 offset + 10 margin
                 if (maxW > 20) 
                 {
                    lblVal.MaximumSize = new Size(maxW, 40);
                    lblTitle.MaximumSize = new Size(maxW, 25);
                 }
                 else
                 {
                    lblVal.MaximumSize = Size.Empty;
                    lblTitle.MaximumSize = Size.Empty; 
                 }
             };
             
             card.Paint += (s, e) => {
                 using(Pen p = new Pen(Color.FromArgb(230, 230, 230)))
                 {
                      if(!isDark) e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
                 }
             };
             return card;
        }

        private void SetupChart(Panel pnl, string title)
        {
            // Header Panel
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.Transparent };
            Label lblTitle = new Label { Text = title, Font = new Font("Georgia", 14, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true };
            Label lblSub = new Label { Text = "Borrowings and returns over time", Font = new Font("Segoe UI", 9), ForeColor = Color.Gray, Location = new Point(22, 50), AutoSize = true };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);
            pnl.Controls.Add(pnlHeader);

            Chart chart = new Chart();
            chart.Dock = DockStyle.Fill;
            // Removed manual Height setting which caused the bug
            
            ChartArea ca = new ChartArea();
            ca.Name = "MainArea";
            ca.BackColor = Color.White;
            ca.AxisX.MajorGrid.LineColor = Color.FromArgb(245, 245, 245);
            ca.AxisY.MajorGrid.LineColor = Color.FromArgb(245, 245, 245);
            ca.AxisX.LineColor = Color.Gray;
            ca.AxisY.LineColor = Color.Transparent;
            ca.AxisX.LabelStyle.Font = new Font("Segoe UI", 8);
            ca.AxisY.LabelStyle.Font = new Font("Segoe UI", 8);
            ca.AxisX.LabelStyle.ForeColor = Color.Gray;
            ca.AxisY.LabelStyle.ForeColor = Color.Gray;
            chart.ChartAreas.Add(ca);

            Series s1 = new Series
            {
                Name = "Borrowings",
                Color = Color.Maroon,
                ChartType = SeriesChartType.Spline,
                BorderWidth = 3
            };
            s1.Points.AddXY("Jan 1", 5);
            s1.Points.AddXY("Jan 2", 12);
            s1.Points.AddXY("Jan 3", 8);
            s1.Points.AddXY("Jan 4", 15);
            s1.Points.AddXY("Jan 5", 6);
            s1.Points.AddXY("Jan 6", 10);
            
             Series s2 = new Series
            {
                Name = "Returns",
                Color = Color.Green,
                ChartType = SeriesChartType.Spline,
                BorderWidth = 3
            };
            s2.Points.AddXY("Jan 1", 2);
            s2.Points.AddXY("Jan 2", 5);
            s2.Points.AddXY("Jan 3", 8);
            s2.Points.AddXY("Jan 4", 10);
            s2.Points.AddXY("Jan 5", 4);
            s2.Points.AddXY("Jan 6", 9);

            chart.Series.Add(s1);
            chart.Series.Add(s2);
            
            pnl.Controls.Add(chart);
            chart.BringToFront(); // Ensure chart is not covered by header if layout is weird, but Dock Fill + Top works naturally

            pnl.Paint += (s, e) => {
                 using(Pen borderPen = new Pen(Color.FromArgb(230, 230, 230))) 
                    e.Graphics.DrawRectangle(borderPen, 0, 0, pnl.Width - 1, pnl.Height - 1);
            };
        }


    }
}

