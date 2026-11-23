using ClassTrack.UI;

namespace ClassTrack.Views
{
    /// <summary>Common behaviour for the pages shown in the main window: glass backdrop, entrance motion, DB guard.</summary>
    public abstract class ViewBase : UserControl
    {
        private static readonly Padding BasePadding = new(6, 4, 6, 6);
        private readonly Tween entrance;

        protected ViewBase()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            BackColor = Color.Transparent;
            Dock = DockStyle.Fill;
            ForeColor = Theme.Text;
            Font = Theme.Body;
            Padding = BasePadding;
            entrance = new Tween(ApplyEntrance, 1f);
        }


        protected override void OnPaintBackground(PaintEventArgs e) => Backdrop.PaintBehind(e.Graphics, this);

        /// <summary>The page rises 14 px into place over 200 ms (ease-out). Skipped when Windows animations are off.</summary>
        public void PlayEntrance()
        {
            entrance.Set(0);
            ApplyEntrance();
            entrance.To(1, 220);
        }

        private void ApplyEntrance()
        {
            int offset = (int)Math.Round(14 * (1 - entrance.Value));
            var wanted = new Padding(BasePadding.Left, BasePadding.Top + offset, BasePadding.Right, BasePadding.Bottom);
            if (Padding == wanted) return;
            Padding = wanted;
            Invalidate(true);   // transparent children leave pixels behind when they move; repaint the whole page
        }

        /// <summary>Reloads whatever this page shows from the database.</summary>
        public abstract Task RefreshDataAsync();

        /// <summary>Runs a database action; shows a friendly dialog and returns false on failure.</summary>
        protected Task<bool> TryDbAsync(Func<Task> action) => DbGuard.RunAsync(this, action);
    }
}
