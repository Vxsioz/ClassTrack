using System.Drawing.Drawing2D;

namespace ClassTrack.UI
{
    /// <summary>Shared drawing for glass surfaces, so every control gets the same edge, sheen and softness.</summary>
    public static class Glass
    {
        public static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            if (d <= 0.5f) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void Smooth(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.Half;
        }

        /// <summary>
        /// A translucent glass surface: faint white fill, a soft sheen across the top, a bright hairline
        /// along the top edge and a gradient border that is brighter at the top-left (light from above).
        /// </summary>
        /// <param name="boost">0..1 extra brightness, e.g. while hovered.</param>
        /// <param name="solid">Use the denser table surface instead of translucent glass.</param>
        public static void Paint(Graphics g, Rectangle bounds, int radius, float boost = 0f, bool solid = false)
        {
            Smooth(g);
            var rect = new RectangleF(bounds.X + 0.5f, bounds.Y + 0.5f, bounds.Width - 1, bounds.Height - 1);
            if (rect.Width < 2 || rect.Height < 2) return;
            using var path = RoundedRect(rect, radius);

            if (solid)
            {
                using var fill = new SolidBrush(Theme.TableSurface);
                g.FillPath(fill, path);
            }
            else
            {
                int a = (int)(Theme.GlassAlpha + (Theme.GlassAlphaHover - Theme.GlassAlpha) * boost);
                using var fill = new SolidBrush(Color.FromArgb(a, Theme.GlassTint));
                g.FillPath(fill, path);
            }

            // Sheen: light falling from above, fading out by mid-height.
            var sheen = new RectangleF(rect.X, rect.Y, rect.Width, Math.Max(2, rect.Height * 0.55f));
            using (var brush = new LinearGradientBrush(sheen, Color.FromArgb(solid ? 14 : 30, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Vertical))
            {
                var state = g.Save();
                g.SetClip(path, CombineMode.Intersect);
                g.FillRectangle(brush, sheen.X, sheen.Y, sheen.Width, sheen.Height - 1.5f);   // -1.5: no wrapped last row (a visible seam)
                g.Restore(state);
            }

            // Edge: gradient hairline, bright at the top-left, faint at the bottom-right.
            using (var edge = new LinearGradientBrush(rect, Color.FromArgb(boost > 0 ? 100 : 78, 255, 255, 255), Color.FromArgb(18, 255, 255, 255), LinearGradientMode.ForwardDiagonal))
            using (var pen = new Pen(edge, 1f))
                g.DrawPath(pen, path);

            // Specular line just inside the top edge.
            if (rect.Width > radius * 2 + 4)
            {
                using var spec = new Pen(Color.FromArgb(solid ? 18 : 40, 255, 255, 255), 1f);
                g.DrawLine(spec, rect.X + radius, rect.Y + 1.5f, rect.Right - radius, rect.Y + 1.5f);
            }
        }

        /// <summary>An input field: slightly denser than glass so text stays crisp, accent edge on focus.</summary>
        public static void PaintField(Graphics g, Rectangle bounds, int radius, bool focused, bool invalid, bool hover)
        {
            Smooth(g);
            var rect = new RectangleF(bounds.X + 0.5f, bounds.Y + 0.5f, bounds.Width - 1, bounds.Height - 1);
            using var path = RoundedRect(rect, radius);
            using (var fill = new SolidBrush(Theme.Input))
                g.FillPath(fill, path);

            if (focused || invalid)
            {
                Color ring = invalid ? Theme.Danger : Theme.Accent;
                var outer = RectangleF.Inflate(rect, 1.5f, 1.5f);
                using var glow = RoundedRect(outer, radius + 1.5f);
                using var glowPen = new Pen(Color.FromArgb(55, ring), 3f);
                var state = g.Save();
                g.ResetClip();
                g.DrawPath(glowPen, glow);
                g.Restore(state);
                using var pen = new Pen(ring, 1f);
                g.DrawPath(pen, path);
            }
            else
            {
                using var pen = new Pen(Color.FromArgb(hover ? 64 : 42, 255, 255, 255), 1f);
                g.DrawPath(pen, path);
            }
        }
    }
}
