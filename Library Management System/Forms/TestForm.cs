using System;
using System.Drawing;
using System.Windows.Forms;
using Library_Management_System.Service;

namespace Library_Management_System.Forms
{
    public partial class TestForm : Form
    {
        private TextBox txtResults;
        private Button btnRunServiceTests;
        private Button btnRunAutomatedTests;
        private Button btnClose;

        public TestForm()
        {
            InitializeComponent();
            SetupForm();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Size = new Size(800, 600);
            this.Text = "Library Management System - Test Suite";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ResumeLayout(false);
        }

        private void SetupForm()
        {
            this.Controls.Clear();

            Label lblTitle = new Label
            {
                Text = "Test Suite",
                Font = new Font("Georgia", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(128, 0, 0),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            Label lblSubtitle = new Label
            {
                Text = "Run automated tests to verify all features",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(20, 50)
            };

            btnRunAutomatedTests = new Button
            {
                Text = "🚀 Run All Automated Tests",
                Location = new Point(20, 90),
                Size = new Size(250, 40),
                BackColor = Color.FromArgb(128, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRunAutomatedTests.FlatAppearance.BorderSize = 0;
            btnRunAutomatedTests.Click += (s, e) =>
            {
                try
                {
                    txtResults.Clear();
                    txtResults.AppendText("Running automated tests...\r\n\r\n");
                    Application.DoEvents();
                    
                    AutomatedTests.RunAllTests();
                    txtResults.AppendText("\r\n" + AutomatedTests.GetTestResults());
                }
                catch (Exception ex)
                {
                    txtResults.AppendText($"\r\n❌ Error: {ex.Message}\r\n{ex.StackTrace}");
                }
            };

            btnRunServiceTests = new Button
            {
                Text = "🔧 Run Service Tests",
                Location = new Point(280, 90),
                Size = new Size(200, 40),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
            btnRunServiceTests.FlatAppearance.BorderSize = 0;
            btnRunServiceTests.Click += (s, e) =>
            {
                try
                {
                    txtResults.Clear();
                    txtResults.AppendText("Running service tests...\r\n\r\n");
                    Application.DoEvents();
                    
                    ServiceTests.RunAllTests();
                }
                catch (Exception ex)
                {
                    txtResults.AppendText($"\r\n❌ Error: {ex.Message}\r\n{ex.StackTrace}");
                }
            };

            btnClose = new Button
            {
                Text = "Close",
                Location = new Point(680, 90),
                Size = new Size(100, 40),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(128, 0, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(128, 0, 0);
            btnClose.FlatAppearance.BorderSize = 1;
            btnClose.Click += (s, e) => this.Close();

            txtResults = new TextBox
            {
                Location = new Point(20, 150),
                Size = new Size(760, 400),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9F),
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.LimeGreen
            };

            this.Controls.AddRange(new Control[] 
            { 
                lblTitle, lblSubtitle, 
                btnRunAutomatedTests, btnRunServiceTests, btnClose,
                txtResults
            });
        }
    }
}
