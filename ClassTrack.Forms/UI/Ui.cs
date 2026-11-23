using ClassTrack.UI.Controls;

namespace ClassTrack.UI
{
    /// <summary>Tiny factory helpers so views don't repeat label boilerplate.</summary>
    public static class Ui
    {
        /// <summary>A transparent label (it shows the glass or backdrop behind it).</summary>
        public static Label Text(string text, Font font, Color color, Padding? margin = null) => new GlassLabel()
        {
            Text = text,
            Font = font,
            ForeColor = color,
            AutoSize = true,
            Margin = margin ?? Padding.Empty,
            BackColor = Color.Transparent
        };

        /// <summary>A hairline divider that lets the glass show through.</summary>
        public static Panel Divider(int height = 1) => new()
        {
            Height = height,
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(26, 255, 255, 255),
            Margin = new Padding(0, 6, 0, 6)
        };
    }
}
