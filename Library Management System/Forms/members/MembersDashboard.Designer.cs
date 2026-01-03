namespace Library_Management_System.Forms.members
{
    partial class MembersDashboard
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel pnlSidebar;
        private System.Windows.Forms.Panel pnlMainContent;
        private System.Windows.Forms.PictureBox picLogo;
        private System.Windows.Forms.Label lblLibraryMS;
        private System.Windows.Forms.Label lblManagementSystem;
        private System.Windows.Forms.Button btnDashboard;
        private System.Windows.Forms.Button btnCatalog;
        private System.Windows.Forms.Button btnReservations;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.Panel pnlAdministrator;
        private System.Windows.Forms.Label lblAdminName;
        private System.Windows.Forms.Button btnSignOut;
        private System.Windows.Forms.Label lblWelcome;
        private System.Windows.Forms.Label lblDate;
        private System.Windows.Forms.Panel pnlCardTotalBooks;
        private System.Windows.Forms.Label lblTotalBooks;
        private System.Windows.Forms.Label lblTotalBooksChange;
        private System.Windows.Forms.Panel pnlCardActiveMembers;
        private System.Windows.Forms.Label lblActiveMembers;
        private System.Windows.Forms.Label lblActiveMembersChange;
        private System.Windows.Forms.Panel pnlCardBooksBorrowed;
        private System.Windows.Forms.Label lblBooksBorrowed;
        private System.Windows.Forms.Label lblBooksBorrowedChange;
        private System.Windows.Forms.Panel pnlCardOverdueBooks;
        private System.Windows.Forms.Label lblOverdueBooks;
        private System.Windows.Forms.Label lblOverdueBooksChange;
        private System.Windows.Forms.Panel pnlCardTodaysBorrowings;
        private System.Windows.Forms.Label lblTodaysBorrowings;
        private System.Windows.Forms.Panel pnlCardTodaysReturns;
        private System.Windows.Forms.Label lblTodaysReturns;
        private System.Windows.Forms.Panel pnlCardPendingFines;
        private System.Windows.Forms.Label lblPendingFines;
        private System.Windows.Forms.Panel pnlWeeklyCirculation;
        private System.Windows.Forms.Label lblWeeklyCirculation;
        private System.Windows.Forms.Panel pnlCollectionCategory;
        private System.Windows.Forms.Label lblCollectionCategory;
        private System.Windows.Forms.Panel pnlMembersView;
        private System.Windows.Forms.Label lblMembersTitle;
        private System.Windows.Forms.Label lblMembersSubtitle;
        private System.Windows.Forms.Panel pnlCardTotalMembers;
        private System.Windows.Forms.Label lblTotalMembers;
        private System.Windows.Forms.Label lblTotalMembersLabel;
        private System.Windows.Forms.Panel pnlCardActiveMembersStat;
        private System.Windows.Forms.Label lblActiveMembersStat;
        private System.Windows.Forms.Label lblActiveMembersStatLabel;
        private System.Windows.Forms.Panel pnlCardSuspendedMembers;
        private System.Windows.Forms.Label lblSuspendedMembers;
        private System.Windows.Forms.Label lblSuspendedMembersLabel;
        private System.Windows.Forms.Panel pnlCardExpiredMembers;
        private System.Windows.Forms.Label lblExpiredMembers;
        private System.Windows.Forms.Label lblExpiredMembersLabel;
        private System.Windows.Forms.Panel pnlSearchFilter;
        private System.Windows.Forms.TextBox txtSearchMembers;
        private System.Windows.Forms.ComboBox cmbStatusFilter;
        private System.Windows.Forms.ComboBox cmbTypeFilter;
        private System.Windows.Forms.DataGridView dgvMembers;
        private System.Windows.Forms.Button btnAddMember;
        private System.Windows.Forms.Label lblTotalMembersIcon;
        private System.Windows.Forms.Label lblActiveMembersIcon;
        private System.Windows.Forms.Label lblSuspendedMembersIcon;
        private System.Windows.Forms.Label lblExpiredMembersIcon;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlSidebar = new System.Windows.Forms.Panel();
            this.pnlAdministrator = new System.Windows.Forms.Panel();
            this.btnSignOut = new System.Windows.Forms.Button();
            this.lblAdminName = new System.Windows.Forms.Label();
            this.btnSettings = new System.Windows.Forms.Button();
            this.btnSearch = new System.Windows.Forms.Button();
            this.btnReports = new System.Windows.Forms.Button();
            this.btnInventory = new System.Windows.Forms.Button();
            this.btnFines = new System.Windows.Forms.Button();
            this.btnReservations = new System.Windows.Forms.Button();
            this.btnCirculation = new System.Windows.Forms.Button();
            this.btnCatalog = new System.Windows.Forms.Button();
            this.btnMembers = new System.Windows.Forms.Button();
            this.btnDashboard = new System.Windows.Forms.Button();
            this.lblManagementSystem = new System.Windows.Forms.Label();
            this.lblLibraryMS = new System.Windows.Forms.Label();
            this.picLogo = new System.Windows.Forms.PictureBox();
            this.pnlMainContent = new System.Windows.Forms.Panel();
            this.pnlCollectionCategory = new System.Windows.Forms.Panel();
            this.lblCollectionCategory = new System.Windows.Forms.Label();
            this.pnlWeeklyCirculation = new System.Windows.Forms.Panel();
            this.lblWeeklyCirculation = new System.Windows.Forms.Label();
            this.pnlCardPendingFines = new System.Windows.Forms.Panel();
            this.lblPendingFines = new System.Windows.Forms.Label();
            this.pnlCardTodaysReturns = new System.Windows.Forms.Panel();
            this.lblTodaysReturns = new System.Windows.Forms.Label();
            this.pnlCardTodaysBorrowings = new System.Windows.Forms.Panel();
            this.lblTodaysBorrowings = new System.Windows.Forms.Label();
            this.pnlCardBooksBorrowed = new System.Windows.Forms.Panel();
            this.lblBooksBorrowed = new System.Windows.Forms.Label();
            this.lblBooksBorrowedChange = new System.Windows.Forms.Label();
            this.pnlCardActiveMembers = new System.Windows.Forms.Panel();
            this.lblActiveMembers = new System.Windows.Forms.Label();
            this.lblActiveMembersChange = new System.Windows.Forms.Label();
            this.pnlCardTotalBooks = new System.Windows.Forms.Panel();
            this.lblTotalBooks = new System.Windows.Forms.Label();
            this.lblTotalBooksChange = new System.Windows.Forms.Label();
            this.lblWelcome = new System.Windows.Forms.Label();
            this.lblDate = new System.Windows.Forms.Label();
            this.pnlMembersView = new System.Windows.Forms.Panel();
            this.dgvMembers = new System.Windows.Forms.DataGridView();
            this.pnlSearchFilter = new System.Windows.Forms.Panel();
            this.cmbTypeFilter = new System.Windows.Forms.ComboBox();
            this.cmbStatusFilter = new System.Windows.Forms.ComboBox();
            this.txtSearchMembers = new System.Windows.Forms.TextBox();
            this.pnlCardExpiredMembers = new System.Windows.Forms.Panel();
            this.lblExpiredMembersIcon = new System.Windows.Forms.Label();
            this.lblExpiredMembers = new System.Windows.Forms.Label();
            this.lblExpiredMembersLabel = new System.Windows.Forms.Label();
            this.pnlCardSuspendedMembers = new System.Windows.Forms.Panel();
            this.lblSuspendedMembersIcon = new System.Windows.Forms.Label();
            this.lblSuspendedMembers = new System.Windows.Forms.Label();
            this.lblSuspendedMembersLabel = new System.Windows.Forms.Label();
            this.pnlCardActiveMembersStat = new System.Windows.Forms.Panel();
            this.lblActiveMembersIcon = new System.Windows.Forms.Label();
            this.lblActiveMembersStat = new System.Windows.Forms.Label();
            this.lblActiveMembersStatLabel = new System.Windows.Forms.Label();
            this.pnlCardTotalMembers = new System.Windows.Forms.Panel();
            this.lblTotalMembersIcon = new System.Windows.Forms.Label();
            this.lblTotalMembers = new System.Windows.Forms.Label();
            this.lblTotalMembersLabel = new System.Windows.Forms.Label();
            this.pnlCardOverdueBooks = new System.Windows.Forms.Panel();
            this.lblOverdueBooks = new System.Windows.Forms.Label();
            this.lblOverdueBooksChange = new System.Windows.Forms.Label();
            this.lblMembersSubtitle = new System.Windows.Forms.Label();
            this.lblMembersTitle = new System.Windows.Forms.Label();
            this.btnAddMember = new System.Windows.Forms.Button();
            this.pnlSidebar.SuspendLayout();
            this.pnlAdministrator.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).BeginInit();
            this.pnlMainContent.SuspendLayout();
            this.pnlCollectionCategory.SuspendLayout();
            this.pnlWeeklyCirculation.SuspendLayout();
            this.pnlCardPendingFines.SuspendLayout();
            this.pnlCardTodaysReturns.SuspendLayout();
            this.pnlCardTodaysBorrowings.SuspendLayout();
            this.pnlCardBooksBorrowed.SuspendLayout();
            this.pnlCardActiveMembers.SuspendLayout();
            this.pnlCardTotalBooks.SuspendLayout();
            this.pnlMembersView.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMembers)).BeginInit();
            this.pnlSearchFilter.SuspendLayout();
            this.pnlCardExpiredMembers.SuspendLayout();
            this.pnlCardSuspendedMembers.SuspendLayout();
            this.pnlCardActiveMembersStat.SuspendLayout();
            this.pnlCardTotalMembers.SuspendLayout();
            this.pnlCardOverdueBooks.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlSidebar
            // 
            this.pnlSidebar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.pnlSidebar.Controls.Add(this.pnlAdministrator);
            this.pnlSidebar.Controls.Add(this.pnlAdministrator);
            this.pnlSidebar.Controls.Add(this.btnSearch);
            this.pnlSidebar.Controls.Add(this.btnReservations);
            this.pnlSidebar.Controls.Add(this.btnCatalog);
            this.pnlSidebar.Controls.Add(this.btnDashboard);
            this.pnlSidebar.Controls.Add(this.lblManagementSystem);
            this.pnlSidebar.Controls.Add(this.lblLibraryMS);
            this.pnlSidebar.Controls.Add(this.picLogo);
            this.pnlSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlSidebar.Location = new System.Drawing.Point(0, 0);
            this.pnlSidebar.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.pnlSidebar.Name = "pnlSidebar";
            this.pnlSidebar.Size = new System.Drawing.Size(280, 650);
            this.pnlSidebar.TabIndex = 0;
            // 
            // pnlAdministrator
            // 
            this.pnlAdministrator.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.pnlAdministrator.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.pnlAdministrator.Controls.Add(this.btnSignOut);
            this.pnlAdministrator.Controls.Add(this.lblAdminName);
            this.pnlAdministrator.Location = new System.Drawing.Point(0, 560);
            this.pnlAdministrator.Margin = new System.Windows.Forms.Padding(0);
            this.pnlAdministrator.Name = "pnlAdministrator";
            this.pnlAdministrator.Padding = new System.Windows.Forms.Padding(0, 15, 0, 15);
            this.pnlAdministrator.Size = new System.Drawing.Size(280, 90);
            this.pnlAdministrator.TabIndex = 12;
            // 
            // btnSignOut
            // 
            this.btnSignOut.BackColor = System.Drawing.Color.Transparent;
            this.btnSignOut.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSignOut.FlatAppearance.BorderSize = 0;
            this.btnSignOut.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.btnSignOut.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSignOut.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSignOut.ForeColor = System.Drawing.Color.White;
            this.btnSignOut.Location = new System.Drawing.Point(20, 45);
            this.btnSignOut.Margin = new System.Windows.Forms.Padding(0);
            this.btnSignOut.Name = "btnSignOut";
            this.btnSignOut.Padding = new System.Windows.Forms.Padding(20, 0, 0, 0);
            this.btnSignOut.Size = new System.Drawing.Size(240, 50);
            this.btnSignOut.TabIndex = 1;
            this.btnSignOut.Text = "🚪 Sign Out";
            this.btnSignOut.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSignOut.UseVisualStyleBackColor = false;
            this.btnSignOut.Click += new System.EventHandler(this.BtnSignOut_Click);
            // 
            // lblAdminName
            // 
            this.lblAdminName.AutoSize = true;
            this.lblAdminName.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAdminName.ForeColor = System.Drawing.Color.White;
            this.lblAdminName.Location = new System.Drawing.Point(14, 9);
            this.lblAdminName.Name = "lblAdminName";
            this.lblAdminName.Size = new System.Drawing.Size(96, 36);
            this.lblAdminName.TabIndex = 0;
            this.lblAdminName.Text = "Admin";

            // 
            // btnDashboard
            // 
            this.btnDashboard.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.btnDashboard.FlatAppearance.BorderSize = 0;
            this.btnDashboard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDashboard.Font = new System.Drawing.Font("Segoe UI", 11.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDashboard.ForeColor = System.Drawing.Color.White;
            this.btnDashboard.Location = new System.Drawing.Point(0, 90);
            this.btnDashboard.Margin = new System.Windows.Forms.Padding(0);
            this.btnDashboard.Name = "btnDashboard";
            this.btnDashboard.Padding = new System.Windows.Forms.Padding(25, 0, 0, 0);
            this.btnDashboard.Size = new System.Drawing.Size(280, 50);
            this.btnDashboard.TabIndex = 2;
            this.btnDashboard.Tag = "Dashboard";
            this.btnDashboard.Text = "  ▦ Dashboard";
            this.btnDashboard.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnDashboard.UseVisualStyleBackColor = false;
            this.btnDashboard.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // btnCatalog
            // 
            this.btnCatalog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.btnCatalog.FlatAppearance.BorderSize = 0;
            this.btnCatalog.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCatalog.Font = new System.Drawing.Font("Segoe UI", 11.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCatalog.ForeColor = System.Drawing.Color.White;
            this.btnCatalog.Location = new System.Drawing.Point(0, 140);
            this.btnCatalog.Margin = new System.Windows.Forms.Padding(0);
            this.btnCatalog.Name = "btnCatalog";
            this.btnCatalog.Padding = new System.Windows.Forms.Padding(25, 0, 0, 0);
            this.btnCatalog.Size = new System.Drawing.Size(280, 50);
            this.btnCatalog.TabIndex = 3;
            this.btnCatalog.Tag = "Catalog";
            this.btnCatalog.Text = "  📚 Catalog";
            this.btnCatalog.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCatalog.UseVisualStyleBackColor = false;
            this.btnCatalog.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // btnReservations
            // 
            this.btnReservations.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.btnReservations.FlatAppearance.BorderSize = 0;
            this.btnReservations.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReservations.Font = new System.Drawing.Font("Segoe UI", 11.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnReservations.ForeColor = System.Drawing.Color.White;
            this.btnReservations.Location = new System.Drawing.Point(0, 190);
            this.btnReservations.Margin = new System.Windows.Forms.Padding(0);
            this.btnReservations.Name = "btnReservations";
            this.btnReservations.Padding = new System.Windows.Forms.Padding(25, 0, 0, 0);
            this.btnReservations.Size = new System.Drawing.Size(280, 50);
            this.btnReservations.TabIndex = 4;
            this.btnReservations.Tag = "Reservations";
            this.btnReservations.Text = "  📅 Reservations";
            this.btnReservations.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnReservations.UseVisualStyleBackColor = false;
            this.btnReservations.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // btnSearch
            // 
            this.btnSearch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.btnSearch.FlatAppearance.BorderSize = 0;
            this.btnSearch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSearch.Font = new System.Drawing.Font("Segoe UI", 11.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSearch.ForeColor = System.Drawing.Color.White;
            this.btnSearch.Location = new System.Drawing.Point(0, 240);
            this.btnSearch.Margin = new System.Windows.Forms.Padding(0);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Padding = new System.Windows.Forms.Padding(25, 0, 0, 0);
            this.btnSearch.Size = new System.Drawing.Size(280, 50);
            this.btnSearch.TabIndex = 5;
            this.btnSearch.Tag = "Search";
            this.btnSearch.Text = "  🔍 Search";
            this.btnSearch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSearch.UseVisualStyleBackColor = false;
            this.btnSearch.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // lblManagementSystem
            // 
            this.lblManagementSystem.AutoSize = true;
            this.lblManagementSystem.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblManagementSystem.ForeColor = System.Drawing.Color.White;
            this.lblManagementSystem.Location = new System.Drawing.Point(76, 57);
            this.lblManagementSystem.Name = "lblManagementSystem";
            this.lblManagementSystem.Size = new System.Drawing.Size(195, 28);
            this.lblManagementSystem.TabIndex = 1;
            this.lblManagementSystem.Text = "Management System";
            // 
            // lblLibraryMS
            // 
            this.lblLibraryMS.AutoSize = true;
            this.lblLibraryMS.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLibraryMS.ForeColor = System.Drawing.Color.White;
            this.lblLibraryMS.Location = new System.Drawing.Point(80, 0);
            this.lblLibraryMS.Name = "lblLibraryMS";
            this.lblLibraryMS.Size = new System.Drawing.Size(191, 48);
            this.lblLibraryMS.TabIndex = 0;
            this.lblLibraryMS.Text = "LibraryMS";
            // 
            // picLogo
            // 
            this.picLogo.BackColor = System.Drawing.Color.Transparent;
            this.picLogo.ImageLocation = "";
            this.picLogo.Location = new System.Drawing.Point(20, 15);
            this.picLogo.Name = "picLogo";
            this.picLogo.Size = new System.Drawing.Size(50, 50);
            this.picLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picLogo.TabIndex = 13;
            this.picLogo.TabStop = false;
            // 
            // pnlMainContent
            // 
            this.pnlMainContent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.pnlMainContent.Controls.Add(this.pnlCollectionCategory);
            this.pnlMainContent.Controls.Add(this.pnlWeeklyCirculation);
            this.pnlMainContent.Controls.Add(this.pnlCardPendingFines);
            this.pnlMainContent.Controls.Add(this.pnlCardTodaysReturns);
            this.pnlMainContent.Controls.Add(this.pnlCardTodaysBorrowings);
            this.pnlMainContent.Controls.Add(this.pnlCardBooksBorrowed);
            this.pnlMainContent.Controls.Add(this.pnlCardActiveMembers);
            this.pnlMainContent.Controls.Add(this.pnlCardTotalBooks);
            this.pnlMainContent.Controls.Add(this.lblWelcome);
            this.pnlMainContent.Controls.Add(this.lblDate);
            this.pnlMainContent.Controls.Add(this.pnlMembersView);
            this.pnlMainContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMainContent.Location = new System.Drawing.Point(280, 0);
            this.pnlMainContent.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.pnlMainContent.Name = "pnlMainContent";
            this.pnlMainContent.Padding = new System.Windows.Forms.Padding(30, 35, 30, 35);
            this.pnlMainContent.Size = new System.Drawing.Size(1120, 650);
            this.pnlMainContent.TabIndex = 1;
            // 
            // pnlCollectionCategory
            // 
            this.pnlCollectionCategory.BackColor = System.Drawing.Color.White;
            this.pnlCollectionCategory.Controls.Add(this.lblCollectionCategory);
            this.pnlCollectionCategory.Location = new System.Drawing.Point(570, 500);
            this.pnlCollectionCategory.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.pnlCollectionCategory.Name = "pnlCollectionCategory";
            this.pnlCollectionCategory.Size = new System.Drawing.Size(504, 200);
            this.pnlCollectionCategory.TabIndex = 11;
            // 
            // lblCollectionCategory
            // 
            this.lblCollectionCategory.AutoSize = true;
            this.lblCollectionCategory.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCollectionCategory.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.lblCollectionCategory.Location = new System.Drawing.Point(20, 15);
            this.lblCollectionCategory.Name = "lblCollectionCategory";
            this.lblCollectionCategory.Size = new System.Drawing.Size(340, 41);
            this.lblCollectionCategory.TabIndex = 0;
            this.lblCollectionCategory.Text = "Collection by Category";
            // 
            // pnlWeeklyCirculation
            // 
            this.pnlWeeklyCirculation.BackColor = System.Drawing.Color.White;
            this.pnlWeeklyCirculation.Controls.Add(this.lblWeeklyCirculation);
            this.pnlWeeklyCirculation.Location = new System.Drawing.Point(38, 500);
            this.pnlWeeklyCirculation.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.pnlWeeklyCirculation.Name = "pnlWeeklyCirculation";
            this.pnlWeeklyCirculation.Size = new System.Drawing.Size(520, 200);
            this.pnlWeeklyCirculation.TabIndex = 10;
            // 
            // lblWeeklyCirculation
            // 
            this.lblWeeklyCirculation.AutoSize = true;
            this.lblWeeklyCirculation.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblWeeklyCirculation.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.lblWeeklyCirculation.Location = new System.Drawing.Point(20, 15);
            this.lblWeeklyCirculation.Name = "lblWeeklyCirculation";
            this.lblWeeklyCirculation.Size = new System.Drawing.Size(283, 41);
            this.lblWeeklyCirculation.TabIndex = 0;
            this.lblWeeklyCirculation.Text = "Weekly Circulation";
            // 
            // pnlCardPendingFines
            // 
            this.pnlCardPendingFines.BackColor = System.Drawing.Color.White;
            this.pnlCardPendingFines.Controls.Add(this.lblPendingFines);
            this.pnlCardPendingFines.Location = new System.Drawing.Point(572, 320);
            this.pnlCardPendingFines.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.pnlCardPendingFines.Name = "pnlCardPendingFines";
            this.pnlCardPendingFines.Size = new System.Drawing.Size(250, 150);
            this.pnlCardPendingFines.TabIndex = 8;
            // 
            // lblPendingFines
            // 
            this.lblPendingFines.AutoSize = true;
            this.lblPendingFines.Font = new System.Drawing.Font("Segoe UI", 28F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPendingFines.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.lblPendingFines.Location = new System.Drawing.Point(20, 50);
            this.lblPendingFines.Name = "lblPendingFines";
            this.lblPendingFines.Size = new System.Drawing.Size(128, 74);
            this.lblPendingFines.TabIndex = 0;
            this.lblPendingFines.Text = "565";
            // 
            // pnlCardTodaysReturns
            // 
            this.pnlCardTodaysReturns.BackColor = System.Drawing.Color.White;
            this.pnlCardTodaysReturns.Controls.Add(this.lblTodaysReturns);
            this.pnlCardTodaysReturns.Location = new System.Drawing.Point(305, 320);
            this.pnlCardTodaysReturns.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.pnlCardTodaysReturns.Name = "pnlCardTodaysReturns";
            this.pnlCardTodaysReturns.Size = new System.Drawing.Size(250, 150);
            this.pnlCardTodaysReturns.TabIndex = 7;
            // 
            // lblTodaysReturns
            // 
            this.lblTodaysReturns.AutoSize = true;
            this.lblTodaysReturns.Font = new System.Drawing.Font("Segoe UI", 28F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTodaysReturns.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.lblTodaysReturns.Location = new System.Drawing.Point(20, 50);
            this.lblTodaysReturns.Name = "lblTodaysReturns";
            this.lblTodaysReturns.Size = new System.Drawing.Size(96, 74);
            this.lblTodaysReturns.TabIndex = 0;
            this.lblTodaysReturns.Text = "12";
            // 
            // pnlCardTodaysBorrowings
            // 
            this.pnlCardTodaysBorrowings.BackColor = System.Drawing.Color.White;
            this.pnlCardTodaysBorrowings.Controls.Add(this.lblTodaysBorrowings);
            this.pnlCardTodaysBorrowings.Location = new System.Drawing.Point(38, 320);
            this.pnlCardTodaysBorrowings.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.pnlCardTodaysBorrowings.Name = "pnlCardTodaysBorrowings";
            this.pnlCardTodaysBorrowings.Size = new System.Drawing.Size(250, 150);
            this.pnlCardTodaysBorrowings.TabIndex = 6;
            // 
            // lblTodaysBorrowings
            // 
            this.lblTodaysBorrowings.AutoSize = true;
            this.lblTodaysBorrowings.Font = new System.Drawing.Font("Segoe UI", 28F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTodaysBorrowings.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.lblTodaysBorrowings.Location = new System.Drawing.Point(20, 50);
            this.lblTodaysBorrowings.Name = "lblTodaysBorrowings";
            this.lblTodaysBorrowings.Size = new System.Drawing.Size(96, 74);
            this.lblTodaysBorrowings.TabIndex = 0;
            this.lblTodaysBorrowings.Text = "15";
            // 
            // pnlCardBooksBorrowed
            // 
            this.pnlCardBooksBorrowed.BackColor = System.Drawing.Color.White;
            this.pnlCardBooksBorrowed.Controls.Add(this.lblBooksBorrowed);
            this.pnlCardBooksBorrowed.Controls.Add(this.lblBooksBorrowedChange);
            this.pnlCardBooksBorrowed.Location = new System.Drawing.Point(572, 110);
            this.pnlCardBooksBorrowed.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.pnlCardBooksBorrowed.Name = "pnlCardBooksBorrowed";
            this.pnlCardBooksBorrowed.Size = new System.Drawing.Size(250, 180);
            this.pnlCardBooksBorrowed.TabIndex = 4;
            // 
            // lblBooksBorrowed
            // 
            this.lblBooksBorrowed.AutoSize = true;
            this.lblBooksBorrowed.Font = new System.Drawing.Font("Segoe UI", 30F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBooksBorrowed.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.lblBooksBorrowed.Location = new System.Drawing.Point(20, 25);
            this.lblBooksBorrowed.Name = "lblBooksBorrowed";
            this.lblBooksBorrowed.Size = new System.Drawing.Size(105, 81);
            this.lblBooksBorrowed.TabIndex = 0;
            this.lblBooksBorrowed.Text = "78";
            // 
            // lblBooksBorrowedChange
            // 
            this.lblBooksBorrowedChange.AutoSize = true;
            this.lblBooksBorrowedChange.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBooksBorrowedChange.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.lblBooksBorrowedChange.Location = new System.Drawing.Point(20, 130);
            this.lblBooksBorrowedChange.Name = "lblBooksBorrowedChange";
            this.lblBooksBorrowedChange.Size = new System.Drawing.Size(206, 30);
            this.lblBooksBorrowedChange.TabIndex = 1;
            this.lblBooksBorrowedChange.Text = "+5% from last week";
            // 
            // pnlCardActiveMembers
            // 
            this.pnlCardActiveMembers.BackColor = System.Drawing.Color.White;
            this.pnlCardActiveMembers.Controls.Add(this.lblActiveMembers);
            this.pnlCardActiveMembers.Controls.Add(this.lblActiveMembersChange);
            this.pnlCardActiveMembers.Location = new System.Drawing.Point(305, 110);
            this.pnlCardActiveMembers.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.pnlCardActiveMembers.Name = "pnlCardActiveMembers";
            this.pnlCardActiveMembers.Size = new System.Drawing.Size(250, 180);
            this.pnlCardActiveMembers.TabIndex = 3;
            // 
            // lblActiveMembers
            // 
            this.lblActiveMembers.AutoSize = true;
            this.lblActiveMembers.Font = new System.Drawing.Font("Segoe UI", 30F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblActiveMembers.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.lblActiveMembers.Location = new System.Drawing.Point(20, 25);
            this.lblActiveMembers.Name = "lblActiveMembers";
            this.lblActiveMembers.Size = new System.Drawing.Size(140, 81);
            this.lblActiveMembers.TabIndex = 0;
            this.lblActiveMembers.Text = "142";
            // 
            // lblActiveMembersChange
            // 
            this.lblActiveMembersChange.AutoSize = true;
            this.lblActiveMembersChange.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblActiveMembersChange.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.lblActiveMembersChange.Location = new System.Drawing.Point(20, 130);
            this.lblActiveMembersChange.Name = "lblActiveMembersChange";
            this.lblActiveMembersChange.Size = new System.Drawing.Size(206, 30);
            this.lblActiveMembersChange.TabIndex = 1;
            this.lblActiveMembersChange.Text = "+8% from last week";
            // 
            // pnlCardTotalBooks
            // 
            this.pnlCardTotalBooks.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.pnlCardTotalBooks.Controls.Add(this.lblTotalBooks);
            this.pnlCardTotalBooks.Controls.Add(this.lblTotalBooksChange);
            this.pnlCardTotalBooks.Location = new System.Drawing.Point(38, 110);
            this.pnlCardTotalBooks.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.pnlCardTotalBooks.Name = "pnlCardTotalBooks";
            this.pnlCardTotalBooks.Size = new System.Drawing.Size(250, 180);
            this.pnlCardTotalBooks.TabIndex = 2;
            // 
            // lblTotalBooks
            // 
            this.lblTotalBooks.AutoSize = true;
            this.lblTotalBooks.Font = new System.Drawing.Font("Segoe UI", 30F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTotalBooks.ForeColor = System.Drawing.Color.White;
            this.lblTotalBooks.Location = new System.Drawing.Point(20, 25);
            this.lblTotalBooks.Name = "lblTotalBooks";
            this.lblTotalBooks.Size = new System.Drawing.Size(105, 81);
            this.lblTotalBooks.TabIndex = 0;
            this.lblTotalBooks.Text = "22";
            // 
            // lblTotalBooksChange
            // 
            this.lblTotalBooksChange.AutoSize = true;
            this.lblTotalBooksChange.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTotalBooksChange.ForeColor = System.Drawing.Color.White;
            this.lblTotalBooksChange.Location = new System.Drawing.Point(20, 130);
            this.lblTotalBooksChange.Name = "lblTotalBooksChange";
            this.lblTotalBooksChange.Size = new System.Drawing.Size(218, 30);
            this.lblTotalBooksChange.TabIndex = 1;
            this.lblTotalBooksChange.Text = "+12% from last week";
            // 
            // lblWelcome
            // 
            this.lblWelcome.AutoSize = true;
            this.lblWelcome.Font = new System.Drawing.Font("Segoe UI", 28F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblWelcome.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.lblWelcome.Location = new System.Drawing.Point(38, 20);
            this.lblWelcome.Name = "lblWelcome";
            this.lblWelcome.Size = new System.Drawing.Size(411, 74);
            this.lblWelcome.TabIndex = 0;
            this.lblWelcome.Text = "Welcome back";
            // 
            // lblDate
            // 
            this.lblDate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblDate.AutoSize = true;
            this.lblDate.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.lblDate.Location = new System.Drawing.Point(950, 20);
            this.lblDate.Name = "lblDate";
            this.lblDate.Size = new System.Drawing.Size(149, 36);
            this.lblDate.TabIndex = 1;
            this.lblDate.Text = "Dec 6, 2025";
            // 
            // pnlMembersView
            // 
            this.pnlMembersView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.pnlMembersView.Controls.Add(this.dgvMembers);
            this.pnlMembersView.Controls.Add(this.pnlSearchFilter);
            this.pnlMembersView.Controls.Add(this.pnlCardExpiredMembers);
            this.pnlMembersView.Controls.Add(this.pnlCardSuspendedMembers);
            this.pnlMembersView.Controls.Add(this.pnlCardActiveMembersStat);
            this.pnlMembersView.Controls.Add(this.pnlCardTotalMembers);
            this.pnlMembersView.Controls.Add(this.pnlCardOverdueBooks);
            this.pnlMembersView.Controls.Add(this.lblMembersSubtitle);
            this.pnlMembersView.Controls.Add(this.lblMembersTitle);
            this.pnlMembersView.Controls.Add(this.btnAddMember);
            this.pnlMembersView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMembersView.Location = new System.Drawing.Point(30, 35);
            this.pnlMembersView.Name = "pnlMembersView";
            this.pnlMembersView.Padding = new System.Windows.Forms.Padding(25);
            this.pnlMembersView.Size = new System.Drawing.Size(1060, 580);
            this.pnlMembersView.TabIndex = 12;
            this.pnlMembersView.Visible = false;
            // 
            // dgvMembers
            // 
            this.dgvMembers.AllowUserToAddRows = false;
            this.dgvMembers.AllowUserToDeleteRows = false;
            this.dgvMembers.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvMembers.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvMembers.BackgroundColor = System.Drawing.Color.White;
            this.dgvMembers.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvMembers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvMembers.Location = new System.Drawing.Point(25, 340);
            this.dgvMembers.Name = "dgvMembers";
            this.dgvMembers.ReadOnly = true;
            this.dgvMembers.RowHeadersWidth = 51;
            this.dgvMembers.RowTemplate.Height = 40;
            this.dgvMembers.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvMembers.Size = new System.Drawing.Size(990, 215);
            this.dgvMembers.TabIndex = 7;
            // 
            // pnlSearchFilter
            // 
            this.pnlSearchFilter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlSearchFilter.BackColor = System.Drawing.Color.Transparent;
            this.pnlSearchFilter.Controls.Add(this.cmbTypeFilter);
            this.pnlSearchFilter.Controls.Add(this.cmbStatusFilter);
            this.pnlSearchFilter.Controls.Add(this.txtSearchMembers);
            this.pnlSearchFilter.Location = new System.Drawing.Point(25, 280);
            this.pnlSearchFilter.Name = "pnlSearchFilter";
            this.pnlSearchFilter.Size = new System.Drawing.Size(1030, 63);
            this.pnlSearchFilter.TabIndex = 6;
            // 
            // cmbTypeFilter
            // 
            this.cmbTypeFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbTypeFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTypeFilter.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbTypeFilter.FormattingEnabled = true;
            this.cmbTypeFilter.Items.AddRange(new object[] {
            "All Types",
            "Student",
            "Faculty",
            "Staff",
            "Guest"});
            this.cmbTypeFilter.Location = new System.Drawing.Point(889, 0);
            this.cmbTypeFilter.Name = "cmbTypeFilter";
            this.cmbTypeFilter.Size = new System.Drawing.Size(140, 36);
            this.cmbTypeFilter.TabIndex = 2;

            // 
            // cmbStatusFilter
            // 
            this.cmbStatusFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbStatusFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbStatusFilter.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbStatusFilter.FormattingEnabled = true;
            this.cmbStatusFilter.Items.AddRange(new object[] {
            "All Status",
            "Active",
            "Suspended",
            "Expired",
            "Inactive"});
            this.cmbStatusFilter.Location = new System.Drawing.Point(743, 0);
            this.cmbStatusFilter.Name = "cmbStatusFilter";
            this.cmbStatusFilter.Size = new System.Drawing.Size(140, 36);
            this.cmbStatusFilter.TabIndex = 1;

            // 
            // txtSearchMembers
            // 
            this.txtSearchMembers.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSearchMembers.Location = new System.Drawing.Point(0, 10);
            this.txtSearchMembers.Name = "txtSearchMembers";
            this.txtSearchMembers.Size = new System.Drawing.Size(600, 34);
            this.txtSearchMembers.TabIndex = 0;

            // 
            // pnlCardExpiredMembers
            // 
            this.pnlCardExpiredMembers.BackColor = System.Drawing.Color.White;
            this.pnlCardExpiredMembers.Controls.Add(this.lblExpiredMembersIcon);
            this.pnlCardExpiredMembers.Controls.Add(this.lblExpiredMembers);
            this.pnlCardExpiredMembers.Controls.Add(this.lblExpiredMembersLabel);
            this.pnlCardExpiredMembers.Location = new System.Drawing.Point(805, 120);
            this.pnlCardExpiredMembers.Name = "pnlCardExpiredMembers";
            this.pnlCardExpiredMembers.Size = new System.Drawing.Size(250, 150);
            this.pnlCardExpiredMembers.TabIndex = 5;
            // 
            // lblExpiredMembersIcon
            // 
            this.lblExpiredMembersIcon.AutoSize = true;
            this.lblExpiredMembersIcon.Font = new System.Drawing.Font("Segoe UI", 28F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblExpiredMembersIcon.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(152)))), ((int)(((byte)(0)))));
            this.lblExpiredMembersIcon.Location = new System.Drawing.Point(15, 25);
            this.lblExpiredMembersIcon.Name = "lblExpiredMembersIcon";
            this.lblExpiredMembersIcon.Size = new System.Drawing.Size(155, 74);
            this.lblExpiredMembersIcon.TabIndex = 2;
            this.lblExpiredMembersIcon.Text = "👥✗";
            // 
            // lblExpiredMembers
            // 
            this.lblExpiredMembers.AutoSize = true;
            this.lblExpiredMembers.Font = new System.Drawing.Font("Segoe UI", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblExpiredMembers.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.lblExpiredMembers.Location = new System.Drawing.Point(65, 20);
            this.lblExpiredMembers.Name = "lblExpiredMembers";
            this.lblExpiredMembers.Size = new System.Drawing.Size(81, 96);
            this.lblExpiredMembers.TabIndex = 0;
            this.lblExpiredMembers.Text = "0";
            // 
            // lblExpiredMembersLabel
            // 
            this.lblExpiredMembersLabel.AutoSize = true;
            this.lblExpiredMembersLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblExpiredMembersLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.lblExpiredMembersLabel.Location = new System.Drawing.Point(65, 120);
            this.lblExpiredMembersLabel.Name = "lblExpiredMembersLabel";
            this.lblExpiredMembersLabel.Size = new System.Drawing.Size(85, 30);
            this.lblExpiredMembersLabel.TabIndex = 1;
            this.lblExpiredMembersLabel.Text = "Expired";
            // 
            // pnlCardSuspendedMembers
            // 
            this.pnlCardSuspendedMembers.BackColor = System.Drawing.Color.White;
            this.pnlCardSuspendedMembers.Controls.Add(this.lblSuspendedMembersIcon);
            this.pnlCardSuspendedMembers.Controls.Add(this.lblSuspendedMembers);
            this.pnlCardSuspendedMembers.Controls.Add(this.lblSuspendedMembersLabel);
            this.pnlCardSuspendedMembers.Location = new System.Drawing.Point(545, 120);
            this.pnlCardSuspendedMembers.Name = "pnlCardSuspendedMembers";
            this.pnlCardSuspendedMembers.Size = new System.Drawing.Size(250, 150);
            this.pnlCardSuspendedMembers.TabIndex = 4;
            // 
            // lblSuspendedMembersIcon
            // 
            this.lblSuspendedMembersIcon.AutoSize = true;
            this.lblSuspendedMembersIcon.Font = new System.Drawing.Font("Segoe UI", 28F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSuspendedMembersIcon.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(67)))), ((int)(((byte)(54)))));
            this.lblSuspendedMembersIcon.Location = new System.Drawing.Point(15, 25);
            this.lblSuspendedMembersIcon.Name = "lblSuspendedMembersIcon";
            this.lblSuspendedMembersIcon.Size = new System.Drawing.Size(155, 74);
            this.lblSuspendedMembersIcon.TabIndex = 2;
            this.lblSuspendedMembersIcon.Text = "👥✗";
            // 
            // lblSuspendedMembers
            // 
            this.lblSuspendedMembers.AutoSize = true;
            this.lblSuspendedMembers.Font = new System.Drawing.Font("Segoe UI", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSuspendedMembers.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.lblSuspendedMembers.Location = new System.Drawing.Point(65, 20);
            this.lblSuspendedMembers.Name = "lblSuspendedMembers";
            this.lblSuspendedMembers.Size = new System.Drawing.Size(81, 96);
            this.lblSuspendedMembers.TabIndex = 0;
            this.lblSuspendedMembers.Text = "0";
            // 
            // lblSuspendedMembersLabel
            // 
            this.lblSuspendedMembersLabel.AutoSize = true;
            this.lblSuspendedMembersLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSuspendedMembersLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.lblSuspendedMembersLabel.Location = new System.Drawing.Point(65, 120);
            this.lblSuspendedMembersLabel.Name = "lblSuspendedMembersLabel";
            this.lblSuspendedMembersLabel.Size = new System.Drawing.Size(121, 30);
            this.lblSuspendedMembersLabel.TabIndex = 1;
            this.lblSuspendedMembersLabel.Text = "Suspended";
            // 
            // pnlCardActiveMembersStat
            // 
            this.pnlCardActiveMembersStat.BackColor = System.Drawing.Color.White;
            this.pnlCardActiveMembersStat.Controls.Add(this.lblActiveMembersIcon);
            this.pnlCardActiveMembersStat.Controls.Add(this.lblActiveMembersStat);
            this.pnlCardActiveMembersStat.Controls.Add(this.lblActiveMembersStatLabel);
            this.pnlCardActiveMembersStat.Location = new System.Drawing.Point(285, 120);
            this.pnlCardActiveMembersStat.Name = "pnlCardActiveMembersStat";
            this.pnlCardActiveMembersStat.Size = new System.Drawing.Size(250, 150);
            this.pnlCardActiveMembersStat.TabIndex = 3;
            // 
            // lblActiveMembersIcon
            // 
            this.lblActiveMembersIcon.AutoSize = true;
            this.lblActiveMembersIcon.Font = new System.Drawing.Font("Segoe UI", 28F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblActiveMembersIcon.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(76)))), ((int)(((byte)(175)))), ((int)(((byte)(80)))));
            this.lblActiveMembersIcon.Location = new System.Drawing.Point(15, 25);
            this.lblActiveMembersIcon.Name = "lblActiveMembersIcon";
            this.lblActiveMembersIcon.Size = new System.Drawing.Size(151, 74);
            this.lblActiveMembersIcon.TabIndex = 2;
            this.lblActiveMembersIcon.Text = "👥✓";
            // 
            // lblActiveMembersStat
            // 
            this.lblActiveMembersStat.AutoSize = true;
            this.lblActiveMembersStat.Font = new System.Drawing.Font("Segoe UI", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblActiveMembersStat.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.lblActiveMembersStat.Location = new System.Drawing.Point(65, 20);
            this.lblActiveMembersStat.Name = "lblActiveMembersStat";
            this.lblActiveMembersStat.Size = new System.Drawing.Size(81, 96);
            this.lblActiveMembersStat.TabIndex = 0;
            this.lblActiveMembersStat.Text = "0";
            // 
            // lblActiveMembersStatLabel
            // 
            this.lblActiveMembersStatLabel.AutoSize = true;
            this.lblActiveMembersStatLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblActiveMembersStatLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.lblActiveMembersStatLabel.Location = new System.Drawing.Point(65, 120);
            this.lblActiveMembersStatLabel.Name = "lblActiveMembersStatLabel";
            this.lblActiveMembersStatLabel.Size = new System.Drawing.Size(72, 30);
            this.lblActiveMembersStatLabel.TabIndex = 1;
            this.lblActiveMembersStatLabel.Text = "Active";
            // 
            // pnlCardTotalMembers
            // 
            this.pnlCardTotalMembers.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.pnlCardTotalMembers.Controls.Add(this.lblTotalMembersIcon);
            this.pnlCardTotalMembers.Controls.Add(this.lblTotalMembers);
            this.pnlCardTotalMembers.Controls.Add(this.lblTotalMembersLabel);
            this.pnlCardTotalMembers.Location = new System.Drawing.Point(25, 120);
            this.pnlCardTotalMembers.Name = "pnlCardTotalMembers";
            this.pnlCardTotalMembers.Size = new System.Drawing.Size(250, 150);
            this.pnlCardTotalMembers.TabIndex = 2;
            // 
            // lblTotalMembersIcon
            // 
            this.lblTotalMembersIcon.AutoSize = true;
            this.lblTotalMembersIcon.Font = new System.Drawing.Font("Segoe UI", 28F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTotalMembersIcon.ForeColor = System.Drawing.Color.White;
            this.lblTotalMembersIcon.Location = new System.Drawing.Point(15, 25);
            this.lblTotalMembersIcon.Name = "lblTotalMembersIcon";
            this.lblTotalMembersIcon.Size = new System.Drawing.Size(109, 74);
            this.lblTotalMembersIcon.TabIndex = 2;
            this.lblTotalMembersIcon.Text = "👥";
            // 
            // lblTotalMembers
            // 
            this.lblTotalMembers.AutoSize = true;
            this.lblTotalMembers.Font = new System.Drawing.Font("Segoe UI", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTotalMembers.ForeColor = System.Drawing.Color.White;
            this.lblTotalMembers.Location = new System.Drawing.Point(65, 20);
            this.lblTotalMembers.Name = "lblTotalMembers";
            this.lblTotalMembers.Size = new System.Drawing.Size(81, 96);
            this.lblTotalMembers.TabIndex = 0;
            this.lblTotalMembers.Text = "0";
            // 
            // lblTotalMembersLabel
            // 
            this.lblTotalMembersLabel.AutoSize = true;
            this.lblTotalMembersLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTotalMembersLabel.ForeColor = System.Drawing.Color.White;
            this.lblTotalMembersLabel.Location = new System.Drawing.Point(65, 120);
            this.lblTotalMembersLabel.Name = "lblTotalMembersLabel";
            this.lblTotalMembersLabel.Size = new System.Drawing.Size(158, 30);
            this.lblTotalMembersLabel.TabIndex = 1;
            this.lblTotalMembersLabel.Text = "Total Members";
            // 
            // pnlCardOverdueBooks
            // 
            this.pnlCardOverdueBooks.BackColor = System.Drawing.Color.White;
            this.pnlCardOverdueBooks.Controls.Add(this.lblOverdueBooks);
            this.pnlCardOverdueBooks.Controls.Add(this.lblOverdueBooksChange);
            this.pnlCardOverdueBooks.Location = new System.Drawing.Point(839, 110);
            this.pnlCardOverdueBooks.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.pnlCardOverdueBooks.Name = "pnlCardOverdueBooks";
            this.pnlCardOverdueBooks.Size = new System.Drawing.Size(250, 180);
            this.pnlCardOverdueBooks.TabIndex = 5;
            // 
            // lblOverdueBooks
            // 
            this.lblOverdueBooks.AutoSize = true;
            this.lblOverdueBooks.Font = new System.Drawing.Font("Segoe UI", 30F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOverdueBooks.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.lblOverdueBooks.Location = new System.Drawing.Point(20, 25);
            this.lblOverdueBooks.Name = "lblOverdueBooks";
            this.lblOverdueBooks.Size = new System.Drawing.Size(105, 81);
            this.lblOverdueBooks.TabIndex = 0;
            this.lblOverdueBooks.Text = "12";
            // 
            // lblOverdueBooksChange
            // 
            this.lblOverdueBooksChange.AutoSize = true;
            this.lblOverdueBooksChange.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOverdueBooksChange.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.lblOverdueBooksChange.Location = new System.Drawing.Point(20, 130);
            this.lblOverdueBooksChange.Name = "lblOverdueBooksChange";
            this.lblOverdueBooksChange.Size = new System.Drawing.Size(200, 30);
            this.lblOverdueBooksChange.TabIndex = 1;
            this.lblOverdueBooksChange.Text = "-3% from last week";
            // 
            // lblMembersSubtitle
            // 
            this.lblMembersSubtitle.AutoSize = true;
            this.lblMembersSubtitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMembersSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.lblMembersSubtitle.Location = new System.Drawing.Point(25, 85);
            this.lblMembersSubtitle.Name = "lblMembersSubtitle";
            this.lblMembersSubtitle.Size = new System.Drawing.Size(456, 28);
            this.lblMembersSubtitle.TabIndex = 1;
            this.lblMembersSubtitle.Text = "Manage library member accounts and registrations";
            // 
            // lblMembersTitle
            // 
            this.lblMembersTitle.AutoSize = true;
            this.lblMembersTitle.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMembersTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.lblMembersTitle.Location = new System.Drawing.Point(25, 25);
            this.lblMembersTitle.Name = "lblMembersTitle";
            this.lblMembersTitle.Size = new System.Drawing.Size(240, 65);
            this.lblMembersTitle.TabIndex = 0;
            this.lblMembersTitle.Text = "Members";
            // 
            // btnAddMember
            // 
            this.btnAddMember.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddMember.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.btnAddMember.FlatAppearance.BorderSize = 0;
            this.btnAddMember.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddMember.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAddMember.ForeColor = System.Drawing.Color.White;
            this.btnAddMember.Location = new System.Drawing.Point(854, 28);
            this.btnAddMember.Name = "btnAddMember";
            this.btnAddMember.Size = new System.Drawing.Size(150, 40);
            this.btnAddMember.TabIndex = 8;
            this.btnAddMember.Text = "👥+ Add Member";
            this.btnAddMember.UseVisualStyleBackColor = false;

            // 
            // DashboardForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(240)))), ((int)(((byte)(230)))));
            this.ClientSize = new System.Drawing.Size(1400, 650);
            this.Controls.Add(this.pnlMainContent);
            this.Controls.Add(this.pnlSidebar);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "DashboardForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "LibraryMS - Dashboard";
            this.pnlSidebar.ResumeLayout(false);
            this.pnlSidebar.PerformLayout();
            this.pnlAdministrator.ResumeLayout(false);
            this.pnlAdministrator.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).EndInit();
            this.pnlMainContent.ResumeLayout(false);
            this.pnlMainContent.PerformLayout();
            this.pnlCollectionCategory.ResumeLayout(false);
            this.pnlCollectionCategory.PerformLayout();
            this.pnlWeeklyCirculation.ResumeLayout(false);
            this.pnlWeeklyCirculation.PerformLayout();
            this.pnlCardPendingFines.ResumeLayout(false);
            this.pnlCardPendingFines.PerformLayout();
            this.pnlCardTodaysReturns.ResumeLayout(false);
            this.pnlCardTodaysReturns.PerformLayout();
            this.pnlCardTodaysBorrowings.ResumeLayout(false);
            this.pnlCardTodaysBorrowings.PerformLayout();
            this.pnlCardBooksBorrowed.ResumeLayout(false);
            this.pnlCardBooksBorrowed.PerformLayout();
            this.pnlCardActiveMembers.ResumeLayout(false);
            this.pnlCardActiveMembers.PerformLayout();
            this.pnlCardTotalBooks.ResumeLayout(false);
            this.pnlCardTotalBooks.PerformLayout();
            this.pnlMembersView.ResumeLayout(false);
            this.pnlMembersView.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMembers)).EndInit();
            this.pnlSearchFilter.ResumeLayout(false);
            this.pnlSearchFilter.PerformLayout();
            this.pnlCardExpiredMembers.ResumeLayout(false);
            this.pnlCardExpiredMembers.PerformLayout();
            this.pnlCardSuspendedMembers.ResumeLayout(false);
            this.pnlCardSuspendedMembers.PerformLayout();
            this.pnlCardActiveMembersStat.ResumeLayout(false);
            this.pnlCardActiveMembersStat.PerformLayout();
            this.pnlCardTotalMembers.ResumeLayout(false);
            this.pnlCardTotalMembers.PerformLayout();
            this.pnlCardOverdueBooks.ResumeLayout(false);
            this.pnlCardOverdueBooks.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}

