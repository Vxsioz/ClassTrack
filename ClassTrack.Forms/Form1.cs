using ClassTrack.UI;
using ClassTrack.UI.Controls;
using ClassTrack.Views;

namespace ClassTrack
{
    /// <summary>
    /// The main window: a floating glass sidebar, a top bar (page title, search, identity) and one page at a time.
    /// All page logic lives in the Views folder; this form only navigates between them.
    /// </summary>
    public partial class Form1 : DarkForm
    {
        private readonly DashboardView dashboard = new();
        private readonly StudentsView students = new();
        private readonly StudentDetailsView details = new();
        private readonly StudentEditorView editor = new();
        private readonly SettingsView settings = new();

        private NavItem navDashboard = null!;
        private NavItem navStudents = null!;
        private NavItem navAdd = null!;
        private NavItem navSettings = null!;
        private readonly Label lblPageTitle = Ui.Text("", Theme.H2, Theme.Text);
        private readonly Label lblCrumb = Ui.Text("ClassTrack  /", Theme.Body, Theme.TextFaint);
        private readonly DarkTextBox txtGlobalSearch = new();
        private Action? editorReturn;

        public Form1()
        {
            InitializeComponent();
            BuildUi();
            ShowDashboard();
        }

        /// <summary>Keeps the window inside the screen's usable area (above the taskbar) on smaller screens.</summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            var area = Screen.FromControl(this).WorkingArea;
            int width = Math.Min(Width, area.Width), height = Math.Min(Height, area.Height);
            Size = new Size(width, height);
            Location = new Point(
                Math.Clamp(Left, area.Left, Math.Max(area.Left, area.Right - width)),
                Math.Clamp(Top, area.Top, Math.Max(area.Top, area.Bottom - height)));
        }

        // =====================================================================
        //  Shell
        // =====================================================================

