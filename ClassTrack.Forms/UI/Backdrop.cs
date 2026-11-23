using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

using ClassTrack.UI.Controls;

namespace ClassTrack.UI
{
    /// <summary>
    /// The soft, blurred ambient scene the glass sits on: a deep charcoal gradient with a few large,
    /// faint pools of cool light. It is rendered small and scaled up (a cheap, smooth blur) and cached
    /// per window size. Glass controls paint the slice of it that lies behind them, so panels really do
    /// look translucent, and it costs one bitmap draw per control instead of a live blur.
    /// </summary>
    public static class Backdrop
    {
        public static Bitmap Render(int width, int height)
        {
            width = Math.Max(width, 1);
            height = Math.Max(height, 1);
            int lw = Math.Max(width / 4, 16), lh = Math.Max(height / 4, 16);

            using var low = new Bitmap(lw, lh);
            using (var g = Graphics.FromImage(low))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var baseBrush = new LinearGradientBrush(new Rectangle(0, 0, lw, lh),
                           Theme.BackdropTop, Theme.BackdropBottom, LinearGradientMode.Vertical))
                    g.FillRectangle(baseBrush, 0, 0, lw, lh);

                // Pools of light: cool blue top-left, violet bottom-right, a faint warm touch top-right.
                Pool(g, lw * 0.14f, lh * 0.06f, lw * 0.62f, lh * 0.70f, Color.FromArgb(78, 30, 84, 150));
                Pool(g, lw * 0.92f, lh * 0.98f, lw * 0.58f, lh * 0.66f, Color.FromArgb(66, 66, 54, 138));
                Pool(g, lw * 0.80f, lh * 0.04f, lw * 0.34f, lh * 0.40f, Color.FromArgb(28, 110, 80, 70));
                Pool(g, lw * 0.30f, lh * 1.02f, lw * 0.40f, lh * 0.40f, Color.FromArgb(30, 30, 110, 120));
            }

            var full = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            using var fg = Graphics.FromImage(full);
            fg.InterpolationMode = InterpolationMode.HighQualityBicubic;
            fg.PixelOffsetMode = PixelOffsetMode.Half;
            fg.DrawImage(low, new Rectangle(0, 0, width, height), 0, 0, lw, lh, GraphicsUnit.Pixel);
            return full;
        }

        private static void Pool(Graphics g, float cx, float cy, float rx, float ry, Color color)
        {
            using var path = new GraphicsPath();
            path.AddEllipse(cx - rx, cy - ry, rx * 2, ry * 2);
            using var brush = new PathGradientBrush(path)
            {
                CenterColor = color,
                SurroundColors = new[] { Color.FromArgb(0, color) }
            };
            brush.SetSigmaBellShape(0.55f);   // soft falloff, no visible rim
            g.FillPath(brush, path);
        }

        /// <summary>
        /// Paints, into a control's own coordinates, exactly what is behind it: the window backdrop slice at its
        /// screen position, then every glass panel it sits inside (outermost first). Controls call this instead of
        /// using WinForms transparency, which repaints sibling controls at wrong offsets in nested layouts.
        /// </summary>
        public static void PaintBehind(Graphics g, Control c)
        {
            if (c.TopLevelControl is not DarkForm form || !form.IsHandleCreated || !c.IsHandleCreated)
            {
                g.Clear(Theme.Bg);
                return;
            }

            Point mine = c.PointToScreen(Point.Empty);
            Point origin = form.PointToClient(mine);
            using (var attrs = new ImageAttributes())
            {
                attrs.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(form.GetBackdrop(), new Rectangle(0, 0, c.Width, c.Height),
                    origin.X, origin.Y, c.Width, c.Height, GraphicsUnit.Pixel, attrs);
            }

            var glass = new Stack<GlassPanel>();
            for (var parent = c.Parent; parent != null && parent != form; parent = parent.Parent)
                if (parent is GlassPanel panel) glass.Push(panel);

            while (glass.Count > 0)
            {
                var panel = glass.Pop();
                Point at = panel.PointToScreen(Point.Empty);
                var state = g.Save();
                g.TranslateTransform(at.X - mine.X, at.Y - mine.Y);
                Glass.Paint(g, panel.ClientRectangle, panel.Radius, 0f, panel.Solid);
                g.Restore(state);
            }
        }
    }
}