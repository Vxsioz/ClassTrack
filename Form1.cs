using ClassTrack.Data;
using Microsoft.EntityFrameworkCore;

namespace ClassTrack
{
    public partial class Form1 : Form
    {
        // Palette taken from the web UI
        private static readonly Color NavColor = Color.FromArgb(31, 41, 78);
        private static readonly Color PageColor = Color.FromArgb(244, 246, 250);
        private static readonly Color Primary = Color.FromArgb(63, 81, 181);
        private static readonly Color TitleColor = Color.FromArgb(31, 41, 78);
        private static readonly Color Muted = Color.FromArgb(108, 117, 125);
        private static readonly Color Danger = Color.FromArgb(220, 53, 69);

        private readonly TextBox txtSearch = new();
        private readonly DataGridView grid = new();

        public Form1()
        {
            InitializeComponent();
            BuildUi();
            LoadGrid(null);
        }

        private void BuildUi()
        {
            Text = "Students - ClassTrack";
            Font = new Font("Segoe UI", 10F);
            BackColor = PageColor;
            ClientSize = new Size(1000, 640);
            MinimumSize = new Size(760, 480);

            // ---------- Nav bar ----------
            var navbar = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = NavColor };
            var brand = new Label
            {
                Text = "ClassTrack",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(40, 12)
            };
            var navStudents = MakeNavLink("Students", 160, (_, _) => { txtSearch.Clear(); LoadGrid(null); });
            var navAdd = MakeNavLink("Add Student", 245, (_, _) => AddStudent());
            navbar.Controls.AddRange(new Control[] { brand, navStudents, navAdd });

            // ---------- Footer ----------
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.White };
            footer.Controls.Add(new Label
            {
                Text = "© 2026 - ClassTrack Student Information Management",
                ForeColor = Muted,
                AutoSize = true,
                Location = new Point(40, 18)
            });

            // ---------- Content ----------
            var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(40, 24, 40, 16), BackColor = PageColor };

            // Header: title + subtitle + Add button
            var header = new Panel { Dock = DockStyle.Top, Height = 84 };
            header.Controls.Add(new Label
            {
                Text = "Students",
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = TitleColor,
                AutoSize = true,
                Location = new Point(0, 0)
            });
            header.Controls.Add(new Label
            {
                Text = "All registered students",
                ForeColor = Muted,
                AutoSize = true,
                Location = new Point(3, 50)
            });
            var btnAdd = MakeButton("+ Add Student", Primary, Color.White, Primary, 140, 40);
            btnAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAdd.Location = new Point(header.Width - btnAdd.Width, 8);
            btnAdd.Click += (_, _) => AddStudent();
            header.Controls.Add(btnAdd);

            // Search row
            var searchRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 56,
                ColumnCount = 3,
                RowCount = 1
            };
            searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            txtSearch.PlaceholderText = "Search by ID, first name or surname";
            txtSearch.Font = new Font("Segoe UI", 11F);
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.Margin = new Padding(0, 8, 8, 8);
            txtSearch.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Search(); }
            };
            var btnSearch = MakeButton("Search", Primary, Color.White, Primary, 82, 38);
            btnSearch.Margin = new Padding(0, 8, 8, 8);
            btnSearch.Click += (_, _) => Search();
            var btnClear = MakeButton("Clear", Color.White, Muted, Muted, 72, 38);
            btnClear.Margin = new Padding(0, 8, 0, 8);
            btnClear.Click += (_, _) => { txtSearch.Clear(); LoadGrid(null); };
            searchRow.Controls.Add(txtSearch, 0, 0);
            searchRow.Controls.Add(btnSearch, 1, 0);
            searchRow.Controls.Add(btnClear, 2, 0);

            // Grid
            ConfigureGrid();
            var gridHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };
            gridHost.Controls.Add(grid);

            // Dock order: last added is outermost (top)
            content.Controls.Add(gridHost);
            content.Controls.Add(searchRow);
            content.Controls.Add(header);

            Controls.Add(content);
            Controls.Add(footer);
            Controls.Add(navbar);
        }

        private void ConfigureGrid()
        {
            grid.Dock = DockStyle.Top;
            grid.Height = 300;
            grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Color.FromArgb(222, 226, 230);
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 42;
            grid.RowTemplate.Height = 48;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 252);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(33, 37, 41);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 249, 252);
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(33, 37, 41);
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);

            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(33, 37, 41);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 236, 250);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(33, 37, 41);
            grid.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10.5F);

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "Id", Width = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colFirst", HeaderText = "First Name", Width = 170 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colSurname",
                HeaderText = "Surname",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            grid.Columns.Add(MakeActionColumn("colDetails", "Details", Muted));
            grid.Columns.Add(MakeActionColumn("colEdit", "Edit", Primary));
            grid.Columns.Add(MakeActionColumn("colDelete", "Delete", Danger));

            grid.CellContentClick += Grid_CellContentClick;
            grid.SelectionChanged += (_, _) => grid.ClearSelection(); // keep the clean, selection-free look
        }

        private static DataGridViewButtonColumn MakeActionColumn(string name, string text, Color color)
        {
            var col = new DataGridViewButtonColumn
            {
                Name = name,
                HeaderText = name == "colDetails" ? "Actions" : "",
                Text = text,
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = name == "colDetails" ? 80 : 70,
                Resizable = DataGridViewTriState.False
            };
            col.DefaultCellStyle.ForeColor = color;
            col.DefaultCellStyle.BackColor = Color.White;
            col.DefaultCellStyle.SelectionBackColor = Color.White;
            col.DefaultCellStyle.SelectionForeColor = color;
            col.DefaultCellStyle.Padding = new Padding(2);
            col.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            return col;
        }

        private Label MakeNavLink(string text, int x, EventHandler onClick)
        {
            var link = new Label
            {
                Text = text,
                ForeColor = Color.FromArgb(210, 216, 235),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Location = new Point(x, 20)
            };
            link.MouseEnter += (_, _) => link.ForeColor = Color.White;
            link.MouseLeave += (_, _) => link.ForeColor = Color.FromArgb(210, 216, 235);
            link.Click += onClick;
            return link;
        }

        private static Button MakeButton(string text, Color back, Color fore, Color border, int w, int h)
        {
            var b = new Button
            {
                Text = text,
                Size = new Size(w, h),
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

        // ---------- Database helpers ----------

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
                LoadGrid(txtSearch.Text);
            }
            return found;
        }

        // ---------- Data / actions ----------

        private void LoadGrid(string? term)
        {
            var rows = new List<Student>();

            if (!TryDb(() =>
            {
                using var db = new AppDbContext();
                IQueryable<Student> query = db.Students.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(term))
                {
                    string t = term.Trim();
                    bool isId = int.TryParse(t, out int id);
                    query = query.Where(s =>
                        (isId && s.Id == id) ||
                        s.FirstName.Contains(t) ||
                        s.Surname.Contains(t));
                }

                rows = query.OrderBy(s => s.Id).ToList();
            }))
                return;

            grid.Rows.Clear();
            foreach (var s in rows)
                grid.Rows.Add(s.Id, s.FirstName, s.Surname);

            grid.Height = grid.ColumnHeadersHeight + Math.Max(rows.Count, 1) * grid.RowTemplate.Height + 2;
            grid.ClearSelection();
        }

        private void Search() => LoadGrid(txtSearch.Text);

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

            if (saved) LoadGrid(txtSearch.Text);
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

                        if (saved) LoadGrid(txtSearch.Text);
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

                    if (deleted) LoadGrid(txtSearch.Text);
                    break;
            }
        }
    }
}
