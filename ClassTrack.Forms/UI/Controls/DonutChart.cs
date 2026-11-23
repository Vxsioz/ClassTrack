using System.Drawing.Drawing2D;
using ClassTrack.Data;

namespace ClassTrack.UI.Controls
{
    /// <summary>A ring chart with the total in the middle and a legend beside it (label, count, share).</summary>
    public sealed class DonutChart : Control
    {
        private List<CountBy> items = new();
        private int total;
        private string emptyText = "No data yet.";
        private readonly Tween reveal;

        public DonutChart()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Margin = Padding.Empty;
            Dock = DockStyle.Fill;
            Height = 176;
            reveal = new Tween(Invalidate, 1f);
        }

        public void SetItems(List<CountBy> data, string empty)
        {
            items = data;
            emptyText = empty;
            total = data.Sum(i => i.Count);
            reveal.Set(0);
            reveal.To(1, 520);   // the ring sweeps in
            Invalidate();
        }

        private static Color ColorFor(CountBy item, int index) =>
            item.Label == "Unassigned" ? Theme.TextFaint : Theme.ChartColors[index % (Theme.ChartColors.Length - 1)];


        protected override void OnPaintBackground(PaintEventArgs e) { }   // painted at the start of OnPaint instead (buffered controls ignore this)

        protected override void OnPaint(PaintEventArgs e)
        {
            Backdrop.PaintBehind(e.Graphics, this);   // what is behind us: backdrop and any glass around us
            var g = e.Graphics;
            Glass.Smooth(g);

            if (items.Count == 0 || total == 0)
            {
                TextRenderer.DrawText(g, emptyText, Theme.Body, ClientRectangle, Theme.TextMuted,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                return;
            }

            int size = Math.Min(Math.Min(Height, 170), (int)(Width * 0.44));   // shrink with the panel so the legend keeps room
            var ring = new RectangleF(8, (Height - size) / 2f + 8, size - 16, size - 16);
            float thickness = 15;

            // Faint track, then one arc per slice with a small gap between them.
            using (var track = new Pen(Color.FromArgb(22, 255, 255, 255), thickness))
                g.DrawEllipse(track, ring);

            float sweepMax = 360f * Anim.EaseOut(Math.Min(1f, reveal.Value));
            float start = -90f, used = 0f;
            for (int i = 0; i < items.Count; i++)
            {
                float sweep = 360f * items[i].Count / total;
                float gap = items.Count > 1 ? 3f : 0f;
                float visible = Math.Min(sweep, Math.Max(0, sweepMax - used));
                if (visible > gap + 0.2f)
                {
                    using var pen = new Pen(ColorFor(items[i], i), thickness) { StartCap = LineCap.Flat, EndCap = LineCap.Flat };
                    g.DrawArc(pen, ring, start + gap / 2, visible - gap);
                }
                start += sweep;
                used += sweep;
            }

            // Centre: total
            var center = new Rectangle((int)ring.X, (int)ring.Y, (int)ring.Width, (int)ring.Height);
            TextRenderer.DrawText(g, total.ToString(), Theme.Stat, new Rectangle(center.X, center.Y + center.Height / 2 - 40, center.Width, 42),
                Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, total == 1 ? "student" : "students", Theme.Small,
                new Rectangle(center.X, center.Y + center.Height / 2 + 4, center.Width, 20),
                Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);

            // Legend
            int x = size + 24, rowHeight = Math.Min(30, (Height - 8) / Math.Max(1, Math.Min(items.Count, 6)));
            int y = (Height - rowHeight * Math.Min(items.Count, 6)) / 2;
            for (int i = 0; i < items.Count && i < 6; i++, y += rowHeight)
            {
                using (var dot = new SolidBrush(ColorFor(items[i], i)))
                    g.FillEllipse(dot, x, y + (rowHeight - 9) / 2f, 9, 9);

                string share = $"{items[i].Count}  ·  {Math.Round(100.0 * items[i].Count / total)}%";
                int shareWidth = TextRenderer.MeasureText(share, Theme.Small).Width + 4;
                TextRenderer.DrawText(g, items[i].Label, Theme.Body, new Rectangle(x + 18, y, Width - x - 18 - shareWidth - 8, rowHeight),
                    items[i].Label == "Unassigned" ? Theme.TextMuted : Theme.Text,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, share, Theme.Small, new Rectangle(Width - shareWidth, y, shareWidth, rowHeight),
                    Theme.TextMuted, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
            }
        }
    }
}
