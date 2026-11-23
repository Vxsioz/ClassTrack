using System.Text;
using System.Text.RegularExpressions;
using ClassTrack.Data;
using ClassTrack.UI;
using ClassTrack.UI.Controls;

namespace ClassTrack.Views
{
    /// <summary>The student directory: search, course/year filters, sort, page, and per-row View / Edit / Delete.</summary>
    public sealed class StudentsView : ViewBase
    {
        private const int PageSize = 10;
        private const int AvatarSize = 34;
        private static readonly Regex PlainPhone = new(@"^\+?[0-9 ()\-]+$");

        private readonly StudentRepository repo = new();
        private readonly StudentActions actions;

        // List state: search, filters, sort and page are kept together so they survive each other.
        private string? appliedTerm;
        private string? selectedCourse;
        private byte? selectedYear;
        private string sortBy = "Id";      // Id | FirstName | Surname | Course | YearLevel
        private bool sortDescending;
        private int page = 1;
        private int totalPages = 1;
        private int loadVersion;           // lets a slow, older load be ignored
        private bool updatingFilters;      // true while we fill the filter boxes ourselves
        private readonly List<Bitmap> avatars = new();
        private readonly System.Windows.Forms.Timer searchDelay = new() { Interval = 320 };

        private readonly PageHeader header = new("Students", "");
        private readonly DarkTextBox txtSearch = new();
        private readonly DarkSelect cboCourse = new();
        private readonly DarkSelect cboYear = new();
        private readonly DarkGrid grid = new();
        private readonly Label lblPage = Ui.Text("", Theme.Small, Theme.TextMuted);
        private readonly Label lblEmptyTitle = Ui.Text("", Theme.H2, Theme.Text);
        private readonly Label lblEmptyHint = Ui.Text("", Theme.Body, Theme.TextMuted);
        private readonly Panel emptyState = new() { Dock = DockStyle.Fill, BackColor = Theme.TableSurface, Visible = false };
        private readonly FlatButton btnPrev = new("Previous", ButtonKind.Ghost) { Icon = IconKind.ChevronLeft };
        private readonly FlatButton btnNext = new("Next", ButtonKind.Ghost);

        /// <summary>Raised with a student's ID when the user opens their details.</summary>
        public event EventHandler<int>? StudentRequested;

        /// <summary>Raised with a student's ID when the user chooses Edit.</summary>
        public event EventHandler<int>? EditRequested;

        /// <summary>Raised when the user chooses Add student.</summary>
        public event EventHandler? AddRequested;

