    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.IO;
    using System.Linq;
    using System.Web;
    using System.Windows.Forms;
    using System.Windows.Forms.DataVisualization.Charting;
using Library_Management_System.Helper;
using Library_Management_System.Models;
using Library_Management_System.Service;
using static Library_Management_System.Helper.PlaceholderTextHelper;
namespace Library_Management_System.Forms.staff
{
    partial class StaffDashboard : Form
    {
        private bool _isLoadingMembersData = false;
        private bool _isProcessingAction = false;
        private System.Windows.Forms.Timer _sessionTimer;
        private const int SessionTimeoutMinutes = 30; 
        private Panel pnlReportsView;
        private Panel pnlReportsSubNav;
        private Panel pnlReportsContent;
        private Button btnReportCirculation;
        private Button btnReportMembers;
        private Button btnReportCollection;
        private Button btnReportFines;
        private Panel pnlSearchView;
        private Panel pnlSearchContent;
        private TextBox txtSearchInput;
        private Button btnTriggerSearch;
        private Button btnSearchFilters;
        private List<Control> _originalMainContentControls = new List<Control>();
        private SearchService _searchService;
        public StaffDashboard()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to initialize dashboard components. Please restart the application.\n\nIf the problem persists, contact system administrator.",
                    "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"Dashboard initialization error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
            try
            {
                InitializeDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to set up dashboard. Please try again or contact system administrator.",
                    "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"Dashboard setup error: {ex.Message}\n{ex.StackTrace}");
            }
        }
        private void InitializeDashboard()
        {
            this.WindowState = FormWindowState.Normal;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1400, 700);
            this.MinimumSize = new Size(1200, 600);
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            SetFormIcon();
            this.Resize += DashboardForm_Resize;
            InitializeSessionTimeout();
            StoreOriginalControls();
            ApplyModernDashboardStyling();
            ResetMenuHighlights();
            txtSearchMembers.SetPlaceholder("Search members by name, email, or ID...");
            LoadUserInfo();
            string logoPath = System.IO.Path.Combine(Application.StartupPath, "Resources", "images-removebg-preview.png");
            if (System.IO.File.Exists(logoPath))
            {
                picLogo.Image = Image.FromFile(logoPath);
            }
            LoadDashboardData();
            SetupCardStyling();
            SetupMenuButtonHoverEffects();
            SetupSidebarStyling();
            SetupMembersView();
            SetupReportsView();
            _searchService = new SearchService();
            SetupSearchView();
            this.Load += (s, e) => {
                if (pnlMainContent.Controls.Contains(pnlCardTotalBooks))
                {
                    AdjustDashboardCards();
                    AdjustDashboardText();
                }
            };
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
                if (!_originalMainContentControls.Contains(ctrl))
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
                if (!_originalMainContentControls.Contains(ctrl))
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
        private void InitializeSessionTimeout()
        {
            _sessionTimer = new System.Windows.Forms.Timer();
            _sessionTimer.Interval = SessionTimeoutMinutes * 60 * 1000; 
            _sessionTimer.Tick += SessionTimer_Tick;
            this.MouseMove += ResetSessionTimer;
            this.KeyPress += ResetSessionTimer;
            this.MouseClick += ResetSessionTimer;
            _sessionTimer.Start();
        }
        private void ResetSessionTimer(object sender, EventArgs e)
        {
            _sessionTimer.Stop();
            _sessionTimer.Start();
        }
        private void SessionTimer_Tick(object sender, EventArgs e)
        {
            _sessionTimer.Stop();
            MessageBox.Show($"Your session has expired due to inactivity. You will be logged out for security reasons.",
                "Session Expired", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            PerformLogout();
        }
        private void PerformLogout()
        {
            try
            {
                string userEmail = SiginForm.CurrentUser?.Email ?? "Unknown";
                Library_Management_System.Helper.AuditLogger.LogLogout(userEmail, "SESSION_TIMEOUT");
                SiginForm.ClearCurrentUser();
                this.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during logout: {ex.Message}");
                Application.Exit();
            }
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

            // Add Header Panel for White Background
            Panel staffMembersHeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 140, 
                BackColor = Color.White
            };
            pnlMembersView.Controls.Add(staffMembersHeaderPanel);

            // Move Title and Subtitle to Header Panel
            if (pnlMembersView.Controls.Contains(lblMembersTitle))
            {
                pnlMembersView.Controls.Remove(lblMembersTitle);
                staffMembersHeaderPanel.Controls.Add(lblMembersTitle);
                lblMembersTitle.Location = new Point(30, 20);
            }
            if (pnlMembersView.Controls.Contains(lblMembersSubtitle))
            {
                pnlMembersView.Controls.Remove(lblMembersSubtitle);
                staffMembersHeaderPanel.Controls.Add(lblMembersSubtitle);
                lblMembersSubtitle.Location = new Point(30, 95);
            }

            // Adjust layouts of other controls to prevent overlap (Shift down to typically 150)
            pnlCardTotalMembers.Top = 150;
            pnlCardActiveMembersStat.Top = 150;
            pnlCardSuspendedMembers.Top = 150;
            pnlCardExpiredMembers.Top = 150;
            pnlCardOverdueBooks.Top = 150;

            pnlSearchFilter.Top = 320;
            
            dgvMembers.Top = 390;
            dgvMembers.Height -= 50; 

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
            bool isHover = button.BackColor == ThemeConstants.GetHoverColor(ThemeConstants.PrimaryMaroon);
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
                        Color.FromArgb(System.Math.Min(255, card.BackColor.R + 30), System.Math.Min(255, card.BackColor.G + 30), System.Math.Min(255, card.BackColor.B + 30)),
                        card.BackColor,
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
            if (SiginForm.IsLoggedIn)
            {
                User currentUser = SiginForm.CurrentUser;
                lblWelcome.Text = $"Welcome back, {currentUser.FirstName}.";
                lblAdminName.Text = currentUser.FirstName;
            }
            else
            {
                lblWelcome.Text = "Welcome back, Admin.";
                lblAdminName.Text = "Admin";
            }
            lblDate.Text = DateTime.Now.ToString("MMM d, yyyy");

            // Fix: Ensure dashboard labels have transparent background to match other sections
            lblWelcome.BackColor = Color.Transparent;
            lblDate.BackColor = Color.Transparent;
        }
        private void LoadDashboardData()
        {
            try
            {
                var dashboardService = new Library_Management_System.Service.DashboardService();
                var stats = dashboardService.GetDashboardStatistics();
                lblTotalBooks.Text = stats.TotalBooks.ToString();
                lblTotalBooksChange.Text = ((Library_Management_System.Service.DashboardService)dashboardService).CalculatePercentageChangeFormatted(stats.TotalBooks, stats.TotalBooksLastWeek);
                AddCardIcon(pnlCardTotalBooks, "📚");
                lblActiveMembers.Text = stats.ActiveMembers.ToString();
                lblActiveMembersChange.Text = ((Library_Management_System.Service.DashboardService)dashboardService).CalculatePercentageChangeFormatted(stats.ActiveMembers, stats.ActiveMembersLastWeek);
                AddCardIcon(pnlCardActiveMembers, "👥");
                lblBooksBorrowed.Text = stats.BooksBorrowed.ToString();
                lblBooksBorrowedChange.Text = ((Library_Management_System.Service.DashboardService)dashboardService).CalculatePercentageChangeFormatted(stats.BooksBorrowed, stats.BooksBorrowedLastWeek);
                AddCardIcon(pnlCardBooksBorrowed, "⇄");
                lblOverdueBooks.Text = stats.OverdueBooks.ToString();
                lblOverdueBooksChange.Text = ((Library_Management_System.Service.DashboardService)dashboardService).CalculatePercentageChangeFormatted(stats.OverdueBooks, stats.OverdueBooksLastWeek);
                AddCardIcon(pnlCardOverdueBooks, "⚠");
                lblTodaysBorrowings.Text = stats.TodaysBorrowings.ToString();
                AddCardIcon(pnlCardTodaysBorrowings, "📚");
                lblTodaysReturns.Text = stats.TodaysReturns.ToString();
                AddCardIcon(pnlCardTodaysReturns, "👥");
                lblPendingFines.Text = $"₱{stats.PendingFines:F2}";
                AddCardIcon(pnlCardPendingFines, "₱");
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
                Font = new Font("Segoe UI", 28F, FontStyle.Regular),
                ForeColor = cardPanel.BackColor == ThemeConstants.PrimaryMaroon ? ThemeConstants.TextWhite : ThemeConstants.PrimaryMaroon,
                Location = new Point(cardPanel.Width - 70, 20),
                Size = new Size(50, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "CardIcon"
            };
            cardPanel.Controls.Add(lblIcon);
        }
        private void ResetMenuHighlights()
        {
            btnDashboard.BackColor = ThemeConstants.SecondaryMaroon;
            btnDashboard.ForeColor = ThemeConstants.TextWhite;
            btnDashboard.Font = ThemeConstants.FontBody;
            btnDashboard.Text = "  🏠 Dashboard";
            btnMembers.BackColor = ThemeConstants.SecondaryMaroon;
            btnMembers.ForeColor = ThemeConstants.TextWhite;
            btnMembers.Font = ThemeConstants.FontBody;
            btnMembers.Text = "  👥 Members";
            btnCatalog.BackColor = ThemeConstants.SecondaryMaroon;
            btnCatalog.ForeColor = ThemeConstants.TextWhite;
            btnCatalog.Font = ThemeConstants.FontBody;
            btnCatalog.Text = "  📖 Catalog";
            btnCirculation.BackColor = ThemeConstants.SecondaryMaroon;
            btnCirculation.ForeColor = ThemeConstants.TextWhite;
            btnCirculation.Font = ThemeConstants.FontBody;
            btnCirculation.Text = "  🔄 Circulation";
            btnFines.BackColor = ThemeConstants.SecondaryMaroon;
            btnFines.ForeColor = ThemeConstants.TextWhite;
            btnFines.Font = ThemeConstants.FontBody;
            btnFines.Text = "  💰 Fines";
            btnInventory.BackColor = ThemeConstants.SecondaryMaroon;
            btnInventory.ForeColor = ThemeConstants.TextWhite;
            btnInventory.Font = ThemeConstants.FontBody;
            btnInventory.Text = "  📦 Inventory";
            btnReservations.BackColor = ThemeConstants.SecondaryMaroon;
            btnReservations.ForeColor = ThemeConstants.TextWhite;
            btnReservations.Font = ThemeConstants.FontBody;
            btnReservations.Text = "  📅 Reservations";
            btnReports.BackColor = ThemeConstants.SecondaryMaroon;
            btnReports.ForeColor = ThemeConstants.TextWhite;
            btnReports.Font = ThemeConstants.FontBody;
            btnReports.Text = "  📊 Reports";
            btnSearch.BackColor = ThemeConstants.SecondaryMaroon;
            btnSearch.ForeColor = ThemeConstants.TextWhite;
            btnSearch.Font = ThemeConstants.FontBody;
            btnSearch.Text = "  🔍 Search";
            ApplyModernButtonStyling(btnDashboard);
            ApplyModernButtonStyling(btnMembers);
            ApplyModernButtonStyling(btnCatalog);
            ApplyModernButtonStyling(btnCirculation);
            ApplyModernButtonStyling(btnFines);
            ApplyModernButtonStyling(btnInventory);
            ApplyModernButtonStyling(btnReservations);
            ApplyModernButtonStyling(btnReports);
            ApplyModernButtonStyling(btnSearch);
            btnDashboard.BackColor = ThemeConstants.AccentMaroon;
        }
        private void ApplyModernDashboardStyling()
        {
            pnlMainContent.BackColor = ThemeConstants.BackgroundLight;
            pnlMainContent.Paint += (s, e) => {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    pnlMainContent.ClientRectangle,
                    ThemeConstants.BackgroundWhite,
                    ThemeConstants.BackgroundLight,
                    LinearGradientMode.Vertical))
                {
                    e.Graphics.FillRectangle(brush, pnlMainContent.ClientRectangle);
                }
            };
            pnlSidebar.BackColor = ThemeConstants.SecondaryMaroon;
            pnlSidebar.Paint += (s, e) => {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    pnlSidebar.ClientRectangle,
                    ThemeConstants.SecondaryMaroon,
                    ThemeConstants.SecondaryMaroonDark,
                    LinearGradientMode.Vertical))
                {
                    e.Graphics.FillRectangle(brush, pnlSidebar.ClientRectangle);
                }
            };
            pnlMainContent.BackColor = ThemeConstants.BackgroundLight;
        }
        private void ApplyModernButtonStyling(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Padding = new Padding(10, 0, 0, 0);
            button.MouseEnter += (s, e) => {
                if (button.BackColor != ThemeConstants.AccentMaroon)
                {
                    button.BackColor = ThemeConstants.GetHoverColor(button.BackColor);
                }
            };
            button.MouseLeave += (s, e) => {
                if (button.BackColor != ThemeConstants.AccentMaroon)
                {
                    button.BackColor = ThemeConstants.SecondaryMaroon;
                }
            };
        }
        private void DashboardForm_Resize(object sender, EventArgs e)
        {
            pnlSidebar.Height = this.ClientSize.Height;
            pnlMainContent.Width = this.ClientSize.Width - pnlSidebar.Width;
            pnlMainContent.Height = this.ClientSize.Height;
            pnlMainContent.Padding = new Padding(30, 35, 30, 35);
            RefreshCurrentView();
        }
        private void RefreshCurrentView()
        {
            if (pnlMainContent.Controls.Contains(pnlCardTotalBooks))
            {
                AdjustDashboardCards();
                AdjustDashboardText();
            }
            else if (pnlMainContent.Controls.Contains(pnlMembersView))
            {
                AdjustMembersViewLayout();
            }
            else if (pnlReportsView != null && !pnlReportsView.IsDisposed && pnlMainContent.Controls.Contains(pnlReportsView))
            {
               pnlReportsView.Size = pnlMainContent.ClientSize;
               if(pnlReportsContent != null && !pnlReportsContent.IsDisposed)
                   pnlReportsContent.Size = new Size(pnlReportsView.Width, pnlReportsView.Height - pnlReportsSubNav.Height);
            }
            else if (pnlSearchView != null && !pnlSearchView.IsDisposed && pnlMainContent.Controls.Contains(pnlSearchView))
            {
                pnlSearchView.Size = pnlMainContent.ClientSize;
            }
            else
            {
                AdjustModuleViewLayout();
            }
        }
        private void AdjustMembersViewLayout()
        {
            if (pnlMembersView != null)
            {
                pnlMembersView.Width = pnlMainContent.Width - 60;
                pnlMembersView.Height = pnlMainContent.Height - 80;
            }
        }
        private void AdjustModuleViewLayout()
        {
            foreach (Control ctrl in pnlMainContent.Controls)
            {
                if (ctrl is DataGridView dgv)
                {
                    dgv.Width = pnlMainContent.Width - 60;
                    dgv.Height = pnlMainContent.Height - ctrl.Top - 20;
                }
                else if (ctrl is Panel panel && (panel.Name.Contains("search") || panel.Name.Contains("summary") || panel.Name.Contains("alerts")))
                {
                    panel.Width = pnlMainContent.Width - 60;
                }
            }
        }
        private void AdjustDashboardText()
        {
            if (lblWelcome != null)
            {
                int maxWidth = pnlMainContent.Width - 120;
                if (lblWelcome.Width > maxWidth)
                {
                    lblWelcome.MaximumSize = new Size(maxWidth, 0);
                    lblWelcome.AutoSize = true;
                }
            }
            if (lblDate != null)
            {
                lblDate.Location = new Point(pnlMainContent.Width - lblDate.Width - 60, 20);
            }
        }
        private void AdjustDashboardCards()
        {
            if (pnlCardTotalBooks != null && pnlCardActiveMembers != null &&
                pnlCardBooksBorrowed != null && pnlCardOverdueBooks != null)
            {
                int topRowY = 110;
                int topCardWidth = 250;
                int topCardHeight = 180;
                int startX = 38;
                pnlCardTotalBooks.Size = new Size(topCardWidth, topCardHeight);
                pnlCardTotalBooks.Location = new Point(startX, topRowY);
                pnlCardActiveMembers.Size = new Size(topCardWidth, topCardHeight);
                pnlCardActiveMembers.Location = new Point(startX + topCardWidth + 17, topRowY);
                pnlCardBooksBorrowed.Size = new Size(topCardWidth, topCardHeight);
                pnlCardBooksBorrowed.Location = new Point(startX + (topCardWidth + 17) * 2, topRowY);
                pnlCardOverdueBooks.Size = new Size(topCardWidth, topCardHeight);
                pnlCardOverdueBooks.Location = new Point(startX + (topCardWidth + 17) * 3, topRowY);
                if (pnlCardTodaysBorrowings != null && pnlCardTodaysReturns != null && pnlCardPendingFines != null)
                {
                    int secondRowY = 320; 
                    int secondCardWidth = 250;
                    int secondCardHeight = 150;
                    pnlCardTodaysBorrowings.Size = new Size(secondCardWidth, secondCardHeight);
                    pnlCardTodaysBorrowings.Location = new Point(startX, secondRowY);
                    pnlCardTodaysReturns.Size = new Size(secondCardWidth, secondCardHeight);
                    pnlCardTodaysReturns.Location = new Point(startX + secondCardWidth + 17, secondRowY);
                    pnlCardPendingFines.Size = new Size(secondCardWidth, secondCardHeight);
                    pnlCardPendingFines.Location = new Point(startX + (secondCardWidth + 17) * 2, secondRowY);
                }
                if (pnlWeeklyCirculation != null && pnlCollectionCategory != null)
                {
                    int bottomRowY = 500; 
                    int panelHeight = 200;
                    int leftPanelWidth = 520;
                    int rightPanelWidth = 504;
                    int panelGap = 12;
                    pnlWeeklyCirculation.Size = new Size(leftPanelWidth, panelHeight);
                    pnlWeeklyCirculation.Location = new Point(startX, bottomRowY);
                    pnlCollectionCategory.Size = new Size(rightPanelWidth, panelHeight);
                    pnlCollectionCategory.Location = new Point(startX + leftPanelWidth + panelGap, bottomRowY);
                }
            }
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
                Library_Management_System.Helper.AuditLogger.LogLogout(
                    SiginForm.CurrentUser?.Email ?? "Unknown",
                    "USER_INITIATED");
                SiginForm.ClearCurrentUser();
                this.Close();
            }
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
                if (SiginForm.IsLoggedIn)
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
                        SiginForm.ClearCurrentUser();
                        Application.Exit();
                    }
                }
            }
        }
        private void ShowDashboardView()
        {
            RestoreOriginalControls();
            pnlMembersView.Visible = false;
            ShowDashboardControls(true);
            LoadDashboardData();
        }
        private void ShowMembersView()
        {
            RestoreOriginalControls();
            ShowDashboardControls(false);
            pnlMembersView.Visible = true;
            LoadMembersData();
        }
        private void ShowReportsView()
        {
            RestoreOriginalControls();
            ShowDashboardControls(false);
            pnlMembersView.Visible = false;
            if (pnlReportsView == null || pnlReportsView.IsDisposed)
            {
                SetupReportsView();
            }
            if (!pnlMainContent.Controls.Contains(pnlReportsView))
            {
                pnlMainContent.Controls.Add(pnlReportsView);
            }
            pnlReportsView.Visible = true;
            pnlReportsView.BringToFront();
            pnlReportsView.Dock = DockStyle.Fill;
            ShowReportsCirculation();
        }
        private void SetupReportsView()
        {
            if (pnlReportsView != null && !pnlReportsView.IsDisposed) return;
            pnlReportsView = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeConstants.BackgroundLight,
                Visible = false
            };
            pnlReportsSubNav = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(20, 0, 20, 0)
            };
            btnReportCirculation = CreateReportSubNavButton("Circulation", true);
            btnReportMembers = CreateReportSubNavButton("Members", false);
            btnReportCollection = CreateReportSubNavButton("Collection", false);
            btnReportFines = CreateReportSubNavButton("Fines", false);
            int x = 20;
            int gap = 10;
            btnReportCirculation.Location = new Point(x, 10);
            x += btnReportCirculation.Width + gap;
            btnReportMembers.Location = new Point(x, 10);
            x += btnReportMembers.Width + gap;
            btnReportCollection.Location = new Point(x, 10);
            x += btnReportCollection.Width + gap;
            btnReportFines.Location = new Point(x, 10);
            pnlReportsSubNav.Controls.AddRange(new Control[] { 
                btnReportCirculation, btnReportMembers, btnReportCollection, btnReportFines 
            });
            pnlReportsContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeConstants.BackgroundLight,
                Padding = new Padding(20)
            };
            pnlReportsView.Controls.Add(pnlReportsContent);
            pnlReportsView.Controls.Add(pnlReportsSubNav);
            btnReportCirculation.Click += (s, e) => ShowReportsCirculation();
            btnReportMembers.Click += (s, e) => ShowReportsMembers();
            btnReportCollection.Click += (s, e) => ShowReportsCollection();
            btnReportFines.Click += (s, e) => ShowReportsFines();
        }
        private Button CreateReportSubNavButton(string text, bool isActive)
        {
            return new Button
            {
                Text = text,
                Size = new Size(150, 40),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = isActive ? Color.White : Color.White,
                Font = new Font("Segoe UI", 10F, isActive ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isActive ? ThemeConstants.PrimaryMaroon : Color.Gray,
                Cursor = Cursors.Hand,
                Tag = text
            };
        }
        private void ResetReportNavButtons()
        {
             foreach(Control c in pnlReportsSubNav.Controls)
             {
                 if(c is Button btn)
                 {
                     btn.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
                     btn.ForeColor = Color.Gray;
                     btn.Invalidate();
                 }
             }
        }
        private void SetActiveReportNavButton(Button btn)
        {
            ResetReportNavButtons();
            btn.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btn.ForeColor = ThemeConstants.PrimaryMaroon;
        }
        private void ShowReportsCirculation()
        {
            SetActiveReportNavButton(btnReportCirculation);
            pnlReportsContent.Controls.Clear();
            int chartHeight = 300;
            int gap = 20;
            Panel pnlChart = new Panel
            {
                Size = new Size((pnlReportsContent.Width - gap) / 2, chartHeight),
                Location = new Point(0, 0),
                BackColor = Color.White,
                Padding = new Padding(15)
            };
            Label lblChartTitle = new Label { Text = "Daily Circulation", Font = new Font("Segoe UI", 14F, FontStyle.Bold), AutoSize = true, Location = new Point(15, 15) };
            Label lblChartSub = new Label { Text = "Borrowings and returns over time", Font = new Font("Segoe UI", 9F), ForeColor = Color.Gray, AutoSize = true, Location = new Point(15, 55) };
            Chart chart = new Chart { Dock = DockStyle.Bottom, Height = 220 };
            ChartArea ca = new ChartArea("MainArea");
            ca.AxisX.MajorGrid.LineColor = Color.LightGray;
            ca.AxisY.MajorGrid.LineColor = Color.LightGray;
            ca.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            ca.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            ca.BackColor = Color.White;
            chart.ChartAreas.Add(ca);
            Series sBorrow = new Series("Borrowing") { ChartType = SeriesChartType.Line, Color = Color.Green, BorderWidth = 2 };
            sBorrow.Points.AddXY("Dec 28", 1);
            sBorrow.Points.AddXY("Dec 29", 3);
            sBorrow.Points.AddXY("Dec 30", 2);
            sBorrow.Points.AddXY("Dec 31", 0);
            sBorrow.Points.AddXY("Jan 1", 4);
            sBorrow.Points.AddXY("Jan 2", 2);
            sBorrow.Points.AddXY("Jan 3", 1);
            chart.Series.Add(sBorrow);
            pnlChart.Controls.AddRange(new Control[] { lblChartTitle, lblChartSub, chart });
            Panel pnlTopBooks = new Panel
            {
                Size = new Size((pnlReportsContent.Width - gap) / 2, chartHeight),
                Location = new Point((pnlReportsContent.Width - gap) / 2 + gap, 0),
                BackColor = Color.White,
                Padding = new Padding(15)
            };
            Label lblTopBooks = new Label { Text = "Most Borrowed Books", Font = new Font("Segoe UI", 14F, FontStyle.Bold), AutoSize = true, Location = new Point(15, 15) };
            Label lblTopBooksSub = new Label { Text = "Top 10 most popular books", Font = new Font("Segoe UI", 9F), ForeColor = Color.Gray, AutoSize = true, Location = new Point(15, 55) };
            Label lblNoData = new Label { Text = "No data for selected period", Font = new Font("Segoe UI", 10F), ForeColor = Color.Gray, AutoSize = true, Location = new Point(100, 100) };
            pnlTopBooks.Controls.AddRange(new Control[] { lblTopBooks, lblTopBooksSub, lblNoData });
            pnlTopBooks.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Panel pnlOverdue = new Panel
            {
                Size = new Size(pnlReportsContent.Width, 200),
                Location = new Point(0, chartHeight + gap),
                BackColor = Color.White,
                Padding = new Padding(15),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Label lblOverdue = new Label { Text = "Overdue Books", Font = new Font("Segoe UI", 14F, FontStyle.Bold), AutoSize = true, Location = new Point(15, 15) };
            Label lblOverdueSub = new Label { Text = "Books that are past their due date", Font = new Font("Segoe UI", 9F), ForeColor = Color.Gray, AutoSize = true, Location = new Point(15, 55) };
            Label lblHeader = new Label { 
                Text = String.Format("{0,-40} {1,-30} {2,-20} {3,-20} {4,-15}", "Book", "Member", "Borrow Date", "Due Date", "Days Overdue"),
                Font = new Font("Consolas", 9F, FontStyle.Bold),
                Location = new Point(15, 75),
                AutoSize = true
            };
            Label lblRow1 = new Label { 
                 Text = String.Format("{0,-40} {1,-30} {2,-20} {3,-20} {4,-15}", "Pride and Prejudice", "Michael Brown", "Oct 20, 2024", "Nov 10, 2024", "420 days"),
                 Font = new Font("Consolas", 9F),
                 Location = new Point(15, 100),
                 ForeColor = Color.Red,
                 AutoSize = true
            };
            pnlOverdue.Controls.AddRange(new Control[] { lblOverdue, lblOverdueSub, lblHeader, lblRow1 });
            pnlReportsContent.Controls.Add(pnlChart);
            pnlReportsContent.Controls.Add(pnlTopBooks);
            pnlReportsContent.Controls.Add(pnlOverdue);
            pnlReportsContent.Resize += (s,ev) => {
                 int w = pnlReportsContent.Width;
                 pnlChart.Width = (w - gap) / 2;
                 pnlTopBooks.Location = new Point(pnlChart.Width + gap, 0);
                 pnlTopBooks.Width = w - pnlChart.Width - gap;
                 pnlOverdue.Width = w;
            };
        }
        private void ShowReportsMembers() { SetActiveReportNavButton(btnReportMembers); pnlReportsContent.Controls.Clear(); }
        private void ShowReportsCollection() { SetActiveReportNavButton(btnReportCollection); pnlReportsContent.Controls.Clear(); }
        private void ShowReportsFines() { SetActiveReportNavButton(btnReportFines); pnlReportsContent.Controls.Clear(); }
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
            pnlWeeklyCirculation.Visible = show;
            pnlCollectionCategory.Visible = show;
            if (pnlReportsView != null && !pnlReportsView.IsDisposed) pnlReportsView.Visible = false;
            if (pnlSearchView != null && !pnlSearchView.IsDisposed) pnlSearchView.Visible = false;
        }
        private void ShowSearchView()
        {
            RestoreOriginalControls();
            ShowDashboardControls(false);
            pnlMembersView.Visible = false;
            if (pnlSearchView == null || pnlSearchView.IsDisposed) SetupSearchView();
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
                Location = new Point(0, 0)
            };
            Label lblSubtitle = new Label
            {
                Text = "Find books, resources, and check availability",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(2, 35)
            };
            Panel pnlSearchBar = new Panel
            {
                Size = new Size(pnlSearchView.Width - 60, 60),
                Location = new Point(0, 70),
                BackColor = Color.White,
                Padding = new Padding(10),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlSearchBar.Paint += (s, e) => {
                using (Pen p = new Pen(Color.LightGray)) {
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
            btnTriggerSearch.Click += (s, e) => PerformSearch();
            txtSearchInput.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) PerformSearch(); };
            btnSearchFilters = new Button
            {
                Text = "Reference  ▼",
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
            Label lblSearchIcon = new Label { Text = "🔍", Font = new Font("Segoe UI", 12F), Location = new Point(10, 15), AutoSize = true, ForeColor = Color.Gray };
            pnlSearchBar.Resize += (s, e) => {
                txtSearchInput.Width = pnlSearchBar.Width - 260;
            };
            txtSearchInput.Enter += (s, e) => {
                if(txtSearchInput.Text == "Search by title, author, ISBN, subject...") {
                    txtSearchInput.Text = "";
                    txtSearchInput.ForeColor = Color.Black;
                }
            };
            txtSearchInput.Leave += (s, e) => {
                if(string.IsNullOrWhiteSpace(txtSearchInput.Text)) {
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
             pnlSearchContent.Paint += (s, e) => {
                using (Pen p = new Pen(Color.LightGray)) {
                    e.Graphics.DrawRectangle(p, 0, 0, pnlSearchContent.Width - 1, pnlSearchContent.Height - 1);
                }
            };
            Label lblEmptyIcon = new Label 
            { 
                Text = "🔍", 
                Font = new Font("Segoe UI", 60F), 
                ForeColor = Color.LightGray, 
                AutoSize = true 
            };
            Label lblEmptyTitle = new Label 
            { 
                Text = "Start your search", 
                Font = new Font("Georgia", 16F, FontStyle.Bold), 
                ForeColor = ThemeConstants.PrimaryMaroon, 
                AutoSize = true 
            };
            Label lblEmptySub = new Label 
            { 
                Text = "Enter a search term to find books in the library catalog", 
                Font = new Font("Segoe UI", 10F), 
                ForeColor = Color.Gray, 
                AutoSize = true 
            };
            pnlSearchContent.Resize += (s, e) => {
                int cx = pnlSearchContent.Width / 2;
                int cy = pnlSearchContent.Height / 2;
                lblEmptyIcon.Location = new Point(cx - lblEmptyIcon.Width / 2, cy - 80);
                lblEmptyTitle.Location = new Point(cx - lblEmptyTitle.Width / 2, cy + 20);
                lblEmptySub.Location = new Point(cx - lblEmptySub.Width / 2, cy + 50);
            };
            FlowLayoutPanel flpChips = new FlowLayoutPanel 
            { 
                AutoSize = true, 
                Anchor = AnchorStyles.None 
            };
            string[] topics = { "Fiction", "Science", "History", "Technology" };
            foreach(var t in topics)
            {
                Label chip = new Label { 
                    Text = t, 
                    AutoSize = true, 
                    Padding = new Padding(10, 5, 10, 5), 
                    BackColor = Color.FromArgb(240, 240, 240),
                    ForeColor = Color.Gray,
                    Font = new Font("Segoe UI", 9F),
                    Margin = new Padding(5)
                };
                 chip.Paint += (s,e) => ControlPaint.DrawBorder(e.Graphics, chip.ClientRectangle, Color.LightGray, ButtonBorderStyle.Solid);
                flpChips.Controls.Add(chip);
            }
             pnlSearchContent.Resize += (s, e) => {
                 flpChips.Location = new Point((pnlSearchContent.Width - flpChips.Width) / 2, (pnlSearchContent.Height / 2) + 90);
             };
            pnlSearchContent.Controls.AddRange(new Control[] { lblEmptyIcon, lblEmptyTitle, lblEmptySub, flpChips });
            pnlSearchView.Controls.Add(lblTitle);
            pnlSearchView.Controls.Add(lblSubtitle);
            pnlSearchView.Controls.Add(pnlSearchBar);
            pnlSearchView.Controls.Add(pnlSearchContent);
        }
        
        private void PerformSearch()
        {
            string searchText = txtSearchInput.Text;
            if (searchText == "Search by title, author, ISBN, subject..." || string.IsNullOrWhiteSpace(searchText))
            {
                return;
            }
            
            try
            {
                var filters = new SearchService.SearchFilters
                {
                    Category = btnSearchFilters.Text.Replace("  ▼", "").Replace("All Categories", "All Categories")
                };
                
                var results = _searchService.SearchBooks(searchText, filters);
                DisplaySearchResults(results);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error performing search: {ex.Message}", "Search Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void DisplaySearchResults(List<SearchService.SearchResult> results)
        {
            pnlSearchContent.Controls.Clear();
            
            if (results.Count == 0)
            {
                Label lblNoResults = new Label
                {
                    Text = "No results found",
                    Font = new Font("Segoe UI", 12F),
                    ForeColor = Color.Gray,
                    AutoSize = true,
                    Location = new Point(20, 20),
                    BackColor = Color.Transparent
                };
                pnlSearchContent.Controls.Add(lblNoResults);
                return;
            }
            
            Label lblResultsCount = new Label
            {
                Text = $"Found {results.Count} result(s)",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(15, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlSearchContent.Controls.Add(lblResultsCount);
            
            Panel resultsPanel = new Panel
            {
                Location = new Point(15, 45),
                Size = new Size(pnlSearchContent.Width - 30, pnlSearchContent.Height - 60),
                AutoScroll = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            
            int yPos = 0;
            foreach (var result in results)
            {
                Panel resultCard = new Panel
                {
                    Size = new Size(resultsPanel.Width - 20, 120),
                    Location = new Point(0, yPos),
                    BackColor = Color.White,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                resultCard.Paint += (s, e) => {
                    using (Pen p = new Pen(Color.LightGray))
                        e.Graphics.DrawRectangle(p, 0, 0, resultCard.Width - 1, resultCard.Height - 1);
                };
                
                Label lblTitle = new Label
                {
                    Text = result.Title,
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    Location = new Point(15, 10),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                
                Label lblAuthor = new Label
                {
                    Text = $"By {result.Author}",
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = Color.Gray,
                    Location = new Point(15, 35),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                
                Label lblDetails = new Label
                {
                    Text = $"ISBN: {result.ISBN ?? "N/A"} | Category: {result.Category} | Year: {result.PublicationYear?.ToString() ?? "N/A"}",
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = Color.DarkGray,
                    Location = new Point(15, 55),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                
                Label lblAvailability = new Label
                {
                    Text = result.IsAvailable ? $"✓ Available ({result.AvailableCopies} copies)" : "✗ Unavailable",
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = result.IsAvailable ? Color.Green : Color.Red,
                    Location = new Point(15, 80),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                
                resultCard.Controls.AddRange(new Control[] { lblTitle, lblAuthor, lblDetails, lblAvailability });
                resultsPanel.Controls.Add(resultCard);
                yPos += 130;
            }
            
            pnlSearchContent.Controls.Add(resultsPanel);
        }
        
        private void LoadMembersData()
        {
            if (_isLoadingMembersData) return;
            try
            {
                _isLoadingMembersData = true;
                var membersService = new Library_Management_System.Service.MembersService();
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
                var membersService = new Library_Management_System.Service.MembersService();
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
                MessageBox.Show("Failed to load member details. Please try again.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"Error loading member details: {ex.Message}\n{ex.StackTrace}");
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
                Text = "Member Information",
                Size = new Size(900, 1000),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = true,
                ShowInTaskbar = false,
                BackColor = Color.White
            };
            Label titleLabel = new Label
            {
                Text = member.Name,
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                Location = new Point(30, 30),
                Size = new Size(600, 65),
                ForeColor = Color.FromArgb(33, 37, 41),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Label memberIdLabel = new Label
            {
                Text = $"Member ID: {member.MemberId}",
                Font = new Font("Segoe UI", 12F, FontStyle.Regular),
                Location = new Point(30, 100),
                Size = new Size(600, 30),
                ForeColor = Color.FromArgb(108, 117, 125),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(40, 30, 40, 40),
                BackColor = Color.FromArgb(250, 251, 252)
            };
            int yPos = 135;
            Panel userInfoPanel = new Panel
            {
                Location = new Point(0, yPos),
                Size = new Size(820, 180),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            userInfoPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, userInfoPanel.Width, userInfoPanel.Height), 15))
                {
                    e.Graphics.FillPath(new SolidBrush(Color.White), path);
                    e.Graphics.DrawPath(new Pen(Color.FromArgb(222, 226, 230), 1), path);
                }
            };
            Panel avatarPanel = new Panel
            {
                Location = new Point(30, 20),
                Size = new Size(90, 90),
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
                using (Font font = new Font("Segoe UI", 26F, FontStyle.Bold))
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
                Location = new Point(140, 25),
                Size = new Size(85, 32),
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
                Location = new Point(240, 25),
                Size = new Size(85, 32),
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
                Location = new Point(140, 70),
                Size = new Size(650, 30),
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(52, 58, 64),
                AutoSize = false
            };
            Label phoneLabel = new Label
            {
                Text = member.Phone ?? "Not provided",
                Location = new Point(140, 100),
                Size = new Size(650, 30),
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(52, 58, 64),
                AutoSize = false
            };
            userInfoPanel.Controls.AddRange(new Control[] { avatarPanel, statusTag, typeTag, emailLabel, phoneLabel });
            yPos += 205; 
            Panel membershipCard = CreateInfoCard("Membership Details", yPos, 160);
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
            yPos += 185; 
            Panel borrowingCard = CreateInfoCard("Borrowing Summary", yPos, 150);
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
            yPos += 175; 
            Panel privilegesCard = CreateInfoCard($"Privileges ({member.Type.ToLower()})", yPos, 170);
            int cardWidth = 820;
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
                Text = "₱25",
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
            contentPanel.Controls.AddRange(new Control[] { titleLabel, memberIdLabel, userInfoPanel, membershipCard, borrowingCard, privilegesCard });
            previewForm.Controls.Add(contentPanel);
            previewForm.ShowDialog(this);
        }
        private Panel CreateInfoCard(string title, int yPosition, int height = 100)
        {
            Panel card = new Panel
            {
                Location = new Point(0, yPosition),
                Size = new Size(820, height),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(0)
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, card.Width, card.Height), 15))
                {
                    e.Graphics.FillPath(new SolidBrush(Color.White), path);
                    e.Graphics.DrawPath(new Pen(Color.FromArgb(222, 226, 230), 1), path);
                }
            };
            Label titleLabel = new Label
            {
                Text = title,
                Location = new Point(30, 20),
                Size = new Size(760, 30),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
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
                var membersService = new Library_Management_System.Service.MembersService();
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
                    var membersService = new Library_Management_System.Service.MembersService();
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
                Size = new Size(520, 680),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false
            };
            Label titleLabel = new Label
            {
                Text = "Edit Member",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(440, 65),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            Label subtitleLabel = new Label
            {
                Text = "Update member information",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(20, 100),
                Size = new Size(440, 25),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
            Label lblFirstName = new Label { Text = "First Name", Location = new Point(20, 140), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtFirstName = new TextBox { Location = new Point(20, 165), Size = new Size(440, 30), Font = new Font("Segoe UI", 10F), Text = firstName };
            Label lblLastName = new Label { Text = "Last Name", Location = new Point(20, 205), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtLastName = new TextBox { Location = new Point(20, 230), Size = new Size(440, 30), Font = new Font("Segoe UI", 10F), Text = lastName };
            Label lblEmail = new Label { Text = "Email", Location = new Point(20, 270), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtEmail = new TextBox 
            { 
                Location = new Point(20, 295), 
                Size = new Size(440, 30), 
                Font = new Font("Segoe UI", 10F), 
                Text = member.Email
            };
            Label lblPhone = new Label { Text = "Phone", Location = new Point(20, 335), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtPhone = new TextBox 
            { 
                Location = new Point(20, 360), 
                Size = new Size(440, 30), 
                Font = new Font("Segoe UI", 10F), 
                Text = member.Phone ?? ""
            };
            Label lblAddress = new Label { Text = "Address", Location = new Point(20, 400), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtAddress = new TextBox 
            { 
                Location = new Point(20, 425), 
                Size = new Size(440, 30), 
                Font = new Font("Segoe UI", 10F), 
                Text = member.Address ?? ""
            };
            Label lblMemberType = new Label { Text = "Member Type", Location = new Point(20, 465), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            ComboBox cmbMemberType = new ComboBox
            {
                Location = new Point(20, 490),
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
            Label lblStatus = new Label { Text = "Status", Location = new Point(20, 530), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            ComboBox cmbStatus = new ComboBox
            {
                Location = new Point(20, 555),
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
                Location = new Point(270, 600),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.Click += (s, e) => editForm.Close();
            Button btnSave = new Button
            {
                Text = "Save Changes",
                Location = new Point(360, 600),
                Size = new Size(110, 35),
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(128, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;
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
            txtPhone.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar) &&
                    e.KeyChar != ' ' && e.KeyChar != '-' && e.KeyChar != '(' &&
                    e.KeyChar != ')' && e.KeyChar != '+')
                {
                    e.Handled = true; 
                }
            };
            txtAddress.KeyPress += (s, e) =>
            {
            };
            btnSave.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtFirstName.Text) ||
                    string.IsNullOrWhiteSpace(txtLastName.Text) ||
                    string.IsNullOrWhiteSpace(txtEmail.Text))
                {
                    MessageBox.Show("Please fill in all required fields (First Name, Last Name, Email).",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (txtFirstName.Text.Trim().Length < 2)
                {
                    MessageBox.Show("First name must be at least 2 characters long.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtFirstName.Focus();
                    return;
                }
                if (txtLastName.Text.Trim().Length < 2)
                {
                    MessageBox.Show("Last name must be at least 2 characters long.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtLastName.Focus();
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
                    MessageBox.Show("Please ensure:\n\n• It must be institutional email\n• Format: firstname.lastname.IDnumber.tc@umindanao.edu.ph\n\nExample: john.doe.123456.tc@umindanao.edu.ph",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }
                try
                {
                    btnSave.Enabled = false;
                    btnSave.Text = "Saving...";
                    string memberType = cmbMemberType.SelectedItem?.ToString();
                    if (string.IsNullOrEmpty(memberType) || cmbMemberType.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please select a member type.",
                            "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        cmbMemberType.Focus();
                        btnSave.Enabled = true;
                        btnSave.Text = "Save Changes";
                        return;
                    }
                    string status = cmbStatus.SelectedItem?.ToString();
                    if (string.IsNullOrEmpty(status) || cmbStatus.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please select a status.",
                            "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        cmbStatus.Focus();
                        btnSave.Enabled = true;
                        btnSave.Text = "Save Changes";
                        return;
                    }
                    var membersService = new Library_Management_System.Service.MembersService();
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
                    MessageBox.Show("Failed to update member information. Please check your input and try again.",
                        "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    System.Diagnostics.Debug.WriteLine($"Error updating member: {ex.Message}\n{ex.StackTrace}");
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
        private void ShowDatabaseDiagnostics()
        {
            try
            {
                var membersService = new Library_Management_System.Service.MembersService();
                string diagnostics = membersService.DiagnoseDatabase();
                Form diagForm = new Form
                {
                    Text = "Database Diagnostics",
                    Size = new Size(600, 400),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };
                TextBox txtDiagnostics = new TextBox
                {
                    Text = diagnostics,
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    ReadOnly = true,
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 9F),
                    BackColor = Color.White,
                    ForeColor = Color.Black
                };
                Button btnClose = new Button
                {
                    Text = "Close",
                    Location = new Point(250, 350),
                    Size = new Size(100, 35),
                    DialogResult = DialogResult.OK
                };
                diagForm.Controls.AddRange(new Control[] { txtDiagnostics, btnClose });
                diagForm.AcceptButton = btnClose;
                diagForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running diagnostics: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ShowRegisterMemberDialog()
        {
            Form registerForm = new Form
            {
                Text = "Register New Member",
                Size = new Size(900, 800),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false,
                BackColor = ThemeConstants.BackgroundLight
            };
            Label titleLabel = new Label
            {
                Text = "👤 Register New Member",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(740, 65),
                ForeColor = Color.FromArgb(40, 40, 40),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Label subtitleLabel = new Label
            {
                Text = "Complete member information for library system registration",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(30, 100),
                Size = new Size(740, 20),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
            Panel personalPanel = new Panel
            {
                Location = new Point(20, 140),
                Size = new Size(860, 180),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ThemeConstants.BackgroundLight
            };
            Label personalHeader = new Label
            {
                Text = "📋 Personal Information",
                Font = ThemeConstants.FontBodyLarge,
                Location = new Point(15, 10),
                Size = new Size(200, 25),
                ForeColor = ThemeConstants.TextPrimary
            };
            Label lblFirstName = new Label { Text = "First Name *", Location = new Point(20, 45), Size = new Size(100, 20), Font = ThemeConstants.FontBodySmall, ForeColor = ThemeConstants.TextPrimary };
            TextBox txtFirstName = new TextBox { Location = new Point(20, 65), Size = new Size(200, ThemeConstants.InputHeight), Font = ThemeConstants.FontBodySmall, BackColor = ThemeConstants.BackgroundWhite, ForeColor = ThemeConstants.TextPrimary };
            Label lblLastName = new Label { Text = "Last Name *", Location = new Point(240, 45), Size = new Size(100, 20), Font = ThemeConstants.FontBodySmall, ForeColor = ThemeConstants.TextPrimary };
            TextBox txtLastName = new TextBox { Location = new Point(240, 65), Size = new Size(200, ThemeConstants.InputHeight), Font = ThemeConstants.FontBodySmall, BackColor = ThemeConstants.BackgroundWhite, ForeColor = ThemeConstants.TextPrimary };
            Label lblIdNumber = new Label { Text = "ID Number", Location = new Point(460, 45), Size = new Size(100, 20), Font = ThemeConstants.FontBodySmall, ForeColor = ThemeConstants.TextPrimary };
            TextBox txtIdNumber = new TextBox { Location = new Point(460, 65), Size = new Size(160, ThemeConstants.InputHeight), Font = ThemeConstants.FontBodySmall, BackColor = ThemeConstants.BackgroundWhite, ForeColor = ThemeConstants.TextPrimary };
            Label hintId = new Label
            {
                Text = "6 digits only",
                Font = new Font("Segoe UI", 8F),
                Location = new Point(625, 68),
                Size = new Size(80, 15),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
            Label lblEmail = new Label { Text = "Email *", Location = new Point(20, 100), Size = new Size(100, 20), Font = ThemeConstants.FontBodySmall, ForeColor = ThemeConstants.TextPrimary };
            TextBox txtEmail = new TextBox { Location = new Point(20, 120), Size = new Size(400, ThemeConstants.InputHeight), Font = ThemeConstants.FontBodySmall, BackColor = ThemeConstants.BackgroundWhite, ForeColor = ThemeConstants.TextPrimary };
            Label lblDateOfBirth = new Label { Text = "Date of Birth", Location = new Point(440, 100), Size = new Size(100, 20), Font = ThemeConstants.FontBodySmall, ForeColor = ThemeConstants.TextPrimary };
            DateTimePicker dtpDateOfBirth = new DateTimePicker
            {
                Location = new Point(440, 120),
                Size = new Size(180, ThemeConstants.InputHeight),
                Font = ThemeConstants.FontBodySmall,
                BackColor = ThemeConstants.BackgroundWhite,
                ForeColor = ThemeConstants.TextPrimary,
                Format = DateTimePickerFormat.Short,
                MaxDate = DateTime.Today.AddYears(-5),
                MinDate = DateTime.Today.AddYears(-120),
                Value = DateTime.Today.AddYears(-18)
            };
            Panel contactPanel = new Panel
            {
                Location = new Point(20, 340),
                Size = new Size(860, 170),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ThemeConstants.BackgroundLight
            };
            Label contactHeader = new Label
            {
                Text = "📞 Contact Information",
                Font = ThemeConstants.FontBodyLarge,
                Location = new Point(15, 10),
                Size = new Size(200, 25),
                ForeColor = ThemeConstants.TextPrimary
            };
            Label lblPhone = new Label { Text = "Phone Number", Location = new Point(20, 45), Size = new Size(100, 20), Font = ThemeConstants.FontBodySmall, ForeColor = ThemeConstants.TextPrimary };
            TextBox txtPhone = new TextBox { Location = new Point(20, 65), Size = new Size(180, 25), Font = ThemeConstants.FontBodySmall, BackColor = ThemeConstants.BackgroundWhite, ForeColor = ThemeConstants.TextPrimary };
            Label lblAddress = new Label { Text = "Address", Location = new Point(220, 45), Size = new Size(100, 20), Font = ThemeConstants.FontBodySmall, ForeColor = ThemeConstants.TextPrimary };
            TextBox txtAddress = new TextBox { Location = new Point(220, 65), Size = new Size(400, 25), Font = ThemeConstants.FontBodySmall, BackColor = ThemeConstants.BackgroundWhite, ForeColor = ThemeConstants.TextPrimary };
            Label lblEmergencyName = new Label { Text = "Emergency Contact Name", Location = new Point(20, 100), Size = new Size(150, 20), Font = ThemeConstants.FontBodySmall, ForeColor = ThemeConstants.TextPrimary };
            TextBox txtEmergencyName = new TextBox { Location = new Point(20, 120), Size = new Size(200, 25), Font = ThemeConstants.FontBodySmall, BackColor = ThemeConstants.BackgroundWhite, ForeColor = ThemeConstants.TextPrimary };
            Label lblEmergencyPhone = new Label { Text = "Emergency Contact Phone", Location = new Point(240, 100), Size = new Size(160, 20), Font = ThemeConstants.FontBodySmall, ForeColor = ThemeConstants.TextPrimary };
            TextBox txtEmergencyPhone = new TextBox { Location = new Point(240, 120), Size = new Size(180, 25), Font = ThemeConstants.FontBodySmall, BackColor = ThemeConstants.BackgroundWhite, ForeColor = ThemeConstants.TextPrimary };
            txtEmergencyPhone.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
                {
                    e.Handled = true;
                }
                if (char.IsDigit(e.KeyChar) && txtEmergencyPhone.Text.Length >= 11)
                {
                    e.Handled = true;
                }
            };
            Panel academicPanel = new Panel
            {
                Location = new Point(20, 530),
                Size = new Size(860, 140),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = ThemeConstants.BackgroundLight
            };
            Label academicHeader = new Label
            {
                Text = "🎓 Academic Information",
                Font = ThemeConstants.FontBodyLarge,
                Location = new Point(15, 10),
                Size = new Size(200, 25),
                ForeColor = ThemeConstants.TextPrimary
            };
            Label lblMemberType = new Label { Text = "Member Type *", Location = new Point(20, 45), Size = new Size(100, 20), Font = ThemeConstants.FontBodySmall, ForeColor = ThemeConstants.TextPrimary };
            ComboBox cmbMemberType = new ComboBox
            {
                Location = new Point(20, 65),
                Size = new Size(160, 25),
                Font = ThemeConstants.FontBodySmall,
                BackColor = ThemeConstants.BackgroundWhite,
                ForeColor = ThemeConstants.TextPrimary,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbMemberType.Items.AddRange(new[] { "Student", "Faculty", "Staff", "Guest" });
            cmbMemberType.SelectedIndex = 0;
            Label lblDepartment = new Label { Text = "Department/Program", Location = new Point(200, 45), Size = new Size(130, 20), Font = ThemeConstants.FontBodySmall, ForeColor = ThemeConstants.TextPrimary };
            ComboBox cmbDepartment = new ComboBox
            {
                Location = new Point(200, 65),
                Size = new Size(200, 25),
                Font = ThemeConstants.FontBodySmall,
                BackColor = ThemeConstants.BackgroundWhite,
                ForeColor = ThemeConstants.TextPrimary,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.Suggest,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            cmbDepartment.Items.AddRange(new[] {
                "Information Technology",
                "Computer Science",
                "Business Administration",
                "Engineering",
                "Education",
                "Nursing",
                "Arts and Sciences",
                "Law",
                "Medicine",
                "Other"
            });
            Label lblStatus = new Label { Text = "Account Status", Location = new Point(420, 45), Size = new Size(100, 20), Font = ThemeConstants.FontBodySmall, ForeColor = ThemeConstants.TextPrimary };
            ComboBox cmbStatus = new ComboBox
            {
                Location = new Point(420, 65),
                Size = new Size(120, 25),
                Font = ThemeConstants.FontBodySmall,
                BackColor = ThemeConstants.BackgroundWhite,
                ForeColor = ThemeConstants.TextPrimary,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStatus.Items.AddRange(new[] { "Active", "Inactive", "Suspended", "Expired" });
            cmbStatus.SelectedIndex = 0; 
            Label lblExpiryDate = new Label { Text = "Membership Expiry", Location = new Point(560, 45), Size = new Size(120, 20), Font = ThemeConstants.FontBodySmall, ForeColor = ThemeConstants.TextPrimary };
            DateTimePicker dtpExpiryDate = new DateTimePicker
            {
                Location = new Point(560, 65),
                Size = new Size(160, 25),
                Font = ThemeConstants.FontBodySmall,
                BackColor = ThemeConstants.BackgroundWhite,
                ForeColor = ThemeConstants.TextPrimary,
                Format = DateTimePickerFormat.Short,
                MinDate = DateTime.Today,
                Value = DateTime.Today.AddYears(1)
            };
            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(580, 700),
                Size = new Size(120, ThemeConstants.ButtonHeight),
                Font = ThemeConstants.FontButton,
                BackColor = ThemeConstants.BackgroundMedium,
                ForeColor = ThemeConstants.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            Button btnRegister = new Button
            {
                Text = "Add Member",
                Location = new Point(720, 700),
                Size = new Size(130, ThemeConstants.ButtonHeight),
                Font = ThemeConstants.FontButton,
                BackColor = Color.Maroon,
                ForeColor = ThemeConstants.TextWhite,
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
            txtFirstName.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtIdNumber.Focus(); };
            txtIdNumber.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtLastName.Focus(); };
            txtIdNumber.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
                {
                    e.Handled = true; 
                }
                if (char.IsDigit(e.KeyChar) && txtIdNumber.Text.Length >= 6)
                {
                    e.Handled = true; 
                }
            };
            txtLastName.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtEmail.Focus(); };
            txtEmail.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) dtpDateOfBirth.Focus(); };
            dtpDateOfBirth.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtPhone.Focus(); };
            txtPhone.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtAddress.Focus(); };
            txtAddress.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) cmbMemberType.Focus(); };
            cmbMemberType.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) cmbDepartment.Focus(); };
            cmbMemberType.SelectedIndexChanged += (s, e) =>
            {
                string memberType = cmbMemberType.SelectedItem?.ToString();
                if (memberType == "Student")
                {
                    txtEmail.SetPlaceholder("john.doe.123456.tc@umindanao.edu.ph");
                }
                else
                {
                    txtEmail.SetPlaceholder("Enter email address");
                }
            };
            cmbDepartment.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) cmbStatus.Focus(); };
            cmbStatus.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnRegister.PerformClick(); };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.MouseEnter += (s, e) => btnCancel.BackColor = ThemeConstants.GetHoverColor(ThemeConstants.BackgroundMedium);
            btnCancel.MouseLeave += (s, e) => btnCancel.BackColor = ThemeConstants.BackgroundMedium;
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.MouseEnter += (s, e) => btnRegister.BackColor = Color.FromArgb(128, 0, 0); 
            btnRegister.MouseLeave += (s, e) => btnRegister.BackColor = Color.Maroon;
            txtPhone.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar) &&
                    e.KeyChar != ' ' && e.KeyChar != '-' && e.KeyChar != '(' &&
                    e.KeyChar != ')' && e.KeyChar != '+')
                {
                    e.Handled = true; 
                }
            };
            txtAddress.KeyPress += (s, e) =>
            {
            };
            registerForm.AcceptButton = btnRegister;
            registerForm.CancelButton = btnCancel;
            btnCancel.Click += (s, e) => registerForm.Close();
            btnRegister.MouseEnter += (s, e) => btnRegister.Invalidate();
            btnRegister.MouseLeave += (s, e) => btnRegister.Invalidate();
            btnRegister.MouseDown += (s, e) => btnRegister.Invalidate();
            btnRegister.MouseUp += (s, e) => btnRegister.Invalidate();
            personalPanel.Controls.AddRange(new Control[] {
                personalHeader, lblFirstName, txtFirstName, lblLastName, txtLastName,
                lblIdNumber, txtIdNumber, lblDateOfBirth, dtpDateOfBirth, lblEmail, txtEmail, hintId
            });
            contactPanel.Controls.AddRange(new Control[] {
                contactHeader, lblPhone, txtPhone, lblAddress, txtAddress,
                lblEmergencyName, txtEmergencyName, lblEmergencyPhone, txtEmergencyPhone
            });
            academicPanel.Controls.AddRange(new Control[] {
                academicHeader, lblMemberType, cmbMemberType, lblDepartment, cmbDepartment, lblStatus, cmbStatus,
                lblExpiryDate, dtpExpiryDate
            });
            txtFirstName.SetPlaceholder("Enter first name");
            txtLastName.SetPlaceholder("Enter last name");
            txtIdNumber.SetPlaceholder("Enter ID number");
            txtEmail.SetPlaceholder("Enter email address");
            txtPhone.SetPlaceholder("Enter phone number");
            txtAddress.SetPlaceholder("Enter address");
            txtEmergencyName.SetPlaceholder("Contact name");
            txtEmergencyPhone.SetPlaceholder("Contact phone");
            registerForm.Controls.AddRange(new Control[]
            {
                titleLabel, subtitleLabel,
                personalPanel, contactPanel, academicPanel,
                btnCancel, btnRegister
            });
            btnRegister.Click += (s, args) =>
            {
                if (!IsValidName(txtFirstName.GetActualText(), "First Name"))
                {
                    MessageBox.Show("Please enter a valid first name (2-50 characters, letters only).",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtFirstName.Focus();
                    return;
                }
                if (!IsValidName(txtLastName.GetActualText(), "Last Name"))
                {
                    MessageBox.Show("Please enter a valid last name (2-50 characters, letters only).",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtLastName.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtEmail.GetActualText()))
                {
                    MessageBox.Show("Please enter an email address.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtEmail.Focus();
                    return;
                }
                string email = txtEmail.GetActualText().Trim();
                string memberType = cmbMemberType.SelectedItem?.ToString();
                bool requiresUmindanaoEmail = memberType == "Student";
                if (requiresUmindanaoEmail)
                {
                    if (email == "firstname.lastname.IDnumber.tc@umindanao.edu.ph" ||
                        email == "firstname.lastname.idnumber.tc@umindanao.edu.ph" ||
                        string.IsNullOrWhiteSpace(email))
                    {
                        MessageBox.Show("Please ensure:\n\n• It must be institutional email\n• Format: firstname.lastname.IDnumber.tc@umindanao.edu.ph\n\nExample: john.doe.123456.tc@umindanao.edu.ph",
                            "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtEmail.Focus();
                        return;
                    }
                    email = email.ToLower();
                    if (!IsValidEducationalEmail(email))
                    {
                        MessageBox.Show("Please ensure:\n\n• It must be institutional email\n• Format: firstname.lastname.IDnumber.tc@umindanao.edu.ph\n\nExample: john.doe.123456.tc@umindanao.edu.ph",
                            "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtEmail.Focus();
                        return;
                    }
                }
                else
                {
                    if (!IsValidRegularEmail(email))
                    {
                        MessageBox.Show("Please enter a valid email address.\n\nExample: john.doe@example.com",
                            "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtEmail.Focus();
                        return;
                    }
                    email = email.ToLower();
                }
                if (!IsValidPhoneNumber(txtPhone.GetActualText()))
                {
                    MessageBox.Show("Please enter a valid Philippine phone number (7-12 digits, starting with 09 or 63).",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPhone.Focus();
                    return;
                }
                if (!IsValidAddress(txtAddress.GetActualText()))
                {
                    MessageBox.Show("Please enter a valid address (5-200 characters, letters and numbers only).",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtAddress.Focus();
                    return;
                }
                if (!string.IsNullOrWhiteSpace(txtIdNumber.Text) && !IsValidIdNumber(txtIdNumber.Text))
                {
                    MessageBox.Show("Please enter a valid ID number (exactly 6 digits only, no letters).\nExample: 142275",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtIdNumber.Focus();
                    return;
                }
                if (cmbMemberType.SelectedItem == null)
                {
                    MessageBox.Show("Please select a member type.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbMemberType.Focus();
                    return;
                }
                if (cmbStatus.SelectedItem == null)
                {
                    MessageBox.Show("Please select a status.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbStatus.Focus();
                    return;
                }
                string emergencyPhone = txtEmergencyPhone.GetActualText().Trim();
                if (!string.IsNullOrWhiteSpace(emergencyPhone))
                {
                    if (emergencyPhone.Length != 11 || (!emergencyPhone.StartsWith("09") && !emergencyPhone.StartsWith("63")))
                    {
                        MessageBox.Show("Emergency phone must be 11 digits starting with 09 or 63.\nExample: 09123456789",
                            "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtEmergencyPhone.Focus();
                        return;
                    }
                }
                try
                {
                    btnRegister.Enabled = false;
                    btnRegister.Text = "Checking...";
                    var membersService = new Library_Management_System.Service.MembersService();
                    if (membersService.IsEmailRegistered(email))
                    {
                        MessageBox.Show("This email address is already registered in the system.\nPlease use a different email.",
                            "Duplicate Email", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtEmail.Focus();
                        btnRegister.Enabled = true;
                        btnRegister.Text = "Add Member";
                        return;
                    }
                    string idNumber = txtIdNumber.GetActualText().Trim();
                    if (!string.IsNullOrWhiteSpace(idNumber) && membersService.IsIdNumberRegistered(idNumber))
                    {
                        MessageBox.Show("This ID number is already registered to another member.\nPlease verify the ID number.",
                            "Duplicate ID Number", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtIdNumber.Focus();
                        btnRegister.Enabled = true;
                        btnRegister.Text = "Add Member";
                        return;
                    }
                    btnRegister.Text = "Registering...";
                    membersService.RegisterMember(
                        txtFirstName.GetActualText().Trim(),
                        txtLastName.GetActualText().Trim(),
                        email, 
                        txtPhone.GetActualText().Trim(),
                        txtAddress.GetActualText().Trim(),
                        cmbMemberType.SelectedItem.ToString(),
                        cmbStatus.SelectedItem.ToString(),
                        idNumber,
                        dtpDateOfBirth.Value.Date,
                        "", 
                        cmbDepartment.Text.Trim(),
                        txtEmergencyName.GetActualText().Trim(),
                        txtEmergencyPhone.GetActualText().Trim(),
                        dtpExpiryDate.Value.Date
                    );
                    Library_Management_System.Helper.AuditLogger.LogMemberRegistration(
                        SiginForm.CurrentUser?.Email ?? "Unknown",
                        email);
                    MessageBox.Show($"Member registered successfully!\n\nMember will be able to login with:\nEmail: {email}\nDefault Password: Member123!",
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    registerForm.DialogResult = DialogResult.OK;
                    registerForm.Close();
                }
                catch (Exception ex)
                {
                    string errorMessage = "Failed to register member. ";
                    if (ex.Message.Contains("Email already exists"))
                        errorMessage += "This email address is already registered.";
                    else if (ex.Message.Contains("ID number already exists"))
                        errorMessage += "This ID number is already registered.";
                    else if (ex.Message.Contains("not found"))
                        errorMessage += "Please check your input and try again.";
                    else
                        errorMessage += "Please contact system administrator if the problem persists.";
                    MessageBox.Show(errorMessage, "Registration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    System.Diagnostics.Debug.WriteLine($"Error registering member: {ex.Message}\n{ex.StackTrace}");
                    btnRegister.Enabled = true;
                    btnRegister.Text = "Add Member";
                }
            };
            registerForm.Load += (s, e) => txtFirstName.Focus();
            txtFirstName.TextChanged += (s, e) => {
                txtFirstName.BackColor = string.IsNullOrWhiteSpace(txtFirstName.Text) ?
                    Color.FromArgb(255, 245, 245) : Color.White;
            };
            txtLastName.TextChanged += (s, e) => {
                txtLastName.BackColor = string.IsNullOrWhiteSpace(txtLastName.Text) ?
                    Color.FromArgb(255, 245, 245) : Color.White;
            };
            txtEmail.TextChanged += (s, e) => {
                if (txtEmail.Text != "firstname.lastname.IDnumber.tc@umindanao.edu.ph") {
                    txtEmail.BackColor = string.IsNullOrWhiteSpace(txtEmail.Text) ?
                        Color.FromArgb(255, 245, 245) : Color.White;
                }
            };
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
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
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
                Location = new Point(65, 12),
                Size = new Size(125, 28),
                ForeColor = Color.FromArgb(40, 40, 40),
                TextAlign = ContentAlignment.MiddleLeft,
                Tag = "Value",
                BackColor = Color.Transparent
            };
            Label lblLabel = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 9F),
                Location = new Point(65, 40),
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
        private void ShowCatalogView()
        {
            pnlMembersView.Visible = false;
            ShowDashboardControls(false);
            ClearDynamicControls();
            Panel staffCatalogHeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 140,
                BackColor = Color.White
            };
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
            staffCatalogHeaderPanel.Controls.AddRange(new Control[] { titleLabel, subtitleLabel });
            Button btnAddBookHeader = new Button
            {
                Text = "+ Add Book",
                Location = new Point(pnlMainContent.Width - 150, 20),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(128, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAddBookHeader.FlatAppearance.BorderSize = 0;
            btnAddBookHeader.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAddBookHeader.Click += (s, e) => ShowAddBookDialog();
            Panel statsPanel = null;
            foreach (Control ctrl in pnlMainContent.Controls)
            {
                if (ctrl is Panel panel && panel.Tag?.ToString() == "CatalogStatsPanel")
                {
                    statsPanel = panel;
                    foreach (Control child in panel.Controls)
                    {
                        child.Dispose();
                    }
                    panel.Controls.Clear();
                    break;
                }
            }
            if (statsPanel == null)
            {
                statsPanel = new Panel
                {
                    Location = new Point(30, 150),
                    Size = new Size(pnlMainContent.Width - 60, 80),
                    BackColor = Color.Transparent,
                    Tag = "CatalogStatsPanel"
                };
            }
            Panel cardTotalTitles = CreateCatalogStatCard("📚", "0", "Total Titles", Color.FromArgb(128, 0, 0), new Point(0, 0));
            Panel cardAvailableCopies = CreateCatalogStatCard("📖", "0", "Available Copies", Color.FromArgb(76, 175, 80), new Point(200, 0));
            Panel cardTotalCopies = CreateCatalogStatCard("📚", "0", "Total Copies", Color.FromArgb(33, 150, 243), new Point(400, 0));
            Panel cardCategories = CreateCatalogStatCard("🔖", "0", "Categories", Color.FromArgb(255, 152, 0), new Point(600, 0));
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
                Text = "🔍",
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
            Button btnGridView = new Button
            {
                Text = "⊞",
                Location = new Point(searchPanel.Width - 100, 15),
                Size = new Size(40, 40),
                Font = new Font("Segoe UI", 14F),
                BackColor = Color.FromArgb(128, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = "Grid"
            };
            btnGridView.FlatAppearance.BorderSize = 0;
            Button btnListView = new Button
            {
                Text = "☰",
                Location = new Point(searchPanel.Width - 50, 15),
                Size = new Size(40, 40),
                Font = new Font("Segoe UI", 14F),
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(100, 100, 100),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = "List"
            };
            btnListView.FlatAppearance.BorderSize = 0;
            btnGridView.Click += (s, e) =>
            {
                btnGridView.BackColor = Color.FromArgb(128, 0, 0);
                btnGridView.ForeColor = Color.White;
                btnListView.BackColor = Color.FromArgb(240, 240, 240);
                btnListView.ForeColor = Color.FromArgb(100, 100, 100);
            };
            btnListView.Click += (s, e) =>
            {
                btnListView.BackColor = Color.FromArgb(128, 0, 0);
                btnListView.ForeColor = Color.White;
                btnGridView.BackColor = Color.FromArgb(240, 240, 240);
                btnGridView.ForeColor = Color.FromArgb(100, 100, 100);
            };
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
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
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
            DataGridViewButtonColumn editButtonColumn = new DataGridViewButtonColumn
            {
                Name = "Edit",
                HeaderText = "",
                Text = "Edit",
                UseColumnTextForButtonValue = true,
                Width = 60
            };
            editButtonColumn.DefaultCellStyle.BackColor = Color.FromArgb(33, 150, 243);
            editButtonColumn.DefaultCellStyle.ForeColor = Color.White;
            dgvBooks.Columns.Add(editButtonColumn);
            DataGridViewButtonColumn deleteButtonColumn = new DataGridViewButtonColumn
            {
                Name = "Delete",
                HeaderText = "",
                Text = "Delete",
                UseColumnTextForButtonValue = true,
                Width = 70
            };
            deleteButtonColumn.DefaultCellStyle.BackColor = Color.FromArgb(244, 67, 54);
            deleteButtonColumn.DefaultCellStyle.ForeColor = Color.White;
            dgvBooks.Columns.Add(deleteButtonColumn);
            dgvBooks.Columns["BookId"].Width = 60;
            dgvBooks.Columns["Title"].Width = 200;
            dgvBooks.Columns["Author"].Width = 150;
            dgvBooks.Columns["ISBN"].Width = 120;
            dgvBooks.Columns["Category"].Width = 100;
            dgvBooks.Columns["TotalCopies"].Width = 70;
            dgvBooks.Columns["AvailableCopies"].Width = 80;
            dgvBooks.Columns["BookId"].Visible = false;
            void LoadBooksData()
            {
                try
                {
                    var bookService = new Library_Management_System.Service.BookService();
                    string searchText = txtSearchBooks.GetActualText();
                    string categoryFilter = cmbCategoryFilter.SelectedItem?.ToString() ?? "All Categories";
                    var books = bookService.GetBooks(searchText, categoryFilter);
                    dgvBooks.Rows.Clear();
                    int totalTitles = books.Count;
                    int totalCopies = 0;
                    int availableCopies = 0;
                    var categories = new HashSet<string>();
                    foreach (var book in books)
                    {
                        totalCopies += book.TotalCopies;
                        availableCopies += book.AvailableCopies;
                        if (!string.IsNullOrEmpty(book.Category))
                            categories.Add(book.Category);
                        int rowIndex = dgvBooks.Rows.Add();
                        dgvBooks.Rows[rowIndex].Cells["BookId"].Value = book.BookId;
                        dgvBooks.Rows[rowIndex].Cells["Title"].Value = book.Title;
                        dgvBooks.Rows[rowIndex].Cells["Author"].Value = book.Author;
                        dgvBooks.Rows[rowIndex].Cells["ISBN"].Value = book.ISBN ?? "N/A";
                        dgvBooks.Rows[rowIndex].Cells["Category"].Value = book.Category ?? "Uncategorized";
                        dgvBooks.Rows[rowIndex].Cells["TotalCopies"].Value = book.TotalCopies;
                        dgvBooks.Rows[rowIndex].Cells["AvailableCopies"].Value = book.AvailableCopies;
                    }
                    UpdateCatalogStatCard(cardTotalTitles, totalTitles.ToString());
                    UpdateCatalogStatCard(cardAvailableCopies, availableCopies.ToString());
                    UpdateCatalogStatCard(cardTotalCopies, totalCopies.ToString());
                    var allCategories = bookService.GetCategories();
                    UpdateCatalogStatCard(cardCategories, allCategories.Count.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading books: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            void LoadCategories()
            {
                try
                {
                    cmbCategoryFilter.Items.Clear();
                    cmbCategoryFilter.Items.Add("All Categories");
                    var bookService = new Library_Management_System.Service.BookService();
                    var categories = bookService.GetCategories();
                    foreach (var category in categories)
                    {
                        cmbCategoryFilter.Items.Add(category);
                    }
                    cmbCategoryFilter.SelectedIndex = 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading categories: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            txtSearchBooks.TextChanged += (s, e) =>
            {
                if (txtSearchBooks.ForeColor != Color.Gray)
                {
                    LoadBooksData();
                }
            };
            cmbCategoryFilter.SelectedIndexChanged += (s, e) => LoadBooksData();
            dgvBooks.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string columnName = dgvBooks.Columns[e.ColumnIndex].Name;
                string bookId = dgvBooks.Rows[e.RowIndex].Cells["BookId"].Value.ToString();
                if (columnName == "Edit")
                {
                    ShowEditBookDialog(bookId);
                }
                else if (columnName == "Delete")
                {
                    if (MessageBox.Show("Are you sure you want to delete this book?", "Confirm Delete",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        try
                        {
                            var bookService = new Library_Management_System.Service.BookService();
                            bookService.DeleteBook(bookId);
                            MessageBox.Show("Book deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadBooksData(); 
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error deleting book: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            };
            searchContainer.Controls.AddRange(new Control[] { lblSearchIcon, txtSearchBooks });
            searchPanel.Controls.AddRange(new Control[] { searchContainer, lblCategoryFilter, cmbCategoryFilter, btnGridView, btnListView });
            List<Control> controlsToAdd = new List<Control>();
            if (!pnlMainContent.Controls.Contains(titleLabel)) controlsToAdd.Add(titleLabel);
            if (!pnlMainContent.Controls.Contains(subtitleLabel)) controlsToAdd.Add(subtitleLabel);
            if (!pnlMainContent.Controls.Contains(btnAddBookHeader)) controlsToAdd.Add(btnAddBookHeader);
            if (!pnlMainContent.Controls.Contains(statsPanel)) controlsToAdd.Add(statsPanel);
            if (!pnlMainContent.Controls.Contains(searchPanel)) controlsToAdd.Add(searchPanel);
            if (!pnlMainContent.Controls.Contains(dgvBooks)) controlsToAdd.Add(dgvBooks);
            if (controlsToAdd.Count > 0)
            {
                pnlMainContent.Controls.AddRange(controlsToAdd.ToArray());
            }
            LoadCategories();
            LoadBooksData();
        }
        private void ShowAddBookDialog()
        {
            Form addForm = new Form
            {
                Text = "Add New Book",
                Size = new Size(900, 750),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = true,
                ShowInTaskbar = false,
                BackColor = Color.FromArgb(245, 245, 245)
            };
            Panel staffAddBookHeaderPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(addForm.Width, 80),
                BackColor = Color.White,
                Dock = DockStyle.Top
            };
            Label titleLabel = new Label
            {
                Text = "Add New Book",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(300, 65),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            Label subtitleLabel = new Label
            {
                Text = "Enter the book details to add it to the catalog",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(30, 100),
                Size = new Size(500, 20),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
            staffAddBookHeaderPanel.Controls.AddRange(new Control[] { titleLabel, subtitleLabel });
            Panel contentPanel = new Panel
            {
                BackColor = Color.White,
                AutoScroll = true,
                Dock = DockStyle.Fill
            };
            int leftColumnX = 30;
            int rightColumnX = 480;
            int startY = 30;
            int fieldSpacing = 50;
            int fieldHeight = 30;
            int labelHeight = 20;
            int fieldWidth = 380;
            Label lblTitle = new Label { Text = "Title *", Location = new Point(leftColumnX, startY), Size = new Size(150, labelHeight), Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(40, 40, 40) };
            TextBox txtTitle = new TextBox 
            { 
                Location = new Point(leftColumnX, startY + labelHeight + 5), 
                Size = new Size(fieldWidth, fieldHeight), 
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.White
            };
            txtTitle.SetPlaceholder("Book title");
            startY += fieldSpacing;
            Label lblISBN = new Label { Text = "ISBN *", Location = new Point(leftColumnX, startY), Size = new Size(150, labelHeight), Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(40, 40, 40) };
            TextBox txtISBN = new TextBox 
            { 
                Location = new Point(leftColumnX, startY + labelHeight + 5), 
                Size = new Size(fieldWidth, fieldHeight), 
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.White,
                Text = "978-0-00-000000-0"
            };
            startY += fieldSpacing;
            Label lblAuthor = new Label { Text = "Author *", Location = new Point(leftColumnX, startY), Size = new Size(150, labelHeight), Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(40, 40, 40) };
            TextBox txtAuthor = new TextBox 
            { 
                Location = new Point(leftColumnX, startY + labelHeight + 5), 
                Size = new Size(fieldWidth, fieldHeight), 
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.White
            };
            txtAuthor.SetPlaceholder("Author name");
            startY += fieldSpacing;
            Label lblYear = new Label { Text = "Publication Year", Location = new Point(leftColumnX, startY), Size = new Size(150, labelHeight), Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(40, 40, 40) };
            NumericUpDown numYear = new NumericUpDown
            {
                Location = new Point(leftColumnX, startY + labelHeight + 5),
                Size = new Size(fieldWidth, fieldHeight),
                Font = new Font("Segoe UI", 10F),
                Minimum = 1000,
                Maximum = DateTime.Now.Year + 10,
                Value = DateTime.Now.Year,
                BackColor = Color.White
            };
            startY += fieldSpacing;
            Label lblLanguage = new Label { Text = "Language", Location = new Point(leftColumnX, startY), Size = new Size(150, labelHeight), Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(40, 40, 40) };
            TextBox txtLanguage = new TextBox 
            { 
                Location = new Point(leftColumnX, startY + labelHeight + 5), 
                Size = new Size(fieldWidth, fieldHeight), 
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.White,
                Text = "English"
            };
            startY += fieldSpacing;
            Label lblResourceType = new Label { Text = "Resource Type", Location = new Point(leftColumnX, startY), Size = new Size(150, labelHeight), Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(40, 40, 40) };
            ComboBox cmbResourceType = new ComboBox
            {
                Location = new Point(leftColumnX, startY + labelHeight + 5),
                Size = new Size(fieldWidth, fieldHeight),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White
            };
            cmbResourceType.Items.AddRange(new[] { "Book", "Journal", "Magazine", "E-Book", "Reference", "Other" });
            cmbResourceType.SelectedIndex = 0;
            startY += fieldSpacing;
            Label lblDescription = new Label { Text = "Description", Location = new Point(leftColumnX, startY), Size = new Size(150, labelHeight), Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(40, 40, 40) };
            TextBox txtDescription = new TextBox
            {
                Location = new Point(leftColumnX, startY + labelHeight + 5),
                Size = new Size(fieldWidth, 100),
                Font = new Font("Segoe UI", 10F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White
            };
            txtDescription.SetPlaceholder("Brief description of the book...");
            startY = 30;
            Label lblSubtitle = new Label { Text = "Subtitle", Location = new Point(rightColumnX, startY), Size = new Size(150, labelHeight), Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(40, 40, 40) };
            TextBox txtSubtitle = new TextBox 
            { 
                Location = new Point(rightColumnX, startY + labelHeight + 5), 
                Size = new Size(fieldWidth, fieldHeight), 
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.White
            };
            txtSubtitle.SetPlaceholder("Optional subtitle");
            startY += fieldSpacing;
            Label lblCategory = new Label { Text = "Category *", Location = new Point(rightColumnX, startY), Size = new Size(150, labelHeight), Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(40, 40, 40) };
            ComboBox cmbCategory = new ComboBox
            {
                Location = new Point(rightColumnX, startY + labelHeight + 5),
                Size = new Size(fieldWidth, fieldHeight),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDown,
                BackColor = Color.White,
                ForeColor = Color.Gray
            };
            cmbCategory.Text = "Select category";
            cmbCategory.Enter += (s, e) =>
            {
                if (cmbCategory.Text == "Select category")
                {
                    cmbCategory.Text = "";
                    cmbCategory.ForeColor = Color.Black;
                }
            };
            cmbCategory.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(cmbCategory.Text))
                {
                    cmbCategory.Text = "Select category";
                    cmbCategory.ForeColor = Color.Gray;
                }
            };
            startY += fieldSpacing;
            Label lblPublisher = new Label { Text = "Publisher *", Location = new Point(rightColumnX, startY), Size = new Size(150, labelHeight), Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(40, 40, 40) };
            TextBox txtPublisher = new TextBox 
            { 
                Location = new Point(rightColumnX, startY + labelHeight + 5), 
                Size = new Size(fieldWidth, fieldHeight), 
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.White
            };
            txtPublisher.SetPlaceholder("Publisher name");
            startY += fieldSpacing;
            Label lblPages = new Label { Text = "Pages", Location = new Point(rightColumnX, startY), Size = new Size(150, labelHeight), Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(40, 40, 40) };
            NumericUpDown numPages = new NumericUpDown
            {
                Location = new Point(rightColumnX, startY + labelHeight + 5),
                Size = new Size(fieldWidth, fieldHeight),
                Font = new Font("Segoe UI", 10F),
                Minimum = 0,
                Maximum = 10000,
                Value = 0,
                BackColor = Color.White
            };
            startY += fieldSpacing;
            Label lblCopies = new Label { Text = "Copies", Location = new Point(rightColumnX, startY), Size = new Size(150, labelHeight), Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(40, 40, 40) };
            NumericUpDown numCopies = new NumericUpDown
            {
                Location = new Point(rightColumnX, startY + labelHeight + 5),
                Size = new Size(fieldWidth, fieldHeight),
                Font = new Font("Segoe UI", 10F),
                Minimum = 1,
                Maximum = 1000,
                Value = 1,
                BackColor = Color.White
            };
            startY += fieldSpacing;
            Label lblLocation = new Label { Text = "Location *", Location = new Point(rightColumnX, startY), Size = new Size(150, labelHeight), Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(40, 40, 40) };
            TextBox txtLocation = new TextBox 
            { 
                Location = new Point(rightColumnX, startY + labelHeight + 5), 
                Size = new Size(fieldWidth, fieldHeight), 
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.White,
                Text = "Section A, Shelf 1"
            };
            contentPanel.Controls.AddRange(new Control[] {
                lblTitle, txtTitle, lblISBN, txtISBN, lblAuthor, txtAuthor,
                lblYear, numYear, lblLanguage, txtLanguage, lblResourceType, cmbResourceType,
                lblDescription, txtDescription,
                lblSubtitle, txtSubtitle, lblCategory, cmbCategory, lblPublisher, txtPublisher,
                lblPages, numPages, lblCopies, numCopies, lblLocation, txtLocation
            });
            Panel buttonPanel = new Panel
            {
                Height = 80,
                BackColor = Color.White,
                Dock = DockStyle.Bottom
            };
            Button btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 40),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.FromArgb(40, 40, 40),
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.None
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            Button btnSave = new Button
            {
                Text = "Add Book",
                Size = new Size(110, 40),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(128, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.None
            };
            btnSave.FlatAppearance.BorderSize = 0;
            buttonPanel.Controls.AddRange(new Control[] { btnCancel, btnSave });
            void PositionButtons()
            {
                if (buttonPanel.Width > 0)
                {
                    btnCancel.Location = new Point(buttonPanel.Width - 230, 20);
                    btnSave.Location = new Point(buttonPanel.Width - 120, 20);
                }
            }
            buttonPanel.Layout += (s, e) => PositionButtons();
            addForm.Shown += (s, e) => PositionButtons();
            cmbCategory.Items.AddRange(new[] {
                "Programming",
                "Data Structures",
                "Algorithms",
                "Database Systems",
                "Web Development",
                "Software Engineering",
                "Computer Networks",
                "Operating Systems",
                "Cybersecurity",
                "Artificial Intelligence",
                "Machine Learning",
                "Computer Graphics",
                "Mobile Development",
                "Cloud Computing",
                "Information Systems",
                "Computer Architecture",
                "Data Science",
                "Game Development"
            });
            btnSave.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtTitle.GetActualText()))
                {
                    MessageBox.Show("Please enter a book title.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtTitle.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtAuthor.GetActualText()))
                {
                    MessageBox.Show("Please enter an author.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtAuthor.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(cmbCategory.Text) || cmbCategory.Text == "Select category" || cmbCategory.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select a category.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbCategory.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtPublisher.GetActualText()))
                {
                    MessageBox.Show("Please enter a publisher.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPublisher.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtLocation.GetActualText()))
                {
                    MessageBox.Show("Please enter a location.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtLocation.Focus();
                    return;
                }
                try
                {
                    var bookService = new Library_Management_System.Service.BookService();
                    bookService.AddBook(
                        txtISBN.GetActualText().Trim(),
                        txtTitle.GetActualText().Trim(),
                        txtAuthor.GetActualText().Trim(),
                        txtPublisher.GetActualText().Trim(),
                        (int?)numYear.Value,
                        cmbCategory.Text.Trim(),
                        (int)numCopies.Value,
                        txtDescription.GetActualText().Trim()
                    );
                    MessageBox.Show("Book added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    addForm.DialogResult = DialogResult.OK;
                    addForm.Close();
                    ShowCatalogView();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding book: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            addForm.AcceptButton = btnSave;
            addForm.CancelButton = btnCancel;
            btnCancel.Click += (s, e) => addForm.Close();
            addForm.Controls.Add(staffAddBookHeaderPanel);
            addForm.Controls.Add(buttonPanel);
            addForm.Controls.Add(contentPanel);
            addForm.ShowDialog();
        }
        private void ShowEditBookDialog(string bookId)
        {
            try
            {
                var bookService = new Library_Management_System.Service.BookService();
                var book = bookService.GetBookById(bookId);
                if (book == null)
                {
                    MessageBox.Show("Book not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                Form editForm = new Form
                {
                    Text = "Edit Book",
                    Size = new Size(520, 710),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    ControlBox = false,
                    ShowInTaskbar = false
                };
                Label titleLabel = new Label
                {
                    Text = "Edit Book",
                    Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                    Location = new Point(30, 20),
                    Size = new Size(400, 65),
                    ForeColor = Color.FromArgb(40, 40, 40)
                };
                int startY = 100;
                int fieldHeight = 35;
                int labelWidth = 120;
                int fieldWidth = 330;
                Label lblISBN = new Label { Text = "ISBN:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
                TextBox txtISBN = new TextBox
                {
                    Location = new Point(160, startY),
                    Size = new Size(fieldWidth, fieldHeight),
                    Font = new Font("Segoe UI", 10F),
                    Text = book.ISBN ?? ""
                };
                startY += 45;
                Label lblTitle = new Label { Text = "Title:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
                TextBox txtTitle = new TextBox
                {
                    Location = new Point(160, startY),
                    Size = new Size(fieldWidth, fieldHeight),
                    Font = new Font("Segoe UI", 10F),
                    Text = book.Title
                };
                startY += 45;
                Label lblAuthor = new Label { Text = "Author:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
                TextBox txtAuthor = new TextBox
                {
                    Location = new Point(160, startY),
                    Size = new Size(fieldWidth, fieldHeight),
                    Font = new Font("Segoe UI", 10F),
                    Text = book.Author
                };
                startY += 45;
                Label lblPublisher = new Label { Text = "Publisher:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
                TextBox txtPublisher = new TextBox
                {
                    Location = new Point(160, startY),
                    Size = new Size(fieldWidth, fieldHeight),
                    Font = new Font("Segoe UI", 10F),
                    Text = book.Publisher ?? ""
                };
                startY += 45;
                Label lblYear = new Label { Text = "Publication Year:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
                NumericUpDown numYear = new NumericUpDown
                {
                    Location = new Point(160, startY),
                    Size = new Size(fieldWidth, fieldHeight),
                    Font = new Font("Segoe UI", 10F),
                    Minimum = 1000,
                    Maximum = DateTime.Now.Year,
                    Value = book.PublicationYear ?? DateTime.Now.Year
                };
                startY += 45;
                Label lblCategory = new Label { Text = "Category:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
                ComboBox cmbCategory = new ComboBox
                {
                    Location = new Point(160, startY),
                    Size = new Size(fieldWidth, fieldHeight),
                    Font = new Font("Segoe UI", 10F),
                    DropDownStyle = ComboBoxStyle.DropDown,
                    Text = book.Category ?? ""
                };
                startY += 45;
                Label lblCopies = new Label { Text = "Total Copies:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
                NumericUpDown numCopies = new NumericUpDown
                {
                    Location = new Point(160, startY),
                    Size = new Size(fieldWidth, fieldHeight),
                    Font = new Font("Segoe UI", 10F),
                    Minimum = 1,
                    Maximum = 1000,
                    Value = book.TotalCopies
                };
                startY += 45;
                Label lblAvailable = new Label
                {
                    Text = $"Available Copies: {book.AvailableCopies}",
                    Location = new Point(30, startY),
                    Size = new Size(300, 25),
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = Color.FromArgb(100, 100, 100)
                };
                startY += 35;
                Label lblDescription = new Label { Text = "Description:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
                TextBox txtDescription = new TextBox
                {
                    Location = new Point(160, startY),
                    Size = new Size(fieldWidth, 80),
                    Font = new Font("Segoe UI", 10F),
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    Text = book.Description ?? ""
                };
                Button btnCancel = new Button
                {
                    Text = "Cancel",
                    Location = new Point(290, startY + 100),
                    Size = new Size(80, 35),
                    Font = new Font("Segoe UI", 10F),
                    DialogResult = DialogResult.Cancel
                };
                Button btnSave = new Button
                {
                    Text = "Update Book",
                    Location = new Point(380, startY + 100),
                    Size = new Size(100, 35),
                    Font = new Font("Segoe UI", 10F),
                    BackColor = Color.FromArgb(33, 150, 243),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnSave.FlatAppearance.BorderSize = 0;
                try
                {
                    var categories = bookService.GetCategories();
                    cmbCategory.Items.AddRange(categories.ToArray());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading categories: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                txtTitle.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtAuthor.Focus(); };
                txtAuthor.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtPublisher.Focus(); };
                txtPublisher.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) numYear.Focus(); };
                numYear.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) cmbCategory.Focus(); };
                cmbCategory.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) numCopies.Focus(); };
                numCopies.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtDescription.Focus(); };
                txtDescription.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter && e.Control) btnSave.PerformClick(); };
                btnSave.Click += (s, args) =>
                {
                    if (string.IsNullOrWhiteSpace(txtTitle.Text))
                    {
                        MessageBox.Show("Please enter a book title.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtTitle.Focus();
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(txtAuthor.Text))
                    {
                        MessageBox.Show("Please enter an author.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtAuthor.Focus();
                        return;
                    }
                    try
                    {
                        bookService.UpdateBook(
                            bookId,
                            txtISBN.Text.Trim(),
                            txtTitle.Text.Trim(),
                            txtAuthor.Text.Trim(),
                            txtPublisher.Text.Trim(),
                            (int?)numYear.Value,
                            cmbCategory.Text.Trim(),
                            (int)numCopies.Value,
                            txtDescription.Text.Trim()
                        );
                        MessageBox.Show("Book updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        editForm.DialogResult = DialogResult.OK;
                        editForm.Close();
                        ShowCatalogView();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating book: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                editForm.AcceptButton = btnSave;
                editForm.CancelButton = btnCancel;
                btnCancel.Click += (s, e) => editForm.Close();
                editForm.Controls.AddRange(new Control[] {
                    titleLabel, lblISBN, txtISBN, lblTitle, txtTitle, lblAuthor, txtAuthor,
                    lblPublisher, txtPublisher, lblYear, numYear, lblCategory, cmbCategory,
                    lblCopies, numCopies, lblAvailable, lblDescription, txtDescription, btnCancel, btnSave
                });
                editForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading book: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ShowCirculationView()
        {
            pnlMembersView.Visible = false;
            ShowDashboardControls(false);
            ClearDynamicControls();
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 140,
                BackColor = Color.White
            };
            Label titleLabel = new Label
            {
                Text = "Circulation",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(300, 70),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            Label subtitleLabel = new Label
            {
                Text = "Manage book borrowings, returns, and renewals",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Location = new Point(30, 95),
                Size = new Size(500, 25),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
            headerPanel.Controls.AddRange(new Control[] { titleLabel, subtitleLabel });
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
                Text = "🔍",
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
                BackColor = Color.FromArgb(128, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnCheckout.FlatAppearance.BorderSize = 0;
            btnCheckout.MouseEnter += (s, e) => btnCheckout.BackColor = Color.FromArgb(150, 0, 0);
            btnCheckout.MouseLeave += (s, e) => btnCheckout.BackColor = Color.FromArgb(128, 0, 0);
            Button btnReturn = new Button
            {
                Text = "Return",
                Location = new Point(pnlMainContent.Width - 130, 20),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(128, 0, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnReturn.FlatAppearance.BorderSize = 1;
            btnReturn.FlatAppearance.BorderColor = Color.FromArgb(128, 0, 0);
            btnReturn.MouseEnter += (s, e) => btnReturn.BackColor = Color.FromArgb(250, 250, 250);
            btnReturn.MouseLeave += (s, e) => btnReturn.BackColor = Color.White;
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
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
                SelectionBackColor = Color.FromArgb(128, 0, 0),
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
            DataGridViewButtonColumn returnButtonColumn = new DataGridViewButtonColumn
            {
                Name = "Return",
                HeaderText = "",
                Text = "Return",
                UseColumnTextForButtonValue = true,
                Width = 70
            };
            returnButtonColumn.DefaultCellStyle.BackColor = Color.FromArgb(255, 152, 0);
            returnButtonColumn.DefaultCellStyle.ForeColor = Color.White;
            dgvBorrowings.Columns.Add(returnButtonColumn);
            DataGridViewButtonColumn renewButtonColumn = new DataGridViewButtonColumn
            {
                Name = "Renew",
                HeaderText = "",
                Text = "Renew",
                UseColumnTextForButtonValue = true,
                Width = 70
            };
            renewButtonColumn.DefaultCellStyle.BackColor = Color.FromArgb(33, 150, 243);
            renewButtonColumn.DefaultCellStyle.ForeColor = Color.White;
            dgvBorrowings.Columns.Add(renewButtonColumn);
            dgvBorrowings.Columns["BorrowingId"].Width = 80;
            dgvBorrowings.Columns["AccessionNumber"].Width = 120;
            dgvBorrowings.Columns["BookTitle"].Width = 200;
            dgvBorrowings.Columns["MemberName"].Width = 150;
            dgvBorrowings.Columns["BorrowDate"].Width = 100;
            dgvBorrowings.Columns["DueDate"].Width = 100;
            dgvBorrowings.Columns["Status"].Width = 100;
            dgvBorrowings.Columns["Fine"].Width = 80;
            dgvBorrowings.Columns["BorrowingId"].Visible = false;
            void LoadBorrowingsData()
            {
                try
                {
                    var circulationService = new Library_Management_System.Service.CirculationService();
                    string searchText = txtSearchBorrowings.GetActualText();
                    string statusFilter = cmbStatusFilter.SelectedItem?.ToString() ?? "All Status";
                    var borrowings = circulationService.GetBorrowings(searchText, statusFilter);
                    dgvBorrowings.Rows.Clear();
                    int currentlyBorrowed = 0;
                    int overdue = 0;
                    int returnedToday = 0;
                    DateTime today = DateTime.Now.Date;
                    foreach (var borrowing in borrowings)
                    {
                        if (!borrowing.ReturnDate.HasValue)
                        {
                            currentlyBorrowed++;
                            if (DateTime.Now > borrowing.DueDate)
                            {
                                overdue++;
                            }
                        }
                        else if (borrowing.ReturnDate.Value.Date == today)
                        {
                            returnedToday++;
                        }
                        int rowIndex = dgvBorrowings.Rows.Add();
                        dgvBorrowings.Rows[rowIndex].Cells["BorrowingId"].Value = borrowing.BorrowingId;
                        string accessionNumber = $"ACC-{borrowing.BorrowDate:yyyy}-{borrowing.BorrowingId.PadLeft(5, '0')}";
                        dgvBorrowings.Rows[rowIndex].Cells["AccessionNumber"].Value = accessionNumber;
                        dgvBorrowings.Rows[rowIndex].Cells["BookTitle"].Value = borrowing.BookTitle;
                        dgvBorrowings.Rows[rowIndex].Cells["MemberName"].Value = borrowing.MemberName;
                        dgvBorrowings.Rows[rowIndex].Cells["BorrowDate"].Value = borrowing.BorrowDate.ToString("MMM dd, yyyy");
                        var dueDateCell = dgvBorrowings.Rows[rowIndex].Cells["DueDate"];
                        dueDateCell.Value = borrowing.DueDate.ToString("MMM dd, yyyy");
                        if (!borrowing.ReturnDate.HasValue && DateTime.Now > borrowing.DueDate)
                        {
                            dueDateCell.Style.ForeColor = Color.FromArgb(244, 67, 54); 
                        }
                        var statusCell = dgvBorrowings.Rows[rowIndex].Cells["Status"];
                        if (borrowing.ReturnDate.HasValue)
                        {
                            statusCell.Value = "Returned";
                            statusCell.Style.ForeColor = Color.FromArgb(76, 175, 80); 
                        }
                        else if (DateTime.Now > borrowing.DueDate)
                        {
                            statusCell.Value = "Overdue";
                            statusCell.Style.ForeColor = Color.FromArgb(244, 67, 54); 
                        }
                        else
                        {
                            statusCell.Value = "Active";
                            statusCell.Style.ForeColor = Color.FromArgb(33, 150, 243); 
                        }
                        var fineCell = dgvBorrowings.Rows[rowIndex].Cells["Fine"];
                        if (borrowing.FineAmount > 0)
                        {
                            fineCell.Value = $"${borrowing.FineAmount:F2}";
                            fineCell.Style.ForeColor = Color.FromArgb(244, 67, 54); 
                        }
                        else
                        {
                            fineCell.Value = "-";
                        }
                        if (borrowing.ReturnDate.HasValue)
                        {
                            dgvBorrowings.Rows[rowIndex].Cells["Return"].Value = "";
                            dgvBorrowings.Rows[rowIndex].Cells["Renew"].Value = "";
                            dgvBorrowings.Rows[rowIndex].Cells["Return"].Style.BackColor = Color.Transparent;
                            dgvBorrowings.Rows[rowIndex].Cells["Renew"].Style.BackColor = Color.Transparent;
                        }
                        else
                        {
                            dgvBorrowings.Rows[rowIndex].Cells["Return"].Value = "Return";
                            dgvBorrowings.Rows[rowIndex].Cells["Renew"].Value = "Renew";
                        }
                    }
                    UpdateCatalogStatCard(cardCurrentlyBorrowed, currentlyBorrowed.ToString());
                    UpdateCatalogStatCard(cardOverdue, overdue.ToString());
                    UpdateCatalogStatCard(cardReturnedToday, returnedToday.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading borrowings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            txtSearchBorrowings.TextChanged += (s, e) =>
            {
                if (txtSearchBorrowings.ForeColor != Color.Gray)
                {
                    LoadBorrowingsData();
                }
            };
            cmbStatusFilter.SelectedIndexChanged += (s, e) => LoadBorrowingsData();
            btnCheckout.Click += (s, e) => ShowCheckoutDialog();
            btnReturn.Click += (s, e) => ShowReturnDialog();
            dgvBorrowings.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string columnName = dgvBorrowings.Columns[e.ColumnIndex].Name;
                string borrowingId = dgvBorrowings.Rows[e.RowIndex].Cells["BorrowingId"].Value.ToString();
                if (columnName == "Return")
                {
                    var borrowing = new Library_Management_System.Service.CirculationService().GetBorrowingById(borrowingId);
                    if (borrowing != null && !borrowing.ReturnDate.HasValue)
                    {
                            if (MessageBox.Show($"Return '{borrowing.BookTitle}' for {borrowing.MemberName}?",
                                "Confirm Return", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            {
                                try
                                {
                                    var circulationService = new Library_Management_System.Service.CirculationService();
                                    circulationService.ReturnBook(borrowingId);
                                    LoadBorrowingsData();
                                    MessageBox.Show("Book returned successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Error returning book: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                        }
                    }
                }
                else if (columnName == "Renew")
                {
                    var borrowing = new Library_Management_System.Service.CirculationService().GetBorrowingById(borrowingId);
                    if (borrowing != null && !borrowing.ReturnDate.HasValue)
                    {
                        if (MessageBox.Show($"Renew '{borrowing.BookTitle}' for {borrowing.MemberName}?",
                            "Confirm Renewal", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            try
                            {
                                var circulationService = new Library_Management_System.Service.CirculationService();
                                circulationService.RenewBook(borrowingId);
                                LoadBorrowingsData();
                                MessageBox.Show("Book renewed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error renewing book: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            };
            searchContainer.Controls.AddRange(new Control[] { searchIcon, txtSearchBorrowings });
            searchPanel.Controls.AddRange(new Control[] { searchContainer, lblStatusFilter, cmbStatusFilter });
            pnlMainContent.Controls.AddRange(new Control[] { headerPanel, btnCheckout, btnReturn, statsPanel, searchPanel, dgvBorrowings });
            LoadBorrowingsData();
        }
        private void ShowCheckoutDialog()
        {
            Form checkoutForm = new Form
            {
                Text = "Book Checkout",
                Size = new Size(500, 430),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false
            };
            Label titleLabel = new Label
            {
                Text = "Checkout Book",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(400, 65),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            int startY = 100;
            Label lblMemberId = new Label { Text = "Member ID:", Location = new Point(30, startY), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtMemberId = new TextBox { Location = new Point(140, startY), Size = new Size(300, 30), Font = new Font("Segoe UI", 10F) };
            startY += 50;
            Label lblBookId = new Label { Text = "Book ID:", Location = new Point(30, startY), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtBookId = new TextBox { Location = new Point(140, startY), Size = new Size(300, 30), Font = new Font("Segoe UI", 10F) };
            startY += 50;
            Label lblLoanPeriod = new Label { Text = "Loan Period (days):", Location = new Point(30, startY), Size = new Size(120, 25), Font = new Font("Segoe UI", 10F) };
            NumericUpDown numLoanPeriod = new NumericUpDown
            {
                Location = new Point(160, startY),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 10F),
                Minimum = 1,
                Maximum = 60,
                Value = 14
            };
            Label lblDueDate = new Label
            {
                Text = $"Due Date: {DateTime.Now.AddDays(14):yyyy-MM-dd}",
                Location = new Point(280, startY),
                Size = new Size(160, 25),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(250, startY + 80),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                DialogResult = DialogResult.Cancel
            };
            Button btnCheckout = new Button
            {
                Text = "Checkout",
                Location = new Point(340, startY + 80),
                Size = new Size(90, 35),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCheckout.FlatAppearance.BorderSize = 0;
            numLoanPeriod.ValueChanged += (s, e) =>
            {
                lblDueDate.Text = $"Due Date: {DateTime.Now.AddDays((int)numLoanPeriod.Value):yyyy-MM-dd}";
            };
            txtMemberId.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtBookId.Focus(); };
            txtBookId.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) numLoanPeriod.Focus(); };
            numLoanPeriod.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnCheckout.PerformClick(); };
            btnCheckout.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtMemberId.GetActualText()))
                {
                    MessageBox.Show("Please enter a member ID.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMemberId.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtBookId.GetActualText()))
                {
                    MessageBox.Show("Please enter a book ID.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtBookId.Focus();
                    return;
                }
                try
                {
                    var circulationService = new Library_Management_System.Service.CirculationService();
                    circulationService.CheckoutBook(txtMemberId.GetActualText().Trim(), txtBookId.GetActualText().Trim(), (int)numLoanPeriod.Value);
                    MessageBox.Show("Book checked out successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    checkoutForm.DialogResult = DialogResult.OK;
                    checkoutForm.Close();
                    ShowCirculationView();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error checking out book: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            checkoutForm.AcceptButton = btnCheckout;
            checkoutForm.CancelButton = btnCancel;
            btnCancel.Click += (s, e) => checkoutForm.Close();
            txtMemberId.SetPlaceholder("Enter member ID");
            txtBookId.SetPlaceholder("Enter book ID");
            checkoutForm.Controls.AddRange(new Control[] {
                titleLabel, lblMemberId, txtMemberId, lblBookId, txtBookId,
                lblLoanPeriod, numLoanPeriod, lblDueDate, btnCancel, btnCheckout
            });
            checkoutForm.ShowDialog();
        }
        private void ShowReturnDialog()
        {
            Form returnForm = new Form
            {
                Text = "Book Return",
                Size = new Size(400, 280),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false
            };
            Label titleLabel = new Label
            {
                Text = "Return Book",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(300, 65),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            Label lblBorrowingId = new Label { Text = "Borrowing ID:", Location = new Point(30, 100), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtBorrowingId = new TextBox { Location = new Point(140, 95), Size = new Size(200, 30), Font = new Font("Segoe UI", 10F) };
            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(150, 180),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                DialogResult = DialogResult.Cancel
            };
            Button btnReturn = new Button
            {
                Text = "Return",
                Location = new Point(240, 180),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnReturn.FlatAppearance.BorderSize = 0;
            txtBorrowingId.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnReturn.PerformClick(); };
            btnReturn.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtBorrowingId.GetActualText()))
                {
                    MessageBox.Show("Please enter a borrowing ID.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtBorrowingId.Focus();
                    return;
                }
                try
                {
                    var circulationService = new Library_Management_System.Service.CirculationService();
                    circulationService.ReturnBook(txtBorrowingId.GetActualText().Trim());
                    MessageBox.Show("Book returned successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    returnForm.DialogResult = DialogResult.OK;
                    returnForm.Close();
                    ShowCirculationView();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error returning book: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            returnForm.AcceptButton = btnReturn;
            returnForm.CancelButton = btnCancel;
            btnCancel.Click += (s, e) => returnForm.Close();
            txtBorrowingId.SetPlaceholder("Enter borrowing ID");
            returnForm.Controls.AddRange(new Control[] {
                titleLabel, lblBorrowingId, txtBorrowingId, btnCancel, btnReturn
            });
            returnForm.ShowDialog();
        }
        private void ShowBorrowingDetailsDialog(Library_Management_System.Service.CirculationService.BorrowingInfo borrowing)
        {
            Form detailsForm = new Form
            {
                Text = "Borrowing Details",
                Size = new Size(500, 430),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false
            };
            Label titleLabel = new Label
            {
                Text = "Borrowing Details",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(400, 65),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            int startY = 100;
            int labelWidth = 120;
            int fieldWidth = 300;
            Label lblBorrowingId = new Label { Text = "Borrowing ID:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblBorrowingIdValue = new Label { Text = borrowing.BorrowingId, Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            startY += 35;
            Label lblMember = new Label { Text = "Member:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblMemberValue = new Label { Text = borrowing.MemberName, Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F) };
            startY += 35;
            Label lblBook = new Label { Text = "Book:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblBookValue = new Label { Text = borrowing.BookTitle, Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F) };
            startY += 35;
            Label lblISBN = new Label { Text = "ISBN:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblISBNValue = new Label { Text = borrowing.BookISBN ?? "N/A", Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F) };
            startY += 35;
            Label lblBorrowDate = new Label { Text = "Borrow Date:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblBorrowDateValue = new Label { Text = borrowing.BorrowDate.ToString("yyyy-MM-dd"), Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F) };
            startY += 35;
            Label lblDueDate = new Label { Text = "Due Date:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblDueDateValue = new Label { Text = borrowing.DueDate.ToString("yyyy-MM-dd"), Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F) };
            startY += 35;
            Label lblReturnDate = new Label { Text = "Return Date:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblReturnDateValue = new Label
            {
                Text = borrowing.ReturnDate?.ToString("yyyy-MM-dd") ?? "Not returned",
                Location = new Point(160, startY),
                Size = new Size(fieldWidth, 25),
                Font = new Font("Segoe UI", 10F)
            };
            startY += 35;
            Label lblFine = new Label { Text = "Fine Amount:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblFineValue = new Label
            {
                Text = $"${borrowing.FineAmount:F2}",
                Location = new Point(160, startY),
                Size = new Size(fieldWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = borrowing.FineAmount > 0 ? Color.FromArgb(244, 67, 54) : Color.FromArgb(76, 175, 80)
            };
            Button btnClose = new Button
            {
                Text = "Close",
                Location = new Point(350, startY + 40),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                DialogResult = DialogResult.OK
            };
            detailsForm.AcceptButton = btnClose;
            detailsForm.CancelButton = btnClose;
            detailsForm.Controls.AddRange(new Control[] {
                titleLabel, lblBorrowingId, lblBorrowingIdValue, lblMember, lblMemberValue,
                lblBook, lblBookValue, lblISBN, lblISBNValue, lblBorrowDate, lblBorrowDateValue,
                lblDueDate, lblDueDateValue, lblReturnDate, lblReturnDateValue, lblFine, lblFineValue, btnClose
            });
            detailsForm.ShowDialog();
        }
        private void ShowReservationsView()
        {
            pnlMembersView.Visible = false;
            ShowDashboardControls(false);
            ClearDynamicControls();
            Panel staffReservationsHeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 140,
                BackColor = Color.White
            };
            Label titleLabel = new Label
            {
                Text = "Reservations",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(200, 65),
                ForeColor = Color.FromArgb(40, 40, 40),
                BackColor = Color.Transparent
            };
            Label subtitleLabel = new Label
            {
                Text = "Manage book reservations and pickup notifications",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Location = new Point(30, 100),
                Size = new Size(400, 25),
                ForeColor = Color.FromArgb(100, 100, 100),
                BackColor = Color.Transparent
            };
            staffReservationsHeaderPanel.Controls.AddRange(new Control[] { titleLabel, subtitleLabel });
            Button btnNewReservation = new Button
            {
                Text = "New Reservation",
                Location = new Point(pnlMainContent.Width - 180, 20),
                Size = new Size(150, 35),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(128, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnNewReservation.FlatAppearance.BorderSize = 0;
            btnNewReservation.MouseEnter += (s, e) => btnNewReservation.BackColor = Color.FromArgb(150, 0, 0);
            btnNewReservation.MouseLeave += (s, e) => btnNewReservation.BackColor = Color.FromArgb(128, 0, 0);
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
                Text = "🔍",
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
            Button btnFilterAll = new Button
            {
                Text = "All",
                Location = new Point(searchPanel.Width - 320, 15),
                Size = new Size(70, 40),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(128, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Tag = "All"
            };
            btnFilterAll.FlatAppearance.BorderSize = 0;
            Button btnFilterPending = new Button
            {
                Text = "Pending",
                Location = new Point(searchPanel.Width - 240, 15),
                Size = new Size(80, 40),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(100, 100, 100),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Tag = "Pending"
            };
            btnFilterPending.FlatAppearance.BorderSize = 0;
            Button btnFilterReady = new Button
            {
                Text = "Ready",
                Location = new Point(searchPanel.Width - 150, 15),
                Size = new Size(80, 40),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(100, 100, 100),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Tag = "Ready"
            };
            btnFilterReady.FlatAppearance.BorderSize = 0;
            Button btnFilterFulfilled = new Button
            {
                Text = "Fulfilled",
                Location = new Point(searchPanel.Width - 60, 15),
                Size = new Size(80, 40),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(100, 100, 100),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Tag = "Fulfilled"
            };
            btnFilterFulfilled.FlatAppearance.BorderSize = 0;
            DataGridView dgvReservations = new DataGridView
            {
                Location = new Point(30, 270),
                Size = new Size(pnlMainContent.Width - 60, pnlMainContent.Height - 300),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvReservations.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(40, 40, 40),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgvReservations.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(40, 40, 40),
                Font = new Font("Segoe UI", 9F),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                SelectionBackColor = Color.FromArgb(128, 0, 0),
                SelectionForeColor = Color.White
            };
            dgvReservations.Columns.Add("ReservationId", "ID");
            dgvReservations.Columns.Add("BookTitle", "Book");
            dgvReservations.Columns.Add("MemberName", "Member");
            dgvReservations.Columns.Add("ReservedDate", "Reserved On");
            dgvReservations.Columns.Add("ExpiryDate", "Expires");
            dgvReservations.Columns.Add("Status", "Status");
            dgvReservations.Columns.Add("Notified", "Notified");
            DataGridViewButtonColumn cancelButtonColumn = new DataGridViewButtonColumn
            {
                Name = "Cancel",
                HeaderText = "",
                Text = "Cancel",
                UseColumnTextForButtonValue = true,
                Width = 70
            };
            cancelButtonColumn.DefaultCellStyle.BackColor = Color.FromArgb(244, 67, 54);
            cancelButtonColumn.DefaultCellStyle.ForeColor = Color.White;
            dgvReservations.Columns.Add(cancelButtonColumn);
            dgvReservations.Columns["ReservationId"].Width = 80;
            dgvReservations.Columns["BookTitle"].Width = 200;
            dgvReservations.Columns["MemberName"].Width = 150;
            dgvReservations.Columns["ReservedDate"].Width = 120;
            dgvReservations.Columns["ExpiryDate"].Width = 120;
            dgvReservations.Columns["Status"].Width = 100;
            dgvReservations.Columns["Notified"].Width = 80;
            dgvReservations.Columns["ReservationId"].Visible = false;
            string currentFilter = "All";
            Action<string> setActiveFilter = (filter) =>
            {
                currentFilter = filter;
                btnFilterAll.BackColor = filter == "All" ? Color.FromArgb(128, 0, 0) : Color.FromArgb(240, 240, 240);
                btnFilterAll.ForeColor = filter == "All" ? Color.White : Color.FromArgb(100, 100, 100);
                btnFilterAll.Font = new Font("Segoe UI", 10F, filter == "All" ? FontStyle.Bold : FontStyle.Regular);
                btnFilterPending.BackColor = filter == "Pending" ? Color.FromArgb(128, 0, 0) : Color.FromArgb(240, 240, 240);
                btnFilterPending.ForeColor = filter == "Pending" ? Color.White : Color.FromArgb(100, 100, 100);
                btnFilterPending.Font = new Font("Segoe UI", 10F, filter == "Pending" ? FontStyle.Bold : FontStyle.Regular);
                btnFilterReady.BackColor = filter == "Ready" ? Color.FromArgb(128, 0, 0) : Color.FromArgb(240, 240, 240);
                btnFilterReady.ForeColor = filter == "Ready" ? Color.White : Color.FromArgb(100, 100, 100);
                btnFilterReady.Font = new Font("Segoe UI", 10F, filter == "Ready" ? FontStyle.Bold : FontStyle.Regular);
                btnFilterFulfilled.BackColor = filter == "Fulfilled" ? Color.FromArgb(128, 0, 0) : Color.FromArgb(240, 240, 240);
                btnFilterFulfilled.ForeColor = filter == "Fulfilled" ? Color.White : Color.FromArgb(100, 100, 100);
                btnFilterFulfilled.Font = new Font("Segoe UI", 10F, filter == "Fulfilled" ? FontStyle.Bold : FontStyle.Regular);
            };
            void LoadReservationsData()
            {
                try
                {
                    var reservationService = new Library_Management_System.Service.ReservationService();
                    string searchText = txtSearchReservations.GetActualText();
                    string statusFilter = currentFilter;
                    var reservations = reservationService.GetReservations(searchText, statusFilter);
                    dgvReservations.Rows.Clear();
                    int pending = 0;
                    int ready = 0;
                    int fulfilled = 0;
                    int expired = 0;
                    foreach (var reservation in reservations)
                    {
                        if (reservation.Status == "Pending")
                            pending++;
                        else if (reservation.Status == "Ready")
                            ready++;
                        else if (reservation.Status == "Fulfilled")
                            fulfilled++;
                        else if (reservation.Status == "Expired" || reservation.Status == "Cancelled")
                            expired++;
                        int rowIndex = dgvReservations.Rows.Add();
                        dgvReservations.Rows[rowIndex].Cells["ReservationId"].Value = reservation.ReservationId;
                        dgvReservations.Rows[rowIndex].Cells["BookTitle"].Value = reservation.BookTitle;
                        dgvReservations.Rows[rowIndex].Cells["MemberName"].Value = reservation.MemberName;
                        dgvReservations.Rows[rowIndex].Cells["ReservedDate"].Value = reservation.ReservedDate.ToString("MMM dd, yyyy");
                        var expiryCell = dgvReservations.Rows[rowIndex].Cells["ExpiryDate"];
                        expiryCell.Value = reservation.ExpiryDate.ToString("MMM dd, yyyy");
                        if (DateTime.Now > reservation.ExpiryDate && reservation.Status != "Fulfilled" && reservation.Status != "Cancelled")
                        {
                            expiryCell.Style.ForeColor = Color.FromArgb(244, 67, 54); 
                        }
                        var statusCell = dgvReservations.Rows[rowIndex].Cells["Status"];
                        statusCell.Value = reservation.Status;
                        if (reservation.Status == "Pending")
                        {
                            statusCell.Style.ForeColor = Color.FromArgb(255, 152, 0); 
                        }
                        else if (reservation.Status == "Ready")
                        {
                            statusCell.Style.ForeColor = Color.FromArgb(76, 175, 80); 
                        }
                        else if (reservation.Status == "Fulfilled")
                        {
                            statusCell.Style.ForeColor = Color.FromArgb(33, 150, 243); 
                        }
                        else if (reservation.Status == "Cancelled" || reservation.Status == "Expired")
                        {
                            statusCell.Style.ForeColor = Color.FromArgb(158, 158, 158); 
                        }
                        dgvReservations.Rows[rowIndex].Cells["Notified"].Value = reservation.IsNotified ? "🔔" : "○";
                        if (reservation.Status == "Fulfilled" || reservation.Status == "Cancelled" || reservation.Status == "Expired")
                        {
                            dgvReservations.Rows[rowIndex].Cells["Cancel"].Value = "";
                            dgvReservations.Rows[rowIndex].Cells["Cancel"].Style.BackColor = Color.Transparent;
                        }
                        else
                        {
                            dgvReservations.Rows[rowIndex].Cells["Cancel"].Value = "Cancel";
                        }
                    }
                    var allReservations = reservationService.GetReservations("", "All");
                    pending = 0;
                    ready = 0;
                    fulfilled = 0;
                    expired = 0;
                    foreach (var res in allReservations)
                    {
                        if (res.Status == "Pending") pending++;
                        else if (res.Status == "Ready") ready++;
                        else if (res.Status == "Fulfilled") fulfilled++;
                        else if (res.Status == "Expired" || res.Status == "Cancelled") expired++;
                    }
                    UpdateCatalogStatCard(cardPending, pending.ToString());
                    UpdateCatalogStatCard(cardReady, ready.ToString());
                    UpdateCatalogStatCard(cardFulfilled, fulfilled.ToString());
                    UpdateCatalogStatCard(cardExpired, expired.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading reservations: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            btnFilterAll.Click += (s, e) => { setActiveFilter("All"); LoadReservationsData(); };
            btnFilterPending.Click += (s, e) => { setActiveFilter("Pending"); LoadReservationsData(); };
            btnFilterReady.Click += (s, e) => { setActiveFilter("Ready"); LoadReservationsData(); };
            btnFilterFulfilled.Click += (s, e) => { setActiveFilter("Fulfilled"); LoadReservationsData(); };
            txtSearchReservations.TextChanged += (s, e) =>
            {
                if (txtSearchReservations.ForeColor != Color.Gray)
                {
                    LoadReservationsData();
                }
            };
            btnNewReservation.Click += (s, e) => ShowNewReservationDialog();
            dgvReservations.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string columnName = dgvReservations.Columns[e.ColumnIndex].Name;
                string reservationId = dgvReservations.Rows[e.RowIndex].Cells["ReservationId"].Value.ToString();
                if (columnName == "Cancel")
                {
                    var reservation = new Library_Management_System.Service.ReservationService().GetReservationById(reservationId);
                    if (reservation != null)
                    {
                        if (MessageBox.Show($"Cancel reservation for '{reservation.BookTitle}' by {reservation.MemberName}?",
                            "Confirm Cancel", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            try
                            {
                                var reservationService = new Library_Management_System.Service.ReservationService();
                                reservationService.CancelReservation(reservationId);
                                LoadReservationsData();
                                MessageBox.Show("Reservation cancelled successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error cancelling reservation: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            };
            searchPanel.Controls.AddRange(new Control[] { searchContainer, btnFilterAll, btnFilterPending, btnFilterReady, btnFilterFulfilled });
            pnlMainContent.Controls.AddRange(new Control[] { staffReservationsHeaderPanel, btnNewReservation, statsPanel, searchPanel, dgvReservations });
            LoadReservationsData();
        }
        private void ShowNewReservationDialog()
        {
            Form newReservationForm = new Form
            {
                Text = "New Reservation",
                Size = new Size(500, 350),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false
            };
            Label titleLabel = new Label
            {
                Text = "New Reservation",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(400, 35),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            int startY = 70;
            Label lblMemberId = new Label { Text = "Member ID:", Location = new Point(30, startY), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtMemberId = new TextBox { Location = new Point(140, startY), Size = new Size(300, 30), Font = new Font("Segoe UI", 10F) };
            startY += 50;
            Label lblBookId = new Label { Text = "Book ID:", Location = new Point(30, startY), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtBookId = new TextBox { Location = new Point(140, startY), Size = new Size(300, 30), Font = new Font("Segoe UI", 10F) };
            startY += 50;
            Label lblReservationDays = new Label { Text = "Reservation Period (days):", Location = new Point(30, startY), Size = new Size(150, 25), Font = new Font("Segoe UI", 10F) };
            NumericUpDown numReservationDays = new NumericUpDown
            {
                Location = new Point(190, startY),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 10F),
                Minimum = 1,
                Maximum = 30,
                Value = 7
            };
            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(250, startY + 60),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                DialogResult = DialogResult.Cancel
            };
            Button btnCreate = new Button
            {
                Text = "Create Reservation",
                Location = new Point(340, startY + 60),
                Size = new Size(130, 35),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(128, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCreate.FlatAppearance.BorderSize = 0;
            txtMemberId.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtBookId.Focus(); };
            txtBookId.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) numReservationDays.Focus(); };
            numReservationDays.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnCreate.PerformClick(); };
            btnCreate.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtMemberId.GetActualText()))
                {
                    MessageBox.Show("Please enter a member ID.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMemberId.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtBookId.GetActualText()))
                {
                    MessageBox.Show("Please enter a book ID.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtBookId.Focus();
                    return;
                }
                try
                {
                    var reservationService = new Library_Management_System.Service.ReservationService();
                    reservationService.CreateReservation(
                        txtMemberId.GetActualText().Trim(),
                        txtBookId.GetActualText().Trim(),
                        (int)numReservationDays.Value
                    );
                    MessageBox.Show("Reservation created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    newReservationForm.DialogResult = DialogResult.OK;
                    newReservationForm.Close();
                    ShowReservationsView();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating reservation: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            newReservationForm.AcceptButton = btnCreate;
            newReservationForm.CancelButton = btnCancel;
            btnCancel.Click += (s, e) => newReservationForm.Close();
            txtMemberId.SetPlaceholder("Enter member ID");
            txtBookId.SetPlaceholder("Enter book ID");
            newReservationForm.Controls.AddRange(new Control[] {
                titleLabel, lblMemberId, txtMemberId, lblBookId, txtBookId,
                lblReservationDays, numReservationDays, btnCancel, btnCreate
            });
            newReservationForm.ShowDialog();
        }
        private void ShowFinesView()
        {
            pnlMembersView.Visible = false;
            ShowDashboardControls(false);
            ClearDynamicControls();
            Panel staffFinesHeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 140,
                BackColor = Color.White
            };
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
            staffFinesHeaderPanel.Controls.AddRange(new Control[] { titleLabel, subtitleLabel });
            Button btnAddFine = new Button
            {
                Text = "Add Fine",
                Location = new Point(pnlMainContent.Width - 150, 20),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(128, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnAddFine.FlatAppearance.BorderSize = 0;
            btnAddFine.MouseEnter += (s, e) => btnAddFine.BackColor = Color.FromArgb(150, 0, 0);
            btnAddFine.MouseLeave += (s, e) => btnAddFine.BackColor = Color.FromArgb(128, 0, 0);
            Panel statsPanel = new Panel
            {
                Location = new Point(30, 150),
                Size = new Size(pnlMainContent.Width - 60, 80),
                BackColor = Color.Transparent,
                Tag = "FinesStatsPanel"
            };
            Panel cardPendingFines = CreateCatalogStatCard("⚠️", "₱0.00", "Pending Fines", Color.FromArgb(128, 0, 0), new Point(0, 0));
            Panel cardCollected = CreateCatalogStatCard("✓", "₱0.00", "Collected", Color.FromArgb(76, 175, 80), new Point(200, 0));
            Panel cardWaived = CreateCatalogStatCard("✗", "₱0.00", "Waived", Color.FromArgb(33, 150, 243), new Point(400, 0));
            Panel cardPendingCases = CreateCatalogStatCard("₱", "0", "Pending Cases", Color.FromArgb(255, 193, 7), new Point(600, 0));
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
                Text = "🔍",
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
            Button btnFilterAll = new Button
            {
                Text = "All",
                Location = new Point(searchPanel.Width - 340, 15),
                Size = new Size(80, 40),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(128, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Tag = "All"
            };
            btnFilterAll.FlatAppearance.BorderSize = 0;
            Button btnFilterPending = new Button
            {
                Text = "Pending",
                Location = new Point(searchPanel.Width - 250, 15),
                Size = new Size(80, 40),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(100, 100, 100),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Tag = "Pending"
            };
            btnFilterPending.FlatAppearance.BorderSize = 0;
            Button btnFilterPaid = new Button
            {
                Text = "Paid",
                Location = new Point(searchPanel.Width - 160, 15),
                Size = new Size(80, 40),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(100, 100, 100),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Tag = "Paid"
            };
            btnFilterPaid.FlatAppearance.BorderSize = 0;
            Button btnFilterWaived = new Button
            {
                Text = "Waived",
                Location = new Point(searchPanel.Width - 70, 15),
                Size = new Size(80, 40),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(100, 100, 100),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Tag = "Waived"
            };
            btnFilterWaived.FlatAppearance.BorderSize = 0;
            string currentFilter = "All";
            Action<string> setActiveFilter = (filter) =>
            {
                currentFilter = filter;
                btnFilterAll.BackColor = filter == "All" ? Color.FromArgb(128, 0, 0) : Color.FromArgb(240, 240, 240);
                btnFilterAll.ForeColor = filter == "All" ? Color.White : Color.FromArgb(100, 100, 100);
                btnFilterAll.Font = new Font("Segoe UI", 10F, filter == "All" ? FontStyle.Bold : FontStyle.Regular);
                btnFilterPending.BackColor = filter == "Pending" ? Color.FromArgb(128, 0, 0) : Color.FromArgb(240, 240, 240);
                btnFilterPending.ForeColor = filter == "Pending" ? Color.White : Color.FromArgb(100, 100, 100);
                btnFilterPending.Font = new Font("Segoe UI", 10F, filter == "Pending" ? FontStyle.Bold : FontStyle.Regular);
                btnFilterPaid.BackColor = filter == "Paid" ? Color.FromArgb(128, 0, 0) : Color.FromArgb(240, 240, 240);
                btnFilterPaid.ForeColor = filter == "Paid" ? Color.White : Color.FromArgb(100, 100, 100);
                btnFilterPaid.Font = new Font("Segoe UI", 10F, filter == "Paid" ? FontStyle.Bold : FontStyle.Regular);
                btnFilterWaived.BackColor = filter == "Waived" ? Color.FromArgb(128, 0, 0) : Color.FromArgb(240, 240, 240);
                btnFilterWaived.ForeColor = filter == "Waived" ? Color.White : Color.FromArgb(100, 100, 100);
                btnFilterWaived.Font = new Font("Segoe UI", 10F, filter == "Waived" ? FontStyle.Bold : FontStyle.Regular);
            };
            DataGridView dgvFines = new DataGridView
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
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(230, 230, 230),
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            };
            dgvFines.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 249, 250),
                ForeColor = Color.FromArgb(33, 37, 41),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                SelectionBackColor = Color.FromArgb(248, 249, 250)
            };
            dgvFines.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(33, 37, 41),
                Font = new Font("Segoe UI", 9F),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0),
                SelectionBackColor = Color.FromArgb(33, 150, 243, 20),
                SelectionForeColor = Color.FromArgb(33, 37, 41)
            };
            dgvFines.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(252, 252, 252),
                ForeColor = Color.FromArgb(33, 37, 41),
                Font = new Font("Segoe UI", 9F),
                SelectionBackColor = Color.FromArgb(33, 150, 243, 20),
                SelectionForeColor = Color.FromArgb(33, 37, 41)
            };
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
            dgvFines.Columns["BookReason"].Width = 250;
            dgvFines.Columns["Type"].Width = 100;
            dgvFines.Columns["Amount"].Width = 100;
            dgvFines.Columns["Paid"].Width = 100;
            dgvFines.Columns["Status"].Width = 100;
            dgvFines.Columns["Date"].Width = 120;
            DataGridViewButtonColumn payButtonColumn = new DataGridViewButtonColumn
            {
                Name = "Pay",
                HeaderText = "",
                Text = "Pay",
                UseColumnTextForButtonValue = true,
                Width = 70
            };
            payButtonColumn.DefaultCellStyle.BackColor = Color.FromArgb(128, 0, 0);
            payButtonColumn.DefaultCellStyle.ForeColor = Color.White;
            dgvFines.Columns.Add(payButtonColumn);
            DataGridViewButtonColumn waiveButtonColumn = new DataGridViewButtonColumn
            {
                Name = "Waive",
                HeaderText = "",
                Text = "Waive",
                UseColumnTextForButtonValue = true,
                Width = 70
            };
            waiveButtonColumn.DefaultCellStyle.BackColor = Color.White;
            waiveButtonColumn.DefaultCellStyle.ForeColor = Color.FromArgb(128, 0, 0);
            waiveButtonColumn.DefaultCellStyle.SelectionBackColor = Color.White;
            dgvFines.Columns.Add(waiveButtonColumn);
            void LoadFinesData()
            {
                try
                {
                    var finesService = new Library_Management_System.Service.FinesService();
                    string searchText = txtSearchFines.GetActualText();
                    string statusFilter = currentFilter == "All" ? "All Status" : (currentFilter == "Pending" ? "Unpaid" : currentFilter);
                    var fines = finesService.GetFines(searchText, statusFilter);
                    var allFines = finesService.GetFines("", "All Status");
                    decimal pendingFines = allFines.Where(f => f.Status == "Unpaid").Sum(f => f.Amount);
                    decimal collectedFines = allFines.Where(f => f.Status == "Paid").Sum(f => f.Amount);
                    decimal waivedFines = allFines.Where(f => f.Status == "Waived").Sum(f => f.Amount);
                    int pendingCases = allFines.Count(f => f.Status == "Unpaid");
                    UpdateCatalogStatCard(cardPendingFines, $"₱{pendingFines:F2}");
                    UpdateCatalogStatCard(cardCollected, $"₱{collectedFines:F2}");
                    UpdateCatalogStatCard(cardWaived, $"₱{waivedFines:F2}");
                    UpdateCatalogStatCard(cardPendingCases, pendingCases.ToString());
                    dgvFines.Rows.Clear();
                    foreach (var fine in fines)
                    {
                        int rowIndex = dgvFines.Rows.Add();
                        dgvFines.Rows[rowIndex].Cells["FineId"].Value = fine.FineId;
                        dgvFines.Rows[rowIndex].Cells["MemberName"].Value = fine.MemberName;
                        string bookReason = "";
                        if (!string.IsNullOrEmpty(fine.BookTitle))
                        {
                            bookReason = fine.BookTitle;
                        }
                        if (!string.IsNullOrEmpty(fine.Reason))
                        {
                            if (fine.Reason == "Overdue return")
                            {
                                if (string.IsNullOrEmpty(bookReason))
                                {
                                    bookReason = fine.Reason;
                                }
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(bookReason))
                                {
                                    bookReason = fine.Reason;
                                }
                                else
                                {
                                    if (!fine.Reason.Contains(fine.BookTitle))
                                    {
                                        bookReason += "\n" + fine.Reason;
                                    }
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(bookReason))
                        {
                            bookReason = "N/A";
                        }
                        dgvFines.Rows[rowIndex].Cells["BookReason"].Value = bookReason;
                        var typeCell = dgvFines.Rows[rowIndex].Cells["Type"];
                        string reasonUpper = fine.Reason?.ToUpper() ?? "";
                        if (reasonUpper.Contains("LOST") || reasonUpper.StartsWith("LOST"))
                        {
                            typeCell.Value = "Lost";
                            typeCell.Style.ForeColor = Color.FromArgb(244, 67, 54); 
                        }
                        else if (reasonUpper.Contains("DAMAGED") || reasonUpper.StartsWith("DAMAGED"))
                        {
                            typeCell.Value = "Damaged";
                            typeCell.Style.ForeColor = Color.FromArgb(255, 87, 34); 
                        }
                        else if (reasonUpper.Contains("OVERDUE") || reasonUpper.StartsWith("OVERDUE"))
                        {
                            typeCell.Value = "Overdue";
                            typeCell.Style.ForeColor = Color.FromArgb(255, 193, 7); 
                        }
                        else
                        {
                            string type = "Other";
                            if (fine.Reason != null)
                            {
                                int dashIndex = fine.Reason.IndexOf(" - ");
                                if (dashIndex > 0)
                                {
                                    type = fine.Reason.Substring(0, dashIndex).Trim();
                                }
                                else if (fine.Reason.Length > 0)
                                {
                                    string[] parts = fine.Reason.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Length > 0 && (parts[0].Equals("Other", StringComparison.OrdinalIgnoreCase) ||
                                        parts[0].Equals("Lost", StringComparison.OrdinalIgnoreCase) ||
                                        parts[0].Equals("Damaged", StringComparison.OrdinalIgnoreCase) ||
                                        parts[0].Equals("Overdue", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        type = parts[0];
                                    }
                                }
                            }
                            typeCell.Value = type;
                            typeCell.Style.ForeColor = Color.FromArgb(96, 125, 139); 
                        }
                        dgvFines.Rows[rowIndex].Cells["Amount"].Value = $"₱{fine.Amount:F2}";
                        var paidCell = dgvFines.Rows[rowIndex].Cells["Paid"];
                        if (fine.Status == "Paid")
                        {
                            paidCell.Value = $"₱{fine.Amount:F2}";
                            paidCell.Style.ForeColor = Color.FromArgb(76, 175, 80); 
                        }
                        else
                        {
                            paidCell.Value = "₱0.00";
                            paidCell.Style.ForeColor = Color.FromArgb(76, 175, 80); 
                        }
                        var statusCell = dgvFines.Rows[rowIndex].Cells["Status"];
                        switch (fine.Status)
                        {
                            case "Unpaid":
                                statusCell.Value = "Pending";
                                statusCell.Style.ForeColor = Color.FromArgb(255, 152, 0); 
                                break;
                            case "Paid":
                                statusCell.Value = "Paid";
                                statusCell.Style.ForeColor = Color.FromArgb(76, 175, 80); 
                                break;
                            case "Waived":
                                statusCell.Value = "Waived";
                                statusCell.Style.ForeColor = Color.FromArgb(158, 158, 158); 
                                break;
                            default:
                                statusCell.Value = fine.Status;
                                break;
                        }
                        dgvFines.Rows[rowIndex].Cells["Date"].Value = fine.CreatedDate.ToString("MMM dd, yyyy");
                        if (fine.Status == "Unpaid")
                        {
                            dgvFines.Rows[rowIndex].Cells["Pay"].Value = "Pay";
                            dgvFines.Rows[rowIndex].Cells["Waive"].Value = "Waive";
                        }
                        else if (fine.Status == "Paid" && fine.PaidDate.HasValue)
                        {
                            dgvFines.Rows[rowIndex].Cells["Pay"].Value = $"Paid {fine.PaidDate.Value:MMM dd}";
                            dgvFines.Rows[rowIndex].Cells["Pay"].Style.BackColor = Color.Transparent;
                            dgvFines.Rows[rowIndex].Cells["Pay"].Style.ForeColor = Color.FromArgb(76, 175, 80);
                            dgvFines.Rows[rowIndex].Cells["Waive"].Value = "";
                        }
                        else
                        {
                            dgvFines.Rows[rowIndex].Cells["Pay"].Value = "";
                            dgvFines.Rows[rowIndex].Cells["Waive"].Value = "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading fines: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            txtSearchFines.TextChanged += (s, e) =>
            {
                if (txtSearchFines.ForeColor != Color.Gray)
                {
                    LoadFinesData();
                }
            };
            btnFilterAll.Click += (s, e) => { setActiveFilter("All"); LoadFinesData(); };
            btnFilterPending.Click += (s, e) => { setActiveFilter("Pending"); LoadFinesData(); };
            btnFilterPaid.Click += (s, e) => { setActiveFilter("Paid"); LoadFinesData(); };
            btnFilterWaived.Click += (s, e) => { setActiveFilter("Waived"); LoadFinesData(); };
            btnAddFine.Click += (s, e) => ShowAddFineDialog();
            setActiveFilter("All");
            dgvFines.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string columnName = dgvFines.Columns[e.ColumnIndex].Name;
                string fineId = dgvFines.Rows[e.RowIndex].Cells["FineId"].Value.ToString();
                if (columnName == "Pay")
                {
                    var fine = new Library_Management_System.Service.FinesService().GetFineById(fineId);
                    if (fine != null && fine.Status == "Unpaid")
                        {
                        if (MessageBox.Show($"Process payment of ₱{fine.Amount:F2} for {fine.MemberName}?",
                                "Confirm Payment", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            {
                                try
                                {
                                    var finesService = new Library_Management_System.Service.FinesService();
                                    finesService.ProcessFinePayment(fineId);
                                    LoadFinesData();
                                    MessageBox.Show("Payment processed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Error processing payment: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                        }
                }
                else if (columnName == "Waive")
                {
                    var fine = new Library_Management_System.Service.FinesService().GetFineById(fineId);
                    if (fine != null && fine.Status == "Unpaid")
                    {
                        ShowWaiveFineDialog(fineId, fine.MemberName, fine.Amount);
                    }
                }
            };
            searchPanel.Controls.AddRange(new Control[] { searchContainer, btnFilterAll, btnFilterPending, btnFilterPaid, btnFilterWaived });
            pnlMainContent.Controls.AddRange(new Control[] { staffFinesHeaderPanel, btnAddFine, statsPanel, searchPanel, dgvFines });
            LoadFinesData();
        }
        private void ShowAddFineDialog()
        {
            Form addFineForm = new Form
            {
                Text = "Add New Fine",
                Size = new Size(600, 700),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false,
                BackColor = Color.FromArgb(245, 245, 245)
            };
            Panel staffAddFineHeaderPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(addFineForm.ClientSize.Width, 80),
                BackColor = Color.White,
                Dock = DockStyle.Top
            };
            Label titleLabel = new Label
            {
                Text = "Add New Fine",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(300, 65),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            Label subtitleLabel = new Label
            {
                Text = "Create a new fine for a member.",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(30, 100),
                Size = new Size(400, 20),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
            Button btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Location = new Point(staffAddFineHeaderPanel.Width - 40, 10),
                Size = new Size(30, 30),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(100, 100, 100),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => addFineForm.Close();
            staffAddFineHeaderPanel.Controls.AddRange(new Control[] { titleLabel, subtitleLabel, btnClose });
            Panel contentPanel = new Panel
            {
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                Padding = new Padding(35, 25, 35, 25)
            };
            int startY = 0;
            int fieldSpacing = 60; 
            int fieldHeight = 35;
            int labelHeight = 20;
            int fieldWidth = 510; 
            Label lblMember = new Label 
            { 
                Text = "Select Member *", 
                Location = new Point(0, startY), 
                Size = new Size(200, labelHeight), 
                Font = new Font("Segoe UI", 10F), 
                ForeColor = Color.FromArgb(40, 40, 40) 
            };
            ComboBox cmbMember = new ComboBox
            {
                Location = new Point(0, startY + labelHeight + 5),
                Size = new Size(fieldWidth, fieldHeight),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White,
                ForeColor = Color.Gray
            };
            cmbMember.Items.Add("Choose a member...");
            cmbMember.SelectedIndex = 0;
            cmbMember.SelectedIndexChanged += (s, e) =>
            {
                if (cmbMember.SelectedIndex > 0)
                {
                    cmbMember.ForeColor = Color.Black;
                }
                else
                {
                    cmbMember.ForeColor = Color.Gray;
                }
            };
            try
            {
                var membersService = new Library_Management_System.Service.MembersService();
                var members = membersService.GetMembers("", "All Status", "All Types");
                foreach (var member in members)
                {
                    cmbMember.Items.Add($"{member.Name} ({member.MemberId})");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading members: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            startY += fieldSpacing + labelHeight + 5;
            Label lblFineType = new Label 
            { 
                Text = "Fine Type *", 
                Location = new Point(0, startY), 
                Size = new Size(200, labelHeight), 
                Font = new Font("Segoe UI", 10F), 
                ForeColor = Color.FromArgb(40, 40, 40) 
            };
            ComboBox cmbFineType = new ComboBox
            {
                Location = new Point(0, startY + labelHeight + 5),
                Size = new Size(fieldWidth, fieldHeight),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White
            };
            cmbFineType.Items.AddRange(new[] { "Overdue", "Lost Book", "Damaged Book", "Other" });
            cmbFineType.SelectedIndex = 3; 
            startY += fieldSpacing + labelHeight + 5;
            Label lblAmount = new Label 
            { 
                Text = "Amount *", 
                Location = new Point(0, startY), 
                Size = new Size(200, labelHeight), 
                Font = new Font("Segoe UI", 10F), 
                ForeColor = Color.FromArgb(40, 40, 40) 
            };
            Panel amountPanel = new Panel
            {
                Location = new Point(0, startY + labelHeight + 5),
                Size = new Size(fieldWidth, fieldHeight),
                BackColor = Color.White
            };
            Label lblCurrency = new Label
            {
                Text = "₱",
                Location = new Point(10, 8),
                Size = new Size(20, 20),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            NumericUpDown numAmount = new NumericUpDown
            {
                Location = new Point(30, 0),
                Size = new Size(fieldWidth - 30, fieldHeight),
                Font = new Font("Segoe UI", 10F),
                Minimum = 0,
                Maximum = 999999.99m,
                DecimalPlaces = 2,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            amountPanel.Controls.AddRange(new Control[] { lblCurrency, numAmount });
            amountPanel.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, amountPanel.ClientRectangle, Color.FromArgb(200, 200, 200), ButtonBorderStyle.Solid);
            };
            startY += fieldSpacing + labelHeight + 5;
            Label lblBookTitle = new Label 
            { 
                Text = "Book Title (optional)", 
                Location = new Point(0, startY), 
                Size = new Size(200, labelHeight), 
                Font = new Font("Segoe UI", 10F), 
                ForeColor = Color.FromArgb(40, 40, 40) 
            };
            TextBox txtBookTitle = new TextBox
            {
                Location = new Point(0, startY + labelHeight + 5),
                Size = new Size(fieldWidth, fieldHeight),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.White
            };
            txtBookTitle.SetPlaceholder("Related book title...");
            startY += fieldSpacing + labelHeight + 5;
            Label lblNotes = new Label
            {
                Text = "Notes",
                Location = new Point(0, startY),
                Size = new Size(200, labelHeight),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            TextBox txtNotes = new TextBox
            {
                Location = new Point(0, startY + labelHeight + 5),
                Size = new Size(fieldWidth, 100),
                Font = new Font("Segoe UI", 10F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White
            };
            txtNotes.SetPlaceholder("Additional notes...");
            startY += labelHeight + 5 + 100 + 20; 
            contentPanel.Controls.AddRange(new Control[] {
                lblMember, cmbMember, lblFineType, cmbFineType, lblAmount, amountPanel,
                lblBookTitle, txtBookTitle, lblNotes, txtNotes
            });
            Panel buttonPanel = new Panel
            {
                Height = 80,
                BackColor = Color.White,
                Dock = DockStyle.Bottom
            };
            Button btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 40),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.FromArgb(40, 40, 40),
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.None,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => addFineForm.Close();
            Button btnAddFine = new Button
            {
                Text = "Add Fine",
                Size = new Size(110, 40),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(128, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.None,
                Cursor = Cursors.Hand
            };
            btnAddFine.FlatAppearance.BorderSize = 0;
            btnAddFine.MouseEnter += (s, e) => btnAddFine.BackColor = Color.FromArgb(150, 0, 0);
            btnAddFine.MouseLeave += (s, e) => btnAddFine.BackColor = Color.FromArgb(128, 0, 0);
            buttonPanel.Controls.AddRange(new Control[] { btnCancel, btnAddFine });
            void PositionButtons()
            {
                if (buttonPanel.Width > 0)
                {
                    btnCancel.Location = new Point(buttonPanel.Width - 230, 20);
                    btnAddFine.Location = new Point(buttonPanel.Width - 120, 20);
                }
            }
            buttonPanel.Layout += (s, e) => PositionButtons();
            addFineForm.Shown += (s, e) => PositionButtons();
            btnAddFine.Click += (s, args) =>
            {
                if (cmbMember.SelectedIndex <= 0)
                {
                    MessageBox.Show("Please select a member.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cmbMember.Focus();
                    return;
                }
                if (numAmount.Value <= 0)
                {
                    MessageBox.Show("Please enter a valid amount greater than zero.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    numAmount.Focus();
                    return;
                }
                try
                {
                    string selectedMember = cmbMember.SelectedItem.ToString();
                    string memberId = selectedMember.Substring(selectedMember.LastIndexOf('(') + 1).TrimEnd(')');
                    string fineType = cmbFineType.SelectedItem.ToString();
                    decimal amount = numAmount.Value;
                    string bookTitle = txtBookTitle.GetActualText().Trim();
                    string notes = txtNotes.GetActualText().Trim();
                    var finesService = new Library_Management_System.Service.FinesService();
                    bool success = finesService.AddFine(memberId, amount, fineType, 
                        string.IsNullOrWhiteSpace(bookTitle) ? null : bookTitle,
                        string.IsNullOrWhiteSpace(notes) ? null : notes);
                    if (success)
                    {
                        MessageBox.Show("Fine added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        addFineForm.DialogResult = DialogResult.OK;
                        addFineForm.Close();
                        if (pnlMainContent.Controls.Count > 0)
                        {
                            var finesViewControls = pnlMainContent.Controls.Cast<Control>()
                                .Where(c => c.Tag?.ToString() == "FinesStatsPanel" || c is DataGridView)
                                .ToList();
                            if (finesViewControls.Count > 0)
                            {
                                ShowFinesView();
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Failed to add fine. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding fine: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            addFineForm.Controls.AddRange(new Control[] { staffAddFineHeaderPanel, contentPanel, buttonPanel });
            addFineForm.ShowDialog();
        }
        private void ShowWaiveFineDialog(string fineId, string memberName, decimal amount)
        {
            Form waiveForm = new Form
            {
                Text = "Waive Fine",
                Size = new Size(450, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            Label titleLabel = new Label
            {
                Text = $"Waive Fine for {memberName}",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(400, 65),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            Label amountLabel = new Label
            {
                Text = $"Amount: ₱{amount:F2}",
                Font = new Font("Segoe UI", 12F),
                Location = new Point(20, 100),
                Size = new Size(400, 25)
            };
            Label reasonLabel = new Label
            {
                Text = "Reason for waiving:",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(20, 140),
                Size = new Size(200, 25)
            };
            TextBox txtReason = new TextBox
            {
                Location = new Point(20, 165),
                Size = new Size(390, 30),
                Font = new Font("Segoe UI", 10F),
                Multiline = true,
                Height = 60
            };
            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(260, 235),
                Size = new Size(70, 30),
                DialogResult = DialogResult.Cancel
            };
            Button btnWaive = new Button
            {
                Text = "Waive Fine",
                Location = new Point(340, 235),
                Size = new Size(70, 30),
                BackColor = Color.FromArgb(128, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnWaive.FlatAppearance.BorderSize = 0;
            bool waived = false;
            btnWaive.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtReason.Text))
                {
                    MessageBox.Show("Please provide a reason for waiving the fine.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                try
                {
                    var finesService = new Library_Management_System.Service.FinesService();
                    finesService.WaiveFine(fineId, txtReason.Text);
                    waived = true;
                    MessageBox.Show("Fine waived successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    waiveForm.DialogResult = DialogResult.OK;
                    waiveForm.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error waiving fine: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            waiveForm.Controls.AddRange(new Control[] { titleLabel, amountLabel, reasonLabel, txtReason, btnCancel, btnWaive });
            waiveForm.CancelButton = btnCancel;
            waiveForm.ShowDialog();
            if (waived)
            {
                ShowFinesView();
            }
        }
        private void ShowPaymentDialog()
        {
            Form paymentForm = new Form
            {
                Text = "Process Fine Payment",
                Size = new Size(400, 280),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false
            };
            Label titleLabel = new Label
            {
                Text = "Process Payment",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(300, 65),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            Label lblFineId = new Label { Text = "Fine ID:", Location = new Point(30, 100), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtFineId = new TextBox { Location = new Point(140, 95), Size = new Size(200, 30), Font = new Font("Segoe UI", 10F) };
            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(150, 180),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                DialogResult = DialogResult.Cancel
            };
            Button btnProcess = new Button
            {
                Text = "Process",
                Location = new Point(240, 180),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnProcess.FlatAppearance.BorderSize = 0;
            txtFineId.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnProcess.PerformClick(); };
            btnProcess.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtFineId.GetActualText()))
                {
                    MessageBox.Show("Please enter a fine ID.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtFineId.Focus();
                    return;
                }
                try
                {
                    var finesService = new Library_Management_System.Service.FinesService();
                    if (finesService.ProcessFinePayment(txtFineId.GetActualText().Trim()))
                    {
                        MessageBox.Show("Payment processed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        paymentForm.DialogResult = DialogResult.OK;
                        paymentForm.Close();
                    }
                    else
                    {
                        MessageBox.Show("Fine not found or already paid.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error processing payment: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            paymentForm.AcceptButton = btnProcess;
            paymentForm.CancelButton = btnCancel;
            btnCancel.Click += (s, e) => paymentForm.Close();
            txtFineId.SetPlaceholder("Enter fine ID");
            paymentForm.Controls.AddRange(new Control[] {
                titleLabel, lblFineId, txtFineId, btnCancel, btnProcess
            });
            paymentForm.ShowDialog();
        }
        private void ShowWaiveDialog()
        {
            Form waiveForm = new Form
            {
                Text = "Waive Fine",
                Size = new Size(450, 330),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false
            };
            Label titleLabel = new Label
            {
                Text = "Waive Fine",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(300, 65),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            Label lblFineId = new Label { Text = "Fine ID:", Location = new Point(30, 100), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtFineId = new TextBox { Location = new Point(140, 95), Size = new Size(250, 30), Font = new Font("Segoe UI", 10F) };
            Label lblReason = new Label { Text = "Reason:", Location = new Point(30, 140), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtReason = new TextBox
            {
                Location = new Point(140, 135),
                Size = new Size(250, 60),
                Font = new Font("Segoe UI", 10F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(250, 230),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                DialogResult = DialogResult.Cancel
            };
            Button btnWaive = new Button
            {
                Text = "Waive",
                Location = new Point(340, 230),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnWaive.FlatAppearance.BorderSize = 0;
            txtFineId.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtReason.Focus(); };
            btnWaive.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtFineId.GetActualText()))
                {
                    MessageBox.Show("Please enter a fine ID.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtFineId.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtReason.GetActualText()))
                {
                    MessageBox.Show("Please enter a reason for waiving the fine.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtReason.Focus();
                    return;
                }
                try
                {
                    var finesService = new Library_Management_System.Service.FinesService();
                    if (finesService.WaiveFine(txtFineId.GetActualText().Trim(), txtReason.GetActualText().Trim()))
                    {
                        MessageBox.Show("Fine waived successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        waiveForm.DialogResult = DialogResult.OK;
                        waiveForm.Close();
                    }
                    else
                    {
                        MessageBox.Show("Fine not found or already processed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error waiving fine: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            waiveForm.AcceptButton = btnWaive;
            waiveForm.CancelButton = btnCancel;
            btnCancel.Click += (s, e) => waiveForm.Close();
            txtFineId.SetPlaceholder("Enter fine ID");
            txtReason.SetPlaceholder("Enter reason for fine");
            waiveForm.Controls.AddRange(new Control[] {
                titleLabel, lblFineId, txtFineId, lblReason, txtReason, btnCancel, btnWaive
            });
            waiveForm.ShowDialog();
        }
        private void ShowFineDetailsDialog(Library_Management_System.Service.FinesService.FineInfo fine)
        {
            Form detailsForm = new Form
            {
                Text = "Fine Details",
                Size = new Size(500, 430),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false
            };
            Label titleLabel = new Label
            {
                Text = "Fine Details",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(400, 65),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            int startY = 100;
            int labelWidth = 120;
            int fieldWidth = 300;
            Label lblFineId = new Label { Text = "Fine ID:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblFineIdValue = new Label { Text = fine.FineId, Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            startY += 35;
            Label lblMember = new Label { Text = "Member:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblMemberValue = new Label { Text = fine.MemberName, Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F) };
            startY += 35;
            Label lblBook = new Label { Text = "Book:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblBookValue = new Label { Text = fine.BookTitle, Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F) };
            startY += 35;
            Label lblAmount = new Label { Text = "Amount:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblAmountValue = new Label { Text = $"${fine.Amount:F2}", Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(244, 67, 54) };
            startY += 35;
            Label lblReason = new Label { Text = "Reason:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblReasonValue = new Label { Text = fine.Reason ?? "N/A", Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F) };
            startY += 35;
            Label lblStatus = new Label { Text = "Status:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblStatusValue = new Label
            {
                Text = fine.Status,
                Location = new Point(160, startY),
                Size = new Size(fieldWidth, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = fine.Status == "Paid" ? Color.FromArgb(76, 175, 80) :
                           fine.Status == "Waived" ? Color.FromArgb(255, 152, 0) :
                           Color.FromArgb(244, 67, 54)
            };
            startY += 35;
            Label lblCreated = new Label { Text = "Created:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblCreatedValue = new Label { Text = fine.CreatedDate.ToString("yyyy-MM-dd HH:mm"), Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F) };
            startY += 35;
            Label lblPaid = new Label { Text = "Paid Date:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblPaidValue = new Label
            {
                Text = fine.PaidDate?.ToString("yyyy-MM-dd HH:mm") ?? "Not paid",
                Location = new Point(160, startY),
                Size = new Size(fieldWidth, 25),
                Font = new Font("Segoe UI", 10F)
            };
            Button btnClose = new Button
            {
                Text = "Close",
                Location = new Point(350, startY + 30),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                DialogResult = DialogResult.OK
            };
            detailsForm.AcceptButton = btnClose;
            detailsForm.CancelButton = btnClose;
            detailsForm.Controls.AddRange(new Control[] {
                titleLabel, lblFineId, lblFineIdValue, lblMember, lblMemberValue,
                lblBook, lblBookValue, lblAmount, lblAmountValue, lblReason, lblReasonValue,
                lblStatus, lblStatusValue, lblCreated, lblCreatedValue, lblPaid, lblPaidValue, btnClose
            });
            detailsForm.ShowDialog();
        }
        private void ShowInventoryView()
        {
            pnlMembersView.Visible = false;
            ShowDashboardControls(false);
            ClearDynamicControls();
            Panel staffInventoryHeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 140,
                BackColor = Color.White
            };
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
                Size = new Size(400, 25),
                ForeColor = Color.FromArgb(100, 100, 100),
                BackColor = Color.Transparent
            };
            staffInventoryHeaderPanel.Controls.AddRange(new Control[] { titleLabel, subtitleLabel });
            Button btnExportInventory = new Button
            {
                Text = "Export Inventory",
                Location = new Point(pnlMainContent.Width - 180, 20),
                Size = new Size(150, 35),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(128, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnExportInventory.FlatAppearance.BorderSize = 0;
            btnExportInventory.MouseEnter += (s, e) => btnExportInventory.BackColor = Color.FromArgb(150, 0, 0);
            btnExportInventory.MouseLeave += (s, e) => btnExportInventory.BackColor = Color.FromArgb(128, 0, 0);
            Panel statsPanel = new Panel
            {
                Location = new Point(30, 150),
                Size = new Size(pnlMainContent.Width - 60, 160), 
                BackColor = Color.Transparent,
                Tag = "InventoryStatsPanel"
            };
            Panel cardTotalTitles = CreateCatalogStatCard("📚", "0", "Total Titles", Color.FromArgb(128, 0, 0), new Point(0, 0));
            Panel cardTotalCopies = CreateCatalogStatCard("📖", "0", "Total Copies", Color.White, new Point(250, 0));
            Panel cardAvailable = CreateCatalogStatCard("✓", "0", "Available", Color.FromArgb(76, 175, 80), new Point(500, 0));
            Panel cardBorrowed = CreateCatalogStatCard("📗", "0", "Borrowed", Color.FromArgb(33, 150, 243), new Point(0, 80));
            Panel cardDamaged = CreateCatalogStatCard("⚠", "0", "Damaged", Color.FromArgb(255, 193, 7), new Point(250, 80));
            Panel cardLost = CreateCatalogStatCard("❌", "0", "Lost", Color.FromArgb(244, 67, 54), new Point(500, 80));
            statsPanel.Controls.AddRange(new Control[] { cardTotalTitles, cardTotalCopies, cardAvailable, cardBorrowed, cardDamaged, cardLost });
            Panel categoryPanel = new Panel
            {
                Location = new Point(30, 330), 
                Size = new Size(pnlMainContent.Width - 60, 120),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Tag = "CategoryPanel"
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
            Label categoryTitle = new Label
            {
                Text = "Collection by Category",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(20, 15),
                Size = new Size(250, 25),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            Label categorySubtitle = new Label
            {
                Text = "Availability distribution across categories",
                Font = new Font("Segoe UI", 9F),
                Location = new Point(20, 40),
                Size = new Size(300, 20),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
            Panel categoryContentPanel = new Panel
            {
                Location = new Point(20, 65),
                Size = new Size(categoryPanel.Width - 40, 45),
                BackColor = Color.Transparent
            };
            categoryPanel.Controls.AddRange(new Control[] { categoryTitle, categorySubtitle, categoryContentPanel });
            Panel searchPanel = new Panel
            {
                Location = new Point(30, 470), 
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
                Size = new Size(400, 40),
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.None
            };
            Label searchIcon = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI", 12F),
                Location = new Point(10, 10),
                Size = new Size(25, 20),
                ForeColor = Color.FromArgb(150, 150, 150),
                TextAlign = ContentAlignment.MiddleCenter
            };
            searchContainer.Controls.Add(searchIcon);
            TextBox txtSearchInventory = new TextBox
            {
                Location = new Point(40, 8),
                Size = new Size(340, 24),
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(245, 245, 245)
            };
            txtSearchInventory.SetPlaceholder("Search by title, accession number, or location...");
            searchContainer.Controls.Add(txtSearchInventory);
            Label lblStatusFilter = new Label
            {
                Text = "Status:",
                Location = new Point(searchPanel.Width - 220, 18),
                Size = new Size(60, 25),
                Font = new Font("Segoe UI", 10F),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            ComboBox cmbStatusFilter = new ComboBox
            {
                Location = new Point(searchPanel.Width - 160, 15),
                Size = new Size(140, 30),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            cmbStatusFilter.Items.AddRange(new[] { "All Status", "Available", "Borrowed", "Damaged", "Lost" });
            cmbStatusFilter.SelectedIndex = 0;
            DataGridView dgvInventory = new DataGridView
            {
                Location = new Point(30, 550), 
                Size = new Size(pnlMainContent.Width - 60, pnlMainContent.Height - 580), 
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvInventory.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(40, 40, 40),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };
            dgvInventory.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(40, 40, 40),
                Font = new Font("Segoe UI", 9F),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                SelectionBackColor = Color.FromArgb(128, 0, 0),
                SelectionForeColor = Color.White
            };
            dgvInventory.Columns.Add("BookId", "ID");
            dgvInventory.Columns.Add("CopyId", "Copy ID");
            dgvInventory.Columns.Add("BookTitle", "Book Title");
            dgvInventory.Columns.Add("AccessionNumber", "Accession #");
            dgvInventory.Columns.Add("Location", "Location");
            dgvInventory.Columns.Add("Condition", "Condition");
            dgvInventory.Columns.Add("Status", "Status");
            dgvInventory.Columns.Add("Actions", "Actions");
            dgvInventory.Columns["BookId"].Visible = false;
            dgvInventory.Columns["CopyId"].Width = 100;
            dgvInventory.Columns["BookTitle"].Width = 250;
            dgvInventory.Columns["AccessionNumber"].Width = 150;
            dgvInventory.Columns["Location"].Width = 200;
            dgvInventory.Columns["Condition"].Width = 100;
            dgvInventory.Columns["Status"].Width = 120;
            DataGridViewButtonColumn editButtonColumn = new DataGridViewButtonColumn
            {
                Name = "Edit",
                HeaderText = "",
                Text = "✏️",
                UseColumnTextForButtonValue = true,
                Width = 50
            };
            dgvInventory.Columns.Add(editButtonColumn);
            void LoadInventoryData()
            {
                try
                {
                    var inventoryService = new Library_Management_System.Service.InventoryService();
                    var bookService = new Library_Management_System.Service.BookService();
                    string searchText = txtSearchInventory.GetActualText();
                    string statusFilter = cmbStatusFilter.SelectedItem?.ToString() ?? "All Status";
                    var books = bookService.GetBooks("", "All Categories");
                    var summary = inventoryService.GetInventorySummary();
                    int totalTitles = books.Count;
                    int totalCopies = summary.ContainsKey("TotalCopies") ? summary["TotalCopies"] : 0;
                    int availableCopies = summary.ContainsKey("AvailableCopies") ? summary["AvailableCopies"] : 0;
                    int borrowedCopies = summary.ContainsKey("BorrowedCopies") ? summary["BorrowedCopies"] : 0;
                    int damagedCopies = 0; 
                    int lostCopies = 0; 
                    UpdateCatalogStatCard(cardTotalTitles, totalTitles.ToString());
                    UpdateCatalogStatCard(cardTotalCopies, totalCopies.ToString());
                    UpdateCatalogStatCard(cardAvailable, availableCopies.ToString());
                    UpdateCatalogStatCard(cardBorrowed, borrowedCopies.ToString());
                    UpdateCatalogStatCard(cardDamaged, damagedCopies.ToString());
                    UpdateCatalogStatCard(cardLost, lostCopies.ToString());
                    var categoryStats = new Dictionary<string, (int available, int total)>();
                    foreach (var book in books)
                    {
                        string cat = book.Category ?? "Uncategorized";
                        if (!categoryStats.ContainsKey(cat))
                        {
                            categoryStats[cat] = (0, 0);
                        }
                        var current = categoryStats[cat];
                        categoryStats[cat] = (current.available + book.AvailableCopies, current.total + book.TotalCopies);
                    }
                    categoryContentPanel.Controls.Clear();
                    int categoryY = 0;
                    foreach (var kvp in categoryStats)
                    {
                        string categoryName = kvp.Key;
                        int available = kvp.Value.available;
                        int total = kvp.Value.total;
                        Label categoryLabel = new Label
                        {
                            Text = $"{categoryName}: {available}/{total} available",
                            Font = new Font("Segoe UI", 9F),
                            Location = new Point(0, categoryY),
                            Size = new Size(200, 20),
                            ForeColor = Color.FromArgb(40, 40, 40)
                        };
                        Panel progressBarBg = new Panel
                        {
                            Location = new Point(210, categoryY + 2),
                            Size = new Size(400, 16),
                            BackColor = Color.FromArgb(230, 230, 230),
                            BorderStyle = BorderStyle.None
                        };
                        int progressWidth = total > 0 ? (int)(400 * ((double)available / total)) : 0;
                        Panel progressBar = new Panel
                        {
                            Location = new Point(0, 0),
                            Size = new Size(progressWidth, 16),
                            BackColor = Color.FromArgb(128, 0, 0),
                            BorderStyle = BorderStyle.None
                        };
                        progressBarBg.Controls.Add(progressBar);
                        categoryContentPanel.Controls.Add(categoryLabel);
                        categoryContentPanel.Controls.Add(progressBarBg);
                        categoryY += 22;
                    }
                    dgvInventory.Rows.Clear();
                    int copyNumber = 1;
                    foreach (var book in books)
                    {
                        for (int copyIndex = 1; copyIndex <= book.TotalCopies; copyIndex++)
                        {
                            string copyStatus;
                            if (copyIndex <= book.AvailableCopies)
                            {
                                copyStatus = "Available";
                        }
                        else
                        {
                                copyStatus = "Borrowed";
                            }
                            if (statusFilter != "All Status" && copyStatus != statusFilter)
                            {
                                continue;
                            }
                            if (!string.IsNullOrEmpty(searchText))
                            {
                                string searchLower = searchText.ToLower();
                                bool matches = 
                                    book.Title.ToLower().Contains(searchLower) ||
                                    book.Author.ToLower().Contains(searchLower) ||
                                    $"ACC-2024-{copyNumber:D5}".ToLower().Contains(searchLower) ||
                                    "Section A, Shelf 3".ToLower().Contains(searchLower);
                                if (!matches)
                                {
                                    copyNumber++;
                                    continue;
                                }
                            }
                            string accessionNumber = $"ACC-2024-{copyNumber:D5}";
                            string location = "Section A, Shelf 3";
                            string condition = "Good";
                            int rowIndex = dgvInventory.Rows.Add();
                            dgvInventory.Rows[rowIndex].Cells["BookId"].Value = book.BookId;
                            dgvInventory.Rows[rowIndex].Cells["CopyId"].Value = $"Copy #{copyIndex}";
                            dgvInventory.Rows[rowIndex].Cells["BookTitle"].Value = $"{book.Title} {book.Author}";
                            dgvInventory.Rows[rowIndex].Cells["AccessionNumber"].Value = accessionNumber;
                            dgvInventory.Rows[rowIndex].Cells["Location"].Value = $"📍 {location}";
                            dgvInventory.Rows[rowIndex].Cells["Condition"].Value = condition;
                            var statusCell = dgvInventory.Rows[rowIndex].Cells["Status"];
                            if (copyStatus == "Available")
                            {
                                statusCell.Value = "✓ Available";
                                statusCell.Style.ForeColor = Color.FromArgb(76, 175, 80); 
                            }
                            else if (copyStatus == "Borrowed")
                            {
                                statusCell.Value = "📗 Borrowed";
                                statusCell.Style.ForeColor = Color.FromArgb(33, 150, 243); 
                            }
                            else if (copyStatus == "Damaged")
                            {
                                statusCell.Value = "⚠ Damaged";
                                statusCell.Style.ForeColor = Color.FromArgb(255, 152, 0); 
                                damagedCopies++;
                            }
                            else if (copyStatus == "Lost")
                            {
                                statusCell.Value = "❌ Lost";
                                statusCell.Style.ForeColor = Color.FromArgb(244, 67, 54); 
                                lostCopies++;
                            }
                            var conditionCell = dgvInventory.Rows[rowIndex].Cells["Condition"];
                            conditionCell.Style.ForeColor = Color.FromArgb(33, 150, 243); 
                            copyNumber++;
                        }
                    }
                    UpdateCatalogStatCard(cardDamaged, damagedCopies.ToString());
                    UpdateCatalogStatCard(cardLost, lostCopies.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading inventory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            txtSearchInventory.TextChanged += (s, e) =>
            {
                if (txtSearchInventory.ForeColor != Color.Gray)
                {
                    LoadInventoryData();
                }
            };
            cmbStatusFilter.SelectedIndexChanged += (s, e) => LoadInventoryData();
            btnExportInventory.Click += (s, e) =>
            {
                try
                {
                    Form exportDialog = new Form
                    {
                        Text = "Export Inventory",
                        Size = new Size(450, 300),
                        StartPosition = FormStartPosition.CenterParent,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        MaximizeBox = false,
                        MinimizeBox = false,
                        ControlBox = false,
                        BackColor = Color.FromArgb(245, 245, 245)
                    };
                    Panel staffExportHeaderPanel = new Panel
                    {
                        Location = new Point(0, 0),
                        Size = new Size(exportDialog.ClientSize.Width, 100),
                        BackColor = Color.White
                    };
                    Label dialogTitleLabel = new Label
                    {
                        Text = "Choose Export Format",
                        Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                        Location = new Point(30, 15),
                        Size = new Size(300, 50),
                        ForeColor = Color.FromArgb(40, 40, 40)
                    };
                    Label dialogSubtitleLabel = new Label
                    {
                        Text = "Select your preferred format for the inventory data",
                        Font = new Font("Segoe UI", 9F),
                        Location = new Point(30, 70),
                        Size = new Size(350, 20),
                        ForeColor = Color.FromArgb(100, 100, 100)
                    };
                    Button btnCloseDialog = new Button
                    {
                        Text = "✕",
                        Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                        Location = new Point(exportDialog.Width - 45, 15),
                        Size = new Size(30, 30),
                        BackColor = Color.Transparent,
                        ForeColor = Color.FromArgb(100, 100, 100),
                        FlatStyle = FlatStyle.Flat,
                        Cursor = Cursors.Hand
                    };
                    btnCloseDialog.FlatAppearance.BorderSize = 0;
                    btnCloseDialog.Click += (sd, ed) => exportDialog.Close();
                    staffExportHeaderPanel.Controls.AddRange(new Control[] { dialogTitleLabel, dialogSubtitleLabel, btnCloseDialog });
                    Panel contentPanel = new Panel
                    {
                        Location = new Point(0, 100),
                        Size = new Size(exportDialog.ClientSize.Width, exportDialog.ClientSize.Height - 100),
                        BackColor = Color.FromArgb(250, 250, 250)
                    };
                    Label lblFormat = new Label
                    {
                        Text = "Select export format:",
                        Location = new Point(30, 20),
                        Size = new Size(200, 25),
                        Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(40, 40, 40)
                    };
                    ComboBox cmbFormat = new ComboBox
                    {
                        Location = new Point(30, 50),
                        Size = new Size(350, 35),
                        Font = new Font("Segoe UI", 11F),
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        BackColor = Color.White
                    };
                    cmbFormat.Items.AddRange(new[] { "CSV (Comma Separated Values)", "Excel (XLSX)", "Word (DOCX)", "PDF (Portable Document Format)" });
                    cmbFormat.SelectedIndex = 0;
                    Button btnCancel = new Button
                    {
                        Text = "Cancel",
                        Size = new Size(100, 40),
                        Location = new Point(120, 130),
                        Font = new Font("Segoe UI", 11F),
                        BackColor = Color.FromArgb(240, 240, 240),
                        ForeColor = Color.FromArgb(40, 40, 40),
                        FlatStyle = FlatStyle.Flat,
                        Cursor = Cursors.Hand,
                        DialogResult = DialogResult.Cancel
                    };
                    btnCancel.FlatAppearance.BorderSize = 0;
                    btnCancel.Click += (sd, ed) => exportDialog.Close();
                    Button btnExport = new Button
                    {
                        Text = "Export",
                        Size = new Size(100, 40),
                        Location = new Point(240, 130),
                        Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                        BackColor = Color.FromArgb(128, 0, 0),
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        Cursor = Cursors.Hand
                    };
                    btnExport.FlatAppearance.BorderSize = 0;
                    btnExport.MouseEnter += (sd, ed) => btnExport.BackColor = Color.FromArgb(150, 0, 0);
                    btnExport.MouseLeave += (sd, ed) => btnExport.BackColor = Color.FromArgb(128, 0, 0);
                    btnExport.Click += (sd, ed) =>
                    {
                        string format = cmbFormat.SelectedItem.ToString().Split(' ')[0];
                        ExportInventoryData(format);
                        exportDialog.Close();
                    };
                    contentPanel.Controls.AddRange(new Control[] { lblFormat, cmbFormat, btnCancel, btnExport });
                    exportDialog.Controls.AddRange(new Control[] { staffExportHeaderPanel, contentPanel });
                    exportDialog.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            dgvInventory.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                if (dgvInventory.Columns[e.ColumnIndex].Name == "Edit")
                {
                    string bookId = dgvInventory.Rows[e.RowIndex].Cells["BookId"].Value?.ToString();
                    string copyId = dgvInventory.Rows[e.RowIndex].Cells["CopyId"].Value?.ToString();
                    if (!string.IsNullOrEmpty(bookId))
                    {
                        ShowEditCopyDialog(bookId, copyId);
                    }
                }
            };
            searchPanel.Controls.AddRange(new Control[] { searchContainer, lblStatusFilter, cmbStatusFilter });
            pnlMainContent.Controls.AddRange(new Control[] { 
                staffInventoryHeaderPanel, btnExportInventory, 
                statsPanel, categoryPanel, searchPanel, dgvInventory 
            });
            LoadInventoryData();
        }
        private void ExportInventoryData(string format)
        {
            try
            {
                string fileName = "";
                string filter = "";
                string defaultExt = "";
                switch (format.ToUpper())
                {
                    case "CSV":
                        filter = "CSV files (*.csv)|*.csv";
                        defaultExt = ".csv";
                        fileName = $"Inventory_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                        break;
                    case "EXCEL":
                        filter = "Excel files (*.xlsx)|*.xlsx";
                        defaultExt = ".xlsx";
                        fileName = $"Inventory_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        break;
                    case "WORD":
                        filter = "Word files (*.docx)|*.docx";
                        defaultExt = ".docx";
                        fileName = $"Inventory_Export_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
                        break;
                    case "PDF":
                        filter = "PDF files (*.pdf)|*.pdf";
                        defaultExt = ".pdf";
                        fileName = $"Inventory_Export_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                        break;
                }
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = filter,
                    FileName = fileName,
                    DefaultExt = defaultExt
                };
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    switch (format.ToUpper())
                    {
                        case "CSV":
                            ExportToCSV(saveDialog.FileName);
                            break;
                        case "EXCEL":
                            ExportToExcel(saveDialog.FileName);
                            break;
                        case "WORD":
                            ExportToWord(saveDialog.FileName);
                            break;
                        case "PDF":
                            ExportToPDF(saveDialog.FileName);
                            break;
                    }
                    MessageBox.Show("Inventory exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting inventory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ExportToCSV(string filePath)
        {
            var dgvInventory = pnlMainContent.Controls.Find("dgvInventory", true).FirstOrDefault() as DataGridView;
            if (dgvInventory == null) return;
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filePath))
            {
                writer.WriteLine("Copy ID,Book Title,Accession Number,Location,Condition,Status");
                foreach (DataGridViewRow row in dgvInventory.Rows)
                {
                    if (row.Cells["CopyId"].Value != null)
                    {
                        writer.WriteLine($"\"{row.Cells["CopyId"].Value}\"," +
                                       $"\"{row.Cells["BookTitle"].Value}\"," +
                                       $"\"{row.Cells["AccessionNumber"].Value}\"," +
                                       $"\"{row.Cells["Location"].Value}\"," +
                                       $"\"{row.Cells["Condition"].Value}\"," +
                                       $"\"{row.Cells["Status"].Value}\"");
                    }
                }
            }
        }
        private void ExportToExcel(string filePath)
        {
            var dgvInventory = pnlMainContent.Controls.Find("dgvInventory", true).FirstOrDefault() as DataGridView;
            if (dgvInventory == null) return;
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filePath))
            {
                writer.WriteLine("<?xml version=\"1.0\"?>");
                writer.WriteLine("<?mso-application progid=\"Excel.Sheet\"?>");
                writer.WriteLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
                writer.WriteLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
                writer.WriteLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
                writer.WriteLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
                writer.WriteLine(" <Worksheet ss:Name=\"Inventory\">");
                writer.WriteLine("  <Table>");
                writer.WriteLine("   <Row>");
                writer.WriteLine("    <Cell><Data ss:Type=\"String\">Copy ID</Data></Cell>");
                writer.WriteLine("    <Cell><Data ss:Type=\"String\">Book Title</Data></Cell>");
                writer.WriteLine("    <Cell><Data ss:Type=\"String\">Accession Number</Data></Cell>");
                writer.WriteLine("    <Cell><Data ss:Type=\"String\">Location</Data></Cell>");
                writer.WriteLine("    <Cell><Data ss:Type=\"String\">Condition</Data></Cell>");
                writer.WriteLine("    <Cell><Data ss:Type=\"String\">Status</Data></Cell>");
                writer.WriteLine("   </Row>");
                foreach (DataGridViewRow row in dgvInventory.Rows)
                {
                    if (row.Cells["CopyId"].Value != null)
                    {
                        writer.WriteLine("   <Row>");
                        writer.WriteLine($"    <Cell><Data ss:Type=\"String\">{row.Cells["CopyId"].Value}</Data></Cell>");
                        writer.WriteLine($"    <Cell><Data ss:Type=\"String\">{row.Cells["BookTitle"].Value}</Data></Cell>");
                        writer.WriteLine($"    <Cell><Data ss:Type=\"String\">{row.Cells["AccessionNumber"].Value}</Data></Cell>");
                        writer.WriteLine($"    <Cell><Data ss:Type=\"String\">{row.Cells["Location"].Value}</Data></Cell>");
                        writer.WriteLine($"    <Cell><Data ss:Type=\"String\">{row.Cells["Condition"].Value}</Data></Cell>");
                        writer.WriteLine($"    <Cell><Data ss:Type=\"String\">{row.Cells["Status"].Value}</Data></Cell>");
                        writer.WriteLine("   </Row>");
                    }
                }
                writer.WriteLine("  </Table>");
                writer.WriteLine(" </Worksheet>");
                writer.WriteLine("</Workbook>");
            }
        }
        private void ExportToWord(string filePath)
        {
            var dgvInventory = pnlMainContent.Controls.Find("dgvInventory", true).FirstOrDefault() as DataGridView;
            if (dgvInventory == null) return;
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filePath))
            {
                writer.WriteLine("{\\rtf1\\ansi\\deff0");
                writer.WriteLine("{\\fonttbl{\\f0\\fswiss\\fcharset0 Arial;}}");
                writer.WriteLine("\\viewkind4\\uc1\\pard\\lang1033\\f0\\fs24");
                writer.WriteLine("\\b Library Inventory Report\\b0\\par");
                writer.WriteLine($"Generated on: {DateTime.Now:MMMM dd, yyyy} at {DateTime.Now:hh:mm tt}\\par\\par");
                writer.WriteLine("\\trowd\\trgaph108\\trleft0");
                writer.WriteLine("\\clbrdrt\\brdrs\\clbrdrl\\brdrs\\clbrdrb\\brdrs\\clbrdrr\\brdrs");
                writer.WriteLine("\\cellx1440 Copy ID\\cellx4320 Book Title\\cellx5760 Accession #\\cellx7200 Location\\cellx8640 Condition\\cellx10080 Status\\cell\\row");
                foreach (DataGridViewRow row in dgvInventory.Rows)
                {
                    if (row.Cells["CopyId"].Value != null)
                    {
                        writer.WriteLine("\\trowd\\trgaph108\\trleft0");
                        writer.WriteLine("\\clbrdrt\\brdrs\\clbrdrl\\brdrs\\clbrdrb\\brdrs\\clbrdrr\\brdrs");
                        writer.WriteLine($"\\cellx1440 {row.Cells["CopyId"].Value}\\cellx4320 {row.Cells["BookTitle"].Value}\\cellx5760 {row.Cells["AccessionNumber"].Value}\\cellx7200 {row.Cells["Location"].Value}\\cellx8640 {row.Cells["Condition"].Value}\\cellx10080 {row.Cells["Status"].Value}\\cell\\row");
                    }
                }
                writer.WriteLine("\\par");
                writer.WriteLine("Total Records: " + dgvInventory.Rows.Count.ToString());
                writer.WriteLine("}");
            }
        }
        private void ExportToPDF(string filePath)
        {
            var dgvInventory = pnlMainContent.Controls.Find("dgvInventory", true).FirstOrDefault() as DataGridView;
            if (dgvInventory == null) return;
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filePath))
            {
                writer.WriteLine("<!DOCTYPE html>");
                writer.WriteLine("<html><head>");
                writer.WriteLine("<title>Library Inventory Report</title>");
                writer.WriteLine("<style>");
                writer.WriteLine("body { font-family: Arial, sans-serif; margin: 20px; }");
                writer.WriteLine("h1 { color: #800000; text-align: center; }");
                writer.WriteLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
                writer.WriteLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
                writer.WriteLine("th { background-color: #f2f2f2; font-weight: bold; }");
                writer.WriteLine("tr:nth-child(even) { background-color: #f9f9f9; }");
                writer.WriteLine(".header { text-align: center; margin-bottom: 10px; color: #666; }");
                writer.WriteLine("</style>");
                writer.WriteLine("</head><body>");
                writer.WriteLine("<h1>Library Inventory Report</h1>");
                writer.WriteLine($"<div class='header'>Generated on: {DateTime.Now:MMMM dd, yyyy} at {DateTime.Now:hh:mm tt}</div>");
                writer.WriteLine("<table>");
                writer.WriteLine("<tr>");
                writer.WriteLine("<th>Copy ID</th>");
                writer.WriteLine("<th>Book Title</th>");
                writer.WriteLine("<th>Accession Number</th>");
                writer.WriteLine("<th>Location</th>");
                writer.WriteLine("<th>Condition</th>");
                writer.WriteLine("<th>Status</th>");
                writer.WriteLine("</tr>");
                foreach (DataGridViewRow row in dgvInventory.Rows)
                {
                    if (row.Cells["CopyId"].Value != null)
                    {
                        writer.WriteLine("<tr>");
                        writer.WriteLine($"<td>{HtmlEncode(row.Cells["CopyId"].Value.ToString())}</td>");
                        writer.WriteLine($"<td>{HtmlEncode(row.Cells["BookTitle"].Value.ToString())}</td>");
                        writer.WriteLine($"<td>{HtmlEncode(row.Cells["AccessionNumber"].Value.ToString())}</td>");
                        writer.WriteLine($"<td>{HtmlEncode(row.Cells["Location"].Value.ToString())}</td>");
                        writer.WriteLine($"<td>{HtmlEncode(row.Cells["Condition"].Value.ToString())}</td>");
                        writer.WriteLine($"<td>{HtmlEncode(row.Cells["Status"].Value.ToString())}</td>");
                        writer.WriteLine("</tr>");
                    }
                }
                writer.WriteLine("</table>");
                writer.WriteLine($"<p><strong>Total Records: {dgvInventory.Rows.Count.ToString()}</strong></p>");
                writer.WriteLine("</body></html>");
            }
            string htmlFilePath = Path.ChangeExtension(filePath, ".html");
            File.Move(filePath, htmlFilePath);
            MessageBox.Show("HTML file created successfully! You can open it in a web browser and print/save as PDF.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private string HtmlEncode(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Replace("&", "&amp;")
                      .Replace("<", "&lt;")
                      .Replace(">", "&gt;")
                      .Replace("\"", "&quot;")
                      .Replace("'", "&#39;");
        }
        private void ShowEditCopyDialog(string bookId, string copyId)
        {
            MessageBox.Show($"Edit Copy dialog for {copyId} of Book ID: {bookId}\n\nThis feature will allow editing:\n- Location\n- Condition\n- Status", 
                "Edit Copy", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void ShowUpdateStockDialog()
        {
            Form updateForm = new Form
            {
                Text = "Update Stock Level",
                Size = new Size(450, 330),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false
            };
            Label titleLabel = new Label
            {
                Text = "Update Stock Level",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(300, 65),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            Label lblBookId = new Label { Text = "Book ID:", Location = new Point(30, 100), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtBookId = new TextBox { Location = new Point(140, 95), Size = new Size(200, 30), Font = new Font("Segoe UI", 10F) };
            Label lblCurrentInfo = new Label
            {
                Text = "Current: Total - 0, Available - 0",
                Location = new Point(30, 140),
                Size = new Size(350, 25),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
            Label lblNewTotal = new Label { Text = "New Total Copies:", Location = new Point(30, 150), Size = new Size(120, 25), Font = new Font("Segoe UI", 10F) };
            NumericUpDown numNewTotal = new NumericUpDown
            {
                Location = new Point(160, 145),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 10F),
                Minimum = 0,
                Maximum = 1000,
                Value = 0
            };
            Label lblPreview = new Label
            {
                Text = "Available after update: 0",
                Location = new Point(30, 185),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(33, 150, 243)
            };
            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(250, 220),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                DialogResult = DialogResult.Cancel
            };
            Button btnUpdate = new Button
            {
                Text = "Update",
                Location = new Point(340, 220),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnUpdate.FlatAppearance.BorderSize = 0;
            txtBookId.TextChanged += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(txtBookId.Text))
                {
                    try
                    {
                        var bookService = new Library_Management_System.Service.BookService();
                        var book = bookService.GetBookById(txtBookId.Text.Trim());
                        if (book != null)
                        {
                            lblCurrentInfo.Text = $"Current: Total - {book.TotalCopies}, Available - {book.AvailableCopies}";
                            numNewTotal.Value = book.TotalCopies;
                            int borrowed = book.TotalCopies - book.AvailableCopies;
                            int newAvailable = System.Math.Max(0, (int)numNewTotal.Value - borrowed);
                            lblPreview.Text = $"Available after update: {newAvailable}";
                        }
                        else
                        {
                            lblCurrentInfo.Text = "Book not found";
                            lblPreview.Text = "Available after update: 0";
                        }
                    }
                    catch (Exception ex)
                    {
                        lblCurrentInfo.Text = $"Error: {ex.Message}";
                        lblPreview.Text = "Available after update: 0";
                    }
                }
            };
            numNewTotal.ValueChanged += (s, e) =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(txtBookId.Text))
                    {
                        var bookService = new Library_Management_System.Service.BookService();
                        var book = bookService.GetBookById(txtBookId.Text.Trim());
                        if (book != null)
                        {
                            int borrowed = book.TotalCopies - book.AvailableCopies;
                            int newAvailable = System.Math.Max(0, (int)numNewTotal.Value - borrowed);
                            lblPreview.Text = $"Available after update: {newAvailable}";
                        }
                    }
                }
                catch (Exception)
                {
                    lblPreview.Text = "Available after update: 0";
                }
            };
            txtBookId.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) numNewTotal.Focus(); };
            btnUpdate.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtBookId.Text))
                {
                    MessageBox.Show("Please enter a book ID.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtBookId.Focus();
                    return;
                }
                try
                {
                    var inventoryService = new Library_Management_System.Service.InventoryService();
                    inventoryService.UpdateInventory(txtBookId.Text.Trim(), (int)numNewTotal.Value);
                    MessageBox.Show("Stock level updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    updateForm.DialogResult = DialogResult.OK;
                    updateForm.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating stock: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            updateForm.AcceptButton = btnUpdate;
            updateForm.CancelButton = btnCancel;
            btnCancel.Click += (s, e) => updateForm.Close();
            updateForm.Controls.AddRange(new Control[] {
                titleLabel, lblBookId, txtBookId, lblCurrentInfo, lblNewTotal, numNewTotal, lblPreview, btnCancel, btnUpdate
            });
            updateForm.ShowDialog();
        }
    }
}
