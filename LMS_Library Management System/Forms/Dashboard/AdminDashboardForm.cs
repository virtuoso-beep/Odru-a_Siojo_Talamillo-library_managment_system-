using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using LMS_Library_Management_System.Forms.Authentication;
using LMS_Library_Management_System.Helper;
using static LMS_Library_Management_System.Helper.PlaceholderTextHelper;
using LMS_Library_Management_System.Service;
using LMS_Library_Management_System.Models;
using LMS_Library_Management_System.Interfaces;

using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing.Printing;

namespace LMS_Library_Management_System.Forms.Dashboard
{
    public partial class AdminDashboardForm : Form
    {
        // Dynamic view panels

        private Panel pnlSearchView;
        private Panel pnlSearchResultsContainer;
        private TextBox txtSearchInput;
        private Button btnTriggerSearch;
        private Button btnSearchFilters;
        private Label lblSearchResultsCount;
        private bool isGridView = true;
        private ComboBox cmbSearchCategoryFilter;
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
        private CirculationService _circulationService;
        private MemberService _memberService;
        private UserRole _currentUserManagementRole = UserRole.Administrator;
        public AdminDashboardForm()
        {
            InitializeComponent();
            this.Text = "Library Management System - Administrator Dashboard";
            this.WindowState = FormWindowState.Maximized;
            this.Size = new Size(1400, 700);
            this.MinimumSize = new Size(1200, 600);
            _userManagementService = new Service.UserManagementService();
            _circulationService = new CirculationService();
            _memberService = new MemberService();
            
            // Add FormClosing event handler for confirmation dialog
            this.FormClosing += AdminDashboardForm_FormClosing;
            
            InitializeDashboard();
        }
        
        private void AdminDashboardForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Don't show confirmation if it's a sign out (handled by btnLogout_Click)
            // Check if the close is from sign out by checking if user is already cleared
            if (SignInForm.CurrentUser == null)
            {
                return; // Allow close if user is already cleared (sign out scenario)
            }
            
