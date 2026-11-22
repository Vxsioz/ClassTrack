using ClassTrack.Data;
using ClassTrack.UI;
using ClassTrack.UI.Controls;

namespace ClassTrack.Views
{
    /// <summary>One student's picture and saved information, with Edit and Delete.</summary>
    public sealed class StudentDetailsView : ViewBase
    {
        private const int PictureSize = 168;

        private readonly StudentRepository repo = new();
        private readonly StudentActions actions;
        private Student? current;

        private readonly PictureBox picture = new() { Size = new Size(PictureSize, PictureSize), Margin = new Padding(0, 4, 36, 0) };
        private readonly Label lblName = Ui.Text("", Theme.H1, Theme.Text);
        private readonly Label lblId = Ui.Text("", Theme.Body, Theme.TextMuted);
        private readonly Label valEmail = Ui.Text("", Theme.Body, Theme.Text);
        private readonly Label valContact = Ui.Text("", Theme.Body, Theme.Text);
        private readonly Label valCourse = Ui.Text("", Theme.Body, Theme.Text);
        private readonly Label valYear = Ui.Text("", Theme.Body, Theme.Text);

        /// <summary>Raised when the user goes back to the list, or the student no longer exists.</summary>
        public event EventHandler? BackRequested;

        public StudentDetailsView()
        {
            actions = new StudentActions(repo);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Theme.Bg };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.Controls.Add(BuildTopBar(), 0, 0);
            root.Controls.Add(BuildContent(), 0, 1);
            Controls.Add(root);
        }

        private Control BuildTopBar()
        {
            var bar = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 4, Margin = new Padding(0, 0, 0, 28) };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var back = new FlatButton("‹  Students", ButtonKind.Ghost);
            back.Click += (_, _) => BackRequested?.Invoke(this, EventArgs.Empty);

            var edit = new FlatButton("Edit", ButtonKind.Primary) { Margin = new Padding(0, 0, 8, 0) };
            edit.Click += async (_, _) => await EditAsync();
            var delete = new FlatButton("Delete", ButtonKind.Danger);
            delete.Click += async (_, _) => await DeleteAsync();

            bar.Controls.Add(back, 0, 0);
            bar.Controls.Add(edit, 2, 0);
            bar.Controls.Add(delete, 3, 0);
            return bar;
        }

        private Control BuildContent()
        {
            var content = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 1, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var info = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, Margin = Padding.Empty };
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            lblId.Margin = new Padding(2, 4, 0, 28);
            info.Controls.Add(lblName, 0, 0);
            info.SetColumnSpan(lblName, 2);
            info.Controls.Add(lblId, 0, 1);
            info.SetColumnSpan(lblId, 2);

            AddField(info, 2, 0, "Email address", valEmail);
            AddField(info, 2, 1, "Contact number", valContact);
            AddField(info, 3, 0, "Course / program", valCourse);
            AddField(info, 3, 1, "Year level", valYear);

            content.Controls.Add(picture, 0, 0);
            content.Controls.Add(info, 1, 0);
            return content;
        }

        private static void AddField(TableLayoutPanel grid, int row, int column, string caption, Label value)
        {
            var block = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0, 0, 24, 24)
            };
            block.Controls.Add(Ui.Text(caption.ToUpperInvariant(), Theme.Caption, Theme.TextMuted, new Padding(0, 0, 0, 4)));
            block.Controls.Add(value);
            grid.Controls.Add(block, column, row);
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
            SetValue(valEmail, student.Email);
            SetValue(valContact, student.ContactNumber);
            SetValue(valCourse, student.Course);
            SetValue(valYear, student.YearLevel == null ? null : $"Year {student.YearLevel}");

            var old = picture.Image;
            picture.Image = Avatar.Create(student, PictureSize, 12);
            old?.Dispose();
        }

        /// <summary>Shows the value, or a faint "Not provided" when the optional field is empty.</summary>
        private static void SetValue(Label label, string? value)
        {
            bool has = !string.IsNullOrWhiteSpace(value);
            label.Text = has ? value : "Not provided";
            label.ForeColor = has ? Theme.Text : Theme.TextFaint;
        }

        private async Task EditAsync()
        {
            if (current == null) return;
            if (await actions.EditAsync(this, current))
                await ShowStudentAsync(current.Id);
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
