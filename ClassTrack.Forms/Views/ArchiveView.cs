using ClassTrack.Data;
using ClassTrack.UI;
using ClassTrack.UI.Controls;

namespace ClassTrack.Views
{
    /// <summary>
    /// Archived students: records that were "deleted" from the directory but are kept. From here a student can be
    /// restored to the directory or removed permanently (with their picture).
    /// </summary>
    public sealed class ArchiveView : ViewBase
    {
        private const int PageSize = 10;
        private const int AvatarSize = 34;

        private readonly StudentRepository repo = new();
        private readonly StudentActions actions;
        private readonly List<Bitmap> avatars = new();
        private readonly System.Windows.Forms.Timer searchDelay = new() { Interval = 320 };

        private string? appliedTerm;
        private int page = 1;
        private int totalPages = 1;
        private int loadVersion;           // lets a slow, older load be ignored

        private readonly PageHeader header = new("Archive", "");
        private readonly DarkTextBox txtSearch = new();
        private readonly DarkGrid grid = new();
        private readonly Label lblPage = Ui.Text("", Theme.Small, Theme.TextMuted);
        private readonly Label lblEmptyTitle = Ui.Text("", Theme.H2, Theme.Text);
        private readonly Label lblEmptyHint = Ui.Text("", Theme.Body, Theme.TextMuted);
        private readonly Panel emptyState = new() { Dock = DockStyle.Fill, BackColor = Theme.TableSurface, Visible = false };
        private readonly FlatButton btnPrev = new("Previous", ButtonKind.Ghost) { Icon = IconKind.ChevronLeft };
        private readonly FlatButton btnNext = new("Next", ButtonKind.Ghost);

