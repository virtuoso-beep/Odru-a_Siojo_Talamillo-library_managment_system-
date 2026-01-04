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
namespace Library_Management_System.Forms.members
{
    partial class MembersDashboard : Form
    {
        private System.Windows.Forms.Timer _sessionTimer;
        private const int SessionTimeoutMinutes = 30; 
        private Panel pnlSearchView;
        private Panel pnlSearchContent;
        private TextBox txtSearchInput;
        private Button btnTriggerSearch;
        private Button btnSearchFilters;
        private List<Control> _originalMainContentControls = new List<Control>();
        public MembersDashboard()
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
        private void SetupMenuButtonHoverEffects()
        {
            Button[] menuButtons = { btnDashboard, btnCatalog, 
                btnReservations, btnSearch };
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
            btnSearch.BackColor = ThemeConstants.SecondaryMaroon;
            btnSearch.ForeColor = ThemeConstants.TextWhite;
            btnSearch.Font = ThemeConstants.FontBody;
            btnSearch.Text = "  🔍 Search";
            ApplyModernButtonStyling(btnDashboard);
            ApplyModernButtonStyling(btnCatalog);
            ApplyModernButtonStyling(btnReservations);
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
            else if (pnlSearchView != null && pnlMainContent.Controls.Contains(pnlSearchView))
            {
                pnlSearchView.Size = pnlMainContent.ClientSize;
            }
            else
            {
                AdjustModuleViewLayout();
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
                case "Catalog":
                    ShowCatalogView();
                    break;
                case "Reservations":
                    ShowReservationsView();
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
        }
        private void ShowSearchView()
        {
            RestoreOriginalControls();
            ShowDashboardControls(false);
            pnlMembersView.Visible = false;
            if (pnlSearchView == null) SetupSearchView();
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
            if (pnlSearchView != null) return;
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
            Label titleLabel = new Label
            {
                Text = "Catalog",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(200, 40),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            Label subtitleLabel = new Label
            {
                Text = "Browse and manage library books and resources",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Location = new Point(30, 60),
                Size = new Size(400, 25),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
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
                    Location = new Point(30, 100),
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
                Location = new Point(30, 270),
                Size = new Size(pnlMainContent.Width - 60, pnlMainContent.Height - 300),
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
            Panel headerPanel = new Panel
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
                Size = new Size(300, 30),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            Label subtitleLabel = new Label
            {
                Text = "Enter the book details to add it to the catalog",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(30, 50),
                Size = new Size(500, 20),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
            headerPanel.Controls.AddRange(new Control[] { titleLabel, subtitleLabel });
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
            addForm.Controls.Add(headerPanel);
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
                    Size = new Size(520, 650),
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
                    Size = new Size(400, 35),
                    ForeColor = Color.FromArgb(40, 40, 40)
                };
                int startY = 70;
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
        private void ShowReservationsView()
        {
            pnlMembersView.Visible = false;
            ShowDashboardControls(false);
            ClearDynamicControls();
            Label titleLabel = new Label
            {
                Text = "Reservations",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(200, 40),
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            Label subtitleLabel = new Label
            {
                Text = "Manage book reservations and pickup notifications",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                Location = new Point(30, 60),
                Size = new Size(400, 25),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
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
            pnlMainContent.Controls.AddRange(new Control[] { titleLabel, subtitleLabel, btnNewReservation, statsPanel, searchPanel, dgvReservations });
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
    }
}