        private void BuildUi()
        {
            Text = "ClassTrack";
            ClientSize = new Size(1180, 760);
            MinimumSize = new Size(1060, 660);
            Padding = new Padding(14);

            // ---------- Pages ----------
            var pageHost = new ClearPanel { Dock = DockStyle.Fill };
            foreach (ViewBase view in new ViewBase[] { dashboard, students, details, editor, settings })
            {
                view.Visible = false;
                pageHost.Controls.Add(view);
            }

            WirePages();

            // ---------- Top bar ----------
            var topBar = BuildTopBar();

            var content = new ClearPanel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 0, 0) };
            content.Controls.Add(pageHost);
            content.Controls.Add(topBar);

            // ---------- Sidebar ----------
            var sidebar = BuildSidebar();

            // Dock order: last added docks first, so the sidebar takes the left edge and the pages fill the rest.
            Controls.Add(content);
            Controls.Add(sidebar);
        }

        private Control BuildSidebar()
        {
            navDashboard = new NavItem("Dashboard", IconKind.Dashboard) { Dock = DockStyle.Fill };
            navStudents = new NavItem("Students", IconKind.Students) { Dock = DockStyle.Fill };
            navAdd = new NavItem("Add student", IconKind.AddUser) { Dock = DockStyle.Fill };
            navSettings = new NavItem("Settings", IconKind.Settings) { Dock = DockStyle.Fill };
            navDashboard.Click += (_, _) => ShowDashboard();
            navStudents.Click += (_, _) => ShowStudents();
            navAdd.Click += async (_, _) => await ShowEditor(null, () => ShowStudents());
            navSettings.Click += (_, _) => ShowView(settings, navSettings, "Settings");

            var sidebar = new GlassPanel { Dock = DockStyle.Left, Width = 244, Padding = new Padding(14, 18, 14, 14), Radius = 26 };
            var stack = new TablePanel { BackColor = Color.Transparent, Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7 };
            for (int i = 0; i < 6; i++) stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stack.RowStyles[1] = new RowStyle(SizeType.Absolute, 22F);                 // gap under the logo
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            // rows: 0 logo | 1 gap | 2 dashboard | 3 students | 4 add | 5 (filler) | 6 settings
            stack.RowStyles.Clear();
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var logo = new Logo { Dock = DockStyle.Fill, Margin = new Padding(6, 0, 0, 0) };
            stack.Controls.Add(logo, 0, 0);
            stack.Controls.Add(navDashboard, 0, 2);
            stack.Controls.Add(navStudents, 0, 3);
            stack.Controls.Add(navAdd, 0, 4);
            stack.Controls.Add(navSettings, 0, 6);
            sidebar.Controls.Add(stack);
            return sidebar;
        }

        private Control BuildTopBar()
        {
            var bar = new TablePanel { BackColor = Color.Transparent, Dock = DockStyle.Top, Height = 60, ColumnCount = 3, RowCount = 1 };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340F));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            // Breadcrumb: muted workspace name, then the current page
            var crumbs = new FlowPanel { AutoSize = true, WrapContents = false, Anchor = AnchorStyles.Left, Margin = new Padding(8, 0, 0, 0) };
            lblCrumb.Margin = new Padding(0, 3, 8, 0);
            crumbs.Controls.Add(lblCrumb);
            crumbs.Controls.Add(lblPageTitle);

            txtGlobalSearch.Caption = "Search students";
            txtGlobalSearch.LeftIcon = IconKind.Search;
            txtGlobalSearch.PlaceholderText = "Search students";
            txtGlobalSearch.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtGlobalSearch.Margin = new Padding(0, 0, 18, 0);
            txtGlobalSearch.Inner.KeyDown += async (_, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                string term = txtGlobalSearch.Text.Trim();
                txtGlobalSearch.Text = "";
                ShowStudents(refresh: false);
                await students.SetSearchAsync(term);
            };

            // Identity: who is using this workspace
            string name = Environment.UserName.Length > 0 ? Environment.UserName : "ClassTrack";
            string display = char.ToUpper(name[0]) + name[1..];
            var identity = new FlowPanel { BackColor = Color.Transparent, AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight, Anchor = AnchorStyles.Right, Margin = new Padding(0, 0, 4, 0) };
            var avatar = new GlassPicture
            {
                Size = new Size(38, 38),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 10, 0),
                Image = Avatar.CreateFrom(null, display[..1], 38, 19)
            };
            var labels = new FlowPanel { BackColor = Color.Transparent, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, 1, 0, 0) };
            labels.Controls.Add(Ui.Text(display, Theme.H3, Theme.Text));
            labels.Controls.Add(Ui.Text("Local workspace", Theme.Small, Theme.TextMuted));
            identity.Controls.Add(avatar);
            identity.Controls.Add(labels);

            bar.Controls.Add(crumbs, 0, 0);
            bar.Controls.Add(txtGlobalSearch, 1, 0);
            bar.Controls.Add(identity, 2, 0);
            return bar;
        }

        // =====================================================================
        //  Navigation
        // =====================================================================

        private void WirePages()
        {
            dashboard.ViewAllRequested += (_, _) => ShowStudents();
            dashboard.StudentRequested += async (_, id) => await ShowDetails(id);

            students.StudentRequested += async (_, id) => await ShowDetails(id);
            students.EditRequested += async (_, id) => await ShowEditor(id, () => ShowStudents());
            students.AddRequested += async (_, _) => await ShowEditor(null, () => ShowStudents());

            details.BackRequested += (_, _) => ShowStudents();
            details.EditRequested += async (_, id) => await ShowEditor(id, () => _ = ShowDetails(id));

            editor.Saved += async (_, id) => await ShowDetails(id);
            editor.Cancelled += (_, _) => (editorReturn ?? (() => ShowStudents()))();
        }

        private void ShowDashboard() => ShowView(dashboard, navDashboard, "Dashboard");

        private void ShowStudents(bool refresh = true) => ShowView(students, navStudents, "Students", refresh);

        private async Task ShowDetails(int id)
        {
            ShowView(details, navStudents, "Student details", refresh: false);
            await details.ShowStudentAsync(id);
        }

        /// <summary>Opens the Add (id == null) or Edit page. <paramref name="back"/> runs if the user cancels.</summary>
        private async Task ShowEditor(int? id, Action back)
        {
            editorReturn = back;
            ShowView(editor, id == null ? navAdd : navStudents, id == null ? "Add student" : "Edit student", refresh: false);
            await editor.LoadAsync(id);
        }

        /// <summary>Shows one page, highlights its nav item, reloads its data and plays the entrance.</summary>
        private void ShowView(ViewBase view, NavItem nav, string title, bool refresh = true)
        {
            dashboard.Visible = view == dashboard;
            students.Visible = view == students;
            details.Visible = view == details;
            editor.Visible = view == editor;
            settings.Visible = view == settings;
            foreach (var item in new[] { navDashboard, navStudents, navAdd, navSettings }) item.Active = item == nav;
            lblPageTitle.Text = title;
            Text = $"{title} - ClassTrack";
            view.PlayEntrance();
            if (refresh) _ = view.RefreshDataAsync();   // database errors are handled inside the view
        }
    }
}
