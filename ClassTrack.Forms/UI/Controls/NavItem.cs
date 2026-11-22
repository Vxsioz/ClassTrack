namespace ClassTrack.UI.Controls
{
    /// <summary>A sidebar entry: muted text, subtle hover, accent bar when active. Keyboard accessible.</summary>
    public sealed class NavItem : Control
    {
        private bool hover;
        private bool active;

        public NavItem(string text)
        {
            Text = text;
            Font = Theme.Body;
            Cursor = Cursors.Hand;
            TabStop = true;
            Margin = Padding.Empty;
            Size = new Size(100, Font.Height + 22);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable | ControlStyles.StandardClick, true);
        }

        [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool Active
        {
            get => active;
            set { active = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Enter or Keys.Space)
            {
                e.Handled = true;
                OnClick(EventArgs.Empty);
            }
            base.OnKeyDown(e);
        }

        protected override bool IsInputKey(Keys keyData) => keyData is Keys.Enter or Keys.Space || base.IsInputKey(keyData);

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.Sidebar);

            var pill = new Rectangle(10, 2, Width - 20, Height - 4);
            if (active || hover)
            {
                using var fill = new SolidBrush(active ? Theme.Surface : Theme.SidebarHover);
                g.FillRectangle(fill, pill);
            }
            if (active)
            {
                using var bar = new SolidBrush(Theme.Accent);
                g.FillRectangle(bar, pill.Left, pill.Top + 7, 3, pill.Height - 14);
            }
            if (Focused && ShowFocusCues)
            {
                using var pen = new Pen(Theme.BorderStrong);
                g.DrawRectangle(pen, pill.Left, pill.Top, pill.Width - 1, pill.Height - 1);
            }

            TextRenderer.DrawText(g, Text, Font, new Rectangle(pill.Left + 16, 0, pill.Width - 20, Height),
                active || hover ? Theme.Text : Theme.TextMuted,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
}
