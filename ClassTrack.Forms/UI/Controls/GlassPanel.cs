namespace ClassTrack.UI.Controls
{
    /// <summary>A translucent glass surface that hosts other controls. Draws the backdrop behind it, then the glass.</summary>
    public class GlassPanel : Panel
    {
        public GlassPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Padding = new Padding(22);
            Margin = Padding.Empty;
        }

        /// <summary>Corner radius in pixels.</summary>
        [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int Radius { get; set; } = 20;

        /// <summary>Use the denser table surface (for grids) instead of translucent glass.</summary>
        [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool Solid { get; set; }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Backdrop.PaintBehind(e.Graphics, this);   // whatever is behind us: backdrop, then any glass we sit inside
            Glass.Paint(e.Graphics, ClientRectangle, Radius, 0f, Solid);
        }
    }

    /// <summary>A transparent container that shows the window backdrop (for page layout and scrolling regions).</summary>
    public class ClearPanel : Panel
    {
        public ClearPanel()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            BackColor = Color.Transparent;
            Margin = Padding.Empty;
        }


        protected override void OnPaintBackground(PaintEventArgs e) => Backdrop.PaintBehind(e.Graphics, this);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (AutoScroll) Theme.ApplyDarkScrollbars(Handle);
        }
    }
}
