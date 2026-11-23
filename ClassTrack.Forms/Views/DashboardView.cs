using ClassTrack.Data;
using ClassTrack.UI;
using ClassTrack.UI.Controls;

namespace ClassTrack.Views
{
    /// <summary>
    /// Home page. A calm welcome, one overview band, two distributions (course bars, year ring)
    /// and the most recently added students.
    /// </summary>
    public sealed class DashboardView : ViewBase
    {
        private const int RecentCount = 6;
        private const int AvatarSize = 40;

        private readonly StudentRepository repo = new();
        private readonly ScrollHost page = new();

        private readonly Label lblDate = Ui.Text("", Theme.Caption, Theme.AccentSoft);
        private readonly Label lblWelcome = Ui.Text("", Theme.Hero, Theme.Text);
        private readonly StatBlock statTotal = new("Total students", divider: false);
        private readonly StatBlock statCourses = new("Courses", divider: true);
        private readonly StatBlock statYears = new("Year levels", divider: true);
        private readonly StatBlock statLatest = new("Latest student", divider: true, text: true);
        private readonly BarList byCourse = new();
        private readonly DonutChart byYear = new();
        private readonly RecentList recent = new();
        private readonly List<Bitmap> avatars = new();

        /// <summary>Raised when the user asks to see the full student list.</summary>
        public event EventHandler? ViewAllRequested;

        /// <summary>Raised with a student's ID when a recent row is opened.</summary>
        public event EventHandler<int>? StudentRequested;

        public DashboardView()
        {
            Controls.Add(page);
            page.AddRow(BuildWelcome());
            page.AddRow(BuildOverview());
            page.AddRow(BuildDistributions());
            page.AddRow(BuildRecent());
            recent.RowActivated += (_, id) => StudentRequested?.Invoke(this, id);
        }

        // =====================================================================
        //  Sections
        // =====================================================================

        private Control BuildWelcome()
        {
            var row = new TablePanel { BackColor = Color.Transparent, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 6, 0, 26) };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var text = new FlowPanel { BackColor = Color.Transparent, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = Padding.Empty };
            lblDate.Margin = new Padding(3, 0, 0, 4);
            var blurb = Ui.Text("ClassTrack keeps every student record, contact and enrolment in one place. " +
                                "Here is how your classes look today.", Theme.Body, Theme.TextMuted, new Padding(3, 8, 0, 0));
            blurb.MaximumSize = new Size(560, 0);
            text.Controls.Add(lblDate);
            text.Controls.Add(lblWelcome);
            text.Controls.Add(blurb);

            var open = new FlatButton("Student directory", ButtonKind.Primary) { Icon = IconKind.Students, Anchor = AnchorStyles.Right | AnchorStyles.Top, Margin = new Padding(0, 14, 0, 0) };
            open.Click += (_, _) => ViewAllRequested?.Invoke(this, EventArgs.Empty);

            row.Controls.Add(text, 0, 0);
            row.Controls.Add(open, 1, 0);

            string name = Environment.UserName;
            lblWelcome.Text = $"Welcome back, {(name.Length > 0 ? char.ToUpper(name[0]) + name[1..] : "there")}";
            return row;
        }

        /// <summary>One wide glass band with four figures, separated by hairlines.</summary>
        private Control BuildOverview()
        {
            var band = new GlassPanel { AutoSize = true, Padding = new Padding(26, 22, 22, 20), Margin = new Padding(0, 0, 0, 20) };
            var grid = new TablePanel { BackColor = Color.Transparent, Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 4 };
            for (int i = 0; i < 4; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            grid.Controls.Add(statTotal, 0, 0);
            grid.Controls.Add(statCourses, 1, 0);
            grid.Controls.Add(statYears, 2, 0);
            grid.Controls.Add(statLatest, 3, 0);
            band.Controls.Add(grid);
            return band;
        }

        private Control BuildDistributions()
        {
            var row = new TablePanel { BackColor = Color.Transparent, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 0, 0, 20) };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            row.Controls.Add(Panel("By course", "Where your students are enrolled", byCourse, new Padding(0, 0, 10, 0)), 0, 0);
            row.Controls.Add(Panel("By year level", "How the cohort is spread", byYear, new Padding(10, 0, 0, 0)), 1, 0);
            return row;
        }

