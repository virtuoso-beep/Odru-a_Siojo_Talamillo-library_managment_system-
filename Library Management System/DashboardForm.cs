    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Windows.Forms;
using Library_Management_System.Helper;
using Library_Management_System.Models;
using Library_Management_System.Service;
using static Library_Management_System.Helper.PlaceholderTextHelper;

namespace Library_Management_System
{

    partial class DashboardForm : Form
    {
        private bool _isLoadingMembersData = false;
        private bool _isProcessingAction = false;
        private System.Windows.Forms.Timer _sessionTimer;
        private const int SessionTimeoutMinutes = 30; // 30 minutes timeout

        public DashboardForm()
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
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            // Remove size constraints for full screen mode
            // this.Size = new Size(1200, 650);
            // this.MinimumSize = new Size(1200, 650);
            // this.MaximumSize = new Size(1200, 650);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            SetFormIcon();
            
            ApplyFormRoundedCorners();
            
            this.Resize += DashboardForm_Resize;

            // Initialize session timeout timer (30 minutes)
            InitializeSessionTimeout();

            ApplyModernDashboardStyling();
            ResetMenuHighlights();

            // Set up placeholder text for search fields
            txtSearchMembers.SetPlaceholder("Search members by name, email, or ID...");

            LoadUserInfo();

            // Load logo
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
        }

        private void InitializeSessionTimeout()
        {
            _sessionTimer = new System.Windows.Forms.Timer();
            _sessionTimer.Interval = SessionTimeoutMinutes * 60 * 1000; // Convert minutes to milliseconds
            _sessionTimer.Tick += SessionTimer_Tick;

            // Reset timer on user activity
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
                // Add gradient effect for modern look
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

                // Draw border
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
                lblWelcome.Text = $"Welcome back, {currentUser.FirstName}. Here's what's happening today.";
                lblAdminName.Text = currentUser.FirstName;
            }
            else
            {
                lblWelcome.Text = "Welcome! admin";
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
                Font = new Font("Segoe UI", 22F, FontStyle.Regular),
                ForeColor = cardPanel.BackColor == ThemeConstants.PrimaryMaroon ? ThemeConstants.TextWhite : ThemeConstants.PrimaryMaroon,
                Location = new Point(cardPanel.Width - 55, 18),
                Size = new Size(40, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = "CardIcon"
            };
            cardPanel.Controls.Add(lblIcon);
        }

        private void ResetMenuHighlights()
        {
            // Apply consistent maroon theme to all menu buttons
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

            btnSettings.BackColor = ThemeConstants.SecondaryMaroon;
            btnSettings.ForeColor = ThemeConstants.TextWhite;
            btnSettings.Font = ThemeConstants.FontBody;
            btnSettings.Text = "  ⚙️ Settings";

            // Apply modern styling to all buttons
            ApplyModernButtonStyling(btnDashboard);
            ApplyModernButtonStyling(btnMembers);
            ApplyModernButtonStyling(btnCatalog);
            ApplyModernButtonStyling(btnCirculation);
            ApplyModernButtonStyling(btnFines);
            ApplyModernButtonStyling(btnInventory);
            ApplyModernButtonStyling(btnReservations);
            ApplyModernButtonStyling(btnReports);
            ApplyModernButtonStyling(btnSearch);
            ApplyModernButtonStyling(btnSettings);

            // Set active state for dashboard
            btnDashboard.BackColor = ThemeConstants.AccentMaroon;
        }

        private void ApplyModernDashboardStyling()
        {
            // Apply gradient background to main panel
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

            // Add modern styling to sidebar
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

            // Style the main content area
            pnlMainContent.BackColor = ThemeConstants.BackgroundLight;
        }

        private void ApplyModernButtonStyling(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Padding = new Padding(10, 0, 0, 0);

            // Add hover effects
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
            ApplyFormRoundedCorners();

            // Adjust layout for full screen mode
            if (this.WindowState == FormWindowState.Maximized)
            {
                // Ensure panels fill the entire screen properly
                pnlSidebar.Height = this.ClientSize.Height;
                pnlMainContent.Width = this.ClientSize.Width - pnlSidebar.Width;
                pnlMainContent.Height = this.ClientSize.Height;

                // Adjust main content padding for full screen
                pnlMainContent.Padding = new Padding(40, 45, 40, 45);

                // Refresh the current view to ensure proper scaling
                RefreshCurrentView();
            }
        }

        private void RefreshCurrentView()
        {
            // This method will be called to refresh the current view when resized
            if (pnlMainContent.Controls.Contains(pnlCardTotalBooks))
            {
                // Dashboard view is active, refresh card positions and text
                AdjustDashboardCards();
                AdjustDashboardText();
            }
            else if (pnlMainContent.Controls.Contains(pnlMembersView))
            {
                // Members view is active, adjust layout
                AdjustMembersViewLayout();
            }
            else
            {
                // Other module views (Catalog, Circulation, Fines, Inventory)
                AdjustModuleViewLayout();
            }
        }

        private void AdjustMembersViewLayout()
        {
            // Ensure members view scales properly in full screen
            if (pnlMembersView != null)
            {
                pnlMembersView.Width = pnlMainContent.Width - 60;
                pnlMembersView.Height = pnlMainContent.Height - 80;
            }
        }

        private void AdjustModuleViewLayout()
        {
            // Adjust layout for module views (Catalog, Circulation, Fines, Inventory)
            foreach (Control ctrl in pnlMainContent.Controls)
            {
                if (ctrl is DataGridView dgv)
                {
                    // Adjust DataGridView size for full screen
                    dgv.Width = pnlMainContent.Width - 60;
                    dgv.Height = pnlMainContent.Height - ctrl.Top - 20;
                }
                else if (ctrl is Panel panel && (panel.Name.Contains("search") || panel.Name.Contains("summary") || panel.Name.Contains("alerts")))
                {
                    // Adjust panel widths
                    panel.Width = pnlMainContent.Width - 60;
                }
            }
        }

        private void AdjustDashboardText()
        {
            // Ensure text elements scale properly in full screen mode
            if (lblWelcome != null)
            {
                // Make welcome text responsive
                int maxWidth = pnlMainContent.Width - 120;
                if (lblWelcome.Width > maxWidth)
                {
                    lblWelcome.MaximumSize = new Size(maxWidth, 0);
                    lblWelcome.AutoSize = true;
                }
            }

            if (lblDate != null)
            {
                // Ensure date label is positioned correctly
                lblDate.Location = new Point(pnlMainContent.Width - lblDate.Width - 60, 40);
            }
        }

