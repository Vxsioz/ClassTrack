using System.Drawing.Drawing2D;

namespace ClassTrack.UI.Controls
{
    /// <summary>The ClassTrack mark (a glossy rounded tile with an open ring) and wordmark.</summary>
    public sealed class Logo : Control
    {
        public Logo()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Margin = Padding.Empty;
            Height = 52;
        }


        protected override void OnPaintBackground(PaintEventArgs e) { }   // painted at the start of OnPaint instead (buffered controls ignore this)

        protected override void OnPaint(PaintEventArgs e)
        {
            Backdrop.PaintBehind(e.Graphics, this);   // what is behind us: backdrop and any glass around us
            var g = e.Graphics;
            Glass.Smooth(g);

            var tile = new RectangleF(2, (Height - 36) / 2f, 36, 36);
            using (var path = Glass.RoundedRect(tile, 11))
            {
                using var fill = new LinearGradientBrush(tile, Theme.AccentHover, Color.FromArgb(48, 190, 235), LinearGradientMode.ForwardDiagonal);
                g.FillPath(fill, path);
                var sheen = new RectangleF(tile.X, tile.Y, tile.Width, tile.Height * 0.55f);
                using var sheenBrush = new LinearGradientBrush(sheen, Color.FromArgb(90, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Vertical);
                var state = g.Save();
                g.SetClip(path, CombineMode.Intersect);
                g.FillRectangle(sheenBrush, sheen.X, sheen.Y, sheen.Width, sheen.Height - 1.5f);
                g.Restore(state);
                using var edge = new Pen(Color.FromArgb(110, 255, 255, 255));
                g.DrawPath(edge, path);
            }

            // An open ring with a dot: a "C" that doubles as a progress mark.
            var ring = new RectangleF(tile.X + 9.5f, tile.Y + 9.5f, 17, 17);
            using (var pen = new Pen(Color.White, 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawArc(pen, ring, 45, 270);
            using (var dot = new SolidBrush(Color.White))
                g.FillEllipse(dot, tile.X + 18 - 2f, tile.Y + 18 - 2f, 4f, 4f);

            TextRenderer.DrawText(g, "ClassTrack", Theme.H2, new Rectangle(50, 6, Width - 52, 24), Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, "Student management", Theme.Small, new Rectangle(50, 28, Width - 52, 18), Theme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
        }
    }
}
