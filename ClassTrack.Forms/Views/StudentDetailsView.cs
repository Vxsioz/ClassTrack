using ClassTrack.Data;
using ClassTrack.UI;
using ClassTrack.UI.Controls;

namespace ClassTrack.Views
{
    /// <summary>One student's picture and saved information, organised into sections, with Edit and Delete.</summary>
    public sealed class StudentDetailsView : ViewBase
    {
        private const int PictureSize = 148;

        private readonly StudentRepository repo = new();
        private readonly StudentActions actions;
        private readonly ScrollHost page = new();
        private Student? current;

        private readonly GlassPicture picture = new() { Size = new Size(PictureSize, PictureSize), Margin = new Padding(0, 0, 30, 0), BackColor = Color.Transparent };
        private readonly Label lblName = Ui.Text("", Theme.H1, Theme.Text);
        private readonly Label lblId = Ui.Text("", Theme.Body, Theme.TextMuted);
        private readonly Label valEmail = Ui.Text("", Theme.Body, Theme.Text);
        private readonly Label valContact = Ui.Text("", Theme.Body, Theme.Text);
        private readonly Label valCourse = Ui.Text("", Theme.Body, Theme.Text);
        private readonly Label valYear = Ui.Text("", Theme.Body, Theme.Text);
        private readonly Label chipCourse = Ui.Text("", Theme.Small, Theme.AccentSoft);
        private readonly Label chipYear = Ui.Text("", Theme.Small, Theme.AccentSoft);

        /// <summary>Raised when the user goes back to the directory, or the student no longer exists / was deleted.</summary>
        public event EventHandler? BackRequested;

        /// <summary>Raised with the student's ID when the user chooses Edit.</summary>
        public event EventHandler<int>? EditRequested;

        public StudentDetailsView()
        {
            actions = new StudentActions(repo);
            Controls.Add(page);
            page.AddRow(BuildTopBar());
            page.AddRow(BuildHero());
            page.AddRow(BuildSections());
        }

        private Control BuildTopBar()
        {
            var bar = new TablePanel { BackColor = Color.Transparent, AutoSize = true, ColumnCount = 4, Margin = new Padding(0, 0, 0, 22) };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var back = new FlatButton("Students", ButtonKind.Ghost) { Icon = IconKind.ChevronLeft };
            back.Click += (_, _) => BackRequested?.Invoke(this, EventArgs.Empty);
            var edit = new FlatButton("Edit", ButtonKind.Primary) { Margin = new Padding(0, 0, 10, 0) };
            edit.Click += (_, _) => { if (current != null) EditRequested?.Invoke(this, current.Id); };
            var delete = new FlatButton("Delete", ButtonKind.Danger);
            delete.Click += async (_, _) => await DeleteAsync();

            bar.Controls.Add(back, 0, 0);
            bar.Controls.Add(edit, 2, 0);
            bar.Controls.Add(delete, 3, 0);
            return bar;
        }

        private Control BuildHero()
        {
            var hero = new GlassPanel { AutoSize = true, Padding = new Padding(30, 28, 30, 28), Margin = new Padding(0, 0, 0, 20), Radius = 24 };
            var row = new TablePanel { BackColor = Color.Transparent, Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2 };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var text = new FlowPanel { BackColor = Color.Transparent, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Anchor = AnchorStyles.Left, Margin = Padding.Empty };
            lblName.Font = Theme.Hero;
            lblId.Margin = new Padding(4, 4, 0, 14);
            var chips = new FlowPanel { BackColor = Color.Transparent, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(4, 0, 0, 0) };
            chips.Controls.Add(Chip(chipCourse));
            chips.Controls.Add(Chip(chipYear));
            text.Controls.Add(lblName);
            text.Controls.Add(lblId);
            text.Controls.Add(chips);

            picture.Anchor = AnchorStyles.Left;
            row.Controls.Add(picture, 0, 0);
            row.Controls.Add(text, 1, 0);
            hero.Controls.Add(row);
            return hero;
        }

        /// <summary>A small rounded label with an accent tint.</summary>
        private static Control Chip(Label label)
        {
            label.Margin = Padding.Empty;
            label.Font = Theme.Body;
            var chip = new GlassPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(16, 7, 16, 8), Radius = 16, Margin = new Padding(0, 0, 10, 0) };
            chip.Controls.Add(label);
            return chip;
        }

        private Control BuildSections()
        {
            var panel = new GlassPanel { AutoSize = true, Padding = new Padding(30, 26, 30, 10), Margin = new Padding(0, 0, 0, 8), Radius = 24 };
            var stack = new TablePanel { BackColor = Color.Transparent, Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2 };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            Section(stack, 0, "Contact information", ("Email address", valEmail), ("Contact number", valContact));
            Section(stack, 1, "Academic information", ("Course / program", valCourse), ("Year level", valYear));
            panel.Controls.Add(stack);
            return panel;
        }

        private static void Section(TableLayoutPanel grid, int column, string title, params (string Caption, Label Value)[] fields)
        {
            var block = new FlowPanel { BackColor = Color.Transparent, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, 0, 24, 18) };
            block.Controls.Add(Ui.Text(title, Theme.H3, Theme.Text, new Padding(0, 0, 0, 14)));
            foreach (var (caption, value) in fields)
            {
                block.Controls.Add(Ui.Text(caption.ToUpperInvariant(), Theme.Caption, Theme.TextMuted, new Padding(0, 0, 0, 4)));
                value.Margin = new Padding(0, 0, 0, 16);
                block.Controls.Add(value);
            }
            grid.Controls.Add(block, column, 0);
        }

        public override Task RefreshDataAsync() =>
            current == null ? Task.CompletedTask : ShowStudentAsync(current.Id);

        public async Task ShowStudentAsync(int id)
        {
            Student? student = null;
            if (!await TryDbAsync(async () => student = await repo.GetAsync(id))) return;

            if (student == null)
            {
                Dialogs.Info(FindForm(), "Not found", "That student no longer exists.");
                BackRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            current = student;
            lblName.Text = student.FullName;
            lblId.Text = $"Student ID {student.Id}";
            chipCourse.Text = student.Course ?? "No course";
            chipYear.Text = student.YearLevel == null ? "No year level" : $"Year {student.YearLevel}";
            SetValue(valEmail, student.Email);
            SetValue(valContact, student.ContactNumber);
            SetValue(valCourse, student.Course);
            SetValue(valYear, student.YearLevel == null ? null : $"Year {student.YearLevel}");

            var old = picture.Image;
            picture.Image = Avatar.Create(student, PictureSize, 26);
            old?.Dispose();
            page.Fit();
        }

        /// <summary>Shows the value, or a faint "Not provided" when the optional field is empty.</summary>
        private static void SetValue(Label label, string? value)
        {
            bool has = !string.IsNullOrWhiteSpace(value);
            label.Text = has ? value : "Not provided";
            label.ForeColor = has ? Theme.Text : Theme.TextFaint;
        }

        private async Task DeleteAsync()
        {
            if (current == null) return;
            if (await actions.DeleteAsync(this, current))
            {
                current = null;
                BackRequested?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
