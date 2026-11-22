using ClassTrack.Data;
using Microsoft.EntityFrameworkCore;

namespace ClassTrack
{
    public partial class Form1 : Form
    {
        // Palette taken from the web UI
        private static readonly Color NavColor = Color.FromArgb(31, 41, 78);
        private static readonly Color NavLinkColor = Color.FromArgb(210, 216, 235);
        private static readonly Color PageColor = Color.FromArgb(244, 246, 250);
        private static readonly Color Primary = Color.FromArgb(63, 81, 181);
        private static readonly Color TitleColor = Color.FromArgb(31, 41, 78);
        private static readonly Color Muted = Color.FromArgb(108, 117, 125);
        private static readonly Color Danger = Color.FromArgb(220, 53, 69);

        // Fonts: every size below is derived from these, so the layout follows the screen's font scaling.
        private static readonly Font BodyFont = new("Segoe UI", 10.5F);
        private static readonly Font HeaderFont = new("Segoe UI", 10.5F, FontStyle.Bold);
        private static readonly Font ActionFont = new("Segoe UI", 9.5F);

        private const int PageSize = 10;
        private const int RecentCount = 5;

        // List state: search, sort and page are kept together so they survive each other.
        private string? appliedTerm;          // the search that is currently applied
        private string sortBy = "Id";         // Id | FirstName | Surname
        private bool sortDescending;
        private int page = 1;
        private int totalPages = 1;

        // Views
        private Panel studentsView = null!;
        private Panel dashboardView = null!;
        private Label navDashboard = null!;
        private Label navStudents = null!;

        // Students view controls
        private readonly TextBox txtSearch = new();
        private readonly DataGridView grid = new();
        private Button btnPrev = null!;
        private Button btnNext = null!;
        private readonly Label lblPage = new();
        private readonly Label lblCount = new();

        // Dashboard controls
        private readonly Label lblTotal = new(), lblTotalSub = new();
        private readonly Label lblLatest = new(), lblLatestSub = new();
        private readonly Label lblUnique = new(), lblUniqueSub = new();
        private readonly Label lblTopSurname = new(), lblTopSurnameSub = new();
        private readonly DataGridView recentGrid = new();

        private sealed record DashboardData(
            int Total, Student? Latest, int UniqueSurnames,
            string? TopSurname, int TopSurnameCount, List<Student> Recent);

        public Form1()
        {
            InitializeComponent();
            BuildUi();
            ShowStudents();
        }

        // =====================================================================
        //  UI construction
        //  Layout uses auto-sizing panels and table layouts (no hard-coded
        //  pixel positions), so text never overlaps or gets cut off.
        // =====================================================================

        private void BuildUi()
        {
            Text = "ClassTrack";
            Font = new Font("Segoe UI", 10F);
            BackColor = PageColor;
            ClientSize = new Size(1000, 760);
            MinimumSize = new Size(760, 520);

            // ---------- Nav bar ----------
            var navbar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = NavColor,
                ColumnCount = 5,
                RowCount = 1,
                Padding = new Padding(40, 14, 0, 14)
            };
            for (int i = 0; i < 4; i++) navbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            navbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            navbar.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var brand = new Label
            {
                Text = "ClassTrack",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 32, 0)
            };
            navDashboard = MakeNavLink("Dashboard", (_, _) => ShowDashboard());
            navStudents = MakeNavLink("Students", (_, _) => ShowStudents());
            var navAdd = MakeNavLink("Add Student", (_, _) => AddStudent());
            navbar.Controls.Add(brand, 0, 0);
            navbar.Controls.Add(navDashboard, 1, 0);
            navbar.Controls.Add(navStudents, 2, 0);
            navbar.Controls.Add(navAdd, 3, 0);

