using System.Net.Mail;
using System.Text.RegularExpressions;
using ClassTrack.Data;
using ClassTrack.Services;
using ClassTrack.UI;
using ClassTrack.UI.Controls;

namespace ClassTrack.Views
{
    /// <summary>
    /// The Add / Edit student page. Basic, contact and academic information plus a profile picture.
    ///
    /// Picture rule: nothing is written to disk until Save. A new file is written first, the old one is
    /// deleted only after the database accepted the change, and a failed save removes the new file again,
    /// so one student's picture is never lost or orphaned.
    /// </summary>
    public sealed class StudentEditorView : ViewBase
    {
        private const int PhotoSize = 164;
        private static readonly Regex PhonePattern = new(@"^\+?[0-9 ()\-]{5,30}$");

        private readonly StudentRepository repo = new();
        private readonly ScrollHost page = new();
        private readonly PageHeader header = new("Add student", "Create a new student record.");

        private readonly DarkTextBox txtFirstName = new();
        private readonly DarkTextBox txtSurname = new();
        private readonly DarkTextBox txtEmail = new();
        private readonly DarkTextBox txtContact = new();
        private readonly DarkTextBox txtCourse = new();
        private readonly DarkSelect cboYear = new();
        private readonly Label lblError = Ui.Text("", Theme.Small, Theme.Danger);

        private readonly GlassPicture photoBox = new() { Size = new Size(PhotoSize, PhotoSize), Margin = new Padding(0, 0, 0, 18), BackColor = Color.Transparent };
        private readonly FlatButton btnPhoto = new("Choose picture");
        private readonly FlatButton btnRemovePhoto = new("Remove picture", ButtonKind.Ghost);
        private readonly FlatButton btnSave = new("Save student", ButtonKind.Primary);

        private Student? editing;          // null when adding
        private string? originalPhotoPath;
        private Bitmap? newPhoto;          // cropped, not yet saved
        private bool photoRemoved;

        /// <summary>Raised after a successful save, with the student's ID.</summary>
        public event EventHandler<int>? Saved;

        /// <summary>Raised when the user cancels.</summary>
        public event EventHandler? Cancelled;

        public StudentEditorView()
        {
            Controls.Add(page);
            page.AddRow(header);
            page.AddRow(BuildBody());
            page.AddRow(BuildActionBar());

            txtFirstName.Caption = "First name"; txtSurname.Caption = "Surname"; txtEmail.Caption = "Email address";
            txtContact.Caption = "Contact number"; txtCourse.Caption = "Course"; cboYear.AccessibleName = "Year level";
            photoBox.AccessibleName = "Profile picture preview";
            txtFirstName.MaxLength = 50;
            txtSurname.MaxLength = 50;
            txtEmail.MaxLength = 254;
            txtContact.MaxLength = 30;
            txtCourse.MaxLength = 100;
            cboYear.Items.Add("Not set");
            for (int y = 1; y <= Student.MaxYearLevel; y++) cboYear.Items.Add($"Year {y}");
            cboYear.SelectedIndex = 0;

            // The initials placeholder follows the typed name until a picture is chosen.
            txtFirstName.Inner.TextChanged += (_, _) => { if (!HasPhoto) RefreshPhoto(); };
            txtSurname.Inner.TextChanged += (_, _) => { if (!HasPhoto) RefreshPhoto(); };
            txtFirstName.Inner.KeyDown += EnterToSave;
            txtSurname.Inner.KeyDown += EnterToSave;
            txtEmail.Inner.KeyDown += EnterToSave;
            txtContact.Inner.KeyDown += EnterToSave;
            txtCourse.Inner.KeyDown += EnterToSave;
        }

        public override Task RefreshDataAsync() => Task.CompletedTask;   // the form holds the user's edits; never reload over them

        // =====================================================================
        //  Layout
        // =====================================================================