        public StudentsView()
        {
            actions = new StudentActions(repo);
            searchDelay.Tick += async (_, _) => { searchDelay.Stop(); await SearchAsync(); };

            var root = new TablePanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Color.Transparent };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // header
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // toolbar
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));     // table
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // pager
            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildToolbar(), 0, 1);
            root.Controls.Add(BuildTable(), 0, 2);
            root.Controls.Add(BuildPager(), 0, 3);
            Controls.Add(root);
        }

        public override Task RefreshDataAsync() => ReloadAsync();

        /// <summary>Applies a search typed elsewhere (the top bar) and shows the results.</summary>
        public Task SetSearchAsync(string term)
        {
            txtSearch.Text = term;
            appliedTerm = term;
            page = 1;
            return ReloadAsync();
        }

        // =====================================================================
        //  Layout
        // =====================================================================

        private Control BuildHeader()
        {
            var btnExport = new FlatButton("Export CSV");
            btnExport.Click += async (_, _) => await ExportCsvAsync();
            var btnAdd = new FlatButton("Add student", ButtonKind.Primary) { Icon = IconKind.Plus };
            btnAdd.Click += (_, _) => AddRequested?.Invoke(this, EventArgs.Empty);
            header.AddAction(btnExport);
            header.AddAction(btnAdd);
            return header;
        }

        private Control BuildToolbar()
        {
            var bar = new GlassPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(14, 12, 14, 12), Radius = 18, Margin = new Padding(0, 0, 0, 14) };
            var row = new TablePanel { BackColor = Color.Transparent, Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 4 };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            txtSearch.Caption = "Directory search";
            cboCourse.AccessibleName = "Course filter";
            cboYear.AccessibleName = "Year filter";
            grid.AccessibleName = "Students table";
            txtSearch.LeftIcon = IconKind.Search;
            txtSearch.PlaceholderText = "Search by ID, name, email or course";
            txtSearch.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtSearch.Inner.TextChanged += (_, _) =>
            {
                if (updatingFilters) return;
                searchDelay.Stop();
                searchDelay.Start();             // search as you type, once you pause
            };
            txtSearch.Inner.KeyDown += async (_, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                searchDelay.Stop();
                await SearchAsync();             // Enter searches immediately
            };

            cboCourse.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cboCourse.Margin = new Padding(10, 0, 0, 0);
            cboYear.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cboYear.Margin = new Padding(10, 0, 0, 0);
            cboYear.Items.Add("All years");
            for (int y = 1; y <= Student.MaxYearLevel; y++) cboYear.Items.Add($"Year {y}");
            cboYear.SelectedIndex = 0;

            cboCourse.SelectedIndexChanged += async (_, _) =>
            {
                if (updatingFilters) return;
                selectedCourse = cboCourse.SelectedIndex > 0 ? cboCourse.Text : null;
                page = 1;
                await LoadAsync();
            };
            cboYear.SelectedIndexChanged += async (_, _) =>
            {
                if (updatingFilters) return;
                selectedYear = cboYear.SelectedIndex > 0 ? (byte)cboYear.SelectedIndex : null;
                page = 1;
                await LoadAsync();
            };

            var btnClear = new FlatButton("Clear", ButtonKind.Ghost) { Anchor = AnchorStyles.Left, Margin = new Padding(10, 0, 0, 0) };
            btnClear.Click += async (_, _) => await ClearAsync();

            row.Controls.Add(txtSearch, 0, 0);
            row.Controls.Add(cboCourse, 1, 0);
            row.Controls.Add(cboYear, 2, 0);
            row.Controls.Add(btnClear, 3, 0);
            bar.Controls.Add(row);
            return bar;
        }

        private Control BuildTable()
        {
            ConfigureGrid();

            // The empty state sits on top of the grid and is shown instead of it.
            var stack = new FlowPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Anchor = AnchorStyles.None,
                BackColor = Theme.TableSurface
            };
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

            // Solid glass: tables need a calm, dense surface for readability.
            var panel = new GlassPanel { Dock = DockStyle.Fill, Solid = true, Padding = new Padding(10, 8, 10, 12), Radius = 20 };
            panel.Controls.Add(grid);
            panel.Controls.Add(emptyState);
            return panel;
        }

        private Control BuildPager()
        {
            var pager = new TablePanel { BackColor = Color.Transparent, Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 4, Margin = new Padding(0, 14, 0, 0) };
            pager.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pager.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pager.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pager.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            btnPrev.Margin = new Padding(0, 0, 8, 0);
            btnPrev.Click += async (_, _) => await GoToPageAsync(page - 1);
            btnNext.Margin = new Padding(8, 0, 0, 0);
            btnNext.Click += async (_, _) => await GoToPageAsync(page + 1);
            lblPage.Anchor = AnchorStyles.None;
            lblPage.Margin = new Padding(12, 0, 12, 0);

            pager.Controls.Add(btnPrev, 0, 0);
            pager.Controls.Add(lblPage, 1, 0);
            pager.Controls.Add(btnNext, 2, 0);
            return pager;
        }

        private void ConfigureGrid()
        {
            // Sortable headers are sorted in the query (Programmatic), not by the grid itself.
            grid.Columns.Add(new DataGridViewImageColumn
            {
                Name = "colPhoto", HeaderText = "",
                Width = AvatarSize + 30, Resizable = DataGridViewTriState.False,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ImageLayout = DataGridViewImageCellLayout.Normal,
                DefaultCellStyle = { Padding = new Padding(14, 0, 0, 0), Alignment = DataGridViewContentAlignment.MiddleLeft }
            });
            AddText("colId", "ID", 76, 0, "Id");
            AddText("colFirst", "FIRST NAME", 0, 24, "FirstName");
            AddText("colSurname", "SURNAME", 0, 24, "Surname");
            AddText("colCourse", "COURSE", 0, 40, "Course");
            AddText("colYear", "YEAR", 84, 0, "YearLevel");

            grid.Columns.Add(DarkGrid.LinkColumn("colView", "View", Theme.TextMuted));
            grid.Columns.Add(DarkGrid.LinkColumn("colEdit", "Edit", Theme.AccentSoft));
            grid.Columns.Add(DarkGrid.LinkColumn("colDelete", "Delete", Theme.Danger));

            grid.CellContentClick += Grid_CellContentClick;
            grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;
            grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0 && grid.Columns[e.ColumnIndex].Name is not ("colView" or "colEdit" or "colDelete"))
                    StudentRequested?.Invoke(this, RowId(e.RowIndex));
            };

            void AddText(string name, string headerText, int width, float fill, string sortKey)
            {
                var col = new DataGridViewTextBoxColumn
                {
                    Name = name, HeaderText = headerText, Tag = sortKey,
                    SortMode = DataGridViewColumnSortMode.Programmatic,
                    MinimumWidth = DarkGrid.HeaderWidth(headerText, 26)
                };
                if (width > 0) { col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; col.Width = width; }
                else { col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; col.FillWeight = fill; }
                grid.Columns.Add(col);
            }
        }

        private int RowId(int rowIndex) => (int)grid.Rows[rowIndex].Cells["colId"].Value!;

        // =====================================================================
        //  Loading (search + filters + sort + paging)
        // =====================================================================

        /// <summary>Refreshes the course filter list, then the current page.</summary>
        private async Task ReloadAsync()
        {
            List<string> courses = new();
            if (!await TryDbAsync(async () => courses = await repo.GetCoursesAsync())) return;
            PopulateCourseFilter(courses);
            await LoadAsync();
        }

        private void PopulateCourseFilter(List<string> courses)
        {
            updatingFilters = true;
            cboCourse.Items.Clear();
            cboCourse.Items.Add("All courses");
            foreach (var c in courses) cboCourse.Items.Add(c);

            int index = selectedCourse == null ? 0 : cboCourse.Items.IndexOf(selectedCourse);
            if (index < 0) { selectedCourse = null; index = 0; }   // that course no longer exists
            cboCourse.SelectedIndex = index;
            updatingFilters = false;
        }

        private async Task LoadAsync()
        {
            int version = ++loadVersion;
            StudentPage? result = null;

            var query = new StudentQuery(appliedTerm, selectedCourse, selectedYear, sortBy, sortDescending, page, PageSize);
            if (!await TryDbAsync(async () => result = await repo.GetPageAsync(query)))
                return;
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
                grid.Rows.Add(avatar, s.Id, s.FirstName, s.Surname,
                    s.Course ?? "—", s.YearLevel == null ? "—" : $"Year {s.YearLevel}");
            }
            grid.ClearSelection();

            UpdateSortGlyphs();
            UpdateStatus(result);
        }

        private void UpdateSortGlyphs()
        {
            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (col.Tag is not string key) continue;
                col.HeaderCell.SortGlyphDirection =
                    sortBy != key ? SortOrder.None :
                    sortDescending ? SortOrder.Descending : SortOrder.Ascending;
            }
        }

        private void UpdateStatus(StudentPage result)
        {
            bool searching = !string.IsNullOrWhiteSpace(appliedTerm);
            bool filtering = selectedCourse != null || selectedYear != null;

            // Empty states: tell the user why the table is empty and what to do.
            emptyState.Visible = result.Total == 0;
            grid.Visible = result.Total > 0;
            if (result.Total == 0)
            {
                lblEmptyTitle.Text = searching ? $"No students match “{appliedTerm!.Trim()}”"
                                   : filtering ? "No students match these filters"
                                   : "No students yet";
                lblEmptyHint.Text = searching || filtering
                    ? "Try a different search or filter, or use Clear to see everyone."
                    : "Add your first student to get started.";
            }

            string noun = result.Total == 1 ? "student" : "students";
            header.Subtitle = searching
                ? $"{result.Total} {(result.Total == 1 ? "result" : "results")} for “{appliedTerm!.Trim()}”"
                : filtering ? $"{result.Total} {noun} match the filters"
                : $"{result.Total} registered {noun}";

            int first = (result.Page - 1) * PageSize + 1;
            lblPage.Text = result.Total == 0
                ? "Page 1 of 1"
                : $"Page {result.Page} of {result.TotalPages}  ·  Showing {first}–{first + result.Items.Count - 1} of {result.Total}";
            btnPrev.Enabled = result.Page > 1;
            btnNext.Enabled = result.Page < result.TotalPages;
        }

        private Task GoToPageAsync(int newPage)
        {
            page = Math.Clamp(newPage, 1, totalPages);
            return LoadAsync();
        }

        private Task SearchAsync()
        {
            appliedTerm = txtSearch.Text;
            page = 1;            // a new search starts on page 1; sort order and filters are kept
            return LoadAsync();
        }

        /// <summary>Clears the search and both filters.</summary>
        private Task ClearAsync()
        {
            searchDelay.Stop();
            updatingFilters = true;
            txtSearch.Text = "";
            appliedTerm = null;
            selectedCourse = null;
            selectedYear = null;
            cboCourse.SelectedIndex = cboCourse.Items.Count > 0 ? 0 : -1;
            cboYear.SelectedIndex = 0;
            updatingFilters = false;
            page = 1;
            return LoadAsync();
        }

        private async void Grid_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (grid.Columns[e.ColumnIndex].Tag is not string key) return;

            if (sortBy == key)
                sortDescending = !sortDescending;   // same column: flip direction
            else
            {
                sortBy = key;                       // new column: start ascending
                sortDescending = false;
            }

            page = 1;                               // search and filters are kept
            await LoadAsync();
        }

        // =====================================================================
        //  Row actions
        // =====================================================================

        private async void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string action = grid.Columns[e.ColumnIndex].Name;
            if (action is not ("colView" or "colEdit" or "colDelete")) return;

            int id = RowId(e.RowIndex);
            if (action == "colView") { StudentRequested?.Invoke(this, id); return; }
            if (action == "colEdit") { EditRequested?.Invoke(this, id); return; }

            Student? student = null;
            if (!await TryDbAsync(async () => student = await repo.GetAsync(id))) return;
            if (student == null)
            {
                Dialogs.Info(FindForm(), "Not found", "That student no longer exists.");
                await ReloadAsync();
                return;
            }

            if (await actions.DeleteAsync(this, student))
                await ReloadAsync();   // the page is clamped if the last row of a page was deleted
        }

        // =====================================================================
        //  CSV export
        // =====================================================================

        /// <summary>Exports ALL students matching the search and filters (not just this page), in the current sort order.</summary>
        private async Task ExportCsvAsync()
        {
            List<Student> rows = new();
            var query = new StudentQuery(appliedTerm, selectedCourse, selectedYear, sortBy, sortDescending);
            if (!await TryDbAsync(async () => rows = await repo.GetAllAsync(query))) return;

            bool filtered = !string.IsNullOrWhiteSpace(appliedTerm) || selectedCourse != null || selectedYear != null;
            if (rows.Count == 0)
            {
                Dialogs.Info(FindForm(), "Export CSV",
                    filtered ? "No students match your search, so there is nothing to export."
                             : "There are no students to export yet.");
                return;
            }

            using var dlg = new SaveFileDialog
            {
                Title = "Export students to CSV",
                Filter = "CSV files (*.csv)|*.csv",
                DefaultExt = "csv",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = $"students_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

            try
            {
                var sb = new StringBuilder("Id,First Name,Surname,Email,Contact Number,Course,Year Level\r\n");
                foreach (var s in rows)
                    sb.Append(s.Id).Append(',')
                      .Append(CsvField(s.FirstName)).Append(',')
                      .Append(CsvField(s.Surname)).Append(',')
                      .Append(CsvField(s.Email)).Append(',')
                      .Append(CsvField(s.ContactNumber, isPhone: true)).Append(',')
                      .Append(CsvField(s.Course)).Append(',')
                      .Append(s.YearLevel).Append("\r\n");

                // UTF-8 with BOM so Excel shows accented characters correctly.
                await File.WriteAllTextAsync(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Dialogs.Error(FindForm(), "Export CSV",
                    "The file could not be saved. It may be open in another program, or the folder may not allow writing.\n\n" + ex.Message);
                return;
            }

            Toast.Show(this, $"Exported {rows.Count} {(rows.Count == 1 ? "student" : "students")} to {Path.GetFileName(dlg.FileName)}.");
        }

        /// <summary>Quotes a CSV field when needed and neutralises spreadsheet formulas.</summary>
        private static string CsvField(string? value, bool isPhone = false)
        {
            if (string.IsNullOrEmpty(value)) return "";

            // A cell starting with = + - @ (or a tab/CR) can run as a formula in Excel; prefix it so it stays text.
            // Phone numbers made only of digits and + ( ) - can't call functions, so they are left as typed.
            bool plainPhone = isPhone && PlainPhone.IsMatch(value);
            if (!plainPhone && "=+-@\t\r".Contains(value[0]))
                value = "'" + value;

            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
                value = "\"" + value.Replace("\"", "\"\"") + "\"";

            return value;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) searchDelay.Dispose();
            base.Dispose(disposing);
        }
    }
}