        private void AdjustDashboardCards()
        {
            // Ensure dashboard cards are properly positioned in full screen
            if (pnlCardTotalBooks != null && pnlCardActiveMembers != null &&
                pnlCardBooksBorrowed != null && pnlCardOverdueBooks != null)
            {
                // Calculate card spacing for full screen
                int cardWidth = (pnlMainContent.Width - 120) / 4; // 4 cards with margins
                int cardHeight = 120;
                int startX = 30;
                int startY = 120;

                pnlCardTotalBooks.Size = new Size(cardWidth, cardHeight);
                pnlCardTotalBooks.Location = new Point(startX, startY);

                pnlCardActiveMembers.Size = new Size(cardWidth, cardHeight);
                pnlCardActiveMembers.Location = new Point(startX + cardWidth + 30, startY);

                pnlCardBooksBorrowed.Size = new Size(cardWidth, cardHeight);
                pnlCardBooksBorrowed.Location = new Point(startX + (cardWidth + 30) * 2, startY);

                pnlCardOverdueBooks.Size = new Size(cardWidth, cardHeight);
                pnlCardOverdueBooks.Location = new Point(startX + (cardWidth + 30) * 3, startY);
            }
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
                SiginForm.ClearCurrentUser();
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
                    ShowFeatureMessage("Reservations", 
                        "The Reservations module will allow you to:\n" +
                        "• View book reservations\n" +
                        "• Manage reservation requests\n" +
                        "• Process reservation confirmations");
                    break;

                case "Fines":
                    ShowFinesView();
                    break;

                case "Inventory":
                    ShowInventoryView();
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
            pnlWeeklyCirculation.Visible = show;
            pnlCollectionCategory.Visible = show;
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
                Text = "Member Preview",
                Size = new Size(650, 800),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
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

            // Input validation
            txtPhone.KeyPress += (s, e) =>
            {
                // Allow numbers, spaces, hyphens, parentheses, plus sign, and backspace
                if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar) &&
                    e.KeyChar != ' ' && e.KeyChar != '-' && e.KeyChar != '(' &&
                    e.KeyChar != ')' && e.KeyChar != '+')
                {
                    e.Handled = true; // Block the character
                }
            };

            // Ensure address field accepts all input including numbers
            txtAddress.KeyPress += (s, e) =>
            {
                // Allow all characters - no restrictions for address field
                // This explicitly allows numbers, letters, symbols, etc.
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

                // Additional validation for names
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

        private void ShowRegisterMemberDialog()
        {
            Form registerForm = new Form
            {
                Text = "Register New Member",
                Size = new Size(800, 650),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false,
                BackColor = ThemeConstants.BackgroundLight
            };

            // Header Section
            Label titleLabel = new Label
            {
                Text = "👤 Register New Member",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(740, 45),
                ForeColor = Color.FromArgb(40, 40, 40),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label subtitleLabel = new Label
            {
                Text = "Complete member information for library system registration",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(30, 65),
                Size = new Size(740, 20),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            // Personal Information Section
            Panel personalPanel = new Panel
            {
                Location = new Point(20, 100),
                Size = new Size(760, 180),
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

            // Left Column
            Label lblFirstName = new Label { Text = "First Name *", Location = new Point(20, 45), Size = new Size(100, 20), Font = ThemeConstants.FontBodySmall, ForeColor = ThemeConstants.TextPrimary };
            TextBox txtFirstName = new TextBox { Location = new Point(20, 65), Size = new Size(160, ThemeConstants.InputHeight), Font = ThemeConstants.FontBodySmall, BackColor = ThemeConstants.BackgroundWhite, ForeColor = ThemeConstants.TextPrimary };

            Label lblLastName = new Label { Text = "Last Name *", Location = new Point(20, 95), Size = new Size(100, 20), Font = ThemeConstants.FontBodySmall, ForeColor = ThemeConstants.TextPrimary };
            TextBox txtLastName = new TextBox { Location = new Point(20, 115), Size = new Size(160, ThemeConstants.InputHeight), Font = ThemeConstants.FontBodySmall, BackColor = ThemeConstants.BackgroundWhite, ForeColor = ThemeConstants.TextPrimary };

            Label lblDateOfBirth = new Label { Text = "Date of Birth", Location = new Point(20, 145), Size = new Size(100, 20), Font = ThemeConstants.FontBodySmall, ForeColor = ThemeConstants.TextPrimary };
            DateTimePicker dtpDateOfBirth = new DateTimePicker
            {
                Location = new Point(20, 165),
                Size = new Size(160, ThemeConstants.InputHeight),
                Font = ThemeConstants.FontBodySmall,
                BackColor = ThemeConstants.BackgroundWhite,
                ForeColor = ThemeConstants.TextPrimary,
                Format = DateTimePickerFormat.Short,
                MaxDate = DateTime.Today.AddYears(-5),
                MinDate = DateTime.Today.AddYears(-120),
                Value = DateTime.Today.AddYears(-18)
            };

            // Right Column
            Label lblIdNumber = new Label { Text = "ID Number", Location = new Point(200, 45), Size = new Size(100, 20), Font = ThemeConstants.FontBodySmall, ForeColor = ThemeConstants.TextPrimary };
            TextBox txtIdNumber = new TextBox { Location = new Point(200, 65), Size = new Size(160, ThemeConstants.InputHeight), Font = ThemeConstants.FontBodySmall, BackColor = ThemeConstants.BackgroundWhite, ForeColor = ThemeConstants.TextPrimary };

            Label lblEmail = new Label { Text = "Email *", Location = new Point(200, 95), Size = new Size(100, 20), Font = ThemeConstants.FontBodySmall, ForeColor = ThemeConstants.TextPrimary };
            TextBox txtEmail = new TextBox { Location = new Point(200, 115), Size = new Size(320, ThemeConstants.InputHeight), Font = ThemeConstants.FontBodySmall, BackColor = ThemeConstants.BackgroundWhite, ForeColor = ThemeConstants.TextPrimary };

            // Add hint for ID Number
            Label hintId = new Label
            {
                Text = "Student ID, Employee ID, or Library Card Number",
                Font = new Font("Segoe UI", 8F),
                Location = new Point(200, 90),
                Size = new Size(200, 15),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            // Contact Information Section
            Panel contactPanel = new Panel
            {
                Location = new Point(20, 290),
                Size = new Size(760, 120),
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
            TextBox txtAddress = new TextBox { Location = new Point(220, 65), Size = new Size(300, 25), Font = ThemeConstants.FontBodySmall, BackColor = ThemeConstants.BackgroundWhite, ForeColor = ThemeConstants.TextPrimary };

            // Academic Information Section
            Panel academicPanel = new Panel
            {
                Location = new Point(20, 420),
                Size = new Size(760, 120),
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
                ForeColor = ThemeConstants.TextPrimary
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
            cmbDepartment.DropDownStyle = ComboBoxStyle.DropDown;

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
            cmbStatus.SelectedIndex = 0; // Default to Active

            // Footer with helpful information
            Panel footerPanel = new Panel
            {
                Location = new Point(20, 550),
                Size = new Size(760, 50),
                BackColor = ThemeConstants.BackgroundLight,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label footerInfo = new Label
            {
                Text = "ℹ️ Member will receive login credentials via email. Default password: Member123!\n   Required fields are marked with *",
                Font = ThemeConstants.FontBodySmall,
                Location = new Point(15, 8),
                Size = new Size(730, 35),
                ForeColor = ThemeConstants.TextSecondary
            };

            Button btnCancel = new Button
            {
                Text = "❌ Cancel",
                Location = new Point(520, 610),
                Size = new Size(120, ThemeConstants.ButtonHeight),
                Font = ThemeConstants.FontButton,
                BackColor = ThemeConstants.BackgroundMedium,
                ForeColor = ThemeConstants.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };

            Button btnRegister = new Button
            {
                Text = "✅ Register Member",
                Location = new Point(650, 610),
                Size = new Size(130, ThemeConstants.ButtonHeight),
                Font = ThemeConstants.FontButton,
                BackColor = ThemeConstants.SuccessGreen,
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
            
            // Enhanced keyboard navigation for two-column layout
            txtFirstName.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtIdNumber.Focus(); };
            txtIdNumber.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtLastName.Focus(); };
            txtLastName.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtEmail.Focus(); };
            txtEmail.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) dtpDateOfBirth.Focus(); };
            dtpDateOfBirth.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtPhone.Focus(); };
            txtPhone.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtAddress.Focus(); };
            txtAddress.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) cmbMemberType.Focus(); };
            cmbMemberType.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) cmbDepartment.Focus(); };
            cmbDepartment.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) cmbStatus.Focus(); };
            cmbStatus.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnRegister.PerformClick(); };

            // Button styling with theme constants
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.MouseEnter += (s, e) => btnCancel.BackColor = ThemeConstants.GetHoverColor(ThemeConstants.BackgroundMedium);
            btnCancel.MouseLeave += (s, e) => btnCancel.BackColor = ThemeConstants.BackgroundMedium;

            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.MouseEnter += (s, e) => btnRegister.BackColor = ThemeConstants.GetHoverColor(ThemeConstants.SuccessGreen);
            btnRegister.MouseLeave += (s, e) => btnRegister.BackColor = ThemeConstants.SuccessGreen;

            // Input validation for phone field only
            txtPhone.KeyPress += (s, e) =>
            {
                // Allow numbers, spaces, hyphens, parentheses, plus sign, and backspace
                if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar) &&
                    e.KeyChar != ' ' && e.KeyChar != '-' && e.KeyChar != '(' &&
                    e.KeyChar != ')' && e.KeyChar != '+')
                {
                    e.Handled = true; // Block the character
                }
            };

            // Ensure address field accepts all input including numbers
            txtAddress.KeyPress += (s, e) =>
            {
                // Allow all characters - no restrictions for address field
                // This explicitly allows numbers, letters, symbols, etc.
            };
            
            registerForm.AcceptButton = btnRegister;
            registerForm.CancelButton = btnCancel;
            
            btnCancel.Click += (s, e) => registerForm.Close();
            
            btnRegister.MouseEnter += (s, e) => btnRegister.Invalidate();
            btnRegister.MouseLeave += (s, e) => btnRegister.Invalidate();
            btnRegister.MouseDown += (s, e) => btnRegister.Invalidate();
            btnRegister.MouseUp += (s, e) => btnRegister.Invalidate();

            // Add panels and their contents
            personalPanel.Controls.AddRange(new Control[] {
                personalHeader, lblFirstName, txtFirstName, lblLastName, txtLastName,
                lblIdNumber, txtIdNumber, lblDateOfBirth, dtpDateOfBirth, lblEmail, txtEmail, hintId
            });

            contactPanel.Controls.AddRange(new Control[] {
                contactHeader, lblPhone, txtPhone, lblAddress, txtAddress
            });

            academicPanel.Controls.AddRange(new Control[] {
                academicHeader, lblMemberType, cmbMemberType, lblDepartment, cmbDepartment, lblStatus, cmbStatus
            });

            footerPanel.Controls.Add(footerInfo);

            // Set up placeholders for all textboxes
            txtFirstName.SetPlaceholder("Enter first name");
            txtLastName.SetPlaceholder("Enter last name");
            txtIdNumber.SetPlaceholder("Enter ID number");
            txtEmail.SetPlaceholder("john.doe.123456.tc@umindanao.edu.ph");
            txtPhone.SetPlaceholder("Enter phone number");
            txtAddress.SetPlaceholder("Enter address");

            registerForm.Controls.AddRange(new Control[]
            {
                titleLabel, subtitleLabel,
                personalPanel, contactPanel, academicPanel, footerPanel,
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
                    MessageBox.Show("Please enter a valid ID number (alphanumeric, 3-20 characters).",
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

                try
                {
                    btnRegister.Enabled = false;
                    btnRegister.Text = "Registering...";

                    var membersService = new Library_Management_System.Service.MembersService();
                    membersService.RegisterMember(
                        txtFirstName.Text.Trim(),
                        txtLastName.Text.Trim(),
                        email, 
                        txtPhone.Text.Trim(),
                        txtAddress.Text.Trim(),
                        cmbMemberType.SelectedItem.ToString(),
                        cmbStatus.SelectedItem.ToString(),
                        txtIdNumber.Text.Trim(),
                        dtpDateOfBirth.Value.Date,
                        "", // Gender - will add later if needed
                        cmbDepartment.Text.Trim()
                    );

                    // Audit log the registration
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
                    else if (ex.Message.Contains("not found"))
                        errorMessage += "Please check your input and try again.";
                    else
                        errorMessage += "Please contact system administrator if the problem persists.";

                    MessageBox.Show(errorMessage, "Registration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    System.Diagnostics.Debug.WriteLine($"Error registering member: {ex.Message}\n{ex.StackTrace}");
                    
                    btnRegister.Enabled = true;
                    btnRegister.Text = "Register Member";
                }
            };

            registerForm.Load += (s, e) => txtFirstName.Focus();

            // Add visual feedback for required fields
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

        private bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return true; // Phone is optional

            phone = phone.Trim();

            // Remove all formatting characters for validation
            string digitsOnly = System.Text.RegularExpressions.Regex.Replace(phone, @"[^\d]", "");

            // Philippine phone numbers: 10-11 digits (mobile) or with area code
            if (digitsOnly.Length < 7 || digitsOnly.Length > 12)
                return false;

            // Must start with valid prefixes for Philippine numbers
            if (digitsOnly.Length >= 10)
            {
                // Mobile numbers should start with 09 or 63
                if (!digitsOnly.StartsWith("09") && !digitsOnly.StartsWith("639") && !digitsOnly.StartsWith("63"))
                    return false;
            }

            // Check for valid characters in original format
            return System.Text.RegularExpressions.Regex.IsMatch(phone, @"^[\d\s\-\(\)\+]+$");
        }

        private bool IsValidName(string name, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            name = name.Trim();

            if (name.Length < 2 || name.Length > 50)
                return false;

            // Allow letters, spaces, hyphens, apostrophes
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z\s\-']+$"))
                return false;

            // Must contain at least one letter
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"[a-zA-Z]"))
                return false;

            return true;
        }

        private bool IsValidAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return true; // Address is optional

            address = address.Trim();

            if (address.Length < 5 || address.Length > 200)
                return false;

            // Basic validation - allow common address characters
            return System.Text.RegularExpressions.Regex.IsMatch(address, @"^[a-zA-Z0-9\s,.\-#]+$");
        }

        private bool IsValidIdNumber(string idNumber)
        {
            if (string.IsNullOrWhiteSpace(idNumber))
                return true; // ID Number is optional

            idNumber = idNumber.Trim();

            if (idNumber.Length < 3 || idNumber.Length > 20)
                return false;

            // Allow alphanumeric characters, hyphens, and underscores
            return System.Text.RegularExpressions.Regex.IsMatch(idNumber, @"^[a-zA-Z0-9\-_]+$");
        }

        // ==================== CATALOG MANAGEMENT ====================

        private void ShowCatalogView()
        {
            pnlMainContent.Controls.Clear();

            // Title with enhanced styling
            Label titleLabel = new Label
            {
                Text = "📚 LIBRARY CATALOG",
                Font = new Font("Segoe UI", 28F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(500, 60),
                ForeColor = Color.FromArgb(33, 150, 243),
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20, 10, 20, 10)
            };

            // Subtitle
            Label subtitleLabel = new Label
            {
                Text = "Manage your library's book collection with comprehensive cataloging tools",
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                Location = new Point(30, 75),
                Size = new Size(600, 25),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            // Enhanced search and filter panel for catalog
            Panel searchPanel = new Panel
            {
                Location = new Point(30, 110),
                Size = new Size(pnlMainContent.Width - 60, 70),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Add subtle shadow effect and modern styling
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

            // Enhanced search textbox with modern styling
            Panel searchContainer = new Panel
            {
                Location = new Point(20, 15),
                Size = new Size(320, 40),
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.None
            };

            TextBox txtSearchBooks = new TextBox
            {
                Location = new Point(40, 8),
                Size = new Size(260, 24),
                Font = new Font("Segoe UI", 10F),
                Text = "🔍 Search by title, author, or ISBN...",
                ForeColor = Color.Gray
            };
            txtSearchBooks.GotFocus += (s, e) =>
            {
                if (txtSearchBooks.Text == "🔍 Search books...")
                {
                    txtSearchBooks.Text = "";
                    txtSearchBooks.ForeColor = Color.Black;
                }
            };
            txtSearchBooks.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSearchBooks.Text))
                {
                    txtSearchBooks.Text = "🔍 Search books...";
                    txtSearchBooks.ForeColor = Color.Gray;
                }
            };

            // Category filter
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

            // Enhanced action buttons
            Button btnAddBook = new Button
            {
                Text = "➕ Add New Book",
                Location = new Point(searchPanel.Width - 200, 15),
                Size = new Size(110, 40),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAddBook.FlatAppearance.BorderSize = 0;
            btnAddBook.FlatAppearance.BorderColor = Color.FromArgb(56, 142, 60);

            // Add hover effects
            btnAddBook.MouseEnter += (s, e) =>
            {
                btnAddBook.BackColor = Color.FromArgb(56, 142, 60);
                btnAddBook.FlatAppearance.BorderColor = Color.FromArgb(46, 125, 50);
            };
            btnAddBook.MouseLeave += (s, e) =>
            {
                btnAddBook.BackColor = Color.FromArgb(76, 175, 80);
                btnAddBook.FlatAppearance.BorderColor = Color.FromArgb(56, 142, 60);
            };

            Button btnRefreshBooks = new Button
            {
                Text = "🔄 Refresh",
                Location = new Point(searchPanel.Width - 80, 15),
                Size = new Size(50, 40),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRefreshBooks.FlatAppearance.BorderSize = 0;
            btnRefreshBooks.FlatAppearance.BorderColor = Color.FromArgb(117, 117, 117);

            // Enhanced Books DataGridView with modern styling
            DataGridView dgvBooks = new DataGridView
            {
                Location = new Point(30, 190),
                Size = new Size(pnlMainContent.Width - 60, pnlMainContent.Height - 220),
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

            // Modern column header styling
            dgvBooks.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 249, 250),
                ForeColor = Color.FromArgb(33, 37, 41),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                SelectionBackColor = Color.FromArgb(248, 249, 250)
            };

            // Enhanced row styling
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

            // Alternating row colors for better readability
            dgvBooks.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(252, 252, 252),
                ForeColor = Color.FromArgb(33, 37, 41),
                Font = new Font("Segoe UI", 9F),
                SelectionBackColor = Color.FromArgb(33, 150, 243, 20),
                SelectionForeColor = Color.FromArgb(33, 37, 41)
            };

            // Add columns
            dgvBooks.Columns.Add("BookId", "ID");
            dgvBooks.Columns.Add("Title", "Title");
            dgvBooks.Columns.Add("Author", "Author");
            dgvBooks.Columns.Add("ISBN", "ISBN");
            dgvBooks.Columns.Add("Category", "Category");
            dgvBooks.Columns.Add("TotalCopies", "Total");
            dgvBooks.Columns.Add("AvailableCopies", "Available");

            // Add button columns
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

            // Set column widths
            dgvBooks.Columns["BookId"].Width = 60;
            dgvBooks.Columns["Title"].Width = 200;
            dgvBooks.Columns["Author"].Width = 150;
            dgvBooks.Columns["ISBN"].Width = 120;
            dgvBooks.Columns["Category"].Width = 100;
            dgvBooks.Columns["TotalCopies"].Width = 70;
            dgvBooks.Columns["AvailableCopies"].Width = 80;
            // Edit and Delete button columns already have their widths set above

            // Hide BookId column
            dgvBooks.Columns["BookId"].Visible = false;

            // Load books function
            void LoadBooksData()
            {
                try
                {
                    var bookService = new Library_Management_System.Service.BookService();
                    string searchText = txtSearchBooks.Text;
                    if (searchText == "🔍 Search books..." || searchText == "Search books...") searchText = "";
                    string categoryFilter = cmbCategoryFilter.SelectedItem?.ToString() ?? "All Categories";

                    var books = bookService.GetBooks(searchText, categoryFilter);

                    dgvBooks.Rows.Clear();

                    foreach (var book in books)
                    {
                        int rowIndex = dgvBooks.Rows.Add();
                        dgvBooks.Rows[rowIndex].Cells["BookId"].Value = book.BookId;
                        dgvBooks.Rows[rowIndex].Cells["Title"].Value = book.Title;
                        dgvBooks.Rows[rowIndex].Cells["Author"].Value = book.Author;
                        dgvBooks.Rows[rowIndex].Cells["ISBN"].Value = book.ISBN ?? "N/A";
                        dgvBooks.Rows[rowIndex].Cells["Category"].Value = book.Category ?? "Uncategorized";
                        dgvBooks.Rows[rowIndex].Cells["TotalCopies"].Value = book.TotalCopies;
                        dgvBooks.Rows[rowIndex].Cells["AvailableCopies"].Value = book.AvailableCopies;

                        // The button columns will automatically show the button text
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading books: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // Load categories
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

            // Event handlers
            txtSearchBooks.TextChanged += (s, e) =>
            {
                string text = txtSearchBooks.Text;
                if (text != "🔍 Search books..." && text != "Search books...")
                {
                    LoadBooksData();
                }
            };

            cmbCategoryFilter.SelectedIndexChanged += (s, e) => LoadBooksData();
            btnRefreshBooks.Click += (s, e) => { LoadCategories(); LoadBooksData(); };
            btnAddBook.Click += (s, e) => ShowAddBookDialog();

            // DataGridView cell click for button columns
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
                            LoadBooksData();
                            MessageBox.Show("Book deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error deleting book: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            };

            // Add controls to panels
            searchPanel.Controls.AddRange(new Control[] { txtSearchBooks, lblCategoryFilter, cmbCategoryFilter, btnAddBook, btnRefreshBooks });

            pnlMainContent.Controls.AddRange(new Control[] { titleLabel, searchPanel, dgvBooks });

            // Load initial data
            LoadCategories();
            LoadBooksData();
        }

        private void ShowAddBookDialog()
        {
            Form addForm = new Form
            {
                Text = "Add New Book",
                Size = new Size(520, 650),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false
            };

            // Title
            Label titleLabel = new Label
            {
                Text = "Add New Book",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(400, 35),
                ForeColor = Color.FromArgb(40, 40, 40)
            };

            // Form fields
            int startY = 70;
            int fieldHeight = 35;
            int labelWidth = 120;
            int fieldWidth = 330;

            // ISBN
            Label lblISBN = new Label { Text = "ISBN:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtISBN = new TextBox { Location = new Point(160, startY), Size = new Size(fieldWidth, fieldHeight), Font = new Font("Segoe UI", 10F) };

            // Title
            startY += 45;
            Label lblTitle = new Label { Text = "Title:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtTitle = new TextBox { Location = new Point(160, startY), Size = new Size(fieldWidth, fieldHeight), Font = new Font("Segoe UI", 10F) };

            // Author
            startY += 45;
            Label lblAuthor = new Label { Text = "Author:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtAuthor = new TextBox { Location = new Point(160, startY), Size = new Size(fieldWidth, fieldHeight), Font = new Font("Segoe UI", 10F) };

            // Publisher
            startY += 45;
            Label lblPublisher = new Label { Text = "Publisher:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtPublisher = new TextBox { Location = new Point(160, startY), Size = new Size(fieldWidth, fieldHeight), Font = new Font("Segoe UI", 10F) };

            // Publication Year
            startY += 45;
            Label lblYear = new Label { Text = "Publication Year:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            NumericUpDown numYear = new NumericUpDown
            {
                Location = new Point(160, startY),
                Size = new Size(fieldWidth, fieldHeight),
                Font = new Font("Segoe UI", 10F),
                Minimum = 1000,
                Maximum = DateTime.Now.Year,
                Value = DateTime.Now.Year
            };

            // Category
            startY += 45;
            Label lblCategory = new Label { Text = "Category:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            ComboBox cmbCategory = new ComboBox
            {
                Location = new Point(160, startY),
                Size = new Size(fieldWidth, fieldHeight),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDown
            };

            // Total Copies
            startY += 45;
            Label lblCopies = new Label { Text = "Total Copies:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            NumericUpDown numCopies = new NumericUpDown
            {
                Location = new Point(160, startY),
                Size = new Size(fieldWidth, fieldHeight),
                Font = new Font("Segoe UI", 10F),
                Minimum = 1,
                Maximum = 1000,
                Value = 1
            };

            // Description
            startY += 45;
            Label lblDescription = new Label { Text = "Description:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtDescription = new TextBox
            {
                Location = new Point(160, startY),
                Size = new Size(fieldWidth, 80),
                Font = new Font("Segoe UI", 10F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            // Buttons
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
                Text = "Add Book",
                Location = new Point(380, startY + 100),
                Size = new Size(90, 35),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;

            // Load categories
            try
            {
                var bookService = new Library_Management_System.Service.BookService();
                var categories = bookService.GetCategories();
                cmbCategory.Items.AddRange(categories.ToArray());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading categories: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Event handlers
            txtTitle.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtAuthor.Focus(); };
            txtAuthor.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtPublisher.Focus(); };
            txtPublisher.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) numYear.Focus(); };
            numYear.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) cmbCategory.Focus(); };
            cmbCategory.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) numCopies.Focus(); };
            numCopies.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtDescription.Focus(); };
            txtDescription.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter && e.Control) btnSave.PerformClick(); };

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

                try
                {
                    var bookService = new Library_Management_System.Service.BookService();
                    bookService.AddBook(
                        txtISBN.GetActualText().Trim(),
                        txtTitle.GetActualText().Trim(),
                        txtAuthor.GetActualText().Trim(),
                        txtPublisher.GetActualText().Trim(),
                        (int)numYear.Value,
                        cmbCategory.Text.Trim(),
                        (int)numCopies.Value,
                        txtDescription.GetActualText().Trim()
                    );

                    MessageBox.Show("Book added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    addForm.DialogResult = DialogResult.OK;
                    addForm.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding book: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            addForm.AcceptButton = btnSave;
            addForm.CancelButton = btnCancel;

            btnCancel.Click += (s, e) => addForm.Close();

            // Set up placeholders for book form textboxes
            txtISBN.SetPlaceholder("Enter ISBN");
            txtTitle.SetPlaceholder("Enter book title");
            txtAuthor.SetPlaceholder("Enter author name");
            txtPublisher.SetPlaceholder("Enter publisher");
            txtDescription.SetPlaceholder("Enter book description");

            addForm.Controls.AddRange(new Control[] {
                titleLabel, lblISBN, txtISBN, lblTitle, txtTitle, lblAuthor, txtAuthor,
                lblPublisher, txtPublisher, lblYear, numYear, lblCategory, cmbCategory,
                lblCopies, numCopies, lblDescription, txtDescription, btnCancel, btnSave
            });

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

                // Title
                Label titleLabel = new Label
                {
                    Text = "Edit Book",
                    Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                    Location = new Point(30, 20),
                    Size = new Size(400, 35),
                    ForeColor = Color.FromArgb(40, 40, 40)
                };

                // Form fields
                int startY = 70;
                int fieldHeight = 35;
                int labelWidth = 120;
                int fieldWidth = 330;

                // ISBN
                Label lblISBN = new Label { Text = "ISBN:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
                TextBox txtISBN = new TextBox
                {
                    Location = new Point(160, startY),
                    Size = new Size(fieldWidth, fieldHeight),
                    Font = new Font("Segoe UI", 10F),
                    Text = book.ISBN ?? ""
                };

                // Title
                startY += 45;
                Label lblTitle = new Label { Text = "Title:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
                TextBox txtTitle = new TextBox
                {
                    Location = new Point(160, startY),
                    Size = new Size(fieldWidth, fieldHeight),
                    Font = new Font("Segoe UI", 10F),
                    Text = book.Title
                };

                // Author
                startY += 45;
                Label lblAuthor = new Label { Text = "Author:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
                TextBox txtAuthor = new TextBox
                {
                    Location = new Point(160, startY),
                    Size = new Size(fieldWidth, fieldHeight),
                    Font = new Font("Segoe UI", 10F),
                    Text = book.Author
                };

                // Publisher
                startY += 45;
                Label lblPublisher = new Label { Text = "Publisher:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
                TextBox txtPublisher = new TextBox
                {
                    Location = new Point(160, startY),
                    Size = new Size(fieldWidth, fieldHeight),
                    Font = new Font("Segoe UI", 10F),
                    Text = book.Publisher ?? ""
                };

                // Publication Year
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

                // Category
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

                // Total Copies
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

                // Available Copies (read-only display)
                startY += 45;
                Label lblAvailable = new Label
                {
                    Text = $"Available Copies: {book.AvailableCopies}",
                    Location = new Point(30, startY),
                    Size = new Size(300, 25),
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = Color.FromArgb(100, 100, 100)
                };

                // Description
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

                // Buttons
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

                // Load categories
                try
                {
                    var categories = bookService.GetCategories();
                    cmbCategory.Items.AddRange(categories.ToArray());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading categories: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // Event handlers
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
                            (int)numYear.Value,
                            cmbCategory.Text.Trim(),
                            (int)numCopies.Value,
                            txtDescription.Text.Trim()
                        );

                        MessageBox.Show("Book updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        editForm.DialogResult = DialogResult.OK;
                        editForm.Close();
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

        // ==================== CIRCULATION MANAGEMENT ====================

        private void ShowCirculationView()
        {
            pnlMainContent.Controls.Clear();

            // Enhanced title with modern styling
            Label titleLabel = new Label
            {
                Text = "🔄 CIRCULATION MANAGEMENT",
                Font = new Font("Segoe UI", 28F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(500, 60),
                ForeColor = Color.FromArgb(255, 152, 0),
                BackColor = Color.FromArgb(255, 251, 235),
                Padding = new Padding(20, 10, 20, 10)
            };

            // Subtitle
            Label subtitleLabel = new Label
            {
                Text = "Manage book checkouts, returns, and renewals with comprehensive tracking",
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                Location = new Point(30, 75),
                Size = new Size(600, 25),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            // Enhanced search and filter panel
            Panel searchPanel = new Panel
            {
                Location = new Point(30, 110),
                Size = new Size(pnlMainContent.Width - 60, 70),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Add modern styling with subtle shadow
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

            // Enhanced search container
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
                Text = "🔍 Search by member, book, or ID...",
                ForeColor = Color.Gray
            };
            txtSearchBorrowings.GotFocus += (s, e) =>
            {
                if (txtSearchBorrowings.Text == "🔍 Search borrowings...")
                {
                    txtSearchBorrowings.Text = "";
                    txtSearchBorrowings.ForeColor = Color.Black;
                }
            };
            txtSearchBorrowings.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSearchBorrowings.Text))
                {
                    txtSearchBorrowings.Text = "🔍 Search borrowings...";
                    txtSearchBorrowings.ForeColor = Color.Gray;
                }
            };

            // Status filter
            Label lblStatusFilter = new Label
            {
                Text = "Status:",
                Location = new Point(340, 18),
                Size = new Size(60, 25),
                Font = new Font("Segoe UI", 10F)
            };

            ComboBox cmbStatusFilter = new ComboBox
            {
                Location = new Point(400, 15),
                Size = new Size(120, 30),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStatusFilter.Items.AddRange(new[] { "All Status", "Active", "Returned", "Overdue" });
            cmbStatusFilter.SelectedIndex = 0;

            // Add search icon to container
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

            // Enhanced action buttons
            Button btnCheckout = new Button
            {
                Text = "📖 Checkout Book",
                Location = new Point(searchPanel.Width - 240, 15),
                Size = new Size(100, 40),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCheckout.FlatAppearance.BorderSize = 0;
            btnCheckout.FlatAppearance.BorderColor = Color.FromArgb(56, 142, 60);

            // Add hover effects
            btnCheckout.MouseEnter += (s, e) =>
            {
                btnCheckout.BackColor = Color.FromArgb(56, 142, 60);
                btnCheckout.FlatAppearance.BorderColor = Color.FromArgb(46, 125, 50);
            };
            btnCheckout.MouseLeave += (s, e) =>
            {
                btnCheckout.BackColor = Color.FromArgb(76, 175, 80);
                btnCheckout.FlatAppearance.BorderColor = Color.FromArgb(56, 142, 60);
            };

            Button btnReturn = new Button
            {
                Text = "↩️ Return Book",
                Location = new Point(searchPanel.Width - 130, 15),
                Size = new Size(100, 40),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnReturn.FlatAppearance.BorderSize = 0;
            btnReturn.FlatAppearance.BorderColor = Color.FromArgb(245, 127, 23);

            btnReturn.MouseEnter += (s, e) =>
            {
                btnReturn.BackColor = Color.FromArgb(245, 127, 23);
                btnReturn.FlatAppearance.BorderColor = Color.FromArgb(230, 81, 0);
            };
            btnReturn.MouseLeave += (s, e) =>
            {
                btnReturn.BackColor = Color.FromArgb(255, 152, 0);
                btnReturn.FlatAppearance.BorderColor = Color.FromArgb(245, 127, 23);
            };

            Button btnRefreshCirculation = new Button
            {
                Text = "🔄 Refresh",
                Location = new Point(searchPanel.Width - 80, 15),
                Size = new Size(50, 40),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRefreshCirculation.FlatAppearance.BorderSize = 0;
            btnRefreshCirculation.FlatAppearance.BorderColor = Color.FromArgb(117, 117, 117);

            // Borrowings DataGridView
            DataGridView dgvBorrowings = new DataGridView
            {
                Location = new Point(30, 170),
                Size = new Size(pnlMainContent.Width - 60, pnlMainContent.Height - 200),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Configure DataGridView
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

            // Add columns
            dgvBorrowings.Columns.Add("BorrowingId", "ID");
            dgvBorrowings.Columns.Add("MemberName", "Member");
            dgvBorrowings.Columns.Add("BookTitle", "Book Title");
            dgvBorrowings.Columns.Add("ISBN", "ISBN");
            dgvBorrowings.Columns.Add("BorrowDate", "Borrow Date");
            dgvBorrowings.Columns.Add("DueDate", "Due Date");
            dgvBorrowings.Columns.Add("Status", "Status");
            dgvBorrowings.Columns.Add("Actions", "Actions");

            // Set column widths
            dgvBorrowings.Columns["BorrowingId"].Width = 80;
            dgvBorrowings.Columns["MemberName"].Width = 150;
            dgvBorrowings.Columns["BookTitle"].Width = 200;
            dgvBorrowings.Columns["ISBN"].Width = 100;
            dgvBorrowings.Columns["BorrowDate"].Width = 100;
            dgvBorrowings.Columns["DueDate"].Width = 100;
            dgvBorrowings.Columns["Status"].Width = 80;
            dgvBorrowings.Columns["Actions"].Width = 120;

            // Hide BorrowingId column
            dgvBorrowings.Columns["BorrowingId"].Visible = false;

            // Load borrowings function
            void LoadBorrowingsData()
            {
                try
                {
                    var circulationService = new Library_Management_System.Service.CirculationService();
                    string searchText = txtSearchBorrowings.Text;
                    if (searchText == "🔍 Search borrowings..." || searchText == "Search borrowings...") searchText = "";
                    string statusFilter = cmbStatusFilter.SelectedItem?.ToString() ?? "All Status";

                    var borrowings = circulationService.GetBorrowings(searchText, statusFilter);

                    dgvBorrowings.Rows.Clear();

                    foreach (var borrowing in borrowings)
                    {
                        int rowIndex = dgvBorrowings.Rows.Add();
                        dgvBorrowings.Rows[rowIndex].Cells["BorrowingId"].Value = borrowing.BorrowingId;
                        dgvBorrowings.Rows[rowIndex].Cells["MemberName"].Value = borrowing.MemberName;
                        dgvBorrowings.Rows[rowIndex].Cells["BookTitle"].Value = borrowing.BookTitle;
                        dgvBorrowings.Rows[rowIndex].Cells["ISBN"].Value = borrowing.BookISBN ?? "N/A";
                        dgvBorrowings.Rows[rowIndex].Cells["BorrowDate"].Value = borrowing.BorrowDate.ToString("yyyy-MM-dd");
                        dgvBorrowings.Rows[rowIndex].Cells["DueDate"].Value = borrowing.DueDate.ToString("yyyy-MM-dd");

                        // Status with color coding
                        var statusCell = dgvBorrowings.Rows[rowIndex].Cells["Status"];
                        if (borrowing.ReturnDate.HasValue)
                        {
                            statusCell.Value = "Returned";
                            statusCell.Style.ForeColor = Color.FromArgb(76, 175, 80); // Green
                        }
                        else if (DateTime.Now > borrowing.DueDate)
                        {
                            statusCell.Value = "Overdue";
                            statusCell.Style.ForeColor = Color.FromArgb(244, 67, 54); // Red
                        }
                        else
                        {
                            statusCell.Value = "Active";
                            statusCell.Style.ForeColor = Color.FromArgb(33, 150, 243); // Blue
                        }

                        // Create action buttons
                        DataGridViewButtonCell actionCell = new DataGridViewButtonCell();

                        if (borrowing.ReturnDate.HasValue)
                        {
                            actionCell.Value = "View";
                            actionCell.Style = new DataGridViewCellStyle
                            {
                                BackColor = Color.FromArgb(158, 158, 158),
                                ForeColor = Color.White,
                                Font = new Font("Segoe UI", 8F)
                            };
                        }
                        else
                        {
                            actionCell.Value = "Return";
                            actionCell.Style = new DataGridViewCellStyle
                            {
                                BackColor = Color.FromArgb(255, 152, 0),
                                ForeColor = Color.White,
                                Font = new Font("Segoe UI", 8F)
                            };
                        }

                        dgvBorrowings.Rows[rowIndex].Cells["Actions"] = actionCell;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading borrowings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // Event handlers
            txtSearchBorrowings.TextChanged += (s, e) =>
            {
                string text = txtSearchBorrowings.Text;
                if (text != "🔍 Search borrowings..." && text != "Search borrowings...")
                {
                    LoadBorrowingsData();
                }
            };

            cmbStatusFilter.SelectedIndexChanged += (s, e) => LoadBorrowingsData();
            btnRefreshCirculation.Click += (s, e) => LoadBorrowingsData();
            btnCheckout.Click += (s, e) => ShowCheckoutDialog();
            btnReturn.Click += (s, e) => ShowReturnDialog();

            // DataGridView cell click for actions
            dgvBorrowings.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

                string columnName = dgvBorrowings.Columns[e.ColumnIndex].Name;
                string borrowingId = dgvBorrowings.Rows[e.RowIndex].Cells["BorrowingId"].Value.ToString();

                if (columnName == "Actions")
                {
                    var borrowing = new Library_Management_System.Service.CirculationService().GetBorrowingById(borrowingId);
                    if (borrowing != null)
                    {
                        if (borrowing.ReturnDate.HasValue)
                        {
                            // View details
                            ShowBorrowingDetailsDialog(borrowing);
                        }
                        else
                        {
                            // Return book
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
                }
            };

            // Add controls to panels
            searchPanel.Controls.AddRange(new Control[] { txtSearchBorrowings, lblStatusFilter, cmbStatusFilter, btnCheckout, btnReturn, btnRefreshCirculation });

            pnlMainContent.Controls.AddRange(new Control[] { titleLabel, searchPanel, dgvBorrowings });

            // Load initial data
            LoadBorrowingsData();
        }

        private void ShowCheckoutDialog()
        {
            Form checkoutForm = new Form
            {
                Text = "Book Checkout",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false
            };

            // Title
            Label titleLabel = new Label
            {
                Text = "Checkout Book",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(400, 35),
                ForeColor = Color.FromArgb(40, 40, 40)
            };

            // Member ID
            int startY = 70;
            Label lblMemberId = new Label { Text = "Member ID:", Location = new Point(30, startY), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtMemberId = new TextBox { Location = new Point(140, startY), Size = new Size(300, 30), Font = new Font("Segoe UI", 10F) };

            // Book ID
            startY += 50;
            Label lblBookId = new Label { Text = "Book ID:", Location = new Point(30, startY), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtBookId = new TextBox { Location = new Point(140, startY), Size = new Size(300, 30), Font = new Font("Segoe UI", 10F) };

            // Loan period
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

            // Due date display
            Label lblDueDate = new Label
            {
                Text = $"Due Date: {DateTime.Now.AddDays(14):yyyy-MM-dd}",
                Location = new Point(280, startY),
                Size = new Size(160, 25),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            // Buttons
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

            // Update due date when loan period changes
            numLoanPeriod.ValueChanged += (s, e) =>
            {
                lblDueDate.Text = $"Due Date: {DateTime.Now.AddDays((int)numLoanPeriod.Value):yyyy-MM-dd}";
            };

            // Event handlers
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
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error checking out book: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            checkoutForm.AcceptButton = btnCheckout;
            checkoutForm.CancelButton = btnCancel;

            btnCancel.Click += (s, e) => checkoutForm.Close();

            // Set up placeholders for circulation form textboxes
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
                Size = new Size(400, 250),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false
            };

            // Title
            Label titleLabel = new Label
            {
                Text = "Return Book",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(300, 35),
                ForeColor = Color.FromArgb(40, 40, 40)
            };

            // Borrowing ID
            Label lblBorrowingId = new Label { Text = "Borrowing ID:", Location = new Point(30, 80), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtBorrowingId = new TextBox { Location = new Point(140, 75), Size = new Size(200, 30), Font = new Font("Segoe UI", 10F) };

            // Buttons
            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(150, 150),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                DialogResult = DialogResult.Cancel
            };

            Button btnReturn = new Button
            {
                Text = "Return",
                Location = new Point(240, 150),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnReturn.FlatAppearance.BorderSize = 0;

            // Event handlers
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
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error returning book: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            returnForm.AcceptButton = btnReturn;
            returnForm.CancelButton = btnCancel;

            btnCancel.Click += (s, e) => returnForm.Close();

            // Set up placeholder for return book form
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
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false
            };

            // Title
            Label titleLabel = new Label
            {
                Text = "Borrowing Details",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(400, 35),
                ForeColor = Color.FromArgb(40, 40, 40)
            };

            int startY = 70;
            int labelWidth = 120;
            int fieldWidth = 300;

            // Borrowing ID
            Label lblBorrowingId = new Label { Text = "Borrowing ID:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblBorrowingIdValue = new Label { Text = borrowing.BorrowingId, Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };

            // Member
            startY += 35;
            Label lblMember = new Label { Text = "Member:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblMemberValue = new Label { Text = borrowing.MemberName, Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F) };

            // Book
            startY += 35;
            Label lblBook = new Label { Text = "Book:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblBookValue = new Label { Text = borrowing.BookTitle, Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F) };

            // ISBN
            startY += 35;
            Label lblISBN = new Label { Text = "ISBN:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblISBNValue = new Label { Text = borrowing.BookISBN ?? "N/A", Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F) };

            // Borrow Date
            startY += 35;
            Label lblBorrowDate = new Label { Text = "Borrow Date:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblBorrowDateValue = new Label { Text = borrowing.BorrowDate.ToString("yyyy-MM-dd"), Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F) };

            // Due Date
            startY += 35;
            Label lblDueDate = new Label { Text = "Due Date:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblDueDateValue = new Label { Text = borrowing.DueDate.ToString("yyyy-MM-dd"), Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F) };

            // Return Date
            startY += 35;
            Label lblReturnDate = new Label { Text = "Return Date:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblReturnDateValue = new Label
            {
                Text = borrowing.ReturnDate?.ToString("yyyy-MM-dd") ?? "Not returned",
                Location = new Point(160, startY),
                Size = new Size(fieldWidth, 25),
                Font = new Font("Segoe UI", 10F)
            };

            // Fine Amount
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

            // Close button
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

        // ==================== FINES MANAGEMENT ====================

        private void ShowFinesView()
        {
            pnlMainContent.Controls.Clear();

            // Enhanced title with financial theme
            Label titleLabel = new Label
            {
                Text = "💰 FINES MANAGEMENT",
                Font = new Font("Segoe UI", 28F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(500, 60),
                ForeColor = Color.FromArgb(211, 47, 47),
                BackColor = Color.FromArgb(255, 235, 238),
                Padding = new Padding(20, 10, 20, 10)
            };

            // Subtitle
            Label subtitleLabel = new Label
            {
                Text = "Track, manage, and process library fines with automated calculations",
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                Location = new Point(30, 75),
                Size = new Size(600, 25),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            // Enhanced summary panel with financial dashboard styling
            Panel summaryPanel = new Panel
            {
                Location = new Point(30, 110),
                Size = new Size(pnlMainContent.Width - 60, 90),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Add modern styling with subtle shadow
            summaryPanel.Paint += (s, e) =>
            {
                using (var shadowBrush = new SolidBrush(Color.FromArgb(15, 0, 0, 0)))
                {
                    e.Graphics.FillRectangle(shadowBrush, 3, 3, summaryPanel.Width - 3, summaryPanel.Height - 3);
                }
                using (var bgBrush = new SolidBrush(Color.FromArgb(250, 250, 250)))
                {
                    e.Graphics.FillRectangle(bgBrush, 0, 0, summaryPanel.Width, summaryPanel.Height);
                }
                using (var borderPen = new Pen(Color.FromArgb(220, 220, 220), 1))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, summaryPanel.Width - 1, summaryPanel.Height - 1);
                }
            };

            // Enhanced summary labels with better visual hierarchy
            Label lblTotalFines = new Label
            {
                Text = "💵 Total Fines: $0.00",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                Location = new Point(30, 15),
                Size = new Size(250, 35),
                ForeColor = Color.FromArgb(211, 47, 47),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Create summary boxes for better visual organization
            Panel unpaidBox = new Panel
            {
                Location = new Point(300, 15),
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(255, 235, 238),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblUnpaidFines = new Label
            {
                Text = "❌ Unpaid: $0.00",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(10, 5),
                Size = new Size(130, 25),
                ForeColor = Color.FromArgb(211, 47, 47),
                TextAlign = ContentAlignment.MiddleLeft
            };
            unpaidBox.Controls.Add(lblUnpaidFines);

            Panel paidBox = new Panel
            {
                Location = new Point(470, 15),
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(232, 245, 233),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblPaidFines = new Label
            {
                Text = "✅ Paid: $0.00",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(10, 5),
                Size = new Size(130, 25),
                ForeColor = Color.FromArgb(56, 142, 60),
                TextAlign = ContentAlignment.MiddleLeft
            };
            paidBox.Controls.Add(lblPaidFines);

            // Search and filter panel
            Panel searchPanel = new Panel
            {
                Location = new Point(30, 180),
                Size = new Size(pnlMainContent.Width - 60, 60),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Search textbox
            TextBox txtSearchFines = new TextBox
            {
                Location = new Point(20, 15),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 10F),
                Text = "🔍 Search fines...",
                ForeColor = Color.Gray
            };
            txtSearchFines.GotFocus += (s, e) =>
            {
                if (txtSearchFines.Text == "🔍 Search fines...")
                {
                    txtSearchFines.Text = "";
                    txtSearchFines.ForeColor = Color.Black;
                }
            };
            txtSearchFines.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSearchFines.Text))
                {
                    txtSearchFines.Text = "🔍 Search fines...";
                    txtSearchFines.ForeColor = Color.Gray;
                }
            };

            // Status filter
            Label lblStatusFilter = new Label
            {
                Text = "Status:",
                Location = new Point(340, 18),
                Size = new Size(60, 25),
                Font = new Font("Segoe UI", 10F)
            };

            ComboBox cmbStatusFilter = new ComboBox
            {
                Location = new Point(400, 15),
                Size = new Size(120, 30),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStatusFilter.Items.AddRange(new[] { "All Status", "Unpaid", "Paid", "Waived" });
            cmbStatusFilter.SelectedIndex = 0;

            // Buttons
            Button btnProcessPayment = new Button
            {
                Text = "💳 Process Payment",
                Location = new Point(searchPanel.Width - 180, 10),
                Size = new Size(80, 40),
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnProcessPayment.FlatAppearance.BorderSize = 0;

            Button btnWaiveFine = new Button
            {
                Text = "🆓 Waive",
                Location = new Point(searchPanel.Width - 90, 10),
                Size = new Size(80, 40),
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnWaiveFine.FlatAppearance.BorderSize = 0;

            // Fines DataGridView
            DataGridView dgvFines = new DataGridView
            {
                Location = new Point(30, 250),
                Size = new Size(pnlMainContent.Width - 60, pnlMainContent.Height - 280),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Configure DataGridView
            dgvFines.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(40, 40, 40),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter
            };

            dgvFines.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(40, 40, 40),
                Font = new Font("Segoe UI", 9F),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                SelectionBackColor = Color.FromArgb(128, 0, 0),
                SelectionForeColor = Color.White
            };

            // Add columns
            dgvFines.Columns.Add("FineId", "ID");
            dgvFines.Columns.Add("MemberName", "Member");
            dgvFines.Columns.Add("BookTitle", "Book");
            dgvFines.Columns.Add("Amount", "Amount");
            dgvFines.Columns.Add("Reason", "Reason");
            dgvFines.Columns.Add("Status", "Status");
            dgvFines.Columns.Add("CreatedDate", "Created");
            dgvFines.Columns.Add("Actions", "Actions");

            // Set column widths
            dgvFines.Columns["FineId"].Width = 80;
            dgvFines.Columns["MemberName"].Width = 150;
            dgvFines.Columns["BookTitle"].Width = 200;
            dgvFines.Columns["Amount"].Width = 80;
            dgvFines.Columns["Reason"].Width = 150;
            dgvFines.Columns["Status"].Width = 80;
            dgvFines.Columns["CreatedDate"].Width = 100;
            dgvFines.Columns["Actions"].Width = 120;

            // Hide FineId column
            dgvFines.Columns["FineId"].Visible = false;

            // Load fines function
            void LoadFinesData()
            {
                try
                {
                    var finesService = new Library_Management_System.Service.FinesService();
                    string searchText = txtSearchFines.Text;
                    if (searchText == "🔍 Search fines..." || searchText == "Search fines...") searchText = "";
                    string statusFilter = cmbStatusFilter.SelectedItem?.ToString() ?? "All Status";

                    var fines = finesService.GetFines(searchText, statusFilter);

                    // Update summary
                    decimal totalFines = finesService.GetTotalFines();
                    decimal unpaidFines = finesService.GetTotalFines(null, "Unpaid");
                    decimal paidFines = finesService.GetTotalFines(null, "Paid");

                    lblTotalFines.Text = $"Total Fines: ${totalFines:F2}";
                    lblUnpaidFines.Text = $"Unpaid: ${unpaidFines:F2}";
                    lblPaidFines.Text = $"Paid: ${paidFines:F2}";

                    dgvFines.Rows.Clear();

                    foreach (var fine in fines)
                    {
                        int rowIndex = dgvFines.Rows.Add();
                        dgvFines.Rows[rowIndex].Cells["FineId"].Value = fine.FineId;
                        dgvFines.Rows[rowIndex].Cells["MemberName"].Value = fine.MemberName;
                        dgvFines.Rows[rowIndex].Cells["BookTitle"].Value = fine.BookTitle;
                        dgvFines.Rows[rowIndex].Cells["Amount"].Value = $"${fine.Amount:F2}";
                        dgvFines.Rows[rowIndex].Cells["Reason"].Value = fine.Reason ?? "N/A";
                        dgvFines.Rows[rowIndex].Cells["CreatedDate"].Value = fine.CreatedDate.ToString("yyyy-MM-dd");

                        // Status with color coding
                        var statusCell = dgvFines.Rows[rowIndex].Cells["Status"];
                        switch (fine.Status)
                        {
                            case "Unpaid":
                                statusCell.Value = "Unpaid";
                                statusCell.Style.ForeColor = Color.FromArgb(244, 67, 54); // Red
                                break;
                            case "Paid":
                                statusCell.Value = "Paid";
                                statusCell.Style.ForeColor = Color.FromArgb(76, 175, 80); // Green
                                break;
                            case "Waived":
                                statusCell.Value = "Waived";
                                statusCell.Style.ForeColor = Color.FromArgb(255, 152, 0); // Orange
                                break;
                            default:
                                statusCell.Value = fine.Status;
                                break;
                        }

                        // Create action buttons
                        DataGridViewButtonCell actionCell = new DataGridViewButtonCell();

                        if (fine.Status == "Unpaid")
                        {
                            actionCell.Value = "Pay";
                            actionCell.Style = new DataGridViewCellStyle
                            {
                                BackColor = Color.FromArgb(76, 175, 80),
                                ForeColor = Color.White,
                                Font = new Font("Segoe UI", 8F)
                            };
                        }
                        else
                        {
                            actionCell.Value = "View";
                            actionCell.Style = new DataGridViewCellStyle
                            {
                                BackColor = Color.FromArgb(158, 158, 158),
                                ForeColor = Color.White,
                                Font = new Font("Segoe UI", 8F)
                            };
                        }

                        dgvFines.Rows[rowIndex].Cells["Actions"] = actionCell;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading fines: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // Event handlers
            txtSearchFines.TextChanged += (s, e) =>
            {
                string text = txtSearchFines.Text;
                if (text != "🔍 Search fines..." && text != "Search fines...")
                {
                    LoadFinesData();
                }
            };

            cmbStatusFilter.SelectedIndexChanged += (s, e) => LoadFinesData();
            btnProcessPayment.Click += (s, e) => ShowPaymentDialog();
            btnWaiveFine.Click += (s, e) => ShowWaiveDialog();

            // DataGridView cell click for actions
            dgvFines.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

                string columnName = dgvFines.Columns[e.ColumnIndex].Name;
                string fineId = dgvFines.Rows[e.RowIndex].Cells["FineId"].Value.ToString();

                if (columnName == "Actions")
                {
                    var fine = new Library_Management_System.Service.FinesService().GetFineById(fineId);
                    if (fine != null)
                    {
                        if (fine.Status == "Unpaid")
                        {
                            // Process payment
                            if (MessageBox.Show($"Process payment of ${fine.Amount:F2} for {fine.MemberName}?",
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
                        else
                        {
                            // View details
                            ShowFineDetailsDialog(fine);
                        }
                    }
                }
            };

            // Add controls to panels
            summaryPanel.Controls.AddRange(new Control[] { lblTotalFines, lblUnpaidFines, lblPaidFines });
            searchPanel.Controls.AddRange(new Control[] { txtSearchFines, lblStatusFilter, cmbStatusFilter, btnProcessPayment, btnWaiveFine });

            pnlMainContent.Controls.AddRange(new Control[] { titleLabel, summaryPanel, searchPanel, dgvFines });

            // Load initial data
            LoadFinesData();
        }

        private void ShowPaymentDialog()
        {
            Form paymentForm = new Form
            {
                Text = "Process Fine Payment",
                Size = new Size(400, 250),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false
            };

            // Title
            Label titleLabel = new Label
            {
                Text = "Process Payment",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(300, 35),
                ForeColor = Color.FromArgb(40, 40, 40)
            };

            // Fine ID
            Label lblFineId = new Label { Text = "Fine ID:", Location = new Point(30, 80), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtFineId = new TextBox { Location = new Point(140, 75), Size = new Size(200, 30), Font = new Font("Segoe UI", 10F) };

            // Buttons
            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(150, 150),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                DialogResult = DialogResult.Cancel
            };

            Button btnProcess = new Button
            {
                Text = "Process",
                Location = new Point(240, 150),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnProcess.FlatAppearance.BorderSize = 0;

            // Event handlers
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

            // Set up placeholder for fine payment form
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
                Size = new Size(450, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false
            };

            // Title
            Label titleLabel = new Label
            {
                Text = "Waive Fine",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(300, 35),
                ForeColor = Color.FromArgb(40, 40, 40)
            };

            // Fine ID
            Label lblFineId = new Label { Text = "Fine ID:", Location = new Point(30, 70), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtFineId = new TextBox { Location = new Point(140, 65), Size = new Size(250, 30), Font = new Font("Segoe UI", 10F) };

            // Reason
            Label lblReason = new Label { Text = "Reason:", Location = new Point(30, 110), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtReason = new TextBox
            {
                Location = new Point(140, 105),
                Size = new Size(250, 60),
                Font = new Font("Segoe UI", 10F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            // Buttons
            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(250, 200),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                DialogResult = DialogResult.Cancel
            };

            Button btnWaive = new Button
            {
                Text = "Waive",
                Location = new Point(340, 200),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnWaive.FlatAppearance.BorderSize = 0;

            // Event handlers
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

            // Set up placeholders for fine waiver form
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
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false
            };

            // Title
            Label titleLabel = new Label
            {
                Text = "Fine Details",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(400, 35),
                ForeColor = Color.FromArgb(40, 40, 40)
            };

            int startY = 70;
            int labelWidth = 120;
            int fieldWidth = 300;

            // Fine ID
            Label lblFineId = new Label { Text = "Fine ID:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblFineIdValue = new Label { Text = fine.FineId, Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };

            // Member
            startY += 35;
            Label lblMember = new Label { Text = "Member:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblMemberValue = new Label { Text = fine.MemberName, Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F) };

            // Book
            startY += 35;
            Label lblBook = new Label { Text = "Book:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblBookValue = new Label { Text = fine.BookTitle, Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F) };

            // Amount
            startY += 35;
            Label lblAmount = new Label { Text = "Amount:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblAmountValue = new Label { Text = $"${fine.Amount:F2}", Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(244, 67, 54) };

            // Reason
            startY += 35;
            Label lblReason = new Label { Text = "Reason:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblReasonValue = new Label { Text = fine.Reason ?? "N/A", Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F) };

            // Status
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

            // Created Date
            startY += 35;
            Label lblCreated = new Label { Text = "Created:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblCreatedValue = new Label { Text = fine.CreatedDate.ToString("yyyy-MM-dd HH:mm"), Location = new Point(160, startY), Size = new Size(fieldWidth, 25), Font = new Font("Segoe UI", 10F) };

            // Paid Date
            startY += 35;
            Label lblPaid = new Label { Text = "Paid Date:", Location = new Point(30, startY), Size = new Size(labelWidth, 25), Font = new Font("Segoe UI", 10F) };
            Label lblPaidValue = new Label
            {
                Text = fine.PaidDate?.ToString("yyyy-MM-dd HH:mm") ?? "Not paid",
                Location = new Point(160, startY),
                Size = new Size(fieldWidth, 25),
                Font = new Font("Segoe UI", 10F)
            };

            // Close button
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

        // ==================== INVENTORY MANAGEMENT ====================

        private void ShowInventoryView()
        {
            pnlMainContent.Controls.Clear();

            // Enhanced title with inventory theme
            Label titleLabel = new Label
            {
                Text = "📦 INVENTORY MANAGEMENT",
                Font = new Font("Segoe UI", 28F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(500, 60),
                ForeColor = Color.FromArgb(156, 39, 176),
                BackColor = Color.FromArgb(248, 237, 251),
                Padding = new Padding(20, 10, 20, 10)
            };

            // Subtitle
            Label subtitleLabel = new Label
            {
                Text = "Monitor stock levels, track book availability, and manage inventory efficiently",
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                Location = new Point(30, 75),
                Size = new Size(650, 25),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            // Enhanced summary panel with inventory dashboard styling
            Panel summaryPanel = new Panel
            {
                Location = new Point(30, 110),
                Size = new Size(pnlMainContent.Width - 60, 90),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Add modern styling with subtle shadow
            summaryPanel.Paint += (s, e) =>
            {
                using (var shadowBrush = new SolidBrush(Color.FromArgb(15, 0, 0, 0)))
                {
                    e.Graphics.FillRectangle(shadowBrush, 3, 3, summaryPanel.Width - 3, summaryPanel.Height - 3);
                }
                using (var bgBrush = new SolidBrush(Color.FromArgb(250, 250, 250)))
                {
                    e.Graphics.FillRectangle(bgBrush, 0, 0, summaryPanel.Width, summaryPanel.Height);
                }
                using (var borderPen = new Pen(Color.FromArgb(220, 220, 220), 1))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, summaryPanel.Width - 1, summaryPanel.Height - 1);
                }
            };

            // Enhanced summary labels with visual indicators
            Label lblTotalCopies = new Label
            {
                Text = "📚 Total Copies: 0",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                Location = new Point(30, 15),
                Size = new Size(160, 30),
                ForeColor = Color.FromArgb(33, 150, 243),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label lblAvailableCopies = new Label
            {
                Text = "✅ Available: 0",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(210, 15),
                Size = new Size(130, 30),
                ForeColor = Color.FromArgb(76, 175, 80),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label lblBorrowedCopies = new Label
            {
                Text = "🔄 Borrowed: 0",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(360, 15),
                Size = new Size(130, 30),
                ForeColor = Color.FromArgb(255, 152, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label lblLowStock = new Label
            {
                Text = "⚠️ Low Stock: 0",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(510, 15),
                Size = new Size(130, 30),
                ForeColor = Color.FromArgb(255, 152, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label lblOutOfStock = new Label
            {
                Text = "❌ Out of Stock: 0",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(30, 50),
                Size = new Size(160, 30),
                ForeColor = Color.FromArgb(244, 67, 54),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Enhanced alerts panel with better styling
            Panel alertsPanel = new Panel
            {
                Location = new Point(30, 210),
                Size = new Size(pnlMainContent.Width - 60, 110),
                BackColor = Color.FromArgb(255, 248, 220),
                BorderStyle = BorderStyle.None
            };

            // Add modern styling
            alertsPanel.Paint += (s, e) =>
            {
                using (var shadowBrush = new SolidBrush(Color.FromArgb(15, 0, 0, 0)))
                {
                    e.Graphics.FillRectangle(shadowBrush, 3, 3, alertsPanel.Width - 3, alertsPanel.Height - 3);
                }
                using (var bgBrush = new SolidBrush(Color.FromArgb(255, 251, 235)))
                {
                    e.Graphics.FillRectangle(bgBrush, 0, 0, alertsPanel.Width, alertsPanel.Height);
                }
                using (var borderPen = new Pen(Color.FromArgb(255, 193, 7, 50), 1))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, alertsPanel.Width - 1, alertsPanel.Height - 1);
                }
            };

            Label lblAlerts = new Label
            {
                Text = "🚨 INVENTORY ALERTS",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                Location = new Point(25, 15),
                Size = new Size(250, 30),
                ForeColor = Color.FromArgb(255, 152, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };

            ListBox lstAlerts = new ListBox
            {
                Location = new Point(25, 50),
                Size = new Size(alertsPanel.Width - 50, 45),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(255, 251, 235),
                BorderStyle = BorderStyle.None,
                ForeColor = Color.FromArgb(69, 90, 100)
            };

            // Search and filter panel
            Panel searchPanel = new Panel
            {
                Location = new Point(30, 290),
                Size = new Size(pnlMainContent.Width - 60, 60),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Search textbox
            TextBox txtSearchInventory = new TextBox
            {
                Location = new Point(20, 15),
                Size = new Size(250, 30),
                Font = new Font("Segoe UI", 10F),
                Text = "🔍 Search inventory...",
                ForeColor = Color.Gray
            };
            txtSearchInventory.GotFocus += (s, e) =>
            {
                if (txtSearchInventory.Text == "🔍 Search inventory...")
                {
                    txtSearchInventory.Text = "";
                    txtSearchInventory.ForeColor = Color.Black;
                }
            };
            txtSearchInventory.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSearchInventory.Text))
                {
                    txtSearchInventory.Text = "🔍 Search inventory...";
                    txtSearchInventory.ForeColor = Color.Gray;
                }
            };

            // Category filter
            Label lblCategoryFilter = new Label
            {
                Text = "Category:",
                Location = new Point(290, 18),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 10F)
            };

            ComboBox cmbCategoryFilter = new ComboBox
            {
                Location = new Point(370, 15),
                Size = new Size(120, 30),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Status filter
            Label lblStatusFilter = new Label
            {
                Text = "Status:",
                Location = new Point(510, 18),
                Size = new Size(60, 25),
                Font = new Font("Segoe UI", 10F)
            };

            ComboBox cmbStatusFilter = new ComboBox
            {
                Location = new Point(570, 15),
                Size = new Size(120, 30),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStatusFilter.Items.AddRange(new[] { "All Status", "Available", "Low Stock", "Out of Stock" });
            cmbStatusFilter.SelectedIndex = 0;

            // Buttons
            Button btnUpdateStock = new Button
            {
                Text = "📊 Update Stock",
                Location = new Point(searchPanel.Width - 160, 10),
                Size = new Size(80, 40),
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnUpdateStock.FlatAppearance.BorderSize = 0;

            Button btnAudit = new Button
            {
                Text = "🔍 Audit",
                Location = new Point(searchPanel.Width - 70, 10),
                Size = new Size(80, 40),
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAudit.FlatAppearance.BorderSize = 0;

            // Inventory DataGridView
            DataGridView dgvInventory = new DataGridView
            {
                Location = new Point(30, 360),
                Size = new Size(pnlMainContent.Width - 60, pnlMainContent.Height - 390),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Configure DataGridView
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

            // Add columns
            dgvInventory.Columns.Add("BookId", "ID");
            dgvInventory.Columns.Add("Title", "Title");
            dgvInventory.Columns.Add("Author", "Author");
            dgvInventory.Columns.Add("ISBN", "ISBN");
            dgvInventory.Columns.Add("Category", "Category");
            dgvInventory.Columns.Add("TotalCopies", "Total");
            dgvInventory.Columns.Add("AvailableCopies", "Available");
            dgvInventory.Columns.Add("BorrowedCopies", "Borrowed");
            dgvInventory.Columns.Add("Status", "Status");

            // Set column widths
            dgvInventory.Columns["BookId"].Width = 60;
            dgvInventory.Columns["Title"].Width = 200;
            dgvInventory.Columns["Author"].Width = 120;
            dgvInventory.Columns["ISBN"].Width = 100;
            dgvInventory.Columns["Category"].Width = 100;
            dgvInventory.Columns["TotalCopies"].Width = 70;
            dgvInventory.Columns["AvailableCopies"].Width = 80;
            dgvInventory.Columns["BorrowedCopies"].Width = 70;
            dgvInventory.Columns["Status"].Width = 80;

            // Hide BookId column
            dgvInventory.Columns["BookId"].Visible = false;

            // Load inventory function
            void LoadInventoryData()
            {
                try
                {
                    var inventoryService = new Library_Management_System.Service.InventoryService();
                    string searchText = txtSearchInventory.Text;
                    if (searchText == "🔍 Search inventory..." || searchText == "Search inventory...") searchText = "";
                    string categoryFilter = cmbCategoryFilter.SelectedItem?.ToString() ?? "All Categories";
                    string statusFilter = cmbStatusFilter.SelectedItem?.ToString() ?? "All Status";

                    var inventory = inventoryService.GetInventory(searchText, categoryFilter, statusFilter);

                    // Update summary
                    var summary = inventoryService.GetInventorySummary();
                    lblTotalCopies.Text = $"Total Copies: {(summary.ContainsKey("TotalCopies") ? summary["TotalCopies"] : 0)}";
                    lblAvailableCopies.Text = $"Available: {(summary.ContainsKey("AvailableCopies") ? summary["AvailableCopies"] : 0)}";
                    lblBorrowedCopies.Text = $"Borrowed: {(summary.ContainsKey("BorrowedCopies") ? summary["BorrowedCopies"] : 0)}";
                    lblLowStock.Text = $"Low Stock: {(summary.ContainsKey("LowStock") ? summary["LowStock"] : 0)}";
                    lblOutOfStock.Text = $"Out of Stock: {(summary.ContainsKey("OutOfStock") ? summary["OutOfStock"] : 0)}";

                    // Update alerts
                    lstAlerts.Items.Clear();
                    var lowStockAlerts = inventoryService.GetLowStockAlerts();
                    var outOfStockItems = inventoryService.GetOutOfStockItems();

                    foreach (var alert in lowStockAlerts)
                    {
                        lstAlerts.Items.Add($"LOW STOCK: {alert}");
                    }

                    foreach (var item in outOfStockItems)
                    {
                        lstAlerts.Items.Add($"OUT OF STOCK: {item}");
                    }

                    if (lstAlerts.Items.Count == 0)
                    {
                        lstAlerts.Items.Add("All items are well stocked");
                    }

                    dgvInventory.Rows.Clear();

                    foreach (var item in inventory)
                    {
                        int rowIndex = dgvInventory.Rows.Add();
                        dgvInventory.Rows[rowIndex].Cells["BookId"].Value = item.BookId;
                        dgvInventory.Rows[rowIndex].Cells["Title"].Value = item.Title;
                        dgvInventory.Rows[rowIndex].Cells["Author"].Value = item.Author;
                        dgvInventory.Rows[rowIndex].Cells["ISBN"].Value = item.ISBN ?? "N/A";
                        dgvInventory.Rows[rowIndex].Cells["Category"].Value = item.Category ?? "Uncategorized";
                        dgvInventory.Rows[rowIndex].Cells["TotalCopies"].Value = item.TotalCopies;
                        dgvInventory.Rows[rowIndex].Cells["AvailableCopies"].Value = item.AvailableCopies;
                        dgvInventory.Rows[rowIndex].Cells["BorrowedCopies"].Value = item.BorrowedCopies;

                        // Status with color coding
                        var statusCell = dgvInventory.Rows[rowIndex].Cells["Status"];
                        if (item.AvailableCopies == 0)
                        {
                            statusCell.Value = "Out of Stock";
                            statusCell.Style.ForeColor = Color.FromArgb(244, 67, 54); // Red
                        }
                        else if (item.AvailableCopies <= 2)
                        {
                            statusCell.Value = "Low Stock";
                            statusCell.Style.ForeColor = Color.FromArgb(255, 152, 0); // Orange
                        }
                        else
                        {
                            statusCell.Value = "Available";
                            statusCell.Style.ForeColor = Color.FromArgb(76, 175, 80); // Green
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading inventory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // Load categories
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

            // Event handlers
            txtSearchInventory.TextChanged += (s, e) =>
            {
                string text = txtSearchInventory.Text;
                if (text != "🔍 Search inventory..." && text != "Search inventory...")
                {
                    LoadInventoryData();
                }
            };

            cmbCategoryFilter.SelectedIndexChanged += (s, e) => LoadInventoryData();
            cmbStatusFilter.SelectedIndexChanged += (s, e) => LoadInventoryData();
            btnUpdateStock.Click += (s, e) => ShowUpdateStockDialog();
            btnAudit.Click += (s, e) =>
            {
                try
                {
                    var inventoryService = new Library_Management_System.Service.InventoryService();
                    inventoryService.PerformInventoryAudit();
                    MessageBox.Show("Inventory audit completed!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadInventoryData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error performing audit: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // Add controls to panels
            summaryPanel.Controls.AddRange(new Control[] { lblTotalCopies, lblAvailableCopies, lblBorrowedCopies, lblLowStock, lblOutOfStock });
            alertsPanel.Controls.AddRange(new Control[] { lblAlerts, lstAlerts });
            searchPanel.Controls.AddRange(new Control[] { txtSearchInventory, lblCategoryFilter, cmbCategoryFilter, lblStatusFilter, cmbStatusFilter, btnUpdateStock, btnAudit });

            pnlMainContent.Controls.AddRange(new Control[] { titleLabel, summaryPanel, alertsPanel, searchPanel, dgvInventory });

            // Load initial data
            LoadCategories();
            LoadInventoryData();
        }

        private void ShowUpdateStockDialog()
        {
            Form updateForm = new Form
            {
                Text = "Update Stock Level",
                Size = new Size(450, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                ShowInTaskbar = false
            };

            // Title
            Label titleLabel = new Label
            {
                Text = "Update Stock Level",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(30, 20),
                Size = new Size(300, 35),
                ForeColor = Color.FromArgb(40, 40, 40)
            };

            // Book ID
            Label lblBookId = new Label { Text = "Book ID:", Location = new Point(30, 70), Size = new Size(100, 25), Font = new Font("Segoe UI", 10F) };
            TextBox txtBookId = new TextBox { Location = new Point(140, 65), Size = new Size(200, 30), Font = new Font("Segoe UI", 10F) };

            // Current stock info
            Label lblCurrentInfo = new Label
            {
                Text = "Current: Total - 0, Available - 0",
                Location = new Point(30, 110),
                Size = new Size(350, 25),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            // New total copies
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

            // Preview available
            Label lblPreview = new Label
            {
                Text = "Available after update: 0",
                Location = new Point(30, 185),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(33, 150, 243)
            };

            // Buttons
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

            // Load book info when Book ID changes
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

                            // Update preview
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

            // Update preview when total changes
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

            // Event handlers
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

