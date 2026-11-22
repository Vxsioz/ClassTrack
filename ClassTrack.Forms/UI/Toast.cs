namespace ClassTrack.UI
{
    /// <summary>A small message that appears bottom-right for a few seconds ("Student saved").</summary>
    public static class Toast
    {
        private static Label? current;
        private static readonly System.Windows.Forms.Timer Timer = new() { Interval = 3500 };

        static Toast() => Timer.Tick += (_, _) => Dismiss();

        public static void Show(Control owner, string message, bool error = false)
        {
            var form = owner.FindForm();
            if (form == null) return;

            Dismiss();

            Color accent = error ? Theme.Danger : Theme.Success;
            var label = new Label
            {
                Text = message,
                Font = Theme.Body,
                ForeColor = Theme.Text,
                BackColor = Theme.Surface,
                AutoSize = true,
                MaximumSize = new Size(440, 0),
                Padding = new Padding(18, 12, 16, 12),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            label.Paint += (_, e) =>
            {
                using var border = new Pen(Theme.BorderStrong);
                e.Graphics.DrawRectangle(border, 0, 0, label.Width - 1, label.Height - 1);
                using var bar = new SolidBrush(accent);
                e.Graphics.FillRectangle(bar, 0, 0, 3, label.Height);
            };

            form.Controls.Add(label);
            var size = label.PreferredSize;
            label.Location = new Point(form.ClientSize.Width - size.Width - 24, form.ClientSize.Height - size.Height - 24);
            label.BringToFront();

            current = label;
            Timer.Stop();
            Timer.Start();
        }

        private static void Dismiss()
        {
            Timer.Stop();
            if (current == null) return;
            current.Parent?.Controls.Remove(current);
            current.Dispose();
            current = null;
        }
    }
}
