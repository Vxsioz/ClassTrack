namespace ClassTrack.UI.Controls
{
    // Building blocks that paint their own background by sampling what is behind them (see Backdrop.PaintBehind).
    // Use these instead of transparent stock controls.

    public class TablePanel : TableLayoutPanel
    {
        public TablePanel() { Margin = Padding.Empty; }
        protected override void OnPaintBackground(PaintEventArgs e) => Backdrop.PaintBehind(e.Graphics, this);
    }

    public class FlowPanel : FlowLayoutPanel
    {
        public FlowPanel() { Margin = Padding.Empty; }
        protected override void OnPaintBackground(PaintEventArgs e) => Backdrop.PaintBehind(e.Graphics, this);
    }

    public class GlassLabel : Label
    {
        public GlassLabel() { }
        protected override void OnPaintBackground(PaintEventArgs e) => Backdrop.PaintBehind(e.Graphics, this);
    }

    public class GlassPicture : PictureBox
    {
        public GlassPicture() { }
        protected override void OnPaintBackground(PaintEventArgs e) => Backdrop.PaintBehind(e.Graphics, this);
    }
}