            // ---------- Footer ----------
            var footer = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.White,
                ColumnCount = 1,
                RowCount = 1,
                Padding = new Padding(40, 18, 40, 18)
            };
            footer.Controls.Add(new Label
            {
                Text = "© 2026 - ClassTrack Student Information Management",
                ForeColor = Muted,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0)
            }, 0, 0);

            studentsView = BuildStudentsView();
            dashboardView = BuildDashboardView();

            Controls.Add(studentsView);
            Controls.Add(dashboardView);
            Controls.Add(footer);
            Controls.Add(navbar);
        }

        /// <summary>Title + subtitle on the left, optional action button on the right.</summary>
        private static TableLayoutPanel MakeHeader(string title, string subtitle, Button? action)
        {
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 0, 0, 16)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var text = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0)
            };
            text.Controls.Add(new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true,
                Margin = new Padding(0)
            });
            text.Controls.Add(new Label
            {
                Text = subtitle,
                ForeColor = Muted,
                AutoSize = true,
                Margin = new Padding(3, 0, 0, 0)
            });
            header.Controls.Add(text, 0, 0);

            if (action != null)
            {
                action.Anchor = AnchorStyles.Right;
                action.Margin = new Padding(0);
                header.Controls.Add(action, 1, 0);
            }
            return header;
        }

        private Panel BuildStudentsView()
        {
            var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(40, 24, 40, 8), BackColor = PageColor };

            var btnAdd = MakeButton("+ Add Student", Primary, Color.White, Primary);
            btnAdd.Click += (_, _) => AddStudent();
            var header = MakeHeader("Students", "All registered students", btnAdd);

            // Search row
            var searchRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0, 0, 0, 8)
            };
            searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            searchRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            txtSearch.PlaceholderText = "Search by ID, first name or surname";
            txtSearch.Font = new Font("Segoe UI", 11F);
            txtSearch.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtSearch.Margin = new Padding(0, 0, 8, 0);
            txtSearch.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Search(); }
            };
            var btnSearch = MakeButton("Search", Primary, Color.White, Primary);
            btnSearch.Margin = new Padding(0, 0, 8, 0);
            btnSearch.Click += (_, _) => Search();
            var btnClear = MakeButton("Clear", Color.White, Muted, Muted);
            btnClear.Margin = new Padding(0);
            btnClear.Click += (_, _) => ShowAll();
            searchRow.Controls.Add(txtSearch, 0, 0);
            searchRow.Controls.Add(btnSearch, 1, 0);
            searchRow.Controls.Add(btnClear, 2, 0);

            // Pager: Previous | Page X of Y | Next ........ Showing a-b of n students
            var pager = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 5,
                RowCount = 1,
                Padding = new Padding(0, 8, 0, 8)
            };
            pager.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pager.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pager.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pager.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pager.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pager.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            btnPrev = MakeButton("‹ Previous", Color.White, Primary, Primary);
            btnPrev.Margin = new Padding(0, 0, 4, 0);
            btnPrev.Click += (_, _) => GoToPage(page - 1);
            btnNext = MakeButton("Next ›", Color.White, Primary, Primary);
            btnNext.Margin = new Padding(4, 0, 0, 0);
            btnNext.Click += (_, _) => GoToPage(page + 1);

            lblPage.AutoSize = true;
            lblPage.Anchor = AnchorStyles.Left;
            lblPage.Margin = new Padding(8, 0, 8, 0);
            lblPage.ForeColor = TitleColor;

            lblCount.AutoSize = true;
            lblCount.Anchor = AnchorStyles.Right;
            lblCount.Margin = new Padding(0);
            lblCount.ForeColor = Muted;

            pager.Controls.Add(btnPrev, 0, 0);
            pager.Controls.Add(lblPage, 1, 0);
            pager.Controls.Add(btnNext, 2, 0);
            pager.Controls.Add(lblCount, 4, 0);

            // Grid
            ConfigureGrid();
            var gridHost = new Panel { Dock = DockStyle.Fill };
            gridHost.Controls.Add(grid);

            // Dock order: last added is outermost
            content.Controls.Add(gridHost);
            content.Controls.Add(pager);
            content.Controls.Add(searchRow);
            content.Controls.Add(header);
            return content;
        }

        private Panel BuildDashboardView()
        {
            var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(40, 24, 40, 16), BackColor = PageColor };

            var btnViewAll = MakeButton("View all students", Primary, Color.White, Primary);
            btnViewAll.Click += (_, _) => ShowStudents();
            var header = MakeHeader("Dashboard", "Overview of registered students", btnViewAll);

            // Summary cards
            var cards = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 4,
                RowCount = 1
            };
            for (int i = 0; i < 4; i++)
                cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            cards.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            cards.Controls.Add(MakeCard("Total Students", lblTotal, lblTotalSub, rightGap: true), 0, 0);
            cards.Controls.Add(MakeCard("Latest Student", lblLatest, lblLatestSub, rightGap: true), 1, 0);
            cards.Controls.Add(MakeCard("Unique Surnames", lblUnique, lblUniqueSub, rightGap: true), 2, 0);
            cards.Controls.Add(MakeCard("Most Common Surname", lblTopSurname, lblTopSurnameSub, rightGap: false), 3, 0);

            // Recently added
            var recentHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0, 24, 0, 8)
            };
            recentHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            recentHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            recentHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            recentHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            recentHeader.Controls.Add(new Label
            {
                Text = "Recently added",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 12, 0)
            }, 0, 0);
            recentHeader.Controls.Add(new Label
            {
                Text = $"Latest {RecentCount} students by ID",
                ForeColor = Muted,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0)
            }, 1, 0);

            ConfigureRecentGrid();
            var gridHost = new Panel { Dock = DockStyle.Fill };
            gridHost.Controls.Add(recentGrid);

            // Dock order: last added is outermost
            content.Controls.Add(gridHost);
            content.Controls.Add(recentHeader);
            content.Controls.Add(cards);
            content.Controls.Add(header);
            return content;
        }

        private static Panel MakeCard(string caption, Label value, Label sub, bool rightGap)
        {
            var captionFont = new Font("Segoe UI", 9.5F);
            var valueFont = new Font("Segoe UI", 20F, FontStyle.Bold);
            var subFont = new Font("Segoe UI", 9.5F);

            var lblCaption = new Label
            {
                Text = caption,
                ForeColor = Muted,
                Font = captionFont,
                AutoSize = false,
                AutoEllipsis = true,
                Dock = DockStyle.Top,
                Height = captionFont.Height + 6
            };

            value.AutoSize = false;
            value.AutoEllipsis = true;
            value.Font = valueFont;
            value.ForeColor = TitleColor;
            value.Dock = DockStyle.Top;
            value.Height = valueFont.Height + 8;
            value.Text = "—";

            sub.AutoSize = false;
            sub.AutoEllipsis = true;
            sub.Font = subFont;
            sub.ForeColor = Muted;
            sub.Dock = DockStyle.Top;
            sub.Height = subFont.Height + 6;

            var accent = new Panel { Dock = DockStyle.Left, Width = 5, BackColor = Primary };

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, rightGap ? 14 : 0, 0),
                Padding = new Padding(20, 12, 10, 12)
            };
            card.Height = card.Padding.Vertical + lblCaption.Height + value.Height + sub.Height;

            // Dock order: last added is outermost (top)
            card.Controls.Add(sub);
            card.Controls.Add(value);
            card.Controls.Add(lblCaption);
            card.Controls.Add(accent);
            return card;
        }

        /// <summary>Styling shared by the students grid and the "recently added" grid.</summary>
        private static void ApplyGridStyle(DataGridView g)
        {
            g.Dock = DockStyle.Fill;
            g.BackgroundColor = PageColor;
            g.BorderStyle = BorderStyle.None;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.GridColor = Color.FromArgb(222, 226, 230);
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.ReadOnly = true;
            g.RowHeadersVisible = false;
            g.MultiSelect = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = HeaderFont.Height + 22;
            g.RowTemplate.Height = BodyFont.Height + 26;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(33, 37, 41);
            g.ColumnHeadersDefaultCellStyle.Font = HeaderFont;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 249, 252);
            g.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(33, 37, 41);
            g.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);

            g.DefaultCellStyle.BackColor = Color.White;
            g.DefaultCellStyle.ForeColor = Color.FromArgb(33, 37, 41);
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 236, 250);
            g.DefaultCellStyle.SelectionForeColor = Color.FromArgb(33, 37, 41);
            g.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            g.DefaultCellStyle.Font = BodyFont;

            g.SelectionChanged += (_, _) => g.ClearSelection(); // keep the clean, selection-free look
        }

        /// <summary>Width that fits a header in bold plus room for padding and the sort arrow.</summary>
        private static int HeaderWidth(string text, int extra = 44) =>
            TextRenderer.MeasureText(text, HeaderFont).Width + extra;

        private void ConfigureGrid()
        {
            ApplyGridStyle(grid);

            // Id / First Name / Surname headers are clickable for sorting (we sort in the query).
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colId", HeaderText = "Id",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                MinimumWidth = HeaderWidth("Id"),
                SortMode = DataGridViewColumnSortMode.Programmatic
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colFirst", HeaderText = "First Name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                MinimumWidth = HeaderWidth("First Name") + 60,
                SortMode = DataGridViewColumnSortMode.Programmatic
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colSurname",
                HeaderText = "Surname",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = HeaderWidth("Surname"),
                SortMode = DataGridViewColumnSortMode.Programmatic
            });
            grid.Columns.Add(MakeActionColumn("colDetails", "Details", "Actions", Muted));
            grid.Columns.Add(MakeActionColumn("colEdit", "Edit", "", Primary));
            grid.Columns.Add(MakeActionColumn("colDelete", "Delete", "", Danger));

            grid.CellContentClick += Grid_CellContentClick;
            grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;
        }

        private void ConfigureRecentGrid()
        {
            ApplyGridStyle(recentGrid);

            recentGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "rId", HeaderText = "Id",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                MinimumWidth = HeaderWidth("Id", 24),
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            recentGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "rFirst", HeaderText = "First Name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                MinimumWidth = HeaderWidth("First Name", 24) + 60,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            recentGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "rSurname", HeaderText = "Surname",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = HeaderWidth("Surname", 24),
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        }

        private static DataGridViewButtonColumn MakeActionColumn(string name, string text, string header, Color color)
        {
            // Width comes from the measured text, so it fits at any font scaling.
            int width = TextRenderer.MeasureText(text, ActionFont).Width + 36;
            if (header.Length > 0)
                width = Math.Max(width, HeaderWidth(header, 24));

            var col = new DataGridViewButtonColumn
            {
                Name = name,
                HeaderText = header,
                Text = text,
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = width,
                Resizable = DataGridViewTriState.False,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            col.DefaultCellStyle.ForeColor = color;
            col.DefaultCellStyle.BackColor = Color.White;
            col.DefaultCellStyle.SelectionBackColor = Color.White;
            col.DefaultCellStyle.SelectionForeColor = color;
            col.DefaultCellStyle.Padding = new Padding(2);
            col.DefaultCellStyle.Font = ActionFont;
            return col;
        }

        private Label MakeNavLink(string text, EventHandler onClick)
        {
            var link = new Label
            {
                Text = text,
                ForeColor = NavLinkColor,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 24, 0),
                Cursor = Cursors.Hand
            };
            link.MouseEnter += (_, _) => link.ForeColor = Color.White;
            link.MouseLeave += (_, _) =>
                link.ForeColor = Equals(link.Tag, "active") ? Color.White : NavLinkColor;
            link.Click += onClick;
            return link;
        }

        /// <summary>Button that sizes itself to its text (never truncated).</summary>
        private static Button MakeButton(string text, Color back, Color fore, Color border)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10, 5, 10, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = fore,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderColor = border;
            b.FlatAppearance.BorderSize = 1;
            return b;
        }

        // =====================================================================
        //  View switching
        // =====================================================================

        private bool DashboardVisible => dashboardView.Visible;

        private void SetActiveNav(Label active)
        {
            foreach (var link in new[] { navDashboard, navStudents })
            {
                bool isActive = link == active;
                link.Tag = isActive ? "active" : null;
                link.ForeColor = isActive ? Color.White : NavLinkColor;
                link.Font = new Font(link.Font, isActive ? FontStyle.Bold : FontStyle.Regular);
            }
        }

        private void ShowDashboard()
        {
            studentsView.Visible = false;
            dashboardView.Visible = true;
            Text = "Dashboard - ClassTrack";
            SetActiveNav(navDashboard);
            LoadDashboard();
        }

        private void ShowStudents()
        {
            dashboardView.Visible = false;
            studentsView.Visible = true;
            Text = "Students - ClassTrack";
            SetActiveNav(navStudents);
            ShowAll();
        }

        /// <summary>Reloads whichever view is on screen.</summary>
        private void RefreshCurrentView()
        {
            if (DashboardVisible) LoadDashboard();
            else _ = LoadGrid();
        }

        // =====================================================================
        //  Database helpers
        // =====================================================================

        /// <summary>Runs a database action; shows a friendly message and returns false on failure.</summary>
        private bool TryDb(Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Could not complete the database operation.\n\n" +
                    "Make sure SQL Server LocalDB is running and the ClassTrackDb database exists " +
                    "(created by the ClassTrack.Web app: dotnet ef database update).\n\n" +
                    ex.GetBaseException().Message,
                    "Database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private async Task<bool> TryDbAsync(Func<Task> action)
        {
            try
            {
                await action();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Could not complete the database operation.\n\n" + ex.GetBaseException().Message,
                    "Database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private Student? FindStudent(int id)
        {
            Student? found = null;
            if (!TryDb(() =>
            {
                using var db = new AppDbContext();
                found = db.Students.AsNoTracking().FirstOrDefault(s => s.Id == id);
            }))
                return null;

            if (found == null)
            {
                MessageBox.Show(this, "That student no longer exists.", "Not found",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                _ = LoadGrid();
            }
            return found;
        }

        // =====================================================================
        //  Dashboard
        // =====================================================================

        private void LoadDashboard()
        {
            DashboardData? data = null;

            if (!TryDb(() =>
            {
                using var db = new AppDbContext();

                int total = db.Students.Count();
                var latest = db.Students.AsNoTracking().OrderByDescending(s => s.Id).FirstOrDefault();
                int unique = db.Students.Select(s => s.Surname).Distinct().Count();

                var top = db.Students
                    .GroupBy(s => s.Surname)
                    .Select(g => new { Surname = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count).ThenBy(x => x.Surname)
                    .FirstOrDefault();

                var recent = db.Students.AsNoTracking()
                    .OrderByDescending(s => s.Id)
                    .Take(RecentCount)
                    .ToList();

                data = new DashboardData(total, latest, unique, top?.Surname, top?.Count ?? 0, recent);
            }))
                return;

            if (data == null) return;

            lblTotal.Text = data.Total.ToString();
            lblTotalSub.Text = data.Total == 1 ? "registered student" : "registered students";

            lblLatest.Text = data.Latest == null ? "—" : $"{data.Latest.FirstName} {data.Latest.Surname}";
            lblLatestSub.Text = data.Latest == null ? "no students yet" : $"ID {data.Latest.Id}";

            lblUnique.Text = data.UniqueSurnames.ToString();
            lblUniqueSub.Text = "different surnames";

            lblTopSurname.Text = data.TopSurname ?? "—";
            lblTopSurnameSub.Text = data.TopSurname == null
                ? "no students yet"
                : $"{data.TopSurnameCount} {(data.TopSurnameCount == 1 ? "student" : "students")}";

            recentGrid.Rows.Clear();
            foreach (var s in data.Recent)
                recentGrid.Rows.Add(s.Id, s.FirstName, s.Surname);
            recentGrid.ClearSelection();
        }

        // =====================================================================
        //  Students list: search + sort + paging
        // =====================================================================

        /// <summary>Loads the current page using the current search term and sort order.</summary>
        private async Task LoadGrid()
        {
            var rows = new List<Student>();
            int total = 0;

            if (!await TryDbAsync(async () =>
            {
                using var db = new AppDbContext();
                IQueryable<Student> query = db.Students.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(appliedTerm))
                {
                    string t = appliedTerm!.Trim();
                    bool isId = int.TryParse(t, out int id);
                    query = query.Where(s =>
                        (isId && s.Id == id) ||
                        s.FirstName.Contains(t) ||
                        s.Surname.Contains(t));
                }

                total = await query.CountAsync();
                totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
                if (page > totalPages) page = totalPages;
                if (page < 1) page = 1;

                // Only whitelisted columns are ever sorted; Id is the tie-breaker so paging is stable.
                IOrderedQueryable<Student> ordered = (sortBy, sortDescending) switch
                {
                    ("FirstName", false) => query.OrderBy(s => s.FirstName).ThenBy(s => s.Id),
                    ("FirstName", true) => query.OrderByDescending(s => s.FirstName).ThenBy(s => s.Id),
                    ("Surname", false) => query.OrderBy(s => s.Surname).ThenBy(s => s.Id),
                    ("Surname", true) => query.OrderByDescending(s => s.Surname).ThenBy(s => s.Id),
                    (_, true) => query.OrderByDescending(s => s.Id),
                    _ => query.OrderBy(s => s.Id)
                };

                rows = await ordered.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
            }))
                return;

            grid.Rows.Clear();
            foreach (var s in rows)
                grid.Rows.Add(s.Id, s.FirstName, s.Surname);
            grid.ClearSelection();

            UpdateSortGlyphs();
            UpdatePager(total, rows.Count);
        }

        private void UpdateSortGlyphs()
        {
            SetGlyph("colId", "Id");
            SetGlyph("colFirst", "FirstName");
            SetGlyph("colSurname", "Surname");

            void SetGlyph(string columnName, string key)
            {
                grid.Columns[columnName].HeaderCell.SortGlyphDirection =
                    sortBy != key ? SortOrder.None :
                    sortDescending ? SortOrder.Descending : SortOrder.Ascending;
            }
        }

        private void UpdatePager(int total, int shown)
        {
            lblPage.Text = $"Page {page} of {totalPages}";
            btnPrev.Enabled = page > 1;
            btnNext.Enabled = page < totalPages;

            if (total == 0)
            {
                lblCount.Text = string.IsNullOrWhiteSpace(appliedTerm)
                    ? "No students registered yet"
                    : "No students match your search";
            }
            else
            {
                int first = (page - 1) * PageSize + 1;
                lblCount.Text = $"Showing {first}–{first + shown - 1} of {total} students";
            }
        }

        private void GoToPage(int newPage)
        {
            page = Math.Clamp(newPage, 1, totalPages);
            _ = LoadGrid();
        }

        private void Search()
        {
            appliedTerm = txtSearch.Text;
            page = 1;            // new search starts on page 1; sort order is kept
            _ = LoadGrid();
        }

        private void ShowAll()
        {
            txtSearch.Clear();
            appliedTerm = null;
            page = 1;            // sort order is kept
            _ = LoadGrid();
        }

        private void Grid_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            string? key = grid.Columns[e.ColumnIndex].Name switch
            {
                "colId" => "Id",
                "colFirst" => "FirstName",
                "colSurname" => "Surname",
                _ => null
            };
            if (key == null) return;

            if (sortBy == key)
                sortDescending = !sortDescending;   // same column: flip direction
            else
            {
                sortBy = key;                       // new column: start ascending
                sortDescending = false;
            }

            page = 1;                               // search term is kept
            _ = LoadGrid();
        }

        // =====================================================================
        //  Student actions
        // =====================================================================

        private void AddStudent()
        {
            using var dlg = new StudentForm();
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            bool saved = TryDb(() =>
            {
                using var db = new AppDbContext();
                db.Students.Add(new Student { FirstName = dlg.FirstName, Surname = dlg.Surname });
                db.SaveChanges();
            });

            if (saved) RefreshCurrentView();
        }

        private void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            int id = (int)grid.Rows[e.RowIndex].Cells["colId"].Value!;
            var student = FindStudent(id);
            if (student == null) return;

            switch (grid.Columns[e.ColumnIndex].Name)
            {
                case "colDetails":
                    MessageBox.Show(this,
                        $"Id: {student.Id}\nFirst Name: {student.FirstName}\nSurname: {student.Surname}",
                        "Student Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;

                case "colEdit":
                    using (var dlg = new StudentForm(student))
                    {
                        if (dlg.ShowDialog(this) != DialogResult.OK) break;

                        bool saved = TryDb(() =>
                        {
                            using var db = new AppDbContext();
                            var tracked = db.Students.Find(id);
                            if (tracked == null) return;
                            tracked.FirstName = dlg.FirstName;
                            tracked.Surname = dlg.Surname;
                            db.SaveChanges();
                        });

                        if (saved) _ = LoadGrid();
                    }
                    break;

                case "colDelete":
                    var answer = MessageBox.Show(this,
                        $"Delete {student.FirstName} {student.Surname}?",
                        "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (answer != DialogResult.Yes) break;

                    bool deleted = TryDb(() =>
                    {
                        using var db = new AppDbContext();
                        var tracked = db.Students.Find(id);
                        if (tracked == null) return;
                        db.Students.Remove(tracked);
                        db.SaveChanges();
                    });

                    if (deleted) _ = LoadGrid();   // page is clamped if the last row of a page was deleted
                    break;
            }
        }
    }
}
