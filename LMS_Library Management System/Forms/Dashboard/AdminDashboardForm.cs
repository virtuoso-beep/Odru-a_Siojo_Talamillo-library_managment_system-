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
using LMS_Library_Management_System.Service;
using LMS_Library_Management_System.Models;
using LMS_Library_Management_System.Interfaces;

using System.Windows.Forms.DataVisualization.Charting;

namespace LMS_Library_Management_System.Forms.Dashboard
{
    public partial class AdminDashboardForm : Form
    {
        // Dynamic view panels

        private Panel pnlSearchView;
        private Panel pnlSearchContent;
        private TextBox txtSearchInput;
        private Button btnTriggerSearch;
        private Button btnSearchFilters;
        private Panel pnlSettingsView;
        private Panel pnlSettingsSubNav;
        private Panel pnlSettingsContent;
        private Button btnSettingGeneral;
        private Button btnSettingNotifications;
        private Button btnSettingBorrowing;
        private Button btnSettingFines;
        private Panel pnlUserManagementView;
        private Panel pnlUserManagementTabs;
        private Panel pnlUserManagementContent;
        private Button btnUserTabLibrarians;
        private Button btnUserTabStaff;
        private DataGridView dgvUserManagement;
        private TextBox txtSearchUsers;
        private Button btnSearchUsers;
        private Button btnAddUser;
        private List<Control> _originalMainContentControls = new List<Control>();
        private bool _isLoadingMembersData = false;
        private bool _isProcessingAction = false;
        private Service.UserManagementService _userManagementService;
        private UserRole _currentUserManagementRole = UserRole.Administrator;
        public AdminDashboardForm()
        {
            InitializeComponent();
            this.Text = "Library Management System - Administrator Dashboard";
            this.WindowState = FormWindowState.Maximized;
            this.Size = new Size(1400, 700);
            this.MinimumSize = new Size(1200, 600);
            _userManagementService = new Service.UserManagementService();
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
                    ctrl != pnlSettingsView &&
                    ctrl != pnlMembersView &&
                    ctrl != pnlUserManagementView)
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
                if (!_originalMainContentControls.Contains(ctrl) && ctrl != pnlMembersView && ctrl != pnlUserManagementView)
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
            if (pnlSettingsView != null && !pnlSettingsView.IsDisposed) pnlSettingsView.Visible = false;
            if (pnlMembersView != null && !pnlMembersView.IsDisposed) pnlMembersView.Visible = false;
            if (pnlUserManagementView != null && !pnlUserManagementView.IsDisposed) pnlUserManagementView.Visible = false;
        }

        private void WireUpMenuButtons()
        {
            btnReports.Click += (s, e) => ShowReportsView();
        }

