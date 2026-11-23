using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ClassTrack.UI.Controls
{
    /// <summary>
    /// Interactive 1:1 crop. A square frame stays fixed in the middle; the user drags the picture
    /// to reposition it under the frame and zooms in and out. The picture always covers the frame
    /// (no empty edges) and is never stretched: scale is uniform in both directions.
    /// </summary>
    public sealed class CropControl : Control
    {
        public const float MaxZoom = 8f;      // relative to "just covers the frame"
        private const int FrameMargin = 28;

        private Bitmap? source;
        private float minScale = 1f;          // scale at which the picture just covers the frame
        private float scale = 1f;             // screen pixels per source pixel
        private PointF offset;                // screen position of the picture's top-left corner
        private RectangleF frame;

        private bool dragging;
        private Point dragStart;
        private PointF dragStartOffset;

        /// <summary>Raised whenever the framed region changes (drag or zoom).</summary>
        public event EventHandler? CropChanged;
        /// <summary>Raised when the zoom changes (so a slider can follow).</summary>
        public event EventHandler? ZoomChanged;

        public CropControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = true;
            Cursor = Cursors.SizeAll;
            AccessibleName = "Crop area";
            AccessibleRole = AccessibleRole.Graphic;
            BackColor = Color.Transparent;
        }

        /// <summary>The picture being cropped (not owned: the caller disposes it).</summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Bitmap? Source
        {
            get => source;
            set { source = value; Reset(); }
        }

        /// <summary>1 = fully zoomed out (picture just covers the frame), up to <see cref="MaxZoom"/>.</summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float Zoom
        {
            get => minScale <= 0 ? 1 : scale / minScale;
            set => ZoomAt(value, new PointF(frame.Left + frame.Width / 2, frame.Top + frame.Height / 2));
        }

        // ---- geometry ---------------------------------------------------------

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Reset();
        }

        /// <summary>Recomputes the frame and centres the picture at minimum zoom.</summary>
        public void Reset()
        {
            float side = Math.Max(1, Math.Min(Width, Height) - 2 * FrameMargin);
            frame = new RectangleF((Width - side) / 2, (Height - side) / 2, side, side);

            if (source != null)
            {
                minScale = side / Math.Min(source.Width, source.Height);
                scale = minScale;
                offset = new PointF(frame.Left + (side - source.Width * scale) / 2,
                                    frame.Top + (side - source.Height * scale) / 2);
                ClampOffset();
            }
            Invalidate();
            ZoomChanged?.Invoke(this, EventArgs.Empty);
            CropChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Keeps the picture covering the whole frame.</summary>
        private void ClampOffset()
        {
            if (source == null) return;
            float w = source.Width * scale, h = source.Height * scale;
            offset.X = Math.Min(frame.Left, Math.Max(frame.Right - w, offset.X));
            offset.Y = Math.Min(frame.Top, Math.Max(frame.Bottom - h, offset.Y));
        }

        /// <summary>Changes zoom while keeping the source point under <paramref name="anchor"/> in place.</summary>
        private void ZoomAt(float zoom, PointF anchor)
        {
            if (source == null) return;
            zoom = Math.Clamp(zoom, 1f, MaxZoom);
            float newScale = minScale * zoom;
            if (Math.Abs(newScale - scale) < 1e-6f) return;

            float sx = (anchor.X - offset.X) / scale;
            float sy = (anchor.Y - offset.Y) / scale;
            scale = newScale;
            offset = new PointF(anchor.X - sx * scale, anchor.Y - sy * scale);
            ClampOffset();

            Invalidate();
            ZoomChanged?.Invoke(this, EventArgs.Empty);
            CropChanged?.Invoke(this, EventArgs.Empty);
        }

        private void MoveBy(float dx, float dy)
        {
            offset = new PointF(offset.X + dx, offset.Y + dy);
            ClampOffset();
            Invalidate();
            CropChanged?.Invoke(this, EventArgs.Empty);
        }

        // ---- result -----------------------------------------------------------

        /// <summary>The source region inside the frame, in source-pixel coordinates.</summary>
        public RectangleF CropRegion
        {
            get
            {
                if (source == null) return RectangleF.Empty;
                float x = (frame.Left - offset.X) / scale;
                float y = (frame.Top - offset.Y) / scale;
                float side = frame.Width / scale;
                // guard against float rounding pushing the region a hair outside the picture
                x = Math.Clamp(x, 0, Math.Max(0, source.Width - side));
                y = Math.Clamp(y, 0, Math.Max(0, source.Height - side));
                return new RectangleF(x, y, side, side);
            }
        }

        /// <summary>Renders exactly what is inside the frame as a square bitmap. The caller disposes it.</summary>
        public Bitmap? GetCrop(int outputSize, InterpolationMode quality = InterpolationMode.HighQualityBicubic)
        {
            if (source == null) return null;

            var result = new Bitmap(outputSize, outputSize);
            using var g = Graphics.FromImage(result);
            g.InterpolationMode = quality;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.SmoothingMode = SmoothingMode.HighQuality;
            using var attrs = new ImageAttributes();
            attrs.SetWrapMode(WrapMode.TileFlipXY);      // no dark halo at the picture's edge
            g.DrawImage(source, new Rectangle(0, 0, outputSize, outputSize),
                CropRegion.X, CropRegion.Y, CropRegion.Width, CropRegion.Height, GraphicsUnit.Pixel, attrs);
            return result;
        }

        // ---- painting ---------------------------------------------------------


        protected override void OnPaintBackground(PaintEventArgs e) { }   // painted at the start of OnPaint instead (buffered controls ignore this)

        protected override void OnPaint(PaintEventArgs e)
        {
            Backdrop.PaintBehind(e.Graphics, this);   // what is behind us: backdrop and any glass around us
            var g = e.Graphics;
            // Everything is clipped to a rounded rectangle so the stage matches the glass around it.
            Glass.Smooth(g);
            using var stage = Glass.RoundedRect(new RectangleF(0, 0, Width, Height), 16);
            g.SetClip(stage);
            using (var dark = new SolidBrush(Color.FromArgb(7, 8, 10))) g.FillRectangle(dark, 0, 0, Width, Height);
            if (source == null) { g.ResetClip(); return; }

            g.InterpolationMode = dragging ? InterpolationMode.Bilinear : InterpolationMode.HighQualityBilinear;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(source, new RectangleF(offset.X, offset.Y, source.Width * scale, source.Height * scale));

            // Dim everything outside the frame so the kept area is obvious.
            using (var dim = new SolidBrush(Color.FromArgb(165, 8, 9, 11)))
            {
                g.FillRectangle(dim, 0, 0, Width, frame.Top);
                g.FillRectangle(dim, 0, frame.Bottom, Width, Height - frame.Bottom);
                g.FillRectangle(dim, 0, frame.Top, frame.Left, frame.Height);
                g.FillRectangle(dim, frame.Right, frame.Top, Width - frame.Right, frame.Height);
            }

            // Rule-of-thirds guides and the frame itself.
            using (var guide = new Pen(Color.FromArgb(55, 255, 255, 255)))
            {
                for (int i = 1; i <= 2; i++)
                {
                    float x = frame.Left + frame.Width * i / 3, y = frame.Top + frame.Height * i / 3;
                    g.DrawLine(guide, x, frame.Top, x, frame.Bottom);
                    g.DrawLine(guide, frame.Left, y, frame.Right, y);
                }
            }
            using var border = new Pen(Color.FromArgb(230, 255, 255, 255));
            g.DrawRectangle(border, frame.X, frame.Y, frame.Width - 1, frame.Height - 1);

            g.ResetClip();
            using var edge = new Pen(Focused && ShowFocusCues ? Theme.AccentSoft : Color.FromArgb(60, 255, 255, 255), Focused && ShowFocusCues ? 1.5f : 1f);
            g.DrawPath(edge, stage);
        }

        // ---- interaction ------------------------------------------------------

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || source == null) return;
            Focus();
            dragging = true;
            Capture = true;
            dragStart = e.Location;
            dragStartOffset = offset;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!dragging) return;
            offset = new PointF(dragStartOffset.X + e.X - dragStart.X, dragStartOffset.Y + e.Y - dragStart.Y);
            ClampOffset();
            Invalidate();
            CropChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!dragging) return;
            dragging = false;
            Capture = false;
            Invalidate();   // repaint at full quality
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            ZoomAt(Zoom * (float)Math.Pow(1.12, e.Delta / 120.0), e.Location);
        }

        protected override bool IsInputKey(Keys keyData) =>
            keyData is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            float step = e.Shift ? 1 : 10;
            switch (e.KeyCode)
            {
                case Keys.Left: MoveBy(step, 0); e.Handled = true; break;
                case Keys.Right: MoveBy(-step, 0); e.Handled = true; break;
                case Keys.Up: MoveBy(0, step); e.Handled = true; break;
                case Keys.Down: MoveBy(0, -step); e.Handled = true; break;
                case Keys.Oemplus or Keys.Add: Zoom *= 1.1f; e.Handled = true; break;
                case Keys.OemMinus or Keys.Subtract: Zoom /= 1.1f; e.Handled = true; break;
            }
        }

        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
    }
}