        private Control BuildBody()
        {
            var row = new TablePanel { BackColor = Color.Transparent, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 0, 0, 18) };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.Controls.Add(BuildPicturePanel(), 0, 0);
            row.Controls.Add(BuildFieldsPanel(), 1, 0);
            return row;
        }

        private Control BuildPicturePanel()
        {
            var panel = new GlassPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(26, 24, 26, 24), Margin = new Padding(0, 0, 18, 0), Radius = 24 };
            var stack = new FlowPanel { BackColor = Color.Transparent, Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };

            stack.Controls.Add(Ui.Text("Profile picture", Theme.H3, Theme.Text, new Padding(0, 0, 0, 16)));
            stack.Controls.Add(photoBox);
            btnPhoto.Margin = new Padding(0, 0, 0, 8);
            btnPhoto.Click += (_, _) => ChoosePicture();
            btnRemovePhoto.Click += (_, _) =>
            {
                newPhoto?.Dispose();
                newPhoto = null;
                photoRemoved = true;
                RefreshPhoto();
            };
            stack.Controls.Add(btnPhoto);
            stack.Controls.Add(btnRemovePhoto);
            var hint = Ui.Text("JPG or PNG, up to 10 MB. You will crop it to a square next.", Theme.Small, Theme.TextFaint, new Padding(0, 16, 0, 0));
            hint.MaximumSize = new Size(190, 0);
            stack.Controls.Add(hint);

