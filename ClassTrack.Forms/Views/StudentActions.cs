using ClassTrack.Data;
using ClassTrack.Services;
using ClassTrack.UI;

namespace ClassTrack.Views
{
    /// <summary>
    /// The delete flow (confirmation + picture file + database + toast), shared by the directory and the
    /// details page so both behave identically. Add and edit live in <see cref="StudentEditorView"/>.
    /// </summary>
    public sealed class StudentActions
    {
        private readonly StudentRepository repo;

        public StudentActions(StudentRepository repo) => this.repo = repo;

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
    }
}
