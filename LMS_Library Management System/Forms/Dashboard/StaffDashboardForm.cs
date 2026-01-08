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

namespace LMS_Library_Management_System.Forms.Dashboard
{
    public partial class StaffDashboardForm : Form
    {
        // Dynamic view panels
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
            SetupMembersView();
            ResetMenuHighlights();
            WireUpMenuButtons();
            ShowDashboardView();
        }

        private void SetupMembersView()
        {
            txtSearchMembers.SetPlaceholder("🔍 Search members...");
            cmbStatusFilter.Items.AddRange(new[] { "All Status", "Active", "Suspended", "Expired", "Inactive" });
            cmbStatusFilter.SelectedIndex = 0;
            cmbTypeFilter.Items.AddRange(new[] { "All Types", "Student", "Faculty", "Staff", "Guest" });
            cmbTypeFilter.SelectedIndex = 0;
            btnAddMember.Click += BtnAddMember_Click;
            txtSearchMembers.TextChanged += TxtSearchMembers_TextChanged;
            cmbStatusFilter.SelectedIndexChanged += CmbStatusFilter_SelectedIndexChanged;
            cmbTypeFilter.SelectedIndexChanged += CmbTypeFilter_SelectedIndexChanged;
            
            // Setup card styling
            pnlCardTotalMembers.Paint += Card_Paint;
            pnlCardActiveMembersStat.Paint += Card_Paint;
            pnlCardSuspendedMembers.Paint += Card_Paint;
            pnlCardExpiredMembers.Paint += Card_Paint;
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
                    ctrl != pnlReportsView && 
                    ctrl != pnlSearchView)
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
            if (pnlReportsView != null && !pnlReportsView.IsDisposed) pnlReportsView.Visible = false;
            if (pnlSearchView != null && !pnlSearchView.IsDisposed) pnlSearchView.Visible = false;
        }

        private void WireUpMenuButtons()
        {
            btnDashboard.Click += MenuItem_Click;
            btnMembers.Click += MenuItem_Click;
            btnCatalog.Click += MenuItem_Click;
            btnCirculation.Click += MenuItem_Click;
            btnReservations.Click += MenuItem_Click;
            btnFines.Click += MenuItem_Click;
            btnInventory.Click += MenuItem_Click;
            btnReports.Click += MenuItem_Click;
            btnSearch.Click += MenuItem_Click;
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
        }

        private void ShowMembersView()
        {
            RestoreOriginalControls();
            ShowDashboardControls(false);
            pnlMembersView.Visible = true;
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
                    dgvMembers.Columns["MemberId"].Width = 120;
                    dgvMembers.Columns["Name"].Width = 150;
                    dgvMembers.Columns["Type"].Width = 80;
                    dgvMembers.Columns["Email"].Width = 200;
                    dgvMembers.Columns["Status"].Width = 100;
                    dgvMembers.Columns["Books"].Width = 80;
                    dgvMembers.Columns["Fines"].Width = 100;
                    dgvMembers.Columns["Actions"].Width = 180;
                    dgvMembers.Columns["Actions"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                
                string searchText = txtSearchMembers.Text;
                if (searchText == "🔍 Search members..." || searchText == "Search members...")
                {
                    searchText = "";
                }
                string statusFilter = cmbStatusFilter.SelectedItem?.ToString() ?? "All Status";
                string typeFilter = cmbTypeFilter.SelectedItem?.ToString() ?? "All Types";
                
                // Mock data for demonstration - Replace with actual service call when available
                var mockMembers = new List<MemberInfo>
                {
                    new MemberInfo { MemberId = "MEM001", Name = "John Doe", Type = "Student", Email = "john.doe@email.com", Status = "Active", BooksBorrowed = 2, BooksLimit = 5, Fines = 0.00m },
                    new MemberInfo { MemberId = "MEM002", Name = "Jane Smith", Type = "Faculty", Email = "jane.smith@email.com", Status = "Active", BooksBorrowed = 1, BooksLimit = 10, Fines = 25.50m },
                    new MemberInfo { MemberId = "MEM003", Name = "Bob Johnson", Type = "Student", Email = "bob.johnson@email.com", Status = "Suspended", BooksBorrowed = 0, BooksLimit = 5, Fines = 150.00m },
                    new MemberInfo { MemberId = "MEM004", Name = "Alice Williams", Type = "Staff", Email = "alice.williams@email.com", Status = "Active", BooksBorrowed = 3, BooksLimit = 7, Fines = 0.00m },
                    new MemberInfo { MemberId = "MEM005", Name = "Charlie Brown", Type = "Student", Email = "charlie.brown@email.com", Status = "Expired", BooksBorrowed = 0, BooksLimit = 5, Fines = 0.00m },
                    new MemberInfo { MemberId = "MEM006", Name = "Diana Prince", Type = "Faculty", Email = "diana.prince@email.com", Status = "Active", BooksBorrowed = 4, BooksLimit = 10, Fines = 0.00m },
                    new MemberInfo { MemberId = "MEM007", Name = "Edward Lee", Type = "Guest", Email = "edward.lee@email.com", Status = "Active", BooksBorrowed = 1, BooksLimit = 3, Fines = 10.00m },
                    new MemberInfo { MemberId = "MEM008", Name = "Fiona Chen", Type = "Student", Email = "fiona.chen@email.com", Status = "Active", BooksBorrowed = 0, BooksLimit = 5, Fines = 0.00m }
                };
                
                // Apply filters
                IEnumerable<MemberInfo> filteredMembers = mockMembers;
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
                var allMembers = mockMembers;
                lblTotalMembers.Text = allMembers.Count.ToString();
                lblActiveMembersStat.Text = allMembers.Count(m => m.Status == "Active").ToString();
                lblSuspendedMembers.Text = allMembers.Count(m => m.Status == "Suspended").ToString();
                lblExpiredMembers.Text = allMembers.Count(m => m.Status == "Expired").ToString();
                
                // Populate grid
                dgvMembers.Rows.Clear();
                foreach (var member in filteredMembers)
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
                
                // Wire up events (only once)
                dgvMembers.CellClick -= DgvMembers_CellClick;
                dgvMembers.CellClick += DgvMembers_CellClick;
                dgvMembers.CellFormatting -= DgvMembers_CellFormatting;
                dgvMembers.CellFormatting += DgvMembers_CellFormatting;
                
                if (cmbStatusFilter.SelectedIndex == -1)
                {
                    cmbStatusFilter.SelectedIndex = 0;
                }
                if (cmbTypeFilter.SelectedIndex == -1)
                {
                    cmbTypeFilter.SelectedIndex = 0;
                }
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
            try
            {
                using (Form addMemberForm = new Form())
                {
                    addMemberForm.Text = "Add New Member";
                    addMemberForm.Size = new Size(500, 600);
                    addMemberForm.StartPosition = FormStartPosition.CenterParent;
                    addMemberForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    addMemberForm.MaximizeBox = false;
                    addMemberForm.MinimizeBox = false;
                    
                    Label lblTitle = new Label
                    {
                        Text = "Add New Member",
                        Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                        ForeColor = ThemeConstants.PrimaryMaroon,
                        Location = new Point(20, 20),
                        AutoSize = true
                    };
                    
                    Label lblMemberId = new Label { Text = "Member ID:", Location = new Point(20, 70), AutoSize = true };
                    TextBox txtMemberId = new TextBox { Location = new Point(20, 95), Size = new Size(440, 30), Font = new Font("Segoe UI", 10F) };
                    
                    Label lblName = new Label { Text = "Full Name:", Location = new Point(20, 140), AutoSize = true };
                    TextBox txtName = new TextBox { Location = new Point(20, 165), Size = new Size(440, 30), Font = new Font("Segoe UI", 10F) };
                    
                    Label lblType = new Label { Text = "Member Type:", Location = new Point(20, 210), AutoSize = true };
                    ComboBox cmbType = new ComboBox { Location = new Point(20, 235), Size = new Size(440, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F) };
                    cmbType.Items.AddRange(new[] { "Student", "Faculty", "Staff", "Guest" });
                    cmbType.SelectedIndex = 0;
                    
                    Label lblEmail = new Label { Text = "Email:", Location = new Point(20, 280), AutoSize = true };
                    TextBox txtEmail = new TextBox { Location = new Point(20, 305), Size = new Size(440, 30), Font = new Font("Segoe UI", 10F) };
                    
                    Label lblPhone = new Label { Text = "Phone:", Location = new Point(20, 350), AutoSize = true };
                    TextBox txtPhone = new TextBox { Location = new Point(20, 375), Size = new Size(440, 30), Font = new Font("Segoe UI", 10F) };
                    
                    Button btnSave = new Button
                    {
                        Text = "Save",
                        Location = new Point(300, 430),
                        Size = new Size(75, 35),
                        BackColor = ThemeConstants.PrimaryMaroon,
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        DialogResult = DialogResult.OK
                    };
                    
                    Button btnCancel = new Button
                    {
                        Text = "Cancel",
                        Location = new Point(385, 430),
                        Size = new Size(75, 35),
                        DialogResult = DialogResult.Cancel
                    };
                    
                    addMemberForm.Controls.AddRange(new Control[] { lblTitle, lblMemberId, txtMemberId, lblName, txtName, 
                        lblType, cmbType, lblEmail, txtEmail, lblPhone, txtPhone, btnSave, btnCancel });
                    
                    if (addMemberForm.ShowDialog() == DialogResult.OK)
                    {
                        if (string.IsNullOrWhiteSpace(txtMemberId.Text) || string.IsNullOrWhiteSpace(txtName.Text))
                        {
                            MessageBox.Show("Member ID and Name are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        
                        // TODO: Save to database
                        MessageBox.Show($"Member {txtName.Text} has been added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadMembersData();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding member: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                
                // TODO: Load actual member data from database
                // For now, show a dialog with member information
                using (Form viewForm = new Form())
                {
                    viewForm.Text = $"Member Details - {memberId}";
                    viewForm.Size = new Size(500, 500);
                    viewForm.StartPosition = FormStartPosition.CenterParent;
                    viewForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    viewForm.MaximizeBox = false;
                    viewForm.MinimizeBox = false;
                    
                    Label lblTitle = new Label
                    {
                        Text = $"Member Information: {memberId}",
                        Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                        ForeColor = ThemeConstants.PrimaryMaroon,
                        Location = new Point(20, 20),
                        AutoSize = true
                    };
                    
                    // Mock member data - replace with actual data loading
                    string memberInfo = $"Member ID: {memberId}\n\n" +
                                      $"Name: [Member Name]\n" +
                                      $"Type: [Member Type]\n" +
                                      $"Email: [Email Address]\n" +
                                      $"Phone: [Phone Number]\n" +
                                      $"Status: [Status]\n" +
                                      $"Books Borrowed: [Count]\n" +
                                      $"Fines: ₱[Amount]\n\n" +
                                      $"Registration Date: [Date]\n" +
                                      $"Expiry Date: [Date]";
                    
                    TextBox txtInfo = new TextBox
                    {
                        Text = memberInfo,
                        Multiline = true,
                        ReadOnly = true,
                        Location = new Point(20, 60),
                        Size = new Size(440, 350),
                        Font = new Font("Segoe UI", 10F),
                        ScrollBars = ScrollBars.Vertical
                    };
                    
                    Button btnClose = new Button
                    {
                        Text = "Close",
                        Location = new Point(385, 420),
                        Size = new Size(75, 35),
                        DialogResult = DialogResult.OK
                    };
                    
                    viewForm.Controls.AddRange(new Control[] { lblTitle, txtInfo, btnClose });
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
            btnAddBookHeader.Click += (s, e) => ShowFeatureMessage("Add Book", "Add book functionality will be implemented.");
            Panel statsPanel = new Panel
            {
                Location = new Point(30, 150),
                Size = new Size(pnlMainContent.Width - 60, 80),
                BackColor = Color.Transparent,
                Tag = "CatalogStatsPanel"
            };
            Panel cardTotalTitles = CreateCatalogStatCard("📚", "0", "Total Titles", ThemeConstants.PrimaryMaroon, new Point(0, 0));
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
                BackColor = ThemeConstants.PrimaryMaroon,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnCheckout.FlatAppearance.BorderSize = 0;
            btnCheckout.Click += (s, e) => ShowFeatureMessage("Check Out", "Check out functionality will be implemented.");
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
            btnNewReservation.Click += (s, e) => ShowFeatureMessage("New Reservation", "Reservation functionality will be implemented.");
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
            btnAddFine.Click += (s, e) => ShowFeatureMessage("Add Fine", "Add fine functionality will be implemented.");
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
            Panel cardTotalTitles = CreateCatalogStatCard("📚", "0", "Total Titles", ThemeConstants.PrimaryMaroon, new Point(0, 0));
            Panel cardTotalCopies = CreateCatalogStatCard("📖", "0", "Total Copies", Color.White, new Point(250, 0));
            Panel cardAvailable = CreateCatalogStatCard("✓", "0", "Available", Color.FromArgb(76, 175, 80), new Point(500, 0));
            Panel cardBorrowed = CreateCatalogStatCard("📗", "0", "Borrowed", Color.FromArgb(33, 150, 243), new Point(0, 80));
            Panel cardDamaged = CreateCatalogStatCard("⚠", "0", "Damaged", Color.FromArgb(255, 193, 7), new Point(250, 80));
            Panel cardLost = CreateCatalogStatCard("❌", "0", "Lost", Color.FromArgb(244, 67, 54), new Point(500, 80));
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
                Text = "🔍",
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

        private void ShowReportsView()
        {
            RestoreOriginalControls();
            ShowDashboardControls(false);
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

        private void ResetReportNavButtons()
        {
            foreach (Control c in pnlReportsSubNav.Controls)
            {
                if (c is Button btn)
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
            Label lblTitle = new Label
            {
                Text = "Circulation Reports",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                Location = new Point(0, 0),
                AutoSize = true,
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            Label lblSubtitle = new Label
            {
                Text = "View borrowing and return statistics",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(0, 40),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            pnlReportsContent.Controls.Add(lblTitle);
            pnlReportsContent.Controls.Add(lblSubtitle);
        }

        private void ShowReportsMembers()
        {
            SetActiveReportNavButton(btnReportMembers);
            pnlReportsContent.Controls.Clear();
            Label lblTitle = new Label
            {
                Text = "Member Reports",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                Location = new Point(0, 0),
                AutoSize = true,
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            Label lblSubtitle = new Label
            {
                Text = "View member statistics and activity",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(0, 40),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            pnlReportsContent.Controls.Add(lblTitle);
            pnlReportsContent.Controls.Add(lblSubtitle);
        }

        private void ShowReportsCollection()
        {
            SetActiveReportNavButton(btnReportCollection);
            pnlReportsContent.Controls.Clear();
            Label lblTitle = new Label
            {
                Text = "Collection Reports",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                Location = new Point(0, 0),
                AutoSize = true,
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            Label lblSubtitle = new Label
            {
                Text = "View collection statistics and distribution",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(0, 40),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            pnlReportsContent.Controls.Add(lblTitle);
            pnlReportsContent.Controls.Add(lblSubtitle);
        }

        private void ShowReportsFines()
        {
            SetActiveReportNavButton(btnReportFines);
            pnlReportsContent.Controls.Clear();
            Label lblTitle = new Label
            {
                Text = "Fines Reports",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                Location = new Point(0, 0),
                AutoSize = true,
                ForeColor = Color.FromArgb(40, 40, 40)
            };
            Label lblSubtitle = new Label
            {
                Text = "View fine collection and payment statistics",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(0, 40),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            pnlReportsContent.Controls.Add(lblTitle);
            pnlReportsContent.Controls.Add(lblSubtitle);
        }

        private Button CreateReportSubNavButton(string text, bool isActive)
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
    }
}