            // Show confirmation dialog when closing the form
            DialogResult result = MessageBox.Show(
                "Are you sure you want to close the application?",
                "Confirm Exit",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
            
            if (result == DialogResult.No)
            {
                e.Cancel = true; // Cancel the close operation
            }
            else
            {
                // User confirmed, close the entire application
                SignInForm.SetApplicationExiting(true);
                Application.Exit();
            }
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
            // Load statistics
            var stats = _circulationService.GetBorrowingStatistics();
            Panel cardCurrentlyBorrowed = CreateCatalogStatCard("📚", stats.CurrentlyBorrowed.ToString(), "Currently Borrowed", Color.FromArgb(33, 150, 243), new Point(0, 0), "CurrentlyBorrowed");
            Panel cardOverdue = CreateCatalogStatCard("⚠️", stats.Overdue.ToString(), "Overdue", Color.FromArgb(244, 67, 54), new Point(200, 0), "Overdue");
            Panel cardReturnedToday = CreateCatalogStatCard("✓", stats.ReturnedToday.ToString(), "Returned Today", Color.FromArgb(76, 175, 80), new Point(400, 0), "ReturnedToday");
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
            btnReturn.Click += (s, e) => ShowReturnBookDialog();
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
            
            // Add Actions column with buttons
            DataGridViewButtonColumn btnReturnColumn = new DataGridViewButtonColumn();
            btnReturnColumn.Name = "ReturnAction";
            btnReturnColumn.HeaderText = "Actions";
            btnReturnColumn.Text = "Return";
            btnReturnColumn.UseColumnTextForButtonValue = false;
            btnReturnColumn.Width = 150;
            dgvBorrowings.Columns.Add(btnReturnColumn);
            
            // Load borrowing data
            LoadBorrowingsDataAdmin(dgvBorrowings, "All");
            
            // Filter change event
            cmbStatusFilter.SelectedIndexChanged += (s, e) =>
            {
                string filter = cmbStatusFilter.SelectedItem.ToString().Replace(" Status", "");
                LoadBorrowingsDataAdmin(dgvBorrowings, filter);
            };
            
            // Search event
            txtSearchBorrowings.TextChanged += (s, e) =>
            {
                FilterBorrowingsDataAdmin(dgvBorrowings, txtSearchBorrowings.Text);
            };
            
            // Cell painting for status and fine
            dgvBorrowings.CellPainting += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    if (dgvBorrowings.Columns[e.ColumnIndex].Name == "Status")
                    {
                        e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);
                        
                        string status = e.Value?.ToString() ?? "";
                        Color badgeColor = Color.LightBlue;
                        Color textColor = Color.DarkBlue;
                        
                        if (status == "Active")
                        {
                            badgeColor = Color.FromArgb(220, 240, 255);
                            textColor = Color.FromArgb(50, 120, 200);
                        }
                        else if (status == "Overdue")
                        {
                            badgeColor = Color.FromArgb(255, 230, 230);
                            textColor = Color.FromArgb(200, 50, 50);
                        }
                        else if (status == "Returned")
                        {
                            badgeColor = Color.FromArgb(220, 255, 230);
                            textColor = Color.FromArgb(50, 150, 50);
                        }
                        
                        Rectangle badgeRect = new Rectangle(
                            e.CellBounds.X + 10,
                            e.CellBounds.Y + 8,
                            e.CellBounds.Width - 20,
                            e.CellBounds.Height - 16
                        );
                        
                        using (SolidBrush brush = new SolidBrush(badgeColor))
                        {
                            e.Graphics.FillRectangle(brush, badgeRect);
                        }
                        
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        using (SolidBrush textBrush = new SolidBrush(textColor))
                        {
                            e.Graphics.DrawString(status, new Font("Segoe UI", 8.5F, FontStyle.Bold), textBrush, badgeRect, sf);
                        }
                        
                        e.Handled = true;
                    }
                    else if (dgvBorrowings.Columns[e.ColumnIndex].Name == "Fine" && e.Value != null && e.Value.ToString() != "-")
                    {
                        e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);
                        
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(200, 50, 50)))
                        {
                            e.Graphics.DrawString(e.Value.ToString(), new Font("Segoe UI", 9F, FontStyle.Bold), textBrush, e.CellBounds, sf);
                        }
                        
                        e.Handled = true;
                    }
                }
            };
            
            // Cell click event for action buttons
            dgvBorrowings.CellContentClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    if (dgvBorrowings.Columns[e.ColumnIndex].Name == "ReturnAction")
                    {
                        int borrowingId = Convert.ToInt32(dgvBorrowings.Rows[e.RowIndex].Cells["BorrowingId"].Value);
                        string status = dgvBorrowings.Rows[e.RowIndex].Cells["Status"].Value.ToString();
                        
                        if (status == "Active" || status == "Overdue")
                        {
                            ShowReturnBookDialog();
                        }
                    }
                }
            };
            
            pnlMainContent.Controls.AddRange(new Control[] { titleLabel, subtitleLabel, btnCheckout, btnReturn, statsPanel, searchPanel, dgvBorrowings });
        }
        
        private void LoadBorrowingsDataAdmin(DataGridView dgv, string filter)
        {
            dgv.Rows.Clear();
            
            try
            {
                var allBorrowings = _circulationService.GetAllBorrowingsForDisplay();
                
                foreach (var borrowing in allBorrowings)
                {
                    // Apply filter
                    if (filter != "All" && borrowing.Status != filter)
                        continue;
                    
                    string accessionNumber = $"ACC-{borrowing.BorrowDate.Year}-{borrowing.BorrowingId.ToString().PadLeft(5, '0')}";
                    string borrowDate = borrowing.BorrowDate.ToString("MMM dd, yyyy");
                    string dueDate = borrowing.DueDate.ToString("MMM dd, yyyy");
                    string fine = borrowing.FineAmount > 0 ? $"${borrowing.FineAmount:F0}" : "-";
                    string status = borrowing.Status;
                    
                    if (borrowing.ReturnDate.HasValue)
                    {
                        status = "Returned";
                    }
                    else if (borrowing.IsOverdue)
                    {
                        status = "Overdue";
                    }
                    
                    int rowIndex = dgv.Rows.Add(
                        borrowing.BorrowingId,
                        accessionNumber,
                        borrowing.BookTitle,
                        borrowing.MemberName,
                        borrowDate,
                        dueDate,
                        status,
                        fine
                    );
                    
                    // Set button text based on status
                    if (status == "Returned")
                    {
                        dgv.Rows[rowIndex].Cells["ReturnAction"].Value = borrowing.ReturnDate.HasValue ? borrowing.ReturnDate.Value.ToString("MMM dd") : "Returned";
                        dgv.Rows[rowIndex].Cells["ReturnAction"].ReadOnly = true;
                    }
                    else
                    {
                        dgv.Rows[rowIndex].Cells["ReturnAction"].Value = "⏎ Return  🔄 Renew";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading borrowings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void FilterBorrowingsDataAdmin(DataGridView dgv, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    row.Visible = true;
                }
                return;
            }
            
            searchText = searchText.ToLower();
            foreach (DataGridViewRow row in dgv.Rows)
            {
                bool visible = false;
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.Value != null && cell.Value.ToString().ToLower().Contains(searchText))
                    {
                        visible = true;
                        break;
                    }
                }
                row.Visible = visible;
            }
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
            // Load statistics first
            var reservationStats = GetReservationStatistics();
            Panel cardPending = CreateCatalogStatCard("🕒", reservationStats.Pending.ToString(), "Pending", Color.FromArgb(255, 193, 7), new Point(0, 0), "ReservationPending");
            Panel cardReady = CreateCatalogStatCard("🔔", reservationStats.Ready.ToString(), "Ready for Pickup", Color.FromArgb(76, 175, 80), new Point(200, 0), "ReservationReady");
            Panel cardFulfilled = CreateCatalogStatCard("✓", reservationStats.Fulfilled.ToString(), "Fulfilled", Color.FromArgb(33, 150, 243), new Point(400, 0), "ReservationFulfilled");
            Panel cardExpired = CreateCatalogStatCard("✗", reservationStats.Expired.ToString(), "Expired", Color.FromArgb(158, 158, 158), new Point(600, 0), "ReservationExpired");
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
            
            // Load reservations data
            LoadReservationsData(dgvReservations);
            UpdateReservationStats(statsPanel);
            RefreshReservationStatistics();
            
            // Search functionality
            txtSearchReservations.TextChanged += (s, e) =>
            {
                FilterReservationsData(dgvReservations, txtSearchReservations.Text);
            };
            
            pnlMainContent.Controls.AddRange(new Control[] { titleLabel, subtitleLabel, btnNewReservation, statsPanel, searchPanel, dgvReservations });
        }
        
        private void UpdateReservationStats(Panel statsPanel)
        {
            try
            {
                EnsureReservationsTableExists();
                
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Ensure we're using the correct database
                    using (var useDbCmd = new MySqlCommand("USE LMS_DB", connection))
                    {
                        useDbCmd.ExecuteNonQuery();
                    }
                    
                    // Update expired reservations first
                    string updateExpiredQuery = @"
                        UPDATE Reservations 
                        SET Status = 'Expired' 
                        WHERE Status != 'Fulfilled' 
                        AND Status != 'Expired' 
                        AND Expires < NOW()";
                    
                    using (var updateCmd = new MySqlCommand(updateExpiredQuery, connection))
                    {
                        updateCmd.ExecuteNonQuery();
                    }
                    
                    // Update "Ready" status for reservations where book is now available
                    string updateReadyQuery = @"
                        UPDATE Reservations r
                        INNER JOIN Books b ON r.BookId = b.BookId
                        SET r.Status = 'Ready'
                        WHERE r.Status = 'Pending'
                        AND b.AvailableCopies > 0";
                    
                    using (var updateReadyCmd = new MySqlCommand(updateReadyQuery, connection))
                    {
                        updateReadyCmd.ExecuteNonQuery();
                    }
                    
                    // Get counts for each status
                    string statsQuery = @"
                        SELECT 
                            SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) AS Pending,
                            SUM(CASE WHEN Status = 'Ready' THEN 1 ELSE 0 END) AS Ready,
                            SUM(CASE WHEN Status = 'Fulfilled' THEN 1 ELSE 0 END) AS Fulfilled,
                            SUM(CASE WHEN Status = 'Expired' THEN 1 ELSE 0 END) AS Expired
                        FROM Reservations";
                    
                    using (var cmd = new MySqlCommand(statsQuery, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int pending = reader["Pending"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Pending"]);
                                int ready = reader["Ready"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Ready"]);
                                int fulfilled = reader["Fulfilled"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Fulfilled"]);
                                int expired = reader["Expired"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Expired"]);
                                
                                // Update stat cards using tags
                                foreach (Control control in statsPanel.Controls)
                                {
                                    if (control is Panel card)
                                    {
                                        foreach (Control ctrl in card.Controls)
                                        {
                                            if (ctrl is Label lbl && lbl.Font.Bold)
                                            {
                                                // Set tag if not set
                                                if (string.IsNullOrEmpty(lbl.Tag?.ToString()))
                                                {
                                                    if (lbl.Text.Contains("Pending") || card.Location.X == 0) lbl.Tag = "ReservationPending";
                                                    else if (lbl.Text.Contains("Ready") || card.Location.X == 200) lbl.Tag = "ReservationReady";
                                                    else if (lbl.Text.Contains("Fulfilled") || card.Location.X == 400) lbl.Tag = "ReservationFulfilled";
                                                    else if (lbl.Text.Contains("Expired") || card.Location.X == 600) lbl.Tag = "ReservationExpired";
                                                }
                                                
                                                // Update by tag
                                                string tag = lbl.Tag?.ToString();
                                                if (tag == "ReservationPending") lbl.Text = pending.ToString();
                                                else if (tag == "ReservationReady") lbl.Text = ready.ToString();
                                                else if (tag == "ReservationFulfilled") lbl.Text = fulfilled.ToString();
                                                else if (tag == "ReservationExpired") lbl.Text = expired.ToString();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating reservation stats: {ex.Message}");
            }
        }
        
        private void LoadReservationsData(DataGridView dgv)
        {
            dgv.Rows.Clear();
            
            try
            {
                EnsureReservationsTableExists();
                
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Ensure we're using the correct database
                    using (var useDbCmd = new MySqlCommand("USE LMS_DB", connection))
                    {
                        useDbCmd.ExecuteNonQuery();
                    }
                    
                    // Update expired reservations first
                    string updateExpiredQuery = @"
                        UPDATE Reservations 
                        SET Status = 'Expired' 
                        WHERE Status != 'Fulfilled' 
                        AND Status != 'Expired' 
                        AND Expires < NOW()";
                    
                    using (var updateCmd = new MySqlCommand(updateExpiredQuery, connection))
                    {
                        updateCmd.ExecuteNonQuery();
                    }
                    
                    // Update "Ready" status for reservations where book is now available
                    string updateReadyQuery = @"
                        UPDATE Reservations r
                        INNER JOIN Books b ON r.BookId = b.BookId
                        SET r.Status = 'Ready'
                        WHERE r.Status = 'Pending'
                        AND b.AvailableCopies > 0";
                    
                    using (var updateReadyCmd = new MySqlCommand(updateReadyQuery, connection))
                    {
                        updateReadyCmd.ExecuteNonQuery();
                    }
                    
                    string query = @"
                        SELECT 
                            r.ReservationId,
                            bk.Title AS BookTitle,
                            CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
                            r.ReservedOn AS ReservationDate,
                            CASE 
                                WHEN r.Status = 'Pending' AND bk.AvailableCopies > 0 THEN 'Ready'
                                WHEN r.Status != 'Fulfilled' AND r.Status != 'Expired' AND r.Expires < NOW() THEN 'Expired'
                                ELSE r.Status
                            END AS Status
                        FROM Reservations r
                        INNER JOIN Books bk ON r.BookId = bk.BookId
                        INNER JOIN Members m ON r.MemberId = m.MemberId
                        INNER JOIN Users u ON m.UserId = u.UserId
                        ORDER BY r.ReservedOn DESC";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string reservationDate = reader.GetDateTime("ReservationDate").ToString("MMM dd, yyyy");
                                string status = reader.GetString("Status");
                                
                                dgv.Rows.Add(
                                    reader.GetInt32("ReservationId"),
                                    reader.GetString("BookTitle"),
                                    reader.GetString("MemberName"),
                                    reservationDate,
                                    status
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading reservations: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void FilterReservationsData(DataGridView dgv, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                LoadReservationsData(dgv);
                return;
            }
            
            string lowerSearch = searchText.ToLower();
            foreach (DataGridViewRow row in dgv.Rows)
            {
                bool visible = false;
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.Value != null && cell.Value.ToString().ToLower().Contains(lowerSearch))
                    {
                        visible = true;
                        break;
                    }
                }
                row.Visible = visible;
            }
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
            // Load statistics first
            var finesStats = GetFinesStatistics();
            Panel cardPendingFines = CreateCatalogStatCard("⚠️", $"₱{finesStats.PendingFines:N2}", "Pending Fines", ThemeConstants.PrimaryMaroon, new Point(0, 0), "PendingFines");
            Panel cardCollected = CreateCatalogStatCard("✓", $"₱{finesStats.Collected:N2}", "Collected", Color.FromArgb(76, 175, 80), new Point(200, 0), "CollectedFines");
            Panel cardWaived = CreateCatalogStatCard("✗", $"₱{finesStats.Waived:N2}", "Waived", Color.FromArgb(33, 150, 243), new Point(400, 0), "WaivedFines");
            Panel cardPendingCases = CreateCatalogStatCard("₱", finesStats.PendingCases.ToString(), "Pending Cases", Color.FromArgb(255, 193, 7), new Point(600, 0), "PendingCases");
            statsPanel.Controls.AddRange(new Control[] { cardPendingFines, cardCollected, cardWaived, cardPendingCases });
            // Search panel with search bar and filter tabs
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
            
            // Search bar on the left
            Panel searchContainer = new Panel
            {
                Location = new Point(20, 15),
                Size = new Size(320, 40),
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.None
            };
            searchContainer.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, searchContainer.Width - 1, searchContainer.Height - 1), 8))
                {
                    e.Graphics.FillPath(new SolidBrush(Color.FromArgb(245, 245, 245)), path);
                }
            };
            
            Label searchIcon = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI", 12F),
                Location = new Point(10, 10),
                Size = new Size(25, 20),
                ForeColor = Color.FromArgb(150, 150, 150),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
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
            
            // Filter tabs on the right
            Panel filterPanel = new Panel
            {
                Location = new Point(searchPanel.Width - 400, 20),
                Size = new Size(380, 40),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            
            // Declare DataGridView first
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                RowHeadersVisible = false,
                ColumnHeadersHeight = 40,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                GridColor = Color.FromArgb(240, 240, 240)
            };
            dgvFines.RowTemplate.Height = 50;
            
            string[] filterOptions = { "All", "Pending", "Paid", "Waived" };
            Button[] filterButtons = new Button[4];
            string selectedFilter = "All";
            
            for (int i = 0; i < filterOptions.Length; i++)
            {
                filterButtons[i] = new Button
                {
                    Text = filterOptions[i],
                    Location = new Point(i * 90, 0),
                    Size = new Size(80, 35),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Tag = filterOptions[i]
                };
                filterButtons[i].FlatAppearance.BorderSize = 0;
                
                if (i == 0)
                {
                    filterButtons[i].BackColor = ThemeConstants.PrimaryMaroon;
                    filterButtons[i].ForeColor = Color.White;
                }
                else
                {
                    filterButtons[i].BackColor = Color.White;
                    filterButtons[i].ForeColor = Color.FromArgb(60, 60, 60);
                }
                
                filterButtons[i].Paint += (s, e) =>
                {
                    Button btn = s as Button;
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 6))
                    {
                        e.Graphics.FillPath(new SolidBrush(btn.BackColor), path);
                        TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, new Rectangle(0, 0, btn.Width, btn.Height), btn.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                };
                
                filterButtons[i].Click += (s, e) =>
                {
                    Button clickedBtn = s as Button;
                    selectedFilter = clickedBtn.Tag.ToString();
                    
                    foreach (var btn in filterButtons)
                    {
                        if (btn == clickedBtn)
                        {
                            btn.BackColor = ThemeConstants.PrimaryMaroon;
                            btn.ForeColor = Color.White;
                        }
                        else
                        {
                            btn.BackColor = Color.White;
                            btn.ForeColor = Color.FromArgb(60, 60, 60);
                        }
                        btn.Invalidate();
                    }
                    
                    LoadFinesData(dgvFines, selectedFilter, txtSearchFines.Text);
                };
                
                filterPanel.Controls.Add(filterButtons[i]);
            }
            
            // Configure columns - Full design: Member, Book/Reason, Type, Amount, Paid, Status, Date, Actions
            dgvFines.Columns.Add("FineId", "ID");
            dgvFines.Columns.Add("MemberName", "Member");
            dgvFines.Columns.Add("BookReason", "Book/Reason");
            dgvFines.Columns.Add("Type", "Type");
            dgvFines.Columns.Add("Amount", "Amount");
            dgvFines.Columns.Add("Paid", "Paid");
            dgvFines.Columns.Add("Status", "Status");
            dgvFines.Columns.Add("Date", "Date");
            dgvFines.Columns.Add("Actions", "Actions");
            
            dgvFines.Columns["FineId"].Visible = false;
            dgvFines.Columns["MemberName"].Width = 150;
            dgvFines.Columns["BookReason"].Width = 200;
            dgvFines.Columns["Type"].Width = 100;
            dgvFines.Columns["Amount"].Width = 100;
            dgvFines.Columns["Paid"].Width = 100;
            dgvFines.Columns["Status"].Width = 120;
            dgvFines.Columns["Date"].Width = 120;
            dgvFines.Columns["Actions"].Width = 200;
            
            // Custom cell painting for badges and styling
            dgvFines.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                
                if (e.ColumnIndex == dgvFines.Columns["Type"].Index || e.ColumnIndex == dgvFines.Columns["Status"].Index)
                {
                    e.PaintBackground(e.CellBounds, false);
                    
                    string cellValue = e.Value?.ToString() ?? "";
                    Color badgeColor = Color.White;
                    Color textColor = Color.Black;
                    
                    if (e.ColumnIndex == dgvFines.Columns["Type"].Index)
                    {
                        if (cellValue.Contains("Overdue"))
                        {
                            badgeColor = Color.FromArgb(255, 193, 7); // Yellow
                            textColor = Color.Black;
                        }
                        else if (cellValue.Contains("Lost"))
                        {
                            badgeColor = Color.FromArgb(255, 87, 34); // Light Red
                            textColor = Color.White;
                        }
                        else if (cellValue.Contains("Damaged"))
                        {
                            badgeColor = Color.FromArgb(255, 152, 0); // Orange
                            textColor = Color.White;
                        }
                        else
                        {
                            badgeColor = Color.FromArgb(158, 158, 158); // Gray
                            textColor = Color.White;
                        }
                    }
                    else if (e.ColumnIndex == dgvFines.Columns["Status"].Index)
                    {
                        if (cellValue.Contains("Paid"))
                        {
                            badgeColor = Color.FromArgb(76, 175, 80); // Green
                            textColor = Color.White;
                        }
                        else if (cellValue.Contains("Pending"))
                        {
                            badgeColor = Color.FromArgb(255, 152, 0); // Orange
                            textColor = Color.White;
                        }
                        else if (cellValue.Contains("Waived"))
                        {
                            badgeColor = Color.FromArgb(33, 150, 243); // Blue
                            textColor = Color.White;
                        }
                    }
                    
                    Rectangle badgeRect = new Rectangle(e.CellBounds.X + 5, e.CellBounds.Y + 10, e.CellBounds.Width - 10, 25);
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (GraphicsPath path = CreateRoundedRectangle(badgeRect, 12))
                    {
                        e.Graphics.FillPath(new SolidBrush(badgeColor), path);
                    }
                    
                    // Add icon for status
                    if (e.ColumnIndex == dgvFines.Columns["Status"].Index)
                    {
                        string icon = "";
                        if (cellValue.Contains("Paid")) icon = "✓ ";
                        else if (cellValue.Contains("Pending")) icon = "🕒 ";
                        
                        TextRenderer.DrawText(e.Graphics, icon + cellValue, dgvFines.Font, badgeRect, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                    else
                    {
                        TextRenderer.DrawText(e.Graphics, cellValue, dgvFines.Font, badgeRect, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                    
                    e.Handled = true;
                }
                else if (e.ColumnIndex == dgvFines.Columns["Paid"].Index)
                {
                    e.PaintBackground(e.CellBounds, false);
                    string cellValue = e.Value?.ToString() ?? "$0.00";
                    Color textColor = Color.FromArgb(76, 175, 80); // Green
                    TextRenderer.DrawText(e.Graphics, cellValue, new Font(dgvFines.Font, FontStyle.Regular), e.CellBounds, textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == dgvFines.Columns["Amount"].Index)
                {
                    e.PaintBackground(e.CellBounds, false);
                    string cellValue = e.Value?.ToString() ?? "$0.00";
                    TextRenderer.DrawText(e.Graphics, cellValue, dgvFines.Font, e.CellBounds, Color.Black, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == dgvFines.Columns["Actions"].Index)
                {
                    e.PaintBackground(e.CellBounds, false);
                    string status = dgvFines.Rows[e.RowIndex].Cells["Status"].Value?.ToString() ?? "";
                    string dateStr = dgvFines.Rows[e.RowIndex].Cells["Date"].Value?.ToString() ?? "";
                    
                    if (status.Contains("Paid"))
                    {
                        // Show "Paid [Date]" text
                        string paidText = $"Paid {dateStr}";
                        TextRenderer.DrawText(e.Graphics, paidText, dgvFines.Font, e.CellBounds, Color.FromArgb(120, 120, 120), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                    }
                    else
                    {
                        // Show Pay button and Waive link
                        Rectangle payBtnRect = new Rectangle(e.CellBounds.X + 5, e.CellBounds.Y + 10, 60, 25);
                        Rectangle waiveRect = new Rectangle(e.CellBounds.X + 75, e.CellBounds.Y + 15, 50, 20);
                        
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using (GraphicsPath path = CreateRoundedRectangle(payBtnRect, 6))
                        {
                            e.Graphics.FillPath(new SolidBrush(ThemeConstants.PrimaryMaroon), path);
                        }
                        TextRenderer.DrawText(e.Graphics, "💳 Pay", new Font(dgvFines.Font, FontStyle.Bold), payBtnRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        
                        TextRenderer.DrawText(e.Graphics, "Waive", dgvFines.Font, waiveRect, Color.FromArgb(33, 150, 243), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                    }
                    e.Handled = true;
                }
            };
            
            // Handle action buttons
            dgvFines.CellClick += (s, e) =>
            {
                if (e.ColumnIndex == dgvFines.Columns["Actions"].Index && e.RowIndex >= 0)
                {
                    string status = dgvFines.Rows[e.RowIndex].Cells["Status"].Value?.ToString() ?? "";
                    if (status.Contains("Pending"))
                    {
                        Rectangle cellRect = dgvFines.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                        Point clickPoint = dgvFines.PointToClient(Control.MousePosition);
                        
                        int relativeX = clickPoint.X - cellRect.X;
                        int relativeY = clickPoint.Y - cellRect.Y;
                        
                        Rectangle payBtnRect = new Rectangle(5, 10, 60, 25);
                        Rectangle waiveRect = new Rectangle(75, 15, 50, 20);
                        
                        Point relativePoint = new Point(relativeX, relativeY);
                        
                        if (payBtnRect.Contains(relativePoint))
                        {
                            int fineId = Convert.ToInt32(dgvFines.Rows[e.RowIndex].Cells["FineId"].Value);
                            string memberName = dgvFines.Rows[e.RowIndex].Cells["MemberName"].Value?.ToString() ?? "";
                            string bookReason = dgvFines.Rows[e.RowIndex].Cells["BookReason"].Value?.ToString() ?? "";
                            string amountStr = dgvFines.Rows[e.RowIndex].Cells["Amount"].Value?.ToString() ?? "₱0.00";
                            string paidStr = dgvFines.Rows[e.RowIndex].Cells["Paid"].Value?.ToString() ?? "₱0.00";
                            
                            decimal totalFine = decimal.Parse(amountStr.Replace("₱", "").Replace(",", ""), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
                            decimal alreadyPaid = decimal.Parse(paidStr.Replace("₱", "").Replace(",", ""), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
                            
                            using (var dialog = new RecordPaymentDialog(fineId, memberName, bookReason, totalFine, alreadyPaid))
                            {
                                if (dialog.ShowDialog(this) == DialogResult.OK && dialog.PaymentProcessed)
                                {
                                    // Refresh fines data and statistics
                                    LoadFinesData(dgvFines, selectedFilter, txtSearchFines.Text);
                                    RefreshFinesStatistics();
                                }
                            }
                        }
                        else if (waiveRect.Contains(relativePoint))
                        {
                            int fineId = Convert.ToInt32(dgvFines.Rows[e.RowIndex].Cells["FineId"].Value);
                            string memberName = dgvFines.Rows[e.RowIndex].Cells["MemberName"].Value?.ToString() ?? "";
                            string amountStr = dgvFines.Rows[e.RowIndex].Cells["Amount"].Value?.ToString() ?? "₱0.00";
                            
                            decimal amountToWaive = decimal.Parse(amountStr.Replace("₱", "").Replace(",", ""), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
                            
                            using (var dialog = new WaiveFineDialog(fineId, memberName, amountToWaive))
                            {
                                if (dialog.ShowDialog(this) == DialogResult.OK && dialog.FineWaived)
                                {
                                    // Refresh fines data and statistics
                                    LoadFinesData(dgvFines, selectedFilter, txtSearchFines.Text);
                                    RefreshFinesStatistics();
                                }
                            }
                        }
                    }
                }
            };
            
            txtSearchFines.TextChanged += (s, e) =>
            {
                LoadFinesData(dgvFines, selectedFilter, txtSearchFines.Text);
            };
            
            searchPanel.Controls.Add(searchContainer);
            searchPanel.Controls.Add(filterPanel);
            
            // Load fines data
            LoadFinesData(dgvFines, selectedFilter, "");
            
            // Set tags on cards and refresh statistics
            SetFinesCardTags(statsPanel);
            RefreshFinesStatistics();
            pnlMainContent.Controls.AddRange(new Control[] { titleLabel, subtitleLabel, btnAddFine, statsPanel, searchPanel, dgvFines });
        }

        /// <summary>
        /// Sets tags on fines statistics cards for easy updating
        /// </summary>
        private void SetFinesCardTags(Panel statsPanel)
        {
            foreach (Control card in statsPanel.Controls)
            {
                if (card is Panel cardPanel)
                {
                    foreach (Control ctrl in cardPanel.Controls)
                    {
                        if (ctrl is Label lbl && lbl.Font.Bold)
                        {
                            // Set tag based on card location
                            if (cardPanel.Location.X == 0) lbl.Tag = "PendingFines";
                            else if (cardPanel.Location.X == 200) lbl.Tag = "CollectedFines";
                            else if (cardPanel.Location.X == 400) lbl.Tag = "WaivedFines";
                            else if (cardPanel.Location.X == 600) lbl.Tag = "PendingCases";
                        }
                    }
                }
            }
        }
        
        private void LoadFinesData(DataGridView dgv, string filter, string searchText)
        {
            dgv.Rows.Clear();
            
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Ensure PaidAmount column exists before querying
                    EnsurePaidAmountColumnExists(connection);
                    
                    string query = @"
                        SELECT 
                            f.FineId,
                            COALESCE(CONCAT(u.FirstName, ' ', u.LastName), m.MemberNumber, 'Unknown Member') AS MemberName,
                            CASE 
                                WHEN bk.Title IS NOT NULL THEN bk.Title
                                WHEN f.Reason LIKE '% - %' THEN 
                                    SUBSTRING_INDEX(SUBSTRING_INDEX(f.Reason, ' - ', -1), ' (', 1)
                                WHEN f.Reason IS NOT NULL AND f.Reason != '' THEN f.Reason
                                ELSE 'N/A'
                            END AS BookReason,
                            CASE 
                                WHEN f.Reason LIKE '%Lost%' OR f.Reason LIKE '%lost%' THEN 'Lost'
                                WHEN f.Reason LIKE '%Damaged%' OR f.Reason LIKE '%damaged%' THEN 'Damaged'
                                WHEN f.Reason LIKE '%Overdue%' OR f.Reason LIKE '%overdue%' OR f.Reason IS NULL THEN 'Overdue'
                                ELSE 'Other'
                            END AS Type,
                            f.Amount,
                            COALESCE(f.PaidAmount, CASE WHEN f.Status = 'Paid' THEN f.Amount ELSE 0 END) AS PaidAmount,
                            CASE 
                                WHEN f.Status = 'Unpaid' THEN 'Pending'
                                ELSE f.Status
                            END AS Status,
                            COALESCE(f.PaidDate, br.DueDate, f.CreatedDate) AS FineDate,
                            f.Reason
                        FROM Fines f
                        INNER JOIN Members m ON f.MemberId = m.MemberId
                        LEFT JOIN Users u ON m.UserId = u.UserId
                        LEFT JOIN Borrowings br ON f.BorrowingId = br.BorrowingId
                        LEFT JOIN Books bk ON br.BookId = bk.BookId
                        WHERE 1=1";
                    
                    // Apply filter
                    if (!string.IsNullOrEmpty(filter) && filter != "All")
                    {
                        if (filter == "Pending")
                        {
                            query += " AND f.Status = 'Unpaid'";
                        }
                        else if (filter == "Paid")
                        {
                            query += " AND f.Status = 'Paid'";
                        }
                        else if (filter == "Waived")
                        {
                            query += " AND f.Status = 'Waived'";
                        }
                    }
                    // If filter is "All" or empty, no additional WHERE clause is added - shows all fines
                    
                    // Apply search filter - search by member name
                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        query += " AND (u.FirstName LIKE @Search OR u.LastName LIKE @Search OR CONCAT(u.FirstName, ' ', u.LastName) LIKE @Search OR m.MemberNumber LIKE @Search)";
                    }
                    
                    query += " ORDER BY f.CreatedDate DESC";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(searchText))
                        {
                            command.Parameters.AddWithValue("@Search", $"%{searchText}%");
                        }
                        
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                try
                                {
                                    int fineId = reader.GetInt32("FineId");
                                    string memberName = reader.GetString("MemberName");
                                    string bookReason = reader.GetString("BookReason");
                                    string type = reader.GetString("Type");
                                    decimal amount = reader.GetDecimal("Amount");
                                    decimal paidAmount = reader.GetDecimal("PaidAmount");
                                    string status = reader.GetString("Status");
                                    DateTime fineDate = reader.GetDateTime("FineDate");
                                    string reason = reader.IsDBNull(reader.GetOrdinal("Reason")) ? "" : reader.GetString("Reason");
                                    
                                    // Format display reason
                                    string displayReason = bookReason;
                                    if (!string.IsNullOrEmpty(reason) && reason != bookReason && !bookReason.Contains(reason))
                                    {
                                        displayReason = $"{bookReason}\n{reason}";
                                    }
                                    
                                    // Format date
                                    string dateStr = fineDate.ToString("MMM dd, yyyy");
                                    
                                    // Format amounts
                                    string amountStr = $"₱{amount:N2}";
                                    string paidStr = $"₱{paidAmount:N2}";
                                    
                                    // Add row with all columns: FineId, MemberName, BookReason, Type, Amount, Paid, Status, Date, Actions
                                    dgv.Rows.Add(fineId, memberName, displayReason, type, amountStr, paidStr, status, dateStr, "");
                                }
                                catch (Exception rowEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error reading fine row: {rowEx.Message}");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading fines: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void RefreshFinesView()
        {
            // Find the DataGridView and search textbox in the fines view and refresh it
            DataGridView finesDgv = null;
            TextBox searchTextBox = null;
            
            foreach (Control control in pnlMainContent.Controls)
            {
                if (control is DataGridView dgv)
                {
                    if (dgv.Columns.Contains("FineId") && dgv.Columns.Contains("MemberName"))
                    {
                        finesDgv = dgv;
                    }
                }
                else if (control is Panel searchPanel)
                {
                    // Look for search textbox in the search panel
                    foreach (Control subControl in searchPanel.Controls)
                    {
                        if (subControl is TextBox txt)
                        {
                            // Check if it's a search textbox by checking the Tag for PlaceholderData
                            if (txt.Tag is PlaceholderTextHelper.PlaceholderData placeholderData && 
                                placeholderData.PlaceholderText != null && 
                                placeholderData.PlaceholderText.Contains("Search"))
                            {
                                searchTextBox = txt;
                                break;
                            }
                        }
                    }
                }
            }
            
            if (finesDgv != null)
            {
                string currentSearch = "";
                string currentFilter = "All";
                
                if (searchTextBox != null)
                {
                    // Get actual text, excluding placeholder
                    if (searchTextBox.Tag is PlaceholderTextHelper.PlaceholderData data && 
                        searchTextBox.Text == data.PlaceholderText)
                    {
                        currentSearch = "";
                    }
                    else
                    {
                        currentSearch = searchTextBox.Text ?? "";
                    }
                }
                
                // Find the active filter button
                foreach (Control panel in pnlMainContent.Controls)
                {
                    if (panel is Panel searchPanel)
                    {
                        foreach (Control ctrl in searchPanel.Controls)
                        {
                            if (ctrl is Panel filterPanel)
                            {
                                foreach (Control btn in filterPanel.Controls)
                                {
                                    if (btn is Button filterBtn)
                                    {
                                        // Check if this button is active (has maroon background and white text)
                                        if (filterBtn.BackColor == ThemeConstants.PrimaryMaroon && filterBtn.ForeColor == Color.White)
                                        {
                                            currentFilter = filterBtn.Tag?.ToString() ?? "All";
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                LoadFinesData(finesDgv, currentFilter, currentSearch);
            }
            else
            {
                // If not found, reload the entire view
                ShowFinesView();
            }
        }

        private void EnsureBookCopiesTableExists()
        {
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Ensure we're using the correct database
                    using (var useDbCmd = new MySqlCommand("USE LMS_DB", connection))
                    {
                        useDbCmd.ExecuteNonQuery();
                    }
                    
                    string checkTableQuery = @"
                        SELECT COUNT(*) 
                        FROM information_schema.tables 
                        WHERE table_schema = 'LMS_DB' 
                        AND table_name = 'BookCopies'";
                    
                    using (var checkCmd = new MySqlCommand(checkTableQuery, connection))
                    {
                        int tableExists = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (tableExists == 0)
                        {
                            // Create BookCopies table
                            string createTableQuery = @"
                                CREATE TABLE BookCopies (
                                    CopyId INT PRIMARY KEY AUTO_INCREMENT,
                                    BookId INT NOT NULL,
                                    AccessionNumber VARCHAR(50) UNIQUE NOT NULL,
                                    Location VARCHAR(255) DEFAULT 'Main Library',
                                    `Condition` VARCHAR(50) DEFAULT 'Good',
                                    Status VARCHAR(50) DEFAULT 'Available',
                                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                                    LastUpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                                    FOREIGN KEY (BookId) REFERENCES Books(BookId) ON DELETE CASCADE,
                                    INDEX idx_BookId (BookId),
                                    INDEX idx_Status (Status),
                                    INDEX idx_Condition (`Condition`),
                                    INDEX idx_AccessionNumber (AccessionNumber)
                                )";
                            
                            using (var createCmd = new MySqlCommand(createTableQuery, connection))
                            {
                                createCmd.ExecuteNonQuery();
                                System.Diagnostics.Debug.WriteLine("BookCopies table created successfully");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("BookCopies table already exists");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error ensuring BookCopies table exists: {ex.Message}");
                MessageBox.Show($"Error creating BookCopies table: {ex.Message}\n\nPlease ensure the database connection is working properly.", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void InitializeBookCopiesFromBooks()
        {
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Ensure we're using the correct database
                    using (var useDbCmd = new MySqlCommand("USE LMS_DB", connection))
                    {
                        useDbCmd.ExecuteNonQuery();
                    }
                    
                    // Get all books and their copy counts
                    string booksQuery = @"
                        SELECT 
                            b.BookId,
                            b.TotalCopies,
                            b.CreatedDate,
                            COUNT(bc.CopyId) AS ExistingCopies
                        FROM Books b
                        LEFT JOIN BookCopies bc ON b.BookId = bc.BookId
                        GROUP BY b.BookId, b.TotalCopies, b.CreatedDate";
                    
                    using (var booksCmd = new MySqlCommand(booksQuery, connection))
                    {
                        using (var reader = booksCmd.ExecuteReader())
                        {
                            List<(int BookId, int TotalCopies, int ExistingCopies, DateTime CreatedDate)> booksToSync = new List<(int, int, int, DateTime)>();
                            
                            while (reader.Read())
                            {
                                int bookId = reader.GetInt32("BookId");
                                int totalCopies = reader.GetInt32("TotalCopies");
                                int existingCopies = reader.GetInt32("ExistingCopies");
                                DateTime createdDate = reader.GetDateTime("CreatedDate");
                                
                                if (existingCopies < totalCopies)
                                {
                                    booksToSync.Add((bookId, totalCopies, existingCopies, createdDate));
                                }
                            }
                            
                            // Create missing copies
                            foreach (var book in booksToSync)
                            {
                                int copiesToCreate = book.TotalCopies - book.ExistingCopies;
                                
                                for (int i = 1; i <= copiesToCreate; i++)
                                {
                                    string accessionNumber = $"ACC-{book.CreatedDate.Year}-{book.BookId:D5}-{book.ExistingCopies + i:D3}";
                                    
                                    string insertQuery = @"
                                        INSERT INTO BookCopies (BookId, AccessionNumber, Location, `Condition`, Status)
                                        VALUES (@BookId, @AccessionNumber, 'Main Library', 'Good', 'Available')";
                                    
                                    using (var insertCmd = new MySqlCommand(insertQuery, connection))
                                    {
                                        insertCmd.Parameters.AddWithValue("@BookId", book.BookId);
                                        insertCmd.Parameters.AddWithValue("@AccessionNumber", accessionNumber);
                                        insertCmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing BookCopies: {ex.Message}");
            }
        }

        private void ShowInventoryView()
        {
            // Ensure BookCopies table exists
            EnsureBookCopiesTableExists();
            
            // Initialize BookCopies if needed (sync with Books table)
            InitializeBookCopiesFromBooks();
            
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
            btnExportInventory.Click += (s, e) => GenerateInventoryReport();
            
            Panel statsPanel = new Panel
            {
                Location = new Point(30, 150),
                Size = new Size(pnlMainContent.Width - 60, 160),
                BackColor = Color.Transparent,
                Tag = "InventoryStatsPanel"
            };
            // Load statistics first
            var inventoryStats = GetInventoryStatistics();
            Panel cardTotalTitles = CreateCatalogStatCard("📚", inventoryStats.TotalTitles.ToString(), "Total Titles", ThemeConstants.PrimaryMaroon, new Point(0, 0), "TotalTitles");
            Panel cardTotalCopies = CreateCatalogStatCard("📖", inventoryStats.TotalCopies.ToString(), "Total Copies", Color.White, new Point(250, 0), "TotalCopies");
            Panel cardAvailable = CreateCatalogStatCard("✓", inventoryStats.Available.ToString(), "Available", Color.FromArgb(76, 175, 80), new Point(500, 0), "Available");
            Panel cardBorrowed = CreateCatalogStatCard("📗", inventoryStats.Borrowed.ToString(), "Borrowed", Color.FromArgb(33, 150, 243), new Point(0, 80), "Borrowed");
            Panel cardDamaged = CreateCatalogStatCard("⚠", inventoryStats.Damaged.ToString(), "Damaged", Color.FromArgb(255, 193, 7), new Point(250, 80), "Damaged");
            Panel cardLost = CreateCatalogStatCard("❌", inventoryStats.Lost.ToString(), "Lost", Color.FromArgb(244, 67, 54), new Point(500, 80), "Lost");
            statsPanel.Controls.AddRange(new Control[] { cardTotalTitles, cardTotalCopies, cardAvailable, cardBorrowed, cardDamaged, cardLost });
            
            // Collection by Category Section
            Panel categoryPanel = new Panel
            {
                Location = new Point(30, 330),
                Size = new Size(pnlMainContent.Width - 60, 120),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            
            categoryPanel.Paint += (s, e) =>
            {
                using (var shadowBrush = new SolidBrush(Color.FromArgb(15, 0, 0, 0)))
                {
                    e.Graphics.FillRectangle(shadowBrush, 3, 3, categoryPanel.Width - 3, categoryPanel.Height - 3);
                }
                using (var bgBrush = new SolidBrush(Color.White))
                {
                    e.Graphics.FillRectangle(bgBrush, 0, 0, categoryPanel.Width, categoryPanel.Height);
                }
                using (var borderPen = new Pen(Color.FromArgb(220, 220, 220), 1))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, categoryPanel.Width - 1, categoryPanel.Height - 1);
                }
            };
            
            Label lblCategoryTitle = new Label
            {
                Text = "📊 Collection by Category",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Location = new Point(20, 15),
                AutoSize = true
            };
            
            Label lblCategorySubtitle = new Label
            {
                Text = "Availability distribution across categories.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(20, 40),
                AutoSize = true
            };
            
            categoryPanel.Controls.Add(lblCategoryTitle);
            categoryPanel.Controls.Add(lblCategorySubtitle);
            
            // Load category data and create progress bars
            LoadCategoryDistribution(categoryPanel);
            
            var searchBarComponents = CreateConsistentSearchBar("🔍 Search by title, accession number, or location...", 920);
            Panel searchPanel = searchBarComponents.panel;
            TextBox txtSearchInventory = searchBarComponents.textBox;
            Button btnSearchInventory = searchBarComponents.button;
            searchPanel.Location = new Point(30, 470);
            
            // Status filter dropdown
            Panel filterPanel = new Panel
            {
                Location = new Point(searchPanel.Width - 150, 15),
                Size = new Size(130, 30),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            
            ComboBox cmbStatusFilter = new ComboBox
            {
                Location = new Point(0, 0),
                Size = new Size(130, 30),
                Font = new Font("Segoe UI", 9F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            cmbStatusFilter.Items.AddRange(new string[] { "All Status", "Available", "Borrowed", "Damaged", "Lost", "For Repair" });
            cmbStatusFilter.SelectedIndex = 0;
            filterPanel.Controls.Add(cmbStatusFilter);
            searchPanel.Controls.Add(filterPanel);
            
            // Container panel for DataGridView to enable full stack display
            Panel dgvContainer = new Panel
            {
                Location = new Point(30, 560),
                Size = new Size(pnlMainContent.Width - 60, pnlMainContent.Height - 590),
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Padding = new Padding(0)
            };
            
            DataGridView dgvInventory = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };
            
            dgvInventory.Columns.Add("CopyId", "ID");
            dgvInventory.Columns.Add("CopyNumber", "Copy ID");
            dgvInventory.Columns.Add("BookTitle", "Book Title");
            dgvInventory.Columns.Add("AccessionNumber", "Accession #");
            dgvInventory.Columns.Add("Location", "Location");
            dgvInventory.Columns.Add("Condition", "Condition");
            dgvInventory.Columns.Add("Status", "Status");
            dgvInventory.Columns.Add("Actions", "Actions");
            
            dgvInventory.Columns["CopyId"].Visible = false;
            dgvInventory.Columns["CopyNumber"].Width = 80;
            dgvInventory.Columns["BookTitle"].Width = 250;
            dgvInventory.Columns["BookTitle"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvInventory.Columns["AccessionNumber"].Width = 150;
            dgvInventory.Columns["Location"].Width = 180;
            dgvInventory.Columns["Condition"].Width = 120;
            dgvInventory.Columns["Status"].Width = 120;
            dgvInventory.Columns["Actions"].Width = 100;
            
            // Enable row height auto-sizing for multi-line content
            dgvInventory.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvInventory.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            
            // Status filter change handler
            string selectedStatusFilter = "All Status";
            cmbStatusFilter.SelectedIndexChanged += (s, e) =>
            {
                selectedStatusFilter = cmbStatusFilter.SelectedItem?.ToString() ?? "All Status";
                LoadInventoryData(dgvInventory, txtSearchInventory.Text, selectedStatusFilter);
            };
            
            // Load inventory data
            LoadInventoryData(dgvInventory, "", selectedStatusFilter);
            UpdateInventoryStats(statsPanel);
            
            // Search functionality
            txtSearchInventory.TextChanged += (s, e) =>
            {
                LoadInventoryData(dgvInventory, txtSearchInventory.Text, selectedStatusFilter);
            };
            
            // Add context menu for actions
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem markLost = new ToolStripMenuItem("Mark as Lost");
            ToolStripMenuItem markDamaged = new ToolStripMenuItem("Mark as Damaged");
            ToolStripMenuItem markRepair = new ToolStripMenuItem("Mark for Repair");
            ToolStripMenuItem updateLocation = new ToolStripMenuItem("Update Location");
            
            markLost.Click += (s, e) => MarkCopyStatus(dgvInventory, "Lost");
            markDamaged.Click += (s, e) => MarkCopyStatus(dgvInventory, "Damaged");
            markRepair.Click += (s, e) => MarkCopyStatus(dgvInventory, "For Repair");
            updateLocation.Click += (s, e) => UpdateCopyLocation(dgvInventory);
            
            contextMenu.Items.AddRange(new ToolStripItem[] { markLost, markDamaged, markRepair, updateLocation });
            dgvInventory.ContextMenuStrip = contextMenu;
            
            // Handle cell click for actions - Edit button (pencil icon)
            dgvInventory.CellClick += (s, e) =>
            {
                if (e.ColumnIndex == dgvInventory.Columns["Actions"].Index && e.RowIndex >= 0)
                {
                    // Get copy data from the row
                    int copyId = Convert.ToInt32(dgvInventory.Rows[e.RowIndex].Cells["CopyId"].Value);
                    string bookTitle = dgvInventory.Rows[e.RowIndex].Cells["BookTitle"].Value?.ToString() ?? "";
                    string copyNumberStr = dgvInventory.Rows[e.RowIndex].Cells["CopyNumber"].Value?.ToString() ?? "Copy #1";
                    string status = dgvInventory.Rows[e.RowIndex].Cells["Status"].Value?.ToString() ?? "Available";
                    string location = dgvInventory.Rows[e.RowIndex].Cells["Location"].Value?.ToString() ?? "";
                    
                    // Extract copy number
                    int copyNumber = 1;
                    if (copyNumberStr.Contains("#"))
                    {
                        string numStr = copyNumberStr.Replace("Copy #", "").Trim();
                        int.TryParse(numStr, out copyNumber);
                    }
                    
                    // Extract status (remove emoji if present)
                    string cleanStatus = status;
                    if (status.Contains("Available")) cleanStatus = "Available";
                    else if (status.Contains("Borrowed")) cleanStatus = "Borrowed";
                    else if (status.Contains("Damaged")) cleanStatus = "Damaged";
                    else if (status.Contains("Lost")) cleanStatus = "Lost";
                    else if (status.Contains("For Repair")) cleanStatus = "For Repair";
                    
                    // Extract location (remove emoji if present)
                    string cleanLocation = location.Replace("📍", "").Trim();
                    
                    // Get book ID from database
                    int bookId = 0;
                    try
                    {
                        using (var connection = Helper.MYSqlHelper.CreateConnection())
                        {
                            using (var cmd = new MySqlCommand("SELECT BookId FROM BookCopies WHERE CopyId = @CopyId", connection))
                            {
                                cmd.Parameters.AddWithValue("@CopyId", copyId);
                                object result = cmd.ExecuteScalar();
                                if (result != null)
                                {
                                    bookId = Convert.ToInt32(result);
                                }
                            }
                        }
                    }
                    catch { }
                    
                    // Extract book title (remove author if present)
                    string titleOnly = bookTitle;
                    if (bookTitle.Contains("\n"))
                    {
                        titleOnly = bookTitle.Split('\n')[0];
                    }
                    
                    // Show edit dialog
                    using (var dialog = new EditBookCopyDialog(copyId, bookId, titleOnly, copyNumber, cleanStatus, cleanLocation))
                    {
                        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.ChangesSaved)
                        {
                            // Refresh inventory data
                            LoadInventoryData(dgvInventory, txtSearchInventory.Text, selectedStatusFilter);
                            UpdateInventoryStats(statsPanel);
                        }
                    }
                }
            };
            
            // Add DataGridView to container panel
            dgvContainer.Controls.Add(dgvInventory);
            
            // Set tags on cards and refresh statistics
            SetInventoryCardTags(statsPanel);
            RefreshInventoryStatistics();
            pnlMainContent.Controls.AddRange(new Control[] { titleLabel, subtitleLabel, btnExportInventory, statsPanel, categoryPanel, searchPanel, dgvContainer });
        }

        /// <summary>
        /// Sets tags on inventory statistics cards for easy updating
        /// </summary>
        private void SetInventoryCardTags(Panel statsPanel)
        {
            foreach (Control card in statsPanel.Controls)
            {
                if (card is Panel cardPanel)
                {
                    foreach (Control ctrl in cardPanel.Controls)
                    {
                        if (ctrl is Label lbl && lbl.Font.Bold)
                        {
                            // Set tag based on card location
                            Point loc = cardPanel.Location;
                            if (loc.X == 0 && loc.Y == 0) lbl.Tag = "TotalTitles";
                            else if (loc.X == 250 && loc.Y == 0) lbl.Tag = "TotalCopies";
                            else if (loc.X == 500 && loc.Y == 0) lbl.Tag = "Available";
                            else if (loc.X == 0 && loc.Y == 80) lbl.Tag = "Borrowed";
                            else if (loc.X == 250 && loc.Y == 80) lbl.Tag = "Damaged";
                            else if (loc.X == 500 && loc.Y == 80) lbl.Tag = "Lost";
                        }
                    }
                }
            }
        }
        
        private void LoadCategoryDistribution(Panel categoryPanel)
        {
            try
            {
                // Ensure table exists first
                EnsureBookCopiesTableExists();
                
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Ensure we're using the correct database
                    using (var useDbCmd = new MySqlCommand("USE LMS_DB", connection))
                    {
                        useDbCmd.ExecuteNonQuery();
                    }
                    
                    string query = @"
                        SELECT 
                            b.Category,
                            COUNT(bc.CopyId) AS TotalCopies,
                            SUM(CASE WHEN bc.Status = 'Available' THEN 1 ELSE 0 END) AS AvailableCopies
                        FROM Books b
                        LEFT JOIN BookCopies bc ON b.BookId = bc.BookId
                        WHERE b.Category IS NOT NULL AND b.Category != ''
                        GROUP BY b.Category
                        ORDER BY TotalCopies DESC
                        LIMIT 10";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        int yPos = 70;
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string category = reader.GetString("Category");
                                int total = reader.GetInt32("TotalCopies");
                                int available = reader.GetInt32("AvailableCopies");
                                
                                if (total == 0) continue;
                                
                                Label lblCategory = new Label
                                {
                                    Text = category,
                                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                                    ForeColor = Color.FromArgb(60, 60, 60),
                                    Location = new Point(20, yPos),
                                    Size = new Size(100, 20),
                                    AutoSize = false
                                };
                                
                                Label lblRatio = new Label
                                {
                                    Text = $"{available}/{total} available",
                                    Font = new Font("Segoe UI", 8F),
                                    ForeColor = Color.FromArgb(120, 120, 120),
                                    Location = new Point(130, yPos),
                                    Size = new Size(100, 20),
                                    AutoSize = false
                                };
                                
                                Panel progressBar = new Panel
                                {
                                    Location = new Point(240, yPos + 2),
                                    Size = new Size(300, 16),
                                    BackColor = Color.FromArgb(240, 240, 240),
                                    BorderStyle = BorderStyle.None
                                };
                                
                                float percentage = total > 0 ? (float)available / total : 0;
                                int filledWidth = (int)(progressBar.Width * percentage);
                                
                                progressBar.Paint += (s, e) =>
                                {
                                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                                    using (var bgBrush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                                    {
                                        e.Graphics.FillRectangle(bgBrush, 0, 0, progressBar.Width, progressBar.Height);
                                    }
                                    using (var fillBrush = new SolidBrush(ThemeConstants.PrimaryMaroon))
                                    {
                                        e.Graphics.FillRectangle(fillBrush, 0, 0, filledWidth, progressBar.Height);
                                    }
                                };
                                
                                categoryPanel.Controls.Add(lblCategory);
                                categoryPanel.Controls.Add(lblRatio);
                                categoryPanel.Controls.Add(progressBar);
                                
                                yPos += 25;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading category distribution: {ex.Message}");
            }
        }
        
        private void LoadInventoryData(DataGridView dgv, string searchText, string statusFilter = "All Status")
        {
            dgv.Rows.Clear();
            
            try
            {
                // Ensure table exists first
                EnsureBookCopiesTableExists();
                
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    if (connection == null)
                    {
                        System.Diagnostics.Debug.WriteLine("ERROR: Database connection is null!");
                        MessageBox.Show("Cannot connect to database. Please check your database connection.", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        connection.Open();
                    }
                    System.Diagnostics.Debug.WriteLine($"Database connection state: {connection.State}");
                    
                    // Ensure we're using the correct database
                    using (var useDbCmd = new MySqlCommand("USE LMS_DB", connection))
                    {
                        useDbCmd.ExecuteNonQuery();
                    }
                    
                    // First, check if there's any data in BookCopies table
                    using (var countCmd = new MySqlCommand("SELECT COUNT(*) FROM BookCopies", connection))
                    {
                        int totalCopies = Convert.ToInt32(countCmd.ExecuteScalar());
                        System.Diagnostics.Debug.WriteLine($"Total BookCopies in database: {totalCopies}");
                        
                        if (totalCopies == 0)
                        {
                            System.Diagnostics.Debug.WriteLine("BookCopies table is empty. No inventory items to display.");
                            int messageRowIndex = dgv.Rows.Add(
                                -1, // CopyId
                                "", // CopyNumber
                                "No inventory items found.\nThe BookCopies table is empty.\nPlease add book copies to the system.", // BookTitle
                                "", // AccessionNumber
                                "", // Location
                                "", // Condition
                                "", // Status
                                ""  // Actions
                            );
                            dgv.Rows[messageRowIndex].ReadOnly = true;
                            dgv.Rows[messageRowIndex].DefaultCellStyle.ForeColor = Color.Gray;
                            dgv.Rows[messageRowIndex].Cells["BookTitle"].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                            return;
                        }
                    }
                    
                    // Check if there are any books in Books table
                    using (var booksCountCmd = new MySqlCommand("SELECT COUNT(*) FROM Books", connection))
                    {
                        int totalBooks = Convert.ToInt32(booksCountCmd.ExecuteScalar());
                        System.Diagnostics.Debug.WriteLine($"Total Books in database: {totalBooks}");
                    }
                    
                    string query = @"
                        SELECT 
                            bc.CopyId,
                            bc.AccessionNumber,
                            b.Title,
                            b.Author,
                            bc.Location,
                            bc.`Condition`,
                            bc.Status,
                            (SELECT COUNT(*) FROM BookCopies bc2 WHERE bc2.BookId = bc.BookId AND bc2.CopyId <= bc.CopyId) AS CopyNumber
                        FROM BookCopies bc
                        INNER JOIN Books b ON bc.BookId = b.BookId
                        WHERE 1=1";
                    
                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        query += " AND (b.Title LIKE @Search OR b.Author LIKE @Search OR bc.AccessionNumber LIKE @Search OR bc.Location LIKE @Search)";
                    }
                    
                    if (statusFilter != "All Status")
                    {
                        query += " AND bc.Status = @StatusFilter";
                    }
                    
                    query += " ORDER BY b.Title, bc.CopyId";
                    
                    System.Diagnostics.Debug.WriteLine($"Executing query: {query}");
                    System.Diagnostics.Debug.WriteLine($"Search text: '{searchText}', Status filter: '{statusFilter}'");
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(searchText))
                        {
                            command.Parameters.AddWithValue("@Search", $"%{searchText}%");
                        }
                        if (statusFilter != "All Status")
                        {
                            command.Parameters.AddWithValue("@StatusFilter", statusFilter);
                        }
                        
                        using (var reader = command.ExecuteReader())
                        {
                            int rowCount = 0;
                            int errorCount = 0;
                            
                            while (reader.Read())
                            {
                                try
                                {
                                    int copyId = reader.GetInt32("CopyId");
                                    int copyNumber = reader.GetInt32("CopyNumber");
                                    string condition = reader.IsDBNull(reader.GetOrdinal("Condition")) ? "Good" : reader.GetString("Condition");
                                    string status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Available" : reader.GetString("Status");
                                    string location = reader.IsDBNull(reader.GetOrdinal("Location")) ? "Main Library" : reader.GetString("Location");
                                    string title = reader.IsDBNull(reader.GetOrdinal("Title")) ? "Unknown" : reader.GetString("Title");
                                    string author = reader.IsDBNull(reader.GetOrdinal("Author")) ? "Unknown" : reader.GetString("Author");
                                    string accessionNumber = reader.IsDBNull(reader.GetOrdinal("AccessionNumber")) ? "N/A" : reader.GetString("AccessionNumber");
                                    
                                    System.Diagnostics.Debug.WriteLine($"Reading row {rowCount + 1}: CopyId={copyId}, Title={title}, Author={author}, Status={status}");
                                    
                                    // Format status with icon
                                    string statusDisplay = status;
                                    if (status == "Available")
                                    {
                                        statusDisplay = "🟢 Available";
                                    }
                                    else if (status == "Borrowed")
                                    {
                                        statusDisplay = "📗 Borrowed";
                                    }
                                    else if (status == "Damaged")
                                    {
                                        statusDisplay = "⚠️ Damaged";
                                    }
                                    else if (status == "Lost")
                                    {
                                        statusDisplay = "❌ Lost";
                                    }
                                    else if (status == "For Repair")
                                    {
                                        statusDisplay = "🔧 For Repair";
                                    }
                                    
                                    // Format book title with author below (like in the image)
                                    string bookTitleDisplay = $"{title}\n{author}";
                                    
                                    int rowIndex = dgv.Rows.Add(
                                        copyId,
                                        $"Copy #{copyNumber}",
                                        bookTitleDisplay,
                                        accessionNumber,
                                        $"📍 {location}",
                                        condition,
                                        statusDisplay,
                                        "✏️"
                                    );
                                    
                                    // Make the Book Title cell display multiple lines
                                    dgv.Rows[rowIndex].Cells["BookTitle"].Style.WrapMode = DataGridViewTriState.True;
                                    rowCount++;
                                }
                                catch (Exception rowEx)
                                {
                                    errorCount++;
                                    System.Diagnostics.Debug.WriteLine($"ERROR reading inventory row {rowCount + errorCount}: {rowEx.Message}");
                                    System.Diagnostics.Debug.WriteLine($"Stack trace: {rowEx.StackTrace}");
                                    // Continue to next row
                                }
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"Successfully loaded {rowCount} inventory items into DataGridView (with {errorCount} errors)");
                            
                            if (rowCount == 0 && errorCount == 0)
                            {
                                System.Diagnostics.Debug.WriteLine("Query returned no rows. This might be due to:");
                                System.Diagnostics.Debug.WriteLine("1. No matching records based on search/filter criteria");
                                System.Diagnostics.Debug.WriteLine("2. BookCopies records exist but don't have matching Books records (orphaned records)");
                                
                                // Check for orphaned BookCopies (copies without matching books)
                                using (var orphanCheckCmd = new MySqlCommand(
                                    "SELECT COUNT(*) FROM BookCopies bc LEFT JOIN Books b ON bc.BookId = b.BookId WHERE b.BookId IS NULL", 
                                    connection))
                                {
                                    int orphanedCount = Convert.ToInt32(orphanCheckCmd.ExecuteScalar());
                                    if (orphanedCount > 0)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"WARNING: Found {orphanedCount} orphaned BookCopies records (no matching Books)");
                                    }
                                }
                                
                                // Add a message row to inform the user
                                int messageRowIndex = dgv.Rows.Add(
                                    -1, // CopyId
                                    "", // CopyNumber
                                    "No inventory items found matching your criteria.\nTry adjusting your search or filter settings.", // BookTitle
                                    "", // AccessionNumber
                                    "", // Location
                                    "", // Condition
                                    "", // Status
                                    ""  // Actions
                                );
                                // Make the message row non-selectable and style it
                                dgv.Rows[messageRowIndex].ReadOnly = true;
                                dgv.Rows[messageRowIndex].DefaultCellStyle.ForeColor = Color.Gray;
                                dgv.Rows[messageRowIndex].Cells["BookTitle"].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                            }
                        }
                    }
                    
                    // Format condition and status columns
                    dgv.CellFormatting += (s, e) =>
                    {
                        if (e.ColumnIndex == dgv.Columns["Condition"].Index && e.Value != null)
                        {
                            string condition = e.Value.ToString();
                            if (condition == "Good")
                            {
                                e.CellStyle.ForeColor = Color.FromArgb(33, 150, 243);
                            }
                            else if (condition == "Damaged")
                            {
                                e.CellStyle.ForeColor = Color.FromArgb(255, 193, 7);
                            }
                            else if (condition == "Lost")
                            {
                                e.CellStyle.ForeColor = Color.FromArgb(244, 67, 54);
                            }
                        }
                        
                        if (e.ColumnIndex == dgv.Columns["Status"].Index && e.Value != null)
                        {
                            string status = e.Value.ToString();
                            if (status.Contains("Available"))
                            {
                                e.CellStyle.ForeColor = Color.FromArgb(76, 175, 80);
                            }
                            else if (status.Contains("Borrowed"))
                            {
                                e.CellStyle.ForeColor = Color.FromArgb(33, 150, 243);
                            }
                            else if (status.Contains("Lost"))
                            {
                                e.CellStyle.ForeColor = Color.FromArgb(244, 67, 54);
                            }
                            else if (status.Contains("Damaged") || status.Contains("Repair"))
                            {
                                e.CellStyle.ForeColor = Color.FromArgb(255, 193, 7);
                            }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading inventory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void UpdateInventoryStats(Panel statsPanel)
        {
            try
            {
                // Ensure table exists first
                EnsureBookCopiesTableExists();
                
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Ensure we're using the correct database
                    using (var useDbCmd = new MySqlCommand("USE LMS_DB", connection))
                    {
                        useDbCmd.ExecuteNonQuery();
                    }
                    
                    // Get total titles
                    string titlesQuery = "SELECT COUNT(DISTINCT BookId) FROM BookCopies";
                    int totalTitles = 0;
                    using (var cmd = new MySqlCommand(titlesQuery, connection))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null) totalTitles = Convert.ToInt32(result);
                    }
                    
                    // Get total copies
                    string copiesQuery = "SELECT COUNT(*) FROM BookCopies";
                    int totalCopies = 0;
                    using (var cmd = new MySqlCommand(copiesQuery, connection))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null) totalCopies = Convert.ToInt32(result);
                    }
                    
                    // Get available
                    string availableQuery = "SELECT COUNT(*) FROM BookCopies WHERE Status = 'Available'";
                    int available = 0;
                    using (var cmd = new MySqlCommand(availableQuery, connection))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null) available = Convert.ToInt32(result);
                    }
                    
                    // Get borrowed
                    string borrowedQuery = "SELECT COUNT(*) FROM BookCopies WHERE Status = 'Borrowed'";
                    int borrowed = 0;
                    using (var cmd = new MySqlCommand(borrowedQuery, connection))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null) borrowed = Convert.ToInt32(result);
                    }
                    
                    // Get damaged
                    string damagedQuery = "SELECT COUNT(*) FROM BookCopies WHERE `Condition` = 'Damaged' OR Status = 'Damaged'";
                    int damaged = 0;
                    using (var cmd = new MySqlCommand(damagedQuery, connection))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null) damaged = Convert.ToInt32(result);
                    }
                    
                    // Get lost
                    string lostQuery = "SELECT COUNT(*) FROM BookCopies WHERE Status = 'Lost'";
                    int lost = 0;
                    using (var cmd = new MySqlCommand(lostQuery, connection))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null) lost = Convert.ToInt32(result);
                    }
                    
                    // Update stat cards
                    foreach (Control control in statsPanel.Controls)
                    {
                        if (control is Panel card)
                        {
                            foreach (Control ctrl in card.Controls)
                            {
                                if (ctrl is Label lbl && lbl.Font.Bold)
                                {
                                    if (lbl.Text.Contains("Total Titles")) lbl.Text = totalTitles.ToString();
                                    else if (lbl.Text.Contains("Total Copies")) lbl.Text = totalCopies.ToString();
                                    else if (lbl.Text.Contains("Available")) lbl.Text = available.ToString();
                                    else if (lbl.Text.Contains("Borrowed")) lbl.Text = borrowed.ToString();
                                    else if (lbl.Text.Contains("Damaged")) lbl.Text = damaged.ToString();
                                    else if (lbl.Text.Contains("Lost")) lbl.Text = lost.ToString();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating inventory stats: {ex.Message}");
            }
        }
        
        private void MarkCopyStatus(DataGridView dgv, string status)
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a book copy to update.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            DataGridViewRow selectedRow = dgv.SelectedRows[0];
            int copyId = Convert.ToInt32(selectedRow.Cells["CopyId"].Value);
            string accessionNumber = selectedRow.Cells["AccessionNumber"].Value.ToString();
            string bookTitle = selectedRow.Cells["BookTitle"].Value.ToString();
            
            string condition = status;
            if (status == "Lost")
            {
                condition = "Lost";
            }
            else if (status == "Damaged")
            {
                condition = "Damaged";
            }
            else if (status == "For Repair")
            {
                condition = "Damaged";
            }
            
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    string updateQuery = @"
                        UPDATE BookCopies 
                        SET Status = @Status, 
                            `Condition` = @Condition,
                            LastUpdatedDate = NOW()
                        WHERE CopyId = @CopyId";
                    
                    using (var command = new MySqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Status", status);
                        command.Parameters.AddWithValue("@Condition", condition);
                        command.Parameters.AddWithValue("@CopyId", copyId);
                        command.ExecuteNonQuery();
                    }
                }
                
                MessageBox.Show($"Book copy {accessionNumber} ({bookTitle}) has been marked as {status}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Refresh the view
                ComboBox statusFilter = null;
                TextBox searchBox = null;
                foreach (Control ctrl in pnlMainContent.Controls)
                {
                    if (ctrl is Panel searchPanel)
                    {
                        foreach (Control subCtrl in searchPanel.Controls)
                        {
                            if (subCtrl is ComboBox cmb && cmb.Items.Contains("All Status"))
                            {
                                statusFilter = cmb;
                            }
                            if (subCtrl is TextBox txt && txt.Tag is PlaceholderTextHelper.PlaceholderData placeholderData && placeholderData.PlaceholderText != null && placeholderData.PlaceholderText.Contains("Search"))
                            {
                                searchBox = txt;
                            }
                        }
                    }
                }
                
                string currentSearch = searchBox?.Text ?? "";
                string currentFilter = statusFilter?.SelectedItem?.ToString() ?? "All Status";
                LoadInventoryData(dgv, currentSearch, currentFilter);
                
                Panel statsPanel = pnlMainContent.Controls.OfType<Panel>().FirstOrDefault(p => p.Tag?.ToString() == "InventoryStatsPanel");
                if (statsPanel != null)
                {
                    UpdateInventoryStats(statsPanel);
                }
                
                // Refresh category panel
                Panel catPanel = pnlMainContent.Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.Count > 0 && p.Controls[0] is Label && p.Controls[0].Text.Contains("Collection by Category"));
                if (catPanel != null)
                {
                    // Clear existing category controls (except title and subtitle)
                    var controlsToRemove = catPanel.Controls.Cast<Control>().Where(c => !(c is Label && (c.Text.Contains("Collection by Category") || c.Text.Contains("Availability distribution")))).ToList();
                    foreach (var ctrl in controlsToRemove)
                    {
                        catPanel.Controls.Remove(ctrl);
                    }
                    LoadCategoryDistribution(catPanel);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating copy status: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void UpdateCopyLocation(DataGridView dgv)
        {
            if (dgv.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a book copy to update.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            DataGridViewRow selectedRow = dgv.SelectedRows[0];
            int copyId = Convert.ToInt32(selectedRow.Cells["CopyId"].Value);
            string currentLocation = selectedRow.Cells["Location"].Value?.ToString() ?? "Main Library";
            string accessionNumber = selectedRow.Cells["AccessionNumber"].Value.ToString();
            
            // Show dialog to update location
            Form locationForm = new Form
            {
                Size = new Size(400, 200),
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(248, 247, 242),
                ShowInTaskbar = false
            };
            
            locationForm.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, locationForm.Width - 1, locationForm.Height - 1), 10))
                {
                    locationForm.Region = new Region(path);
                    using (Pen p = new Pen(Color.FromArgb(220, 220, 220), 1))
                    {
                        e.Graphics.DrawPath(p, path);
                    }
                }
            };
            
            Label lblTitle = new Label
            {
                Text = "Update Location",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(101, 67, 33),
                Location = new Point(30, 20),
                AutoSize = true
            };
            
            Label lblAccession = new Label
            {
                Text = $"Accession: {accessionNumber}",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(30, 50),
                AutoSize = true
            };
            
            Label lblLocation = new Label
            {
                Text = "Location:",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(30, 85),
                AutoSize = true
            };
            
            TextBox txtLocation = new TextBox
            {
                Location = new Point(30, 110),
                Size = new Size(340, 25),
                Font = new Font("Segoe UI", 10F),
                Text = currentLocation
            };
            
            Button btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(90, 35),
                Location = new Point(200, 150),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(80, 80, 80),
                Font = new Font("Segoe UI", 9.5F),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(220, 220, 220);
            btnCancel.Click += (s, e) => locationForm.Close();
            
            Button btnSave = new Button
            {
                Text = "Update",
                Size = new Size(90, 35),
                Location = new Point(300, 150),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(128, 0, 32),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtLocation.Text))
                {
                    MessageBox.Show("Please enter a location.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                try
                {
                    using (var connection = Helper.MYSqlHelper.CreateConnection())
                    {
                        string updateQuery = @"
                            UPDATE BookCopies 
                            SET Location = @Location,
                                LastUpdatedDate = NOW()
                            WHERE CopyId = @CopyId";
                        
                        using (var command = new MySqlCommand(updateQuery, connection))
                        {
                            command.Parameters.AddWithValue("@Location", txtLocation.Text.Trim());
                            command.Parameters.AddWithValue("@CopyId", copyId);
                            command.ExecuteNonQuery();
                        }
                    }
                    
                    MessageBox.Show("Location updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    locationForm.DialogResult = DialogResult.OK;
                    locationForm.Close();
                    
                    // Refresh the view
                    ComboBox statusFilter = null;
                    TextBox searchBox = null;
                    foreach (Control ctrl in pnlMainContent.Controls)
                    {
                        if (ctrl is Panel searchPanel)
                        {
                            foreach (Control subCtrl in searchPanel.Controls)
                            {
                                if (subCtrl is ComboBox cmb && cmb.Items.Contains("All Status"))
                                {
                                    statusFilter = cmb;
                                }
                                if (subCtrl is TextBox txt && txt.Tag is PlaceholderTextHelper.PlaceholderData placeholderData && placeholderData.PlaceholderText != null && placeholderData.PlaceholderText.Contains("Search"))
                                {
                                    searchBox = txt;
                                }
                            }
                        }
                    }
                    
                    string currentSearch = searchBox?.Text ?? "";
                    string currentFilter = statusFilter?.SelectedItem?.ToString() ?? "All Status";
                    LoadInventoryData(dgv, currentSearch, currentFilter);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating location: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            
            locationForm.Controls.AddRange(new Control[] { lblTitle, lblAccession, lblLocation, txtLocation, btnCancel, btnSave });
            locationForm.ShowDialog(this);
        }
        
        private void ShowStockVerificationDialog()
        {
            Form verifyForm = new Form
            {
                Size = new Size(600, 500),
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(248, 247, 242),
                ShowInTaskbar = false
            };
            
            verifyForm.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, verifyForm.Width - 1, verifyForm.Height - 1), 10))
                {
                    verifyForm.Region = new Region(path);
                    using (Pen p = new Pen(Color.FromArgb(220, 220, 220), 1))
                    {
                        e.Graphics.DrawPath(p, path);
                    }
                }
            };
            
            Label btnClose = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 14F),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(verifyForm.Width - 45, 15),
                Size = new Size(35, 35),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose.Click += (s, e) => verifyForm.Close();
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.FromArgb(80, 80, 80);
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.FromArgb(150, 150, 150);
            
            Label lblTitle = new Label
            {
                Text = "Stock Verification Report",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(101, 67, 33),
                Location = new Point(30, 20),
                AutoSize = true
            };
            
            DataGridView dgvVerification = new DataGridView
            {
                Location = new Point(30, 70),
                Size = new Size(540, 350),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            
            dgvVerification.Columns.Add("BookTitle", "Book Title");
            dgvVerification.Columns.Add("Expected", "Expected Copies");
            dgvVerification.Columns.Add("Actual", "Actual Copies");
            dgvVerification.Columns.Add("Difference", "Difference");
            dgvVerification.Columns["BookTitle"].Width = 250;
            dgvVerification.Columns["Expected"].Width = 120;
            dgvVerification.Columns["Actual"].Width = 120;
            dgvVerification.Columns["Difference"].Width = 120;
            
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    string query = @"
                        SELECT 
                            b.Title AS BookTitle,
                            b.TotalCopies AS Expected,
                            COUNT(bc.CopyId) AS Actual,
                            (b.TotalCopies - COUNT(bc.CopyId)) AS Difference
                        FROM Books b
                        LEFT JOIN BookCopies bc ON b.BookId = bc.BookId
                        GROUP BY b.BookId, b.Title, b.TotalCopies
                        HAVING Difference != 0 OR Actual = 0
                        ORDER BY ABS(Difference) DESC";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int difference = reader.GetInt32("Difference");
                                string diffStr = difference > 0 ? $"+{difference}" : difference.ToString();
                                
                                dgvVerification.Rows.Add(
                                    reader.GetString("BookTitle"),
                                    reader.GetInt32("Expected"),
                                    reader.GetInt32("Actual"),
                                    diffStr
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading verification data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            Button btnCloseDialog = new Button
            {
                Text = "Close",
                Size = new Size(100, 35),
                Location = new Point(470, 430),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(128, 0, 32),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F),
                Cursor = Cursors.Hand
            };
            btnCloseDialog.FlatAppearance.BorderSize = 0;
            btnCloseDialog.Click += (s, e) => verifyForm.Close();
            
            verifyForm.Controls.AddRange(new Control[] { btnClose, lblTitle, dgvVerification, btnCloseDialog });
            verifyForm.ShowDialog(this);
        }
        
        private void GenerateInventoryReport()
        {
            try
            {
                // Ensure table exists
                EnsureBookCopiesTableExists();
                
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Ensure we're using the correct database
                    using (var useDbCmd = new MySqlCommand("USE LMS_DB", connection))
                    {
                        useDbCmd.ExecuteNonQuery();
                    }
                    
                    // Show save dialog
                    SaveFileDialog saveDialog = new SaveFileDialog
                    {
                        Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                        FileName = $"InventoryReport_{DateTime.Now:yyyyMMdd_HHmmss}",
                        Title = "Export Inventory Report"
                    };
                    
                    if (saveDialog.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                    
                    string filePath = saveDialog.FileName;
                    string extension = Path.GetExtension(filePath).ToLower();
                    
                    if (extension == ".csv")
                    {
                        // Export as CSV
                        ExportInventoryToCSV(connection, filePath);
                    }
                    else
                    {
                        // Export as formatted text
                        ExportInventoryToText(connection, filePath);
                    }
                    
                    MessageBox.Show($"Inventory report exported successfully!\n\nSaved to: {filePath}", "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void ExportInventoryToCSV(MySqlConnection connection, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                // Write CSV header matching the Excel template format
                writer.WriteLine("Row Number,Title,Author,ISBN,Subtitle,Editor,Publisher,Publication Year,Edition,Category,Language,NumberOfPages,PhysicalDescription,Location,CallNumber,AccessionNo,BookType,TotalCopies");
                
                string query = @"
                    SELECT 
                        b.Title,
                        b.Author,
                        COALESCE(b.ISBN, '') AS ISBN,
                        '' AS Subtitle,
                        '' AS Editor,
                        COALESCE(b.Publisher, '') AS Publisher,
                        COALESCE(b.PublicationYear, 0) AS PublicationYear,
                        '' AS Edition,
                        COALESCE(b.Category, '') AS Category,
                        'English' AS Language,
                        '' AS NumberOfPages,
                        '' AS PhysicalDescription,
                        bc.Location,
                        '' AS CallNumber,
                        bc.AccessionNumber AS AccessionNo,
                        'Circulation' AS BookType,
                        b.TotalCopies
                    FROM BookCopies bc
                    INNER JOIN Books b ON bc.BookId = b.BookId
                    ORDER BY b.Title, bc.AccessionNumber";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        int rowNum = 1;
                        while (reader.Read())
                        {
                            string line = $"{rowNum}," +
                                         $"\"{EscapeCSV(reader.GetString("Title"))}\"," +
                                         $"\"{EscapeCSV(reader.GetString("Author"))}\"," +
                                         $"\"{EscapeCSV(reader.GetString("ISBN"))}\"," +
                                         $"\"{EscapeCSV(reader.GetString("Subtitle"))}\"," +
                                         $"\"{EscapeCSV(reader.GetString("Editor"))}\"," +
                                         $"\"{EscapeCSV(reader.GetString("Publisher"))}\"," +
                                         $"{reader.GetInt32("PublicationYear")}," +
                                         $"\"{EscapeCSV(reader.GetString("Edition"))}\"," +
                                         $"\"{EscapeCSV(reader.GetString("Category"))}\"," +
                                         $"\"{EscapeCSV(reader.GetString("Language"))}\"," +
                                         $"\"{EscapeCSV(reader.GetString("NumberOfPages"))}\"," +
                                         $"\"{EscapeCSV(reader.GetString("PhysicalDescription"))}\"," +
                                         $"\"{EscapeCSV(reader.GetString("Location"))}\"," +
                                         $"\"{EscapeCSV(reader.GetString("CallNumber"))}\"," +
                                         $"\"{EscapeCSV(reader.GetString("AccessionNo"))}\"," +
                                         $"\"{EscapeCSV(reader.GetString("BookType"))}\"," +
                                         $"{reader.GetInt32("TotalCopies")}";
                            writer.WriteLine(line);
                            rowNum++;
                        }
                    }
                }
            }
        }
        
        private void ExportInventoryToText(MySqlConnection connection, string filePath)
        {
            StringBuilder report = new StringBuilder();
            
            // Header
            report.AppendLine("=".PadRight(80, '='));
            report.AppendLine("LIBRARY MANAGEMENT SYSTEM - INVENTORY REPORT");
            report.AppendLine("=".PadRight(80, '='));
            report.AppendLine($"Generated: {DateTime.Now.ToString("MMMM dd, yyyy 'at' HH:mm:ss")}");
            report.AppendLine($"Generated By: Administrator");
            report.AppendLine();
            
            // Summary Section
            string summaryQuery = @"
                SELECT 
                    COUNT(DISTINCT BookId) AS TotalTitles,
                    COUNT(*) AS TotalCopies,
                    SUM(CASE WHEN Status = 'Available' THEN 1 ELSE 0 END) AS Available,
                    SUM(CASE WHEN Status = 'Borrowed' THEN 1 ELSE 0 END) AS Borrowed,
                    SUM(CASE WHEN `Condition` = 'Damaged' OR Status = 'Damaged' THEN 1 ELSE 0 END) AS Damaged,
                    SUM(CASE WHEN Status = 'Lost' THEN 1 ELSE 0 END) AS Lost
                FROM BookCopies";
            
            using (var cmd = new MySqlCommand(summaryQuery, connection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        report.AppendLine("SUMMARY STATISTICS");
                        report.AppendLine("-".PadRight(80, '-'));
                        report.AppendLine($"Total Book Titles:        {reader.GetInt32("TotalTitles"),10:N0}");
                        report.AppendLine($"Total Copies:             {reader.GetInt32("TotalCopies"),10:N0}");
                        report.AppendLine($"Available Copies:         {reader.GetInt32("Available"),10:N0}");
                        report.AppendLine($"Borrowed Copies:          {reader.GetInt32("Borrowed"),10:N0}");
                        report.AppendLine($"Damaged Copies:           {reader.GetInt32("Damaged"),10:N0}");
                        report.AppendLine($"Lost Copies:              {reader.GetInt32("Lost"),10:N0}");
                        report.AppendLine();
                    }
                }
            }
            
            // Category Distribution
            report.AppendLine("CATEGORY DISTRIBUTION");
            report.AppendLine("-".PadRight(80, '-'));
            
            string categoryQuery = @"
                SELECT 
                    b.Category,
                    COUNT(bc.CopyId) AS TotalCopies,
                    SUM(CASE WHEN bc.Status = 'Available' THEN 1 ELSE 0 END) AS AvailableCopies
                FROM Books b
                LEFT JOIN BookCopies bc ON b.BookId = bc.BookId
                WHERE b.Category IS NOT NULL AND b.Category != ''
                GROUP BY b.Category
                ORDER BY TotalCopies DESC";
            
            using (var cmd = new MySqlCommand(categoryQuery, connection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string category = reader.GetString("Category");
                        int total = reader.GetInt32("TotalCopies");
                        int available = reader.GetInt32("AvailableCopies");
                        int borrowed = total - available;
                        
                        report.AppendLine($"{category,-20} Total: {total,3} | Available: {available,3} | Borrowed: {borrowed,3}");
                    }
                }
            }
            report.AppendLine();
            
            // Detailed Inventory List
            report.AppendLine("DETAILED INVENTORY LIST");
            report.AppendLine("-".PadRight(80, '-'));
            report.AppendLine($"{"Accession #",-20} {"Book Title",-30} {"Location",-20} {"Condition",-12} {"Status",-12}");
            report.AppendLine("-".PadRight(80, '-'));
            
            string detailQuery = @"
                SELECT 
                    bc.AccessionNumber,
                    CONCAT(b.Title, ' by ', b.Author) AS BookTitle,
                    bc.Location,
                    bc.`Condition`,
                    bc.Status
                FROM BookCopies bc
                INNER JOIN Books b ON bc.BookId = b.BookId
                ORDER BY b.Title, bc.AccessionNumber";
            
            using (var cmd = new MySqlCommand(detailQuery, connection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string accession = reader.GetString("AccessionNumber");
                        string bookTitle = reader.GetString("BookTitle");
                        if (bookTitle.Length > 28) bookTitle = bookTitle.Substring(0, 25) + "...";
                        string location = reader.GetString("Location");
                        if (location.Length > 18) location = location.Substring(0, 15) + "...";
                        string condition = reader.GetString("Condition");
                        string status = reader.GetString("Status");
                        
                        report.AppendLine($"{accession,-20} {bookTitle,-30} {location,-20} {condition,-12} {status,-12}");
                    }
                }
            }
            
            report.AppendLine();
            report.AppendLine("-".PadRight(80, '-'));
            report.AppendLine($"End of Report - {DateTime.Now.ToString("MMMM dd, yyyy 'at' HH:mm:ss")}");
            report.AppendLine("=".PadRight(80, '='));
            
            // Write to file
            File.WriteAllText(filePath, report.ToString());
        }
        
        private string EscapeCSV(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            
            // Escape quotes by doubling them
            return value.Replace("\"", "\"\"");
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

            

            Panel pnlDate = new Panel { Size = new Size(160, 36), Location = new Point(pnlHeader.Width - 370, 5), BackColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Right };

            ComboBox cmbDate = new ComboBox { FlatStyle = FlatStyle.Flat, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), DropDownStyle = ComboBoxStyle.DropDownList };

            cmbDate.Items.Add("📅  Last 7 days");
            cmbDate.Items.Add("📅  Last 14 days");
            cmbDate.Items.Add("📅  Last 30 days");
            cmbDate.Items.Add("📅  Last 90 days");

            cmbDate.SelectedIndex = 0;
            
            // Handle time range change
            cmbDate.SelectedIndexChanged += (s, e) =>
            {
                // Refresh current report with new time range
                if (pnlReportsTabs != null)
                {
                    foreach (Control ctrl in pnlReportsTabs.Controls)
                    {
                        if (ctrl is Button btn && btn.Font.Bold)
                        {
                            string tabName = btn.Tag?.ToString() ?? btn.Text;
                            SwitchReportTab(tabName, btn);
                            break;
                        }
                    }
                }
            };

            pnlDate.Controls.Add(cmbDate);

            pnlDate.Paint += (s, e) => {

                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                 using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlDate.Width-1, pnlDate.Height-1), 6))

                 using(Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1)) e.Graphics.DrawPath(pen, path);

            };
            
            // Store time range selector for use in report methods
            pnlDate.Tag = cmbDate;
            _reportsTimeRangeCombo = cmbDate; // Store class-level reference

            // Print button functionality
            btnPrint.Click += (s, e) => PrintCurrentReport(cmbDate);
            
            // Export button functionality
            btnExport.Click += (s, e) => ExportCurrentReport(cmbDate);

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



        private ComboBox _reportsTimeRangeCombo; // Store reference to time range combobox
        
        private void ShowReportsCirculation()
        {
            // Get time range
            int days = 7; // Default
            if (_reportsTimeRangeCombo != null)
            {
                days = GetDaysFromTimeRange(_reportsTimeRangeCombo);
            }
            DateTime startDate = DateTime.Now.AddDays(-days);
            
            // Load real statistics from database
            var circulationService = new Service.CirculationService();
            var stats = circulationService.GetBorrowingStatistics();
            
            // Get additional statistics from database
            int totalCollection = 0;
            int activeMembers = 0;
            int totalTransactions = 0;
            decimal finesCollected = 0;
            
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Total Collection
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM Books", connection))
                    {
                        totalCollection = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    
                    // Active Members
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM Members WHERE Status = 1", connection))
                    {
                        activeMembers = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    
                    // Total Transactions (within time range)
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM Borrowings WHERE BorrowDate >= @StartDate", connection))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", startDate);
                        totalTransactions = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    
                    // Fines Collected (within time range)
                    using (var cmd = new MySqlCommand("SELECT COALESCE(SUM(FineAmount), 0) FROM Borrowings WHERE FineAmount > 0 AND ReturnDate IS NOT NULL AND BorrowDate >= @StartDate", connection))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", startDate);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            finesCollected = Convert.ToDecimal(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading circulation statistics: {ex.Message}");
            }

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
            
            tlpKPI.Controls.Add(CreateReportStatCard("Total Collection", totalCollection.ToString("N0"), "📖", Color.White, Color.Black, false), 0, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Active Members", activeMembers.ToString("N0"), "👥", Color.White, Color.Black, false), 1, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Transactions", totalTransactions.ToString("N0"), "↗", Color.White, Color.Black, false), 2, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Fines Collected", $"${finesCollected:N2}", "💲", Color.White, Color.Black, false), 3, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Overdue", stats.Overdue.ToString("N0"), "📅", Color.White, Color.Black, false), 4, 0);
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
            LoadDailyCirculationChart(pnlChart);
            tlpCharts.Controls.Add(pnlChart, 0, 0);

            Panel pnlTopBooks = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 0, 0) };
            LoadMostBorrowedBooksChart(pnlTopBooks);
            tlpCharts.Controls.Add(pnlTopBooks, 1, 0);
            flpReportsContent.Controls.Add(tlpCharts);

            // 3. Overdue Grid - Full Stack
            Panel pnlGridContainer = new Panel 
            { 
                Height = 400, // Initial height, will be adjusted by anchor
                Width = flpReportsContent.ClientSize.Width - 10, 
                BackColor = Color.White, 
                Margin = new Padding(0, 0, 0, 30),
                MinimumSize = new Size(0, 300)
            };
            
            // Make the grid container fill remaining space when parent resizes
            flpReportsContent.Parent.Resize += (s, e) =>
            {
                if (pnlGridContainer != null && flpReportsContent != null)
                {
                    int availableHeight = flpReportsContent.Parent.Height - 600;
                    if (availableHeight > 300)
                    {
                        pnlGridContainer.Height = availableHeight;
                    }
                }
            };
            
            Label lblGridTitle = new Label 
            { 
                Text = "Overdue Books", 
                Font = new Font("Georgia", 14, FontStyle.Bold), 
                Location = new Point(20, 20), 
                AutoSize = true 
            };
            pnlGridContainer.Controls.Add(lblGridTitle);
            
            // Container for DataGridView to enable full stack
            Panel dgvContainer = new Panel
            {
                Location = new Point(20, 60),
                Size = new Size(pnlGridContainer.Width - 40, pnlGridContainer.Height - 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };
            
            DataGridView dgv = CreateReportGrid();
            dgv.Dock = DockStyle.Fill;
            dgv.Columns.Add("Book", "Book");
            dgv.Columns.Add("Member", "Member");
            dgv.Columns.Add("BorrowDate", "Borrow Date");
            dgv.Columns.Add("DueDate", "Due Date");
            dgv.Columns.Add("DaysOverdue", "Days Overdue");
            dgv.Columns.Add("Status", "Status");
            
            // Load overdue books from database
            LoadOverdueBooksData(dgv);
            
            dgvContainer.Controls.Add(dgv);
            pnlGridContainer.Controls.Add(dgvContainer);
            flpReportsContent.Controls.Add(pnlGridContainer);
        }
        
        private void LoadOverdueBooksData(DataGridView dgv)
        {
            dgv.Rows.Clear();
            
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    string query = @"
                        SELECT 
                            b.BorrowingId,
                            CONCAT(bk.Title, ' by ', bk.Author) AS BookTitle,
                            CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
                            b.BorrowDate,
                            b.DueDate,
                            DATEDIFF(NOW(), b.DueDate) AS DaysOverdue
                        FROM Borrowings b
                        INNER JOIN Books bk ON b.BookId = bk.BookId
                        INNER JOIN Members m ON b.MemberId = m.MemberId
                        INNER JOIN Users u ON m.UserId = u.UserId
                        WHERE b.ReturnDate IS NULL 
                        AND b.DueDate < NOW()
                        ORDER BY b.DueDate ASC";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string bookTitle = reader.IsDBNull(reader.GetOrdinal("BookTitle")) ? "Unknown" : reader.GetString("BookTitle");
                                string memberName = reader.IsDBNull(reader.GetOrdinal("MemberName")) ? "Unknown" : reader.GetString("MemberName");
                                DateTime borrowDate = reader.GetDateTime("BorrowDate");
                                DateTime dueDate = reader.GetDateTime("DueDate");
                                int daysOverdue = reader.GetInt32("DaysOverdue");
                                
                                dgv.Rows.Add(
                                    bookTitle,
                                    memberName,
                                    borrowDate.ToString("MMM dd, yyyy"),
                                    dueDate.ToString("MMM dd, yyyy"),
                                    daysOverdue.ToString(),
                                    $"Overdue ({daysOverdue} days)"
                                );
                            }
                        }
                    }
                }
                
                if (dgv.Rows.Count == 0)
                {
                    dgv.Rows.Add("No overdue books found", "", "", "", "", "");
                    dgv.Rows[0].DefaultCellStyle.ForeColor = Color.Gray;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading overdue books: {ex.Message}");
                dgv.Rows.Add("Error loading data", ex.Message, "", "", "", "");
            }
        }
        
        private void LoadDailyCirculationChart(Panel panel)
        {
            try
            {
                // Get time range from dropdown
                int days = 7; // Default
                if (_reportsTimeRangeCombo != null)
                {
                    days = GetDaysFromTimeRange(_reportsTimeRangeCombo);
                }
                
                Dictionary<string, int> borrowData = new Dictionary<string, int>();
                Dictionary<string, int> returnData = new Dictionary<string, int>();
                
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Get borrowings for selected time range
                    string borrowQuery = $@"
                        SELECT 
                            DATE(BorrowDate) AS BorrowDate,
                            COUNT(*) AS BorrowCount
                        FROM Borrowings
                        WHERE BorrowDate >= DATE_SUB(NOW(), INTERVAL {days} DAY)
                        GROUP BY DATE(BorrowDate)
                        ORDER BY BorrowDate ASC";
                    
                    using (var command = new MySqlCommand(borrowQuery, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime date = reader.GetDateTime("BorrowDate");
                                int count = reader.GetInt32("BorrowCount");
                                borrowData[date.ToString("MMM dd")] = count;
                            }
                        }
                    }
                    
                    // Get returns for selected time range
                    string returnQuery = $@"
                        SELECT 
                            DATE(ReturnDate) AS ReturnDate,
                            COUNT(*) AS ReturnCount
                        FROM Borrowings
                        WHERE ReturnDate >= DATE_SUB(NOW(), INTERVAL {days} DAY)
                        AND ReturnDate IS NOT NULL
                        GROUP BY DATE(ReturnDate)
                        ORDER BY ReturnDate ASC";
                    
                    using (var command = new MySqlCommand(returnQuery, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime date = reader.GetDateTime("ReturnDate");
                                int count = reader.GetInt32("ReturnCount");
                                returnData[date.ToString("MMM dd")] = count;
                            }
                        }
                    }
                }
                
                SetupChartWithData(panel, $"Daily Circulation Trends (Last {days} Days)", borrowData, returnData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading daily circulation chart: {ex.Message}");
                SetupChart(panel, "Daily Circulation Trends");
            }
        }
        
        private void SetupChartWithData(Panel pnl, string title, Dictionary<string, int> borrowData, Dictionary<string, int> returnData)
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
            
            foreach (var item in borrowData)
            {
                s1.Points.AddXY(item.Key, item.Value);
            }

            Series s2 = new Series
            {
                Name = "Returns",
                Color = Color.Green,
                ChartType = SeriesChartType.Spline,
                BorderWidth = 3
            };
            
            foreach (var item in returnData)
            {
                s2.Points.AddXY(item.Key, item.Value);
            }

            chart.Series.Add(s1);
            chart.Series.Add(s2);
            chart.Legends.Add(new Legend { Docking = Docking.Bottom, Alignment = StringAlignment.Center });
            pnl.Controls.Add(chart);
        }
        
        private void LoadMostBorrowedBooksChart(Panel panel)
        {
            try
            {
                Dictionary<string, int> bookData = new Dictionary<string, int>();
                
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    string query = @"
                        SELECT 
                            CONCAT(bk.Title, ' by ', bk.Author) AS BookTitle,
                            COUNT(*) AS BorrowCount
                        FROM Borrowings b
                        INNER JOIN Books bk ON b.BookId = bk.BookId
                        GROUP BY b.BookId, bk.Title, bk.Author
                        ORDER BY BorrowCount DESC
                        LIMIT 5";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string bookTitle = reader.GetString("BookTitle");
                                int count = reader.GetInt32("BorrowCount");
                                // Truncate long titles
                                if (bookTitle.Length > 30)
                                {
                                    bookTitle = bookTitle.Substring(0, 27) + "...";
                                }
                                bookData[bookTitle] = count;
                            }
                        }
                    }
                }
                
                if (bookData.Count > 0)
                {
                    SetupDistributionChart(panel, "Most Borrowed Books", bookData);
                }
                else
                {
                    SetupDistributionChart(panel, "Most Borrowed Books", new Dictionary<string, int>());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading most borrowed books chart: {ex.Message}");
                SetupDistributionChart(panel, "Most Borrowed Books", new Dictionary<string, int>());
            }
        }



        private void ShowReportsMembers()
        {
            // Get time range
            int days = 7; // Default
            if (_reportsTimeRangeCombo != null)
            {
                days = GetDaysFromTimeRange(_reportsTimeRangeCombo);
            }
            DateTime startDate = DateTime.Now.AddDays(-days);
            
            // Load statistics from database
            int totalMembers = 0;
            int newMembers = 0;
            int activeMembers = 0;
            int suspendedMembers = 0;
            Dictionary<string, int> memberTypeData = new Dictionary<string, int>();
            Dictionary<string, int> memberStatusData = new Dictionary<string, int>();
            
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Total Members
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM Members", connection))
                    {
                        totalMembers = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    
                    // New Members (within time range)
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM Members WHERE RegistrationDate >= @StartDate", connection))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", startDate);
                        newMembers = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    
                    // Active Members
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM Members WHERE Status = 1", connection))
                    {
                        activeMembers = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    
                    // Suspended Members
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM Members WHERE Status = 0", connection))
                    {
                        suspendedMembers = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    
                    // Member Distribution by Type
                    string typeQuery = @"
                        SELECT MemberType, COUNT(*) AS Count
                        FROM Members
                        GROUP BY MemberType";
                    using (var cmd = new MySqlCommand(typeQuery, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string memberType = reader.GetString("MemberType");
                                int count = reader.GetInt32("Count");
                                memberTypeData[memberType] = count;
                            }
                        }
                    }
                    
                    // Membership Status
                    string statusQuery = @"
                        SELECT 
                            CASE 
                                WHEN Status = 1 THEN 'Active'
                                WHEN Status = 0 THEN 'Suspended'
                                ELSE 'Inactive'
                            END AS Status,
                            COUNT(*) AS Count
                        FROM Members
                        GROUP BY Status";
                    using (var cmd = new MySqlCommand(statusQuery, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string status = reader.GetString("Status");
                                int count = reader.GetInt32("Count");
                                memberStatusData[status] = count;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading members statistics: {ex.Message}");
            }

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
            
            tlpKPI.Controls.Add(CreateReportStatCard("Total Members", totalMembers.ToString("N0"), "👥", Color.White, Color.Black, false), 0, 0);
            tlpKPI.Controls.Add(CreateReportStatCard($"New (Last {days} days)", newMembers.ToString("N0"), "➕", Color.White, Color.Green, false), 1, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Active Users", activeMembers.ToString("N0"), "⚡", Color.White, Color.Blue, false), 2, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Suspended", suspendedMembers.ToString("N0"), "⛔", Color.White, Color.Red, false), 3, 0);
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
            SetupDistributionChart(pnlTypeChart, "Member Distribution by Type", memberTypeData);
            tlpCharts.Controls.Add(pnlTypeChart, 0, 0);

            Panel pnlStatusChart = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 0, 0) };
            SetupDistributionChart(pnlStatusChart, "Membership Status", memberStatusData);
            tlpCharts.Controls.Add(pnlStatusChart, 1, 0);
            flpReportsContent.Controls.Add(tlpCharts);

            // 3. Grid - Full Stack
            Panel pnlGridContainer = new Panel 
            { 
                Height = 400,
                Width = flpReportsContent.ClientSize.Width - 10, 
                BackColor = Color.White, 
                Margin = new Padding(0, 0, 0, 30),
                MinimumSize = new Size(0, 300)
            };
            
            // Make the grid container fill remaining space when parent resizes
            flpReportsContent.Parent.Resize += (s, e) =>
            {
                if (pnlGridContainer != null && flpReportsContent != null)
                {
                    int availableHeight = flpReportsContent.Parent.Height - 600;
                    if (availableHeight > 300)
                    {
                        pnlGridContainer.Height = availableHeight;
                    }
                }
            };
            
            Label lblGridTitle = new Label { Text = "Recent Member Activity", Font = new Font("Georgia", 14, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true };
            pnlGridContainer.Controls.Add(lblGridTitle);
            
            // Container for DataGridView to enable full stack
            Panel dgvContainer = new Panel
            {
                Location = new Point(20, 60),
                Size = new Size(pnlGridContainer.Width - 40, pnlGridContainer.Height - 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };
            
            DataGridView dgv = CreateReportGrid();
            dgv.Dock = DockStyle.Fill;
            dgv.Columns.Add("MemberId", "Member ID");
            dgv.Columns.Add("Name", "Name");
            dgv.Columns.Add("Activity", "Activity");
            dgv.Columns.Add("Date", "Date");
            
            // Load recent member activity from database
            LoadRecentMemberActivity(dgv, startDate);
            
            dgvContainer.Controls.Add(dgv);
            pnlGridContainer.Controls.Add(dgvContainer);
            flpReportsContent.Controls.Add(pnlGridContainer);
        }
        
        private void LoadRecentMemberActivity(DataGridView dgv, DateTime startDate)
        {
            dgv.Rows.Clear();
            
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    string query = @"
                        SELECT 
                            m.MemberNumber AS MemberId,
                            CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
                            'Borrowed' AS ActivityType,
                            bk.Title AS BookTitle,
                            b.BorrowDate AS ActivityDate
                        FROM Borrowings b
                        INNER JOIN Members m ON b.MemberId = m.MemberId
                        INNER JOIN Users u ON m.UserId = u.UserId
                        INNER JOIN Books bk ON b.BookId = bk.BookId
                        WHERE b.BorrowDate >= @StartDate
                        
                        UNION ALL
                        
                        SELECT 
                            m.MemberNumber AS MemberId,
                            CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
                            'Returned' AS ActivityType,
                            bk.Title AS BookTitle,
                            b.ReturnDate AS ActivityDate
                        FROM Borrowings b
                        INNER JOIN Members m ON b.MemberId = m.MemberId
                        INNER JOIN Users u ON m.UserId = u.UserId
                        INNER JOIN Books bk ON b.BookId = bk.BookId
                        WHERE b.ReturnDate >= @StartDate AND b.ReturnDate IS NOT NULL
                        
                        ORDER BY ActivityDate DESC
                        LIMIT 50";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@StartDate", startDate);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string memberId = reader.GetString("MemberId");
                                string memberName = reader.GetString("MemberName");
                                string activityType = reader.GetString("ActivityType");
                                string bookTitle = reader.GetString("BookTitle");
                                DateTime activityDate = reader.GetDateTime("ActivityDate");
                                
                                string activity = $"{activityType} '{bookTitle}'";
                                string timeAgo = GetTimeAgo(activityDate);
                                
                                dgv.Rows.Add(memberId, memberName, activity, timeAgo);
                            }
                        }
                    }
                }
                
                if (dgv.Rows.Count == 0)
                {
                    dgv.Rows.Add("No recent activity found", "", "", "");
                    dgv.Rows[0].DefaultCellStyle.ForeColor = Color.Gray;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading recent member activity: {ex.Message}");
                dgv.Rows.Add("Error loading data", ex.Message, "", "");
            }
        }
        
        private string GetTimeAgo(DateTime dateTime)
        {
            TimeSpan timeSpan = DateTime.Now - dateTime;
            if (timeSpan.TotalMinutes < 1) return "Just now";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} minute(s) ago";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} hour(s) ago";
            if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays} day(s) ago";
            return dateTime.ToString("MMM dd, yyyy");
        }



        private void ShowReportsCollection()
        {
            // Get time range
            int days = 7; // Default
            if (_reportsTimeRangeCombo != null)
            {
                days = GetDaysFromTimeRange(_reportsTimeRangeCombo);
            }
            DateTime startDate = DateTime.Now.AddDays(-days);
            
            // Load statistics from database
            int totalTitles = 0;
            int totalCopies = 0;
            int availableCopies = 0;
            int lostDamaged = 0;
            Dictionary<string, int> categoryData = new Dictionary<string, int>();
            Dictionary<string, int> popularCategoryData = new Dictionary<string, int>();
            
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Total Titles
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM Books", connection))
                    {
                        totalTitles = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    
                    // Total Copies
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM BookCopies", connection))
                    {
                        totalCopies = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    
                    // Available Copies
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM BookCopies WHERE Status = 'Available'", connection))
                    {
                        availableCopies = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    
                    // Lost/Damaged
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM BookCopies WHERE Status IN ('Lost', 'Damaged') OR `Condition` IN ('Lost', 'Damaged')", connection))
                    {
                        lostDamaged = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    
                    // Collection by Category
                    string categoryQuery = @"
                        SELECT 
                            COALESCE(Category, 'Uncategorized') AS Category,
                            COUNT(*) AS Count
                        FROM Books
                        GROUP BY Category";
                    using (var cmd = new MySqlCommand(categoryQuery, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string category = reader.GetString("Category");
                                int count = reader.GetInt32("Count");
                                categoryData[category] = count;
                            }
                        }
                    }
                    
                    // Most Popular Categories (by borrowings)
                    string popularQuery = @"
                        SELECT 
                            COALESCE(b.Category, 'Uncategorized') AS Category,
                            COUNT(DISTINCT br.BorrowingId) AS BorrowCount
                        FROM Borrowings br
                        INNER JOIN Books b ON br.BookId = b.BookId
                        WHERE br.BorrowDate >= @StartDate
                        GROUP BY b.Category
                        ORDER BY BorrowCount DESC
                        LIMIT 5";
                    using (var cmd = new MySqlCommand(popularQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", startDate);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string category = reader.GetString("Category");
                                int count = reader.GetInt32("BorrowCount");
                                popularCategoryData[category] = count;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading collection statistics: {ex.Message}");
            }

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
            
            tlpKPI.Controls.Add(CreateReportStatCard("Total Titles", totalTitles.ToString("N0"), "📚", Color.White, Color.Black, false), 0, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Total Copies", totalCopies.ToString("N0"), "📖", Color.White, Color.Black, false), 1, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Available", availableCopies.ToString("N0"), "✅", Color.White, Color.Green, false), 2, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Lost/Damaged", lostDamaged.ToString("N0"), "❌", Color.White, Color.Red, false), 3, 0);
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
            SetupDistributionChart(pnlCatChart, "Collection by Category", categoryData);
            tlpCharts.Controls.Add(pnlCatChart, 0, 0);

            Panel pnlTopChart = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 0, 0) };
            SetupDistributionChart(pnlTopChart, $"Most Popular Categories (Last {days} days)", popularCategoryData);
            tlpCharts.Controls.Add(pnlTopChart, 1, 0);
            flpReportsContent.Controls.Add(tlpCharts);

            // 3. Grid - Full Stack
            Panel pnlGridContainer = new Panel 
            { 
                Height = 400,
                Width = flpReportsContent.ClientSize.Width - 10, 
                BackColor = Color.White, 
                Margin = new Padding(0, 0, 0, 30),
                MinimumSize = new Size(0, 300)
            };
            
            // Make the grid container fill remaining space when parent resizes
            flpReportsContent.Parent.Resize += (s, e) =>
            {
                if (pnlGridContainer != null && flpReportsContent != null)
                {
                    int availableHeight = flpReportsContent.Parent.Height - 600;
                    if (availableHeight > 300)
                    {
                        pnlGridContainer.Height = availableHeight;
                    }
                }
            };
            
            Label lblGridTitle = new Label { Text = "Recent Acquisitions", Font = new Font("Georgia", 14, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true };
            pnlGridContainer.Controls.Add(lblGridTitle);
            
            // Container for DataGridView to enable full stack
            Panel dgvContainer = new Panel
            {
                Location = new Point(20, 60),
                Size = new Size(pnlGridContainer.Width - 40, pnlGridContainer.Height - 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };
            
            DataGridView dgv = CreateReportGrid();
            dgv.Dock = DockStyle.Fill;
            dgv.Columns.Add("Id", "Accession #");
            dgv.Columns.Add("Title", "Title");
            dgv.Columns.Add("Category", "Category");
            dgv.Columns.Add("DateAdded", "Date Added");
            
            // Load recent acquisitions from database
            LoadRecentAcquisitions(dgv, startDate);
            
            dgvContainer.Controls.Add(dgv);
            pnlGridContainer.Controls.Add(dgvContainer);
            flpReportsContent.Controls.Add(pnlGridContainer);
        }
        
        private void LoadRecentAcquisitions(DataGridView dgv, DateTime startDate)
        {
            dgv.Rows.Clear();
            
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    string query = @"
                        SELECT DISTINCT
                            bc.AccessionNumber AS AccessionNo,
                            b.Title,
                            COALESCE(b.Category, 'Uncategorized') AS Category,
                            bc.CreatedDate AS DateAdded
                        FROM BookCopies bc
                        INNER JOIN Books b ON bc.BookId = b.BookId
                        WHERE bc.CreatedDate >= @StartDate
                        ORDER BY bc.CreatedDate DESC
                        LIMIT 50";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@StartDate", startDate);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string accessionNo = reader.IsDBNull(reader.GetOrdinal("AccessionNo")) ? "N/A" : reader.GetString("AccessionNo");
                                string title = reader.IsDBNull(reader.GetOrdinal("Title")) ? "Unknown" : reader.GetString("Title");
                                string category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "Uncategorized" : reader.GetString("Category");
                                DateTime dateAdded = reader.GetDateTime("DateAdded");
                                
                                dgv.Rows.Add(accessionNo, title, category, dateAdded.ToString("MMM dd, yyyy"));
                            }
                        }
                    }
                }
                
                if (dgv.Rows.Count == 0)
                {
                    dgv.Rows.Add("No recent acquisitions found", "", "", "");
                    dgv.Rows[0].DefaultCellStyle.ForeColor = Color.Gray;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading recent acquisitions: {ex.Message}");
                dgv.Rows.Add("Error loading data", ex.Message, "", "");
            }
        }



        private void ShowReportsFines()
        {
            // Get time range
            int days = 7; // Default
            if (_reportsTimeRangeCombo != null)
            {
                days = GetDaysFromTimeRange(_reportsTimeRangeCombo);
            }
            DateTime startDate = DateTime.Now.AddDays(-days);
            
            // Load statistics from database
            decimal totalCollected = 0;
            decimal pendingFines = 0;
            decimal waivedFines = 0;
            int overdueCases = 0;
            Dictionary<string, int> finesByReasonData = new Dictionary<string, int>();
            Dictionary<string, decimal> dailyFinesData = new Dictionary<string, decimal>();
            
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Ensure PaidAmount column exists
                    EnsurePaidAmountColumnExists(connection);
                    
                    // Total Collected (paid fines within time range)
                    string collectedQuery = @"
                        SELECT COALESCE(SUM(COALESCE(f.PaidAmount, f.Amount)), 0) AS TotalCollected
                        FROM Fines f
                        WHERE f.Status = 'Paid' 
                        AND f.PaidDate >= @StartDate";
                    using (var cmd = new MySqlCommand(collectedQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", startDate);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            totalCollected = Convert.ToDecimal(result);
                        }
                    }
                    
                    // Pending Fines (unpaid)
                    string pendingQuery = @"
                        SELECT COALESCE(SUM(f.Amount - COALESCE(f.PaidAmount, 0)), 0) AS PendingFines
                        FROM Fines f
                        WHERE f.Status = 'Unpaid' OR (f.Status = 'Paid' AND f.Amount > COALESCE(f.PaidAmount, 0))";
                    using (var cmd = new MySqlCommand(pendingQuery, connection))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            pendingFines = Convert.ToDecimal(result);
                        }
                    }
                    
                    // Waived Fines
                    string waivedQuery = @"
                        SELECT COALESCE(SUM(f.Amount), 0) AS WaivedFines
                        FROM Fines f
                        WHERE f.Status = 'Waived'";
                    using (var cmd = new MySqlCommand(waivedQuery, connection))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            waivedFines = Convert.ToDecimal(result);
                        }
                    }
                    
                    // Overdue Cases (fines that are unpaid and overdue)
                    string overdueQuery = @"
                        SELECT COUNT(*) AS OverdueCases
                        FROM Fines f
                        LEFT JOIN Borrowings br ON f.BorrowingId = br.BorrowingId
                        WHERE f.Status = 'Unpaid' 
                        AND (br.DueDate < NOW() OR f.CreatedDate < DATE_SUB(NOW(), INTERVAL 7 DAY))";
                    using (var cmd = new MySqlCommand(overdueQuery, connection))
                    {
                        overdueCases = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    
                    // Daily Fines Revenue (for chart)
                    string dailyQuery = $@"
                        SELECT 
                            DATE(f.PaidDate) AS PaidDate,
                            SUM(COALESCE(f.PaidAmount, f.Amount)) AS DailyTotal
                        FROM Fines f
                        WHERE f.Status = 'Paid' 
                        AND f.PaidDate >= DATE_SUB(NOW(), INTERVAL {days} DAY)
                        AND f.PaidDate IS NOT NULL
                        GROUP BY DATE(f.PaidDate)
                        ORDER BY PaidDate ASC";
                    using (var cmd = new MySqlCommand(dailyQuery, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime date = reader.GetDateTime("PaidDate");
                                decimal dailyTotal = reader.GetDecimal("DailyTotal");
                                dailyFinesData[date.ToString("MMM dd")] = dailyTotal;
                            }
                        }
                    }
                    
                    // Fines by Reason/Type
                    string reasonQuery = @"
                        SELECT 
                            CASE 
                                WHEN f.Reason LIKE '%Lost%' OR f.Reason LIKE '%lost%' THEN 'Lost'
                                WHEN f.Reason LIKE '%Damaged%' OR f.Reason LIKE '%damaged%' THEN 'Damaged'
                                WHEN f.Reason LIKE '%Overdue%' OR f.Reason LIKE '%overdue%' OR f.Reason IS NULL THEN 'Overdue'
                                ELSE 'Other'
                            END AS Reason,
                            COUNT(*) AS Count
                        FROM Fines f
                        WHERE f.CreatedDate >= @StartDate
                        GROUP BY Reason";
                    using (var cmd = new MySqlCommand(reasonQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", startDate);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string reason = reader.GetString("Reason");
                                int count = reader.GetInt32("Count");
                                finesByReasonData[reason] = count;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading fines statistics: {ex.Message}");
            }

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
            
            tlpKPI.Controls.Add(CreateReportStatCard("Total Collected", $"₱{totalCollected:N2}", "💰", Color.White, Color.Green, false), 0, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Pending Fines", $"₱{pendingFines:N2}", "⚠️", Color.White, Color.Orange, false), 1, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Waived", $"₱{waivedFines:N2}", "👋", Color.White, Color.Gray, false), 2, 0);
            tlpKPI.Controls.Add(CreateReportStatCard("Overdue Cases", overdueCases.ToString("N0"), "⚖️", Color.White, Color.Red, false), 3, 0);
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
            LoadDailyFinesChart(pnlTrendChart, days);
            tlpCharts.Controls.Add(pnlTrendChart, 0, 0);

            Panel pnlTypeChart = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 0, 0) };
            SetupDistributionChart(pnlTypeChart, "Fines by Reason", finesByReasonData);
            tlpCharts.Controls.Add(pnlTypeChart, 1, 0);
            flpReportsContent.Controls.Add(tlpCharts);

            // 3. Grid - Full Stack
            Panel pnlGridContainer = new Panel 
            { 
                Height = 400,
                Width = flpReportsContent.ClientSize.Width - 10, 
                BackColor = Color.White, 
                Margin = new Padding(0, 0, 0, 30),
                MinimumSize = new Size(0, 300)
            };
            
            // Make the grid container fill remaining space when parent resizes
            flpReportsContent.Parent.Resize += (s, e) =>
            {
                if (pnlGridContainer != null && flpReportsContent != null)
                {
                    int availableHeight = flpReportsContent.Parent.Height - 600;
                    if (availableHeight > 300)
                    {
                        pnlGridContainer.Height = availableHeight;
                    }
                }
            };
            
            Label lblGridTitle = new Label { Text = "Unpaid Fines", Font = new Font("Georgia", 14, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true };
            pnlGridContainer.Controls.Add(lblGridTitle);
            
            // Container for DataGridView to enable full stack
            Panel dgvContainer = new Panel
            {
                Location = new Point(20, 60),
                Size = new Size(pnlGridContainer.Width - 40, pnlGridContainer.Height - 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };
            
            DataGridView dgv = CreateReportGrid();
            dgv.Dock = DockStyle.Fill;
            dgv.Columns.Add("Member", "Member");
            dgv.Columns.Add("Book", "Book");
            dgv.Columns.Add("Reason", "Reason");
            dgv.Columns.Add("Amount", "Amount");
            dgv.Columns.Add("DueDate", "Due Date");
            dgv.Columns.Add("Status", "Status");
            
            // Load unpaid fines from database
            LoadUnpaidFines(dgv);
            
            dgvContainer.Controls.Add(dgv);
            pnlGridContainer.Controls.Add(dgvContainer);
            flpReportsContent.Controls.Add(pnlGridContainer);
        }
        
        private void LoadDailyFinesChart(Panel panel, int days)
        {
            try
            {
                Dictionary<string, decimal> dailyData = new Dictionary<string, decimal>();
                
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    string query = $@"
                        SELECT 
                            DATE(ReturnDate) AS ReturnDate,
                            SUM(FineAmount) AS DailyTotal
                        FROM Borrowings
                        WHERE FineAmount > 0 
                        AND ReturnDate >= DATE_SUB(NOW(), INTERVAL {days} DAY)
                        AND ReturnDate IS NOT NULL
                        GROUP BY DATE(ReturnDate)
                        ORDER BY ReturnDate ASC";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime date = reader.GetDateTime("ReturnDate");
                                decimal dailyTotal = reader.GetDecimal("DailyTotal");
                                dailyData[date.ToString("MMM dd")] = dailyTotal;
                            }
                        }
                    }
                }
                
                if (dailyData.Count > 0)
                {
                    SetupFinesRevenueChart(panel, $"Daily Revenue (Fines) - Last {days} Days", dailyData);
                }
                else
                {
                    SetupChart(panel, "Daily Revenue (Fines)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading daily fines chart: {ex.Message}");
                SetupChart(panel, "Daily Revenue (Fines)");
            }
        }
        
        private void SetupFinesRevenueChart(Panel pnl, string title, Dictionary<string, decimal> dailyData)
        {
            // Header Panel
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.Transparent };
            Label lblTitle = new Label { Text = title, Font = new Font("Georgia", 14, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true };
            Label lblSub = new Label { Text = "Fines collected over time", Font = new Font("Segoe UI", 9), ForeColor = Color.Gray, Location = new Point(22, 50), AutoSize = true };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);
            pnl.Controls.Add(pnlHeader);

            Chart chart = new Chart();
            chart.Dock = DockStyle.Fill;

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
                Name = "Fines Collected",
                Color = Color.Green,
                ChartType = SeriesChartType.Spline,
                BorderWidth = 3
            };
            
            foreach (var item in dailyData)
            {
                s1.Points.AddXY(item.Key, (double)item.Value);
            }

            chart.Series.Add(s1);
            chart.Legends.Add(new Legend { Docking = Docking.Bottom, Alignment = StringAlignment.Center });
            pnl.Controls.Add(chart);
        }
        
        private void LoadUnpaidFines(DataGridView dgv)
        {
            dgv.Rows.Clear();
            
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    EnsurePaidAmountColumnExists(connection);
                    
                    string query = @"
                        SELECT 
                            COALESCE(CONCAT(u.FirstName, ' ', u.LastName), m.MemberNumber, 'Unknown Member') AS MemberName,
                            CASE 
                                WHEN bk.Title IS NOT NULL THEN bk.Title
                                WHEN f.Reason IS NOT NULL AND f.Reason != '' THEN f.Reason
                                ELSE 'N/A'
                            END AS BookTitle,
                            COALESCE(f.Reason, 'Overdue') AS Reason,
                            f.Amount AS FineAmount,
                            COALESCE(br.DueDate, f.CreatedDate) AS DueDate,
                            CASE 
                                WHEN f.Status = 'Unpaid' THEN 'Pending'
                                ELSE f.Status
                            END AS Status
                        FROM Fines f
                        INNER JOIN Members m ON f.MemberId = m.MemberId
                        LEFT JOIN Users u ON m.UserId = u.UserId
                        LEFT JOIN Borrowings br ON f.BorrowingId = br.BorrowingId
                        LEFT JOIN Books bk ON br.BookId = bk.BookId
                        WHERE f.Status = 'Unpaid' OR (f.Status = 'Paid' AND f.Amount > COALESCE(f.PaidAmount, 0))
                        ORDER BY COALESCE(br.DueDate, f.CreatedDate) ASC";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string memberName = reader.IsDBNull(reader.GetOrdinal("MemberName")) ? "Unknown" : reader.GetString("MemberName");
                                string bookTitle = reader.IsDBNull(reader.GetOrdinal("BookTitle")) ? "Unknown" : reader.GetString("BookTitle");
                                string reason = reader.IsDBNull(reader.GetOrdinal("Reason")) ? "Fine" : reader.GetString("Reason");
                                decimal fineAmount = reader.GetDecimal("FineAmount");
                                DateTime dueDate = reader.GetDateTime("DueDate");
                                string status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Pending" : reader.GetString("Status");
                                
                                dgv.Rows.Add(
                                    memberName,
                                    bookTitle,
                                    reason,
                                    $"₱{fineAmount:N2}",
                                    dueDate.ToString("MMM dd, yyyy"),
                                    status
                                );
                            }
                        }
                    }
                }
                
                if (dgv.Rows.Count == 0)
                {
                    dgv.Rows.Add("No unpaid fines found", "", "", "", "", "");
                    dgv.Rows[0].DefaultCellStyle.ForeColor = Color.Gray;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading unpaid fines: {ex.Message}");
                dgv.Rows.Add("Error loading data", ex.Message, "", "", "", "");
            }
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
                Font = new Font("Georgia", 24F, FontStyle.Bold),
                ForeColor = Color.FromArgb(101, 67, 33), // Dark brown like in image
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
                Location = new Point(2, 40),
                BackColor = Color.Transparent
            };

            // Search Bar Panel
            Panel pnlSearchBar = new Panel
            {
                Size = new Size(pnlSearchView.Width - 60, 50),
                Location = new Point(0, 80),
                BackColor = Color.White,
                Padding = new Padding(15),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            
            pnlSearchBar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlSearchBar.Width - 1, pnlSearchBar.Height - 1), 8))
                using (Pen p = new Pen(Color.FromArgb(220, 220, 220), 1))
                {
                    e.Graphics.DrawPath(p, path);
                }
            };

            // Search Icon
            Label lblSearchIcon = new Label 
            { 
                Text = "🔍", 
                Font = new Font("Segoe UI", 14F), 
                Location = new Point(15, 13), 
                AutoSize = true, 
                ForeColor = Color.FromArgb(150, 150, 150), 
                BackColor = Color.Transparent 
            };

            // Search Input
            txtSearchInput = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.Gray,
                Location = new Point(45, 12),
                Width = pnlSearchBar.Width - 350,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Color.White
            };
            txtSearchInput.SetPlaceholder("Search by title, author, ISBN, subject...");

            // Filters Button
            btnSearchFilters = new Button
            {
                Text = "Filters",
                Size = new Size(90, 36),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(pnlSearchBar.Width - 200, 7),
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSearchFilters.FlatAppearance.BorderSize = 1;
            btnSearchFilters.FlatAppearance.BorderColor = Color.FromArgb(220, 220, 220);
            btnSearchFilters.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnSearchFilters.Width - 1, btnSearchFilters.Height - 1), 6))
                {
                    e.Graphics.FillPath(new SolidBrush(btnSearchFilters.BackColor), path);
                    using (Pen borderPen = new Pen(Color.FromArgb(220, 220, 220), 1))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }
                }
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(btnSearchFilters.Text, btnSearchFilters.Font, new SolidBrush(btnSearchFilters.ForeColor), 
                    new RectangleF(0, 0, btnSearchFilters.Width, btnSearchFilters.Height), sf);
            };
            btnSearchFilters.MouseEnter += (s, e) => btnSearchFilters.BackColor = Color.FromArgb(250, 250, 250);
            btnSearchFilters.MouseLeave += (s, e) => btnSearchFilters.BackColor = Color.White;
            
            // Category Filter Dropdown (shown when Filters clicked)
            cmbSearchCategoryFilter = new ComboBox
            {
                Size = new Size(150, 36),
                Location = new Point(pnlSearchBar.Width - 300, 7),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            cmbSearchCategoryFilter.Items.Add("All Categories");
            cmbSearchCategoryFilter.SelectedIndex = 0;
            
            // Load categories from database
            LoadSearchCategories();
            
            btnSearchFilters.Click += (s, e) =>
            {
                cmbSearchCategoryFilter.Visible = !cmbSearchCategoryFilter.Visible;
                if (cmbSearchCategoryFilter.Visible)
                {
                    cmbSearchCategoryFilter.BringToFront();
                }
            };

            // Search Button
            btnTriggerSearch = new Button
            {
                Text = "🔍 Search",
                Size = new Size(100, 36),
                BackColor = ThemeConstants.PrimaryMaroon,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(pnlSearchBar.Width - 100, 7),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnTriggerSearch.FlatAppearance.BorderSize = 0;
            btnTriggerSearch.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnTriggerSearch.Width - 1, btnTriggerSearch.Height - 1), 6))
                {
                    e.Graphics.FillPath(new SolidBrush(btnTriggerSearch.BackColor), path);
                }
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(btnTriggerSearch.Text, btnTriggerSearch.Font, new SolidBrush(btnTriggerSearch.ForeColor), 
                    new RectangleF(0, 0, btnTriggerSearch.Width, btnTriggerSearch.Height), sf);
            };
            btnTriggerSearch.MouseEnter += (s, e) => btnTriggerSearch.BackColor = Color.FromArgb(100, 15, 30);
            btnTriggerSearch.MouseLeave += (s, e) => btnTriggerSearch.BackColor = ThemeConstants.PrimaryMaroon;

            pnlSearchBar.Resize += (s, e) =>
            {
                txtSearchInput.Width = pnlSearchBar.Width - 350;
                btnSearchFilters.Location = new Point(pnlSearchBar.Width - 200, 7);
                cmbSearchCategoryFilter.Location = new Point(pnlSearchBar.Width - 300, 7);
                btnTriggerSearch.Location = new Point(pnlSearchBar.Width - 100, 7);
            };

            pnlSearchBar.Controls.AddRange(new Control[] { lblSearchIcon, txtSearchInput, btnSearchFilters, cmbSearchCategoryFilter, btnTriggerSearch });

            // Results Header Panel
            Panel pnlResultsHeader = new Panel
            {
                Size = new Size(pnlSearchView.Width - 60, 40),
                Location = new Point(0, 150),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            lblSearchResultsCount = new Label
            {
                Text = "Found 0 result(s)",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = true,
                Location = new Point(0, 10),
                BackColor = Color.Transparent
            };

            // View Toggle Buttons
            Panel pnlViewToggle = new Panel
            {
                Size = new Size(80, 30),
                Location = new Point(pnlResultsHeader.Width - 90, 5),
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlViewToggle.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlViewToggle.Width - 1, pnlViewToggle.Height - 1), 6))
                using (Pen p = new Pen(Color.FromArgb(220, 220, 220), 1))
                {
                    e.Graphics.DrawPath(p, path);
                }
            };

            Button btnGridView = new Button
            {
                Text = "⊞",
                Size = new Size(38, 28),
                Location = new Point(1, 1),
                BackColor = isGridView ? Color.FromArgb(245, 245, 245) : Color.White,
                ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12F),
                Cursor = Cursors.Hand
            };
            btnGridView.FlatAppearance.BorderSize = 0;

            Button btnListView = new Button
            {
                Text = "☰",
                Size = new Size(38, 28),
                Location = new Point(41, 1),
                BackColor = !isGridView ? Color.FromArgb(245, 245, 245) : Color.White,
                ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12F),
                Cursor = Cursors.Hand
            };
            btnListView.FlatAppearance.BorderSize = 0;
            
            // Assign event handlers after both buttons are declared
            btnGridView.Click += (s, e) =>
            {
                isGridView = true;
                btnGridView.BackColor = Color.FromArgb(245, 245, 245);
                btnListView.BackColor = Color.White;
                RefreshSearchResults();
            };
            
            btnListView.Click += (s, e) =>
            {
                isGridView = false;
                btnGridView.BackColor = Color.White;
                btnListView.BackColor = Color.FromArgb(245, 245, 245);
                RefreshSearchResults();
            };

            pnlViewToggle.Controls.AddRange(new Control[] { btnGridView, btnListView });
            pnlResultsHeader.Controls.AddRange(new Control[] { lblSearchResultsCount, pnlViewToggle });

            // Results Container - Full Stack
            pnlSearchResultsContainer = new Panel
            {
                Location = new Point(0, 200),
                Size = new Size(pnlSearchView.Width - 60, pnlSearchView.Height - 230),
                BackColor = Color.Transparent,
                AutoScroll = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // Empty State
            Label lblEmptyIcon = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI", 60F),
                ForeColor = Color.LightGray,
                AutoSize = true,
                BackColor = Color.Transparent,
                Name = "lblEmptyIcon"
            };

            Label lblEmptyTitle = new Label
            {
                Text = "Start your search",
                Font = new Font("Georgia", 16F, FontStyle.Bold),
                ForeColor = ThemeConstants.PrimaryMaroon,
                AutoSize = true,
                BackColor = Color.Transparent,
                Name = "lblEmptyTitle"
            };

            Label lblEmptySub = new Label
            {
                Text = "Enter a search term to find books in the library catalog",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Gray,
                AutoSize = true,
                BackColor = Color.Transparent,
                Name = "lblEmptySub"
            };

            pnlSearchResultsContainer.Resize += (s, e) =>
            {
                int cx = pnlSearchResultsContainer.Width / 2;
                int cy = pnlSearchResultsContainer.Height / 2;
                lblEmptyIcon.Location = new Point(cx - lblEmptyIcon.Width / 2, cy - 80);
                lblEmptyTitle.Location = new Point(cx - lblEmptyTitle.Width / 2, cy + 20);
                lblEmptySub.Location = new Point(cx - lblEmptySub.Width / 2, cy + 50);
            };

            pnlSearchResultsContainer.Controls.AddRange(new Control[] { lblEmptyIcon, lblEmptyTitle, lblEmptySub });

            // Search functionality
            btnTriggerSearch.Click += (s, e) => PerformSearch();
            txtSearchInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    PerformSearch();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            cmbSearchCategoryFilter.SelectedIndexChanged += (s, e) => PerformSearch();

            pnlSearchView.Controls.Add(lblTitle);
            pnlSearchView.Controls.Add(lblSubtitle);
            pnlSearchView.Controls.Add(pnlSearchBar);
            pnlSearchView.Controls.Add(pnlResultsHeader);
            pnlSearchView.Controls.Add(pnlSearchResultsContainer);
        }
        
        private void LoadSearchCategories()
        {
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    string query = "SELECT DISTINCT Category FROM Books WHERE Category IS NOT NULL AND Category != '' ORDER BY Category";
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            cmbSearchCategoryFilter.Items.Clear();
                            cmbSearchCategoryFilter.Items.Add("All Categories");
                            while (reader.Read())
                            {
                                cmbSearchCategoryFilter.Items.Add(reader.GetString("Category"));
                            }
                            cmbSearchCategoryFilter.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading search categories: {ex.Message}");
            }
        }
        
        private void PerformSearch()
        {
            try
            {
                string searchTerm = txtSearchInput.GetActualText();
                string category = cmbSearchCategoryFilter?.SelectedItem?.ToString() ?? "All Categories";
                
                if (string.IsNullOrWhiteSpace(searchTerm) && category == "All Categories")
                {
                    // Show empty state
                    ShowSearchEmptyState();
                    return;
                }
                
                var bookService = new Service.BookService();
                List<Book> books;
                
                if (string.IsNullOrWhiteSpace(searchTerm) && category != "All Categories")
                {
                    books = bookService.SearchBooks(null, category);
                }
                else if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    books = bookService.SearchBooks(searchTerm, category == "All Categories" ? null : category);
                }
                else
                {
                    books = bookService.GetAllBooks();
                }
                
                DisplaySearchResults(books);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error performing search: {ex.Message}", "Search Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"Error in PerformSearch: {ex.Message}");
            }
        }
        
        private void DisplaySearchResults(List<Book> books)
        {
            pnlSearchResultsContainer.Controls.Clear();
            
            if (books == null || books.Count == 0)
            {
                ShowSearchEmptyState("No results found", "Try adjusting your search terms or filters");
                lblSearchResultsCount.Text = "Found 0 result(s)";
                return;
            }
            
            lblSearchResultsCount.Text = $"Found {books.Count} result(s)";
            
            if (isGridView)
            {
                DisplayGridView(books);
            }
            else
            {
                DisplayListView(books);
            }
        }
        
        private void DisplayGridView(List<Book> books)
        {
            int cardWidth = 220;
            int cardHeight = 380;
            int gap = 20;
            int cardsPerRow = Math.Max(1, (pnlSearchResultsContainer.Width - 40) / (cardWidth + gap));
            int startX = 0;
            int startY = 0;
            int currentX = startX;
            int currentY = startY;
            int row = 0;
            
            foreach (var book in books)
            {
                Panel bookCard = CreateSearchBookCard(book, cardWidth, cardHeight);
                bookCard.Location = new Point(currentX, currentY);
                pnlSearchResultsContainer.Controls.Add(bookCard);
                
                currentX += cardWidth + gap;
                row++;
                if (row >= cardsPerRow)
                {
                    currentX = startX;
                    currentY += cardHeight + gap;
                    row = 0;
                }
            }
            
            // Update container height for scrolling
            int totalRows = (int)Math.Ceiling((double)books.Count / cardsPerRow);
            pnlSearchResultsContainer.AutoScrollMinSize = new Size(0, totalRows * (cardHeight + gap) + 20);
        }
        
        private void DisplayListView(List<Book> books)
        {
            int itemHeight = 120;
            int yPos = 0;
            
            foreach (var book in books)
            {
                Panel listItem = CreateSearchBookListItem(book, pnlSearchResultsContainer.Width - 40, itemHeight);
                listItem.Location = new Point(0, yPos);
                pnlSearchResultsContainer.Controls.Add(listItem);
                yPos += itemHeight + 10;
            }
            
            pnlSearchResultsContainer.AutoScrollMinSize = new Size(0, yPos);
        }
        
        private Panel CreateSearchBookCard(Book book, int width, int height)
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
                Size = new Size(width - 20, 200),
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

            // Availability badge (like in image: "2 of 4 Available")
            int available = book.AvailableCopies;
            int total = book.TotalCopies;
            Label lblAvailability = new Label
            {
                Text = $"{available} of {total} Available",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 152, 0), // Orange like in image
                BackColor = Color.FromArgb(255, 243, 224),
                AutoSize = true,
                Location = new Point(width - 130, 15),
                Padding = new Padding(8, 4, 8, 4),
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
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Location = new Point(10, 220),
                Size = new Size(width - 20, 25),
                AutoEllipsis = true
            };

            // Author
            Label lblAuthor = new Label
            {
                Text = book.Author,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(10, 250),
                Size = new Size(width - 20, 20),
                AutoEllipsis = true
            };

            // Metadata (Category • Year)
            string metadata = $"{book.Category ?? "Uncategorized"} • {book.PublicationYear ?? 0}";
            Label lblMetadata = new Label
            {
                Text = metadata,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(10, 275),
                Size = new Size(width - 20, 20),
                AutoEllipsis = true
            };

            // Details Button
            Button btnDetails = new Button
            {
                Text = "👁 Details",
                Size = new Size(width - 20, 32),
                Location = new Point(10, height - 42),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            btnDetails.FlatAppearance.BorderSize = 1;
            btnDetails.FlatAppearance.BorderColor = Color.FromArgb(220, 220, 220);
            btnDetails.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btnDetails.Width - 1, btnDetails.Height - 1), 6))
                {
                    e.Graphics.FillPath(new SolidBrush(btnDetails.BackColor), path);
                    using (Pen borderPen = new Pen(Color.FromArgb(220, 220, 220), 1))
                    {
                        e.Graphics.DrawPath(borderPen, path);
                    }
                }
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(btnDetails.Text, btnDetails.Font, new SolidBrush(btnDetails.ForeColor), 
                    new RectangleF(0, 0, btnDetails.Width, btnDetails.Height), sf);
            };
            btnDetails.MouseEnter += (s, e) => btnDetails.BackColor = Color.FromArgb(250, 250, 250);
            btnDetails.MouseLeave += (s, e) => btnDetails.BackColor = Color.White;
            btnDetails.Click += (s, e) => ShowViewBookDialog(book.BookId);

            card.Controls.AddRange(new Control[] { pnlCover, lblAvailability, lblTitle, lblAuthor, lblMetadata, btnDetails });

            return card;
        }
        
        private Panel CreateSearchBookListItem(Book book, int width, int height)
        {
            Panel item = new Panel
            {
                Size = new Size(width, height),
                BackColor = Color.White
            };
            
            item.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, width - 1, height - 1), 6))
                using (Pen p = new Pen(Color.FromArgb(230, 230, 230), 1))
                {
                    e.Graphics.DrawPath(p, path);
                }
            };
            
            // Cover thumbnail
            Panel pnlCover = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(80, 100),
                BackColor = Color.FromArgb(240, 240, 240)
            };
            pnlCover.Paint += (s, e) =>
            {
                using (var font = new Font("Segoe UI", 32))
                using (var brush = new SolidBrush(Color.FromArgb(150, 150, 150)))
                {
                    e.Graphics.DrawString("📚", font, brush, new RectangleF(0, 0, pnlCover.Width, pnlCover.Height), 
                        new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                }
            };
            
            // Title and Author
            Label lblTitle = new Label
            {
                Text = book.Title,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                Location = new Point(110, 15),
                Size = new Size(width - 300, 25),
                AutoEllipsis = true
            };
            
            Label lblAuthor = new Label
            {
                Text = book.Author,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(110, 45),
                Size = new Size(width - 300, 20),
                AutoEllipsis = true
            };
            
            // Metadata
            string metadata = $"{book.Category ?? "Uncategorized"} • {book.PublicationYear ?? 0}";
            Label lblMetadata = new Label
            {
                Text = metadata,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(120, 120, 120),
                Location = new Point(110, 70),
                Size = new Size(width - 300, 20),
                AutoEllipsis = true
            };
            
            // Availability
            Label lblAvailability = new Label
            {
                Text = $"{book.AvailableCopies} of {book.TotalCopies} Available",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 152, 0),
                BackColor = Color.FromArgb(255, 243, 224),
                AutoSize = true,
                Location = new Point(width - 200, 40),
                Padding = new Padding(8, 4, 8, 4),
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
            
            // Details Button
            Button btnDetails = new Button
            {
                Text = "👁 Details",
                Size = new Size(100, 32),
                Location = new Point(width - 110, 40),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            btnDetails.FlatAppearance.BorderSize = 1;
            btnDetails.FlatAppearance.BorderColor = Color.FromArgb(220, 220, 220);
            btnDetails.Click += (s, e) => ShowViewBookDialog(book.BookId);
            
            item.Controls.AddRange(new Control[] { pnlCover, lblTitle, lblAuthor, lblMetadata, lblAvailability, btnDetails });
            
            return item;
        }
        
        private void ShowSearchEmptyState(string title = "Start your search", string subtitle = "Enter a search term to find books in the library catalog")
        {
            pnlSearchResultsContainer.Controls.Clear();
            
            Label lblEmptyIcon = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI", 60F),
                ForeColor = Color.LightGray,
                AutoSize = true,
                BackColor = Color.Transparent,
                Name = "lblEmptyIcon"
            };

            Label lblEmptyTitle = new Label
            {
                Text = title,
                Font = new Font("Georgia", 16F, FontStyle.Bold),
                ForeColor = ThemeConstants.PrimaryMaroon,
                AutoSize = true,
                BackColor = Color.Transparent,
                Name = "lblEmptyTitle"
            };

            Label lblEmptySub = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Gray,
                AutoSize = true,
                BackColor = Color.Transparent,
                Name = "lblEmptySub"
            };

            pnlSearchResultsContainer.Resize += (s, e) =>
            {
                int cx = pnlSearchResultsContainer.Width / 2;
                int cy = pnlSearchResultsContainer.Height / 2;
                lblEmptyIcon.Location = new Point(cx - lblEmptyIcon.Width / 2, cy - 80);
                lblEmptyTitle.Location = new Point(cx - lblEmptyTitle.Width / 2, cy + 20);
                lblEmptySub.Location = new Point(cx - lblEmptySub.Width / 2, cy + 50);
            };

            pnlSearchResultsContainer.Controls.AddRange(new Control[] { lblEmptyIcon, lblEmptyTitle, lblEmptySub });
        }
        
        private void RefreshSearchResults()
        {
            // Re-perform search with current settings
            PerformSearch();
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

            // Get current settings from database
            var fineSettings = GetFineSettings();
            
            Panel pnlMaxCap = CreateSettingInput("Maximum Fine Cap (₱)", fineSettings.MaxFineCap.ToString("N2"));
            Panel pnlGracePeriod = CreateSettingInput("Grace Period (Days)", fineSettings.GracePeriodDays.ToString());
            Panel pnlLostMultiplier = CreateSettingInput("Lost Book Multiplier", fineSettings.LostBookMultiplier.ToString("N2"));
            
            tlpConfig.Controls.Add(pnlMaxCap, 0, 0);
            tlpConfig.Controls.Add(pnlGracePeriod, 1, 0);
            tlpConfig.Controls.Add(pnlLostMultiplier, 2, 0);

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
            
            // Get fine rates from database
            var fineRates = GetFineRates();
            decimal[] rateValues = { fineRates.Student, fineRates.Faculty, fineRates.Staff, fineRates.Guest };
            
            // Store rate input panels for saving
            Panel[] rateInputPanels = new Panel[4];

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

                // Create editable input for rate
                Panel pnlRateInput = CreateSettingInput($"Rate (₱/day)", rateValues[i].ToString("N2"));
                rateInputPanels[i] = pnlRateInput;
                
                // Adjust layout for table
                pnlRateInput.Dock = DockStyle.None;
                pnlRateInput.Height = 40;
                pnlRateInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

                tlpRates.Controls.Add(lT, 0, i);
                tlpRates.Controls.Add(pnlRateInput, 1, i);
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
            
            // Update save button to save fine settings
            Button btnSaveFine = pnlSave.Controls.OfType<Button>().FirstOrDefault();
            if (btnSaveFine != null)
            {
                btnSaveFine.Click -= (s, e) => MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnSaveFine.Click += (s, e) => SaveFineSettings(pnlMaxCap, pnlGracePeriod, pnlLostMultiplier, rateInputPanels);
            }

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

        // Fine Settings Data Classes
        private class FineSettings
        {
            public decimal MaxFineCap { get; set; } = 100;
            public int GracePeriodDays { get; set; } = 0;
            public decimal LostBookMultiplier { get; set; } = 2;
        }

        private class FineRates
        {
            public decimal Student { get; set; } = 5;
            public decimal Faculty { get; set; } = 3;
            public decimal Staff { get; set; } = 4;
            public decimal Guest { get; set; } = 10;
        }

        private FineSettings GetFineSettings()
        {
            var settings = new FineSettings();
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Ensure FineSettings table exists
                    EnsureFineSettingsTableExists(connection);

                    using (var cmd = new MySqlCommand(
                        "SELECT SettingKey, SettingValue FROM FineSettings WHERE SettingKey IN ('MaxFineCap', 'GracePeriodDays', 'LostBookMultiplier')",
                        connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string key = reader.GetString("SettingKey");
                                string value = reader.GetString("SettingValue");
                                
                                switch (key)
                                {
                                    case "MaxFineCap":
                                        decimal.TryParse(value, out decimal maxCap);
                                        settings.MaxFineCap = maxCap;
                                        break;
                                    case "GracePeriodDays":
                                        int.TryParse(value, out int gracePeriod);
                                        settings.GracePeriodDays = gracePeriod;
                                        break;
                                    case "LostBookMultiplier":
                                        decimal.TryParse(value, out decimal multiplier);
                                        settings.LostBookMultiplier = multiplier;
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading fine settings: {ex.Message}");
            }
            return settings;
        }

        private FineRates GetFineRates()
        {
            var rates = new FineRates();
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Ensure FineSettings table exists
                    EnsureFineSettingsTableExists(connection);

                    using (var cmd = new MySqlCommand(
                        "SELECT SettingKey, SettingValue FROM FineSettings WHERE SettingKey IN ('FineRate_Student', 'FineRate_Faculty', 'FineRate_Staff', 'FineRate_Guest')",
                        connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string key = reader.GetString("SettingKey");
                                string value = reader.GetString("SettingValue");
                                
                                if (decimal.TryParse(value, out decimal rate))
                                {
                                    switch (key)
                                    {
                                        case "FineRate_Student":
                                            rates.Student = rate;
                                            break;
                                        case "FineRate_Faculty":
                                            rates.Faculty = rate;
                                            break;
                                        case "FineRate_Staff":
                                            rates.Staff = rate;
                                            break;
                                        case "FineRate_Guest":
                                            rates.Guest = rate;
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading fine rates: {ex.Message}");
            }
            return rates;
        }

        private void EnsureFineSettingsTableExists(MySqlConnection connection)
        {
            try
            {
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS FineSettings (
                        SettingKey VARCHAR(50) PRIMARY KEY,
                        SettingValue VARCHAR(255) NOT NULL,
                        UpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                    )";
                
                using (var cmd = new MySqlCommand(createTableQuery, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                // Insert default values if they don't exist
                string[] defaultSettings = {
                    "('MaxFineCap', '100')",
                    "('GracePeriodDays', '0')",
                    "('LostBookMultiplier', '2')",
                    "('FineRate_Student', '5')",
                    "('FineRate_Faculty', '3')",
                    "('FineRate_Staff', '4')",
                    "('FineRate_Guest', '10')"
                };

                foreach (string setting in defaultSettings)
                {
                    using (var cmd = new MySqlCommand(
                        $"INSERT IGNORE INTO FineSettings (SettingKey, SettingValue) VALUES {setting}",
                        connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error ensuring FineSettings table exists: {ex.Message}");
            }
        }

        private void SaveFineSettings(Panel pnlMaxCap, Panel pnlGracePeriod, Panel pnlLostMultiplier, Panel[] rateInputPanels)
        {
            try
            {
                // Get values from input panels
                TextBox txtMaxCap = pnlMaxCap.Controls.OfType<Panel>().FirstOrDefault()?.Controls.OfType<TextBox>().FirstOrDefault();
                TextBox txtGracePeriod = pnlGracePeriod.Controls.OfType<Panel>().FirstOrDefault()?.Controls.OfType<TextBox>().FirstOrDefault();
                TextBox txtLostMultiplier = pnlLostMultiplier.Controls.OfType<Panel>().FirstOrDefault()?.Controls.OfType<TextBox>().FirstOrDefault();

                if (txtMaxCap == null || txtGracePeriod == null || txtLostMultiplier == null)
                {
                    MessageBox.Show("Error: Could not find input fields.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Parse and validate values
                bool maxCapValid = decimal.TryParse(txtMaxCap.Text.Replace("₱", "").Replace(",", "").Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal maxCap);
                if (!maxCapValid || maxCap < 0)
                {
                    MessageBox.Show("Please enter a valid maximum fine cap amount.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMaxCap.Focus();
                    return;
                }

                bool gracePeriodValid = int.TryParse(txtGracePeriod.Text.Trim(), out int gracePeriod);
                if (!gracePeriodValid || gracePeriod < 0)
                {
                    MessageBox.Show("Please enter a valid grace period (non-negative integer).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtGracePeriod.Focus();
                    return;
                }

                bool lostMultiplierValid = decimal.TryParse(txtLostMultiplier.Text.Replace(",", "").Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal lostMultiplier);
                if (!lostMultiplierValid || lostMultiplier < 0)
                {
                    MessageBox.Show("Please enter a valid lost book multiplier (non-negative number).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtLostMultiplier.Focus();
                    return;
                }

                // Parse fine rates
                string[] rateKeys = { "FineRate_Student", "FineRate_Faculty", "FineRate_Staff", "FineRate_Guest" };
                decimal[] rates = new decimal[4];
                
                for (int i = 0; i < 4; i++)
                {
                    if (rateInputPanels[i] == null)
                    {
                        MessageBox.Show($"Error: Could not find rate input field for {rateKeys[i]}.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    
                    TextBox txtRate = rateInputPanels[i].Controls.OfType<Panel>().FirstOrDefault()?.Controls.OfType<TextBox>().FirstOrDefault();
                    if (txtRate == null)
                    {
                        MessageBox.Show($"Error: Could not find rate input textbox for {rateKeys[i]}.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    
                    string rateText = txtRate.Text.Replace("₱", "").Replace("/day", "").Replace(",", "").Trim();
                    bool rateValid = decimal.TryParse(rateText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out rates[i]);
                    if (!rateValid || rates[i] < 0)
                    {
                        MessageBox.Show($"Please enter a valid fine rate for {rateKeys[i].Replace("FineRate_", "")}.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtRate.Focus();
                        return;
                    }
                }

                // Save to database
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    EnsureFineSettingsTableExists(connection);

                    // Update settings using INSERT ... ON DUPLICATE KEY UPDATE
                    List<string> updates = new List<string>
                    {
                        $"INSERT INTO FineSettings (SettingKey, SettingValue) VALUES ('MaxFineCap', '{maxCap:N2}') ON DUPLICATE KEY UPDATE SettingValue = '{maxCap:N2}'",
                        $"INSERT INTO FineSettings (SettingKey, SettingValue) VALUES ('GracePeriodDays', '{gracePeriod}') ON DUPLICATE KEY UPDATE SettingValue = '{gracePeriod}'",
                        $"INSERT INTO FineSettings (SettingKey, SettingValue) VALUES ('LostBookMultiplier', '{lostMultiplier:N2}') ON DUPLICATE KEY UPDATE SettingValue = '{lostMultiplier:N2}'"
                    };

                    // Add fine rate updates
                    for (int i = 0; i < 4; i++)
                    {
                        updates.Add($"INSERT INTO FineSettings (SettingKey, SettingValue) VALUES ('{rateKeys[i]}', '{rates[i]:N2}') ON DUPLICATE KEY UPDATE SettingValue = '{rates[i]:N2}'");
                    }

                    foreach (string update in updates)
                    {
                        using (var cmd = new MySqlCommand(update, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Fine settings saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving fine settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"Error saving fine settings: {ex.Message}");
            }
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

            tlp.Controls.Add(CreateSettingInput("Fine/Day (₱)", fine, 120), 3, 0);

            

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

        /// <summary>
        /// Refreshes the circulation statistics cards with real-time data from the database
        /// </summary>
        private void RefreshCirculationStatistics()
        {
            try
            {
                // Find the circulation stats panel
                Panel statsPanel = null;
                foreach (Control ctrl in pnlMainContent.Controls)
                {
                    if (ctrl is Panel panel && panel.Tag?.ToString() == "CirculationStatsPanel")
                    {
                        statsPanel = panel;
                        break;
                    }
                }

                if (statsPanel == null) return;

                // Get latest statistics from database
                var stats = _circulationService.GetBorrowingStatistics();

                // Update each card
                foreach (Control card in statsPanel.Controls)
                {
                    if (card is Panel cardPanel)
                    {
                        // Find the value label in the card
                        foreach (Control ctrl in cardPanel.Controls)
                        {
                            if (ctrl is Label lbl && lbl.Tag != null)
                            {
                                string tag = lbl.Tag.ToString();
                                if (tag == "CurrentlyBorrowed")
                                {
                                    lbl.Text = stats.CurrentlyBorrowed.ToString();
                                }
                                else if (tag == "Overdue")
                                {
                                    lbl.Text = stats.Overdue.ToString();
                                }
                                else if (tag == "ReturnedToday")
                                {
                                    lbl.Text = stats.ReturnedToday.ToString();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing circulation statistics: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets reservation statistics from the database
        /// </summary>
        private ReservationStatistics GetReservationStatistics()
        {
            var stats = new ReservationStatistics();
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    string query = @"
                        SELECT 
                            SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) AS Pending,
                            SUM(CASE WHEN Status = 'Ready' THEN 1 ELSE 0 END) AS Ready,
                            SUM(CASE WHEN Status = 'Fulfilled' THEN 1 ELSE 0 END) AS Fulfilled,
                            SUM(CASE WHEN Status = 'Expired' THEN 1 ELSE 0 END) AS Expired
                        FROM Reservations";
                    
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                stats.Pending = reader["Pending"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Pending"]);
                                stats.Ready = reader["Ready"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Ready"]);
                                stats.Fulfilled = reader["Fulfilled"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Fulfilled"]);
                                stats.Expired = reader["Expired"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Expired"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting reservation statistics: {ex.Message}");
            }
            return stats;
        }

        /// <summary>
        /// Gets fines statistics from the database
        /// </summary>
        private FinesStatistics GetFinesStatistics()
        {
            var stats = new FinesStatistics();
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Ensure PaidAmount column exists before querying
                    EnsurePaidAmountColumnExists(connection);
                    
                    // Pending Fines (unpaid fines)
                    string pendingQuery = @"
                        SELECT COALESCE(SUM(Amount - COALESCE(PaidAmount, 0)), 0) AS PendingFines
                        FROM Fines
                        WHERE Status = 'Unpaid'";
                    using (var cmd = new MySqlCommand(pendingQuery, connection))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            stats.PendingFines = Convert.ToDecimal(result);
                        }
                    }

                    // Collected Fines (paid fines)
                    string collectedQuery = @"
                        SELECT COALESCE(SUM(COALESCE(PaidAmount, Amount)), 0) AS Collected
                        FROM Fines
                        WHERE Status = 'Paid'";
                    using (var cmd = new MySqlCommand(collectedQuery, connection))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            stats.Collected = Convert.ToDecimal(result);
                        }
                    }

                    // Waived Fines (sum of amounts for waived fines)
                    string waivedQuery = @"
                        SELECT COALESCE(SUM(Amount), 0) AS Waived
                        FROM Fines
                        WHERE Status = 'Waived'";
                    using (var cmd = new MySqlCommand(waivedQuery, connection))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            stats.Waived = Convert.ToDecimal(result);
                        }
                    }

                    // Pending Cases (number of unpaid fines)
                    string casesQuery = @"
                        SELECT COUNT(*) AS PendingCases
                        FROM Fines
                        WHERE Status = 'Unpaid'";
                    using (var cmd = new MySqlCommand(casesQuery, connection))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            stats.PendingCases = Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting fines statistics: {ex.Message}");
            }
            return stats;
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

        /// <summary>
        /// Gets inventory statistics from the database
        /// </summary>
        private InventoryStatistics GetInventoryStatistics()
        {
            var stats = new InventoryStatistics();
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Total Titles
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM Books", connection))
                    {
                        stats.TotalTitles = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // Total Copies (using Books table TotalCopies)
                    using (var cmd = new MySqlCommand("SELECT COALESCE(SUM(TotalCopies), 0) FROM Books", connection))
                    {
                        object result = cmd.ExecuteScalar();
                        stats.TotalCopies = result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
                    }

                    // Available Copies
                    using (var cmd = new MySqlCommand("SELECT COALESCE(SUM(AvailableCopies), 0) FROM Books", connection))
                    {
                        object result = cmd.ExecuteScalar();
                        stats.Available = result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
                    }

                    // Borrowed (Total - Available)
                    stats.Borrowed = stats.TotalCopies - stats.Available;

                    // Damaged and Lost (from BookCopies if table exists, otherwise 0)
                    try
                    {
                        using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM BookCopies WHERE Status = 'Damaged' OR `Condition` = 'Damaged'", connection))
                        {
                            object result = cmd.ExecuteScalar();
                            stats.Damaged = result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
                        }
                        using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM BookCopies WHERE Status = 'Lost'", connection))
                        {
                            object result = cmd.ExecuteScalar();
                            stats.Lost = result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
                        }
                    }
                    catch
                    {
                        stats.Damaged = 0;
                        stats.Lost = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting inventory statistics: {ex.Message}");
            }
            return stats;
        }

        /// <summary>
        /// Refreshes the reservation statistics cards with real-time data from the database
        /// </summary>
        private void RefreshReservationStatistics()
        {
            try
            {
                Panel statsPanel = null;
                foreach (Control ctrl in pnlMainContent.Controls)
                {
                    if (ctrl is Panel panel && panel.Tag?.ToString() == "ReservationsStatsPanel")
                    {
                        statsPanel = panel;
                        break;
                    }
                }

                if (statsPanel == null) return;

                var stats = GetReservationStatistics();

                foreach (Control card in statsPanel.Controls)
                {
                    if (card is Panel cardPanel)
                    {
                        foreach (Control ctrl in cardPanel.Controls)
                        {
                            if (ctrl is Label lbl && lbl.Tag != null)
                            {
                                string tag = lbl.Tag.ToString();
                                if (tag == "ReservationPending") lbl.Text = stats.Pending.ToString();
                                else if (tag == "ReservationReady") lbl.Text = stats.Ready.ToString();
                                else if (tag == "ReservationFulfilled") lbl.Text = stats.Fulfilled.ToString();
                                else if (tag == "ReservationExpired") lbl.Text = stats.Expired.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing reservation statistics: {ex.Message}");
            }
        }

        /// <summary>
        /// Refreshes the fines statistics cards with real-time data from the database
        /// </summary>
        private void RefreshFinesStatistics()
        {
            try
            {
                Panel statsPanel = null;
                foreach (Control ctrl in pnlMainContent.Controls)
                {
                    if (ctrl is Panel panel && panel.Tag?.ToString() == "FinesStatsPanel")
                    {
                        statsPanel = panel;
                        break;
                    }
                }

                if (statsPanel == null) return;

                var stats = GetFinesStatistics();

                foreach (Control card in statsPanel.Controls)
                {
                    if (card is Panel cardPanel)
                    {
                        foreach (Control ctrl in cardPanel.Controls)
                        {
                            if (ctrl is Label lbl && lbl.Tag != null)
                            {
                                string tag = lbl.Tag.ToString();
                                if (tag == "PendingFines") lbl.Text = $"₱{stats.PendingFines:N2}";
                                else if (tag == "CollectedFines") lbl.Text = $"₱{stats.Collected:N2}";
                                else if (tag == "WaivedFines") lbl.Text = $"₱{stats.Waived:N2}";
                                else if (tag == "PendingCases") lbl.Text = stats.PendingCases.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing fines statistics: {ex.Message}");
            }
        }

        /// <summary>
        /// Refreshes the inventory statistics cards with real-time data from the database
        /// </summary>
        private void RefreshInventoryStatistics()
        {
            try
            {
                Panel statsPanel = null;
                foreach (Control ctrl in pnlMainContent.Controls)
                {
                    if (ctrl is Panel panel && panel.Tag?.ToString() == "InventoryStatsPanel")
                    {
                        statsPanel = panel;
                        break;
                    }
                }

                if (statsPanel == null) return;

                var stats = GetInventoryStatistics();

                foreach (Control card in statsPanel.Controls)
                {
                    if (card is Panel cardPanel)
                    {
                        foreach (Control ctrl in cardPanel.Controls)
                        {
                            if (ctrl is Label lbl && lbl.Tag != null)
                            {
                                string tag = lbl.Tag.ToString();
                                if (tag == "TotalTitles") lbl.Text = stats.TotalTitles.ToString();
                                else if (tag == "TotalCopies") lbl.Text = stats.TotalCopies.ToString();
                                else if (tag == "Available") lbl.Text = stats.Available.ToString();
                                else if (tag == "Borrowed") lbl.Text = stats.Borrowed.ToString();
                                else if (tag == "Damaged") lbl.Text = stats.Damaged.ToString();
                                else if (tag == "Lost") lbl.Text = stats.Lost.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing inventory statistics: {ex.Message}");
            }
        }

        // Statistics classes
        private class ReservationStatistics
        {
            public int Pending { get; set; }
            public int Ready { get; set; }
            public int Fulfilled { get; set; }
            public int Expired { get; set; }
        }

        private class FinesStatistics
        {
            public decimal PendingFines { get; set; }
            public decimal Collected { get; set; }
            public decimal Waived { get; set; }
            public int PendingCases { get; set; }
        }

        private class InventoryStatistics
        {
            public int TotalTitles { get; set; }
            public int TotalCopies { get; set; }
            public int Available { get; set; }
            public int Borrowed { get; set; }
            public int Damaged { get; set; }
            public int Lost { get; set; }
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
            try
            {
                // Get circulation statistics
                var circulationStats = _circulationService.GetBorrowingStatistics();
                
                // Get book statistics
                var bookService = new Service.BookService();
                var bookStats = bookService.GetBookStatistics();
                
                // Get member statistics
                var memberService = new Service.MemberService();
                int totalMembers = 0;
                int activeMembers = 0;
                
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Total Members
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM Members", connection))
                    {
                        totalMembers = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    
                    // Active Members
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM Members WHERE Status = 1", connection))
                    {
                        activeMembers = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
                
                // Calculate week-over-week changes
                DateTime oneWeekAgo = DateTime.Now.AddDays(-7);
                int totalBooksLastWeek = 0;
                int activeMembersLastWeek = 0;
                int booksBorrowedLastWeek = 0;
                int overdueBooksLastWeek = 0;
                
                try
                {
                    using (var connection = Helper.MYSqlHelper.CreateConnection())
                    {
                        // Books count last week
                        using (var cmd = new MySqlCommand(
                            "SELECT COUNT(*) FROM Books WHERE CreatedDate < @OneWeekAgo", connection))
                        {
                            cmd.Parameters.AddWithValue("@OneWeekAgo", oneWeekAgo);
                            totalBooksLastWeek = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        
                        // Active members last week
                        using (var cmd = new MySqlCommand(
                            "SELECT COUNT(*) FROM Members WHERE Status = 1 AND CreatedDate < @OneWeekAgo", connection))
                        {
                            cmd.Parameters.AddWithValue("@OneWeekAgo", oneWeekAgo);
                            activeMembersLastWeek = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        
                        // Books borrowed last week
                        using (var cmd = new MySqlCommand(
                            "SELECT COUNT(*) FROM Borrowings WHERE BorrowDate < @OneWeekAgo AND ReturnDate IS NULL", connection))
                        {
                            cmd.Parameters.AddWithValue("@OneWeekAgo", oneWeekAgo);
                            booksBorrowedLastWeek = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        
                        // Overdue books last week
                        using (var cmd = new MySqlCommand(
                            "SELECT COUNT(*) FROM Borrowings WHERE DueDate < @OneWeekAgo AND ReturnDate IS NULL", connection))
                        {
                            cmd.Parameters.AddWithValue("@OneWeekAgo", oneWeekAgo);
                            overdueBooksLastWeek = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                    }
                }
                catch { }
                
                // Calculate percentage changes
                int totalBooks = bookStats.ContainsKey("TotalTitles") ? bookStats["TotalTitles"] : 0;
                int booksBorrowed = circulationStats.CurrentlyBorrowed;
                int overdueBooks = circulationStats.Overdue;
                
                string totalBooksChange = CalculatePercentageChange(totalBooks, totalBooksLastWeek);
                string activeMembersChange = CalculatePercentageChange(activeMembers, activeMembersLastWeek);
                string booksBorrowedChange = CalculatePercentageChange(booksBorrowed, booksBorrowedLastWeek);
                string overdueBooksChange = CalculatePercentageChange(overdueBooks, overdueBooksLastWeek);
                
                // Today's statistics
                DateTime today = DateTime.Now.Date;
                int todaysBorrowings = 0;
                int todaysReturns = 0;
                decimal pendingFines = 0;
                
                try
                {
                    using (var connection = Helper.MYSqlHelper.CreateConnection())
                    {
                        // Today's borrowings
                        using (var cmd = new MySqlCommand(
                            "SELECT COUNT(*) FROM Borrowings WHERE DATE(BorrowDate) = @Today", connection))
                        {
                            cmd.Parameters.AddWithValue("@Today", today);
                            todaysBorrowings = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        
                        // Today's returns
                        using (var cmd = new MySqlCommand(
                            "SELECT COUNT(*) FROM Borrowings WHERE DATE(ReturnDate) = @Today AND ReturnDate IS NOT NULL", connection))
                        {
                            cmd.Parameters.AddWithValue("@Today", today);
                            todaysReturns = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        
                        // Pending fines (unpaid fines)
                        EnsurePaidAmountColumnExists(connection);
                        using (var cmd = new MySqlCommand(
                            @"SELECT COALESCE(SUM(f.Amount - COALESCE(f.PaidAmount, 0)), 0) AS PendingFines
                              FROM Fines f
                              WHERE f.Status = 'Unpaid' OR (f.Status = 'Paid' AND f.Amount > COALESCE(f.PaidAmount, 0))", connection))
                        {
                            object result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                pendingFines = Convert.ToDecimal(result);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading today's statistics: {ex.Message}");
                }
                
                // Update labels
                lblTotalBooks.Text = totalBooks.ToString("N0");
                lblTotalBooksChange.Text = totalBooksChange;
                
                lblActiveMembers.Text = activeMembers.ToString("N0");
                lblActiveMembersChange.Text = activeMembersChange;
                
                lblBooksBorrowed.Text = booksBorrowed.ToString("N0");
                lblBooksBorrowedChange.Text = booksBorrowedChange;
                
                lblOverdueBooks.Text = overdueBooks.ToString("N0");
                lblOverdueBooksChange.Text = overdueBooksChange;
                
                lblTodaysBorrowings.Text = todaysBorrowings.ToString("N0");
                lblTodaysReturns.Text = todaysReturns.ToString("N0");
                lblPendingFines.Text = $"₱{pendingFines:N2}";
                
                // Load charts
                LoadWeeklyCirculationChart();
                LoadCollectionCategoryChart();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
                // Set default values on error
                lblTotalBooks.Text = "0";
                lblActiveMembers.Text = "0";
                lblBooksBorrowed.Text = "0";
                lblOverdueBooks.Text = "0";
                lblTodaysBorrowings.Text = "0";
                lblTodaysReturns.Text = "0";
                lblPendingFines.Text = "₱0.00";
            }
        }
        
        private string CalculatePercentageChange(int current, int previous)
        {
            if (previous == 0)
            {
                return current > 0 ? "+100% from last week" : "0% from last week";
            }
            
            double change = ((double)(current - previous) / previous) * 100;
            string sign = change >= 0 ? "+" : "";
            return $"{sign}{change:F1}% from last week";
        }
        
        private void LoadWeeklyCirculationChart()
        {
            try
            {
                if (pnlWeeklyCirculation == null) return;
                
                pnlWeeklyCirculation.Controls.Clear();
                
                Dictionary<string, int> borrowData = new Dictionary<string, int>();
                Dictionary<string, int> returnData = new Dictionary<string, int>();
                
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Get borrowings for last 7 days
                    string borrowQuery = @"
                        SELECT 
                            DATE(BorrowDate) AS BorrowDate,
                            COUNT(*) AS BorrowCount
                        FROM Borrowings
                        WHERE BorrowDate >= DATE_SUB(NOW(), INTERVAL 7 DAY)
                        GROUP BY DATE(BorrowDate)
                        ORDER BY BorrowDate ASC";
                    
                    using (var command = new MySqlCommand(borrowQuery, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime date = reader.GetDateTime("BorrowDate");
                                int count = reader.GetInt32("BorrowCount");
                                borrowData[date.ToString("MMM dd")] = count;
                            }
                        }
                    }
                    
                    // Get returns for last 7 days
                    string returnQuery = @"
                        SELECT 
                            DATE(ReturnDate) AS ReturnDate,
                            COUNT(*) AS ReturnCount
                        FROM Borrowings
                        WHERE ReturnDate >= DATE_SUB(NOW(), INTERVAL 7 DAY)
                        AND ReturnDate IS NOT NULL
                        GROUP BY DATE(ReturnDate)
                        ORDER BY ReturnDate ASC";
                    
                    using (var command = new MySqlCommand(returnQuery, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime date = reader.GetDateTime("ReturnDate");
                                int count = reader.GetInt32("ReturnCount");
                                returnData[date.ToString("MMM dd")] = count;
                            }
                        }
                    }
                }
                
                SetupChartWithData(pnlWeeklyCirculation, "Weekly Circulation Trends", borrowData, returnData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading weekly circulation chart: {ex.Message}");
                SetupChart(pnlWeeklyCirculation, "Weekly Circulation Trends");
            }
        }
        
        private void LoadCollectionCategoryChart()
        {
            try
            {
                if (pnlCollectionCategory == null) return;
                
                pnlCollectionCategory.Controls.Clear();
                
                Dictionary<string, int> categoryData = new Dictionary<string, int>();
                
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Get collection by category
                    string categoryQuery = @"
                        SELECT 
                            COALESCE(Category, 'Uncategorized') AS Category,
                            COUNT(*) AS Count
                        FROM Books
                        GROUP BY Category
                        ORDER BY Count DESC
                        LIMIT 6";
                    
                    using (var command = new MySqlCommand(categoryQuery, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string category = reader.GetString("Category");
                                int count = reader.GetInt32("Count");
                                categoryData[category] = count;
                            }
                        }
                    }
                }
                
                SetupDistributionChart(pnlCollectionCategory, "Collection by Category", categoryData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading collection category chart: {ex.Message}");
                SetupDistributionChart(pnlCollectionCategory, "Collection by Category", new Dictionary<string, int>());
            }
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
            try
            {
                using (CheckOutBookDialog dialog = new CheckOutBookDialog())
                {
                    dialog.Owner = this;
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        // Validate member before borrowing
                        string validationError;
                        if (!_circulationService.ValidateMember(dialog.SelectedMemberId, out validationError))
                        {
                            MessageBox.Show(validationError, "Member Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // Borrow the book
                        string errorMessage;
                        bool success = _circulationService.BorrowBook(
                            dialog.SelectedMemberId,
                            dialog.SelectedBookId,
                            dialog.DueDate,
                            out errorMessage
                        );

                        if (success)
                        {
                            // Generate and display receipt
                            var member = _memberService.GetMemberById(dialog.SelectedMemberId);
                            string memberName = member != null ? $"{member.FirstName} {member.LastName}" : "Unknown";
                            
                            string receipt = $"✓ Book Checkout Successful\n\n" +
                                           $"Member: {memberName}\n" +
                                           $"Due Date: {dialog.DueDate:dd MMM yyyy}\n" +
                                           $"Date: {DateTime.Now:dd MMM yyyy HH:mm}";
                            
                            MessageBox.Show(receipt, "Checkout Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            
                            // Refresh circulation statistics
                            RefreshCirculationStatistics();
                            
                            // Refresh view if on circulation/dashboard
                            ShowDashboardView();
                        }
                        else
                        {
                            MessageBox.Show(errorMessage, "Checkout Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during checkout: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowReturnBookDialog()
        {
            try
            {
                using (ReturnBookDialog dialog = new ReturnBookDialog())
                {
                    dialog.Owner = this;
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        // Refresh circulation statistics
                        RefreshCirculationStatistics();
                        
                        // Refresh circulation view
                        ShowCirculationView();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during return: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EnsureReservationsTableExists()
        {
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    string checkTableQuery = @"
                        SELECT COUNT(*) 
                        FROM information_schema.tables 
                        WHERE table_schema = DATABASE() 
                        AND table_name = 'Reservations'";
                    
                    using (var checkCmd = new MySqlCommand(checkTableQuery, connection))
                    {
                        int tableExists = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (tableExists == 0)
                        {
                            // Create Reservations table
                            string createTableQuery = @"
                                CREATE TABLE Reservations (
                                    ReservationId INT PRIMARY KEY AUTO_INCREMENT,
                                    MemberId INT NOT NULL,
                                    BookId INT NOT NULL,
                                    ReservedOn DATETIME DEFAULT CURRENT_TIMESTAMP,
                                    Expires DATETIME NOT NULL,
                                    Status VARCHAR(20) DEFAULT 'Pending',
                                    Notified BOOLEAN DEFAULT FALSE,
                                    FOREIGN KEY (MemberId) REFERENCES Members(MemberId) ON DELETE CASCADE,
                                    FOREIGN KEY (BookId) REFERENCES Books(BookId) ON DELETE CASCADE,
                                    INDEX idx_MemberId (MemberId),
                                    INDEX idx_BookId (BookId),
                                    INDEX idx_Status (Status)
                                )";
                            
                            using (var createCmd = new MySqlCommand(createTableQuery, connection))
                            {
                                createCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error ensuring Reservations table exists: {ex.Message}");
            }
        }
        
        private void EnsureFinesTableAllowsNullBorrowingId()
        {
            try
            {
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    // Check if BorrowingId column allows NULL
                    string checkColumnQuery = @"
                        SELECT IS_NULLABLE 
                        FROM information_schema.columns 
                        WHERE table_schema = DATABASE() 
                        AND table_name = 'Fines' 
                        AND column_name = 'BorrowingId'";
                    
                    using (var checkCmd = new MySqlCommand(checkColumnQuery, connection))
                    {
                        object result = checkCmd.ExecuteScalar();
                        if (result != null && result.ToString().ToUpper() == "NO")
                        {
                            // Column doesn't allow NULL, modify it
                            string alterQuery = @"
                                ALTER TABLE Fines 
                                MODIFY COLUMN BorrowingId INT NULL";
                            
                            using (var alterCmd = new MySqlCommand(alterQuery, connection))
                            {
                                alterCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error ensuring Fines table allows NULL BorrowingId: {ex.Message}");
            }
        }

        private void ShowReservationDialog()
        {
            // Ensure Reservations table exists
            EnsureReservationsTableExists();

            Form rvForm = new Form();
            rvForm.Size = new Size(520, 380);
            rvForm.FormBorderStyle = FormBorderStyle.None;
            rvForm.StartPosition = FormStartPosition.CenterParent;
            rvForm.BackColor = Color.FromArgb(248, 247, 242);
            rvForm.ShowInTaskbar = false;

             rvForm.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, rvForm.Width - 1, rvForm.Height - 1), 10))
                {
                    rvForm.Region = new Region(path);
                    using (Pen p = new Pen(Color.FromArgb(220, 220, 220), 1))
                    {
                        e.Graphics.DrawPath(p, path);
                    }
                }
            };

            // Header
            Label btnClose = new Label 
            { 
                Text = "✕", 
                Font = new Font("Segoe UI", 14F), 
                ForeColor = Color.FromArgb(150, 150, 150), 
                Location = new Point(rvForm.Width - 45, 15), 
                Size = new Size(35, 35), 
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose.Click += (s, e) => rvForm.Close();
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.FromArgb(80, 80, 80);
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.FromArgb(150, 150, 150);
            rvForm.Controls.Add(btnClose);

            Label lblTitle = new Label 
            { 
                Text = "Create Reservation", 
                Font = new Font("Segoe UI", 18F, FontStyle.Bold), 
                ForeColor = Color.FromArgb(101, 67, 33), 
                Location = new Point(30, 20), 
                Size = new Size(350, 30), // Set fixed width to prevent overlap
                AutoSize = false
            };
            rvForm.Controls.Add(lblTitle);

            Label lblSubtitle = new Label 
            { 
                Text = "Reserve a book for a library member", 
                Font = new Font("Segoe UI", 9.5F), 
                ForeColor = Color.FromArgb(120, 120, 120), 
                Location = new Point(30, 52), 
                AutoSize = true 
            };
            rvForm.Controls.Add(lblSubtitle);

            int y = 90;
            int selectedMemberId = 0;
            int selectedBookId = 0;

            // Member Select
            Label lblMem = new Label 
            { 
                Text = "Select Member", 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular), 
                ForeColor = Color.FromArgb(60, 60, 60), 
                Location = new Point(30, y), 
                AutoSize = true 
            };
            rvForm.Controls.Add(lblMem);
            
            Panel pnlMem = new Panel 
            { 
                Location = new Point(30, y + 28), 
                Size = new Size(458, 44), 
                BackColor = Color.White,
                Tag = false
            };
            
            TextBox txtMemberSearch = new TextBox
            {
                Location = new Point(12, 12),
                Size = new Size(410, 20),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            txtMemberSearch.SetPlaceholder("Choose a member...");
            
            Panel pnlMemberDropdown = new Panel
            {
                Location = new Point(30, y + 76),
                Size = new Size(458, 0),
                BackColor = Color.White,
                Visible = false,
                BorderStyle = BorderStyle.None
            };
            
            pnlMemberDropdown.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath shadowPath = CreateRoundedRectangle(new Rectangle(2, 2, pnlMemberDropdown.Width - 3, pnlMemberDropdown.Height - 3), 6))
                {
                    using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                    {
                        e.Graphics.FillPath(shadowBrush, shadowPath);
                    }
                }
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlMemberDropdown.Width - 1, pnlMemberDropdown.Height - 1), 6))
                {
                    e.Graphics.FillPath(Brushes.White, path);
                    using (Pen pen = new Pen(Color.FromArgb(200, 200, 200), 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };
            
            ListBox lstMembers = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5F),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60),
                ItemHeight = 22
            };
            
            lstMembers.MouseDown += (s, e) =>
            {
                int index = lstMembers.IndexFromPoint(e.Location);
                if (index >= 0)
                {
                    lstMembers.SelectedIndex = index;
                }
            };
            
            // Load members from database
            MemberService memberService = new MemberService();
            List<MemberData> allMembers = new List<MemberData>();
            var filteredMembers = new List<MemberData>();
            
            Action refreshMemberData = () =>
            {
                try
                {
                    allMembers = memberService.GetAllMembers().Where(m => m.Status == 1).ToList();
                    filteredMembers = new List<MemberData>(allMembers);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading members: {ex.Message}");
                    allMembers = new List<MemberData>();
                    filteredMembers = new List<MemberData>();
                }
            };
            
            refreshMemberData();
            
            Action updateMemberList = () =>
            {
                lstMembers.Items.Clear();
                if (filteredMembers == null || filteredMembers.Count == 0)
                {
                    lstMembers.Items.Add("No active members found");
                }
                else
                {
                    foreach (var member in filteredMembers)
                    {
                        lstMembers.Items.Add($"{member.FirstName} {member.LastName} ({member.MemberNumber})");
                    }
                }
                lstMembers.Visible = true;
                lstMembers.Refresh();
                pnlMemberDropdown.Refresh();
            };
            
            updateMemberList();
            
            txtMemberSearch.TextChanged += (s, e) =>
            {
                string searchText = txtMemberSearch.Text.ToLower();
                if (txtMemberSearch.Tag is PlaceholderTextHelper.PlaceholderData placeholderData)
                {
                    if (searchText == placeholderData.PlaceholderText.ToLower())
                    {
                        searchText = "";
                    }
                }
                
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    filteredMembers = new List<MemberData>(allMembers);
                }
                else
                {
                    filteredMembers = allMembers.Where(m =>
                        m.MemberNumber.ToLower().Contains(searchText) ||
                        m.FirstName.ToLower().Contains(searchText) ||
                        m.LastName.ToLower().Contains(searchText) ||
                        m.Email.ToLower().Contains(searchText)
                    ).ToList();
                }
                updateMemberList();
            };
            
            lstMembers.SelectedIndexChanged += (s, e) =>
            {
                if (lstMembers.SelectedIndex >= 0 && lstMembers.SelectedIndex < filteredMembers.Count)
                {
                    var member = filteredMembers[lstMembers.SelectedIndex];
                    selectedMemberId = member.MemberId;
                    txtMemberSearch.Text = $"{member.FirstName} {member.LastName} ({member.MemberNumber})";
                    txtMemberSearch.ForeColor = Color.Black;
                    pnlMemberDropdown.Visible = false;
                }
            };
            
            pnlMemberDropdown.Controls.Add(lstMembers);
            
            Label lblMemberArrow = new Label
            {
                Text = "▼",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(430, 13),
                Size = new Size(25, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            Action showMemberDropdown = () =>
            {
                refreshMemberData();
                updateMemberList();
                int itemCount = lstMembers.Items.Count;
                int height = itemCount > 0 ? Math.Min(150, itemCount * 22 + 10) : 40;
                
                pnlMemberDropdown.Location = new Point(30, y + 28 + 44 + 2);
                pnlMemberDropdown.Width = 458;
                pnlMemberDropdown.Height = height;
                pnlMemberDropdown.Visible = true;
                pnlMemberDropdown.BringToFront();
                pnlMemberDropdown.Invalidate();
                
                lstMembers.Visible = true;
                lstMembers.Height = height;
                lstMembers.Refresh();
                
                rvForm.Invalidate();
                rvForm.Update();
            };
            
            lblMemberArrow.Click += (s, e) => 
            { 
                txtMemberSearch.Focus();
                showMemberDropdown();
            };
            
            txtMemberSearch.Enter += (s, e) =>
            {
                pnlMem.Tag = true;
                pnlMem.Invalidate();
                showMemberDropdown();
            };
            
            txtMemberSearch.Leave += (s, e) =>
            {
                pnlMem.Tag = false;
                pnlMem.Invalidate();
                System.Threading.Thread.Sleep(200);
                pnlMemberDropdown.Visible = false;
            };
            
            txtMemberSearch.Click += (s, e) =>
            {
                showMemberDropdown();
            };
            
            pnlMem.Controls.AddRange(new Control[] { txtMemberSearch, lblMemberArrow });
            rvForm.Controls.Add(pnlMem);
            
            pnlMem.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool isFocused = (bool)pnlMem.Tag;
                Color borderColor = isFocused ? Color.FromArgb(128, 0, 32) : Color.FromArgb(200, 200, 200);
                int borderWidth = isFocused ? 2 : 1;
                using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlMem.Width - 1, pnlMem.Height - 1), 6))
                using(Pen pen = new Pen(borderColor, borderWidth)) 
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };
            
            y += 90;

            // Book Select
            Label lblBk = new Label 
            { 
                Text = "Select Book", 
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular), 
                ForeColor = Color.FromArgb(60, 60, 60), 
                Location = new Point(30, y), 
                AutoSize = true 
            };
            rvForm.Controls.Add(lblBk);

            Panel pnlBk = new Panel 
            { 
                Location = new Point(30, y + 28), 
                Size = new Size(458, 44), 
                BackColor = Color.White,
                Tag = false
            };
            
            TextBox txtBookSearch = new TextBox
            {
                Location = new Point(12, 12),
                Size = new Size(410, 20),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            txtBookSearch.SetPlaceholder("Choose a book...");
            
            Panel pnlBookDropdown = new Panel
            {
                Location = new Point(30, y + 76),
                Size = new Size(458, 0),
                BackColor = Color.White,
                Visible = false,
                BorderStyle = BorderStyle.None
            };
            
            pnlBookDropdown.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath shadowPath = CreateRoundedRectangle(new Rectangle(2, 2, pnlBookDropdown.Width - 3, pnlBookDropdown.Height - 3), 6))
                {
                    using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                    {
                        e.Graphics.FillPath(shadowBrush, shadowPath);
                    }
                }
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlBookDropdown.Width - 1, pnlBookDropdown.Height - 1), 6))
                {
                    e.Graphics.FillPath(Brushes.White, path);
                    using (Pen pen = new Pen(Color.FromArgb(200, 200, 200), 1))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };
            
            ListBox lstBooks = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5F),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60),
                ItemHeight = 22
            };
            
            lstBooks.MouseDown += (s, e) =>
            {
                int index = lstBooks.IndexFromPoint(e.Location);
                if (index >= 0)
                {
                    lstBooks.SelectedIndex = index;
                }
            };
            
            // Load books from database
            BookService bookService = new BookService();
            List<Book> allBooks = new List<Book>();
            var filteredBooks = new List<Book>();
            
            Action refreshBookData = () =>
            {
                try
                {
                    allBooks = bookService.GetAllBooks().Where(b => b.AvailableCopies > 0).ToList();
                    filteredBooks = new List<Book>(allBooks);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading books: {ex.Message}");
                    allBooks = new List<Book>();
                    filteredBooks = new List<Book>();
                }
            };
            
            refreshBookData();
            
            Action updateBookList = () =>
            {
                lstBooks.Items.Clear();
                if (filteredBooks == null || filteredBooks.Count == 0)
                {
                    lstBooks.Items.Add("No available books found");
                }
                else
                {
                    foreach (var book in filteredBooks)
                    {
                        lstBooks.Items.Add($"{book.Title} by {book.Author}");
                    }
                }
                lstBooks.Visible = true;
                lstBooks.Refresh();
                pnlBookDropdown.Refresh();
            };
            
            updateBookList();
            
            txtBookSearch.TextChanged += (s, e) =>
            {
                string searchText = txtBookSearch.Text.ToLower();
                if (txtBookSearch.Tag is PlaceholderTextHelper.PlaceholderData placeholderData)
                {
                    if (searchText == placeholderData.PlaceholderText.ToLower())
                    {
                        searchText = "";
                    }
                }
                
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    filteredBooks = new List<Book>(allBooks);
                }
                else
                {
                    filteredBooks = allBooks.Where(b =>
                        b.Title.ToLower().Contains(searchText) ||
                        b.Author.ToLower().Contains(searchText) ||
                        (b.ISBN != null && b.ISBN.ToLower().Contains(searchText))
                    ).ToList();
                }
                updateBookList();
            };
            
            lstBooks.SelectedIndexChanged += (s, e) =>
            {
                if (lstBooks.SelectedIndex >= 0 && lstBooks.SelectedIndex < filteredBooks.Count)
                {
                    var book = filteredBooks[lstBooks.SelectedIndex];
                    selectedBookId = book.BookId;
                    txtBookSearch.Text = $"{book.Title} by {book.Author}";
                    txtBookSearch.ForeColor = Color.Black;
                    pnlBookDropdown.Visible = false;
                }
            };
            
            pnlBookDropdown.Controls.Add(lstBooks);
            
            Label lblBookArrow = new Label
            {
                Text = "▼",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(430, 13),
                Size = new Size(25, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            Action showBookDropdown = () =>
            {
                refreshBookData();
                updateBookList();
                int itemCount = lstBooks.Items.Count;
                int height = itemCount > 0 ? Math.Min(150, itemCount * 22 + 10) : 40;
                
                pnlBookDropdown.Location = new Point(30, y + 28 + 44 + 2);
                pnlBookDropdown.Width = 458;
                pnlBookDropdown.Height = height;
                pnlBookDropdown.Visible = true;
                pnlBookDropdown.BringToFront();
                pnlBookDropdown.Invalidate();
                
                lstBooks.Visible = true;
                lstBooks.Height = height;
                lstBooks.Refresh();
                
                rvForm.Invalidate();
                rvForm.Update();
            };
            
            lblBookArrow.Click += (s, e) => 
            { 
                txtBookSearch.Focus();
                showBookDropdown();
            };
            
            txtBookSearch.Enter += (s, e) =>
            {
                pnlBk.Tag = true;
                pnlBk.Invalidate();
                // Don't auto-show dropdown on enter, only on click
            };
            
            txtBookSearch.Leave += (s, e) =>
            {
                pnlBk.Tag = false;
                pnlBk.Invalidate();
                System.Threading.Thread.Sleep(200);
                pnlBookDropdown.Visible = false;
            };
            
            txtBookSearch.Click += (s, e) =>
            {
                showBookDropdown();
            };
            
            pnlBk.Controls.AddRange(new Control[] { txtBookSearch, lblBookArrow });
            rvForm.Controls.Add(pnlBk);
            
            pnlBk.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool isFocused = (bool)pnlBk.Tag;
                Color borderColor = isFocused ? Color.FromArgb(128, 0, 32) : Color.FromArgb(200, 200, 200);
                int borderWidth = isFocused ? 2 : 1;
                using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, pnlBk.Width - 1, pnlBk.Height - 1), 6))
                using(Pen pen = new Pen(borderColor, borderWidth)) 
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            // Buttons
            Button btnCancel = new Button 
            { 
                Text = "Cancel", 
                Size = new Size(90, 38), 
                Location = new Point(220, 300), // Moved left to prevent overlap with Create Reservation button 
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.White, 
                ForeColor = Color.FromArgb(80, 80, 80), 
                Font = new Font("Segoe UI", 9.5F),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(220, 220, 220);
            btnCancel.Click += (s, e) => rvForm.Close();
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

            Button btnConfirm = new Button 
            { 
                Text = "Create Reservation", 
                Size = new Size(150, 38), 
                Location = new Point(320, 300), // Moved left to add more right margin (520 - 50 margin - 150 width = 320)
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.FromArgb(128, 0, 32), 
                ForeColor = Color.White, 
                Font = new Font("Segoe UI", 9.5F),
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
             btnConfirm.Paint += (s, e) => {
                Button btn = s as Button;
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 6))
                using(SolidBrush brush = new SolidBrush(btn.BackColor)) 
                {
                    e.Graphics.FillPath(brush, path);
                }
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(btn.Text, btn.Font, new SolidBrush(btn.ForeColor), new RectangleF(0, 0, btn.Width, btn.Height), sf);
            };
            btnConfirm.MouseEnter += (s, e) => btnConfirm.BackColor = Color.FromArgb(110, 0, 25);
            btnConfirm.MouseLeave += (s, e) => btnConfirm.BackColor = Color.FromArgb(128, 0, 32);
            
            btnConfirm.Click += (s, e) =>
            {
                if (selectedMemberId == 0)
                {
                    MessageBox.Show("Please select a member.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMemberSearch.Focus();
                    return;
                }
                
                if (selectedBookId == 0)
                {
                    MessageBox.Show("Please select a book.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtBookSearch.Focus();
                    return;
                }
                
                // Create reservation in database
                try
                {
                    using (var connection = Helper.MYSqlHelper.CreateConnection())
                    {
                        // Check if member already has an active reservation for this book
                        string checkQuery = @"
                            SELECT COUNT(*) 
                            FROM Reservations 
                            WHERE MemberId = @MemberId 
                            AND BookId = @BookId 
                            AND Status IN ('Pending', 'Ready')";
                        
                        using (var checkCmd = new MySqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@MemberId", selectedMemberId);
                            checkCmd.Parameters.AddWithValue("@BookId", selectedBookId);
                            int existingReservations = Convert.ToInt32(checkCmd.ExecuteScalar());
                            
                            if (existingReservations > 0)
                            {
                                MessageBox.Show("This member already has an active reservation for this book.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }
                        
                        // Create reservation (expires in 7 days)
                        DateTime expiresDate = DateTime.Now.AddDays(7);
                        string insertQuery = @"
                            INSERT INTO Reservations (MemberId, BookId, ReservedOn, Expires, Status, Notified)
                            VALUES (@MemberId, @BookId, @ReservedOn, @Expires, 'Pending', FALSE)";
                        
                        using (var insertCmd = new MySqlCommand(insertQuery, connection))
                        {
                            insertCmd.Parameters.AddWithValue("@MemberId", selectedMemberId);
                            insertCmd.Parameters.AddWithValue("@BookId", selectedBookId);
                            insertCmd.Parameters.AddWithValue("@ReservedOn", DateTime.Now);
                            insertCmd.Parameters.AddWithValue("@Expires", expiresDate);
                            insertCmd.ExecuteNonQuery();
                        }
                    }
                    
                    MessageBox.Show("Reservation created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    rvForm.DialogResult = DialogResult.OK;
                    rvForm.Close();
                    
                    // Refresh reservations view and stats
                    Panel statsPanel = pnlMainContent.Controls.OfType<Panel>().FirstOrDefault(p => p.Tag?.ToString() == "ReservationsStatsPanel");
                    if (statsPanel != null)
                    {
                        UpdateReservationStats(statsPanel);
                    }
                    
                    // Refresh the data grid
                    DataGridView dgv = pnlMainContent.Controls.OfType<DataGridView>().FirstOrDefault();
                    if (dgv != null)
                    {
                        LoadReservationsData(dgv);
                    }
                    else
                    {
                        ShowReservationsView();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating reservation: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            rvForm.Controls.Add(btnCancel);
             rvForm.Controls.Add(btnConfirm);

            // Add dropdown panels last so they appear on top
            rvForm.Controls.Add(pnlMemberDropdown);
            rvForm.Controls.Add(pnlBookDropdown);

            // Ensure dropdowns are hidden when form is shown
            rvForm.Shown += (s, e) =>
            {
                pnlMemberDropdown.Visible = false;
                pnlBookDropdown.Visible = false;
            };

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
            int selectedMemberId = 0;

            // Member Select
            Label lblMem = new Label { Text = "Select Member", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 50), Location = new Point(30, y), AutoSize = true };
            fineForm.Controls.Add(lblMem);
            
            Panel pnlMem = new Panel 
            { 
                Location = new Point(30, y + 25), 
                Size = new Size(390, 45), 
                BackColor = Color.White,
                Tag = false
            };
            
            TextBox txtMemberSearch = new TextBox
            {
                Location = new Point(12, 12),
                Size = new Size(350, 20),
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            txtMemberSearch.SetPlaceholder("Choose a member...");
            
            // Member dropdown panel
            Panel pnlMemberDropdown = new Panel
            {
                Location = new Point(30, y + 25 + 45 + 2),
                Size = new Size(390, 0),
                BackColor = Color.White,
                Visible = false,
                BorderStyle = BorderStyle.None
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
            
            ListBox lstMembers = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5F),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60),
                ItemHeight = 22
            };
            
            lstMembers.MouseDown += (s, e) =>
            {
                int index = lstMembers.IndexFromPoint(e.Location);
                if (index >= 0)
                {
                    lstMembers.SelectedIndex = index;
                }
            };
            
            pnlMemberDropdown.Controls.Add(lstMembers);
            
            // Load members from database
            MemberService memberService = new MemberService();
            List<MemberData> allMembers = new List<MemberData>();
            var filteredMembers = new List<MemberData>();
            
            Action refreshMemberData = () =>
            {
                try
                {
                    allMembers = memberService.GetAllMembers().Where(m => m.Status == 1).ToList();
                    filteredMembers = new List<MemberData>(allMembers);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading members: {ex.Message}");
                    allMembers = new List<MemberData>();
                    filteredMembers = new List<MemberData>();
                }
            };
            
            refreshMemberData();
            
            Action updateMemberList = () =>
            {
                lstMembers.Items.Clear();
                if (filteredMembers == null || filteredMembers.Count == 0)
                {
                    lstMembers.Items.Add("No active members found");
                }
                else
                {
                    foreach (var member in filteredMembers)
                    {
                        lstMembers.Items.Add($"{member.FirstName} {member.LastName} ({member.MemberNumber})");
                    }
                }
                lstMembers.Visible = true;
                lstMembers.Refresh();
                pnlMemberDropdown.Refresh();
            };
            
            updateMemberList();
            
            txtMemberSearch.TextChanged += (s, e) =>
            {
                string searchText = txtMemberSearch.Text.ToLower();
                if (txtMemberSearch.Tag is PlaceholderTextHelper.PlaceholderData placeholderData)
                {
                    if (searchText == placeholderData.PlaceholderText.ToLower())
                    {
                        searchText = "";
                    }
                }
                
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    filteredMembers = new List<MemberData>(allMembers);
                }
                else
                {
                    filteredMembers = allMembers.Where(m =>
                        m.MemberNumber.ToLower().Contains(searchText) ||
                        m.FirstName.ToLower().Contains(searchText) ||
                        m.LastName.ToLower().Contains(searchText) ||
                        m.Email.ToLower().Contains(searchText)
                    ).ToList();
                }
                updateMemberList();
            };
            
            lstMembers.SelectedIndexChanged += (s, e) =>
            {
                if (lstMembers.SelectedIndex >= 0 && lstMembers.SelectedIndex < filteredMembers.Count)
                {
                    var member = filteredMembers[lstMembers.SelectedIndex];
                    selectedMemberId = member.MemberId;
                    txtMemberSearch.Text = $"{member.FirstName} {member.LastName} ({member.MemberNumber})";
                    txtMemberSearch.ForeColor = Color.Black;
                    pnlMemberDropdown.Visible = false;
                }
            };
            
            Label lblMemberArrow = new Label
            {
                Text = "▼",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(370, 13),
                Size = new Size(25, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            
            Action showMemberDropdown = () =>
            {
                refreshMemberData();
                updateMemberList();
                int itemCount = lstMembers.Items.Count;
                int height = itemCount > 0 ? Math.Min(150, itemCount * 22 + 10) : 40;
                
                pnlMemberDropdown.Location = new Point(30, y + 25 + 45 + 2);
                pnlMemberDropdown.Width = 390;
                pnlMemberDropdown.Height = height;
                pnlMemberDropdown.Visible = true;
                pnlMemberDropdown.BringToFront();
                pnlMemberDropdown.Invalidate();
                
                lstMembers.Visible = true;
                lstMembers.Height = height;
                lstMembers.Refresh();
                
                fineForm.Invalidate();
                fineForm.Update();
            };
            
            lblMemberArrow.Click += (s, e) => 
            { 
                txtMemberSearch.Focus();
                showMemberDropdown();
            };
            
            txtMemberSearch.Enter += (s, e) =>
            {
                pnlMem.Tag = true;
                pnlMem.Invalidate();
                showMemberDropdown();
            };
            
            txtMemberSearch.Leave += (s, e) =>
            {
                pnlMem.Tag = false;
                pnlMem.Invalidate();
                System.Threading.Thread.Sleep(200);
                pnlMemberDropdown.Visible = false;
            };
            
            txtMemberSearch.Click += (s, e) =>
            {
                showMemberDropdown();
            };
            
            pnlMem.Controls.AddRange(new Control[] { txtMemberSearch, lblMemberArrow });
            fineForm.Controls.Add(pnlMem);
            
            pnlMem.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool isFocused = (bool)pnlMem.Tag;
                Color borderColor = isFocused ? Color.Maroon : Color.FromArgb(120, 0, 0); 
                using(GraphicsPath path = CreateRoundedRectangle(new Rectangle(1, 1, pnlMem.Width - 3, pnlMem.Height - 3), 8))
                using(Pen pen = new Pen(borderColor, 1.5f)) e.Graphics.DrawPath(pen, path);
            };
            
            // Add dropdown panel last so it appears on top
            fineForm.Controls.Add(pnlMemberDropdown);

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
            
            btnConfirm.Click += (s, e) =>
            {
                if (selectedMemberId == 0)
                {
                    MessageBox.Show("Please select a member.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMemberSearch.Focus();
                    return;
                }
                
                // Validate amount
                if (!decimal.TryParse(txtAmt.Text, out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("Please enter a valid amount greater than 0.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtAmt.Focus();
                    return;
                }
                
                // Get form values
                string fineType = cmbType.SelectedItem?.ToString() ?? "Other";
                string bookTitle = txtBk.Text?.Trim() ?? "";
                if (txtBk.Tag is PlaceholderTextHelper.PlaceholderData placeholderData && bookTitle == placeholderData.PlaceholderText)
                {
                    bookTitle = "";
                }
                string notes = txtNotes.Text?.Trim() ?? "";
                if (txtNotes.Tag is PlaceholderTextHelper.PlaceholderData notesPlaceholder && notes == notesPlaceholder.PlaceholderText)
                {
                    notes = "";
                }
                
                // Build reason string
                string reason = fineType;
                if (!string.IsNullOrWhiteSpace(bookTitle))
                {
                    reason += $" - {bookTitle}";
                }
                if (!string.IsNullOrWhiteSpace(notes))
                {
                    reason += $" ({notes})";
                }
                
                try
                {
                    // Ensure Fines table allows NULL for BorrowingId
                    EnsureFinesTableAllowsNullBorrowingId();
                    
                    using (var connection = Helper.MYSqlHelper.CreateConnection())
                    {
                        // Get BorrowingId if book title is provided (optional)
                        int? borrowingId = null;
                        if (!string.IsNullOrWhiteSpace(bookTitle))
                        {
                            string borrowingQuery = @"
                                SELECT b.BorrowingId 
                                FROM Borrowings b
                                INNER JOIN Books bk ON b.BookId = bk.BookId
                                INNER JOIN Members m ON b.MemberId = m.MemberId
                                WHERE m.MemberId = @MemberId 
                                AND bk.Title LIKE @BookTitle 
                                AND b.ReturnDate IS NULL
                                LIMIT 1";
                            
                            using (var borrowingCmd = new MySqlCommand(borrowingQuery, connection))
                            {
                                borrowingCmd.Parameters.AddWithValue("@MemberId", selectedMemberId);
                                borrowingCmd.Parameters.AddWithValue("@BookTitle", $"%{bookTitle}%");
                                object result = borrowingCmd.ExecuteScalar();
                                if (result != null && result != DBNull.Value)
                                {
                                    borrowingId = Convert.ToInt32(result);
                                }
                            }
                        }
                        
                        // Insert fine into database - use conditional INSERT based on whether BorrowingId exists
                        string insertQuery;
                        MySqlCommand insertCmd;
                        
                        if (borrowingId.HasValue)
                        {
                            insertQuery = @"
                                INSERT INTO Fines (BorrowingId, MemberId, Amount, Reason, Status, CreatedDate)
                                VALUES (@BorrowingId, @MemberId, @Amount, @Reason, 'Unpaid', NOW())";
                            insertCmd = new MySqlCommand(insertQuery, connection);
                            insertCmd.Parameters.AddWithValue("@BorrowingId", borrowingId.Value);
                        }
                        else
                        {
                            insertQuery = @"
                                INSERT INTO Fines (MemberId, Amount, Reason, Status, CreatedDate)
                                VALUES (@MemberId, @Amount, @Reason, 'Unpaid', NOW())";
                            insertCmd = new MySqlCommand(insertQuery, connection);
                        }
                        
                        insertCmd.Parameters.AddWithValue("@MemberId", selectedMemberId);
                        insertCmd.Parameters.AddWithValue("@Amount", amount);
                        insertCmd.Parameters.AddWithValue("@Reason", reason);
                        
                        insertCmd.ExecuteNonQuery();
                        
                        MessageBox.Show("Fine added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        fineForm.DialogResult = DialogResult.OK;
                        
                        // Refresh fines view BEFORE closing
                        RefreshFinesView();
                        
                        fineForm.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating fine: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        
        private int GetDaysFromTimeRange(ComboBox cmbDate)
        {
            string selected = cmbDate.SelectedItem?.ToString() ?? "📅  Last 7 days";
            if (selected.Contains("7 days")) return 7;
            if (selected.Contains("14 days")) return 14;
            if (selected.Contains("30 days")) return 30;
            if (selected.Contains("90 days")) return 90;
            return 7; // Default
        }
        
        private DateTime GetStartDateFromTimeRange(ComboBox cmbDate)
        {
            int days = GetDaysFromTimeRange(cmbDate);
            return DateTime.Now.AddDays(-days);
        }
        
        private void PrintCurrentReport(ComboBox cmbDate)
        {
            try
            {
                // Get current active tab
                string activeTab = "Circulation";
                if (pnlReportsTabs != null)
                {
                    foreach (Control ctrl in pnlReportsTabs.Controls)
                    {
                        if (ctrl is Button btn && btn.Font.Bold)
                        {
                            activeTab = btn.Tag?.ToString() ?? btn.Text;
                            break;
                        }
                    }
                }
                
                // Create print document
                PrintDialog printDialog = new PrintDialog();
                PrintDocument printDoc = new PrintDocument();
                
                printDoc.PrintPage += (s, e) =>
                {
                    PrintReportPage(e, activeTab, cmbDate);
                };
                
                printDialog.Document = printDoc;
                
                if (printDialog.ShowDialog() == DialogResult.OK)
                {
                    printDoc.Print();
                    MessageBox.Show("Report printed successfully!", "Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing report: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void PrintReportPage(PrintPageEventArgs e, string reportType, ComboBox cmbDate)
        {
            Font titleFont = new Font("Arial", 16, FontStyle.Bold);
            Font headerFont = new Font("Arial", 12, FontStyle.Bold);
            Font bodyFont = new Font("Arial", 10);
            
            float yPos = 50;
            float leftMargin = 50;
            float rightMargin = e.MarginBounds.Right;
            
            // Title
            e.Graphics.DrawString($"Library Management System - {reportType} Report", titleFont, Brushes.Black, leftMargin, yPos);
            yPos += 30;
            
            // Time range
            string timeRange = cmbDate.SelectedItem?.ToString() ?? "📅  Last 7 days";
            e.Graphics.DrawString($"Period: {timeRange.Replace("📅", "").Trim()}", bodyFont, Brushes.Black, leftMargin, yPos);
            yPos += 20;
            e.Graphics.DrawString($"Generated: {DateTime.Now:MMMM dd, yyyy 'at' HH:mm:ss}", bodyFont, Brushes.Black, leftMargin, yPos);
            yPos += 30;
            
            // Draw line
            e.Graphics.DrawLine(new Pen(Color.Black, 1), leftMargin, yPos, rightMargin, yPos);
            yPos += 20;
            
            // Report content based on type
            DateTime startDate = GetStartDateFromTimeRange(cmbDate);
            
            using (var connection = Helper.MYSqlHelper.CreateConnection())
            {
                switch (reportType)
                {
                    case "Circulation":
                        PrintCirculationReport(e, connection, startDate, leftMargin, ref yPos, headerFont, bodyFont);
                        break;
                    case "Members":
                        PrintMembersReport(e, connection, startDate, leftMargin, ref yPos, headerFont, bodyFont);
                        break;
                    case "Collection":
                        PrintCollectionReport(e, connection, startDate, leftMargin, ref yPos, headerFont, bodyFont);
                        break;
                    case "Fines":
                        PrintFinesReport(e, connection, startDate, leftMargin, ref yPos, headerFont, bodyFont);
                        break;
                }
            }
        }
        
        private void PrintCirculationReport(PrintPageEventArgs e, MySqlConnection connection, DateTime startDate, float leftMargin, ref float yPos, Font headerFont, Font bodyFont)
        {
            e.Graphics.DrawString("Circulation Statistics", headerFont, Brushes.Black, leftMargin, yPos);
            yPos += 25;
            
            string query = @"
                SELECT 
                    COUNT(*) AS TotalBorrowings,
                    SUM(CASE WHEN ReturnDate IS NULL AND DueDate < NOW() THEN 1 ELSE 0 END) AS Overdue,
                    SUM(CASE WHEN DATE(ReturnDate) = CURDATE() THEN 1 ELSE 0 END) AS ReturnedToday
                FROM Borrowings
                WHERE BorrowDate >= @StartDate";
            
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@StartDate", startDate);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        e.Graphics.DrawString($"Total Borrowings: {reader.GetInt32("TotalBorrowings")}", bodyFont, Brushes.Black, leftMargin, yPos);
                        yPos += 20;
                        e.Graphics.DrawString($"Overdue Books: {reader.GetInt32("Overdue")}", bodyFont, Brushes.Black, leftMargin, yPos);
                        yPos += 20;
                        e.Graphics.DrawString($"Returned Today: {reader.GetInt32("ReturnedToday")}", bodyFont, Brushes.Black, leftMargin, yPos);
                        yPos += 30;
                    }
                }
            }
        }
        
        private void PrintMembersReport(PrintPageEventArgs e, MySqlConnection connection, DateTime startDate, float leftMargin, ref float yPos, Font headerFont, Font bodyFont)
        {
            e.Graphics.DrawString("Members Statistics", headerFont, Brushes.Black, leftMargin, yPos);
            yPos += 25;
            
            string query = @"
                SELECT 
                    COUNT(*) AS TotalMembers,
                    SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS ActiveMembers,
                    SUM(CASE WHEN RegistrationDate >= @StartDate THEN 1 ELSE 0 END) AS NewMembers
                FROM Members";
            
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@StartDate", startDate);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        e.Graphics.DrawString($"Total Members: {reader.GetInt32("TotalMembers")}", bodyFont, Brushes.Black, leftMargin, yPos);
                        yPos += 20;
                        e.Graphics.DrawString($"Active Members: {reader.GetInt32("ActiveMembers")}", bodyFont, Brushes.Black, leftMargin, yPos);
                        yPos += 20;
                        e.Graphics.DrawString($"New Members: {reader.GetInt32("NewMembers")}", bodyFont, Brushes.Black, leftMargin, yPos);
                        yPos += 30;
                    }
                }
            }
        }
        
        private void PrintCollectionReport(PrintPageEventArgs e, MySqlConnection connection, DateTime startDate, float leftMargin, ref float yPos, Font headerFont, Font bodyFont)
        {
            e.Graphics.DrawString("Collection Statistics", headerFont, Brushes.Black, leftMargin, yPos);
            yPos += 25;
            
            string query = @"
                SELECT 
                    COUNT(DISTINCT BookId) AS TotalTitles,
                    COUNT(*) AS TotalCopies,
                    SUM(CASE WHEN Status = 'Available' THEN 1 ELSE 0 END) AS Available
                FROM BookCopies";
            
            using (var cmd = new MySqlCommand(query, connection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        e.Graphics.DrawString($"Total Titles: {reader.GetInt32("TotalTitles")}", bodyFont, Brushes.Black, leftMargin, yPos);
                        yPos += 20;
                        e.Graphics.DrawString($"Total Copies: {reader.GetInt32("TotalCopies")}", bodyFont, Brushes.Black, leftMargin, yPos);
                        yPos += 20;
                        e.Graphics.DrawString($"Available Copies: {reader.GetInt32("Available")}", bodyFont, Brushes.Black, leftMargin, yPos);
                        yPos += 30;
                    }
                }
            }
        }
        
        private void PrintFinesReport(PrintPageEventArgs e, MySqlConnection connection, DateTime startDate, float leftMargin, ref float yPos, Font headerFont, Font bodyFont)
        {
            e.Graphics.DrawString("Fines Statistics", headerFont, Brushes.Black, leftMargin, yPos);
            yPos += 25;
            
            EnsurePaidAmountColumnExists(connection);
            
            string query = @"
                SELECT 
                    COUNT(*) AS TotalFines,
                    SUM(f.Amount) AS TotalAmount,
                    SUM(CASE WHEN f.Status = 'Paid' THEN COALESCE(f.PaidAmount, f.Amount) ELSE 0 END) AS Collected,
                    SUM(CASE WHEN f.Status = 'Unpaid' THEN f.Amount - COALESCE(f.PaidAmount, 0) ELSE 0 END) AS Pending,
                    SUM(CASE WHEN f.Status = 'Waived' THEN f.Amount ELSE 0 END) AS Waived
                FROM Fines f
                WHERE f.CreatedDate >= @StartDate";
            
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@StartDate", startDate);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        e.Graphics.DrawString($"Total Fines: {reader.GetInt32("TotalFines")}", bodyFont, Brushes.Black, leftMargin, yPos);
                        yPos += 20;
                        e.Graphics.DrawString($"Total Amount: ₱{reader.GetDecimal("TotalAmount"):N2}", bodyFont, Brushes.Black, leftMargin, yPos);
                        yPos += 20;
                        e.Graphics.DrawString($"Collected: ₱{reader.GetDecimal("Collected"):N2}", bodyFont, Brushes.Black, leftMargin, yPos);
                        yPos += 20;
                        e.Graphics.DrawString($"Pending: ₱{reader.GetDecimal("Pending"):N2}", bodyFont, Brushes.Black, leftMargin, yPos);
                        yPos += 20;
                        e.Graphics.DrawString($"Waived: ₱{reader.GetDecimal("Waived"):N2}", bodyFont, Brushes.Black, leftMargin, yPos);
                        yPos += 30;
                    }
                }
            }
        }
        
        private void ExportCurrentReport(ComboBox cmbDate)
        {
            try
            {
                // Get current active tab
                string activeTab = "Circulation";
                if (pnlReportsTabs != null)
                {
                    foreach (Control ctrl in pnlReportsTabs.Controls)
                    {
                        if (ctrl is Button btn && btn.Font.Bold)
                        {
                            activeTab = btn.Tag?.ToString() ?? btn.Text;
                            break;
                        }
                    }
                }
                
                // Show save dialog
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    FileName = $"{activeTab}Report_{DateTime.Now:yyyyMMdd_HHmmss}",
                    Title = $"Export {activeTab} Report"
                };
                
                if (saveDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                
                string filePath = saveDialog.FileName;
                string extension = Path.GetExtension(filePath).ToLower();
                DateTime startDate = GetStartDateFromTimeRange(cmbDate);
                
                using (var connection = Helper.MYSqlHelper.CreateConnection())
                {
                    if (extension == ".csv")
                    {
                        ExportReportToCSV(connection, filePath, activeTab, startDate);
                    }
                    else
                    {
                        ExportReportToText(connection, filePath, activeTab, startDate);
                    }
                }
                
                MessageBox.Show($"{activeTab} report exported successfully!\n\nSaved to: {filePath}", "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting report: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void ExportReportToCSV(MySqlConnection connection, string filePath, string reportType, DateTime startDate)
        {
            using (var writer = new StreamWriter(filePath))
            {
                switch (reportType)
                {
                    case "Circulation":
                        writer.WriteLine("BorrowingId,MemberName,BookTitle,BorrowDate,DueDate,ReturnDate,Status,FineAmount");
                        string circQuery = @"
                            SELECT 
                                b.BorrowingId,
                                CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
                                bk.Title AS BookTitle,
                                b.BorrowDate,
                                b.DueDate,
                                b.ReturnDate,
                                b.Status,
                                COALESCE(b.FineAmount, 0) AS FineAmount
                            FROM Borrowings b
                            INNER JOIN Members m ON b.MemberId = m.MemberId
                            INNER JOIN Users u ON m.UserId = u.UserId
                            INNER JOIN Books bk ON b.BookId = bk.BookId
                            WHERE b.BorrowDate >= @StartDate
                            ORDER BY b.BorrowDate DESC";
                        ExportQueryToCSV(connection, writer, circQuery, startDate);
                        break;
                    case "Members":
                        writer.WriteLine("MemberId,MemberNumber,FirstName,LastName,Email,Phone,MemberType,Status,RegistrationDate");
                        string memQuery = @"
                            SELECT 
                                m.MemberId,
                                m.MemberNumber,
                                u.FirstName,
                                u.LastName,
                                u.Email,
                                m.Phone,
                                m.MemberType,
                                CASE WHEN m.Status = 1 THEN 'Active' ELSE 'Inactive' END AS Status,
                                m.RegistrationDate
                            FROM Members m
                            INNER JOIN Users u ON m.UserId = u.UserId
                            WHERE m.RegistrationDate >= @StartDate
                            ORDER BY m.RegistrationDate DESC";
                        ExportQueryToCSV(connection, writer, memQuery, startDate);
                        break;
                    case "Collection":
                        writer.WriteLine("BookId,Title,Author,ISBN,Category,TotalCopies,AvailableCopies,Status");
                        string collQuery = @"
                            SELECT 
                                b.BookId,
                                b.Title,
                                b.Author,
                                COALESCE(b.ISBN, '') AS ISBN,
                                COALESCE(b.Category, '') AS Category,
                                b.TotalCopies,
                                b.AvailableCopies,
                                CASE WHEN b.AvailableCopies > 0 THEN 'Available' ELSE 'Unavailable' END AS Status
                            FROM Books b
                            ORDER BY b.Title";
                        ExportQueryToCSV(connection, writer, collQuery, DateTime.MinValue);
                        break;
                    case "Fines":
                        EnsurePaidAmountColumnExists(connection);
                        writer.WriteLine("FineId,MemberName,Book/Reason,Type,Amount,PaidAmount,Status,Date,Reason");
                        string finesQuery = @"
                            SELECT 
                                f.FineId,
                                COALESCE(CONCAT(u.FirstName, ' ', u.LastName), m.MemberNumber, 'Unknown Member') AS MemberName,
                                CASE 
                                    WHEN bk.Title IS NOT NULL THEN bk.Title
                                    WHEN f.Reason IS NOT NULL AND f.Reason != '' THEN f.Reason
                                    ELSE 'N/A'
                                END AS BookReason,
                                CASE 
                                    WHEN f.Reason LIKE '%Lost%' OR f.Reason LIKE '%lost%' THEN 'Lost'
                                    WHEN f.Reason LIKE '%Damaged%' OR f.Reason LIKE '%damaged%' THEN 'Damaged'
                                    WHEN f.Reason LIKE '%Overdue%' OR f.Reason LIKE '%overdue%' OR f.Reason IS NULL THEN 'Overdue'
                                    ELSE 'Other'
                                END AS Type,
                                f.Amount,
                                COALESCE(f.PaidAmount, 0) AS PaidAmount,
                                CASE 
                                    WHEN f.Status = 'Unpaid' THEN 'Pending'
                                    ELSE f.Status
                                END AS Status,
                                COALESCE(f.PaidDate, br.DueDate, f.CreatedDate) AS FineDate,
                                COALESCE(f.Reason, 'N/A') AS Reason
                            FROM Fines f
                            INNER JOIN Members m ON f.MemberId = m.MemberId
                            LEFT JOIN Users u ON m.UserId = u.UserId
                            LEFT JOIN Borrowings br ON f.BorrowingId = br.BorrowingId
                            LEFT JOIN Books bk ON br.BookId = bk.BookId
                            WHERE f.CreatedDate >= @StartDate
                            ORDER BY f.CreatedDate DESC";
                        ExportQueryToCSV(connection, writer, finesQuery, startDate);
                        break;
                }
            }
        }
        
        private void ExportQueryToCSV(MySqlConnection connection, StreamWriter writer, string query, DateTime startDate)
        {
            using (var cmd = new MySqlCommand(query, connection))
            {
                if (query.Contains("@StartDate"))
                {
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                }
                
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        List<string> values = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (reader.IsDBNull(i))
                            {
                                values.Add("");
                            }
                            else
                            {
                                object value = reader.GetValue(i);
                                if (value is DateTime dt)
                                {
                                    values.Add(dt.ToString("yyyy-MM-dd HH:mm:ss"));
                                }
                                else
                                {
                                    values.Add($"\"{EscapeCSV(value.ToString())}\"");
                                }
                            }
                        }
                        writer.WriteLine(string.Join(",", values));
                    }
                }
            }
        }
        
        private void ExportReportToText(MySqlConnection connection, string filePath, string reportType, DateTime startDate)
        {
            StringBuilder report = new StringBuilder();
            
            report.AppendLine("=".PadRight(80, '='));
            report.AppendLine($"LIBRARY MANAGEMENT SYSTEM - {reportType.ToUpper()} REPORT");
            report.AppendLine("=".PadRight(80, '='));
            report.AppendLine($"Generated: {DateTime.Now:MMMM dd, yyyy 'at' HH:mm:ss}");
            
            // Calculate days from start date
            int days = (DateTime.Now - startDate).Days;
            report.AppendLine($"Period: Last {days} days");
            report.AppendLine();
            
            // Add report-specific content
            switch (reportType)
            {
                case "Circulation":
                    ExportCirculationToText(connection, report, startDate);
                    break;
                case "Members":
                    ExportMembersToText(connection, report, startDate);
                    break;
                case "Collection":
                    ExportCollectionToText(connection, report);
                    break;
                case "Fines":
                    ExportFinesToText(connection, report, startDate);
                    break;
            }
            
            File.WriteAllText(filePath, report.ToString());
        }
        
        private void ExportCirculationToText(MySqlConnection connection, StringBuilder report, DateTime startDate)
        {
            report.AppendLine("CIRCULATION STATISTICS");
            report.AppendLine("-".PadRight(80, '-'));
            
            string query = @"
                SELECT 
                    COUNT(*) AS TotalBorrowings,
                    SUM(CASE WHEN ReturnDate IS NULL AND DueDate < NOW() THEN 1 ELSE 0 END) AS Overdue,
                    SUM(CASE WHEN DATE(ReturnDate) = CURDATE() THEN 1 ELSE 0 END) AS ReturnedToday
                FROM Borrowings
                WHERE BorrowDate >= @StartDate";
            
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@StartDate", startDate);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        report.AppendLine($"Total Borrowings:     {reader.GetInt32("TotalBorrowings"),10:N0}");
                        report.AppendLine($"Overdue Books:        {reader.GetInt32("Overdue"),10:N0}");
                        report.AppendLine($"Returned Today:       {reader.GetInt32("ReturnedToday"),10:N0}");
                    }
                }
            }
        }
        
        private void ExportMembersToText(MySqlConnection connection, StringBuilder report, DateTime startDate)
        {
            report.AppendLine("MEMBERS STATISTICS");
            report.AppendLine("-".PadRight(80, '-'));
            
            string query = @"
                SELECT 
                    COUNT(*) AS TotalMembers,
                    SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS ActiveMembers,
                    SUM(CASE WHEN RegistrationDate >= @StartDate THEN 1 ELSE 0 END) AS NewMembers
                FROM Members";
            
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@StartDate", startDate);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        report.AppendLine($"Total Members:         {reader.GetInt32("TotalMembers"),10:N0}");
                        report.AppendLine($"Active Members:        {reader.GetInt32("ActiveMembers"),10:N0}");
                        report.AppendLine($"New Members:          {reader.GetInt32("NewMembers"),10:N0}");
                    }
                }
            }
        }
        
        private void ExportCollectionToText(MySqlConnection connection, StringBuilder report)
        {
            report.AppendLine("COLLECTION STATISTICS");
            report.AppendLine("-".PadRight(80, '-'));
            
            string query = @"
                SELECT 
                    COUNT(DISTINCT BookId) AS TotalTitles,
                    COUNT(*) AS TotalCopies,
                    SUM(CASE WHEN Status = 'Available' THEN 1 ELSE 0 END) AS Available
                FROM BookCopies";
            
            using (var cmd = new MySqlCommand(query, connection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        report.AppendLine($"Total Titles:          {reader.GetInt32("TotalTitles"),10:N0}");
                        report.AppendLine($"Total Copies:          {reader.GetInt32("TotalCopies"),10:N0}");
                        report.AppendLine($"Available Copies:      {reader.GetInt32("Available"),10:N0}");
                    }
                }
            }
        }
        
        private void ExportFinesToText(MySqlConnection connection, StringBuilder report, DateTime startDate)
        {
            report.AppendLine("FINES STATISTICS");
            report.AppendLine("-".PadRight(80, '-'));
            
            EnsurePaidAmountColumnExists(connection);
            
            string query = @"
                SELECT 
                    COUNT(*) AS TotalFines,
                    SUM(f.Amount) AS TotalAmount,
                    SUM(CASE WHEN f.Status = 'Paid' THEN COALESCE(f.PaidAmount, f.Amount) ELSE 0 END) AS Collected,
                    SUM(CASE WHEN f.Status = 'Unpaid' THEN f.Amount - COALESCE(f.PaidAmount, 0) ELSE 0 END) AS Pending,
                    SUM(CASE WHEN f.Status = 'Waived' THEN f.Amount ELSE 0 END) AS Waived
                FROM Fines f
                WHERE f.CreatedDate >= @StartDate";
            
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@StartDate", startDate);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        report.AppendLine($"Total Fines:           {reader.GetInt32("TotalFines"),10:N0}");
                        report.AppendLine($"Total Amount:          ₱{reader.GetDecimal("TotalAmount"),9:N2}");
                        report.AppendLine($"Collected:             ₱{reader.GetDecimal("Collected"),9:N2}");
                        report.AppendLine($"Pending:               ₱{reader.GetDecimal("Pending"),9:N2}");
                        report.AppendLine($"Waived:                ₱{reader.GetDecimal("Waived"),9:N2}");
                    }
                }
            }
            
            // Add detailed fines list
            report.AppendLine();
            report.AppendLine("DETAILED FINES LIST");
            report.AppendLine("-".PadRight(80, '-'));
            report.AppendLine($"{"Member",-25} {"Book/Reason",-30} {"Amount",12} {"Paid",12} {"Status",-10} {"Date",-12}");
            report.AppendLine("-".PadRight(80, '-'));
            
            string detailsQuery = @"
                SELECT 
                    COALESCE(CONCAT(u.FirstName, ' ', u.LastName), m.MemberNumber, 'Unknown') AS MemberName,
                    CASE 
                        WHEN bk.Title IS NOT NULL THEN LEFT(bk.Title, 28)
                        WHEN f.Reason IS NOT NULL AND f.Reason != '' THEN LEFT(f.Reason, 28)
                        ELSE 'N/A'
                    END AS BookReason,
                    f.Amount,
                    COALESCE(f.PaidAmount, 0) AS PaidAmount,
                    CASE 
                        WHEN f.Status = 'Unpaid' THEN 'Pending'
                        ELSE f.Status
                    END AS Status,
                    DATE_FORMAT(COALESCE(f.PaidDate, br.DueDate, f.CreatedDate), '%Y-%m-%d') AS FineDate
                FROM Fines f
                INNER JOIN Members m ON f.MemberId = m.MemberId
                LEFT JOIN Users u ON m.UserId = u.UserId
                LEFT JOIN Borrowings br ON f.BorrowingId = br.BorrowingId
                LEFT JOIN Books bk ON br.BookId = bk.BookId
                WHERE f.CreatedDate >= @StartDate
                ORDER BY f.CreatedDate DESC
                LIMIT 100";
            
            using (var cmd = new MySqlCommand(detailsQuery, connection))
            {
                cmd.Parameters.AddWithValue("@StartDate", startDate);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string member = reader.GetString("MemberName");
                        string book = reader.GetString("BookReason");
                        decimal amount = reader.GetDecimal("Amount");
                        decimal paid = reader.GetDecimal("PaidAmount");
                        string status = reader.GetString("Status");
                        string date = reader.GetString("FineDate");
                        
                        report.AppendLine($"{member,-25} {book,-30} ₱{amount,10:N2} ₱{paid,10:N2} {status,-10} {date,-12}");
                    }
                }
            }
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

    // Note: Dialog classes (CheckOutBookDialog, ReturnBookDialog, RecordPaymentDialog, 
    // WaiveFineDialog, EditBookCopyDialog) are defined in separate files in the Dashboard folder.
    // They can be accessed directly by their class names.
}


