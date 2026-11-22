using System.Net.Mail;
using System.Text.RegularExpressions;
using ClassTrack.Services;
using ClassTrack.UI;
using ClassTrack.UI.Controls;

namespace ClassTrack
{
    /// <summary>Add / Edit dialog for a student. Optional fields may be left blank.</summary>
    public class StudentForm : DarkForm
    {
        public const int MaxYearLevel = 6;
        private const int PhotoSize = 150;
        private static readonly Regex PhonePattern = new(@"^\+?[0-9 ()\-]{5,30}$");

        private readonly DarkTextBox txtFirstName = new();
        private readonly DarkTextBox txtSurname = new();
        private readonly DarkTextBox txtEmail = new();
        private readonly DarkTextBox txtContact = new();
        private readonly DarkTextBox txtCourse = new();
        private readonly DarkSelect cboYear = new();
        private readonly Label lblError;

        // Picture state. Nothing is written to disk here: the caller saves NewPhoto after Save.
        private readonly PictureBox photoBox = new() { Size = new Size(PhotoSize, PhotoSize), Margin = new Padding(0, 0, 0, 12) };
        private readonly FlatButton btnPhoto = new("Choose picture");
        private readonly FlatButton btnRemovePhoto = new("Remove picture", ButtonKind.Ghost);
        private readonly string? originalPhotoPath;
        private Bitmap? newPhoto;
        private bool photoRemoved;

        /// <summary>A newly cropped picture to save, or null if the picture was not changed. Owned by this dialog.</summary>
        public Bitmap? NewPhoto => newPhoto;

        /// <summary>True if the user removed the existing picture (and did not pick a replacement).</summary>
        public bool RemovePhoto => photoRemoved && newPhoto == null;

        public StudentForm(Student? existing = null, IEnumerable<string>? courseSuggestions = null)
        {
            bool editing = existing != null;
            originalPhotoPath = existing?.PhotoPath;

            Text = editing ? "Edit student" : "Add student";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // Columns: picture | fields (two columns inside)
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 3,
                Padding = new Padding(28, 24, 28, 22)
            };

            var title = Ui.Text(editing ? "Edit student" : "Add student", Theme.H2, Theme.Text);
            var subtitle = Ui.Text(editing ? $"ID {existing!.Id}" : "First name and surname are required; the rest is optional.",
                Theme.Small, Theme.TextMuted, new Padding(0, 2, 0, 18));
            layout.Controls.Add(title, 0, 0);
            layout.SetColumnSpan(title, 3);
            layout.Controls.Add(subtitle, 0, 1);
            layout.SetColumnSpan(subtitle, 3);

            // Limits match the database columns.
            txtFirstName.MaxLength = 50;
            txtSurname.MaxLength = 50;
            txtEmail.MaxLength = 254;
            txtContact.MaxLength = 30;
            txtCourse.MaxLength = 100;
            if (courseSuggestions != null)
            {
                // Typing offers the courses already in use, so spelling stays consistent.
                var source = new AutoCompleteStringCollection();
                source.AddRange(courseSuggestions.ToArray());
                txtCourse.Inner.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                txtCourse.Inner.AutoCompleteSource = AutoCompleteSource.CustomSource;
                txtCourse.Inner.AutoCompleteCustomSource = source;
            }

            cboYear.Items.Add("Not set");
            for (int y = 1; y <= MaxYearLevel; y++) cboYear.Items.Add($"Year {y}");
            cboYear.SelectedIndex = 0;

            layout.Controls.Add(BuildPhotoPanel(), 0, 2);
            layout.SetRowSpan(layout.GetControlFromPosition(0, 2)!, 6);

            AddPair(layout, 2, "First name *", txtFirstName, "Surname *", txtSurname);
            AddPair(layout, 4, "Email address", txtEmail, "Contact number", txtContact);
            AddPair(layout, 6, "Course / program", txtCourse, "Year level", cboYear);

            lblError = Ui.Text("", Theme.Small, Theme.Danger, new Padding(0, 0, 0, 12));
            layout.Controls.Add(lblError, 0, 8);
            layout.SetColumnSpan(lblError, 3);

            var btnSave = new FlatButton("Save", ButtonKind.Primary) { Margin = new Padding(8, 0, 0, 0) };
            btnSave.Click += (_, _) => Save();
            var btnCancel = new FlatButton("Cancel") { DialogResult = DialogResult.Cancel };
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = Padding.Empty
            };
            buttons.Controls.Add(btnSave);
            buttons.Controls.Add(btnCancel);
            layout.Controls.Add(buttons, 0, 9);
            layout.SetColumnSpan(buttons, 3);

            Controls.Add(layout);
            AcceptButton = btnSave;
            CancelButton = btnCancel;

            if (editing)
            {
                txtFirstName.Text = existing!.FirstName;
                txtSurname.Text = existing.Surname;
                txtEmail.Text = existing.Email ?? "";
                txtContact.Text = existing.ContactNumber ?? "";
                txtCourse.Text = existing.Course ?? "";
                cboYear.SelectedIndex = existing.YearLevel is >= 1 and <= MaxYearLevel ? existing.YearLevel.Value : 0;
            }

