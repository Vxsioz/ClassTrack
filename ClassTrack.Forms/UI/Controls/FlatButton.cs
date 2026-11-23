using System.Drawing.Drawing2D;

namespace ClassTrack.UI.Controls
{
    public enum ButtonKind { Primary, Secondary, Ghost, Danger }

    /// <summary>
    /// The app's one button style: rounded glass, brighter on hover, a gentle press, a focus ring.
    /// Still a real Button underneath, so Enter/Esc (AcceptButton/CancelButton) and DialogResult work.
    /// </summary>
    public class FlatButton : Button
    {
        private ButtonKind kind;
        private readonly Tween hover;
        private readonly Tween press;
        private IconKind? icon;

        public FlatButton(string text, ButtonKind kind = ButtonKind.Secondary)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            Text = text;
            this.kind = kind;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Theme.Bg;   // never shown: OnPaintBackground paints what is behind the button
            Font = Theme.Body;
            Cursor = Cursors.Hand;
            Margin = Padding.Empty;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            hover = new Tween(Invalidate);
            press = new Tween(Invalidate);
        }

        [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public ButtonKind Kind { get => kind; set { kind = value; Invalidate(); } }

        [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public IconKind? Icon { get => icon; set { icon = value; PerformLayout(); Invalidate(); } }

        public override Size GetPreferredSize(Size proposedSize)
        {
            int w = TextRenderer.MeasureText(Text, Font).Width + 36 + (icon != null ? 22 : 0);
            return new Size(Math.Max(w, 40), Math.Max(Font.Height + 16, 36));
        }

        protected override void OnMouseEnter(EventArgs e) { hover.To(1); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover.To(0); press.To(0, 90); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { press.To(1, 70); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { press.To(0, 140); base.OnMouseUp(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }


        protected override void OnPaintBackground(PaintEventArgs e) { }   // painted at the start of OnPaint instead (buffered controls ignore this)

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Backdrop.PaintBehind(g, this);   // what is behind us: backdrop and any glass around us
            Glass.Smooth(g);
            float h = hover.Value, p = press.Value;
            float dim = Enabled ? 1f : 0.4f;

            // Pressing shrinks the button by a pixel or two: a physical "give".
            var rect = new RectangleF(1.5f + p * 1.2f, 1.5f + p * 1.2f, Width - 3 - p * 2.4f, Height - 3 - p * 2.4f);
            using var path = Glass.RoundedRect(rect, 11);

            Color text;
            switch (kind)
            {
                case ButtonKind.Primary:
                {
                    using var fill = new LinearGradientBrush(rect, Fade(Blend(Theme.Accent, Theme.AccentHover, h), dim),
                        Fade(Blend(Theme.AccentPressed, Theme.Accent, h * 0.5f), dim), LinearGradientMode.Vertical);
                    g.FillPath(fill, path);
                    Sheen(g, path, rect, 46);
                    using var edge = new Pen(Color.FromArgb((int)(70 * dim), 255, 255, 255));
                    g.DrawPath(edge, path);
                    text = Fade(Color.White, dim);
                    break;
                }
                case ButtonKind.Danger:
                {
                    using var fill = new SolidBrush(Color.FromArgb((int)((24 + 26 * h + 20 * p) * dim), Theme.Danger));
                    g.FillPath(fill, path);
                    using var edge = new Pen(Color.FromArgb((int)((90 + 60 * h) * dim), Theme.Danger));
                    g.DrawPath(edge, path);
                    text = Fade(Theme.Danger, dim);
                    break;
                }
                case ButtonKind.Ghost:
                {
                    using var fill = new SolidBrush(Color.FromArgb((int)((26 * h + 12 * p) * dim), 255, 255, 255));
                    g.FillPath(fill, path);
                    using var edge = new Pen(Color.FromArgb((int)((34 + 26 * h) * dim), 255, 255, 255));
                    g.DrawPath(edge, path);
                    text = Fade(Blend(Theme.TextMuted, Theme.Text, h), dim);
                    break;
                }
                default: // Secondary: glass
                {
                    using var fill = new SolidBrush(Color.FromArgb((int)((26 + 22 * h + 14 * p) * dim), 255, 255, 255));
                    g.FillPath(fill, path);
                    Sheen(g, path, rect, 22);
                    using var edge = new Pen(Color.FromArgb((int)((52 + 36 * h) * dim), 255, 255, 255));
                    g.DrawPath(edge, path);
                    text = Fade(Theme.Text, dim);
                    break;
                }
            }

            if (Focused && ShowFocusCues)
            {
                using var ring = Glass.RoundedRect(RectangleF.Inflate(rect, 2, 2), 13);
                using var pen = new Pen(Color.FromArgb(180, Theme.AccentSoft), 1.5f);
                g.DrawPath(pen, ring);
            }

            // Content: optional icon + label, centred.
            int textW = TextRenderer.MeasureText(Text, Font).Width;
            int iconW = icon != null ? 20 : 0;
            int x = (int)((Width - textW - iconW) / 2f);
            if (icon != null)
            {
                Icons.Draw(g, icon.Value, new RectangleF(x, (Height - 15) / 2f, 15, 15), text, 1.7f);
                x += iconW;
            }
            TextRenderer.DrawText(g, Text, Font, new Rectangle(x, 0, textW + 4, Height), text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }

        private static void Sheen(Graphics g, GraphicsPath path, RectangleF rect, int alpha)
        {
            var top = new RectangleF(rect.X, rect.Y, rect.Width, Math.Max(2, rect.Height * 0.5f));
            using var brush = new LinearGradientBrush(top, Color.FromArgb(alpha, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Vertical);
            var state = g.Save();
            g.SetClip(path, CombineMode.Intersect);
            g.FillRectangle(brush, top.X, top.Y, top.Width, top.Height - 1.5f);
            g.Restore(state);
        }

        private static Color Blend(Color a, Color b, float t) => Color.FromArgb(
            (int)(a.R * (1 - t) + b.R * t), (int)(a.G * (1 - t) + b.G * t), (int)(a.B * (1 - t) + b.B * t));

        private static Color Fade(Color c, float dim) => Color.FromArgb((int)(255 * dim), c.R, c.G, c.B);
    }
}
