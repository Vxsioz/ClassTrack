using ClassTrack.Data;
using ClassTrack.UI;
using ClassTrack.UI.Controls;

namespace ClassTrack.Views
{
    /// <summary>Home page: headline figures and the most recently added students.</summary>
    public sealed class DashboardView : ViewBase
    {
        private readonly StudentRepository repo = new();
        private readonly StatBlock statTotal = new("Total students", divider: false);
        private readonly StatBlock statLatest = new("Latest student", divider: true);
        private readonly StatBlock statCourses = new("Courses", divider: true);
        private readonly DarkGrid recentGrid = new();
        private readonly Label lblRecentEmpty = Ui.Text("No students yet. Add one to see it here.", Theme.Body, Theme.TextMuted);

        /// <summary>Raised when the user asks to see the full student list.</summary>
        public event EventHandler? ViewAllRequested;

        public DashboardView()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Theme.Bg };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Header
            var header = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 0, 0, 22) };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var btnAll = new FlatButton("View all students", ButtonKind.Secondary) { Anchor = AnchorStyles.Right };
            btnAll.Click += (_, _) => ViewAllRequested?.Invoke(this, EventArgs.Empty);
            header.Controls.Add(Ui.Heading("Dashboard", Ui.Text("Overview of registered students", Theme.Body, Theme.TextMuted)), 0, 0);
            header.Controls.Add(btnAll, 1, 0);

            // Stat strip: one bordered band with dividers, not three separate cards
            var strip = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(1),
                BackColor = Theme.Surface,
                Margin = new Padding(0, 0, 0, 28)
            };
            for (int i = 0; i < 3; i++) strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 3));
            strip.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            strip.Controls.Add(statTotal, 0, 0);
            strip.Controls.Add(statLatest, 1, 0);
            strip.Controls.Add(statCourses, 2, 0);
            strip.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, strip.Width - 1, strip.Height - 1);
            };

            // Recently added
            var recentTitle = Ui.Text("Recently added", Theme.H2, Theme.Text, new Padding(0, 0, 0, 10));

            ConfigureRecentGrid();
            var gridHost = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            lblRecentEmpty.Dock = DockStyle.Top;
            lblRecentEmpty.Visible = false;
            gridHost.Controls.Add(recentGrid);
            gridHost.Controls.Add(lblRecentEmpty);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(strip, 0, 1);
            root.Controls.Add(recentTitle, 0, 2);
            root.Controls.Add(gridHost, 0, 3);
            Controls.Add(root);
        }

        private void ConfigureRecentGrid()
        {
            recentGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "rId", HeaderText = "ID", Width = 90, MinimumWidth = DarkGrid.HeaderWidth("ID"),
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            recentGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "rFirst", HeaderText = "FIRST NAME", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 50,
                MinimumWidth = DarkGrid.HeaderWidth("FIRST NAME"), SortMode = DataGridViewColumnSortMode.NotSortable
            });
            recentGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "rSurname", HeaderText = "SURNAME", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 50,
                MinimumWidth = DarkGrid.HeaderWidth("SURNAME"), SortMode = DataGridViewColumnSortMode.NotSortable
            });
        }

        public override async Task RefreshDataAsync()
        {
            DashboardStats? stats = null;
            if (!await TryDbAsync(async () => stats = await repo.GetDashboardAsync(8)) || stats == null) return;

            statTotal.Value.Text = stats.Total.ToString();
            statTotal.Note.Text = stats.Total == 1 ? "registered student" : "registered students";

            var latest = stats.Recent.FirstOrDefault();
            statLatest.Value.Text = latest?.FullName ?? "—";
            statLatest.Note.Text = latest == null ? "no students yet" : $"ID {latest.Id}";

            int courses = stats.ByCourse.Count(c => c.Label != "Unassigned");
            statCourses.Value.Text = courses.ToString();
            statCourses.Note.Text = courses == 0 ? "none assigned yet" : "with enrolled students";

            recentGrid.Rows.Clear();
            foreach (var s in stats.Recent)
                recentGrid.Rows.Add(s.Id, s.FirstName, s.Surname);
            recentGrid.ClearSelection();

            lblRecentEmpty.Visible = stats.Recent.Count == 0;
            recentGrid.Visible = stats.Recent.Count > 0;
        }
    }
}
