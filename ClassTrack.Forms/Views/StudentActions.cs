using ClassTrack.Data;
using ClassTrack.Services;
using ClassTrack.UI;

namespace ClassTrack.Views
{
    /// <summary>
    /// Add / edit / delete flows (dialog + picture file + database + toast), shared by the list and
    /// the details page so both behave identically.
    ///
    /// Picture rule: a new file is written only after the dialog is saved, the old file is deleted only
    /// after the database accepted the change, and a failed save removes the new file again. That keeps
    /// one student's picture from ever being lost or orphaned.
    /// </summary>
    public sealed class StudentActions
    {
        private readonly StudentRepository repo;

        public StudentActions(StudentRepository repo) => this.repo = repo;

        public async Task<Student?> AddAsync(Control owner)
        {
            using var dlg = new StudentForm(null, await GetCoursesAsync());
            if (dlg.ShowDialog(owner.FindForm()) != DialogResult.OK) return null;

            var student = new Student();
            dlg.ApplyTo(student);

            string? newPath = SavePhoto(owner, dlg);
            if (dlg.NewPhoto != null && newPath == null) return null;   // picture could not be written; error already shown
            student.PhotoPath = newPath;

            if (!await DbGuard.RunAsync(owner, () => repo.AddAsync(student)))
            {
                ImageStore.Delete(newPath);   // don't leave an orphan behind
                return null;
            }

            Toast.Show(owner, $"{student.FullName} was added.");
            return student;
        }

        /// <summary>Returns true if changes were saved.</summary>
        public async Task<bool> EditAsync(Control owner, Student student)
        {
            using var dlg = new StudentForm(student, await GetCoursesAsync());
            if (dlg.ShowDialog(owner.FindForm()) != DialogResult.OK) return false;

            string? oldPath = student.PhotoPath;
            dlg.ApplyTo(student);

            string? newPath = null;
            if (dlg.NewPhoto != null)
            {
                newPath = SavePhoto(owner, dlg);
                if (newPath == null) return false;
                student.PhotoPath = newPath;
            }
            else if (dlg.RemovePhoto)
            {
                student.PhotoPath = null;
            }

            bool found = false;
            bool ran = await DbGuard.RunAsync(owner, async () => found = await repo.UpdateAsync(student));
            if (!ran || !found)
            {
                ImageStore.Delete(newPath);   // the change did not land, so the new file is not needed
                if (ran) Dialogs.Info(owner.FindForm(), "Not found", "That student no longer exists.");
                return false;
            }

            if (oldPath != student.PhotoPath) ImageStore.Delete(oldPath);   // replaced or removed
            Toast.Show(owner, "Changes saved.");
            return true;
        }

        /// <summary>Returns true if the student was deleted.</summary>
        public async Task<bool> DeleteAsync(Control owner, Student student)
        {
            if (!Dialogs.Confirm(owner.FindForm(), "Delete student",
                    $"Delete {student.FullName}? This cannot be undone."))
                return false;

            (bool Deleted, string? PhotoPath) result = default;
            if (!await DbGuard.RunAsync(owner, async () => result = await repo.DeleteAsync(student.Id))) return false;

            if (result.Deleted)
            {
                ImageStore.Delete(result.PhotoPath);
                Toast.Show(owner, $"{student.FullName} was deleted.");
            }
            return result.Deleted;
        }

        /// <summary>Writes the dialog's cropped picture to disk. Returns its relative path, or null if there is none / it failed.</summary>
        private static string? SavePhoto(Control owner, StudentForm dlg)
        {
            if (dlg.NewPhoto == null) return null;
            try
            {
                return ImageStore.Save(dlg.NewPhoto);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Runtime.InteropServices.ExternalException)
            {
                Dialogs.Error(owner.FindForm(), "Picture",
                    "The picture could not be saved, so nothing was changed.\n\n" + ex.Message);
                return null;
            }
        }

        /// <summary>Course names already in use, for the editor's suggestion list. Failure just means no suggestions.</summary>
        private async Task<List<string>> GetCoursesAsync()
        {
            try { return await repo.GetCoursesAsync(); }
            catch (Exception) { return new List<string>(); }
        }
    }
}
