namespace ClassTrack
{
    /// <summary>Add / Edit dialog for a student.</summary>
    public class StudentForm : Form
    {
        private readonly TextBox txtFirstName = new();
        private readonly TextBox txtSurname = new();

        public string FirstName => txtFirstName.Text.Trim();
        public string Surname => txtSurname.Text.Trim();

        public StudentForm(Student? existing = null)
        {
            bool editing = existing != null;

            Text = editing ? "Edit Student" : "Add Student";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(380, editing ? 250 : 220);

            int y = 20;

            if (editing)
            {
                Controls.Add(MakeLabel($"Id: {existing!.Id}", 20, y, bold: true));
                y += 32;
            }

            Controls.Add(MakeLabel("First Name", 20, y));
            txtFirstName.SetBounds(20, y + 24, 340, 28);
            Controls.Add(txtFirstName);
            y += 64;

            Controls.Add(MakeLabel("Surname", 20, y));
            txtSurname.SetBounds(20, y + 24, 340, 28);
            Controls.Add(txtSurname);
            y += 68;

            var btnSave = new Button
            {
                Text = "Save",
                Bounds = new Rectangle(190, y, 80, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 81, 181),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (_, _) => Save();

            var btnCancel = new Button
            {
                Text = "Cancel",
                Bounds = new Rectangle(280, y, 80, 34),
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(108, 117, 125);

            Controls.Add(btnSave);
            Controls.Add(btnCancel);
            AcceptButton = btnSave;
            CancelButton = btnCancel;

            if (editing)
            {
                txtFirstName.Text = existing!.FirstName;
                txtSurname.Text = existing.Surname;
            }
        }

        private static Label MakeLabel(string text, int x, int y, bool bold = false) => new()
        {
            Text = text,
            AutoSize = true,
            Location = new Point(x, y),
            Font = new Font("Segoe UI", 10F, bold ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = Color.FromArgb(33, 37, 41)
        };

        private void Save()
        {
            if (FirstName.Length == 0 || Surname.Length == 0)
            {
                MessageBox.Show(this, "First name and surname are required.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