        public ArchiveView()
        {
            actions = new StudentActions(repo);
            searchDelay.Tick += async (_, _) => { searchDelay.Stop(); await SearchAsync(); };

            var root = new TablePanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Color.Transparent };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // header
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // search
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));     // table
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // pager
            root.Controls.Add(header, 0, 0);
            root.Controls.Add(BuildSearch(), 0, 1);
            root.Controls.Add(BuildTable(), 0, 2);
            root.Controls.Add(BuildPager(), 0, 3);
            Controls.Add(root);
        }

        public override Task RefreshDataAsync() => LoadAsync();

        // =====================================================================
        //  Layout
        // =====================================================================

        private Control BuildSearch()
        {
            var bar = new GlassPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(14, 12, 14, 12), Radius = 18, Margin = new Padding(0, 0, 0, 14) };
            txtSearch.Caption = "Archive search";
            txtSearch.LeftIcon = IconKind.Search;
            txtSearch.PlaceholderText = "Search the archive by ID, name, email or course";
            txtSearch.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtSearch.Inner.TextChanged += (_, _) => { searchDelay.Stop(); searchDelay.Start(); };   // search as you type, once you pause
            txtSearch.Inner.KeyDown += async (_, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                searchDelay.Stop();
                await SearchAsync();
            };
            // A layout row gives the auto-sizing panel something to measure (a docked box alone has no height of its own).
            var row = new TablePanel { BackColor = Color.Transparent, Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1 };
            row.Controls.Add(txtSearch, 0, 0);
            bar.Controls.Add(row);
            return bar;
        }

        private Control BuildTable()
        {
            grid.AccessibleName = "Archived students table";
            grid.Columns.Add(new DataGridViewImageColumn
            {
                Name = "colPhoto", HeaderText = "", Width = AvatarSize + 30, Resizable = DataGridViewTriState.False,
                SortMode = DataGridViewColumnSortMode.NotSortable, ImageLayout = DataGridViewImageCellLayout.Normal,
                DefaultCellStyle = { Padding = new Padding(14, 0, 0, 0), Alignment = DataGridViewContentAlignment.MiddleLeft }
            });
            AddText("colId", "ID", 76, 0);
            AddText("colFirst", "FIRST NAME", 0, 24);
            AddText("colSurname", "SURNAME", 0, 24);
            AddText("colCourse", "COURSE", 0, 34);
            AddText("colArchived", "ARCHIVED", 150, 0);
            grid.Columns.Add(DarkGrid.LinkColumn("colRestore", "Restore", Theme.AccentSoft));
            grid.Columns.Add(DarkGrid.LinkColumn("colForever", "Delete forever", Theme.Danger));
            grid.CellContentClick += Grid_CellContentClick;

            var stack = new FlowPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Anchor = AnchorStyles.None, BackColor = Theme.TableSurface };
            lblEmptyTitle.Anchor = AnchorStyles.None;
            lblEmptyHint.Anchor = AnchorStyles.None;
            lblEmptyTitle.BackColor = Theme.TableSurface;
            lblEmptyHint.BackColor = Theme.TableSurface;
            lblEmptyHint.Margin = new Padding(0, 4, 0, 0);
            stack.Controls.Add(lblEmptyTitle);
            stack.Controls.Add(lblEmptyHint);
            var center = new TablePanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 1, BackColor = Theme.TableSurface };
            center.Controls.Add(stack, 0, 0);
            emptyState.Controls.Add(center);

            var panel = new GlassPanel { Dock = DockStyle.Fill, Solid = true, Padding = new Padding(10, 8, 10, 12), Radius = 20 };
            panel.Controls.Add(grid);
            panel.Controls.Add(emptyState);
            return panel;

            void AddText(string name, string headerText, int width, float fill)
            {
                var col = new DataGridViewTextBoxColumn
                {
                    Name = name, HeaderText = headerText, SortMode = DataGridViewColumnSortMode.NotSortable,
                    MinimumWidth = DarkGrid.HeaderWidth(headerText, 26)
                };
                if (width > 0) { col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; col.Width = width; }
                else { col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; col.FillWeight = fill; }
                grid.Columns.Add(col);
            }
        }

        private Control BuildPager()
        {
            var pager = new TablePanel { BackColor = Color.Transparent, Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 4, Margin = new Padding(0, 14, 0, 0) };
            pager.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pager.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pager.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pager.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            btnPrev.Margin = new Padding(0, 0, 8, 0);
            btnPrev.Click += async (_, _) => { page = Math.Max(1, page - 1); await LoadAsync(); };
            btnNext.Margin = new Padding(8, 0, 0, 0);
            btnNext.Click += async (_, _) => { page = Math.Min(totalPages, page + 1); await LoadAsync(); };
            lblPage.Anchor = AnchorStyles.None;
            lblPage.Margin = new Padding(12, 0, 12, 0);
            pager.Controls.Add(btnPrev, 0, 0);
            pager.Controls.Add(lblPage, 1, 0);
            pager.Controls.Add(btnNext, 2, 0);
            return pager;
        }

        // =====================================================================
        //  Loading
        // =====================================================================

        private Task SearchAsync()
        {
            appliedTerm = txtSearch.Text;
            page = 1;
            return LoadAsync();
        }

        private async Task LoadAsync()
        {
            int version = ++loadVersion;
            StudentPage? result = null;
            // Most recently archived first.
            var query = new StudentQuery(appliedTerm, SortBy: "ArchivedAt", Descending: true, Page: page, PageSize: PageSize, Archived: true);
            if (!await TryDbAsync(async () => result = await repo.GetPageAsync(query))) return;
            if (result == null || version != loadVersion) return;   // a newer load is already running

            page = result.Page;
            totalPages = result.TotalPages;

            grid.Rows.Clear();
            foreach (var old in avatars) old.Dispose();
            avatars.Clear();
            foreach (var s in result.Items)
            {
                var avatar = Avatar.Create(s, AvatarSize, AvatarSize / 2);
                avatars.Add(avatar);
                grid.Rows.Add(avatar, s.Id, s.FirstName, s.Surname, s.Course ?? "—", s.ArchivedAt?.ToString("d MMM yyyy") ?? "—");
            }
            grid.ClearSelection();

            bool searching = !string.IsNullOrWhiteSpace(appliedTerm);
            emptyState.Visible = result.Total == 0;
            grid.Visible = result.Total > 0;
            lblEmptyTitle.Text = searching ? $"No archived students match “{appliedTerm!.Trim()}”" : "The archive is empty";
            lblEmptyHint.Text = searching ? "Try a different search." : "Students you archive appear here, and can be restored at any time.";

            string noun = result.Total == 1 ? "student" : "students";
            header.Subtitle = searching ? $"{result.Total} {(result.Total == 1 ? "result" : "results")} for “{appliedTerm!.Trim()}”"
                                        : $"{result.Total} archived {noun}. Archived students are hidden from the directory but never lost.";
            int first = (result.Page - 1) * PageSize + 1;
            lblPage.Text = result.Total == 0 ? "Page 1 of 1"
                : $"Page {result.Page} of {result.TotalPages}  ·  Showing {first}–{first + result.Items.Count - 1} of {result.Total}";
            btnPrev.Enabled = result.Page > 1;
            btnNext.Enabled = result.Page < result.TotalPages;
        }

        // =====================================================================
        //  Row actions
        // =====================================================================

        private async void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string action = grid.Columns[e.ColumnIndex].Name;
            if (action is not ("colRestore" or "colForever")) return;

            int id = (int)grid.Rows[e.RowIndex].Cells["colId"].Value!;
            Student? student = null;
            if (!await TryDbAsync(async () => student = await repo.GetAsync(id))) return;
            if (student == null)
            {
                Dialogs.Info(FindForm(), "Not found", "That student no longer exists.");
                await LoadAsync();
                return;
            }

            bool changed = action == "colRestore"
                ? await actions.RestoreAsync(this, student)
                : await actions.DeleteForeverAsync(this, student);
            if (changed) await LoadAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { searchDelay.Dispose(); foreach (var a in avatars) a.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
