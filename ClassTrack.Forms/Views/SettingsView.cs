using System.Diagnostics;
using ClassTrack.Data;
using ClassTrack.Services;
using ClassTrack.UI;
using ClassTrack.UI.Controls;
using Microsoft.Data.SqlClient;

namespace ClassTrack.Views
{
    /// <summary>Read-only facts about where ClassTrack keeps its data, with two small utilities (test connection, open pictures folder).</summary>
    public sealed class SettingsView : ViewBase
    {
        private readonly StudentRepository repo = new();
        private readonly ScrollHost page = new();
        private readonly Label lblStatus = Ui.Text("Not checked yet", Theme.Body, Theme.TextMuted);

        public SettingsView()
        {
            Controls.Add(page);
            page.AddRow(new PageHeader("Settings", "Where ClassTrack keeps your data."));

            var cs = new SqlConnectionStringBuilder(AppDbContext.ConnectionString);
            var test = new FlatButton("Test connection");
            test.Click += async (_, _) => await TestConnectionAsync();

            string pictures = Path.Combine(ImageStore.RootFolder, "students");
            var open = new FlatButton("Open folder") { Icon = IconKind.Folder };
            open.Click += (_, _) => OpenPicturesFolder(pictures);

            page.AddRow(Card("Database",
                Row("Server", "The SQL Server instance ClassTrack connects to.", Value(cs.DataSource)),
                Row("Database", "Student records live here.", Value(cs.InitialCatalog)),
                Row("Connection", "Checks that the database answers.", StatusWithButton(lblStatus, test))));

            page.AddRow(Card("Pictures",
                Row("Folder", "Cropped student pictures are stored here, one file per student.", Value(pictures, wide: true)),
                Row("Browse", "Open the folder in File Explorer.", open)));

            var assembly = typeof(SettingsView).Assembly.GetName();
            page.AddRow(Card("About",
                Row("ClassTrack", "Student management for Windows.", Value($"Version {assembly.Version?.ToString(3) ?? "1.0"}")),
                Row("Runtime", "The .NET version this app is running on.", Value($".NET {Environment.Version}"))));
        }

        public override Task RefreshDataAsync()
        {
            page.Fit();
            return Task.CompletedTask;
        }

        private async Task TestConnectionAsync()
        {
            lblStatus.ForeColor = Theme.TextMuted;
            lblStatus.Text = "Checking…";
            int total = 0;
            bool ok = await TryDbAsync(async () => total = (await repo.GetPageAsync(new StudentQuery(PageSize: 1))).Total);
            lblStatus.ForeColor = ok ? Theme.Success : Theme.Danger;
            lblStatus.Text = ok ? $"Connected  ·  {total} {(total == 1 ? "student" : "students")}" : "Could not connect";
        }

        private void OpenPicturesFolder(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            {
                Dialogs.Error(FindForm(), "Pictures folder", "The folder could not be opened.\n\n" + ex.Message);
            }
        }

        // ---- layout helpers -----------------------------------------------------

        private static Label Value(string text, bool wide = false)
        {
            var label = Ui.Text(text, Theme.Body, Theme.Text);
            label.Anchor = AnchorStyles.Right;
            label.TextAlign = ContentAlignment.MiddleRight;
            if (wide) label.MaximumSize = new Size(440, 0);
            return label;
        }

        private static Control StatusWithButton(Label status, Control button)
        {
            var row = new FlowPanel { BackColor = Color.Transparent, AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight, Anchor = AnchorStyles.Right, Margin = Padding.Empty };
            status.Margin = new Padding(0, 8, 16, 0);
            row.Controls.Add(status);
            row.Controls.Add(button);
            return row;
        }

        private static Control Row(string title, string description, Control right)
        {
            var row = new TablePanel { BackColor = Color.Transparent, Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, 12, 0, 12) };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var text = new FlowPanel { BackColor = Color.Transparent, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = Padding.Empty };
            text.Controls.Add(Ui.Text(title, Theme.H3, Theme.Text));
            var desc = Ui.Text(description, Theme.Small, Theme.TextMuted, new Padding(0, 2, 0, 0));
            desc.MaximumSize = new Size(420, 0);
            text.Controls.Add(desc);

            right.Anchor = AnchorStyles.Right;
            row.Controls.Add(text, 0, 0);
            row.Controls.Add(right, 1, 0);
            return row;
        }

        private static Control Card(string title, params Control[] rows)
        {
            var panel = new GlassPanel { AutoSize = true, Padding = new Padding(28, 22, 28, 10), Margin = new Padding(0, 0, 0, 18), Radius = 24 };
            var stack = new TablePanel { BackColor = Color.Transparent, Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1 };
            stack.Controls.Add(Ui.Text(title.ToUpperInvariant(), Theme.Caption, Theme.AccentSoft, new Padding(0, 0, 0, 6)));
            for (int i = 0; i < rows.Length; i++)
            {
                if (i > 0) stack.Controls.Add(Ui.Divider());
                stack.Controls.Add(rows[i]);
            }
            panel.Controls.Add(stack);
            return panel;
        }
    }
}
