namespace ClassTrack.UI
{
    /// <summary>Base for every window: ambient glass backdrop, standard font, dark title bar.</summary>
    public class DarkForm : Form
    {
        private Bitmap? backdrop;

        public DarkForm()
        {
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = Theme.Body;
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        /// <summary>The cached backdrop for the current client size (re-rendered after a resize).</summary>
        public Bitmap GetBackdrop()
        {
            var size = ClientSize;
            if (backdrop == null || backdrop.Width != Math.Max(size.Width, 1) || backdrop.Height != Math.Max(size.Height, 1))
            {
                backdrop?.Dispose();
                backdrop = Backdrop.Render(size.Width, size.Height);
            }
            return backdrop;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.DrawImageUnscaled(GetBackdrop(), 0, 0);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.ApplyDarkTitleBar(Handle);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) backdrop?.Dispose();
            base.Dispose(disposing);
        }
    }
}
