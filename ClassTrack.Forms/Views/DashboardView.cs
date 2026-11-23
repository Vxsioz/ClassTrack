using ClassTrack.Data;
using ClassTrack.UI;
using ClassTrack.UI.Controls;

namespace ClassTrack.Views
{
    internal sealed class DarkScrollPanel : Panel
    {
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.ApplyDarkScrollbars(Handle);
        }
    }

    /// <summary>Home page: headline figures, students by course and year level, and the latest additions.</summary>
    public sealed class DashboardView : ViewBase
    {
        private const int RecentCount = 6;
        private const int AvatarSize = 32;

        private readonly StudentRepository repo = new();
        private readonly StatBlock statTotal = new("Total students", divider: false);
        private readonly StatBlock statCourses = new("Courses", divider: true);
        private readonly StatBlock statLatest = new("Latest student", divider: true);
        private readonly BarList byCourse = new();
        private readonly BarList byYear = new();
        private readonly DarkGrid recentGrid = new() { Dock = DockStyle.Top };
        private readonly Panel recentHost = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
        private readonly Label lblRecentEmpty = Ui.Text("No students yet. Add one to see it here.", Theme.Body, Theme.TextMuted);
        private readonly List<Bitmap> avatars = new();
        private readonly Action fitContent;

        /// <summary>Raised when the user asks to see the full student list.</summary>
        public event EventHandler? ViewAllRequested;

        /// <summary>Raised with a student's ID when a recent row is opened.</summary>
        public event EventHandler<int>? StudentRequested;

        public DashboardView()
        {
            // Everything sits in a scrolling panel so a short window never cuts content off.
            var scroller = new DarkScrollPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.Bg };

            var root = new TableLayoutPanel
            {
                Location = Point.Empty,
                ColumnCount = 1,
                BackColor = Theme.Bg
            };

            root.Controls.Add(BuildHeader());
            root.Controls.Add(BuildStatStrip());
            root.Controls.Add(BuildBreakdowns());
            root.Controls.Add(Ui.Text("Recently added", Theme.H2, Theme.Text, new Padding(0, 0, 0, 10)));
            root.Controls.Add(BuildRecent());

            foreach (Control c in root.Controls) c.Dock = DockStyle.Top;
            scroller.Controls.Add(root);
            // The content is exactly as wide as the visible area (so a scrollbar never clips it) and as tall
            // as it needs to be (so the panel scrolls). AutoSize can't do both, so size it here.
            fitContent = () =>
            {
                int w = scroller.ClientSize.Width;
                if (w <= 0) return;
                root.Size = new Size(w, root.GetPreferredSize(new Size(w, 0)).Height);
            };
            scroller.ClientSizeChanged += (_, _) => fitContent();
            Controls.Add(scroller);
            fitContent();
        }

        private Control BuildHeader()
        {
            var header = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 0, 0, 22) };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var btnAll = new FlatButton("View all students") { Anchor = AnchorStyles.Right };
            btnAll.Click += (_, _) => ViewAllRequested?.Invoke(this, EventArgs.Empty);
            header.Controls.Add(Ui.Heading("Dashboard", Ui.Text("Overview of registered students", Theme.Body, Theme.TextMuted)), 0, 0);
            header.Controls.Add(btnAll, 1, 0);
            return header;
        }

        /// <summary>One bordered band with dividers, not three separate cards.</summary>
        private Control BuildStatStrip()
        {
            var strip = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(1),
                BackColor = Theme.Surface,
                Margin = new Padding(0, 0, 0, 30)
            };
            for (int i = 0; i < 3; i++) strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 3));
            strip.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            strip.Controls.Add(statTotal, 0, 0);
            strip.Controls.Add(statCourses, 1, 0);
            strip.Controls.Add(statLatest, 2, 0);
            strip.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, strip.Width - 1, strip.Height - 1);
            };
            return strip;
        }

        private Control BuildBreakdowns()
        {
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 0, 0, 30) };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            byCourse.Margin = new Padding(0, 0, 40, 0);
            byYear.Margin = Padding.Empty;
            grid.Controls.Add(Ui.Text("By course", Theme.H2, Theme.Text, new Padding(0, 0, 0, 12)), 0, 0);
            grid.Controls.Add(Ui.Text("By year level", Theme.H2, Theme.Text, new Padding(0, 0, 0, 12)), 1, 0);
            grid.Controls.Add(byCourse, 0, 1);
            grid.Controls.Add(byYear, 1, 1);
            return grid;
        }

        private Control BuildRecent()
        {
            recentGrid.Columns.Add(new DataGridViewImageColumn
            {
                Name = "rPhoto", HeaderText = "", Width = AvatarSize + 28, Resizable = DataGridViewTriState.False,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = { Padding = new Padding(12, 0, 0, 0), Alignment = DataGridViewContentAlignment.MiddleLeft }
            });
            AddText("rId", "ID", 76, 0);
            AddText("rFirst", "FIRST NAME", 0, 24);
            AddText("rSurname", "SURNAME", 0, 24);
            AddText("rCourse", "COURSE", 0, 40);
            AddText("rYear", "YEAR", 84, 0);

            // Rows are not interactive buttons; double-click opens the student's page.
            recentGrid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0) StudentRequested?.Invoke(this, (int)recentGrid.Rows[e.RowIndex].Cells["rId"].Value!);
            };

            lblRecentEmpty.Dock = DockStyle.Top;
            lblRecentEmpty.Visible = false;
            recentHost.Controls.Add(recentGrid);
            recentHost.Controls.Add(lblRecentEmpty);
            return recentHost;

            void AddText(string name, string header, int width, float fill)
            {
                var col = new DataGridViewTextBoxColumn
                {
                    Name = name, HeaderText = header, SortMode = DataGridViewColumnSortMode.NotSortable,
                    MinimumWidth = DarkGrid.HeaderWidth(header, 24)
                };
                if (width > 0) { col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; col.Width = width; }
                else { col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; col.FillWeight = fill; }
                recentGrid.Columns.Add(col);
            }
        }

        public override async Task RefreshDataAsync()
        {
            DashboardStats? stats = null;
            if (!await TryDbAsync(async () => stats = await repo.GetDashboardAsync(RecentCount)) || stats == null) return;

            // Headline figures
            statTotal.Value.Text = stats.Total.ToString();
            statTotal.Note.Text = stats.Total == 1 ? "registered student" : "registered students";

            int courses = stats.ByCourse.Count(c => c.Label != "Unassigned");
            int noCourse = stats.ByCourse.FirstOrDefault(c => c.Label == "Unassigned")?.Count ?? 0;
            statCourses.Value.Text = courses.ToString();
            statCourses.Note.Text = noCourse == 0 ? (courses == 0 ? "none yet" : "all students assigned")
                                                  : $"{noCourse} {(noCourse == 1 ? "student has" : "students have")} no course";

            var latest = stats.Recent.FirstOrDefault();
            statLatest.Value.Text = latest?.FullName ?? "—";
            statLatest.Note.Text = latest == null ? "no students yet" : $"ID {latest.Id}";

            // Breakdowns
            byCourse.SetItems(stats.ByCourse, "No students yet.");
            byYear.SetItems(stats.ByYear, "No students yet.");

            // Recent
            recentGrid.Rows.Clear();
            foreach (var old in avatars) old.Dispose();
            avatars.Clear();
            foreach (var s in stats.Recent)
            {
                var avatar = Avatar.Create(s, AvatarSize, AvatarSize / 2);
                avatars.Add(avatar);
                recentGrid.Rows.Add(avatar, s.Id, s.FirstName, s.Surname,
                    s.Course ?? "—", s.YearLevel == null ? "—" : $"Year {s.YearLevel}");
            }
            recentGrid.ClearSelection();

            // The grid shows every row (no inner scrollbar); the page scrolls instead.
            recentGrid.Height = recentGrid.ColumnHeadersHeight + stats.Recent.Count * recentGrid.RowTemplate.Height + 2;
            recentGrid.Visible = stats.Recent.Count > 0;
            lblRecentEmpty.Visible = stats.Recent.Count == 0;
            recentHost.Height = stats.Recent.Count == 0 ? lblRecentEmpty.Height : recentGrid.Height;
            fitContent();   // content height changed
        }
    }
}
