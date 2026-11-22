namespace ClassTrack.UI.Controls
{
    public enum ButtonKind { Primary, Secondary, Ghost, Danger }

    /// <summary>One button style for the whole app. Hover/press colors come from FlatAppearance.</summary>
    public class FlatButton : Button
    {
        private ButtonKind kind;
        private Color restForeColor;
        private Color hoverForeColor;

        public FlatButton(string text, ButtonKind kind = ButtonKind.Secondary)
        {
            Text = text;
            this.kind = kind;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            FlatStyle = FlatStyle.Flat;
            Font = Theme.Body;
            Padding = new Padding(14, 5, 14, 5);
            Margin = Padding.Empty;
            Cursor = Cursors.Hand;
            UseVisualStyleBackColor = false;
            FlatAppearance.BorderSize = 1;
            Apply();
        }

        [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public ButtonKind Kind
        {
            get => kind;
            set { kind = value; Apply(); }
        }

        private void Apply()
        {
            switch (kind)
            {
                case ButtonKind.Primary:
                    Set(Theme.Accent, Color.White, Theme.AccentHover, Theme.AccentPressed, Color.White, Color.White, Theme.Accent);
                    break;
                case ButtonKind.Danger:
                    Set(Theme.Surface, Theme.Danger, Theme.DangerHover, Theme.DangerPressed, Theme.Danger, Theme.Danger, Theme.Border);
                    break;
                case ButtonKind.Ghost:
                    Set(Theme.Bg, Theme.TextMuted, Theme.SurfaceHover, Theme.Input, Theme.TextMuted, Theme.Text, Theme.Border);
                    break;
                default: // Secondary
                    Set(Theme.Surface, Theme.Text, Theme.SurfaceHover, Theme.Input, Theme.Text, Theme.Text, Theme.BorderStrong);
                    break;
            }
        }

        private void Set(Color back, Color fore, Color over, Color down, Color rest, Color hover, Color border)
        {
            BackColor = back;
            ForeColor = fore;
            restForeColor = rest;
            hoverForeColor = hover;
            FlatAppearance.MouseOverBackColor = over;
            FlatAppearance.MouseDownBackColor = down;
            FlatAppearance.BorderColor = border;
        }

        protected override void OnMouseEnter(EventArgs e) { ForeColor = hoverForeColor; base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { ForeColor = restForeColor; base.OnMouseLeave(e); }
    }
}
