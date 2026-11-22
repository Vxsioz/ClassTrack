using ClassTrack.UI;

namespace ClassTrack.Views
{
    /// <summary>Runs database work and turns any failure into a friendly dialog instead of a crash.</summary>
    public static class DbGuard
    {
        public static async Task<bool> RunAsync(Control owner, Func<Task> action)
        {
            try
            {
                await action();
                return true;
            }
            catch (Exception ex)
            {
                Dialogs.Error(owner.FindForm(), "Database error",
                    "Could not complete the database operation. Make sure SQL Server Express " +
                    "(.\\SQLEXPRESS) is running and the ClassTrackDb database exists.\n\n" +
                    ex.GetBaseException().Message);
                return false;
            }
        }
    }
}