        private static GlassPanel Panel(string title, string caption, Control body, Padding margin)
        {
            var panel = new GlassPanel { Dock = DockStyle.Fill, AutoSize = true, Margin = margin, Padding = new Padding(26, 22, 26, 22) };
            var stack = new TablePanel { BackColor = Color.Transparent, Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1 };
            stack.Controls.Add(Ui.Text(title, Theme.H2, Theme.Text), 0, 0);
            stack.Controls.Add(Ui.Text(caption, Theme.Small, Theme.TextMuted, new Padding(0, 2, 0, 18)), 0, 1);
            body.Dock = DockStyle.Fill;
            stack.Controls.Add(body, 0, 2);
            panel.Controls.Add(stack);
            return panel;
        }

        private Control BuildRecent()
        {
            var panel = new GlassPanel { AutoSize = true, Padding = new Padding(26, 22, 26, 14), Margin = new Padding(0, 0, 0, 8) };
            var stack = new TablePanel { BackColor = Color.Transparent, Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1 };

            var head = new TablePanel { BackColor = Color.Transparent, Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 0, 0, 10) };
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var titles = new FlowPanel { BackColor = Color.Transparent, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = Padding.Empty };
            titles.Controls.Add(Ui.Text("Recently added", Theme.H2, Theme.Text));
            titles.Controls.Add(Ui.Text("The newest students in your directory", Theme.Small, Theme.TextMuted, new Padding(0, 2, 0, 0)));
            var all = new FlatButton("View all", ButtonKind.Ghost) { Anchor = AnchorStyles.Right };
            all.Click += (_, _) => ViewAllRequested?.Invoke(this, EventArgs.Empty);
            head.Controls.Add(titles, 0, 0);
            head.Controls.Add(all, 1, 0);

            recent.Dock = DockStyle.Fill;
            stack.Controls.Add(head, 0, 0);
            stack.Controls.Add(recent, 0, 1);
            panel.Controls.Add(stack);
            return panel;
        }

        // =====================================================================
        //  Data
        // =====================================================================

        public override async Task RefreshDataAsync()
        {
            lblDate.Text = DateTime.Now.ToString("dddd, d MMMM yyyy").ToUpperInvariant();

            DashboardStats? stats = null;
            if (!await TryDbAsync(async () => stats = await repo.GetDashboardAsync(RecentCount)) || stats == null) return;

            statTotal.Value.Text = stats.Total.ToString();
            statTotal.Note.Text = stats.Total == 1 ? "registered student" : "registered students";

            int courses = stats.ByCourse.Count(c => c.Label != "Unassigned");
            int noCourse = stats.ByCourse.FirstOrDefault(c => c.Label == "Unassigned")?.Count ?? 0;
            statCourses.Value.Text = courses.ToString();
            statCourses.Note.Text = noCourse == 0 ? (courses == 0 ? "none yet" : "all students placed")
                                                  : $"{noCourse} without a course";

            int years = stats.ByYear.Count(y => y.Label != "Unassigned");
            statYears.Value.Text = years.ToString();
            statYears.Note.Text = years == 0 ? "none set yet" : "in use";

            var latest = stats.Recent.FirstOrDefault();
            statLatest.Value.Text = latest?.FullName ?? "—";
            statLatest.Note.Text = latest == null ? "no students yet" : $"ID {latest.Id}";

            byCourse.SetItems(stats.ByCourse, "No students yet.");
            byYear.SetItems(stats.ByYear, "No students yet.");

            foreach (var old in avatars) old.Dispose();
            avatars.Clear();
            var rows = new List<RecentList.Row>();
            foreach (var s in stats.Recent)
            {
                var avatar = Avatar.Create(s, AvatarSize, AvatarSize / 2);
                avatars.Add(avatar);
                rows.Add(new RecentList.Row(s.Id, s.FullName,
                    $"ID {s.Id}  ·  {s.Course ?? "No course"}", s.YearLevel == null ? "—" : $"Year {s.YearLevel}", avatar));
            }
            recent.SetRows(rows, "No students yet. Add one to see them here.");
            page.Fit();
        }
    }
}
