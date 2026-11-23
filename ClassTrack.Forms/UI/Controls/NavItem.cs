namespace ClassTrack.UI.Controls
{
    /// <summary>A sidebar entry: icon + label on glass. Hover brightens it, the active item gets a lit pill and an accent mark.</summary>
    public sealed class NavItem : Control
    {
        private readonly IconKind icon;
        private readonly Tween hover;
        private readonly Tween active;
        private bool isActive;

        public NavItem(string text, IconKind icon)
        {
            Text = text;
            this.icon = icon;
            Font = Theme.Body;
            Cursor = Cursors.Hand;
            TabStop = true;
            Margin = Padding.Empty;
            Size = new Size(100, Font.Height + 24);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable | ControlStyles.StandardClick | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;   // only valid after SupportsTransparentBackColor is set
            hover = new Tween(Invalidate);
            active = new Tween(Invalidate);
        }

        [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool Active
        {
            get => isActive;
            set { isActive = value; active.To(value ? 1 : 0, 180); }
        }

        protected override void OnMouseEnter(EventArgs e) { hover.To(1); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover.To(0); base.OnMouseLeave(e); }
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


        protected override void OnPaintBackground(PaintEventArgs e) { }   // painted at the start of OnPaint instead (buffered controls ignore this)

        protected override void OnPaint(PaintEventArgs e)
        {
            Backdrop.PaintBehind(e.Graphics, this);   // what is behind us: backdrop and any glass around us
            var g = e.Graphics;
            Glass.Smooth(g);
            float h = hover.Value, a = active.Value;

            var pill = new RectangleF(0.5f, 2.5f, Width - 1, Height - 5);
            using (var path = Glass.RoundedRect(pill, 12))
            {
                int fill = (int)(14 * h + 30 * a);
                if (fill > 0)
                {
                    using var brush = new SolidBrush(Color.FromArgb(fill, 255, 255, 255));
                    g.FillPath(brush, path);
                }
                if (a > 0.01f)
                {
                    using var edge = new Pen(Color.FromArgb((int)(64 * a), 255, 255, 255));
                    g.DrawPath(edge, path);
                    // Light spilling from the top of the lit pill
                    var sheen = new RectangleF(pill.X, pill.Y, pill.Width, pill.Height * 0.55f);
                    using var sheenBrush = new System.Drawing.Drawing2D.LinearGradientBrush(sheen,
                        Color.FromArgb((int)(34 * a), 255, 255, 255), Color.FromArgb(0, 255, 255, 255), System.Drawing.Drawing2D.LinearGradientMode.Vertical);
                    var state = g.Save();
                    g.SetClip(path, System.Drawing.Drawing2D.CombineMode.Intersect);
                    g.FillRectangle(sheenBrush, sheen.X, sheen.Y, sheen.Width, sheen.Height - 1.5f);
                    g.Restore(state);
                }
            }
            if (Focused && ShowFocusCues)
            {
                using var ring = Glass.RoundedRect(pill, 12);
                using var pen = new Pen(Color.FromArgb(150, Theme.AccentSoft), 1.5f);
                g.DrawPath(pen, ring);
            }

            Color ink = Mix(Theme.TextMuted, Theme.Text, Math.Max(h * 0.8f, a));
            Color iconInk = Mix(ink, Theme.AccentSoft, a);
            Icons.Draw(g, icon, new RectangleF(18, (Height - 18) / 2f, 18, 18), iconInk, 1.6f);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(48, 0, Width - 56, Height), ink,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            if (a > 0.01f)
            {
                using var mark = new SolidBrush(Color.FromArgb((int)(255 * a), Theme.Accent));
                g.FillEllipse(mark, Width - 20, Height / 2f - 2.5f, 5, 5);
            }
        }

        private static Color Mix(Color a, Color b, float t) => Color.FromArgb(
            (int)(a.R * (1 - t) + b.R * t), (int)(a.G * (1 - t) + b.G * t), (int)(a.B * (1 - t) + b.B * t));
    }
}
