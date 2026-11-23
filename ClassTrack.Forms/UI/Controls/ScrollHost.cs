namespace ClassTrack.UI.Controls
{
    /// <summary>
    /// A scrolling page body. Rows are stacked top to bottom; the content is exactly as wide as the
    /// visible area (so a scrollbar never clips it) and as tall as it needs to be. AutoSize can't do both,
    /// so it is sized here.
    /// </summary>
    public sealed class ScrollHost : ClearPanel
    {
        public TableLayoutPanel Content { get; }

        public ScrollHost()
        {
            AutoScroll = true;
            Dock = DockStyle.Fill;
            Content = new TablePanel
            {
                BackColor = Color.Transparent,
                ColumnCount = 1,
                Location = Point.Empty,
                Margin = Padding.Empty
            };
            Content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            Controls.Add(Content);
            ClientSizeChanged += (_, _) => Fit();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowScrollBar(IntPtr hwnd, int bar, bool show);

        /// <summary>
        /// Scrolling moves child controls together with the pixels they last painted, which were drawn for the old
        /// position (so their glass would not match the backdrop). Repaint everything after any scroll.
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            const int WM_HSCROLL = 0x114, WM_VSCROLL = 0x115, WM_MOUSEWHEEL = 0x20A;
            if (m.Msg is WM_VSCROLL or WM_HSCROLL or WM_MOUSEWHEEL) Invalidate(true);
        }

        /// <summary>Adds a full-width row at the bottom.</summary>
        public void AddRow(Control row)
        {
            row.Dock = DockStyle.Fill;
            Content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Content.RowCount = Content.RowStyles.Count;
            Content.Controls.Add(row, 0, Content.RowCount - 1);
        }

        /// <summary>Recomputes the content size; call after the content's height may have changed.</summary>
        public void Fit()
        {
            int w = ClientSize.Width;
            if (w <= 0) return;
            Content.Size = new Size(w, Content.GetPreferredSize(new Size(w, 0)).Height);
            if (IsHandleCreated) ShowScrollBar(Handle, 0, false);   // SB_HORZ: the content always fits the width, so never show it
        }
    }
}
