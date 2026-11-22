using ClassTrack.Data;
using ClassTrack.UI;
using Microsoft.EntityFrameworkCore;

namespace ClassTrack
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Anything unexpected shows a themed message instead of the default Windows crash box.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) =>
                Dialogs.Error(null, "Unexpected error", "Something went wrong.\n\n" + e.Exception.GetBaseException().Message);

            // Bring the database up to date (creates it on a fresh machine). If SQL Server is
            // unreachable the app still opens and each screen reports the problem itself.
            try
            {
                using var db = new AppDbContext();
                db.Database.Migrate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "ClassTrack could not reach its database (SQL Server Express, .\\SQLEXPRESS).\n\n" +
                    ex.GetBaseException().Message,
                    "ClassTrack", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            Application.Run(new Form1());
        }
    }
}
