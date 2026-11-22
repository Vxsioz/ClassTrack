using ClassTrack.UI;
using ClassTrack.UI.Controls;
using ClassTrack.Views;

namespace ClassTrack
{
    /// <summary>
    /// The main window: a left navigation sidebar and one page at a time on the right.
    /// All page logic lives in the Views folder; this form only switches between them.
    /// </summary>
    public partial class Form1 : DarkForm
    {
        private readonly DashboardView dashboard = new();
        private readonly StudentsView students = new();
        private readonly StudentDetailsView details = new();
        private NavItem navDashboard = null!;
        private NavItem navStudents = null!;

        public Form1()
        {
            InitializeComponent();
            BuildUi();
            ShowView(dashboard, navDashboard, "Dashboard");
        }

        private void BuildUi()
        {
            Text = "ClassTrack";
            ClientSize = new Size(1120, 740);
            MinimumSize = new Size(900, 560);

            // ---------- Pages ----------
            var content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg };
            content.Controls.Add(dashboard);
            content.Controls.Add(students);
            content.Controls.Add(details);
            dashboard.ViewAllRequested += (_, _) => ShowView(students, navStudents, "Students");
            students.StudentRequested += async (_, id) =>
            {
                ShowView(details, navStudents, "Student details", refresh: false);
                await details.ShowStudentAsync(id);
            };
            details.BackRequested += (_, _) => ShowView(students, navStudents, "Students");

            // ---------- Sidebar ----------
            navDashboard = new NavItem("Dashboard") { Dock = DockStyle.Fill };
            navStudents = new NavItem("Students") { Dock = DockStyle.Fill };
            var navAdd = new NavItem("Add student") { Dock = DockStyle.Fill };
            navDashboard.Click += (_, _) => ShowView(dashboard, navDashboard, "Dashboard");
            navStudents.Click += (_, _) => ShowView(students, navStudents, "Students");
            navAdd.Click += async (_, _) =>
            {
                ShowView(students, navStudents, "Students");
                await students.AddStudentAsync();
            };

            var sidebar = new TableLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 232,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Theme.Sidebar
            };
            sidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // brand
            sidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // dashboard
            sidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // students
            sidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // add student
            sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));     // spacer

            var brand = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = new Padding(26, 28, 20, 26)
            };
            brand.Controls.Add(Ui.Text("ClassTrack", Theme.H2, Theme.Text));
            brand.Controls.Add(Ui.Text("Student management", Theme.Small, Theme.TextMuted, new Padding(0, 2, 0, 0)));

            sidebar.Controls.Add(brand, 0, 0);
            sidebar.Controls.Add(navDashboard, 0, 1);
            sidebar.Controls.Add(navStudents, 0, 2);
            sidebar.Controls.Add(navAdd, 0, 3);
            sidebar.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
            };

            // Dock order: last added docks first, so the sidebar takes the left edge and the pages fill the rest.
            Controls.Add(content);
            Controls.Add(sidebar);
        }

        /// <summary>Shows one page, highlights its nav item and reloads its data.</summary>
        private void ShowView(ViewBase view, NavItem nav, string title, bool refresh = true)
        {
            dashboard.Visible = view == dashboard;
            students.Visible = view == students;
            details.Visible = view == details;
            navDashboard.Active = nav == navDashboard;
            navStudents.Active = nav == navStudents;
            Text = $"{title} - ClassTrack";
            if (refresh) _ = view.RefreshDataAsync();   // database errors are handled inside the view
        }
    }
}
