namespace ClassTrack.UI.Controls
{
    /// <summary>
    /// A calm list of people: avatar, name, "ID · course", and the year on the right.
    /// Rows light up under the pointer; click (or Enter) opens the student.
    /// </summary>
    public sealed class RecentList : Control
    {
        public sealed record Row(int Id, string Name, string Detail, string Year, Bitmap Avatar);

        private const int AvatarSize = 40;
        private static int RowHeight => AvatarSize + 20;

        private List<Row> rows = new();
        private string emptyText = "No students yet.";
        private int hoverIndex = -1;
        private int focusIndex = -1;
        private readonly Tween hoverFade;

        public event EventHandler<int>? RowActivated;

        public RecentList()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
            BackColor = Color.Transparent;
            TabStop = true;
            Margin = Padding.Empty;
            Dock = DockStyle.Fill;
            Cursor = Cursors.Default;
            hoverFade = new Tween(Invalidate);
        }

        public void SetRows(List<Row> data, string empty)
        {
            rows = data;
            emptyText = empty;
            hoverIndex = focusIndex = -1;
            Height = Math.Max(1, data.Count) * RowHeight;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int i = e.Y / RowHeight;
            if (i < 0 || i >= rows.Count) i = -1;
            if (i != hoverIndex)
            {
                hoverIndex = i;
                Cursor = i >= 0 ? Cursors.Hand : Cursors.Default;
                hoverFade.Set(0);
                hoverFade.To(1, 120);
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hoverIndex = -1;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            Focus();
            int i = e.Y / RowHeight;
            if (e.Button == MouseButtons.Left && i >= 0 && i < rows.Count) RowActivated?.Invoke(this, rows[i].Id);
            base.OnMouseClick(e);
        }

        protected override bool IsInputKey(Keys keyData) => keyData is Keys.Up or Keys.Down or Keys.Enter || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (rows.Count == 0) return;
            switch (e.KeyCode)
            {
                case Keys.Down: focusIndex = Math.Min(rows.Count - 1, focusIndex + 1); e.Handled = true; Invalidate(); break;
                case Keys.Up: focusIndex = Math.Max(0, focusIndex - 1); e.Handled = true; Invalidate(); break;
                case Keys.Enter when focusIndex >= 0: RowActivated?.Invoke(this, rows[focusIndex].Id); e.Handled = true; break;
            }
            base.OnKeyDown(e);
        }

        protected override void OnGotFocus(EventArgs e) { if (focusIndex < 0 && rows.Count > 0) focusIndex = 0; Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }


        protected override void OnPaintBackground(PaintEventArgs e) { }   // painted at the start of OnPaint instead (buffered controls ignore this)

        protected override void OnPaint(PaintEventArgs e)
        {
            Backdrop.PaintBehind(e.Graphics, this);   // what is behind us: backdrop and any glass around us
            var g = e.Graphics;
            Glass.Smooth(g);

            if (rows.Count == 0)
            {
                TextRenderer.DrawText(g, emptyText, Theme.Body, new Rectangle(0, 0, Width, RowHeight), Theme.TextMuted,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                return;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var area = new Rectangle(0, i * RowHeight, Width, RowHeight);
                bool hot = i == hoverIndex, focus = Focused && ShowFocusCues && i == focusIndex;

                if (hot || focus)
                {
                    using var path = Glass.RoundedRect(new RectangleF(area.X, area.Y + 2, area.Width - 1, area.Height - 4), 14);
                    using var fill = new SolidBrush(Color.FromArgb((int)(hot ? 26 * hoverFade.Value + 8 : 14), 255, 255, 255));
                    g.FillPath(fill, path);
                    if (focus)
                    {
                        using var ring = new Pen(Color.FromArgb(150, Theme.AccentSoft), 1.5f);
                        g.DrawPath(ring, path);
                    }
                }

                g.DrawImage(row.Avatar, new Rectangle(area.X + 12, area.Y + (area.Height - AvatarSize) / 2, AvatarSize, AvatarSize));

                int textX = area.X + 12 + AvatarSize + 14;
                int yearWidth = TextRenderer.MeasureText(row.Year, Theme.Small).Width + 28;
                TextRenderer.DrawText(g, row.Name, Theme.H3, new Rectangle(textX, area.Y + 11, Width - textX - yearWidth - 16, 24), Theme.Text,
                    TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, row.Detail, Theme.Small, new Rectangle(textX, area.Y + 33, Width - textX - yearWidth - 16, 20), Theme.TextMuted,
                    TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                // Year pill
                var pill = new RectangleF(Width - yearWidth - 12, area.Y + (area.Height - 26) / 2f, yearWidth, 26);
                using (var pillPath = Glass.RoundedRect(pill, 13))
                {
                    using var pillFill = new SolidBrush(Color.FromArgb(row.Year == "—" ? 10 : 28, row.Year == "—" ? Color.White : Theme.Accent));
                    g.FillPath(pillFill, pillPath);
                    using var pillEdge = new Pen(Color.FromArgb(row.Year == "—" ? 24 : 70, row.Year == "—" ? Color.White : Theme.AccentSoft));
                    g.DrawPath(pillEdge, pillPath);
                }
                TextRenderer.DrawText(g, row.Year, Theme.Small, Rectangle.Round(pill), row.Year == "—" ? Theme.TextFaint : Theme.AccentSoft,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

                if (i < rows.Count - 1 && !hot && i + 1 != hoverIndex)
                {
                    using var line = new Pen(Color.FromArgb(18, 255, 255, 255));
                    g.DrawLine(line, area.X + 14 + AvatarSize + 12, area.Bottom - 1, area.Right - 12, area.Bottom - 1);
                }
            }
        }
    }
}
