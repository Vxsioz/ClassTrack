using ClassTrack.Data;
using ClassTrack.Services;
using ClassTrack.UI;

namespace ClassTrack.Views
{
    /// <summary>
    /// The archive lifecycle, shared by the directory, the details page and the Archive page so they behave identically:
    /// Archive (hide, keep everything) → Restore (bring back) or Delete forever (remove the record and its picture).
    /// Add and edit live in <see cref="StudentEditorView"/>.
    /// </summary>
    public sealed class StudentActions
    {
        private readonly StudentRepository repo;

        public StudentActions(StudentRepository repo) => this.repo = repo;

        /// <summary>Moves the student to the Archive after confirmation. Nothing is deleted. Returns true if archived.</summary>
        public async Task<bool> ArchiveAsync(Control owner, Student student)
        {
            if (!Dialogs.Confirm(owner.FindForm(), "Archive student",
                    $"Archive {student.FullName}? They will move to the Archive, where you can restore them at any time.",
                    "Archive", danger: false))
                return false;

            bool done = false;
            if (!await DbGuard.RunAsync(owner, async () => done = await repo.ArchiveAsync(student.Id))) return false;

            if (done) Toast.Show(owner, $"{student.FullName} was moved to the Archive.");
            else Dialogs.Info(owner.FindForm(), "Not found", "That student no longer exists.");
            return done;
        }

        /// <summary>Returns an archived student to the directory. Returns true if restored.</summary>
        public async Task<bool> RestoreAsync(Control owner, Student student)
        {
            bool done = false;
            if (!await DbGuard.RunAsync(owner, async () => done = await repo.RestoreAsync(student.Id))) return false;

            if (done) Toast.Show(owner, $"{student.FullName} was restored.");
            else Dialogs.Info(owner.FindForm(), "Not found", "That student no longer exists.");
            return done;
        }

        /// <summary>PERMANENTLY removes an archived student and their picture, after a warning. Returns true if deleted.</summary>
        public async Task<bool> DeleteForeverAsync(Control owner, Student student)
        {
            if (!Dialogs.Confirm(owner.FindForm(), "Delete permanently",
                    $"Permanently delete {student.FullName}? This removes the record and their picture and cannot be undone.",
                    "Delete forever"))
                return false;

            (bool Deleted, string? PhotoPath) result = default;
            if (!await DbGuard.RunAsync(owner, async () => result = await repo.DeleteAsync(student.Id))) return false;

            if (result.Deleted)
            {
                ImageStore.Delete(result.PhotoPath);
                Toast.Show(owner, $"{student.FullName} was permanently deleted.");
            }
            return result.Deleted;
        }
    }
}
