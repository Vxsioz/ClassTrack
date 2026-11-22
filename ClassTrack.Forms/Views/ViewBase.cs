using ClassTrack.UI;

namespace ClassTrack.Views
{
    /// <summary>Common behaviour for the pages shown in the main window.</summary>
    public abstract class ViewBase : UserControl
    {
        protected ViewBase()
        {
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = Theme.Body;
            Padding = new Padding(36, 30, 36, 18);
        }

        /// <summary>Reloads whatever this page shows from the database.</summary>
        public abstract Task RefreshDataAsync();

        /// <summary>Runs a database action; shows a friendly dialog and returns false on failure.</summary>
        protected Task<bool> TryDbAsync(Func<Task> action) => DbGuard.RunAsync(this, action);
    }
}