        private void SetupMenuButtonHoverEffects()
        {
            Button[] menuButtons = { btnDashboard, btnMembers, btnCatalog, btnCirculation, 
                btnReservations, btnFines, btnInventory, btnReports, btnUserManagement, btnSearch, btnSettings };

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
            btnSettings.BackColor = ThemeConstants.SecondaryMaroon;
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

                case "UserManagement":
                    ShowUserManagementView();
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

                case "Settings":
                    ShowSettingsView();
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

        private void BtnSearchMembers_Click(object sender, EventArgs e)
        {
            LoadMembersData();
        }

        private void BtnSearchUsers_Click(object sender, EventArgs e)
        {
            LoadUsersData();
        }

        private void TxtSearchUsers_TextChanged(object sender, EventArgs e)
        {
            if (txtSearchUsers.Text == "🔍 Search users...") return;
            LoadUsersData();
        }

        // Helper method to create consistent search bars across all tabs
        private (Panel panel, TextBox textBox, Button button) CreateConsistentSearchBar(string placeholder, int searchBoxWidth = 920)
        {
            Panel searchPanel = new Panel
            {
                Location = new Point(30, 20),
                Size = new Size(searchBoxWidth + 230, 50), // Search box + button + filters
                BackColor = Color.Transparent
            };

            TextBox searchBox = new TextBox
            {
                Location = new Point(0, 8),
                Size = new Size(searchBoxWidth, 34),
                Font = new Font("Segoe UI", 10F),
                Text = placeholder,
                ForeColor = Color.Gray
            };

            searchBox.Enter += (s, e) => {
                if (((TextBox)s).Text == placeholder)
                {
                    ((TextBox)s).Text = "";
                    ((TextBox)s).ForeColor = Color.Black;
                }
            };

            searchBox.Leave += (s, e) => {
                if (string.IsNullOrWhiteSpace(((TextBox)s).Text))
                {
                    ((TextBox)s).Text = placeholder;
                    ((TextBox)s).ForeColor = Color.Gray;
                }
            };

            Button searchButton = new Button
            {
                Text = "🔍 Search",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ThemeConstants.PrimaryMaroon,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 34),
                Location = new Point(searchBoxWidth + 10, 8)
            };
            searchButton.FlatAppearance.BorderSize = 0;

            searchPanel.Controls.Add(searchBox);
            searchPanel.Controls.Add(searchButton);

            return (searchPanel, searchBox, searchButton);
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
                    dgvMembers.Columns.Add("FirstName", "First Name");
                    dgvMembers.Columns.Add("LastName", "Last Name");
                    dgvMembers.Columns.Add("Email", "Email");
                    dgvMembers.Columns.Add("Phone", "Phone");
                    dgvMembers.Columns.Add("Address", "Address");
                    dgvMembers.Columns.Add("Department", "Department");
                    dgvMembers.Columns.Add("Type", "Type");
                    dgvMembers.Columns.Add("Status", "Status");
                    dgvMembers.Columns.Add("Actions", "Actions");
                    
                    dgvMembers.Columns["MemberId"].Width = 140;
                    dgvMembers.Columns["FirstName"].Width = 110;
                    dgvMembers.Columns["LastName"].Width = 110;
                    dgvMembers.Columns["Email"].Width = 220;
                    dgvMembers.Columns["Phone"].Width = 120;
                    dgvMembers.Columns["Address"].Width = 150;
                    dgvMembers.Columns["Department"].Width = 200;
                    dgvMembers.Columns["Type"].Width = 100;
                    dgvMembers.Columns["Status"].Width = 100;
                    dgvMembers.Columns["Actions"].Width = 120;
                    dgvMembers.RowTemplate.Height = 50;
                    
                    // Ensure proper alignment
                    dgvMembers.Columns["MemberId"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    dgvMembers.Columns["FirstName"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    dgvMembers.Columns["LastName"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    dgvMembers.Columns["Email"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    dgvMembers.Columns["Phone"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvMembers.Columns["Address"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    dgvMembers.Columns["Department"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    dgvMembers.Columns["Type"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvMembers.Columns["Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvMembers.Columns["Actions"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    
                    // Set header alignment
                    dgvMembers.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    dgvMembers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                    dgvMembers.ColumnHeadersHeight = 45;
                }
                
                string searchText = txtSearchMembers.Text;
                if (searchText == "🔍 Search members..." || searchText == "Search members...")
                {
                    searchText = "";
                }
                string statusFilter = cmbStatusFilter.SelectedItem?.ToString() ?? "All Status";
                string typeFilter = cmbTypeFilter.SelectedItem?.ToString() ?? "All Types";
                
                // Get members from database
                var memberService = new Service.MemberService();
                var dbMembers = memberService.GetAllMembers();
                
                // Get all statistics in one optimized batch query
                var allStatistics = memberService.GetAllMembersStatistics();
                
                // Convert to MemberInfo for display with real statistics from database
                var allMembers = dbMembers.Select(m => 
                {
                    // Get real statistics from batch query (much faster than individual queries)
                    var stats = allStatistics.ContainsKey(m.MemberNumber) 
                        ? allStatistics[m.MemberNumber] 
                        : (0, 0.00m);
                    
                    return new MemberInfo
                    {
                        MemberId = m.MemberNumber,
                        FirstName = m.FirstName,
                        LastName = m.LastName,
                        Name = m.FullName,
                        PhoneNumber = m.Phone ?? "",
                        Address = m.Address ?? "",
                        Department = m.Department ?? "",
                        Type = m.MemberType,
                        Email = m.Email,
                        Status = m.StatusText,
                        BooksBorrowed = stats.Item1, // Real data from database (BooksBorrowed)
                        BooksLimit = GetBookLimit(m.MemberType),
                        Fines = stats.Item2 // Real data from database (UnpaidFines)
                    };
                }).ToList();
                
                // Apply filters
                IEnumerable<MemberInfo> filteredMembers = allMembers;
                if (!string.IsNullOrEmpty(searchText))
                {
                    string searchLower = searchText.ToLower();
                    filteredMembers = filteredMembers.Where(m => 
                        m.MemberId.ToLower().Contains(searchLower) ||
                        m.Name.ToLower().Contains(searchLower) ||
                        m.Email.ToLower().Contains(searchLower));
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
                        member.FirstName,
                        member.LastName,
                        member.Email,
                        member.PhoneNumber,
                        member.Address,
                        member.Department,
                        member.Type,
                        member.Status,
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

        private int GetBookLimit(string memberType)
        {
            switch (memberType)
            {
                case "Student": return 5;
                case "Faculty": return 10;
                case "Staff": return 7;
                case "Guest": return 2;
                default: return 3;
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
                 // Draw Icons - View (👁), Edit (✏), Delete (🗑)
                 TextRenderer.DrawText(e.Graphics, "👁", new Font("Segoe UI Symbol", 12F), 
                     new Rectangle(e.CellBounds.X + 10, e.CellBounds.Y, 30, e.CellBounds.Height), Color.Gray, 
                     TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                     
                 TextRenderer.DrawText(e.Graphics, "✏", new Font("Segoe UI Symbol", 12F), 
                     new Rectangle(e.CellBounds.X + 45, e.CellBounds.Y, 30, e.CellBounds.Height), Color.FromArgb(0, 120, 215), 
                     TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                     
                 TextRenderer.DrawText(e.Graphics, "🗑", new Font("Segoe UI Symbol", 12F), 
                     new Rectangle(e.CellBounds.X + 80, e.CellBounds.Y, 30, e.CellBounds.Height), Color.FromArgb(200, 50, 50), 
                     TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                 e.Handled = true;
             }
        }

        private void TxtSearchMembers_Enter(object sender, EventArgs e)
        {
            if (txtSearchMembers.Text == "🔍 Search members...")
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

        private void ShowRegisterMemberDialog()
        {
            // Form Setup
            Form registerForm = new Form();
            registerForm.Size = new Size(550, 780);
            registerForm.FormBorderStyle = FormBorderStyle.None; // Custom chrome
            registerForm.StartPosition = FormStartPosition.CenterParent;
            registerForm.BackColor = Color.FromArgb(245, 240, 235); // Beige background
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
            btnClose.Text = "×"; 
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
            // Initial placeholder will be set based on default member type (Student)
            TextBox txtEmail = AddInput("user@umindanao.edu.ph", 30, currentY, 490);
            currentY += gap;

            // Row 3
            AddLabel("Phone", 30, currentY);
            TextBox txtPhone = AddInput("09XXXXXXXXX", 30, currentY, 490);
            txtPhone.MaxLength = 11;
            // Allow only numeric input for phone number (no letters or symbols) and limit to 11 digits
            txtPhone.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
                // Prevent typing if already 11 digits (excluding control characters)
                if (char.IsDigit(e.KeyChar) && txtPhone.GetActualText().Length >= 11)
                {
                    e.Handled = true;
                }
            };
            currentY += gap;

            // Row 4
            AddLabel("Address", 30, currentY);
            TextBox txtAddress = AddInput("please enter address", 30, currentY, 490);
            currentY += gap;

            // Row 5 - Department Dropdown
            AddLabel("Department", 30, currentY);
            Panel pnlDepartment = new Panel();
            pnlDepartment.Location = new Point(30, currentY + 25);
            pnlDepartment.Size = new Size(490, 42);
            pnlDepartment.BackColor = Color.White;
            
            ComboBox cmbDepartment = new ComboBox();
            cmbDepartment.FlatStyle = FlatStyle.Flat;
            cmbDepartment.Font = new Font("Segoe UI", 11F);
            cmbDepartment.Items.Add("Choose department");
            cmbDepartment.Items.AddRange(new string[] 
            { 
                "Computing Education Department",
                "Department of Engineering Education",
                "Department of Teacher Education",
                "Department of Arts and Sciences Education",
                "Department of Business Administration Education",
                "Department of Hospitality Education",
                "JHS Department"
            });
            cmbDepartment.SelectedIndex = 0;
            cmbDepartment.Location = new Point(10, 8);
            cmbDepartment.Width = 470;
            cmbDepartment.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDepartment.ForeColor = Color.Gray;
            
            cmbDepartment.SelectedIndexChanged += (s, e) =>
            {
                cmbDepartment.ForeColor = cmbDepartment.SelectedIndex == 0 ? Color.Gray : Color.Black;
            };
            
            pnlDepartment.Controls.Add(cmbDepartment);
            registerForm.Controls.Add(pnlDepartment);
            
            pnlDepartment.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnlDepartment.Width - 3, pnlDepartment.Height - 3), 8))
                 using(Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1))
                 {
                     e.Graphics.DrawPath(pen, path);
                 }
            };
            currentY += gap;

            // Row 6 - Member Type ComboBox
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
            
            // Update email placeholder based on member type
            cmbMemberType.SelectedIndexChanged += (s, e) =>
            {
                string selectedType = cmbMemberType.SelectedItem?.ToString() ?? "";
                string currentText = txtEmail.GetActualText();
                string currentDisplayText = txtEmail.Text;
                
                // Only update placeholder if the field is empty or showing placeholder
                if (string.IsNullOrWhiteSpace(currentText))
                {
                    string newPlaceholder = (selectedType == "Student" || selectedType == "Faculty") 
                        ? "user@umindanao.edu.ph" 
                        : "example@library.com";
                    
                    // Check if currently showing a placeholder
                    if (currentDisplayText == "user@umindanao.edu.ph" || 
                        currentDisplayText == "example@library.com" ||
                        string.IsNullOrWhiteSpace(currentDisplayText))
                    {
                        // Update the placeholder directly
                        txtEmail.Text = newPlaceholder;
                        txtEmail.ForeColor = Color.Gray;
                        
                        // Update the stored placeholder data
                        if (txtEmail.Tag is PlaceholderData data)
                        {
                            txtEmail.Tag = new PlaceholderData
                            {
                                PlaceholderText = newPlaceholder,
                                PlaceholderColor = data.PlaceholderColor,
                                OriginalForeColor = data.OriginalForeColor
                            };
                        }
                        else
                        {
                            txtEmail.SetPlaceholder(newPlaceholder);
                        }
                    }
                }
            };
            
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
            btnCancel.Location = new Point(250, 700);
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
            btnRegister.Location = new Point(360, 700);
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
                // Get form values
                string firstName = txtFirstName.GetActualText()?.Trim() ?? "";
                string lastName = txtLastName.GetActualText()?.Trim() ?? "";
                string email = txtEmail.GetActualText()?.Trim() ?? "";
                string phone = txtPhone.GetActualText()?.Trim() ?? "";
                string address = txtAddress.GetActualText()?.Trim() ?? "";
                string department = cmbDepartment.SelectedIndex > 0 ? cmbDepartment.SelectedItem?.ToString() : null;
                string memberType = cmbMemberType.SelectedItem?.ToString() ?? "";

                // Validate First Name and Last Name
                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    MessageBox.Show("Please enter valid first name and last name.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtFirstName.Focus();
                    return;
                }

                // Validate Email
                if (string.IsNullOrWhiteSpace(email))
                {
                    MessageBox.Show("Email is required.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }

                // Validate email format based on member type
                var memberService = new Service.MemberService();
                if (!memberService.IsValidMemberEmail(email, memberType))
                {
                if (memberType == "Student" || memberType == "Faculty")
                {
                        MessageBox.Show($"Email must be a valid @umindanao.edu.ph email address for {memberType} members.", 
                            "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                        MessageBox.Show($"Please enter a valid email address for {memberType} members (e.g., example@library.com).", 
                            "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                        txtEmail.Focus();
                        return;
                    }

                // Validate Phone Number (must be 11 digits starting with 09)
                if (!string.IsNullOrWhiteSpace(phone))
                {
                    if (!memberService.IsValidPhoneNumber(phone))
                {
                        MessageBox.Show("Phone number must be exactly 11 digits starting with '09' (e.g., 09XXXXXXXXX).", 
                            "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPhone.Focus();
                    return;
                }
                }

                // Validate Member Type
                if (string.IsNullOrWhiteSpace(memberType))
                {
                    MessageBox.Show("Please select a member type.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbMemberType.Focus();
                    return;
                }

                // Create member in database
                try
                {
                    int memberId = memberService.CreateMember(email, firstName, lastName, memberType, phone, address, department);
                    
                    if (memberId > 0)
                    {
                MessageBox.Show($"Member {firstName} {lastName} registered successfully!", 
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                registerForm.DialogResult = DialogResult.OK;
                registerForm.Close();
                    }
                    else
                    {
                        MessageBox.Show("Failed to register member. The email may already be registered.", 
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (InvalidOperationException ioEx)
                {
                    // Handle duplicate email or other validation errors
                    MessageBox.Show(ioEx.Message, 
                        "Registration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error registering member: {ex.Message}", 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        /// <summary>
        /// Validates ISBN format (ISBN-10 or ISBN-13)
        /// </summary>
        private bool IsValidISBN(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
                return false;

            // Remove dashes, spaces, and convert to uppercase
            string cleanISBN = System.Text.RegularExpressions.Regex.Replace(isbn.Trim().ToUpper(), @"[-\s]", "");

            // ISBN-10: 10 characters, last can be X
            if (cleanISBN.Length == 10)
            {
                return System.Text.RegularExpressions.Regex.IsMatch(cleanISBN, @"^[0-9]{9}[0-9X]$");
            }

            // ISBN-13: 13 characters, must start with 978 or 979
            if (cleanISBN.Length == 13)
            {
                return System.Text.RegularExpressions.Regex.IsMatch(cleanISBN, @"^(978|979)[0-9]{10}$");
            }

            return false;
        }

        /// <summary>
        /// Formats ISBN with dashes for better readability
        /// </summary>
        private string FormatISBN(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
                return isbn;

            // Remove existing dashes and spaces, convert to uppercase
            string clean = System.Text.RegularExpressions.Regex.Replace(isbn.Trim().ToUpper(), @"[-\s]", "");

            // Format ISBN-10: X-XXXX-XXXX-X
            if (clean.Length == 10)
            {
                return $"{clean.Substring(0, 1)}-{clean.Substring(1, 4)}-{clean.Substring(5, 4)}-{clean.Substring(9, 1)}";
            }

            // Format ISBN-13: XXX-X-XXXXXX-XX-X
            if (clean.Length == 13)
            {
                return $"{clean.Substring(0, 3)}-{clean.Substring(3, 1)}-{clean.Substring(4, 6)}-{clean.Substring(10, 2)}-{clean.Substring(12, 1)}";
            }

            return isbn; // Return original if format doesn't match
        }

        /// <summary>
        /// Checks if ISBN already exists in database
        /// </summary>
        private bool IsISBNDuplicate(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn) || !IsValidISBN(isbn))
                return false;

            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Remove formatting for comparison
                    string cleanISBN = System.Text.RegularExpressions.Regex.Replace(isbn.Trim().ToUpper(), @"[-\s]", "");

                    string query = "SELECT COUNT(*) FROM Books WHERE REPLACE(REPLACE(UPPER(ISBN), '-', ''), ' ', '') = @CleanISBN";
                    using (var command = new MySql.Data.MySqlClient.MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CleanISBN", cleanISBN);
                        object result = command.ExecuteScalar();
                        return result != null && Convert.ToInt32(result) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking ISBN duplicate: {ex.Message}");
                return false; // Don't block if check fails
            }
        }

        /// <summary>
        /// Generates a unique ISBN-13 for library books
        /// Format: 978-0-XXXXXX-XX-X where XXXXXX is a sequential number
        /// </summary>
        private string GenerateISBN()
        {
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Ensure we're using the correct database
                    using (var useDbCmd = new MySql.Data.MySqlClient.MySqlCommand("USE LMS_DB", connection))
                    {
                        useDbCmd.ExecuteNonQuery();
                    }
                    
                    // Get the last generated ISBN from database
                    // Look for ISBNs that match our pattern: 978-0-XXXXXX-XX-X or 9780XXXXXX00X
                    using (var command = new MySql.Data.MySqlClient.MySqlCommand(@"
                        SELECT ISBN 
                        FROM Books 
                        WHERE (ISBN LIKE '978-0-%' OR ISBN LIKE '9780%')
                        AND LENGTH(REPLACE(REPLACE(ISBN, '-', ''), ' ', '')) = 13
                        ORDER BY CAST(SUBSTRING(REPLACE(REPLACE(ISBN, '-', ''), ' ', ''), 5, 6) AS UNSIGNED) DESC
                        LIMIT 1", connection))
                    {
                        object result = command.ExecuteScalar();
                        int nextNumber = 1;
                        
                        if (result != null && result != DBNull.Value)
                        {
                            string lastISBN = result.ToString();
                            System.Diagnostics.Debug.WriteLine($"GenerateISBN: Found last ISBN: {lastISBN}");
                            
                            // Remove dashes and spaces
                            string cleanISBN = System.Text.RegularExpressions.Regex.Replace(lastISBN, @"[-\s]", "");
                            
                            // Extract the 6-digit registrant number (positions 4-9 in ISBN-13)
                            if (cleanISBN.Length >= 10 && cleanISBN.StartsWith("978"))
                            {
                                try
                                {
                                    string registrantPart = cleanISBN.Substring(4, 6);
                                    if (int.TryParse(registrantPart, out int lastNumber))
                                    {
                                        nextNumber = lastNumber + 1;
                                        System.Diagnostics.Debug.WriteLine($"GenerateISBN: Extracted number: {lastNumber}, next: {nextNumber}");
                                    }
                                }
                                catch (Exception parseEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"GenerateISBN: Error parsing registrant: {parseEx.Message}");
                                }
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("GenerateISBN: No existing ISBN found, starting from 1");
                        }
                        
                        // Ensure we don't exceed 6 digits (999999)
                        if (nextNumber > 999999)
                        {
                            nextNumber = 1; // Reset
                            System.Diagnostics.Debug.WriteLine("GenerateISBN: Sequence reset - reached maximum");
                        }
                        
                        // Format: 978-0-XXXXXX-XX-X
                        // 978 = EAN prefix for books
                        // 0 = registration group (0 = English language)
                        // XXXXXX = 6-digit registrant number
                        // XX = 2-digit publication number (00 for now)
                        // X = check digit (calculated)
                        
                        string registrant = nextNumber.ToString("D6"); // 6 digits with leading zeros
                        string publication = "00"; // Fixed for now
                        string prefix = "9780";
                        string baseISBN = prefix + registrant + publication;
                        
                        // Calculate check digit for ISBN-13
                        int checkDigit = CalculateISBN13CheckDigit(baseISBN);
                        
                        // Format with dashes: 978-0-XXXXXX-XX-X
                        string formattedISBN = $"978-0-{registrant}-{publication}-{checkDigit}";
                        
                        System.Diagnostics.Debug.WriteLine($"GenerateISBN: Generated ISBN: {formattedISBN}");
                        
                        // Verify it's unique and find next available if needed
                        int attempts = 0;
                        while (IsISBNDuplicate(formattedISBN) && attempts < 1000)
                        {
                            nextNumber++;
                            if (nextNumber > 999999) nextNumber = 1;
                            registrant = nextNumber.ToString("D6");
                            baseISBN = prefix + registrant + publication;
                            checkDigit = CalculateISBN13CheckDigit(baseISBN);
                            formattedISBN = $"978-0-{registrant}-{publication}-{checkDigit}";
                            attempts++;
                            
                            if (attempts % 100 == 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"GenerateISBN: Still checking uniqueness, attempt {attempts}");
                            }
                        }
                        
                        if (attempts >= 1000)
                        {
                            System.Diagnostics.Debug.WriteLine("GenerateISBN: WARNING - Could not find unique ISBN after 1000 attempts, using timestamp fallback");
                            // Use timestamp fallback
                            string timestamp = DateTime.Now.Ticks.ToString().Substring(Math.Max(0, DateTime.Now.Ticks.ToString().Length - 9));
                            registrant = timestamp.PadLeft(6, '0').Substring(0, 6);
                            baseISBN = prefix + registrant + publication;
                            checkDigit = CalculateISBN13CheckDigit(baseISBN);
                            formattedISBN = $"978-0-{registrant}-{publication}-{checkDigit}";
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"GenerateISBN: Final ISBN: {formattedISBN} (after {attempts} uniqueness checks)");
                        return formattedISBN;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating ISBN: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"GenerateISBN StackTrace: {ex.StackTrace}");
                
                // Fallback: use timestamp-based ISBN
                string timestamp = DateTime.Now.Ticks.ToString().Substring(Math.Max(0, DateTime.Now.Ticks.ToString().Length - 9));
                string registrant = timestamp.PadLeft(6, '0').Substring(0, 6);
                string prefix = "9780";
                string publication = "00";
                string baseISBN = prefix + registrant + publication;
                int checkDigit = CalculateISBN13CheckDigit(baseISBN);
                string fallbackISBN = $"978-0-{registrant}-{publication}-{checkDigit}";
                System.Diagnostics.Debug.WriteLine($"GenerateISBN: Using fallback ISBN: {fallbackISBN}");
                return fallbackISBN;
            }
        }

        /// <summary>
        /// Calculates the check digit for ISBN-13
        /// </summary>
        private int CalculateISBN13CheckDigit(string isbn12)
        {
            if (isbn12.Length != 12)
                throw new ArgumentException("ISBN must be 12 digits for check digit calculation");
            
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = int.Parse(isbn12[i].ToString());
                // Multiply by 1 for odd positions, 3 for even positions (0-indexed)
                sum += digit * (i % 2 == 0 ? 1 : 3);
            }
            
            int remainder = sum % 10;
            int checkDigit = remainder == 0 ? 0 : 10 - remainder;
            return checkDigit;
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
                
                // Divide into 3 sections: View (0-33), Edit (33-66), Delete (66-100)
                int thirdWidth = cellRect.Width / 3;
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
                        Text = "✕",
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
                        Text = "🕐 Borrowing History",
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
                        Location = new Point(470, 15),
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
                    
                    Button btnEdit = new Button
                    {
                        Text = "✏ Edit Member",
                        Size = new Size(130, 40),
                        Location = new Point(580, 15),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = ThemeConstants.PrimaryMaroon,
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                        Cursor = Cursors.Hand
                    };
                    btnEdit.FlatAppearance.BorderSize = 0;
                    btnEdit.Paint += (s, e) =>
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnEdit.Width - 1, btnEdit.Height - 1), 6))
                        using (SolidBrush brush = new SolidBrush(ThemeConstants.PrimaryMaroon))
                            e.Graphics.FillPath(brush, path);
                        TextRenderer.DrawText(e.Graphics, btnEdit.Text, btnEdit.Font, new Rectangle(0, 0, btnEdit.Width, btnEdit.Height), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    };
                    btnEdit.Click += (s, e) =>
                    {
                        _isProcessingAction = false; // Reset flag to allow EditMember to execute
                        EditMember(memberId);
                        // Refresh the view after editing
                        viewForm.Close();
                        if (!string.IsNullOrEmpty(memberId))
                        {
                            ViewMember(memberId);
                        }
                    };
                    footerPanel.Controls.Add(btnEdit);
                    
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
                
                // Load actual member data from database
                var memberService = new Service.MemberService();
                var memberData = memberService.GetMemberByNumber(memberId);
                
                if (memberData == null)
                {
                    MessageBox.Show("Member not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                string firstName = memberData.FirstName;
                string lastName = memberData.LastName;
                string email = memberData.Email;
                string phone = memberData.Phone ?? "";
                string address = memberData.Address ?? "";
                string department = memberData.Department ?? "";
                string memberType = memberData.MemberType;
                string status = memberData.StatusText;
                
                using (Form editForm = new Form())
                {
                    editForm.Text = "";
                    editForm.Size = new Size(700, 800);
                    editForm.StartPosition = FormStartPosition.CenterParent;
                    editForm.FormBorderStyle = FormBorderStyle.None;
                    editForm.BackColor = Color.FromArgb(245, 240, 235); // Beige background
                    editForm.Padding = new Padding(0);
                    
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
                        Height = 80,
                        BackColor = Color.FromArgb(245, 240, 235), // Beige background
                        Padding = new Padding(30, 25, 30, 10)
                    };
                    
                    // Close button (X)
                    Button btnCloseX = new Button
                    {
                        Text = "✕",
                        Size = new Size(30, 30),
                        Location = new Point(490, 25),
                        Anchor = AnchorStyles.Top | AnchorStyles.Right,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.Transparent,
                        ForeColor = Color.Gray,
                        Font = new Font("Segoe UI", 12F),
                        Cursor = Cursors.Hand
                    };
                    btnCloseX.FlatAppearance.BorderSize = 0;
                    btnCloseX.Click += (s, e) => editForm.DialogResult = DialogResult.Cancel;
                    headerPanel.Controls.Add(btnCloseX);
                    
                    // Title
                    Label lblTitle = new Label
                    {
                        Text = "Edit Member",
                        Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(80, 40, 20), // Dark brown
                        Location = new Point(0, 0),
                        AutoSize = true
                    };
                    headerPanel.Controls.Add(lblTitle);
                    
                    // Member ID
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
                        Padding = new Padding(30, 20, 30, 20)
                    };
                    
                    int fieldHeight = 70;
                    
                    // Helper function to create input field
                    Func<string, string, Control> CreateInputField = (label, value) =>
                    {
                        Panel fieldContainer = new Panel
                        {
                            Height = fieldHeight,
                            Dock = DockStyle.Top,
                            BackColor = Color.Transparent
                        };
                        
                        Label lbl = new Label
                        {
                            Text = label,
                            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                            ForeColor = Color.FromArgb(60, 60, 60),
                            Location = new Point(0, 5),
                            AutoSize = true
                        };
                        fieldContainer.Controls.Add(lbl);
                        
                        Panel inputPanel = new Panel
                        {
                            Height = 40,
                            Location = new Point(0, 28),
                            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                            BackColor = Color.White,
                            Padding = new Padding(10, 0, 10, 0)
                        };
                        
                        TextBox txt = new TextBox
                        {
                            Text = value,
                            Dock = DockStyle.Fill,
                            BorderStyle = BorderStyle.None,
                            Font = new Font("Segoe UI", 10F),
                            BackColor = Color.White
                        };
                        
                        inputPanel.Paint += (s, e) =>
                        {
                            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                            bool isFocused = txt.Focused;
                            Color borderColor = isFocused ? ThemeConstants.PrimaryMaroon : Color.FromArgb(220, 220, 220);
                            using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, inputPanel.Width - 1, inputPanel.Height - 1), 6))
                            using (Pen pen = new Pen(borderColor, isFocused ? 1.5f : 1f))
                                e.Graphics.DrawPath(pen, path);
                        };
                        
                        txt.Enter += (s, e) => inputPanel.Invalidate();
                        txt.Leave += (s, e) => inputPanel.Invalidate();
                        
                        // Handle resize for input panel
                        fieldContainer.Resize += (s, e) =>
                        {
                            inputPanel.Width = fieldContainer.Width;
                        };
                        
                        inputPanel.Controls.Add(txt);
                        fieldContainer.Controls.Add(inputPanel);
                        
                        return fieldContainer;
                    };
                    
                    // Helper function to create dropdown field
                    Func<string, string[], string, Control> CreateDropdownField = (label, items, selectedValue) =>
                    {
                        Panel fieldContainer = new Panel
                        {
                            Height = fieldHeight,
                            Dock = DockStyle.Top,
                            BackColor = Color.Transparent
                        };
                        
                        Label lbl = new Label
                        {
                            Text = label,
                            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                            ForeColor = Color.FromArgb(60, 60, 60),
                            Location = new Point(0, 5),
                            AutoSize = true
                        };
                        fieldContainer.Controls.Add(lbl);
                        
                        Panel inputPanel = new Panel
                        {
                            Height = 40,
                            Location = new Point(0, 28),
                            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                            BackColor = Color.White,
                            Padding = new Padding(10, 0, 10, 0)
                        };
                        
                        ComboBox cmb = new ComboBox
                        {
                            Dock = DockStyle.Fill,
                            FlatStyle = FlatStyle.Flat,
                            Font = new Font("Segoe UI", 10F),
                            DropDownStyle = ComboBoxStyle.DropDownList,
                            BackColor = Color.White
                        };
                        cmb.Items.AddRange(items);
                        if (!string.IsNullOrEmpty(selectedValue) && cmb.Items.Contains(selectedValue))
                            cmb.SelectedItem = selectedValue;
                        else if (cmb.Items.Count > 0)
                            cmb.SelectedIndex = 0;
                        
                        inputPanel.Paint += (s, e) =>
                        {
                            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                            bool isFocused = cmb.Focused;
                            Color borderColor = isFocused ? ThemeConstants.PrimaryMaroon : Color.FromArgb(220, 220, 220);
                            using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, inputPanel.Width - 1, inputPanel.Height - 1), 6))
                            using (Pen pen = new Pen(borderColor, isFocused ? 1.5f : 1f))
                                e.Graphics.DrawPath(pen, path);
                        };
                        
                        cmb.Enter += (s, e) => inputPanel.Invalidate();
                        cmb.Leave += (s, e) => inputPanel.Invalidate();
                        
                        // Handle resize for input panel
                        fieldContainer.Resize += (s, e) =>
                        {
                            inputPanel.Width = fieldContainer.Width;
                        };
                        
                        inputPanel.Controls.Add(cmb);
                        fieldContainer.Controls.Add(inputPanel);
                        
                        return fieldContainer;
                    };
                    
                    // Two-column layout for First Name and Last Name
                    Panel nameRowContainer = new Panel
                    {
                        Height = fieldHeight,
                        Dock = DockStyle.Top,
                        BackColor = Color.Transparent
                    };
                    
                    // Calculate field width (form width - padding - spacing)
                    int fieldWidth = (editForm.Width - 60 - 10) / 2;
                    
                    // First Name field (left column)
                    Panel pnlFirstName = CreateInputField("First Name", firstName) as Panel;
                    pnlFirstName.Dock = DockStyle.None;
                    pnlFirstName.Location = new Point(0, 0);
                    pnlFirstName.Width = fieldWidth;
                    nameRowContainer.Controls.Add(pnlFirstName);
                    
                    // Last Name field (right column)
                    Panel pnlLastName = CreateInputField("Last Name", lastName) as Panel;
                    pnlLastName.Dock = DockStyle.None;
                    pnlLastName.Location = new Point(fieldWidth + 10, 0);
                    pnlLastName.Width = fieldWidth;
                    nameRowContainer.Controls.Add(pnlLastName);
                    
                    // Resize handler for name row
                    Action resizeNameRow = () =>
                    {
                        int newFieldWidth = (nameRowContainer.Width - 10) / 2;
                        pnlFirstName.Width = newFieldWidth;
                        pnlLastName.Width = newFieldWidth;
                        pnlLastName.Left = newFieldWidth + 10;
                    };
                    
                    nameRowContainer.Resize += (s, e) => resizeNameRow();
                    
                    // Initial sizing after form loads
                    editForm.Load += (s, e) => resizeNameRow();
                    
                    // Create other fields (full width)
                    Panel pnlEmail = CreateInputField("Email", email) as Panel;
                    Panel pnlPhone = CreateInputField("Phone", phone) as Panel;
                    Panel pnlAddress = CreateInputField("Address", address) as Panel;
                    Panel pnlDepartment = CreateDropdownField("Department", new[] { "Choose department", "Computing Education Department", "Department of Engineering Education", "Department of Teacher Education", "Department of Arts and Sciences Education", "Department of Business Administration Education", "Department of Hospitality Education", "JHS Department" }, string.IsNullOrEmpty(department) ? "Choose department" : department) as Panel;
                    Panel pnlMemberType = CreateDropdownField("Member Type", new[] { "Student", "Faculty", "Staff", "Guest" }, memberType) as Panel;
                    Panel pnlStatus = CreateDropdownField("Status", new[] { "Active", "Expired", "Inactive" }, status) as Panel;
                    
                    // Add fields in reverse order for proper docking
                    contentPanel.Controls.Add(pnlStatus);
                    contentPanel.Controls.Add(pnlMemberType);
                    contentPanel.Controls.Add(pnlDepartment);
                    contentPanel.Controls.Add(pnlAddress);
                    contentPanel.Controls.Add(pnlPhone);
                    contentPanel.Controls.Add(pnlEmail);
                    contentPanel.Controls.Add(nameRowContainer);
                    
                    // Footer buttons
                    Panel footerPanel = new Panel
                    {
                        Dock = DockStyle.Bottom,
                        Height = 70,
                        BackColor = Color.FromArgb(245, 240, 235), // Beige background
                        Padding = new Padding(30, 15, 30, 15)
                    };
                    
                    Button btnCancel = new Button
                    {
                        Text = "Cancel",
                        Size = new Size(100, 40),
                        Anchor = AnchorStyles.Top | AnchorStyles.Right,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.FromArgb(245, 240, 235), // Beige background
                        ForeColor = Color.FromArgb(80, 40, 20), // Dark brown
                        Font = new Font("Segoe UI", 9F),
                        Cursor = Cursors.Hand,
                        DialogResult = DialogResult.Cancel
                    };
                    btnCancel.FlatAppearance.BorderSize = 0;
                    btnCancel.Location = new Point(420, 15);
                    btnCancel.Paint += (s, e) =>
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnCancel.Width - 1, btnCancel.Height - 1), 6))
                        using (SolidBrush brush = new SolidBrush(Color.FromArgb(243, 244, 246)))
                            e.Graphics.FillPath(brush, path);
                        TextRenderer.DrawText(e.Graphics, btnCancel.Text, btnCancel.Font, new Rectangle(0, 0, btnCancel.Width, btnCancel.Height), Color.FromArgb(40, 40, 40), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    };
                    footerPanel.Controls.Add(btnCancel);
                    
                    Button btnSave = new Button
                    {
                        Text = "Save Changes",
                        Size = new Size(130, 40),
                        Location = new Point(530, 15),
                        Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = ThemeConstants.PrimaryMaroon,
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                        Cursor = Cursors.Hand
                    };
                    
                    btnSave.FlatAppearance.BorderSize = 0;
                    btnSave.Paint += (s, e) =>
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnSave.Width - 1, btnSave.Height - 1), 6))
                        using (SolidBrush brush = new SolidBrush(ThemeConstants.PrimaryMaroon))
                            e.Graphics.FillPath(brush, path);
                        TextRenderer.DrawText(e.Graphics, btnSave.Text, btnSave.Font, new Rectangle(0, 0, btnSave.Width, btnSave.Height), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    };
                    btnSave.Click += (s, e) =>
                    {
                        // Get all field values
                        TextBox txtFirstName = pnlFirstName.Controls.OfType<Panel>().First().Controls.OfType<TextBox>().First();
                        TextBox txtLastName = pnlLastName.Controls.OfType<Panel>().First().Controls.OfType<TextBox>().First();
                        TextBox txtEmail = pnlEmail.Controls.OfType<Panel>().First().Controls.OfType<TextBox>().First();
                        TextBox txtPhone = pnlPhone.Controls.OfType<Panel>().First().Controls.OfType<TextBox>().First();
                        TextBox txtAddress = pnlAddress.Controls.OfType<Panel>().First().Controls.OfType<TextBox>().First();
                        ComboBox cmbDepartment = pnlDepartment.Controls.OfType<Panel>().First().Controls.OfType<ComboBox>().First();
                        ComboBox cmbMemberType = pnlMemberType.Controls.OfType<Panel>().First().Controls.OfType<ComboBox>().First();
                        ComboBox cmbStatus = pnlStatus.Controls.OfType<Panel>().First().Controls.OfType<ComboBox>().First();
                        
                        // Validation
                        if (string.IsNullOrWhiteSpace(txtFirstName.Text) || string.IsNullOrWhiteSpace(txtLastName.Text))
                        {
                            MessageBox.Show("First Name and Last Name are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        
                        string editEmail = txtEmail.Text.Trim();
                        string editMemberType = cmbMemberType.SelectedItem?.ToString();
                        
                        // Validate Email
                        if (!memberService.IsValidMemberEmail(editEmail, editMemberType))
                        {
                            if (editMemberType == "Student" || editMemberType == "Faculty")
                            {
                                MessageBox.Show($"Email must be a valid @umindanao.edu.ph address for {editMemberType} members.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            else
                            {
                                MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            return;
                        }
                        
                        // Validate Phone
                        string editPhone = txtPhone.Text.Trim();
                        if (!string.IsNullOrEmpty(editPhone) && !memberService.IsValidPhoneNumber(editPhone))
                        {
                            MessageBox.Show("Phone number must be 11 digits starting with 09.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        
                        // Get department
                        string editDepartment = cmbDepartment.SelectedIndex > 0 ? cmbDepartment.SelectedItem?.ToString() : null;
                        
                        // Get status value
                        int statusValue = 1; // Active by default
                        string statusText = cmbStatus.SelectedItem?.ToString() ?? "Active";
                        if (statusText == "Inactive") statusValue = 0;
                        else if (statusText == "Active") statusValue = 1;
                        else if (statusText == "Expired") statusValue = 3;
                        
                        // Update member in database
                        bool success = memberService.UpdateMember(
                            memberId,
                            txtFirstName.Text.Trim(),
                            txtLastName.Text.Trim(),
                            editEmail,
                            editPhone,
                            txtAddress.Text.Trim(),
                            editDepartment,
                            editMemberType,
                            statusValue
                        );
                        
                        if (success)
                        {
                            MessageBox.Show("Member information updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadMembersData();
                        editForm.DialogResult = DialogResult.OK;
                        }
                        else
                        {
                            MessageBox.Show("Failed to update member information. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    };
                    
                    footerPanel.Controls.Add(btnCancel);
                    footerPanel.Controls.Add(btnSave);
                    
                    // Position buttons properly
                    Action positionButtons = () =>
                    {
                        btnSave.Location = new Point(footerPanel.Width - 30 - 130, 15);
                        btnCancel.Location = new Point(footerPanel.Width - 30 - 130 - 110, 15);
                    };
                    
                    footerPanel.Resize += (s, e) => positionButtons();
                    
                    // Initial positioning
                    editForm.Load += (s, e) => positionButtons();
                    
                    mainPanel.Controls.Add(footerPanel);
                    mainPanel.Controls.Add(contentPanel);
                    mainPanel.Controls.Add(headerPanel);
                    
                    // Form border/shadow effect
                    editForm.Paint += (s, e) =>
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        
                        // Draw shadow layers
                        for (int i = 0; i < 5; i++)
                        {
                            int offset = i * 2;
                            int alpha = 30 - (i * 5);
                            using (GraphicsPath shadowPath = CreateRoundedRectangle(
                                new Rectangle(offset, offset, editForm.Width - 1 - offset, editForm.Height - 1 - offset), 12))
                            using (Pen shadowPen = new Pen(Color.FromArgb(alpha, 0, 0, 0), 1))
                                e.Graphics.DrawPath(shadowPen, shadowPath);
                        }
                        
                        // Draw main border
                        using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, editForm.Width - 1, editForm.Height - 1), 12))
                        using (Pen pen = new Pen(Color.FromArgb(180, 180, 180), 2))
                            e.Graphics.DrawPath(pen, path);
                    };
                    
                    editForm.Controls.Add(mainPanel);
                    
                    // Dialog will handle save in btnSave.Click event
                    editForm.ShowDialog();
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
                
                // Load member data to display name in confirmation
                var memberService = new Service.MemberService();
                var memberData = memberService.GetMemberByNumber(memberId);
                
                if (memberData == null)
                {
                    MessageBox.Show("Member not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                // Confirmation dialog
                DialogResult result = MessageBox.Show(
                    $"Are you sure you want to delete this member?\n\n" +
                    $"Name: {memberData.FirstName} {memberData.LastName}\n" +
                    $"Member ID: {memberId}\n" +
                    $"Email: {memberData.Email}\n\n" +
                    $"This action cannot be undone and will permanently remove all member data from the database.",
                    "Confirm Delete Member",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        // Delete member from database
                        bool success = memberService.DeleteMember(memberId);
                        
                        if (success)
                        {
                            MessageBox.Show(
                                $"✅ Member Deleted Successfully\n\n" +
                                $"Name: {memberData.FirstName} {memberData.LastName}\n" +
                                $"Member ID: {memberId}\n\n" +
                                $"All member data has been permanently removed from the database.", 
                                "Member Deleted", 
                                MessageBoxButtons.OK, 
                                MessageBoxIcon.Information);
                    LoadMembersData();
                        }
                        else
                        {
                            MessageBox.Show(
                                "❌ Failed to delete member.\n\n" +
                                "The member was not found in the database.", 
                                "Delete Failed", 
                                MessageBoxButtons.OK, 
                                MessageBoxIcon.Error);
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Foreign key constraint error - member has dependencies
                        MessageBox.Show(
                            $"❌ Cannot Delete Member\n\n" +
                            $"This member cannot be deleted because they have:\n" +
                            $"• Active book borrowings\n" +
                            $"• Pending fines or fees\n" +
                            $"• Other related records\n\n" +
                            $"Please resolve these dependencies first, then try again.\n\n" +
                            $"Technical details: {ex.Message}", 
                            "Delete Failed - Dependencies Exist", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Warning);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"❌ Error Deleting Member\n\n" +
                            $"An unexpected error occurred:\n{ex.Message}", 
                            "Delete Failed", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Error);
                    }
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
            Panel cardTotalTitles = CreateCatalogStatCard("📚", "0", "Total Titles", ThemeConstants.PrimaryMaroon, new Point(0, 0), "TotalTitles");
            Panel cardAvailableCopies = CreateCatalogStatCard("📖", "0", "Available Copies", Color.FromArgb(76, 175, 80), new Point(200, 0), "AvailableCopies");
            Panel cardTotalCopies = CreateCatalogStatCard("📚", "0", "Total Copies", Color.FromArgb(33, 150, 243), new Point(400, 0), "TotalCopies");
            Panel cardCategories = CreateCatalogStatCard("🔖", "0", "Categories", Color.FromArgb(255, 152, 0), new Point(600, 0), "Categories");
            statsPanel.Tag = "CatalogStatsPanel";
            statsPanel.Controls.AddRange(new Control[] { cardTotalTitles, cardAvailableCopies, cardTotalCopies, cardCategories });
            // Create search bar with enough width for category filter
            var searchBarComponents = CreateConsistentSearchBar("🔍 Search by title, author, or ISBN...", 600);
            Panel searchPanel = searchBarComponents.panel;
            TextBox txtSearchBooks = searchBarComponents.textBox;
            Button btnSearchBooks = searchBarComponents.button;
            searchPanel.Location = new Point(30, 240);
            // Expand panel width to accommodate category filter
            searchPanel.Width = Math.Max(searchPanel.Width, 1100);
            
            // Position category filter after search button with proper spacing
            int categoryFilterX = btnSearchBooks.Location.X + btnSearchBooks.Width + 20;
            
            Label lblCategoryFilter = new Label
            {
                Text = "Category:",
                Location = new Point(categoryFilterX, 18),
                Size = new Size(70, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.Black,
                BackColor = Color.Transparent
            };
            searchPanel.Controls.Add(lblCategoryFilter);
            
            ComboBox cmbCategoryFilter = new ComboBox
            {
                Location = new Point(categoryFilterX + 75, 15),
                Size = new Size(180, 30),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.Black
            };
            
            // Function to refresh category filter
            Action refreshCategoryFilter = () =>
            {
                cmbCategoryFilter.Items.Clear();
                cmbCategoryFilter.Items.Add("All Categories");
                
                // Load categories from database
                var bookService = new LMS_Library_Management_System.Service.BookService();
                try
                {
                    var categories = bookService.GetAllCategories();
                    foreach (var category in categories)
                    {
                        if (!string.IsNullOrWhiteSpace(category))
                        {
                            cmbCategoryFilter.Items.Add(category);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
                }
                
                // Restore selection if possible
                if (cmbCategoryFilter.Items.Count > 0)
                {
                    string previousSelection = cmbCategoryFilter.SelectedItem?.ToString();
                    cmbCategoryFilter.SelectedIndex = 0;
                    
                    // Try to restore previous selection
                    if (!string.IsNullOrEmpty(previousSelection))
                    {
                        for (int i = 0; i < cmbCategoryFilter.Items.Count; i++)
                        {
                            if (cmbCategoryFilter.Items[i].ToString() == previousSelection)
                            {
                                cmbCategoryFilter.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }
            };
            
            // Initial load
            refreshCategoryFilter();
            
            // Search button click event
            btnSearchBooks.Click += (s, e) =>
            {
                string searchTerm = txtSearchBooks.GetActualText();
                string selectedCategory = cmbCategoryFilter?.SelectedItem?.ToString() ?? "All Categories";
                LoadBooksData(searchTerm, selectedCategory);
            };
            
            // Search on Enter key press
            txtSearchBooks.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    string searchTerm = txtSearchBooks.GetActualText();
                    string selectedCategory = cmbCategoryFilter?.SelectedItem?.ToString() ?? "All Categories";
                    LoadBooksData(searchTerm, selectedCategory);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            
            // Category filter change event
            cmbCategoryFilter.SelectedIndexChanged += (s, e) =>
            {
                string searchTerm = txtSearchBooks.GetActualText();
                string selectedCategory = cmbCategoryFilter.SelectedItem?.ToString() ?? "All Categories";
                LoadBooksData(searchTerm, selectedCategory);
            };
            
            searchPanel.Controls.Add(cmbCategoryFilter);
            
            // Store reference to refresh function in panel tag for later use
            searchPanel.Tag = new { RefreshCategories = refreshCategoryFilter, CategoryComboBox = cmbCategoryFilter };
            
            // Scrollable container for book cards
            Panel pnlBooksContainer = new Panel
            {
                Location = new Point(30, 320),
                Size = new Size(pnlMainContent.Width - 60, pnlMainContent.Height - 350),
                BackColor = Color.FromArgb(250, 250, 250),
                AutoScroll = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlBooksContainer.Name = "pnlBooksContainer";
            pnlMainContent.Tag = "CatalogView";
            pnlMainContent.Controls.AddRange(new Control[] { titleLabel, subtitleLabel, btnAddBookHeader, statsPanel, searchPanel, pnlBooksContainer });
            
            // Load statistics immediately
            UpdateCatalogStatistics();
            
            // Load books data
            LoadBooksData();
        }
        
        /// <summary>
        /// Recursively finds a control by name
        /// </summary>
        private Control FindControlByName(Control parent, string name)
        {
            if (parent == null || string.IsNullOrEmpty(name)) return null;
            
            if (parent.Name == name)
                return parent;
            
            foreach (Control child in parent.Controls)
            {
                Control found = FindControlByName(child, name);
                if (found != null)
                    return found;
            }
            
            return null;
        }
        
        /// <summary>
        /// Refreshes the category filter dropdown with latest categories from database
        /// </summary>
        private void RefreshCategoryFilter()
        {
            try
            {
                // Find the category filter ComboBox
                ComboBox cmbCategoryFilter = FindCategoryFilterComboBox();
                if (cmbCategoryFilter != null)
                {
                    string previousSelection = cmbCategoryFilter.SelectedItem?.ToString();
                    
                    cmbCategoryFilter.Items.Clear();
                    cmbCategoryFilter.Items.Add("All Categories");
                    
                    var bookService = new LMS_Library_Management_System.Service.BookService();
                    var categories = bookService.GetAllCategories();
                    foreach (var category in categories)
                    {
                        if (!string.IsNullOrWhiteSpace(category))
                        {
                            cmbCategoryFilter.Items.Add(category);
                        }
                    }
                    
                    // Restore previous selection if possible
                    if (cmbCategoryFilter.Items.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(previousSelection) && previousSelection != "All Categories")
                        {
                            for (int i = 0; i < cmbCategoryFilter.Items.Count; i++)
                            {
                                if (cmbCategoryFilter.Items[i].ToString() == previousSelection)
                                {
                                    cmbCategoryFilter.SelectedIndex = i;
                                    System.Diagnostics.Debug.WriteLine($"RefreshCategoryFilter: Restored selection to: {previousSelection}");
                                    return;
                                }
                            }
                        }
                        cmbCategoryFilter.SelectedIndex = 0;
                    }
                    System.Diagnostics.Debug.WriteLine($"RefreshCategoryFilter: Category filter refreshed with {cmbCategoryFilter.Items.Count} items");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("RefreshCategoryFilter: Category filter ComboBox not found");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshCategoryFilter Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Finds the category filter ComboBox in the catalog view
        /// </summary>
        private ComboBox FindCategoryFilterComboBox()
        {
            foreach (Control control in pnlMainContent.Controls)
            {
                if (control is Panel searchPanel)
                {
                    foreach (Control child in searchPanel.Controls)
                    {
                        if (child is ComboBox cmb && cmb.DropDownStyle == ComboBoxStyle.DropDownList)
                        {
                            // Check if it's the category filter by checking if it has "All Categories"
                            if (cmb.Items.Count > 0 && cmb.Items[0].ToString() == "All Categories")
                            {
                                return cmb;
                            }
                        }
                    }
                }
            }
            return null;
        }
        
        /// <summary>
        /// Updates catalog statistics from database
        /// </summary>
        private void UpdateCatalogStatistics()
        {
            try
            {
                var bookService = new LMS_Library_Management_System.Service.BookService();
                var stats = bookService.GetBookStatistics();
                
                System.Diagnostics.Debug.WriteLine($"UpdateCatalogStatistics: TotalTitles={(stats.ContainsKey("TotalTitles") ? stats["TotalTitles"].ToString() : "N/A")}, " +
                    $"TotalCopies={(stats.ContainsKey("TotalCopies") ? stats["TotalCopies"].ToString() : "N/A")}, " +
                    $"AvailableCopies={(stats.ContainsKey("AvailableCopies") ? stats["AvailableCopies"].ToString() : "N/A")}, " +
                    $"Categories={(stats.ContainsKey("Categories") ? stats["Categories"].ToString() : "N/A")}");
                
                // Find and update stat cards
                bool statsPanelFound = false;
                foreach (Control control in pnlMainContent.Controls)
                {
                    if (control is Panel statsPanel && statsPanel.Tag?.ToString() == "CatalogStatsPanel")
                    {
                        statsPanelFound = true;
                        System.Diagnostics.Debug.WriteLine("UpdateCatalogStatistics: Found CatalogStatsPanel");
                        int cardsFound = 0;
                        int labelsUpdated = 0;
                        
                        foreach (Control statCard in statsPanel.Controls)
                        {
                            if (statCard is Panel card)
                            {
                                cardsFound++;
                                foreach (Control label in card.Controls)
                                {
                                    if (label is Label lbl && lbl.Tag != null)
                                    {
                                        string tag = lbl.Tag.ToString();
                                        
                                        if (tag == "TotalTitles" && stats.ContainsKey("TotalTitles"))
                                        {
                                            lbl.Text = stats["TotalTitles"].ToString();
                                            labelsUpdated++;
                                            System.Diagnostics.Debug.WriteLine($"UpdateCatalogStatistics: Updated TotalTitles to: {stats["TotalTitles"]}");
                                        }
                                        else if (tag == "AvailableCopies" && stats.ContainsKey("AvailableCopies"))
                                        {
                                            lbl.Text = stats["AvailableCopies"].ToString();
                                            labelsUpdated++;
                                            System.Diagnostics.Debug.WriteLine($"UpdateCatalogStatistics: Updated AvailableCopies to: {stats["AvailableCopies"]}");
                                        }
                                        else if (tag == "TotalCopies" && stats.ContainsKey("TotalCopies"))
                                        {
                                            lbl.Text = stats["TotalCopies"].ToString();
                                            labelsUpdated++;
                                            System.Diagnostics.Debug.WriteLine($"UpdateCatalogStatistics: Updated TotalCopies to: {stats["TotalCopies"]}");
                                        }
                                        else if (tag == "Categories" && stats.ContainsKey("Categories"))
                                        {
                                            lbl.Text = stats["Categories"].ToString();
                                            labelsUpdated++;
                                            System.Diagnostics.Debug.WriteLine($"UpdateCatalogStatistics: Updated Categories to: {stats["Categories"]}");
                                        }
                                    }
                                }
                            }
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"UpdateCatalogStatistics: Found {cardsFound} cards, Updated {labelsUpdated} labels");
                        break;
                    }
                }
                
                if (!statsPanelFound)
                {
                    System.Diagnostics.Debug.WriteLine("UpdateCatalogStatistics: WARNING - CatalogStatsPanel not found");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCatalogStatistics Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"UpdateCatalogStatistics StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Loads books data from database and populates the book cards grid
        /// </summary>
        private void LoadBooksData(string searchTerm = null, string category = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"LoadBooksData: Starting - searchTerm='{searchTerm}', category='{category}'");
                
                // Find the books container panel
                Panel pnlBooksContainer = null;
                foreach (Control control in pnlMainContent.Controls)
                {
                    if (control is Panel pnl && pnl.Name == "pnlBooksContainer")
                    {
                        pnlBooksContainer = pnl;
                        break;
                    }
                }

                if (pnlBooksContainer == null)
                {
                    System.Diagnostics.Debug.WriteLine("LoadBooksData: ERROR - pnlBooksContainer not found!");
                    // Try to find it by iterating through all controls recursively
                    pnlBooksContainer = FindControlByName(pnlMainContent, "pnlBooksContainer") as Panel;
                    if (pnlBooksContainer == null)
                    {
                        System.Diagnostics.Debug.WriteLine("LoadBooksData: Could not find container even with recursive search");
                        return;
                    }
                    System.Diagnostics.Debug.WriteLine("LoadBooksData: Found container using recursive search");
                }

                System.Diagnostics.Debug.WriteLine($"LoadBooksData: Found container, clearing {pnlBooksContainer.Controls.Count} existing cards");
                
                // Clear existing cards - use SuspendLayout for better performance
                pnlBooksContainer.SuspendLayout();
                pnlBooksContainer.Controls.Clear();
                pnlBooksContainer.ResumeLayout();

                // Get books from database (with search and filter)
                var bookService = new LMS_Library_Management_System.Service.BookService();
                List<Book> books;
                
                // Normalize category - treat "All Categories" as null/empty
                string normalizedCategory = (string.IsNullOrWhiteSpace(category) || category == "All Categories") ? null : category;
                string normalizedSearchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm;
                
                if (normalizedSearchTerm == null && normalizedCategory == null)
                {
                    // No filters, get all books
                    System.Diagnostics.Debug.WriteLine("LoadBooksData: Getting all books (no filters)");
                    books = bookService.GetAllBooks();
                }
                else
                {
                    // Apply search and/or category filter
                    System.Diagnostics.Debug.WriteLine($"LoadBooksData: Searching with term='{normalizedSearchTerm}', category='{normalizedCategory}'");
                    books = bookService.SearchBooks(normalizedSearchTerm, normalizedCategory);
                }

                System.Diagnostics.Debug.WriteLine($"LoadBooksData: Retrieved {books?.Count ?? 0} books from database");

                // Create book cards
                int cardWidth = 220;
                int cardHeight = 380;
                int gap = 20;
                int cardsPerRow = 4;
                int startX = 0;
                int startY = 0;
                int currentX = startX;
                int currentY = startY;
                int row = 0;

                if (books != null && books.Count > 0)
                {
                    pnlBooksContainer.SuspendLayout();
                    int cardsCreated = 0;
                    foreach (var book in books)
                    {
                        try
                        {
                            Panel bookCard = CreateBookCard(book, cardWidth, cardHeight);
                            bookCard.Location = new Point(currentX, currentY);
                            pnlBooksContainer.Controls.Add(bookCard);
                            cardsCreated++;

                            // Move to next position
                            currentX += cardWidth + gap;
                            if ((row + 1) % cardsPerRow == 0)
                            {
                                currentX = startX;
                                currentY += cardHeight + gap;
                                row = 0;
                            }
                            else
                            {
                                row++;
                            }
                        }
                        catch (Exception cardEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"LoadBooksData: Error creating card for book '{book?.Title}': {cardEx.Message}");
                        }
                    }
                    pnlBooksContainer.ResumeLayout(true);
                    System.Diagnostics.Debug.WriteLine($"LoadBooksData: Successfully created {cardsCreated} book cards");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("LoadBooksData: No books to display");
                }

                // Update container height for scrolling
                if (books != null && books.Count > 0)
                {
                    int totalRows = (int)Math.Ceiling((double)books.Count / cardsPerRow);
                    int totalHeight = totalRows * (cardHeight + gap) - gap;
                    pnlBooksContainer.AutoScrollMinSize = new Size(0, totalHeight);
                }

                // Update statistics
                var stats = bookService.GetBookStatistics();
                
                System.Diagnostics.Debug.WriteLine($"Statistics retrieved: TotalTitles={(stats.ContainsKey("TotalTitles") ? stats["TotalTitles"].ToString() : "N/A")}, " +
                    $"TotalCopies={(stats.ContainsKey("TotalCopies") ? stats["TotalCopies"].ToString() : "N/A")}, " +
                    $"AvailableCopies={(stats.ContainsKey("AvailableCopies") ? stats["AvailableCopies"].ToString() : "N/A")}, " +
                    $"Categories={(stats.ContainsKey("Categories") ? stats["Categories"].ToString() : "N/A")}");
                
                // Find and update stat cards
                bool statsPanelFound = false;
                foreach (Control control in pnlMainContent.Controls)
                {
                    if (control is Panel statsPanel && statsPanel.Tag?.ToString() == "CatalogStatsPanel")
                    {
                        statsPanelFound = true;
                        System.Diagnostics.Debug.WriteLine("Found CatalogStatsPanel");
                        int cardsFound = 0;
                        int labelsUpdated = 0;
                        
                        foreach (Control statCard in statsPanel.Controls)
                        {
                            if (statCard is Panel card)
                            {
                                cardsFound++;
                                foreach (Control label in card.Controls)
                                {
                                    if (label is Label lbl && lbl.Tag != null)
                                    {
                                        string tag = lbl.Tag.ToString();
                                        System.Diagnostics.Debug.WriteLine($"Found label with tag: {tag}");
                                        
                                        if (tag == "TotalTitles" && stats.ContainsKey("TotalTitles"))
                                        {
                                            lbl.Text = stats["TotalTitles"].ToString();
                                            labelsUpdated++;
                                            System.Diagnostics.Debug.WriteLine($"Updated TotalTitles to: {stats["TotalTitles"]}");
                                        }
                                        else if (tag == "AvailableCopies" && stats.ContainsKey("AvailableCopies"))
                                        {
                                            lbl.Text = stats["AvailableCopies"].ToString();
                                            labelsUpdated++;
                                            System.Diagnostics.Debug.WriteLine($"Updated AvailableCopies to: {stats["AvailableCopies"]}");
                                        }
                                        else if (tag == "TotalCopies" && stats.ContainsKey("TotalCopies"))
                                        {
                                            lbl.Text = stats["TotalCopies"].ToString();
                                            labelsUpdated++;
                                            System.Diagnostics.Debug.WriteLine($"Updated TotalCopies to: {stats["TotalCopies"]}");
                                        }
                                        else if (tag == "Categories" && stats.ContainsKey("Categories"))
                                        {
                                            lbl.Text = stats["Categories"].ToString();
                                            labelsUpdated++;
                                            System.Diagnostics.Debug.WriteLine($"Updated Categories to: {stats["Categories"]}");
                                        }
                                    }
                                }
                            }
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"Statistics update complete: Found {cardsFound} cards, Updated {labelsUpdated} labels");
                        break;
                    }
                }
                
                if (!statsPanelFound)
                {
                    System.Diagnostics.Debug.WriteLine("WARNING: CatalogStatsPanel not found in pnlMainContent.Controls");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading books: {ex.Message}");
                MessageBox.Show($"Error loading books: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Creates a book card panel with cover, title, author, category, and action buttons
        /// </summary>
        private Panel CreateBookCard(Book book, int width, int height)
        {
            Panel card = new Panel
            {
                Size = new Size(width, height),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Card shadow and border
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                
                // Shadow
                Rectangle shadowRect = new Rectangle(2, 2, width - 2, height - 2);
                using (var shadowBrush = new SolidBrush(Color.FromArgb(15, 0, 0, 0)))
                using (var shadowPath = CreateRoundedRectangle(shadowRect, 8))
                {
                    e.Graphics.FillPath(shadowBrush, shadowPath);
                }
                
                // Card
                Rectangle cardRect = new Rectangle(0, 0, width - 2, height - 2);
                using (var bgBrush = new SolidBrush(Color.White))
                using (var cardPath = CreateRoundedRectangle(cardRect, 8))
                {
                    e.Graphics.FillPath(bgBrush, cardPath);
                }
                
                // Border
                using (var borderPen = new Pen(Color.FromArgb(230, 230, 230), 1))
                using (var borderPath = CreateRoundedRectangle(cardRect, 8))
                {
                    e.Graphics.DrawPath(borderPen, borderPath);
                }
            };

            // Book cover image placeholder
            Panel pnlCover = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(width - 20, 180),
                BackColor = Color.FromArgb(240, 240, 240)
            };
            pnlCover.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // Draw book cover placeholder
                using (var brush = new SolidBrush(Color.FromArgb(200, 200, 200)))
                {
                    e.Graphics.FillRectangle(brush, 0, 0, pnlCover.Width, pnlCover.Height);
                }
                // Draw book icon
                using (var font = new Font("Segoe UI", 48))
                using (var brush = new SolidBrush(Color.FromArgb(150, 150, 150)))
                {
                    e.Graphics.DrawString("📚", font, brush, new RectangleF(0, 0, pnlCover.Width, pnlCover.Height), 
                        new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                }
            };

            // Availability badge
            Label lblAvailability = new Label
            {
                Text = $"{book.AvailableCopies} Available",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 193, 7),
                BackColor = Color.FromArgb(255, 248, 220),
                AutoSize = true,
                Location = new Point(width - 120, 15),
                Padding = new Padding(6, 3, 6, 3),
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblAvailability.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectangle(new Rectangle(0, 0, lblAvailability.Width, lblAvailability.Height), 12))
                using (var brush = new SolidBrush(lblAvailability.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
                TextRenderer.DrawText(e.Graphics, lblAvailability.Text, lblAvailability.Font, 
                    new Rectangle(0, 0, lblAvailability.Width, lblAvailability.Height), 
                    lblAvailability.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            // Title
            Label lblTitle = new Label
            {
                Text = book.Title,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Location = new Point(10, 200),
                Size = new Size(width - 20, 25),
                AutoEllipsis = true
            };

            // Author
            Label lblAuthor = new Label
            {
                Text = book.Author,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(10, 230),
                Size = new Size(width - 20, 20),
                AutoEllipsis = true
            };

            // Category tag
            Label lblCategory = new Label
            {
                Text = book.Category ?? "Uncategorized",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(100, 100, 100),
                BackColor = Color.FromArgb(245, 245, 245),
                AutoSize = true,
                Location = new Point(10, 255),
                Padding = new Padding(8, 4, 8, 4),
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblCategory.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectangle(new Rectangle(0, 0, lblCategory.Width, lblCategory.Height), 10))
                using (var brush = new SolidBrush(lblCategory.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
                TextRenderer.DrawText(e.Graphics, lblCategory.Text, lblCategory.Font, 
                    new Rectangle(0, 0, lblCategory.Width, lblCategory.Height), 
                    lblCategory.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            // Action buttons container
            Panel pnlActions = new Panel
            {
                Location = new Point(10, height - 45),
                Size = new Size(width - 20, 35),
                BackColor = Color.Transparent
            };

            // View button
            Label btnView = new Label
            {
                Text = "👁",
                Font = new Font("Segoe UI", 16F),
                Size = new Size(35, 35),
                Location = new Point(0, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Tag = book.BookId
            };
            btnView.MouseEnter += (s, e) => btnView.BackColor = Color.FromArgb(240, 240, 240);
            btnView.MouseLeave += (s, e) => btnView.BackColor = Color.Transparent;
            btnView.Click += (s, e) => {
                ShowViewBookDialog(book.BookId);
            };

            // Edit button
            Label btnEdit = new Label
            {
                Text = "✏️",
                Font = new Font("Segoe UI", 16F),
                Size = new Size(35, 35),
                Location = new Point(40, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Tag = book.BookId
            };
            btnEdit.MouseEnter += (s, e) => btnEdit.BackColor = Color.FromArgb(240, 240, 240);
            btnEdit.MouseLeave += (s, e) => btnEdit.BackColor = Color.Transparent;
            btnEdit.Click += (s, e) => {
                ShowEditBookDialog(book.BookId);
            };

            pnlActions.Controls.AddRange(new Control[] { btnView, btnEdit });

            card.Controls.AddRange(new Control[] { pnlCover, lblAvailability, lblTitle, lblAuthor, lblCategory, pnlActions });

            return card;
        }

        /// <summary>
        /// Shows the View Book dialog with full book details from database
        /// </summary>
        private void ShowViewBookDialog(int bookId)
        {
            try
            {
                // Get book data from database
                var bookService = new LMS_Library_Management_System.Service.BookService();
                var book = bookService.GetBookById(bookId);

                if (book == null)
                {
                    MessageBox.Show("Book not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Calculate derived values
                int borrowed = book.TotalCopies - book.AvailableCopies;
                string accessionNumber = $"ACC-{book.CreatedDate.Year}-{book.BookId:D5}";
                string callNumber = GenerateCallNumber(book.Category, book.Author, book.Title);
                
                // Create form
                Form viewForm = new Form
                {
                    Text = "",
                    Size = new Size(800, 900),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.None,
                    BackColor = Color.FromArgb(245, 240, 235),
                    ShowInTaskbar = false
                };

                // Border
                viewForm.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen p = new Pen(Color.FromArgb(200, 200, 200), 1))
                    {
                        e.Graphics.DrawRectangle(p, 0, 0, viewForm.Width - 1, viewForm.Height - 1);
                    }
                };

                // Close button (X)
                Label btnCloseX = new Label
                {
                    Text = "×",
                    Font = new Font("Arial", 18),
                    ForeColor = Color.Gray,
                    Location = new Point(viewForm.Width - 40, 10),
                    Size = new Size(30, 30),
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                btnCloseX.Click += (s, e) => viewForm.Close();
                btnCloseX.MouseEnter += (s, e) => btnCloseX.ForeColor = Color.Black;
                btnCloseX.MouseLeave += (s, e) => btnCloseX.ForeColor = Color.Gray;
                viewForm.Controls.Add(btnCloseX);

                // Header: Title and Accession Number
                Label lblBookTitle = new Label
                {
                    Text = book.Title,
                    Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(40, 40, 40),
                    Location = new Point(30, 20),
                    AutoSize = true
                };
                viewForm.Controls.Add(lblBookTitle);

                Label lblAccession = new Label
                {
                    Text = accessionNumber,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Gray,
                    Location = new Point(30, 50),
                    AutoSize = true
                };
                viewForm.Controls.Add(lblAccession);

                // Main content panel (scrollable)
                Panel mainContent = new Panel
                {
                    Location = new Point(0, 80),
                    Size = new Size(viewForm.Width, viewForm.Height - 150),
                    AutoScroll = true,
                    BackColor = Color.Transparent
                };

                // Book Info Section (Top)
                Panel pnlBookInfo = new Panel
                {
                    Location = new Point(30, 20),
                    Size = new Size(viewForm.Width - 60, 180),
                    BackColor = Color.White
                };
                pnlBookInfo.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var path = CreateRoundedRectangle(new Rectangle(0, 0, pnlBookInfo.Width - 1, pnlBookInfo.Height - 1), 8))
                    using (var brush = new SolidBrush(Color.White))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (var pen = new Pen(Color.FromArgb(230, 230, 230), 1))
                    using (var path = CreateRoundedRectangle(new Rectangle(0, 0, pnlBookInfo.Width - 1, pnlBookInfo.Height - 1), 8))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                };

                // Book cover placeholder
                Panel pnlCover = new Panel
                {
                    Location = new Point(20, 20),
                    Size = new Size(120, 140),
                    BackColor = Color.FromArgb(240, 240, 240)
                };
                pnlCover.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var brush = new SolidBrush(Color.FromArgb(200, 200, 200)))
                    {
                        e.Graphics.FillRectangle(brush, 0, 0, pnlCover.Width, pnlCover.Height);
                    }
                    using (var font = new Font("Segoe UI", 36))
                    using (var brush = new SolidBrush(Color.FromArgb(150, 150, 150)))
                    {
                        e.Graphics.DrawString("📚", font, brush, new RectangleF(0, 0, pnlCover.Width, pnlCover.Height),
                            new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                    }
                };

                // Book details (right of cover)
                Label lblTitle = new Label
            {
                    Text = book.Title,
                    Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(40, 40, 40),
                    Location = new Point(160, 20),
                    AutoSize = true
                };

                Label lblSubtitle = new Label
                {
                    Text = ExtractSubtitle(book.Description) ?? "",
                    Font = new Font("Segoe UI", 11F),
                    ForeColor = Color.FromArgb(100, 100, 100),
                    Location = new Point(160, 50),
                    AutoSize = true
                };

                Label lblAuthor = new Label
                {
                    Text = $"by {book.Author}",
                Font = new Font("Segoe UI", 10F),
                    ForeColor = Color.FromArgb(100, 100, 100),
                    Location = new Point(160, 80),
                    AutoSize = true
                };

                // Badges
                Label lblAvailabilityBadge = new Label
                {
                    Text = $"{book.AvailableCopies} Available",
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(255, 152, 0),
                    BackColor = Color.FromArgb(255, 248, 220),
                    AutoSize = true,
                    Location = new Point(160, 110),
                    Padding = new Padding(10, 5, 10, 5),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                lblAvailabilityBadge.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var path = CreateRoundedRectangle(new Rectangle(0, 0, lblAvailabilityBadge.Width, lblAvailabilityBadge.Height), 15))
                    using (var brush = new SolidBrush(lblAvailabilityBadge.BackColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    TextRenderer.DrawText(e.Graphics, lblAvailabilityBadge.Text, lblAvailabilityBadge.Font,
                        new Rectangle(0, 0, lblAvailabilityBadge.Width, lblAvailabilityBadge.Height),
                        lblAvailabilityBadge.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                };

                Label lblCategoryBadge = new Label
                {
                    Text = book.Category ?? "Uncategorized",
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = Color.FromArgb(100, 100, 100),
                    BackColor = Color.FromArgb(245, 245, 245),
                    AutoSize = true,
                    Location = new Point(160, 145),
                    Padding = new Padding(10, 5, 10, 5),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                lblCategoryBadge.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var path = CreateRoundedRectangle(new Rectangle(0, 0, lblCategoryBadge.Width, lblCategoryBadge.Height), 15))
                    using (var brush = new SolidBrush(lblCategoryBadge.BackColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    TextRenderer.DrawText(e.Graphics, lblCategoryBadge.Text, lblCategoryBadge.Font,
                        new Rectangle(0, 0, lblCategoryBadge.Width, lblCategoryBadge.Height),
                        lblCategoryBadge.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                };

                Label lblTypeBadge = new Label
                {
                    Text = "Book",
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = Color.FromArgb(100, 100, 100),
                    BackColor = Color.FromArgb(245, 245, 245),
                    AutoSize = true,
                    Location = new Point(280, 145),
                    Padding = new Padding(10, 5, 10, 5),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                lblTypeBadge.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var path = CreateRoundedRectangle(new Rectangle(0, 0, lblTypeBadge.Width, lblTypeBadge.Height), 15))
                    using (var brush = new SolidBrush(lblTypeBadge.BackColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    TextRenderer.DrawText(e.Graphics, lblTypeBadge.Text, lblTypeBadge.Font,
                        new Rectangle(0, 0, lblTypeBadge.Width, lblTypeBadge.Height),
                        lblTypeBadge.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                };

                pnlBookInfo.Controls.AddRange(new Control[] { pnlCover, lblTitle, lblSubtitle, lblAuthor, lblAvailabilityBadge, lblCategoryBadge, lblTypeBadge });

                // Book Details Card
                Panel pnlBookDetails = CreateDetailCard("Book Details", new Point(30, 220), new Size(360, 200));
                AddDetailRow(pnlBookDetails, "ISBN:", book.ISBN ?? "N/A", 20);
                AddDetailRow(pnlBookDetails, "Call Number:", callNumber, 50);
                AddDetailRow(pnlBookDetails, "Publisher:", book.Publisher ?? "N/A", 80);
                AddDetailRow(pnlBookDetails, "Year:", book.PublicationYear?.ToString() ?? "N/A", 110);
                AddDetailRow(pnlBookDetails, "Pages:", "N/A", 140); // Not in database
                AddDetailRow(pnlBookDetails, "Language:", "English", 170); // Default or from database if added

                // Inventory Card
                Panel pnlInventory = CreateDetailCard("Inventory", new Point(410, 220), new Size(360, 200));
                AddDetailRow(pnlInventory, "Location:", "Section D, Shelf 1", 20); // Placeholder - not in database
                AddDetailRow(pnlInventory, "Total Copies:", book.TotalCopies.ToString(), 50);
                Label lblAvailable = AddDetailRow(pnlInventory, "Available:", book.AvailableCopies.ToString(), 80);
                lblAvailable.ForeColor = Color.FromArgb(76, 175, 80);
                AddDetailRow(pnlInventory, "Borrowed:", borrowed.ToString(), 110);
                AddDetailRow(pnlInventory, "Added:", book.CreatedDate.ToString("MMM d, yyyy"), 140);

                // Description Card
                Panel pnlDescription = CreateDetailCard("Description", new Point(30, 440), new Size(740, 120));
                Label lblDescription = new Label
                {
                    Text = !string.IsNullOrWhiteSpace(book.Description) ? book.Description : "No description available.",
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = Color.FromArgb(60, 60, 60),
                    Location = new Point(20, 50),
                    Size = new Size(700, 60),
                    AutoEllipsis = true
                };
                pnlDescription.Controls.Add(lblDescription);

                // Add Copies Section
                Panel pnlAddCopies = new Panel
            {
                    Location = new Point(30, 580),
                    Size = new Size(740, 80),
                    BackColor = Color.White
                };
                pnlAddCopies.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var path = CreateRoundedRectangle(new Rectangle(0, 0, pnlAddCopies.Width - 1, pnlAddCopies.Height - 1), 8))
                    using (var brush = new SolidBrush(Color.White))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (var pen = new Pen(Color.FromArgb(230, 230, 230), 1))
                    using (var path = CreateRoundedRectangle(new Rectangle(0, 0, pnlAddCopies.Width - 1, pnlAddCopies.Height - 1), 8))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                };

                Label lblAddCopiesTitle = new Label
                {
                    Text = "Add Copies",
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(40, 40, 40),
                    Location = new Point(20, 20),
                    AutoSize = true
                };

                // Numeric input for copies
                Panel pnlCopiesInput = new Panel
                {
                    Location = new Point(20, 45),
                    Size = new Size(120, 30),
                    BackColor = Color.FromArgb(245, 245, 245)
                };
                TextBox txtCopies = new TextBox
                {
                    Text = "1",
                    Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                    BackColor = Color.FromArgb(245, 245, 245),
                    Location = new Point(10, 5),
                    Size = new Size(60, 20),
                    TextAlign = HorizontalAlignment.Center
                };
                txtCopies.KeyPress += (s, e) =>
                {
                    if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                        e.Handled = true;
                };

                // Up/Down buttons
                Label btnUp = new Label
                {
                    Text = "▲",
                    Font = new Font("Segoe UI", 8F),
                    ForeColor = Color.Gray,
                    Location = new Point(75, 2),
                    Size = new Size(15, 13),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand
                };
                btnUp.Click += (s, e) =>
                {
                    if (int.TryParse(txtCopies.Text, out int val))
                    {
                        txtCopies.Text = (val + 1).ToString();
                    }
                };

                Label btnDown = new Label
                {
                    Text = "▼",
                    Font = new Font("Segoe UI", 8F),
                    ForeColor = Color.Gray,
                    Location = new Point(75, 15),
                    Size = new Size(15, 13),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand
                };
                btnDown.Click += (s, e) =>
                {
                    if (int.TryParse(txtCopies.Text, out int val) && val > 1)
                    {
                        txtCopies.Text = (val - 1).ToString();
                    }
                };

                pnlCopiesInput.Controls.AddRange(new Control[] { txtCopies, btnUp, btnDown });

                Button btnAddCopies = new Button
                {
                    Text = "+ Add Copies",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = ThemeConstants.PrimaryMaroon,
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(150, 45),
                    Size = new Size(130, 30),
                    Cursor = Cursors.Hand
                };
                btnAddCopies.FlatAppearance.BorderSize = 0;
                btnAddCopies.Click += (s, e) =>
                {
                    if (int.TryParse(txtCopies.Text, out int copies) && copies > 0)
                    {
                        try
                        {
                            // Add copies to database
                            if (bookService.AddCopies(bookId, copies))
                            {
                                string copyText = copies == 1 ? "copy" : "copies";
                                int newTotal = book.TotalCopies + copies;
                                int newAvailable = book.AvailableCopies + copies;
                                
                                MessageBox.Show(
                                    $"Successfully added {copies} {copyText} to {book.Title}.\n\nTotal copies: {newTotal}\nAvailable copies: {newAvailable}",
                                    "Add Copies",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information
                                );
                                
                                // Close the dialog and refresh the book list
                                viewForm.Close();
                                
                                // Refresh book list, statistics, and category filter if we're in Catalog view
                                if (pnlMainContent.Tag?.ToString() == "CatalogView")
                                {
                                    LoadBooksData();
                                    UpdateCatalogStatistics();
                                    RefreshCategoryFilter();
                                }
                            }
                            else
                            {
                                MessageBox.Show("Failed to add copies. Book may not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error adding copies: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            System.Diagnostics.Debug.WriteLine($"Error in AddCopies: {ex.Message}");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please enter a valid number of copies (greater than 0).", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };

                pnlAddCopies.Controls.AddRange(new Control[] { lblAddCopiesTitle, pnlCopiesInput, btnAddCopies });

                mainContent.Controls.AddRange(new Control[] { pnlBookInfo, pnlBookDetails, pnlInventory, pnlDescription, pnlAddCopies });
                viewForm.Controls.Add(mainContent);

                // Footer buttons
                Panel pnlFooter = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 70,
                    BackColor = Color.FromArgb(245, 240, 235)
            };

                Button btnClose = new Button
                {
                    Text = "Close",
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = Color.Black,
                BackColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(viewForm.Width - 350, 20),
                    Size = new Size(100, 35),
                    Cursor = Cursors.Hand
                };
                btnClose.Click += (s, e) => viewForm.Close();

                Button btnDelete = new Button
                {
                    Text = "🗑 Delete Book",
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(244, 67, 54),
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(viewForm.Width - 240, 20),
                    Size = new Size(130, 35),
                    Cursor = Cursors.Hand
                };
                btnDelete.FlatAppearance.BorderSize = 0;
                btnDelete.Click += (s, e) =>
                {
                    DialogResult result = MessageBox.Show(
                        $"Are you sure you want to delete '{book.Title}'?\n\nThis action cannot be undone.",
                        "Delete Book",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2
                    );
                    if (result == DialogResult.Yes)
                    {
                        try
                        {
                            if (bookService.DeleteBook(bookId))
                            {
                                MessageBox.Show("Book deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                viewForm.Close();
                                // Refresh book list, statistics, and category filter
                                if (pnlMainContent.Tag?.ToString() == "CatalogView")
                                {
                                    LoadBooksData();
                                    UpdateCatalogStatistics();
                                    RefreshCategoryFilter();
                                }
                            }
                            else
                            {
                                MessageBox.Show("Failed to delete book. It may have active borrowings.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error deleting book: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                };

                Button btnEdit = new Button
                {
                    Text = "✏️ Edit Book",
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = ThemeConstants.PrimaryMaroon,
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(viewForm.Width - 110, 20),
                    Size = new Size(110, 35),
                    Cursor = Cursors.Hand
                };
                btnEdit.FlatAppearance.BorderSize = 0;
                btnEdit.Click += (s, e) =>
                {
                    viewForm.Close();
                    ShowEditBookDialog(bookId);
                };

                pnlFooter.Controls.AddRange(new Control[] { btnClose, btnDelete, btnEdit });
                viewForm.Controls.Add(pnlFooter);

                viewForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading book details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"Error in ShowViewBookDialog: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows the Edit Book dialog with form fields connected to database
        /// </summary>
        private void ShowEditBookDialog(int bookId)
        {
            try
            {
                // Get book data from database
                var bookService = new LMS_Library_Management_System.Service.BookService();
                var book = bookService.GetBookById(bookId);

                if (book == null)
                {
                    MessageBox.Show("Book not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string description = book.Description ?? "";

                // Create form
                Form editForm = new Form
                {
                    Text = "",
                    Size = new Size(600, 650),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.None,
                    BackColor = Color.FromArgb(245, 240, 235),
                    ShowInTaskbar = false
                };

                // Border
                editForm.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen p = new Pen(Color.FromArgb(200, 200, 200), 1))
                    {
                        e.Graphics.DrawRectangle(p, 0, 0, editForm.Width - 1, editForm.Height - 1);
                    }
                };

                // Close button (X)
                Label btnCloseX = new Label
                {
                    Text = "×",
                    Font = new Font("Arial", 18),
                    ForeColor = Color.Gray,
                    Location = new Point(editForm.Width - 40, 10),
                    Size = new Size(30, 30),
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                btnCloseX.Click += (s, e) => editForm.Close();
                btnCloseX.MouseEnter += (s, e) => btnCloseX.ForeColor = Color.Black;
                btnCloseX.MouseLeave += (s, e) => btnCloseX.ForeColor = Color.Gray;
                editForm.Controls.Add(btnCloseX);

                // Header: Title and Accession Number
                Label lblDialogTitle = new Label
                {
                    Text = "Edit Book",
                    Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(40, 40, 40),
                    Location = new Point(30, 20),
                    AutoSize = true
                };
                editForm.Controls.Add(lblDialogTitle);

                string accessionNumber = $"ACC-{book.CreatedDate.Year}-{book.BookId:D5}";
                Label lblAccession = new Label
                {
                    Text = accessionNumber,
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = Color.Gray,
                    Location = new Point(30, 50),
                    AutoSize = true
                };
                editForm.Controls.Add(lblAccession);

                // Main content panel (scrollable)
                Panel mainContent = new Panel
                {
                    Location = new Point(0, 80),
                    Size = new Size(editForm.Width, editForm.Height - 150),
                    AutoScroll = true,
                    BackColor = Color.Transparent
                };

                int yPos = 20;

                // Title field
                Label lblTitle = new Label
                {
                    Text = "Title",
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(60, 60, 60),
                    Location = new Point(30, yPos),
                    AutoSize = true
                };
                TextBox txtTitle = new TextBox
                {
                    Text = book.Title,
                    Font = new Font("Segoe UI", 10F),
                    Location = new Point(30, yPos + 25),
                    Size = new Size(540, 30),
                    BorderStyle = BorderStyle.FixedSingle
                };
                yPos += 70;

                // Language field (ComboBox with DropDown style for type-ahead)
                Label lblLanguage = new Label
                {
                    Text = "Language",
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(60, 60, 60),
                    Location = new Point(30, yPos),
                    AutoSize = true
                };
                ComboBox cmbLanguage = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDown,
                    Font = new Font("Segoe UI", 10F),
                    Location = new Point(30, yPos + 25),
                    Size = new Size(540, 30),
                    Items = { "English", "Tagalog", "Cebuano", "Spanish" }
                };
                if (!string.IsNullOrWhiteSpace(book.Description) && book.Description.Contains("Language:"))
                {
                    // Try to extract language from description if stored there
                    var langMatch = System.Text.RegularExpressions.Regex.Match(book.Description, @"Language:\s*(\w+)");
                    if (langMatch.Success)
                    {
                        cmbLanguage.Text = langMatch.Groups[1].Value;
                    }
                    else
                    {
                        cmbLanguage.Text = "English";
                    }
                }
                else
                {
                    cmbLanguage.Text = "English";
                }
                yPos += 70;

                // Location field
                Label lblLocation = new Label
                {
                    Text = "Location",
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(60, 60, 60),
                    Location = new Point(30, yPos),
                    AutoSize = true
                };
                TextBox txtLocation = new TextBox
                {
                    Text = "Section D, Shelf 1", // Placeholder - not in database yet
                    Font = new Font("Segoe UI", 10F),
                    Location = new Point(30, yPos + 25),
                    Size = new Size(540, 30),
                    BorderStyle = BorderStyle.FixedSingle
                };
                yPos += 70;

                // Description field (multiline)
                Label lblDescription = new Label
                {
                    Text = "Description",
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(60, 60, 60),
                    Location = new Point(30, yPos),
                    AutoSize = true
                };
                TextBox txtDescription = new TextBox
                {
                    Text = description,
                    Font = new Font("Segoe UI", 10F),
                    Location = new Point(30, yPos + 25),
                    Size = new Size(540, 120),
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    BorderStyle = BorderStyle.FixedSingle
                };
                yPos += 150;

                mainContent.Controls.AddRange(new Control[] { 
                    lblTitle, txtTitle, 
                    lblLanguage, cmbLanguage, 
                    lblLocation, txtLocation, 
                    lblDescription, txtDescription 
                });
                editForm.Controls.Add(mainContent);

                // Footer buttons
                Panel pnlFooter = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 70,
                    BackColor = Color.FromArgb(245, 240, 235)
                };

                Button btnCancel = new Button
                {
                    Text = "Cancel",
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = Color.Black,
                    BackColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(editForm.Width - 250, 20),
                    Size = new Size(100, 35),
                    Cursor = Cursors.Hand
                };
                btnCancel.Click += (s, e) => editForm.Close();

                Button btnSave = new Button
                {
                    Text = "Save Changes",
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = ThemeConstants.PrimaryMaroon,
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(editForm.Width - 140, 20),
                    Size = new Size(120, 35),
                    Cursor = Cursors.Hand
                };
                btnSave.FlatAppearance.BorderSize = 0;
                btnSave.Click += (s, e) =>
                {
                    try
                    {
                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(txtTitle.Text))
                        {
                            MessageBox.Show("Title is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            txtTitle.Focus();
                            return;
                        }

                        // Combine language and location with description for storage
                        // (Until Language, Location are added to database)
                        string updatedDescription = txtDescription.Text.Trim();
                        if (!string.IsNullOrWhiteSpace(cmbLanguage.Text))
                        {
                            updatedDescription += $"\n\nLanguage: {cmbLanguage.Text.Trim()}";
                        }
                        if (!string.IsNullOrWhiteSpace(txtLocation.Text))
                        {
                            updatedDescription += $"\n\nLocation: {txtLocation.Text.Trim()}";
                        }

                        // Update book in database
                        // Note: Currently only Title and Description are in database
                        // Language, Location are stored in Description until DB schema is updated
                        if (bookService.UpdateBook(
                            bookId,
                            book.ISBN, // Keep existing ISBN
                            txtTitle.Text.Trim(),
                            book.Author, // Keep existing Author
                            book.Publisher, // Keep existing Publisher
                            book.PublicationYear, // Keep existing Year
                            book.Category, // Keep existing Category
                            book.TotalCopies, // Keep existing TotalCopies
                            book.AvailableCopies, // Keep existing AvailableCopies
                            updatedDescription))
                        {
                            MessageBox.Show(
                                "Book updated successfully.",
                                "Success",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information
                            );
                            
                            editForm.Close();
                            
                            // Refresh book list and statistics if we're in Catalog view
                            if (pnlMainContent.Tag?.ToString() == "CatalogView")
                            {
                                LoadBooksData();
                                UpdateCatalogStatistics();
                            }
                        }
                        else
                        {
                            MessageBox.Show("Failed to update book.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating book: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        System.Diagnostics.Debug.WriteLine($"Error in Save Changes: {ex.Message}");
                    }
                };

                pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnSave });
                editForm.Controls.Add(pnlFooter);

                editForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading book for editing: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"Error in ShowEditBookDialog: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a detail card panel
        /// </summary>
        private Panel CreateDetailCard(string title, Point location, Size size)
        {
            Panel card = new Panel
            {
                Location = location,
                Size = size,
                BackColor = Color.White
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectangle(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8))
                using (var brush = new SolidBrush(Color.White))
                {
                    e.Graphics.FillPath(brush, path);
                }
                using (var pen = new Pen(Color.FromArgb(230, 230, 230), 1))
                using (var path = CreateRoundedRectangle(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Location = new Point(20, 15),
                AutoSize = true
            };
            card.Controls.Add(lblTitle);

            return card;
        }

        /// <summary>
        /// Adds a detail row to a card
        /// </summary>
        private Label AddDetailRow(Panel card, string label, string value, int y)
        {
            Label lblLabel = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(20, y),
                AutoSize = true
            };

            Label lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Location = new Point(150, y),
                AutoSize = true
            };

            card.Controls.Add(lblLabel);
            card.Controls.Add(lblValue);

            return lblValue;
        }

        /// <summary>
        /// Generates a call number from category, author, and title
        /// </summary>
        private string GenerateCallNumber(string category, string author, string title)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(author) || string.IsNullOrWhiteSpace(title))
                return "N/A";

            string catPrefix = category.Length >= 3 ? category.Substring(0, 3).ToUpper() : category.ToUpper();
            string authorPrefix = author.Split(' ').Length > 0 ? author.Split(' ').Last().Substring(0, Math.Min(3, author.Split(' ').Last().Length)).ToUpper() : "UNK";
            string titlePrefix = title.Length >= 3 ? title.Substring(0, 3).ToUpper() : title.ToUpper();

            return $"{catPrefix}-{authorPrefix}-{titlePrefix}";
        }

        /// <summary>
        /// Extracts subtitle from description if available
        /// </summary>
        private string ExtractSubtitle(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return null;

            // Try to extract subtitle (first sentence or line)
            string[] lines = description.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 0 && lines[0].Length < 100)
                return lines[0];

            return null;
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
            Panel cardCurrentlyBorrowed = CreateCatalogStatCard("📚", "0", "Currently Borrowed", Color.FromArgb(33, 150, 243), new Point(0, 0));
            Panel cardOverdue = CreateCatalogStatCard("⚠️", "0", "Overdue", Color.FromArgb(244, 67, 54), new Point(200, 0));
            Panel cardReturnedToday = CreateCatalogStatCard("✓", "0", "Returned Today", Color.FromArgb(76, 175, 80), new Point(400, 0));
            statsPanel.Controls.AddRange(new Control[] { cardCurrentlyBorrowed, cardOverdue, cardReturnedToday });
            var searchBarComponents = CreateConsistentSearchBar("🔍 Search transactions...", 800);
            Panel searchPanel = searchBarComponents.panel;
            TextBox txtSearchBorrowings = searchBarComponents.textBox;
            Button btnSearchBorrowings = searchBarComponents.button;
            searchPanel.Location = new Point(30, 250);
            
            Label lblStatusFilter = new Label
            {
                Text = "Status:",
                Location = new Point(920, 18),
                Size = new Size(60, 25),
                Font = new Font("Segoe UI", 10F)
            };
            searchPanel.Controls.Add(lblStatusFilter);
            
            ComboBox cmbStatusFilter = new ComboBox
            {
                Location = new Point(990, 15),
                Size = new Size(140, 30),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStatusFilter.Items.AddRange(new[] { "All Status", "Active", "Returned", "Overdue" });
            cmbStatusFilter.SelectedIndex = 0;
            searchPanel.Controls.Add(cmbStatusFilter);
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
            Panel cardPending = CreateCatalogStatCard("🕒", "0", "Pending", Color.FromArgb(255, 193, 7), new Point(0, 0));
            Panel cardReady = CreateCatalogStatCard("🔔", "0", "Ready for Pickup", Color.FromArgb(76, 175, 80), new Point(200, 0));
            Panel cardFulfilled = CreateCatalogStatCard("✓", "0", "Fulfilled", Color.FromArgb(33, 150, 243), new Point(400, 0));
            Panel cardExpired = CreateCatalogStatCard("✗", "0", "Expired", Color.FromArgb(158, 158, 158), new Point(600, 0));
            statsPanel.Controls.AddRange(new Control[] { cardPending, cardReady, cardFulfilled, cardExpired });
            var searchBarComponents = CreateConsistentSearchBar("🔍 Search reservations...", 920);
            Panel searchPanel = searchBarComponents.panel;
            TextBox txtSearchReservations = searchBarComponents.textBox;
            Button btnSearchReservations = searchBarComponents.button;
            searchPanel.Location = new Point(30, 190);
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
            Panel cardPendingFines = CreateCatalogStatCard("⚠️", "₱0.00", "Pending Fines", ThemeConstants.PrimaryMaroon, new Point(0, 0));
            Panel cardCollected = CreateCatalogStatCard("✓", "₱0.00", "Collected", Color.FromArgb(76, 175, 80), new Point(200, 0));
            Panel cardWaived = CreateCatalogStatCard("✗", "₱0.00", "Waived", Color.FromArgb(33, 150, 243), new Point(400, 0));
            Panel cardPendingCases = CreateCatalogStatCard("₱", "0", "Pending Cases", Color.FromArgb(255, 193, 7), new Point(600, 0));
            statsPanel.Controls.AddRange(new Control[] { cardPendingFines, cardCollected, cardWaived, cardPendingCases });
            var searchBarComponents = CreateConsistentSearchBar("🔍 Search fines...", 920);
            Panel searchPanel = searchBarComponents.panel;
            TextBox txtSearchFines = searchBarComponents.textBox;
            Button btnSearchFines = searchBarComponents.button;
            searchPanel.Location = new Point(30, 250);
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
            Panel cardTotalTitles = CreateCatalogStatCard("📚", "0", "Total Titles", ThemeConstants.PrimaryMaroon, new Point(0, 0));
            Panel cardTotalCopies = CreateCatalogStatCard("📖", "0", "Total Copies", Color.White, new Point(250, 0));
            Panel cardAvailable = CreateCatalogStatCard("✓", "0", "Available", Color.FromArgb(76, 175, 80), new Point(500, 0));
            Panel cardBorrowed = CreateCatalogStatCard("📗", "0", "Borrowed", Color.FromArgb(33, 150, 243), new Point(0, 80));
            Panel cardDamaged = CreateCatalogStatCard("⚠", "0", "Damaged", Color.FromArgb(255, 193, 7), new Point(250, 80));
            Panel cardLost = CreateCatalogStatCard("❌", "0", "Lost", Color.FromArgb(244, 67, 54), new Point(500, 80));
            statsPanel.Controls.AddRange(new Control[] { cardTotalTitles, cardTotalCopies, cardAvailable, cardBorrowed, cardDamaged, cardLost });
            var searchBarComponents = CreateConsistentSearchBar("🔍 Search inventory...", 920);
            Panel searchPanel = searchBarComponents.panel;
            TextBox txtSearchInventory = searchBarComponents.textBox;
            Button btnSearchInventory = searchBarComponents.button;
            searchPanel.Location = new Point(30, 330);
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
            pnlMainContent.Controls.AddRange(new Control[] { titleLabel, subtitleLabel, btnExportInventory, statsPanel, searchPanel, dgvInventory });
        }

        private Panel pnlReportsTabs;
        private FlowLayoutPanel flpReportsContent;

        private void ShowReportsView()
        {
            RestoreOriginalControls();
            ShowDashboardControls(false);
            ClearDynamicControls();

            Panel pnlReportsMain = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(250, 252, 255) };
            pnlMainContent.Controls.Add(pnlReportsMain);
            pnlReportsMain.BringToFront();

            // 1. Header Section
            Panel pnlHeader = new Panel { Height = 60, Dock = DockStyle.Top, BackColor = Color.Transparent };

            // Header Buttons

             Button btnExport = new Button 

            { 

                Text = "📥  Export", 

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

                Text = "🖨  Print", 

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

            cmbDate.Items.Add("📅  Last 7 days");

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

            

            tlpKPI.Controls.Add(CreateReportStatCard("Total Collection", "2,450", "📖", Color.White, Color.Black, false), 0, 0);

            tlpKPI.Controls.Add(CreateReportStatCard("Active Members", "142", "👥", Color.White, Color.Black, false), 1, 0);

            tlpKPI.Controls.Add(CreateReportStatCard("Transactions", "38", "↗", Color.White, Color.Black, false), 2, 0);

            tlpKPI.Controls.Add(CreateReportStatCard("Fines Collected", "$1,250", "💲", Color.White, Color.Black, false), 3, 0);

            tlpKPI.Controls.Add(CreateReportStatCard("Overdue", "12", "📅", Color.White, Color.Black, false), 4, 0);

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

            

            tlpKPI.Controls.Add(CreateReportStatCard("Total Members", "524", "👥", Color.White, Color.Black, false), 0, 0);

            tlpKPI.Controls.Add(CreateReportStatCard("New This Month", "32", "➕", Color.White, Color.Green, false), 1, 0);

            tlpKPI.Controls.Add(CreateReportStatCard("Active Users", "412", "⚡", Color.White, Color.Blue, false), 2, 0);

            tlpKPI.Controls.Add(CreateReportStatCard("Suspended", "8", "⛔", Color.White, Color.Red, false), 3, 0);

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

            

            tlpKPI.Controls.Add(CreateReportStatCard("Total Titles", "12,450", "📚", Color.White, Color.Black, false), 0, 0);

            tlpKPI.Controls.Add(CreateReportStatCard("Total Copies", "15,200", "📖", Color.White, Color.Black, false), 1, 0);

            tlpKPI.Controls.Add(CreateReportStatCard("Available", "11,500", "✅", Color.White, Color.Green, false), 2, 0);

            tlpKPI.Controls.Add(CreateReportStatCard("Lost/Damaged", "45", "❌", Color.White, Color.Red, false), 3, 0);

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

            

            tlpKPI.Controls.Add(CreateReportStatCard("Total Collected", "$12,450", "💰", Color.White, Color.Green, false), 0, 0);

            tlpKPI.Controls.Add(CreateReportStatCard("Pending Fines", "$2,100", "⚠️", Color.White, Color.Orange, false), 1, 0);

            tlpKPI.Controls.Add(CreateReportStatCard("Waived", "$540", "👋", Color.White, Color.Gray, false), 2, 0);

            tlpKPI.Controls.Add(CreateReportStatCard("Overdue Cases", "45", "⚖️", Color.White, Color.Red, false), 3, 0);

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

                Text = "🔍 Search",

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

                Text = "All Categories  ▼",

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

            Label lblSearchIcon = new Label { Text = "🔍", Font = new Font("Segoe UI", 12F), Location = new Point(10, 15), AutoSize = true, ForeColor = Color.Gray, BackColor = Color.Transparent };

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

                Text = "🔍",

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



        private void ShowSettingsView()
        {
            RestoreOriginalControls();
            ShowDashboardControls(false);

            if (pnlSettingsView == null || pnlSettingsView.IsDisposed)
            {
                SetupSettingsView();
            }

            if (!pnlMainContent.Controls.Contains(pnlSettingsView))
            {
                pnlMainContent.Controls.Add(pnlSettingsView);
            }

            pnlSettingsView.Visible = true;
            pnlSettingsView.BringToFront();
            pnlSettingsView.Dock = DockStyle.Fill;
        }



        private void SetupSettingsView()

        {

            if (pnlSettingsView != null && !pnlSettingsView.IsDisposed) return;

            pnlSettingsView = new Panel

            {

                Dock = DockStyle.Fill,

                BackColor = ThemeConstants.BackgroundLight,

                Visible = false

            };

            pnlSettingsSubNav = new Panel

            {

                Dock = DockStyle.Top,

                Height = 60,

                BackColor = Color.White,

                Padding = new Padding(30, 0, 30, 0)

            };

            btnSettingGeneral = CreateSettingsSubNavButton("General", true);

            btnSettingNotifications = CreateSettingsSubNavButton("Notifications", false);

            btnSettingBorrowing = CreateSettingsSubNavButton("Borrowing", false);

            btnSettingFines = CreateSettingsSubNavButton("Fines", false);

            int x = 0;

            int gap = 10;

            btnSettingGeneral.Location = new Point(x, 10);

            x += btnSettingGeneral.Width + gap;

            btnSettingNotifications.Location = new Point(x, 10);

            x += btnSettingNotifications.Width + gap;

            btnSettingBorrowing.Location = new Point(x, 10);

            x += btnSettingBorrowing.Width + gap;

            btnSettingFines.Location = new Point(x, 10);

            pnlSettingsSubNav.Controls.AddRange(new Control[] { 

                btnSettingGeneral, btnSettingNotifications, btnSettingBorrowing, btnSettingFines 

            });

            pnlSettingsContent = new Panel

            {

                Dock = DockStyle.Fill,

                BackColor = ThemeConstants.BackgroundLight,

                Padding = new Padding(30, 20, 30, 30)

            };

            Panel settingsHeaderPanel = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.White, Padding = new Padding(30, 20, 30, 0) };

            Label lblSettingsTitle = new Label

            {

                Text = "Settings",

                Font = new Font("Segoe UI", 24F, FontStyle.Bold),

                Location = new Point(0, 0),

                AutoSize = true,

                ForeColor = Color.FromArgb(40, 40, 40)

            };

            Label lblSettingsSub = new Label

            {

                Text = "Configure library system settings and policies",

                Font = new Font("Segoe UI", 10F),

                Location = new Point(0, 45),

                AutoSize = true,

                ForeColor = Color.Gray

            };

            settingsHeaderPanel.Controls.Add(lblSettingsTitle);

            settingsHeaderPanel.Controls.Add(lblSettingsSub);

            pnlSettingsView.Controls.Add(pnlSettingsContent);

            pnlSettingsView.Controls.Add(pnlSettingsSubNav);

            pnlSettingsView.Controls.Add(settingsHeaderPanel);

            btnSettingGeneral.Click += (s, e) => ShowSettingsGeneral();
            btnSettingNotifications.Click += (s, e) => ShowSettingsNotifications();
            btnSettingBorrowing.Click += (s, e) => ShowSettingsBorrowing();
            btnSettingFines.Click += (s, e) => ShowSettingsFines();

            // Initialize default content so Settings is never blank on first open.
            ShowSettingsGeneral();
        }



        private void ResetSettingsNavButtons()

        {

            foreach (Control c in pnlSettingsSubNav.Controls)

            {

                if (c is Button btn)

                {

                    btn.Font = new Font("Segoe UI", 10F, FontStyle.Regular);

                    btn.ForeColor = Color.Gray;

                    btn.Invalidate();

                }

            }

        }



        private void SetActiveSettingsNavButton(Button btn)

        {

            ResetSettingsNavButtons();

            btn.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

            btn.ForeColor = ThemeConstants.PrimaryMaroon;

        }



        private void ShowSettingsGeneral()

        {

            SetActiveSettingsNavButton(btnSettingGeneral);

            pnlSettingsContent.Controls.Clear();

            // Library Information Section

            Panel sectionBasic = CreateSettingSection("Library Information", "Basic library details and contact information");

            sectionBasic.AutoSize = false;

            Control pnlHeader = sectionBasic.Controls[0];

            // Two-column layout for inputs

            TableLayoutPanel tlpBasic = new TableLayoutPanel 

            { 

                Dock = DockStyle.Top, 

                Height = 180,

                ColumnCount = 2, 

                RowCount = 2,

                Padding = new Padding(20, 10, 20, 20)

            };

            tlpBasic.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            tlpBasic.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            tlpBasic.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            tlpBasic.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            // Create input panels - they already have Dock=Fill from CreateSettingInput

            Panel pnlLibraryName = CreateSettingInput("Library Name", "Central University Library");

            Panel pnlPhone = CreateSettingInput("Phone", "+1 555-123-4567");

            Panel pnlEmail = CreateSettingInput("Email", "library@university.edu");

            Panel pnlAddress = CreateSettingInput("Address", "123 Campus Drive, University City");

            tlpBasic.Controls.Add(pnlLibraryName, 0, 0);

            tlpBasic.Controls.Add(pnlEmail, 1, 0);

            tlpBasic.Controls.Add(pnlPhone, 0, 1);

            tlpBasic.Controls.Add(pnlAddress, 1, 1);

            // Save button panel

            Panel pnlSave = CreateSettingsSaveButton();

            // Add controls to section in correct order (header, table, save button)

            sectionBasic.Controls.Add(pnlSave);

            sectionBasic.Controls.Add(tlpBasic);

            pnlSave.BringToFront();

            tlpBasic.BringToFront();

            pnlHeader.BringToFront();

            // Calculate and set section height

            int sectionHeight = 70 + 180 + 80; // header + table + save button

            sectionBasic.Height = sectionHeight;

            // Set initial width and add to content

            int initialWidth = pnlSettingsContent.Width > 0 ? Math.Max(600, pnlSettingsContent.Width - 60) : 800;

            sectionBasic.Width = initialWidth;

            sectionBasic.Location = new Point(30, 0);

            pnlSettingsContent.Controls.Add(sectionBasic);

            // Resize handler

            pnlSettingsContent.Resize += (s, e) => {

                int w = pnlSettingsContent.Width - 60;

                if(w < 600) w = 600;

                sectionBasic.Width = w;

            };

        }



        private void ShowSettingsNotifications()

        {

            SetActiveSettingsNavButton(btnSettingNotifications);

            pnlSettingsContent.Controls.Clear();

            // Notification Preferences Section

            Panel sectionPrefs = CreateSettingSection("Notification Preferences", "Configure email and system notifications");

            sectionPrefs.AutoSize = false;

            Control pnlHeader = sectionPrefs.Controls[0];

            // Container panel for all preferences

            Panel pnlContent = new Panel { Dock = DockStyle.Top, Height = 350, Padding = new Padding(20, 0, 20, 20) };

            // Add toggle switches (they already have Dock=Top from CreateSettingToggle)

            Panel toggle1 = CreateSettingToggle("Email Notifications", "Enable email notifications for library events", true);

            Panel toggle2 = CreateSettingToggle("Overdue Reminders", "Send reminders for overdue books", true);

            Panel toggle3 = CreateSettingToggle("Reservation Alerts", "Notify when reserved books become available", true);

            Panel toggle4 = CreateSettingToggle("Due Date Reminders", "Remind members before books are due", true);

            // Reminder interval input

            Panel pnlDays = new Panel { Height = 50, Dock = DockStyle.Top, Padding = new Padding(0, 10, 0, 0) };

            pnlDays.Paint += (s, e) => {

                 using(SolidBrush b = new SolidBrush(Color.FromArgb(248, 248, 248)))

                    e.Graphics.FillRectangle(b, 0, 0, pnlDays.Width, pnlDays.Height);

            };

            Label lblDays = new Label { Text = "Send reminder", AutoSize = true, Location = new Point(0, 15), Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(60,60,60) };

            Panel pnlNum = new Panel { Location = new Point(120, 10), Size = new Size(50, 30), BackColor = Color.White };

            TextBox txtDays = new TextBox { Text = "2", Location = new Point(5, 5), Width = 40, Font = new Font("Segoe UI", 9), TextAlign = HorizontalAlignment.Center, BorderStyle = BorderStyle.None };

            pnlNum.Controls.Add(txtDays);

            pnlNum.Paint += (s, e) => {

                 using(Pen p = new Pen(Color.FromArgb(220, 220, 220))) e.Graphics.DrawRectangle(p, 0, 0, 49, 29);

            };

            Label lblDays2 = new Label { Text = "days before due date", AutoSize = true, Location = new Point(180, 15), Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(60,60,60) };

            pnlDays.Controls.AddRange(new Control[] { lblDays, pnlNum, lblDays2 });

            // Handle resize for days panel

            pnlDays.Resize += (s, e) => {

                pnlNum.Left = 120;

                lblDays2.Left = 180;

            };

            // Add controls in reverse order for proper docking (last added = topmost)

            pnlContent.Controls.Add(pnlDays);

            pnlContent.Controls.Add(toggle4);

            pnlContent.Controls.Add(toggle3);

            pnlContent.Controls.Add(toggle2);

            pnlContent.Controls.Add(toggle1);

            // Save button panel

            Panel pnlSave = CreateSettingsSaveButton();

            sectionPrefs.Controls.Add(pnlSave);

            sectionPrefs.Controls.Add(pnlContent);

            pnlSave.BringToFront();

            pnlContent.BringToFront();

            pnlHeader.BringToFront();

            // Calculate section height

            int sectionHeight = 70 + 350 + 80; // header + content + save button

            sectionPrefs.Height = sectionHeight;

            // Set initial width and add to content

            int initialWidth = pnlSettingsContent.Width > 0 ? Math.Max(600, pnlSettingsContent.Width - 60) : 800;

            sectionPrefs.Width = initialWidth;

            sectionPrefs.Location = new Point(30, 0);

            pnlSettingsContent.Controls.Add(sectionPrefs);

            // Resize handler

            pnlSettingsContent.Resize += (s, e) => {

                int w = pnlSettingsContent.Width - 60;

                if(w < 600) w = 600;

                sectionPrefs.Width = w;

            };

        }



        private void ShowSettingsBorrowing()

        {

            SetActiveSettingsNavButton(btnSettingBorrowing);

            pnlSettingsContent.Controls.Clear();

            // Borrowing Policies Section

            Panel sectionBorrowing = CreateSettingSection("Borrowing Policies", "Configure borrowing limits and privileges by member type");

            sectionBorrowing.AutoSize = false;

            Control pnlHeader = sectionBorrowing.Controls[0];

            // Container for borrowing groups

            Panel pnlGroups = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 20) };

            Panel group1 = CreateBorrowingGroup("Student Members", "5", "14", "2", "5", true);

            Panel group2 = CreateBorrowingGroup("Faculty Members", "10", "30", "3", "3", true);

            Panel group3 = CreateBorrowingGroup("Staff Members", "7", "21", "2", "4", true);

            Panel group4 = CreateBorrowingGroup("Guest Members", "2", "7", "1", "10", false);

            group1.Dock = DockStyle.Top;

            group2.Dock = DockStyle.Top;

            group3.Dock = DockStyle.Top;

            group4.Dock = DockStyle.Top;

            pnlGroups.Controls.Add(group4);

            pnlGroups.Controls.Add(group3);

            pnlGroups.Controls.Add(group2);

            pnlGroups.Controls.Add(group1);

            // Save button panel

            Panel pnlSave = CreateSettingsSaveButton();

            sectionBorrowing.Controls.Add(pnlSave);

            sectionBorrowing.Controls.Add(pnlGroups);

            pnlSave.BringToFront();

            pnlGroups.BringToFront();

            pnlHeader.BringToFront();

            // Calculate section height (4 groups * 150px each + spacing)

            int sectionHeight = 70 + (4 * 150) + 100 + 80; // header + groups + spacing + save button

            sectionBorrowing.Height = sectionHeight;

            // Set initial width and add to content

            int initialWidth = pnlSettingsContent.Width > 0 ? Math.Max(600, pnlSettingsContent.Width - 60) : 800;

            sectionBorrowing.Width = initialWidth;

            sectionBorrowing.Location = new Point(30, 0);

            pnlSettingsContent.Controls.Add(sectionBorrowing);

            // Resize handler

            pnlSettingsContent.Resize += (s, e) => {

                int w = pnlSettingsContent.Width - 60;

                if(w < 600) w = 600;

                sectionBorrowing.Width = w;

            };

        }





        private void ShowSettingsFines()

        {

            SetActiveSettingsNavButton(btnSettingFines);

            pnlSettingsContent.Controls.Clear();

            // Section 1: Fine Configuration

            Panel sectionConfig = CreateSettingSection("Fine Configuration", "Configure fine calculation and limits");

            sectionConfig.AutoSize = false;

            Control pnlHeader1 = sectionConfig.Controls[0];

            TableLayoutPanel tlpConfig = new TableLayoutPanel 

            { 

                Dock = DockStyle.Top, 

                Height = 100, 

                ColumnCount = 3, 

                RowCount = 1,

                Padding = new Padding(20, 10, 20, 20)

            };

            tlpConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));

            tlpConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));

            tlpConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));

            tlpConfig.Controls.Add(CreateSettingInput("Maximum Fine Cap ($)", "100"), 0, 0);

            tlpConfig.Controls.Add(CreateSettingInput("Grace Period (Days)", "0"), 1, 0);

            tlpConfig.Controls.Add(CreateSettingInput("Lost Book Multiplier", "2"), 2, 0);

            sectionConfig.Controls.Add(tlpConfig);

            tlpConfig.BringToFront();

            pnlHeader1.BringToFront();

            int configHeight = 70 + 100; // header + table

            sectionConfig.Height = configHeight;

            int initialWidth1 = pnlSettingsContent.Width > 0 ? Math.Max(600, pnlSettingsContent.Width - 60) : 800;

            sectionConfig.Width = initialWidth1;

            sectionConfig.Location = new Point(30, 0);

            pnlSettingsContent.Controls.Add(sectionConfig);

            // Section 2: Fine Rate Summary

            Panel sectionRates = CreateSettingSection("Fine Rate Summary", "Current fine rates per member type");

            sectionRates.AutoSize = false;

            Control pnlHeader2 = sectionRates.Controls[0];

            TableLayoutPanel tlpRates = new TableLayoutPanel

            {

                Dock = DockStyle.Top,

                ColumnCount = 2,

                RowCount = 4,

                Height = 200,

                Padding = new Padding(40, 20, 40, 20)

            };

            tlpRates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            tlpRates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            tlpRates.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));

            tlpRates.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));

            tlpRates.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));

            tlpRates.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));

            string[] types = { "Student:", "Faculty:", "Staff:", "Guest:" };

            string[] rates = { "$5/day", "$3/day", "$4/day", "$10/day" }; 

            for(int i=0; i<4; i++)

            {

                Label lT = new Label 

                { 

                    Text = types[i], 

                    Font = new Font("Segoe UI", 10, FontStyle.Regular), 

                    AutoSize = false,

                    Dock = DockStyle.Fill,

                    ForeColor = Color.FromArgb(60,60,60),

                    TextAlign = ContentAlignment.MiddleLeft

                };

                Label lR = new Label 

                { 

                    Text = rates[i], 

                    Font = new Font("Segoe UI", 10, FontStyle.Bold), 

                    AutoSize = false,

                    Dock = DockStyle.Fill,

                    ForeColor = ThemeConstants.PrimaryMaroon, 

                    TextAlign = ContentAlignment.MiddleRight

                };

                tlpRates.Controls.Add(lT, 0, i);

                tlpRates.Controls.Add(lR, 1, i);

            }

            sectionRates.Controls.Add(tlpRates);

            tlpRates.BringToFront();

            pnlHeader2.BringToFront();

            int ratesHeight = 70 + 200; // header + table

            sectionRates.Height = ratesHeight;

            sectionRates.Width = initialWidth1;

            sectionRates.Location = new Point(30, configHeight + 20);

            pnlSettingsContent.Controls.Add(sectionRates);

            // Single Save button at the bottom - create a container panel

            Panel pnlSaveContainer = new Panel 

            { 

                Location = new Point(30, configHeight + ratesHeight + 20),

                Width = initialWidth1,

                Height = 80,

                BackColor = Color.Transparent

            };

            Panel pnlSave = CreateSettingsSaveButton();

            pnlSave.Dock = DockStyle.None;

            pnlSave.Location = new Point(pnlSaveContainer.Width - 190, 20);

            pnlSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            pnlSaveContainer.Controls.Add(pnlSave);

            pnlSaveContainer.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            pnlSettingsContent.Controls.Add(pnlSaveContainer);

            pnlSaveContainer.Resize += (s, e) => pnlSave.Left = pnlSaveContainer.Width - 190;

            // Resize handler

            pnlSettingsContent.Resize += (s, e) => {

                int w = pnlSettingsContent.Width - 60;

                if(w < 600) w = 600;

                sectionConfig.Width = w;

                sectionRates.Width = w;

                pnlSaveContainer.Width = w;

                sectionRates.Location = new Point(30, configHeight + 20);

                pnlSaveContainer.Location = new Point(30, configHeight + ratesHeight + 20);

            };

        }

        private void ShowUserManagementView()
        {
            RestoreOriginalControls();
            ShowDashboardControls(false);
            
            if (pnlUserManagementView == null || pnlUserManagementView.IsDisposed)
            {
                SetupUserManagementView();
            }
            
            if (!pnlMainContent.Controls.Contains(pnlUserManagementView))
            {
                pnlMainContent.Controls.Add(pnlUserManagementView);
            }
            
            pnlUserManagementView.Visible = true;
            pnlUserManagementView.BringToFront();
            pnlUserManagementView.Dock = DockStyle.Fill;
            
            // Show Librarians tab by default
            SwitchUserManagementTab("Librarians", btnUserTabLibrarians);
        }

        private void SetupUserManagementView()
        {
            if (pnlUserManagementView != null && !pnlUserManagementView.IsDisposed) return;
            
            // Main panel
            pnlUserManagementView = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeConstants.BackgroundLight,
                Visible = false
            };
            
            // Header panel
            Panel headerPanel = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 100, 
                BackColor = Color.White,
                Padding = new Padding(30, 20, 30, 10)
            };
            
            Label lblTitle = new Label
            {
                Text = "User Management",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = ThemeConstants.PrimaryMaroon,
                Location = new Point(30, 20),
                AutoSize = true
            };
            
            Label lblSubtitle = new Label
            {
                Text = "Manage librarian and staff accounts.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Gray,
                Location = new Point(30, 60),
                AutoSize = true
            };
            
            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(lblSubtitle);
            
            // Search and Add User panel
            Panel pnlSearchAdd = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.White,
                Padding = new Padding(30, 15, 30, 15)
            };
            
            // Search textbox
            txtSearchUsers = new TextBox
            {
                Location = new Point(30, 20),
                Size = new Size(700, 34),
                Font = new Font("Segoe UI", 10F),
                Text = "🔍 Search users..."
            };
            
            txtSearchUsers.Enter += (s, e) => {
                if (txtSearchUsers.Text == "🔍 Search users...")
                {
                    txtSearchUsers.Text = "";
                    txtSearchUsers.ForeColor = Color.Black;
                }
            };
            
            txtSearchUsers.Leave += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtSearchUsers.Text))
                {
                    txtSearchUsers.Text = "🔍 Search users...";
                    txtSearchUsers.ForeColor = Color.Gray;
                }
            };
            
            txtSearchUsers.ForeColor = Color.Gray;
            txtSearchUsers.TextChanged += TxtSearchUsers_TextChanged;
            
            // Search button
            btnSearchUsers = new Button
            {
                Text = "🔍 Search",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ThemeConstants.PrimaryMaroon,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(100, 34),
                Location = new Point(740, 20)
            };
            btnSearchUsers.Click += BtnSearchUsers_Click;
            
            // Add User button
            btnAddUser = new Button
            {
                Text = "+ Add User",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ThemeConstants.PrimaryMaroon, // Deep maroon color matching theme
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(140, 40),
                Location = new Point(pnlSearchAdd.Width - 170, 15),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            
            // Create rounded button effect
            btnAddUser.Paint += (s, e) =>
            {
                Button btn = s as Button;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                
                // Draw rounded background
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 6))
                {
                    e.Graphics.FillPath(new SolidBrush(btn.BackColor), path);
                }
                
                // Draw text
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                e.Graphics.DrawString(btn.Text, btn.Font, new SolidBrush(btn.ForeColor), 
                    new RectangleF(0, 0, btn.Width, btn.Height), sf);
            };
            
            btnAddUser.MouseEnter += (s, e) => 
            {
                btnAddUser.BackColor = ThemeConstants.AccentMaroonHover;
                btnAddUser.Invalidate();
            };
            btnAddUser.MouseLeave += (s, e) => 
            {
                btnAddUser.BackColor = ThemeConstants.PrimaryMaroon;
                btnAddUser.Invalidate();
            };
            btnAddUser.Click += (s, e) => ShowAddUserDialog();
            
            pnlSearchAdd.Controls.Add(txtSearchUsers);
            pnlSearchAdd.Controls.Add(btnSearchUsers);
            pnlSearchAdd.Controls.Add(btnAddUser);
            
            // Tabs panel
            pnlUserManagementTabs = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(30, 10, 30, 0)
            };
            
            Panel tabBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Color.FromArgb(230, 230, 230)
            };
            pnlUserManagementTabs.Controls.Add(tabBorder);
            
            btnUserTabLibrarians = CreateUserManagementTabButton("Librarians/Admins", true);
            btnUserTabStaff = CreateUserManagementTabButton("Staff Accounts", false);
            
            btnUserTabLibrarians.Location = new Point(0, 12);
            btnUserTabStaff.Location = new Point(btnUserTabLibrarians.Width + 15, 12);
            
            btnUserTabLibrarians.Click += (s, e) => SwitchUserManagementTab("Librarians", btnUserTabLibrarians);
            btnUserTabStaff.Click += (s, e) => SwitchUserManagementTab("Staff", btnUserTabStaff);
            
            pnlUserManagementTabs.Controls.AddRange(new Control[] { btnUserTabLibrarians, btnUserTabStaff });
            
            // Content panel - Reduced horizontal padding for wider table
            pnlUserManagementContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeConstants.BackgroundLight,
                Padding = new Padding(15, 20, 15, 30)
            };
            
            // Create table panel - Reduced padding for wider table
            Panel pnlTableContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(15, 15, 15, 15)
            };
            
            // Table title
            Label lblTableTitle = new Label
            {
                Text = "Librarians & Administrators",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(15, 15),
                AutoSize = true
            };
            
            Label lblTableDescription = new Label
            {
                Text = "Users with full system access and administrative privileges.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray,
                Location = new Point(15, 45),
                AutoSize = true
            };
            
            // DataGridView - Added more padding below title
            dgvUserManagement = new DataGridView
            {
                Location = new Point(10, 80), // Increased from 80 to 100 for more padding below title
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ColumnHeadersHeight = 300, // Increased height for more top margin
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                Font = new Font("Segoe UI", 9F),
                GridColor = Color.FromArgb(240, 240, 240),
                Cursor = Cursors.Hand,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Padding = new Padding(12, 8, 12, 8), // Reduced padding for tighter spacing
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    SelectionBackColor = Color.FromArgb(59, 130, 246),
                    SelectionForeColor = Color.White
                }
            };
            
            // Wire up cell click event
            dgvUserManagement.CellClick += DgvUserManagement_CellClick;
            
            // Add columns with proper widths (increased for wider panel)
            dgvUserManagement.Columns.Add("Name", "Name");
            dgvUserManagement.Columns["Name"].Width = 150;
            dgvUserManagement.Columns["Name"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            
            dgvUserManagement.Columns.Add("Email", "Email");
            dgvUserManagement.Columns["Email"].Width = 150;
            dgvUserManagement.Columns["Email"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            
            dgvUserManagement.Columns.Add("Department", "Department");
            dgvUserManagement.Columns["Department"].Width = 150;
            dgvUserManagement.Columns["Department"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            
            dgvUserManagement.Columns.Add("Role", "Role");
            dgvUserManagement.Columns["Role"].Width = 110;
            dgvUserManagement.Columns["Role"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            
            dgvUserManagement.Columns.Add("Status", "Status");
            dgvUserManagement.Columns["Status"].Width = 90;
            dgvUserManagement.Columns["Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            
            dgvUserManagement.Columns.Add("LastLogin", "Last Login");
            dgvUserManagement.Columns["LastLogin"].Width = 90;
            dgvUserManagement.Columns["LastLogin"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            
            dgvUserManagement.Columns.Add("Actions", "Actions");
            dgvUserManagement.Columns["Actions"].Width = 250;
            dgvUserManagement.Columns["Actions"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvUserManagement.Columns["Actions"].DefaultCellStyle.Padding = new Padding(10, 5, 10, 5); // Adjusted emoji padding
            
            // Style column headers - decreased bottom padding
            foreach (DataGridViewColumn col in dgvUserManagement.Columns)
            {
                col.HeaderCell.Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                col.HeaderCell.Style.BackColor = Color.FromArgb(59, 130, 246);
                col.HeaderCell.Style.ForeColor = Color.White;
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                col.HeaderCell.Style.Padding = new Padding(10, 25, 10, 6); // Decreased bottom padding from 12 to 6
            }
            
            // Wire up cell painting for Actions column
            dgvUserManagement.CellPainting += DgvUserManagement_CellPainting;
            dgvUserManagement.CellFormatting += DgvUserManagement_CellFormatting;
            
            // Load users data
            LoadUsersData();
            
            // Wire up search textbox
            if (txtSearchUsers != null)
            {
                txtSearchUsers.TextChanged += (s, e) => LoadUsersData();
            }
            
            pnlTableContainer.Controls.Add(lblTableTitle);
            pnlTableContainer.Controls.Add(lblTableDescription);
            pnlTableContainer.Controls.Add(dgvUserManagement);
            
            pnlUserManagementContent.Controls.Add(pnlTableContainer);
            
            // Add panels to main view
            pnlUserManagementView.Controls.Add(headerPanel);
            pnlUserManagementView.Controls.Add(pnlSearchAdd);
            pnlUserManagementView.Controls.Add(pnlUserManagementTabs);
            pnlUserManagementView.Controls.Add(pnlUserManagementContent);
        }

        private Button CreateUserManagementTabButton(string text, bool isActive)
        {
            Button btn = new Button
            {
                Text = text,
                Size = new Size(180, 40),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F, isActive ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isActive ? ThemeConstants.PrimaryMaroon : Color.Gray,
                Cursor = Cursors.Hand,
                Tag = isActive
            };
            
            btn.Paint += (s, e) =>
            {
                if ((bool)btn.Tag)
                {
                    using (Pen pen = new Pen(ThemeConstants.PrimaryMaroon, 2))
                    {
                        e.Graphics.DrawLine(pen, 0, btn.Height - 2, btn.Width, btn.Height - 2);
                    }
                }
            };
            
            return btn;
        }

        private void SwitchUserManagementTab(string tabName, Button activeBtn)
        {
            // Reset all tabs
            btnUserTabLibrarians.Tag = false;
            btnUserTabLibrarians.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            btnUserTabLibrarians.ForeColor = Color.Gray;
            btnUserTabLibrarians.Invalidate();
            
            btnUserTabStaff.Tag = false;
            btnUserTabStaff.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            btnUserTabStaff.ForeColor = Color.Gray;
            btnUserTabStaff.Invalidate();
            
            // Set active tab
            activeBtn.Tag = true;
            activeBtn.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            activeBtn.ForeColor = ThemeConstants.PrimaryMaroon;
            activeBtn.Invalidate();
            
            // Update table title and data
            Label lblTableTitle = pnlUserManagementContent.Controls[0].Controls[0] as Label;
            Label lblTableDescription = pnlUserManagementContent.Controls[0].Controls[1] as Label;
            
            if (tabName == "Librarians")
            {
                if (lblTableTitle != null)
                {
                    lblTableTitle.Text = "Librarians & Administrators";
                    lblTableDescription.Text = "Users with full system access and administrative privileges.";
                }
            }
            else if (tabName == "Staff")
            {
                if (lblTableTitle != null)
                {
                    lblTableTitle.Text = "Staff Accounts";
                    lblTableDescription.Text = "Library staff members with limited administrative access.";
                }
            }
            
            // Update current role
            _currentUserManagementRole = tabName == "Librarians" ? UserRole.Administrator : UserRole.Staff;
            
            // Refresh data
            LoadUsersData();
        }

        private void LoadUsersData()
        {
            try
            {
            dgvUserManagement.Rows.Clear();
                
                // Get users from database
                var users = _userManagementService.GetUsersByRole(_currentUserManagementRole);
                
                // Apply search filter if any
                string searchText = txtSearchUsers?.Text ?? "";
                if (!string.IsNullOrWhiteSpace(searchText) && searchText != "🔍 Search users...")
                {
                    users = users.Where(u => 
                        u.FullName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) ||
                        u.Email.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }
                
                // Add users to grid
                foreach (var user in users)
                {
                    string roleDisplay = user.Role == UserRole.Administrator ? "Librarian/Admin" : "Staff";
                    
                    // Determine status based on IsActive and LastLogin
                    // Status is Inactive if:
                    // 1. IsActive is false in database, OR
                    // 2. LastLogin is null/never (account has never been used)
                    bool hasNeverLoggedIn = string.IsNullOrWhiteSpace(user.LastLogin) || 
                                           user.LastLogin.Equals("Never", StringComparison.OrdinalIgnoreCase);
                    
                    // Status from database IsActive field
                    bool isActiveInDb = user.IsActive;
                    
                    // Final status: Active only if both IsActive is true AND user has logged in at least once
                    string statusDisplay = (isActiveInDb && !hasNeverLoggedIn) ? "Active" : "Inactive";
                    
                    string lastLoginDisplay = string.IsNullOrWhiteSpace(user.LastLogin) ? "Never" : user.LastLogin;
                    string departmentDisplay = string.IsNullOrWhiteSpace(user.Department) ? "" : user.Department;
                    
                    int rowIndex = dgvUserManagement.Rows.Add(
                        user.FullName,
                        user.Email,
                        departmentDisplay,
                        roleDisplay,
                        statusDisplay,
                        lastLoginDisplay,
                "" // Actions column - will be painted
            );
                    
                    // Store UserId in row tag for later reference
                    dgvUserManagement.Rows[rowIndex].Tag = user.UserId;
                }
            
            // Style rows
            foreach (DataGridViewRow row in dgvUserManagement.Rows)
            {
                row.Height = 55; // Reduced row height for tighter spacing
                
                // Alternate row colors for better readability
                if (row.Index % 2 == 0)
                {
                    row.DefaultCellStyle.BackColor = Color.White;
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
                }
                
                // Style Role column (pink)
                if (row.Cells["Role"].Value != null)
                {
                    row.Cells["Role"].Style.ForeColor = Color.FromArgb(219, 39, 119); // Pink
                    row.Cells["Role"].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }
                
                // Style Status column based on status value
                if (row.Cells["Status"].Value != null)
                {
                    string statusValue = row.Cells["Status"].Value.ToString();
                    if (statusValue == "Active")
                    {
                        row.Cells["Status"].Style.ForeColor = Color.FromArgb(34, 197, 94); // Green
                        row.Cells["Status"].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    }
                    else if (statusValue == "Inactive")
                    {
                        row.Cells["Status"].Style.ForeColor = Color.FromArgb(220, 53, 69); // Red
                        row.Cells["Status"].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    }
                }
            }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvUserManagement_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return; // Header
            
            if (dgvUserManagement.Columns[e.ColumnIndex].Name == "Actions")
            {
                e.PaintBackground(e.CellBounds, true);
                
                // Draw emoji icons with optimized size and spacing
                int iconSize = 18; // Increased size for better visibility
                int spacing = 16; // Balanced spacing between icons
                int totalWidth = (iconSize + spacing) * 3 + iconSize; // 4 icons
                int startX = e.CellBounds.X + (e.CellBounds.Width - totalWidth) / 2; // Center the icons
                int centerY = e.CellBounds.Y + (e.CellBounds.Height / 2);
                
                // Get row state for styling
                bool isSelected = e.RowIndex >= 0 && dgvUserManagement.Rows[e.RowIndex].Selected;
                
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                
                // Enhanced emoji icons with better font size
                Font emojiFont = new Font("Segoe UI Emoji", 16F, FontStyle.Regular);
                
                // Create rectangles for each icon with proper sizing
                Rectangle editRect = new Rectangle(startX, centerY - iconSize/2, iconSize, iconSize);
                Rectangle resetRect = new Rectangle(startX + iconSize + spacing, centerY - iconSize/2, iconSize, iconSize);
                Rectangle roleRect = new Rectangle(startX + (iconSize + spacing) * 2, centerY - iconSize/2, iconSize, iconSize);
                Rectangle deleteRect = new Rectangle(startX + (iconSize + spacing) * 3, centerY - iconSize/2, iconSize, iconSize);
                
                // Edit icon (✏️) - Pencil emoji
                TextRenderer.DrawText(e.Graphics, "✏️", emojiFont, editRect, 
                    isSelected ? Color.White : Color.FromArgb(59, 130, 246),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                
                // Reset Password icon (🔑) - Key emoji
                TextRenderer.DrawText(e.Graphics, "🔑", emojiFont, resetRect, 
                    isSelected ? Color.White : Color.FromArgb(59, 130, 246),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                
                // Assign Role icon (🛡️) - Shield emoji
                TextRenderer.DrawText(e.Graphics, "🛡️", emojiFont, roleRect, 
                    isSelected ? Color.White : Color.FromArgb(59, 130, 246),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                
                // Delete icon (🗑️) - Trash can emoji
                TextRenderer.DrawText(e.Graphics, "🗑️", emojiFont, deleteRect, 
                    isSelected ? Color.White : Color.FromArgb(220, 53, 69),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                
                emojiFont.Dispose();
                e.Handled = true;
            }
        }
        

        private void DgvUserManagement_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex >= 0 && e.RowIndex >= 0)
            {
                DataGridView dgv = sender as DataGridView;
                if (dgv != null && dgv.Columns[e.ColumnIndex].Name == "Actions")
                {
                    // Actions column styling - adjusted emoji padding
                    e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    e.CellStyle.Padding = new Padding(10, 10, 10, 10);
                }
            }
        }

        private void DgvUserManagement_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.ColumnIndex != dgvUserManagement.Columns["Actions"].Index) return;
            
            DataGridViewRow row = dgvUserManagement.Rows[e.RowIndex];
            int userId = row.Tag != null ? (int)row.Tag : 0;
            string name = row.Cells["Name"].Value?.ToString() ?? "";
            string email = row.Cells["Email"].Value?.ToString() ?? "";
            string department = row.Cells["Department"].Value?.ToString() ?? "";
            string role = row.Cells["Role"].Value?.ToString() ?? "";
            
            // Get click position relative to the Actions cell
            Rectangle cellRect = dgvUserManagement.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            Point clickPoint = dgvUserManagement.PointToClient(Control.MousePosition);
            int relativeX = clickPoint.X - cellRect.X;
            
            // Icon positions match the painting positions (centered) - updated to match emoji size
            int iconSize = 18;
            int spacing = 16;
            int totalWidth = (iconSize + spacing) * 3 + iconSize; // 4 icons
            int startX = (cellRect.Width - totalWidth) / 2; // Center the icons
            
            // Calculate which icon was clicked based on actual icon positions
            if (relativeX >= startX && relativeX < startX + iconSize)
            {
                // Edit User (first icon - ✏️)
                ShowEditUserDialog(userId, name, email, "", department);
            }
            else if (relativeX >= startX + iconSize + spacing && relativeX < startX + (iconSize + spacing) * 2)
            {
                // Reset Password (second icon - 🔑)
                ShowResetPasswordDialog(userId, email);
            }
            else if (relativeX >= startX + (iconSize + spacing) * 2 && relativeX < startX + (iconSize + spacing) * 3)
            {
                // Assign Role (third icon - 🛡️)
                ShowAssignRoleDialog(userId, name, role);
            }
            else if (relativeX >= startX + (iconSize + spacing) * 3 && relativeX < startX + (iconSize + spacing) * 4)
            {
                // Deactivate User (fourth icon)
                ShowDeactivateUserDialog(userId, name);
            }
        }

        private void ShowEditUserDialog(int userId, string name, string email, string phone, string department)
        {
            try
            {
                // Get user info from database
                var user = _userManagementService.GetUserById(userId);
                if (user == null)
                {
                    MessageBox.Show("User not found.", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                using (EditUserDialog dialog = new EditUserDialog(user.FullName, user.Email, user.Phone ?? "", user.Department ?? ""))
            {
                dialog.Owner = this;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                        // Update user in database
                        string[] nameParts = dialog.FullName.Split(new[] { ' ' }, 2);
                        string firstName = nameParts.Length > 0 ? nameParts[0] : "";
                        string lastName = nameParts.Length > 1 ? nameParts[1] : "";
                        
                        if (string.IsNullOrWhiteSpace(firstName))
                        {
                            MessageBox.Show("First name is required.", "Validation Error", 
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        
                        try
                        {
                            bool success = _userManagementService.UpdateUser(userId, dialog.Email, firstName, lastName, dialog.Department, dialog.PhoneNumber);
                            
                            if (success)
                            {
                                MessageBox.Show($"User '{dialog.FullName}' has been updated successfully.", "Success", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                                LoadUsersData();
                            }
                            else
                            {
                                MessageBox.Show("Failed to update user. Please try again.", "Error", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        catch (InvalidOperationException ex)
                        {
                            // Handle duplicate email error with user-friendly message
                            MessageBox.Show(ex.Message, "Email Already Exists", 
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating user: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowResetPasswordDialog(int userId, string email)
        {
            try
            {
                using (UserResetPasswordDialog dialog = new UserResetPasswordDialog(email))
                {
                    dialog.Owner = this;
                    if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dialog.NewPassword))
                    {
                        // Validate password before attempting to update
                        string newPassword = dialog.NewPassword.Trim();
                        
                        if (string.IsNullOrWhiteSpace(newPassword))
                        {
                            MessageBox.Show("Password cannot be empty.", "Validation Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        
                        if (newPassword.Length < 6)
                        {
                            MessageBox.Show("Password must be at least 6 characters long.", "Validation Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        
                        // Update password in database
                        bool success = _userManagementService.UpdateUserPassword(email, newPassword);
                        
                        if (success)
                        {
                            MessageBox.Show($"Password has been reset successfully for {email}.", "Success", 
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadUsersData();
                        }
                        else
                        {
                            MessageBox.Show($"Failed to reset password for {email}. The user may not exist.", "Error", 
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while resetting password: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowAssignRoleDialog(int userId, string userName, string currentRole)
        {
            try
            {
                // Map "Librarian/Admin" to "Admin" for the dialog
                string roleForDialog = currentRole == "Librarian/Admin" ? "Admin" : currentRole;
                
                using (AssignRoleDialog dialog = new AssignRoleDialog(userName, roleForDialog))
                {
                    dialog.Owner = this;
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        // Convert role string to UserRole enum
                        UserRole newRole = UserRole.Staff;
                        if (dialog.SelectedRole == "Admin")
                            newRole = UserRole.Administrator;
                        else if (dialog.SelectedRole == "Staff")
                            newRole = UserRole.Staff;
                        else
                        {
                            MessageBox.Show("Invalid role selected.", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        
                        bool success = _userManagementService.UpdateUserRole(userId, newRole);
                        
                        if (success)
                        {
                            MessageBox.Show($"Role '{dialog.SelectedRole}' has been assigned to {userName}.", "Success", 
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadUsersData();
                        }
                        else
                        {
                            MessageBox.Show("Failed to update role. Please try again.", "Error", 
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error assigning role: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowDeactivateUserDialog(int userId, string userName)
        {
            try
        {
            using (DeactivateUserDialog dialog = new DeactivateUserDialog(userName))
            {
                dialog.Owner = this;
                if (dialog.ShowDialog() == DialogResult.OK && dialog.DeactivateUser)
                    {
                        bool success = _userManagementService.DeactivateUser(userId);
                        
                        if (success)
                {
                    MessageBox.Show($"User '{userName}' has been deleted successfully.", "Success", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadUsersData();
                        }
                        else
                        {
                            MessageBox.Show("Failed to delete user. Please try again.", "Error", 
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deactivating user: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowAddUserDialog()
        {
            try
        {
            using (AddUserDialog dialog = new AddUserDialog())
            {
                dialog.Owner = this;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                        // Convert role string to UserRole enum
                        UserRole role = UserRole.Staff;
                        if (dialog.Role == "Librarian/Admin")
                            role = UserRole.Administrator;
                        else if (dialog.Role == "Staff")
                            role = UserRole.Staff;
                        else if (dialog.Role == "Member")
                            role = UserRole.Member;
                        
                        // Split full name
                        string[] nameParts = dialog.FullName.Split(new[] { ' ' }, 2);
                        string firstName = nameParts.Length > 0 ? nameParts[0] : "";
                        string lastName = nameParts.Length > 1 ? nameParts[1] : "";
                        
                        if (string.IsNullOrWhiteSpace(firstName))
                        {
                            MessageBox.Show("First name is required.", "Validation Error", 
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        
                        // Generate default password (in production, send via email)
                        string defaultPassword = GenerateTemporaryPassword();
                        
                        // Check if user already exists
                        if (_userManagementService.UserExists(dialog.Email))
                        {
                            MessageBox.Show("A user with this email already exists.", "Error", 
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        
                        bool success = _userManagementService.CreateUser(dialog.Email, defaultPassword, firstName, lastName, role, dialog.Department, dialog.PhoneNumber);
                        
                        if (success)
                        {
                            MessageBox.Show($"User '{dialog.FullName}' has been added successfully.\n\nDefault password: {defaultPassword}\n\nPlease inform the user to change it on first login.", "Success", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadUsersData();
                        }
                        else
                        {
                            // Check if it's a duplicate user error
                            if (_userManagementService.UserExists(dialog.Email))
                            {
                                MessageBox.Show($"A user with email '{dialog.Email}' already exists. Please use a different email address.", "Duplicate User", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            else
                            {
                                MessageBox.Show("Failed to create user. Please try again.", "Error", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding user: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GenerateTemporaryPassword()
        {
            // Generate a random 8-character password
            const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }





        // Helper Methods for Settings UI

        private Panel CreateSettingSection(string title, string subtitle)

        {

            Panel pnl = new Panel { 

                BackColor = Color.White, 

                Margin = new Padding(0, 0, 0, 20),

                AutoSize = true

            };

            pnl.Paint += (s, e) => {

                using(Pen p = new Pen(Color.FromArgb(230,230,230)))

                   e.Graphics.DrawRectangle(p, 0, 0, pnl.Width-1, pnl.Height-1);

            };



            // Header Panel used for spacing/title

            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.Transparent };

            

            // Icon logic

            string icon = ""; 

            if(title.Contains("Library")) icon = "🏛";

            if(title.Contains("Notification")) icon = "🔔";

            if(title.Contains("Borrowing")) icon = "📖";

            if(title.Contains("Fine")) icon = "💲";

            if(!string.IsNullOrEmpty(icon)) title = icon + " " + title;



            Label lblT = new Label { Text = title, Font = new Font("Georgia", 14, FontStyle.Bold), ForeColor = Color.FromArgb(40,40,40), Location = new Point(15, 15), AutoSize = true };

            Label lblS = new Label { Text = subtitle, Font = new Font("Segoe UI", 9), ForeColor = Color.Gray, Location = new Point(17, 45), AutoSize = true };

            

            pnlHeader.Controls.Add(lblT);

            pnlHeader.Controls.Add(lblS);

            

            pnl.Controls.Add(pnlHeader); 

            

            return pnl;

        }



        private Panel CreateSettingInput(string label, string value, int width = 0)

        {

            // Fixed height container for consistent rows

            Panel pnl = new Panel { Height = 80, Dock = DockStyle.Fill, Padding = new Padding(0, 0, 20, 10) }; 



            Label lbl = new Label { Text = label, Font = new Font("Segoe UI", 9, FontStyle.Regular), AutoSize = true, ForeColor = Color.FromArgb(60,60,60), Location = new Point(0, 0) };

            

            // Custom Input Box

            Panel pnlInput = new Panel { Height = 40, BackColor = Color.FromArgb(252, 252, 252), Location = new Point(0, 25), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            

            if (width > 0) 

            {

                pnl.Width = width;

                pnlInput.Width = width - 20;

            }

            else 

            {

                pnlInput.Width = 300; // Initial default

            }



            TextBox txt = new TextBox 

            { 

                Text = value, 

                Font = new Font("Segoe UI", 10), 

                BorderStyle = BorderStyle.None, 

                BackColor = Color.FromArgb(252, 252, 252),

                ForeColor = Color.FromArgb(40,40,40),

                Location = new Point(10, 10),

                Width = pnlInput.Width - 20,

                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right

            };



            pnlInput.Controls.Add(txt);

            pnl.Controls.Add(lbl);

            pnl.Controls.Add(pnlInput);



            // Important: Handle resize manually for the inner input panel to match parent width

            pnl.Resize += (s, e) => {

                 pnlInput.Width = Math.Max(100, pnl.Width - 10); // Use almost full width

            };



            // Custom Paint for Input Border

            pnlInput.Paint += (s, e) => {

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                bool isFocused = txt.Focused;

                Color borderColor = isFocused ? ThemeConstants.PrimaryMaroon : Color.FromArgb(220, 220, 220);

                using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlInput.Width-1, pnlInput.Height-1), 6))

                using(Pen pen = new Pen(borderColor, isFocused ? 1.5f : 1))

                {

                    e.Graphics.DrawPath(pen, path);

                }

            };

            

            txt.Enter += (s, e) => { pnlInput.Invalidate(); };

            txt.Leave += (s, e) => { pnlInput.Invalidate(); };



            return pnl;

        }



        private Panel CreateSettingToggle(string title, string sub, bool check)

        {

             Panel pnl = new Panel { Height = 60, Dock = DockStyle.Top, Margin = new Padding(0,0,0,10) };

             Label lblT = new Label { Text = title, Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(0, 10), AutoSize = true, ForeColor = Color.FromArgb(40,40,40) };

             Label lblS = new Label { Text = sub, Font = new Font("Segoe UI", 9), ForeColor = Color.Gray, Location = new Point(0, 30), AutoSize = true };

             

             Panel pnlToggle = new Panel { Size = new Size(46, 24), Location = new Point(pnl.Width - 60, 15), Anchor = AnchorStyles.Top | AnchorStyles.Right, Cursor = Cursors.Hand, Tag = check };

             pnlToggle.Paint += (s, e) => {

                 bool isOn = (bool)pnlToggle.Tag;

                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                 Color col = isOn ? ThemeConstants.PrimaryMaroon : Color.FromArgb(200, 200, 200);

                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(0,0,45,23), 12))

                 using(SolidBrush b = new SolidBrush(col)) e.Graphics.FillPath(b, path);

                 

                 int circleX = isOn ? 24 : 2;

                 e.Graphics.FillEllipse(Brushes.White, circleX, 2, 19, 19);

             };

             pnlToggle.Click += (s, e) => {

                 pnlToggle.Tag = !(bool)pnlToggle.Tag;

                 pnlToggle.Invalidate();

             };



             pnl.Controls.Add(lblT);

             pnl.Controls.Add(lblS);

             pnl.Controls.Add(pnlToggle);



             return pnl;

        }



        private Panel CreateSettingsSaveButton()

        {

             Panel pnlObj = new Panel { Height = 80, Dock = DockStyle.Top, Padding = new Padding(0, 20, 0, 0) };

             Button btn = new Button 

             { 

                 Text = "💾  Save Changes", 

                 Size = new Size(160, 45), 

                 BackColor = ThemeConstants.PrimaryMaroon, 

                 ForeColor = Color.White, 

                 FlatStyle = FlatStyle.Flat,

                 Font = new Font("Segoe UI", 10, FontStyle.Bold),

                 Anchor = AnchorStyles.Top | AnchorStyles.Right,

                 Cursor = Cursors.Hand,

                 Location = new Point(pnlObj.Width - 180, 20)

             };

             btn.FlatAppearance.BorderSize = 0;

             pnlObj.Resize += (s, e) => btn.Left = pnlObj.Width - 180;

             

             btn.Paint += (s, e) => {

                  e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                  using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(0,0,btn.Width,btn.Height), 8))

                  using(SolidBrush b = new SolidBrush(ThemeConstants.PrimaryMaroon)) e.Graphics.FillPath(b, path);

                  TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0,0,btn.Width,btn.Height), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

             };

             btn.Click += (s, e) => MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);



             pnlObj.Controls.Add(btn);

             return pnlObj;

        }



        private Panel CreateBorrowingGroup(string title, string maxBooks, string days, string renewals, string fine, bool canReserve)

        {

            Panel pnl = CreateSettingSection(title, "Configure privileges for this group");

            // Override height and structure

             pnl.Controls.Clear();

             

             Label lblT = new Label { Text = "👤  " + title, Font = new Font("Georgia", 11, FontStyle.Bold), ForeColor = Color.FromArgb(40,40,40), Location = new Point(15, 15), AutoSize = true };

             pnl.Controls.Add(lblT);

             

             Panel line = new Panel { Height = 1, BackColor = Color.FromArgb(240,240,240), Dock = DockStyle.Top };

             pnl.Controls.Add(line);

             

             // Top Spacer

             pnl.Controls.Add(new Panel { Height = 50, Dock = DockStyle.Top }); 



             TableLayoutPanel tlp = new TableLayoutPanel 

             { 

                 Dock = DockStyle.Top,

                 Height = 90,

                 ColumnCount = 5, 

                 RowCount = 1,

                 Padding = new Padding(20, 10, 20, 10)

             };

            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            

            // Use width=120 to make them compact inputs

            tlp.Controls.Add(CreateSettingInput("Max Books", maxBooks, 120), 0, 0);

            tlp.Controls.Add(CreateSettingInput("Borrowing Days", days, 120), 1, 0);

            tlp.Controls.Add(CreateSettingInput("Renewal Limit", renewals, 120), 2, 0);

            tlp.Controls.Add(CreateSettingInput("Fine/Day ($)", fine, 120), 3, 0);

            

            // Reserve Toggle in last column

            Panel pnlReserve = new Panel { Dock = DockStyle.Fill };

            Label lblRes = new Label { Text = "Can Reserve", Font = new Font("Segoe UI", 9, FontStyle.Regular), ForeColor = Color.Gray, AutoSize = true, Location = new Point(0, 0) };

             // Toggle Switch

             Panel pnlToggle = new Panel { Size = new Size(40, 20), Location = new Point(0, 30), Cursor = Cursors.Hand, Tag = canReserve };

             pnlToggle.Paint += (s, e) => {

                 bool isOn = (bool)pnlToggle.Tag;

                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                 Color col = isOn ? ThemeConstants.PrimaryMaroon : Color.FromArgb(200, 200, 200);

                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(0,0,39,19), 10))

                 using(SolidBrush b = new SolidBrush(col)) e.Graphics.FillPath(b, path);

                 int circleX = isOn ? 22 : 2;

                 e.Graphics.FillEllipse(Brushes.White, circleX, 2, 16, 16);

             };

             pnlToggle.Click += (s, e) => {

                 pnlToggle.Tag = !(bool)pnlToggle.Tag;

                 pnlToggle.Invalidate();

             };

             pnlReserve.Controls.Add(lblRes);

             pnlReserve.Controls.Add(pnlToggle);

            tlp.Controls.Add(pnlReserve, 4, 0);



             pnl.Controls.Add(tlp);

             pnl.Height = 150;

             pnl.Controls.SetChildIndex(line, 0);

             pnl.Controls.SetChildIndex(lblT, 0); // Header on top

             

             pnl.Paint += (s, e) => {

                using(Pen p = new Pen(Color.FromArgb(230,230,230)))

                   e.Graphics.DrawRectangle(p, 0, 0, pnl.Width-1, pnl.Height-1);

            };



            return pnl;

        }



        private Button CreateSettingsSubNavButton(string text, bool isActive)

        {

            return new Button

            {

                Text = text,

                Size = new Size(150, 40),

                FlatStyle = FlatStyle.Flat,

                FlatAppearance = { BorderSize = 0 },

                BackColor = Color.White,

                Font = new Font("Segoe UI", 10F, isActive ? FontStyle.Bold : FontStyle.Regular),

                ForeColor = isActive ? ThemeConstants.PrimaryMaroon : Color.Gray,

                Cursor = Cursors.Hand,

                Tag = text

            };

        }



        private Panel CreateCatalogStatCard(string icon, string value, string label, Color accentColor, Point location, string tag = null)

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

                Tag = !string.IsNullOrEmpty(tag) ? tag : "Value",

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



        private void AdminDashboardForm_Load(object sender, EventArgs e)

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

                lblAdminName.Text = SignInForm.CurrentUser.FullName;

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

            addBookForm.BackColor = Color.FromArgb(245, 240, 235); // Beige background

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

            Label btnClose = new Label { Text = "×", Font = new Font("Arial", 18), ForeColor = Color.Gray, Location = new Point(addBookForm.Width - 40, 10), Size = new Size(30, 30), Cursor = Cursors.Hand };

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

            Control txtTitleControl = AddInput("Book title", col1, y, w1);
            TextBox txtTitle = txtTitleControl as TextBox ?? (txtTitleControl.Parent.Controls[0] as TextBox);

            AddLabel("Subtitle", col2, y);

            Control txtSubtitleControl = AddInput("Optional subtitle", col2, y, w1);
            TextBox txtSubtitle = txtSubtitleControl as TextBox ?? (txtSubtitleControl.Parent.Controls[0] as TextBox);

            y += 75;



            // Row 2

            AddLabel("ISBN *", col1, y);

            // Custom ISBN Input with auto-formatting and validation
            Panel pnlISBN = new Panel();
            pnlISBN.Location = new Point(col1, y + 25);
            pnlISBN.Size = new Size(w1, 40);
            pnlISBN.BackColor = Color.White;
            pnlISBN.Padding = new Padding(10, 8, 10, 5);

            TextBox txtISBN = new TextBox();
            txtISBN.BorderStyle = BorderStyle.None;
            txtISBN.Font = new Font("Segoe UI", 10F);
            txtISBN.Dock = DockStyle.Fill;
            txtISBN.BackColor = Color.White;
            txtISBN.Name = "txtISBN"; // Name for identification
            txtISBN.SetPlaceholder("978-0-00-000000-0");
            
            // Auto-generate ISBN when form loads
            string generatedISBN = GenerateISBN();
            txtISBN.SetActualText(generatedISBN);

            // Auto-formatting on text change
            txtISBN.TextChanged += (s, e) => {
                if (txtISBN.Focused)
                {
                    string currentText = txtISBN.Text;
                    string placeholder = "";
                    if (txtISBN.Tag is Helper.PlaceholderTextHelper.PlaceholderData data)
                    {
                        placeholder = data.PlaceholderText;
                    }
                    
                    // Don't format if it's placeholder text
                    if (currentText == placeholder)
                        return;

                    int cursorPos = txtISBN.SelectionStart;
                    string cleanText = System.Text.RegularExpressions.Regex.Replace(currentText, @"[^\dX]", "");
                    
                    // Only format if we have enough digits
                    if (cleanText.Length >= 10)
                    {
                        string formatted = FormatISBN(cleanText);
                        if (formatted != currentText)
                        {
                            txtISBN.Text = formatted;
                            // Restore cursor position (approximate)
                            txtISBN.SelectionStart = Math.Min(cursorPos + (formatted.Length - currentText.Length), formatted.Length);
                        }
                    }
                }
            };

            pnlISBN.Controls.Add(txtISBN);
            addBookForm.Controls.Add(pnlISBN);

            pnlISBN.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool isFocused = (pnlISBN.Tag as string == "Focused");
                bool hasError = (pnlISBN.Tag as string == "Error");
                Color borderColor = hasError ? Color.Red : (isFocused ? Color.Maroon : Color.FromArgb(220, 220, 220));
                float width = (isFocused || hasError) ? 1.5f : 1f;
                using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnlISBN.Width - 3, pnlISBN.Height - 3), 6))
                using(Pen pen = new Pen(borderColor, width))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            txtISBN.Enter += (s, e) => { 
                if (pnlISBN.Tag as string != "Error")
                    pnlISBN.Tag = "Focused"; 
                pnlISBN.Invalidate(); 
            };

            // Combined validation and focus management on leave
            txtISBN.Leave += (s, e) => {
                string isbnText = txtISBN.GetActualText();
                string placeholder = "";
                if (txtISBN.Tag is Helper.PlaceholderTextHelper.PlaceholderData data)
                {
                    placeholder = data.PlaceholderText;
                }
                
                if (!string.IsNullOrWhiteSpace(isbnText) && isbnText != placeholder)
                {
                    if (!IsValidISBN(isbnText))
                    {
                        pnlISBN.Tag = "Error";
                        pnlISBN.Invalidate();
                        MessageBox.Show("Invalid ISBN format. Please enter a valid ISBN-10 or ISBN-13.", "Invalid ISBN", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtISBN.Focus();
                    }
                    else
                    {
                        pnlISBN.Tag = "";
                        pnlISBN.Invalidate();
                    }
                }
                else
                {
                    if (pnlISBN.Tag as string != "Error")
                        pnlISBN.Tag = "";
                    pnlISBN.Invalidate();
                }
            };

            

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

            Control txtAuthorControl = AddInput("Author name", col1, y, w1);
            TextBox txtAuthor = txtAuthorControl as TextBox ?? (txtAuthorControl.Parent.Controls[0] as TextBox);

            AddLabel("Publisher *", col2, y);

            Control txtPublisherControl = AddInput("Publisher name", col2, y, w1);
            TextBox txtPublisher = txtPublisherControl as TextBox ?? (txtPublisherControl.Parent.Controls[0] as TextBox);

            y += 75;



            // Row 4

            AddLabel("Publication Year", col1, y);

            Control txtYearControl = AddNumericInput("2026", col1, y, 70);
            TextBox txtYear = txtYearControl as TextBox ?? (txtYearControl.Parent.Controls[0] as TextBox);
            txtYear.Name = "txtYear";

            

            AddLabel("Pages", col1 + 90, y);

            AddNumericInput("0", col1 + 90, y, 70);



            AddLabel("Copies", col2, y);

            Control txtCopiesControl = AddNumericInput("1", col2, y, w1);
            TextBox txtCopies = txtCopiesControl as TextBox ?? (txtCopiesControl.Parent.Controls[0] as TextBox);
            txtCopies.Name = "txtCopies";

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

            btnAdd.Click += (s, e) => {
                try
                {
                    // Collect all field values
                    string title = txtTitle.GetActualText();
                    string isbn = txtISBN.GetActualText();
                    string author = txtAuthor.GetActualText();
                    string publisher = txtPublisher.GetActualText();
                    string category = cmbCat.SelectedIndex == 0 ? "" : cmbCat.SelectedItem.ToString();
                    string description = txtDesc.GetActualText();
                    
                    // Get publication year
                    int? publicationYear = null;
                    string yearText = txtYear.GetActualText();
                    string yearPlaceholder = "";
                    if (txtYear.Tag is Helper.PlaceholderTextHelper.PlaceholderData yearData)
                    {
                        yearPlaceholder = yearData.PlaceholderText;
                    }
                    if (!string.IsNullOrWhiteSpace(yearText) && yearText != yearPlaceholder)
                    {
                        if (int.TryParse(yearText, out int year) && year >= 1900 && year <= 2100)
                        {
                            publicationYear = year;
                        }
                    }
                    
                    // Get copies
                    int totalCopies = 1;
                    string copiesText = txtCopies.GetActualText();
                    string copiesPlaceholder = "";
                    if (txtCopies.Tag is Helper.PlaceholderTextHelper.PlaceholderData copiesData)
                    {
                        copiesPlaceholder = copiesData.PlaceholderText;
                    }
                    if (!string.IsNullOrWhiteSpace(copiesText) && copiesText != copiesPlaceholder)
                    {
                        if (!int.TryParse(copiesText, out totalCopies) || totalCopies < 1)
                        {
                            MessageBox.Show("Copies must be a number greater than 0.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            txtCopies.Focus();
                            return;
                        }
                    }

                    // Validate required fields
                    string titlePlaceholder = "";
                    if (txtTitle.Tag is Helper.PlaceholderTextHelper.PlaceholderData titleData)
                    {
                        titlePlaceholder = titleData.PlaceholderText;
                    }
                    if (string.IsNullOrWhiteSpace(title) || title == titlePlaceholder)
                    {
                        MessageBox.Show("Title is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtTitle.Focus();
                        return;
                    }

                    string isbnPlaceholder = "";
                    if (txtISBN.Tag is Helper.PlaceholderTextHelper.PlaceholderData isbnData)
                    {
                        isbnPlaceholder = isbnData.PlaceholderText;
                    }
                    if (string.IsNullOrWhiteSpace(isbn) || isbn == isbnPlaceholder)
                    {
                        MessageBox.Show("ISBN is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtISBN.Focus();
                        return;
                    }

                    if (!IsValidISBN(isbn))
                    {
                        MessageBox.Show("Invalid ISBN format. Please enter a valid ISBN-10 or ISBN-13.\n\nISBN-10: 10 digits (e.g., 0-123456-78-9)\nISBN-13: 13 digits starting with 978 or 979 (e.g., 978-0-123456-78-9)", 
                            "Invalid ISBN", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtISBN.Focus();
                        return;
                    }

                    string authorPlaceholder = "";
                    if (txtAuthor.Tag is Helper.PlaceholderTextHelper.PlaceholderData authorData)
                    {
                        authorPlaceholder = authorData.PlaceholderText;
                    }
                    if (string.IsNullOrWhiteSpace(author) || author == authorPlaceholder)
                    {
                        MessageBox.Show("Author is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtAuthor.Focus();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(category) || category == "Enter Category")
                    {
                        MessageBox.Show("Category is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        cmbCat.Focus();
                        return;
                    }

                    // Check for duplicate ISBN - prevent duplicates
                    if (IsISBNDuplicate(isbn))
                    {
                        MessageBox.Show(
                            "This ISBN already exists in the database.\n\nPlease use a different ISBN or edit the existing book.",
                            "Duplicate ISBN",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                        txtISBN.Focus();
                        txtISBN.SelectAll();
                        return;
                    }

                    // Add book to database
                    var bookService = new LMS_Library_Management_System.Service.BookService();
                    int bookId = bookService.AddBook(isbn, title, author, publisher, publicationYear, category, totalCopies, description);

                    if (bookId > 0)
                    {
                        MessageBox.Show("Book added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        addBookForm.DialogResult = DialogResult.OK;
                        // Don't refresh here - will refresh after dialog closes
                    }
                    else
                    {
                        MessageBox.Show("Failed to add book. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(ex.Message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while adding the book: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    System.Diagnostics.Debug.WriteLine($"Error adding book: {ex.Message}");
                }
            };



            addBookForm.Controls.Add(btnCancel);

            addBookForm.Controls.Add(btnAdd);

            // Show dialog and refresh after it closes if book was added
            DialogResult result = addBookForm.ShowDialog(this);
            
            // Refresh book list, statistics, and category filter if book was successfully added
            if (result == DialogResult.OK && pnlMainContent.Tag?.ToString() == "CatalogView")
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("ShowAddBookDialog: Starting refresh after book added");
                    
                    // Refresh everything synchronously (we're already on UI thread after ShowDialog)
                    LoadBooksData();
                    UpdateCatalogStatistics();
                    RefreshCategoryFilter();
                    
                    // Force UI update
                    Application.DoEvents();
                    pnlMainContent.Invalidate();
                    pnlMainContent.Update();
                    
                    System.Diagnostics.Debug.WriteLine("ShowAddBookDialog: Successfully refreshed book list after adding book");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ShowAddBookDialog: Error refreshing after add: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"ShowAddBookDialog: StackTrace: {ex.StackTrace}");
                    
                    // Retry with BeginInvoke as fallback
                    this.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            LoadBooksData();
                            UpdateCatalogStatistics();
                            RefreshCategoryFilter();
                        }
                        catch (Exception retryEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"ShowAddBookDialog: Retry refresh also failed: {retryEx.Message}");
                        }
                    }));
                }
            }

        }



        private void ShowCheckOutDialog()

        {

            Form checkOutForm = new Form();

            checkOutForm.Size = new Size(450, 350);

            checkOutForm.FormBorderStyle = FormBorderStyle.None;

            checkOutForm.StartPosition = FormStartPosition.CenterParent;

            checkOutForm.BackColor = Color.FromArgb(245, 240, 235); // Beige background

            checkOutForm.ShowInTaskbar = false;



             checkOutForm.Paint += (s, e) => {

                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                 using (Pen p = new Pen(Color.FromArgb(200, 200, 200), 1))

                 {

                     e.Graphics.DrawRectangle(p, 0, 0, checkOutForm.Width - 1, checkOutForm.Height - 1);

                 }

            };



            Label btnClose = new Label { Text = "×", Font = new Font("Arial", 18), ForeColor = Color.Gray, Location = new Point(checkOutForm.Width - 40, 10), Size = new Size(30, 30), Cursor = Cursors.Hand };

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

            rvForm.BackColor = Color.FromArgb(245, 240, 235); // Beige background

            rvForm.ShowInTaskbar = false;



             rvForm.Paint += (s, e) => {

                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                 using (Pen p = new Pen(Color.FromArgb(200, 200, 200), 1))

                 {

                     e.Graphics.DrawRectangle(p, 0, 0, rvForm.Width - 1, rvForm.Height - 1);

                 }

            };



            Label btnClose = new Label { Text = "×", Font = new Font("Arial", 18), ForeColor = Color.Gray, Location = new Point(rvForm.Width - 40, 10), Size = new Size(30, 30), Cursor = Cursors.Hand };

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

            fineForm.BackColor = Color.FromArgb(245, 240, 235); // Beige background

            fineForm.ShowInTaskbar = false;



             fineForm.Paint += (s, e) => {

                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                 using (Pen p = new Pen(Color.FromArgb(200, 200, 200), 1))

                 {

                     e.Graphics.DrawRectangle(p, 0, 0, fineForm.Width - 1, fineForm.Height - 1);

                 }

            };



            Label btnClose = new Label { Text = "×", Font = new Font("Arial", 18), ForeColor = Color.Gray, Location = new Point(fineForm.Width - 40, 10), Size = new Size(30, 30), Cursor = Cursors.Hand };

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

    // Helper class for member data (shared between Admin and Staff dashboards)
    public class MemberInfo
    {
        public string MemberId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Department { get; set; }
        public string Type { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
        public int BooksBorrowed { get; set; }
        public int BooksLimit { get; set; }
        public decimal Fines { get; set; }
    }

    // Dialog classes consolidated into this file
    public class AddUserDialog : Form
    {
        public string FullName { get; private set; }
        public string UserInput { get; private set; }
        public string Email { get; private set; }
        public string PhoneNumber { get; private set; }
        public string Department { get; private set; }
        public string Role { get; private set; }

        private TextBox txtFullName;
        private TextBox txtUserInput;
        private TextBox txtEmail;
        private TextBox txtPhoneNumber;
        private ComboBox cmbDepartment;
        private ComboBox cmbRole;

        public AddUserDialog()
        {
            InitializeComponent();
            this.Text = "Add New User";
            this.Size = new Size(500, 680); // Increased height to accommodate all fields including Full Name with proper spacing
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            // Soft beige background so the dialog stands out against the app
            this.BackColor = Color.FromArgb(248, 247, 242);
            this.Padding = new Padding(0);
            
            SetupDialog();
        }

        private void SetupDialog()
        {
            // Main container panel (match form background for a unified card look)
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 247, 242),
                Padding = new Padding(0)
            };

            // Header panel
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(248, 247, 242),
                Padding = new Padding(30, 25, 30, 15)
            };

            Label lblTitle = new Label
            {
                Text = "Add New User",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = ThemeConstants.PrimaryMaroon,
                Location = new Point(30, 25),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "Create a new librarian or staff account",
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
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.Black;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.Gray;

            headerPanel.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, btnClose });

            // Content panel (same beige as background so there is no white strip)
            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 247, 242),
                Padding = new Padding(30, 20, 30, 20),
                AutoScroll = true
            };

            int yPos = 20; // Start lower to add more margin from top
            int inputWidth = 400;
            int spacing = 25;

            // Full Name - FIRST FIELD (must be visible at the top)
            Label lblFullName = new Label
            {
                Text = "Full Name *",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, yPos),
                Size = new Size(inputWidth, 25),
                AutoSize = false,
                Visible = true
            };

            Panel pnlFullName = new Panel
            {
                Location = new Point(0, yPos + 25),
                Size = new Size(inputWidth, 40),
                BackColor = Color.FromArgb(248, 247, 242),
                Visible = true
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
                BackColor = Color.FromArgb(248, 247, 242),
                Text = ""
            };
            PlaceholderTextHelper.SetPlaceholder(txtFullName, "Enter full name");
            pnlFullName.Controls.Add(txtFullName);

            yPos += 25 + 40 + spacing;

            // Full Name
            Label lblUserInput = new Label
            {
                Text = "Full Name",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, yPos),
                Size = new Size(inputWidth, 25),
                AutoSize = false
            };

            Panel pnlUserInput = new Panel
            {
                Location = new Point(0, yPos + 25),
                Size = new Size(inputWidth, 40),
                BackColor = Color.FromArgb(248, 247, 242)
            };
            pnlUserInput.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlUserInput.Width - 1, pnlUserInput.Height - 1), 6))
                using (Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            txtUserInput = new TextBox
            {
                Location = new Point(10, 8),
                Size = new Size(inputWidth - 20, 24),
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 247, 242),
                Text = ""
            };
            PlaceholderTextHelper.SetPlaceholder(txtUserInput, "Enter full name");
            pnlUserInput.Controls.Add(txtUserInput);

            yPos += 25 + 40 + spacing;

            // Email
            Label lblEmail = new Label
            {
                Text = "Email Address *",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, yPos),
                Size = new Size(inputWidth, 25),
                AutoSize = false
            };

            Panel pnlEmail = new Panel
            {
                Location = new Point(0, yPos + 25),
                Size = new Size(inputWidth, 40),
                BackColor = Color.FromArgb(248, 247, 242)
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
                BackColor = Color.FromArgb(248, 247, 242),
                Text = ""
            };
            PlaceholderTextHelper.SetPlaceholder(txtEmail, "user@library.com");
            pnlEmail.Controls.Add(txtEmail);

            yPos += 25 + 40 + spacing;

            // Phone Number
            Label lblPhone = new Label
            {
                Text = "Phone Number",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, yPos),
                Size = new Size(inputWidth, 25),
                AutoSize = false
            };

            Panel pnlPhone = new Panel
            {
                Location = new Point(0, yPos + 25),
                Size = new Size(inputWidth, 40),
                BackColor = Color.FromArgb(248, 247, 242)
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
                BackColor = Color.FromArgb(248, 247, 242),
                Text = "",
                MaxLength = 11
            };
            // Allow only numeric input for phone number (no letters or symbols) and limit to 11 digits
            txtPhoneNumber.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
                // Prevent typing if already 11 digits (excluding control characters)
                if (char.IsDigit(e.KeyChar) && txtPhoneNumber.Text.Length >= 11)
                {
                    e.Handled = true;
                }
            };
            PlaceholderTextHelper.SetPlaceholder(txtPhoneNumber, "09XXXXXXXXX");
            pnlPhone.Controls.Add(txtPhoneNumber);

            yPos += 25 + 40 + spacing;

            // Department
            Label lblDepartment = new Label
            {
                Text = "Department",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, yPos),
                Size = new Size(inputWidth, 25),
                AutoSize = false
            };

            Panel pnlDepartment = new Panel
            {
                Location = new Point(0, yPos + 25),
                Size = new Size(inputWidth, 40),
                BackColor = Color.FromArgb(248, 247, 242)
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

            cmbDepartment = new ComboBox
            {
                Location = new Point(10, 5),
                Size = new Size(inputWidth - 20, 30),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(248, 247, 242),
                FlatStyle = FlatStyle.Flat
            };
            // Add placeholder as first item
            cmbDepartment.Items.Add("Choose department");
            cmbDepartment.Items.AddRange(new string[] 
            { 
                "Computing Education Department",
                "Department of Engineering Education",
                "Department of Teacher Education",
                "Department of Arts and Sciences Education",
                "Department of Business Administration Education",
                "Department of Hospitality Education",
                "JHS Department"
            });
            cmbDepartment.SelectedIndex = 0; // Select placeholder by default
            cmbDepartment.ForeColor = Color.Gray; // Gray color for placeholder
            
            // Change color when a real department is selected
            cmbDepartment.SelectedIndexChanged += (s, e) =>
            {
                if (cmbDepartment.SelectedIndex == 0)
                {
                    cmbDepartment.ForeColor = Color.Gray;
                }
                else
                {
                    cmbDepartment.ForeColor = Color.Black;
                }
            };
            
            pnlDepartment.Controls.Add(cmbDepartment);

            yPos += 25 + 40 + spacing;

            // Role
            Label lblRole = new Label
            {
                Text = "Role *",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, yPos),
                Size = new Size(inputWidth, 25),
                AutoSize = false
            };

            Panel pnlRole = new Panel
            {
                Location = new Point(0, yPos + 25),
                Size = new Size(inputWidth, 40),
                BackColor = Color.FromArgb(248, 247, 242)
            };
            pnlRole.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlRole.Width - 1, pnlRole.Height - 1), 6))
                using (Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            cmbRole = new ComboBox
            {
                Location = new Point(10, 5),
                Size = new Size(inputWidth - 20, 30),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(248, 247, 242),
                FlatStyle = FlatStyle.Flat
            };
            cmbRole.Items.AddRange(new string[] { "Staff", "Librarian/Admin" });
            cmbRole.SelectedIndex = 0; // Default to "Staff"
            pnlRole.Controls.Add(cmbRole);

            // Add controls to contentPanel - Full Name MUST be first and at the top
            // Add Full Name controls FIRST
            contentPanel.Controls.Add(lblFullName);
            contentPanel.Controls.Add(pnlFullName);
            
            // Add User Input controls
            contentPanel.Controls.Add(lblUserInput);
            contentPanel.Controls.Add(pnlUserInput);
            
            // Add all other controls
            contentPanel.Controls.Add(lblEmail);
            contentPanel.Controls.Add(pnlEmail);
            contentPanel.Controls.Add(lblPhone);
            contentPanel.Controls.Add(pnlPhone);
            contentPanel.Controls.Add(lblDepartment);
            contentPanel.Controls.Add(pnlDepartment);
            contentPanel.Controls.Add(lblRole);
            contentPanel.Controls.Add(pnlRole);
            
            // CRITICAL: Bring Full Name to front to ensure it's visible
            lblFullName.BringToFront();
            pnlFullName.BringToFront();
            
            // Explicitly set positions to ensure Full Name is at top
            lblFullName.Location = new Point(0, 0);
            pnlFullName.Location = new Point(0, 25);
            
            // Force refresh
            contentPanel.Invalidate();

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
                Anchor = AnchorStyles.Bottom,
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

            // Add User button
            Button btnAddUser = new Button
            {
                Text = "👤+ Add User",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ThemeConstants.PrimaryMaroon,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(140, 40),
                Anchor = AnchorStyles.Bottom,
                Cursor = Cursors.Hand
            };
            btnAddUser.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnAddUser.Width - 1, btnAddUser.Height - 1), 6))
                {
                    e.Graphics.FillPath(new SolidBrush(btnAddUser.BackColor), path);
                }
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                e.Graphics.DrawString(btnAddUser.Text, btnAddUser.Font, new SolidBrush(btnAddUser.ForeColor), 
                    new RectangleF(0, 0, btnAddUser.Width, btnAddUser.Height), sf);
            };
            btnAddUser.MouseEnter += (s, e) => 
            {
                btnAddUser.BackColor = ThemeConstants.AccentMaroonHover;
                btnAddUser.Invalidate();
            };
            btnAddUser.MouseLeave += (s, e) => 
            {
                btnAddUser.BackColor = ThemeConstants.PrimaryMaroon;
                btnAddUser.Invalidate();
            };
            btnAddUser.Click += (s, e) =>
            {
                // Validate Full Name - check txtUserInput (the visible Full Name field)
                string fullName = txtUserInput.GetActualText()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    MessageBox.Show("Full Name is required.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtUserInput.Focus();
                    return;
                }
                
                // Validate that Full Name contains at least two words (first and last name)
                string[] nameParts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (nameParts.Length < 2)
                {
                    MessageBox.Show("Full Name must contain at least two words (first name and last name).", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtUserInput.Focus();
                    return;
                }

                // Get the original Full Name field value (if it exists) for backward compatibility
                string originalFullName = txtFullName.GetActualText()?.Trim() ?? "";
                string userInput = fullName; // Use the validated full name

                // Validate Email - just check format, not .edu requirement
                string email = txtEmail.GetActualText()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(email))
                {
                    MessageBox.Show("Email Address is required.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }
                // Validate email format using MailAddress
                try
                {
                    var addr = new System.Net.Mail.MailAddress(email);
                    if (addr.Address != email || !email.Contains("@"))
                    {
                        MessageBox.Show("Please enter a valid email address format.", "Validation Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtEmail.Focus();
                        return;
                    }
                }
                catch
                {
                    MessageBox.Show("Please enter a valid email address format.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }

                // Validate Phone Number - must start with "09" and be 11 digits
                string phoneNumber = txtPhoneNumber.GetActualText()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    MessageBox.Show("Phone Number is required.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPhoneNumber.Focus();
                    return;
                }
                if (phoneNumber.Length != 11)
                {
                    MessageBox.Show("Phone Number must be exactly 11 digits.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPhoneNumber.Focus();
                    return;
                }
                if (!phoneNumber.StartsWith("09"))
                {
                    MessageBox.Show("Phone Number must start with '09'.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPhoneNumber.Focus();
                    return;
                }

                // Validate Department
                if (cmbDepartment.SelectedIndex <= 0 || cmbDepartment.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select a department.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbDepartment.Focus();
                    return;
                }

                // Validate Role
                if (cmbRole.SelectedIndex == -1 || cmbRole.SelectedItem == null)
                {
                    MessageBox.Show("Please select a role.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbRole.Focus();
                    return;
                }

                // All validations passed - set values and close dialog
                FullName = fullName;
                UserInput = userInput;
                Email = email;
                PhoneNumber = phoneNumber;
                // Get department value (skip placeholder if selected)
                Department = (cmbDepartment.SelectedIndex > 0 && cmbDepartment.SelectedItem != null) 
                    ? cmbDepartment.SelectedItem.ToString() 
                    : "";
                Role = cmbRole.SelectedItem?.ToString() ?? "";
                this.DialogResult = DialogResult.OK;
            };

            footerPanel.Controls.AddRange(new Control[] { btnCancel, btnAddUser });

            // Center inputs horizontally within the content panel on resize
            contentPanel.Resize += (s, e) =>
            {
                int centerX = (contentPanel.ClientSize.Width - inputWidth) / 2;
                if (centerX < 0) centerX = 0;

                // Ensure Full Name is at the absolute top and visible
                lblFullName.Left = centerX;
                lblFullName.Top = 0; // At top of contentPanel client area (padding already applied)
                pnlFullName.Left = centerX;
                pnlFullName.Top = 25; // Label height (25px) - Full Name must be FIRST

                lblUserInput.Left = centerX;
                pnlUserInput.Left = centerX;

                lblEmail.Left = centerX;
                pnlEmail.Left = centerX;

                lblPhone.Left = centerX;
                pnlPhone.Left = centerX;

                lblDepartment.Left = centerX;
                pnlDepartment.Left = centerX;

                lblRole.Left = centerX;
                pnlRole.Left = centerX;
            };

            // Center the buttons as a group at the bottom
            footerPanel.Resize += (s, e) =>
            {
                int totalWidth = btnCancel.Width + 10 + btnAddUser.Width;
                int startX = (footerPanel.ClientSize.Width - totalWidth) / 2;
                if (startX < 0) startX = 0;

                btnCancel.Left = startX;
                btnCancel.Top = 15;

                btnAddUser.Left = startX + btnCancel.Width + 10;
                btnAddUser.Top = 15;
            };

            mainPanel.Controls.Add(headerPanel);
            mainPanel.Controls.Add(contentPanel);
            mainPanel.Controls.Add(footerPanel);

            this.Controls.Add(mainPanel);
            
            // Ensure Full Name field is visible when dialog loads
            this.Load += (s, e) =>
            {
                if (lblFullName != null && pnlFullName != null)
                {
                    // Reset scroll position to top
                    contentPanel.AutoScrollPosition = new Point(0, 0);
                    
                    // Ensure Full Name controls are visible
                    lblFullName.Visible = true;
                    pnlFullName.Visible = true;
                    lblFullName.BringToFront();
                    pnlFullName.BringToFront();
                    
                    // Scroll to show Full Name field
                    contentPanel.ScrollControlIntoView(lblFullName);
                    
                    // Force refresh
                    contentPanel.Invalidate();
                    contentPanel.Update();
                }
            };
            
            // Also ensure Full Name is visible when form is shown
            this.Shown += (s, e) =>
            {
                if (lblFullName != null && pnlFullName != null && contentPanel != null)
                {
                    // Reset scroll to top
                    contentPanel.AutoScrollPosition = new Point(0, 0);
                    // Ensure Full Name is at the top
                    contentPanel.ScrollControlIntoView(lblFullName);
                    // Refresh
                    contentPanel.Invalidate();
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

    public class AssignRoleDialog : Form
    {
        public string SelectedRole { get; private set; }

        private ComboBox cmbRole;
        private Panel pnlPermissions;
        private Label lblPermissionsTitle;

        public AssignRoleDialog(string userName, string currentRole = "Admin")
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
                AutoSize = true,
                Visible = true
            };

            // Role dropdown - positioned below the "Select Role" label
            Panel pnlRole = new Panel
            {
                Location = new Point(0, 100),
                Size = new Size(440, 40),
                BackColor = Color.White,
                Visible = true
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
                FlatStyle = FlatStyle.Flat,
                Visible = true
            };
            
            // Add placeholder item and actual roles
            cmbRole.Items.Add("Choose role"); // Placeholder
            cmbRole.Items.Add("Staff");
            cmbRole.Items.Add("Admin");
            
            // Set selected item based on current role
            if (currentRole == "Staff")
                cmbRole.SelectedItem = "Staff";
            else if (currentRole == "Admin" || currentRole == "Librarian/Admin")
                cmbRole.SelectedItem = "Admin";
            else
                cmbRole.SelectedIndex = 0; // Default to placeholder
            
            // Handle selection change to update permissions
            cmbRole.SelectedIndexChanged += (s, e) => UpdatePermissionsDisplay();
            
            pnlRole.Controls.Add(cmbRole);
            
            // Ensure dropdown is visible and on top
            cmbRole.BringToFront();
            pnlRole.BringToFront();
            lblSelectRole.BringToFront();

            // Role Permissions label - increased margin top for better spacing
            lblPermissionsTitle = new Label
            {
                Text = "Role Permissions:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, 150), // Increased from 90 to 100 for more spacing
                AutoSize = true
            };

            // Permissions list - adjusted to match new label position
            pnlPermissions = new Panel
            {
                Location = new Point(0, 170), // Increased from 120 to 130 to match label spacing
                Size = new Size(440, 200), // Increased height to accommodate detailed permissions
                BackColor = Color.Transparent,
                AutoScroll = true
            };

            // Initialize permissions display
            UpdatePermissionsDisplay();

            contentPanel.Controls.AddRange(new Control[] 
            { 
                lblSelectRole, pnlRole, 
                lblPermissionsTitle, pnlPermissions 
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
                Text = "Assign Role",
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
                string selected = cmbRole.SelectedItem?.ToString() ?? "";
                
                // Validate that a role is selected (not placeholder)
                if (selected == "Choose role" || string.IsNullOrEmpty(selected))
                {
                    MessageBox.Show("Please select a role.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                SelectedRole = selected;
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

        private void UpdatePermissionsDisplay()
        {
            // Clear existing permissions
            pnlPermissions.Controls.Clear();
            
            string selectedRole = cmbRole.SelectedItem?.ToString() ?? "";
            string[] permissions;
            
            if (selectedRole == "Staff")
            {
                permissions = new string[]
                {
                    "• Dashboard: View daily activity summary",
                    "• Members: Register new members, view info",
                    "• Circulation: Borrow, return, renew, validate members",
                    "• Reservations: View reservations, assist members",
                    "• Fines: View fines, collect payments",
                    "• Inventory: Mark damaged, update location",
                    "• Search: Yes",
                    "• Sign Out: Yes",
                    "",
                    "❌ No access: User Management, Catalog edit, Reports, Settings"
                };
            }
            else if (selectedRole == "Admin")
            {
                permissions = new string[]
                {
                    "• Dashboard: View all statistics, charts, analytics",
                    "• User Management: Add, edit, deactivate users, assign roles",
                    "• Members: Register, edit, suspend, activate, view history",
                    "• Catalog: Add, edit, delete, archive books, bulk import",
                    "• Circulation: Borrow, return, renew, override rules",
                    "• Reservations: View, approve, cancel, manage all",
                    "• Fines: View, calculate, collect, waive, adjust, audit",
                    "• Inventory: Mark lost, damaged, repair, update location",
                    "• Reports: Generate ALL reports",
                    "• Search: Full advanced search",
                    "• Settings: Configure system rules, fine rates, limits",
                    "• Sign Out: Yes"
                };
            }
            else
            {
                // No role selected (placeholder)
                permissions = new string[]
                {
                    "Select a role to view permissions"
                };
            }
            
            int permY = 0;
            foreach (string perm in permissions)
            {
                if (string.IsNullOrEmpty(perm))
                {
                    permY += 10; // Add spacing for empty line
                    continue;
                }
                
                Label lblPerm = new Label
                {
                    Text = perm,
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = selectedRole == "Choose role" ? Color.Gray : ThemeConstants.TextDark,
                    Location = new Point(0, permY),
                    AutoSize = true,
                    MaximumSize = new Size(440, 0) // Allow text wrapping
                };
                pnlPermissions.Controls.Add(lblPerm);
                permY += 22; // Reduced spacing slightly for more items
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

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);
        }
    }

    public class DeactivateUserDialog : Form
    {
        public bool DeactivateUser { get; private set; }

        public DeactivateUserDialog(string userName)
        {
            InitializeComponent();
            this.Text = "Delete User";
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
                Text = "Delete User",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, 0),
                AutoSize = true
            };

            // Message
            Label lblMessage = new Label
            {
                Text = $"Are you sure you want to permanently delete {userName}'s account? This action cannot be undone and all user data will be removed from the database.",
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

            // Delete button (red)
            Button btnDeactivate = new Button
            {
                Text = "Delete",
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

    public class EditUserDialog : Form
    {
        public string FullName { get; private set; }
        public string Email { get; private set; }
        public string PhoneNumber { get; private set; }
        public string Department { get; private set; }

        private TextBox txtFullName;
        private TextBox txtEmail;
        private TextBox txtPhoneNumber;
        private ComboBox cmbDepartment;

        public EditUserDialog(string fullName = "", string email = "", string phoneNumber = "", string department = "")
        {
            InitializeComponent();
            this.Text = "Edit User";
            this.Size = new Size(500, 600); // Increased height to ensure all fields are visible
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            // Match AddUserDialog beige
            this.BackColor = Color.FromArgb(248, 247, 242);
            this.Padding = new Padding(0);
            
            SetupDialog(fullName, email, phoneNumber, department);
        }

        private void SetupDialog(string fullName, string email, string phoneNumber, string department)
        {
            // Main container panel
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 247, 242),
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            // Header panel
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(248, 247, 242),
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
                BackColor = Color.FromArgb(248, 247, 242),
                Padding = new Padding(30, 20, 30, 20),
                AutoScroll = true
            };

            int yPos = 100; // Start with margin from top
            int inputWidth = 400;
            int spacing = 25;

            // Full Name - FIRST FIELD (must be visible at the top)
            Label lblFullName = new Label
            {
                Text = "Full Name *",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, yPos),
                Size = new Size(inputWidth, 25),
                AutoSize = false
            };

            Panel pnlFullName = new Panel
            {
                Location = new Point(0, yPos + 25),
                Size = new Size(inputWidth, 40),
                BackColor = Color.FromArgb(248, 247, 242)
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
                BackColor = Color.FromArgb(248, 247, 242),
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
                Size = new Size(inputWidth, 25),
                AutoSize = false
            };

            Panel pnlEmail = new Panel
            {
                Location = new Point(0, yPos + 25),
                Size = new Size(inputWidth, 40),
                BackColor = Color.FromArgb(248, 247, 242)
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
                BackColor = Color.FromArgb(248, 247, 242),
                Text = email
            };
            if (string.IsNullOrWhiteSpace(email))
            {
                PlaceholderTextHelper.SetPlaceholder(txtEmail, "user@library.com");
            }
            pnlEmail.Controls.Add(txtEmail);

            yPos += 25 + 40 + spacing;

            // Phone Number
            Label lblPhone = new Label
            {
                Text = "Phone Number",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, yPos),
                Size = new Size(inputWidth, 25),
                AutoSize = false
            };

            Panel pnlPhone = new Panel
            {
                Location = new Point(0, yPos + 25),
                Size = new Size(inputWidth, 40),
                BackColor = Color.FromArgb(248, 247, 242)
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
                BackColor = Color.FromArgb(248, 247, 242),
                Text = phoneNumber ?? "",
                MaxLength = 11,
                ReadOnly = false,
                Enabled = true,
                ForeColor = Color.Black
            };
            
            // Only set placeholder if phone number is empty or null
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                PlaceholderTextHelper.SetPlaceholder(txtPhoneNumber, "09XXXXXXXXX");
            }
            else
            {
                // If phone number exists, ensure it's displayed and editable
                txtPhoneNumber.Text = phoneNumber;
                txtPhoneNumber.ForeColor = Color.Black;
            }
            
            // Allow only numeric input for phone number (no letters or symbols) and limit to 11 digits
            txtPhoneNumber.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
                // Prevent typing if already 11 digits (excluding control characters)
                if (char.IsDigit(e.KeyChar) && txtPhoneNumber.Text.Length >= 11)
                {
                    e.Handled = true;
                }
            };
            
            // Ensure the field is editable when it gets focus
            txtPhoneNumber.Enter += (s, e) =>
            {
                // Clear placeholder if it's showing
                string actualText = txtPhoneNumber.GetActualText() ?? txtPhoneNumber.Text ?? "";
                if (string.IsNullOrWhiteSpace(actualText) || actualText == "09XXXXXXXXX")
                {
                    txtPhoneNumber.Text = "";
                    txtPhoneNumber.ForeColor = Color.Black;
                }
                else
                {
                    // Ensure text color is black for editing
                    txtPhoneNumber.ForeColor = Color.Black;
                }
            };
            
            // Ensure text color stays black when typing
            txtPhoneNumber.TextChanged += (s, e) =>
            {
                if (txtPhoneNumber.ForeColor != Color.Black && !string.IsNullOrWhiteSpace(txtPhoneNumber.Text))
                {
                    txtPhoneNumber.ForeColor = Color.Black;
                }
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
                Size = new Size(inputWidth, 25),
                AutoSize = false
            };

            Panel pnlDepartment = new Panel
            {
                Location = new Point(0, yPos + 25),
                Size = new Size(inputWidth, 40),
                BackColor = Color.FromArgb(248, 247, 242)
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

            cmbDepartment = new ComboBox
            {
                Location = new Point(10, 5),
                Size = new Size(inputWidth - 20, 30),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(248, 247, 242),
                FlatStyle = FlatStyle.Flat
            };
            // Add placeholder as first item
            cmbDepartment.Items.Add("Choose department");
            cmbDepartment.Items.AddRange(new string[] 
            { 
                "Computing Education Department",
                "Department of Engineering Education",
                "Department of Teacher Education",
                "Department of Arts and Sciences Education",
                "Department of Business Administration Education",
                "Department of Hospitality Education",
                "JHS Department"
            });
            
            // Set selected department if provided
            if (!string.IsNullOrWhiteSpace(department))
            {
                int deptIndex = cmbDepartment.Items.IndexOf(department);
                if (deptIndex > 0)
                {
                    cmbDepartment.SelectedIndex = deptIndex;
                    cmbDepartment.ForeColor = Color.Black;
                }
                else
                {
                    cmbDepartment.SelectedIndex = 0;
                    cmbDepartment.ForeColor = Color.Gray;
                }
            }
            else
            {
                cmbDepartment.SelectedIndex = 0; // Select placeholder by default
                cmbDepartment.ForeColor = Color.Gray; // Gray color for placeholder
            }
            
            // Change color when a real department is selected
            cmbDepartment.SelectedIndexChanged += (s, e) =>
            {
                if (cmbDepartment.SelectedIndex == 0)
                {
                    cmbDepartment.ForeColor = Color.Gray;
                }
                else
                {
                    cmbDepartment.ForeColor = Color.Black;
                }
            };
            
            pnlDepartment.Controls.Add(cmbDepartment);

            // Add controls to contentPanel - Full Name MUST be first and at the top
            // Add Full Name controls FIRST
            contentPanel.Controls.Add(lblFullName);
            contentPanel.Controls.Add(pnlFullName);
            
            
            contentPanel.Controls.Add(lblEmail);
            
            contentPanel.Controls.Add(pnlEmail);
            contentPanel.Controls.Add(lblPhone);
            contentPanel.Controls.Add(pnlPhone);
            contentPanel.Controls.Add(lblDepartment);
            contentPanel.Controls.Add(pnlDepartment);
            
            // CRITICAL: Bring Full Name to front to ensure it's visible
            lblFullName.BringToFront();
            pnlFullName.BringToFront();
            
            // Explicitly set positions to ensure Full Name is at top
            lblFullName.Location = new Point(0, 20);
            pnlFullName.Location = new Point(0, 45);

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
                Anchor = AnchorStyles.Bottom,
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
                Anchor = AnchorStyles.Bottom,
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
                // Full Name Validation
                string validatedFullName = txtFullName.Text?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(validatedFullName))
                {
                    MessageBox.Show("Full Name is required.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtFullName.Focus();
                    return;
                }
                string[] nameParts = validatedFullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (nameParts.Length < 2)
                {
                    MessageBox.Show("Full Name must contain at least two words (first name and last name).", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtFullName.Focus();
                    return;
                }
                
                // Email Validation
                string validatedEmail = txtEmail.Text?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(validatedEmail))
                {
                    MessageBox.Show("Email Address is required.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }
                try
                {
                    var addr = new System.Net.Mail.MailAddress(validatedEmail);
                    if (addr.Address != validatedEmail)
                    {
                        MessageBox.Show("Please enter a valid email address.", "Validation Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtEmail.Focus();
                        return;
                    }
                }
                catch
                {
                    MessageBox.Show("Please enter a valid email address.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }
                
                // Phone Number Validation (optional but if provided, must be valid)
                string validatedPhoneNumber = txtPhoneNumber.GetActualText()?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(validatedPhoneNumber))
                {
                    if (validatedPhoneNumber.Length != 11 || !validatedPhoneNumber.StartsWith("09"))
                    {
                        MessageBox.Show("Phone Number must be exactly 11 digits and start with '09'.", "Validation Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtPhoneNumber.Focus();
                        return;
                    }
                }
                
                // Department Validation (optional)
                string selectedDepartment = "";
                if (cmbDepartment.SelectedIndex > 0)
                {
                    selectedDepartment = cmbDepartment.SelectedItem.ToString();
                }
                
                FullName = validatedFullName;
                Email = validatedEmail;
                PhoneNumber = validatedPhoneNumber;
                Department = selectedDepartment;
                this.DialogResult = DialogResult.OK;
            };

            footerPanel.Controls.AddRange(new Control[] { btnCancel, btnSave });

            // Center inputs horizontally
            contentPanel.Resize += (s, e) =>
            {
                int centerX = (contentPanel.ClientSize.Width - inputWidth) / 2;
                if (centerX < 0) centerX = 0;

                // Ensure Full Name stays at top
                lblFullName.Left = centerX;
                lblFullName.Top = 90;
                pnlFullName.Left = centerX;
                pnlFullName.Top = 115;

                // Position other controls based on their calculated positions
                lblEmail.Left = centerX;
                pnlEmail.Left = centerX;

                lblPhone.Left = centerX;
                pnlPhone.Left = centerX;

                lblDepartment.Left = centerX;
                pnlDepartment.Left = centerX;
            };
            
            // Ensure Full Name is visible on load
            this.Load += (s, e) =>
            {
                contentPanel.AutoScrollPosition = new Point(0, 0);
                lblFullName.BringToFront();
                pnlFullName.BringToFront();
            };
            
            this.Shown += (s, e) =>
            {
                contentPanel.AutoScrollPosition = new Point(0, 0);
                contentPanel.ScrollControlIntoView(lblFullName);
                lblFullName.BringToFront();
                pnlFullName.BringToFront();
            };

            // Center buttons group
            footerPanel.Resize += (s, e) =>
            {
                int totalWidth = btnCancel.Width + 10 + btnSave.Width;
                int startX = (footerPanel.ClientSize.Width - totalWidth) / 2;
                if (startX < 0) startX = 0;

                btnCancel.Left = startX;
                btnCancel.Top = 15;

                btnSave.Left = startX + btnCancel.Width + 10;
                btnSave.Top = 15;
            };

            mainPanel.Controls.Add(headerPanel);
            mainPanel.Controls.Add(contentPanel);
            mainPanel.Controls.Add(footerPanel);

            this.Controls.Add(mainPanel);
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

    public class UserResetPasswordDialog : Form
    {
        public string NewPassword { get; private set; }
        private TextBox txtConfirmPassword;
        private string _email;

        public UserResetPasswordDialog(string email)
        {
            InitializeComponent();
            _email = email;
            this.Text = "Reset Password";
            this.Size = new Size(500, 300); // Reduced height since we only have one field now
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(248, 247, 242);
            this.Padding = new Padding(0);
            
            SetupDialog(email);
        }

        private void SetupDialog(string email)
        {
            // Main container panel
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 247, 242),
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            // Header panel
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = Color.FromArgb(248, 247, 242),
                Padding = new Padding(30, 25, 30, 15)
            };

            Label lblTitle = new Label
            {
                Text = "Reset Password",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = ThemeConstants.PrimaryMaroon,
                Location = new Point(30, 20),
                AutoSize = true
            };

            Label lblUserInfo = new Label
            {
                Text = $"Resetting password for: {email}",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Gray,
                Location = new Point(30, 55),
                AutoSize = true
            };

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(lblUserInfo);

            // Content panel
            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 247, 242),
                Padding = new Padding(30, 40, 30, 30), // Increased top padding to push fields down
                AutoScroll = true // Enable scrolling in case fields don't fit
            };

            int inputWidth = 440;
            int yPos = 0; // Start at top of content panel (padding is handled by panel)

            // Reset Password field (renamed from Confirm Password, New Password field removed)
            Label lblConfirmPassword = new Label
            {
                Text = "Reset Password *",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = ThemeConstants.TextDark,
                Location = new Point(0, yPos),
                Size = new Size(inputWidth, 25),
                AutoSize = false
            };

            Panel pnlConfirmPassword = new Panel
            {
                Location = new Point(0, yPos + 25),
                Size = new Size(inputWidth, 40),
                BackColor = Color.FromArgb(248, 247, 242)
            };
            pnlConfirmPassword.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlConfirmPassword.Width - 1, pnlConfirmPassword.Height - 1), 6))
                using (Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            txtConfirmPassword = new TextBox
            {
                Name = "txtConfirmPassword",
                Location = new Point(10, 8),
                Size = new Size(inputWidth - 20, 24),
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 247, 242),
                PasswordChar = '*',
                UseSystemPasswordChar = true
            };
            PlaceholderTextHelper.SetPlaceholder(txtConfirmPassword, "Enter new password");
            
            // Handle password field focus to show/hide placeholder
            txtConfirmPassword.Enter += (s, e) =>
            {
                string actualText = txtConfirmPassword.GetActualText() ?? "";
                if (string.IsNullOrWhiteSpace(actualText) || actualText == "Enter new password")
                {
                    txtConfirmPassword.Text = "";
                    txtConfirmPassword.PasswordChar = '*';
                    txtConfirmPassword.UseSystemPasswordChar = true;
                    txtConfirmPassword.ForeColor = Color.Black;
                }
            };
            
            txtConfirmPassword.TextChanged += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(txtConfirmPassword.Text) && txtConfirmPassword.ForeColor != Color.Black)
                {
                    txtConfirmPassword.ForeColor = Color.Black;
                    txtConfirmPassword.PasswordChar = '*';
                    txtConfirmPassword.UseSystemPasswordChar = true;
                }
            };
            
            pnlConfirmPassword.Controls.Add(txtConfirmPassword);

            // Add controls to contentPanel - only Reset Password field
            contentPanel.Controls.Add(lblConfirmPassword);
            contentPanel.Controls.Add(pnlConfirmPassword);
            
            // Ensure field is visible and properly positioned
            lblConfirmPassword.Visible = true;
            pnlConfirmPassword.Visible = true;
            
            // Position Reset Password field at top
            lblConfirmPassword.Location = new Point(0, 0);
            pnlConfirmPassword.Location = new Point(0, 25);
            
            // Bring to front to ensure visibility
            lblConfirmPassword.BringToFront();
            pnlConfirmPassword.BringToFront();
            
            // Ensure Confirm Password is positioned correctly below New Password
            lblConfirmPassword.Location = new Point(0, 20 + 25 + 40 + 25); // Below New Password field
            pnlConfirmPassword.Location = new Point(0, 20 + 25 + 40 + 25 + 25); // Below Confirm Password label

            // Footer panel with buttons
            Panel footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.FromArgb(248, 247, 242),
                Padding = new Padding(30, 20, 30, 20)
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
                Location = new Point(footerPanel.Width - 240, 20),
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
                this.DialogResult = DialogResult.Cancel;
            };

            // Reset Password button
            Button btnReset = new Button
            {
                Text = "Reset Password",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ThemeConstants.PrimaryMaroon,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Size = new Size(140, 40),
                Location = new Point(footerPanel.Width - 130, 20),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnReset.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnReset.Width - 1, btnReset.Height - 1), 6))
                {
                    e.Graphics.FillPath(new SolidBrush(btnReset.BackColor), path);
                }
                TextRenderer.DrawText(e.Graphics, btnReset.Text, btnReset.Font,
                    new Rectangle(0, 0, btnReset.Width, btnReset.Height),
                    btnReset.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnReset.Click += (s, e) =>
            {
                // Get actual text from password field (handling placeholder)
                string newPassword = txtConfirmPassword.GetActualText()?.Trim() ?? "";
                
                // If GetActualText returns placeholder, try direct Text property
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword == "Enter new password")
                {
                    newPassword = txtConfirmPassword.Text?.Trim() ?? "";
                }

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    MessageBox.Show("Please enter a new password.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtConfirmPassword.Focus();
                    return;
                }

                if (newPassword.Length < 6)
                {
                    MessageBox.Show("Password must be at least 6 characters long.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtConfirmPassword.Focus();
                    return;
                }

                NewPassword = newPassword;
                this.DialogResult = DialogResult.OK;
            };

            footerPanel.Controls.AddRange(new Control[] { btnCancel, btnReset });

            mainPanel.Controls.Add(headerPanel);
            mainPanel.Controls.Add(contentPanel);
            mainPanel.Controls.Add(footerPanel);

            this.Controls.Add(mainPanel);
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

