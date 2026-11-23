using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace ClassTrack.UI.Controls
{
    /// <summary>A slim dark slider (0–100). Drag, click the track, or use Left/Right/Home/End.</summary>
    public sealed class DarkSlider : Control
    {
        private const int ThumbSize = 16;
        private int value;
        private bool dragging;
        private bool hover;

        public event EventHandler? ValueChanged;

        public DarkSlider()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            TabStop = true;
            Cursor = Cursors.Hand;
            Height = 30;
            Margin = Padding.Empty;
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Value
        {
            get => value;
            set
            {
                int v = Math.Clamp(value, 0, 100);
                if (v == this.value) return;
                this.value = v;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private int TrackLeft => ThumbSize / 2;
        private int TrackWidth => Math.Max(1, Width - ThumbSize);

        private void SetFromX(int x) => Value = (int)Math.Round((x - TrackLeft) * 100.0 / TrackWidth);

        protected override void OnPaintBackground(PaintEventArgs e) { }   // painted at the start of OnPaint instead (buffered controls ignore this)

        protected override void OnPaint(PaintEventArgs e)
        {
            Backdrop.PaintBehind(e.Graphics, this);   // what is behind us: backdrop and any glass around us
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int cy = Height / 2;
            int thumbX = TrackLeft + (int)Math.Round(TrackWidth * value / 100.0);

            using (var track = new Pen(Theme.Border, 3) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawLine(track, TrackLeft, cy, TrackLeft + TrackWidth, cy);
            using (var filled = new Pen(Theme.Accent, 3) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawLine(filled, TrackLeft, cy, thumbX, cy);

            var thumb = new Rectangle(thumbX - ThumbSize / 2, cy - ThumbSize / 2, ThumbSize, ThumbSize);
            using (var fill = new SolidBrush(dragging || hover ? Color.White : Theme.Text))
                g.FillEllipse(fill, thumb);
            if (Focused && ShowFocusCues)
            {
                thumb.Inflate(3, 3);
                using var ring = new Pen(Theme.Accent, 1.5F);
                g.DrawEllipse(ring, thumb);
            }
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Focus();
            dragging = true;
            Capture = true;
            SetFromX(e.X);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (dragging) SetFromX(e.X);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            dragging = false;
            Capture = false;
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData) =>
            keyData is Keys.Left or Keys.Right or Keys.Home or Keys.End || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.Left: Value -= 5; e.Handled = true; break;
                case Keys.Right: Value += 5; e.Handled = true; break;
                case Keys.Home: Value = 0; e.Handled = true; break;
                case Keys.End: Value = 100; e.Handled = true; break;
            }
        }
    }
}
