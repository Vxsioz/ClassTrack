using ClassTrack.Data;

namespace ClassTrack.UI.Controls
{
    /// <summary>
    /// A plain ranked list with thin proportional bars: label, count and share on one line, bar beneath.
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
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Margin = Padding.Empty;
            Dock = DockStyle.Fill;
            SetItems(new List<CountBy>(), emptyText);
        }

        public void SetItems(List<CountBy> data, string empty)
        {
            items = data;
            emptyText = empty;
            total = data.Sum(i => i.Count);
            int rows = data.Count == 0 ? 1 : Math.Min(data.Count, MaxRows) + (data.Count > MaxRows ? 1 : 0);
            Height = rows * RowHeight;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.Bg);

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

                int barY = y + labelHeight + 5;
                using var track = new SolidBrush(Theme.Border);
                g.FillRectangle(track, 0, barY, Width, 4);
                using var fill = new SolidBrush(unassigned ? Theme.TextFaint : Theme.Accent);
                g.FillRectangle(fill, 0, barY, Math.Max(3, (int)((long)Width * item.Count / max)), 4);
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
