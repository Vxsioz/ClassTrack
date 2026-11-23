using System.Drawing.Drawing2D;

namespace ClassTrack.UI
{
    public enum IconKind { Dashboard, Students, AddUser, Settings, Search, ChevronLeft, ChevronRight, Plus, Folder }

    /// <summary>Minimal line icons drawn as vector paths (no image files, crisp at any DPI).</summary>
    public static class Icons
    {
        public static void Draw(Graphics g, IconKind kind, RectangleF box, Color color, float stroke = 1.6f)
        {
            var state = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(color, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };

            float s = Math.Min(box.Width, box.Height);
            float x = box.X + (box.Width - s) / 2, y = box.Y + (box.Height - s) / 2;
            PointF P(float u, float v) => new(x + u * s, y + v * s);
            RectangleF R(float u, float v, float w, float h) => new(x + u * s, y + v * s, w * s, h * s);

            switch (kind)
            {
                case IconKind.Dashboard:
                    foreach (var (u, v) in new[] { (0.12f, 0.12f), (0.56f, 0.12f), (0.12f, 0.56f), (0.56f, 0.56f) })
                    {
                        using var path = Glass.RoundedRect(R(u, v, 0.32f, 0.32f), s * 0.08f);
                        g.DrawPath(pen, path);
                    }
                    break;

                case IconKind.Students:
                    g.DrawEllipse(pen, R(0.33f, 0.12f, 0.34f, 0.34f));
                    g.DrawArc(pen, R(0.16f, 0.56f, 0.68f, 0.68f), 205, 130);
                    break;

                case IconKind.AddUser:
                    g.DrawEllipse(pen, R(0.14f, 0.14f, 0.32f, 0.32f));
                    g.DrawArc(pen, R(0.04f, 0.54f, 0.52f, 0.56f), 200, 140);
                    g.DrawLine(pen, P(0.74f, 0.40f), P(0.74f, 0.76f));
                    g.DrawLine(pen, P(0.56f, 0.58f), P(0.92f, 0.58f));
                    break;

                case IconKind.Settings:
                    foreach (var (v, knob) in new[] { (0.24f, 0.66f), (0.50f, 0.32f), (0.76f, 0.58f) })
                    {
                        g.DrawLine(pen, P(0.10f, v), P(0.90f, v));
                        using var back = new SolidBrush(Color.FromArgb(255, 18, 19, 24));
                        var dot = R(knob - 0.09f, v - 0.09f, 0.18f, 0.18f);
                        g.FillEllipse(back, dot);
                        g.DrawEllipse(pen, dot);
                    }
                    break;

                case IconKind.Search:
                    g.DrawEllipse(pen, R(0.14f, 0.14f, 0.52f, 0.52f));
                    g.DrawLine(pen, P(0.58f, 0.58f), P(0.86f, 0.86f));
                    break;

                case IconKind.ChevronLeft:
                    g.DrawLines(pen, new[] { P(0.62f, 0.18f), P(0.32f, 0.5f), P(0.62f, 0.82f) });
                    break;

                case IconKind.ChevronRight:
                    g.DrawLines(pen, new[] { P(0.38f, 0.18f), P(0.68f, 0.5f), P(0.38f, 0.82f) });
                    break;

                case IconKind.Plus:
                    g.DrawLine(pen, P(0.5f, 0.18f), P(0.5f, 0.82f));
                    g.DrawLine(pen, P(0.18f, 0.5f), P(0.82f, 0.5f));
                    break;

                case IconKind.Folder:
                    using (var path = new GraphicsPath())
                    {
                        path.AddLines(new[] { P(0.10f, 0.28f), P(0.10f, 0.78f), P(0.90f, 0.78f), P(0.90f, 0.36f), P(0.48f, 0.36f), P(0.40f, 0.24f), P(0.18f, 0.24f) });
                        path.CloseFigure();
                        g.DrawPath(pen, path);
                    }
                    break;
            }
            g.Restore(state);
        }
    }
}