            panel.Controls.Add(stack);
            return panel;
        }

        private Control BuildFieldsPanel()
        {
            var panel = new GlassPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(28, 24, 28, 12), Radius = 24 };   // Top, not Fill: don't stretch to the picture card's height
            var stack = new TablePanel { BackColor = Color.Transparent, Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1 };

            stack.Controls.Add(Section("Basic information", ("First name *", txtFirstName), ("Surname *", txtSurname)));
            stack.Controls.Add(Section("Contact information", ("Email address", txtEmail), ("Contact number", txtContact)));
            stack.Controls.Add(Section("Academic information", ("Course / program", txtCourse), ("Year level", cboYear)));
            panel.Controls.Add(stack);
            return panel;
        }

        /// <summary>A titled group with two fields side by side.</summary>
        private static Control Section(string title, (string Caption, Control Input) left, (string Caption, Control Input) right)
        {
            var section = new TablePanel { BackColor = Color.Transparent, Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 0, 0, 10) };
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var heading = Ui.Text(title.ToUpperInvariant(), Theme.Caption, Theme.AccentSoft, new Padding(0, 0, 0, 12));
            section.Controls.Add(heading, 0, 0);
            section.SetColumnSpan(heading, 2);

            Place(section, 0, left, new Padding(0, 0, 8, 0));
            Place(section, 1, right, new Padding(8, 0, 0, 0));
            return section;
        }

        private static void Place(TableLayoutPanel section, int column, (string Caption, Control Input) field, Padding outer)
        {
            var cell = new TablePanel { BackColor = Color.Transparent, Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1, Margin = outer };
            cell.Controls.Add(Ui.Text(field.Caption, Theme.Small, Theme.TextMuted, new Padding(2, 0, 0, 6)), 0, 0);
            field.Input.Dock = DockStyle.Fill;
            field.Input.Margin = new Padding(0, 0, 0, 14);
            cell.Controls.Add(field.Input, 0, 1);
            section.Controls.Add(cell, column, 1);
        }

        private Control BuildActionBar()
        {
            var bar = new TablePanel { BackColor = Color.Transparent, Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, Margin = new Padding(0, 0, 0, 8) };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            lblError.Anchor = AnchorStyles.Left;
            var cancel = new FlatButton("Cancel") { Margin = new Padding(0, 0, 10, 0) };
            cancel.Click += (_, _) => Cancelled?.Invoke(this, EventArgs.Empty);
            btnSave.Click += async (_, _) => await SaveAsync();

            bar.Controls.Add(lblError, 0, 0);
            bar.Controls.Add(cancel, 1, 0);
            bar.Controls.Add(btnSave, 2, 0);
            return bar;
        }

        private async void EnterToSave(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            await SaveAsync();
        }

        // =====================================================================
        //  Loading
        // =====================================================================

        /// <summary>Prepares the form: blank for a new student, or filled in for an existing one.</summary>
        public async Task LoadAsync(int? studentId)
        {
            newPhoto?.Dispose();
            newPhoto = null;
            photoRemoved = false;
            editing = null;
            originalPhotoPath = null;
            lblError.Text = "";
            foreach (var box in new[] { txtFirstName, txtSurname, txtEmail, txtContact, txtCourse }) { box.Text = ""; box.Invalid = false; }
            cboYear.SelectedIndex = 0;

            await LoadCourseSuggestionsAsync();

            if (studentId != null)
            {
                Student? found = null;
                if (!await TryDbAsync(async () => found = await repo.GetAsync(studentId.Value)))
                {
                    Cancelled?.Invoke(this, EventArgs.Empty);
                    return;
                }
                if (found == null)
                {
                    Dialogs.Info(FindForm(), "Not found", "That student no longer exists.");
                    Cancelled?.Invoke(this, EventArgs.Empty);
                    return;
                }
                editing = found;
                originalPhotoPath = found.PhotoPath;
                txtFirstName.Text = found.FirstName;
                txtSurname.Text = found.Surname;
                txtEmail.Text = found.Email ?? "";
                txtContact.Text = found.ContactNumber ?? "";
                txtCourse.Text = found.Course ?? "";
                cboYear.SelectedIndex = found.YearLevel is >= 1 and <= Student.MaxYearLevel ? found.YearLevel.Value : 0;
            }

            header.Title = editing == null ? "Add student" : "Edit student";
            header.Subtitle = editing == null ? "Create a new student record." : $"Update {editing.FullName} (ID {editing.Id}).";
            btnSave.Text = editing == null ? "Add student" : "Save changes";
            RefreshPhoto();
            page.Fit();
            txtFirstName.Focus();
        }

        /// <summary>Typing a course offers the ones already in use, so spelling stays consistent. Failure just means no suggestions.</summary>
        private async Task LoadCourseSuggestionsAsync()
        {
            try
            {
                var courses = await repo.GetCoursesAsync();
                var source = new AutoCompleteStringCollection();
                source.AddRange(courses.ToArray());
                txtCourse.Inner.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtCourse.Inner.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtCourse.Inner.AutoCompleteCustomSource = source;
            }
            catch (Exception) { }
        }

        // =====================================================================
        //  Picture
        // =====================================================================

        private bool HasPhoto => newPhoto != null || (!photoRemoved && !string.IsNullOrEmpty(originalPhotoPath));

        private void RefreshPhoto()
        {
            Bitmap? stored = null;
            try
            {
                Image? shown = newPhoto;
                if (shown == null && !photoRemoved)
                    shown = stored = ImageStore.Load(originalPhotoPath);

                var old = photoBox.Image;
                photoBox.Image = Avatar.CreateFrom(shown, Avatar.Initials(txtFirstName.Text, txtSurname.Text), PhotoSize, 28);
                old?.Dispose();
            }
            finally
            {
                stored?.Dispose();
            }

            btnPhoto.Text = HasPhoto ? "Change picture" : "Choose picture";
            btnRemovePhoto.Visible = HasPhoto;
        }

        private void ChoosePicture()
        {
            using var picker = new OpenFileDialog
            {
                Title = "Choose a picture",
                Filter = "Pictures (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                CheckFileExists = true
            };
            if (picker.ShowDialog(FindForm()) != DialogResult.OK) return;

            string? problem = ImageStore.ValidateSourceFile(picker.FileName);
            if (problem != null)
            {
                Dialogs.Error(FindForm(), "Picture", problem);
                return;
            }

            Bitmap source;
            try
            {
                source = ImageStore.LoadSource(picker.FileName);
            }
            catch (Exception)
            {
                Dialogs.Error(FindForm(), "Picture", "That file could not be read as an image. It may be damaged.");
                return;
            }

            using (source)
            using (var cropper = new CropDialog(source))
            {
                if (cropper.ShowDialog(FindForm()) != DialogResult.OK || cropper.Result == null) return;

                newPhoto?.Dispose();
                newPhoto = cropper.Result;
                photoRemoved = false;
            }
            RefreshPhoto();
        }

        // =====================================================================
        //  Validation + saving
        // =====================================================================

        private static string? NullIfBlank(string text)
        {
            string t = text.Trim();
            return t.Length == 0 ? null : t;
        }

        /// <summary>Copies the entered values onto a student (blank optional fields become null). The picture is handled by SaveAsync.</summary>
        private void ApplyTo(Student s)
        {
            s.FirstName = txtFirstName.Text.Trim();
            s.Surname = txtSurname.Text.Trim();
            s.Email = NullIfBlank(txtEmail.Text);
            s.ContactNumber = NullIfBlank(txtContact.Text);
            s.Course = NullIfBlank(txtCourse.Text);
            s.YearLevel = cboYear.SelectedIndex > 0 ? (byte)cboYear.SelectedIndex : null;
        }

        private bool Fail(string message, DarkTextBox field)
        {
            lblError.Text = message;
            field.Invalid = true;
            field.Focus();
            return false;
        }

        private bool ValidateForm()
        {
            lblError.Text = "";
            string email = txtEmail.Text.Trim();
            string phone = txtContact.Text.Trim();

            if (txtFirstName.Text.Trim().Length == 0) return Fail("First name is required.", txtFirstName);
            if (txtSurname.Text.Trim().Length == 0) return Fail("Surname is required.", txtSurname);
            if (email.Length > 0 && !(MailAddress.TryCreate(email, out var addr) && addr.Address == email && email.Contains('.')))
                return Fail("Enter a valid email address, e.g. name@school.edu.", txtEmail);
            if (phone.Length > 0 && !PhonePattern.IsMatch(phone))
                return Fail("Contact number may only contain digits, spaces, + ( ) and -.", txtContact);
            return true;
        }

        private async Task SaveAsync()
        {
            if (!btnSave.Enabled || !ValidateForm()) return;
            btnSave.Enabled = false;
            try
            {
                var student = editing != null ? new Student { Id = editing.Id } : new Student();
                ApplyTo(student);

                string? newPath = null;
                if (newPhoto != null)
                {
                    try { newPath = ImageStore.Save(newPhoto); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Runtime.InteropServices.ExternalException)
                    {
                        Dialogs.Error(FindForm(), "Picture", "The picture could not be saved, so nothing was changed.\n\n" + ex.Message);
                        return;
                    }
                    student.PhotoPath = newPath;
                }
                else
                {
                    student.PhotoPath = photoRemoved ? null : originalPhotoPath;
                }

                bool found = true;
                bool ran = await TryDbAsync(async () =>
                {
                    if (editing == null) await repo.AddAsync(student);
                    else found = await repo.UpdateAsync(student);
                });
                if (!ran || !found)
                {
                    ImageStore.Delete(newPath);   // the change did not land, so the new file is not needed
                    if (ran) Dialogs.Info(FindForm(), "Not found", "That student no longer exists.");
                    return;
                }

                if (editing != null && originalPhotoPath != student.PhotoPath) ImageStore.Delete(originalPhotoPath);   // replaced or removed
                Toast.Show(this, editing == null ? $"{student.FullName} was added." : "Changes saved.");
                Saved?.Invoke(this, student.Id);
            }
            finally
            {
                btnSave.Enabled = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                newPhoto?.Dispose();
                photoBox.Image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
