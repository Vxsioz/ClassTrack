namespace ClassTrack.UI
{
    /// <summary>Tiny factory helpers so views don't repeat label boilerplate.</summary>
    public static class Ui
    {
        public static Label Text(string text, Font font, Color color, Padding? margin = null) => new()
        {
            Text = text,
            Font = font,
            ForeColor = color,
            AutoSize = true,
            Margin = margin ?? Padding.Empty,
            BackColor = Color.Transparent
        };

        /// <summary>Page title + muted subtitle, stacked.</summary>
        public static FlowLayoutPanel Heading(string title, Label subtitle)
        {
            var panel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = Padding.Empty,
                Anchor = AnchorStyles.Left
            };
            panel.Controls.Add(Text(title, Theme.H1, Theme.Text));
            subtitle.Margin = new Padding(2, 2, 0, 0);
            panel.Controls.Add(subtitle);
            return panel;
        }
    }
}
