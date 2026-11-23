using ClassTrack.UI.Controls;

namespace ClassTrack.UI
{
    /// <summary>A small glass message that slides up bottom-right for a few seconds ("Student saved").</summary>
    public static class Toast
    {
        private static ToastPanel? current;
        private static readonly System.Windows.Forms.Timer Timer = new() { Interval = 3600 };

        static Toast() => Timer.Tick += (_, _) => Dismiss();

        public static void Show(Control owner, string message, bool error = false)
        {
            var form = owner.FindForm();
            if (form == null) return;

            Dismiss();

            var toast = new ToastPanel(message, error ? Theme.Danger : Theme.Success) { Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            form.Controls.Add(toast);
            toast.PerformLayout();

            int x = form.ClientSize.Width - toast.Width - 28;
            int y = form.ClientSize.Height - toast.Height - 28;
            toast.Location = new Point(x, y + 14);
            toast.BringToFront();
            current = toast;

            // Enter: rise 14px into place (ease-out). Reduced-motion users get it in place.
            var slide = new Tween(() => { if (!toast.IsDisposed) toast.Top = y + (int)(14 * (1 - slideValue(toast))); });
            toast.Tag = slide;
            slide.To(1, 220);

            Timer.Stop();
            Timer.Start();

            static float slideValue(ToastPanel t) => t.Tag is Tween tw ? tw.Value : 1f;
        }

        private static void Dismiss()
        {
            Timer.Stop();
            if (current == null) return;
            current.Parent?.Controls.Remove(current);
            current.Dispose();
            current = null;
        }

        private sealed class ToastPanel : GlassPanel
        {
            private readonly Color accent;

            public ToastPanel(string message, Color accent)
            {
                this.accent = accent;
                Radius = 16;
                AutoSize = true;
                AutoSizeMode = AutoSizeMode.GrowAndShrink;
                Padding = new Padding(40, 14, 22, 14);
                var label = Ui.Text(message, Theme.Body, Theme.Text);
                label.MaximumSize = new Size(420, 0);
                Controls.Add(label);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Glass.Smooth(e.Graphics);
                using var glow = new SolidBrush(Color.FromArgb(60, accent));
                e.Graphics.FillEllipse(glow, 15, Height / 2f - 7, 14, 14);
                using var dot = new SolidBrush(accent);
                e.Graphics.FillEllipse(dot, 19, Height / 2f - 3, 6, 6);
            }
        }
    }
}