            // The initials placeholder follows the typed name until a picture is chosen.
            txtFirstName.Inner.TextChanged += (_, _) => { if (!HasPhoto) RefreshPhoto(); };
            txtSurname.Inner.TextChanged += (_, _) => { if (!HasPhoto) RefreshPhoto(); };
            RefreshPhoto();
        }

        // =====================================================================
        //  Picture
        // =====================================================================

        private bool HasPhoto => newPhoto != null || (!photoRemoved && !string.IsNullOrEmpty(originalPhotoPath));

        private Control BuildPhotoPanel()
        {
            var panel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0, 0, 28, 0)
            };
            panel.Controls.Add(Ui.Text("Picture", Theme.Small, Theme.TextMuted, new Padding(0, 0, 0, 5)));
            panel.Controls.Add(photoBox);
            btnPhoto.Margin = new Padding(0, 0, 0, 6);
            btnPhoto.Click += (_, _) => ChoosePicture();
            btnRemovePhoto.Click += (_, _) =>
            {
                newPhoto?.Dispose();
                newPhoto = null;
                photoRemoved = true;
                RefreshPhoto();
            };
            panel.Controls.Add(btnPhoto);
            panel.Controls.Add(btnRemovePhoto);
            return panel;
        }

        private void RefreshPhoto()
        {
            Bitmap? stored = null;
            try
            {
                Image? shown = newPhoto;
                if (shown == null && !photoRemoved)
                    shown = stored = ImageStore.Load(originalPhotoPath);

                var old = photoBox.Image;
                photoBox.Image = Avatar.CreateFrom(shown, Avatar.Initials(txtFirstName.Text, txtSurname.Text), PhotoSize, 12);
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
            if (picker.ShowDialog(this) != DialogResult.OK) return;

            string? problem = ImageStore.ValidateSourceFile(picker.FileName);
            if (problem != null)
            {
                Dialogs.Error(this, "Picture", problem);
                return;
            }

            Bitmap source;
            try
            {
                source = ImageStore.LoadSource(picker.FileName);
            }
            catch (Exception)
            {
                Dialogs.Error(this, "Picture", "That file could not be read as an image. It may be damaged.");
                return;
            }

            using (source)
            using (var cropper = new CropDialog(source))
            {
                if (cropper.ShowDialog(this) != DialogResult.OK || cropper.Result == null) return;

                newPhoto?.Dispose();
                newPhoto = cropper.Result;
                photoRemoved = false;
            }
            RefreshPhoto();
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

        // =====================================================================
        //  Fields
        // =====================================================================

        /// <summary>Copies the entered values onto a student (blank optional fields become null). Picture is handled by the caller.</summary>
        public void ApplyTo(Student s)
        {
            s.FirstName = txtFirstName.Text.Trim();
            s.Surname = txtSurname.Text.Trim();
            s.Email = NullIfBlank(txtEmail.Text);
            s.ContactNumber = NullIfBlank(txtContact.Text);
            s.Course = NullIfBlank(txtCourse.Text);
            s.YearLevel = cboYear.SelectedIndex > 0 ? (byte)cboYear.SelectedIndex : null;
        }

        private static string? NullIfBlank(string text)
        {
            string t = text.Trim();
            return t.Length == 0 ? null : t;
        }

        private static void AddPair(TableLayoutPanel layout, int row,
            string leftCaption, Control left, string rightCaption, Control right)
        {
            left.Width = 250;
            right.Width = 250;
            left.Margin = new Padding(0, 0, 16, 14);
            right.Margin = new Padding(0, 0, 0, 14);
            layout.Controls.Add(Ui.Text(leftCaption, Theme.Small, Theme.TextMuted, new Padding(0, 0, 0, 5)), 1, row);
            layout.Controls.Add(Ui.Text(rightCaption, Theme.Small, Theme.TextMuted, new Padding(0, 0, 0, 5)), 2, row);
            layout.Controls.Add(left, 1, row + 1);
            layout.Controls.Add(right, 2, row + 1);
        }

        private void Fail(string message, Control focus)
        {
            lblError.Text = message;
            if (focus is DarkTextBox box) box.Focus(); else focus.Focus();
        }

        private void Save()
        {
            string email = txtEmail.Text.Trim();
            string phone = txtContact.Text.Trim();

            if (txtFirstName.Text.Trim().Length == 0) { Fail("First name is required.", txtFirstName); return; }
            if (txtSurname.Text.Trim().Length == 0) { Fail("Surname is required.", txtSurname); return; }
            if (email.Length > 0 && !(MailAddress.TryCreate(email, out var addr) && addr.Address == email && email.Contains('.')))
            {
                Fail("Enter a valid email address, e.g. name@school.edu.", txtEmail);
                return;
            }
            if (phone.Length > 0 && !PhonePattern.IsMatch(phone))
            {
                Fail("Contact number may only contain digits, spaces, + ( ) and -.", txtContact);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
