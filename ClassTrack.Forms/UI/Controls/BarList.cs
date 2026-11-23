using System.Drawing.Drawing2D;
using ClassTrack.Data;

namespace ClassTrack.UI.Controls
{
    /// <summary>
    /// A ranked list with soft glass tracks and gradient bars: label, count and share on one line, bar beneath.
    /// Shows the top rows and folds the rest into a "+ N more" line. Sizes itself to its content.
    /// </summary>
    public sealed class BarList : Control
    {
        private const int MaxRows = 6;
        private List<CountBy> items = new();
        private int total;
        private string emptyText = "No data yet.";

        private static int RowHeight => Theme.Body.Height + 20;

        public BarList()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Margin = Padding.Empty;
            Dock = DockStyle.Fill;
            SetItems(new List<CountBy>(), emptyText);
        }

        public void SetItems(List<CountBy> data, string empty)
        {
            items = data;
            emptyText = empty;
            total = data.Sum(i => i.Count);
            // The charts are drawn, so describe them for screen readers (and UI tests): "BSCS 2, Unassigned 1".
            AccessibleRole = AccessibleRole.Chart;
            AccessibleDescription = data.Count == 0 ? empty : string.Join(", ", data.Select(i => $"{i.Label} {i.Count}"));
            int rows = data.Count == 0 ? 1 : Math.Min(data.Count, MaxRows) + (data.Count > MaxRows ? 1 : 0);
            Height = rows * RowHeight;
            Invalidate();
        }


        protected override void OnPaintBackground(PaintEventArgs e) { }   // painted at the start of OnPaint instead (buffered controls ignore this)

        protected override void OnPaint(PaintEventArgs e)
        {
            Backdrop.PaintBehind(e.Graphics, this);   // what is behind us: backdrop and any glass around us
            var g = e.Graphics;
            Glass.Smooth(g);

            if (items.Count == 0)
            {
                TextRenderer.DrawText(g, emptyText, Theme.Body, new Rectangle(0, 0, Width, RowHeight),
                    Theme.TextMuted, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                return;
            }

            int max = items.Max(i => i.Count);
            int labelHeight = Theme.Body.Height + 2;
            int shown = Math.Min(items.Count, MaxRows);

            for (int i = 0; i < shown; i++)
            {
                var item = items[i];
                int y = i * RowHeight;
                bool unassigned = item.Label == "Unassigned";

                string share = $"{item.Count}  ·  {Math.Round(100.0 * item.Count / total)}%";
                int shareWidth = TextRenderer.MeasureText(share, Theme.Small).Width + 4;

                TextRenderer.DrawText(g, item.Label, Theme.Body, new Rectangle(0, y, Width - shareWidth - 8, labelHeight),
                    unassigned ? Theme.TextMuted : Theme.Text,
                    TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
                TextRenderer.DrawText(g, share, Theme.Small, new Rectangle(Width - shareWidth, y, shareWidth, labelHeight),
                    Theme.TextMuted, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);

                var track = new RectangleF(0, y + labelHeight + 5, Width, 6);
                using (var trackPath = Glass.RoundedRect(track, 3))
                using (var trackBrush = new SolidBrush(Color.FromArgb(22, 255, 255, 255)))
                    g.FillPath(trackBrush, trackPath);

                float fillWidth = Math.Max(6, Width * (float)item.Count / max);
                var bar = new RectangleF(0, track.Y, fillWidth, track.Height);
                using var barPath = Glass.RoundedRect(bar, 3);
                Color from = unassigned ? Theme.TextFaint : Theme.Accent;
                Color to = unassigned ? Theme.TextMuted : Theme.Cyan;
                using var barBrush = new LinearGradientBrush(new RectangleF(0, bar.Y, Math.Max(fillWidth, 8), bar.Height), from, to, LinearGradientMode.Horizontal);
                g.FillPath(barBrush, barPath);
            }

            if (items.Count > MaxRows)
            {
                int others = items.Skip(MaxRows).Sum(i => i.Count);
                TextRenderer.DrawText(g, $"+ {items.Count - MaxRows} more  ·  {others} {(others == 1 ? "student" : "students")}",
                    Theme.Small, new Rectangle(0, shown * RowHeight, Width, RowHeight),
                    Theme.TextMuted, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }
        }
    }
}
