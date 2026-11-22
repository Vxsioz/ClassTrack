namespace ClassTrack.UI.Controls
{
    /// <summary>One figure in the dashboard strip: caption, big value, small note. Optional left divider.</summary>
    public sealed class StatBlock : TableLayoutPanel
    {
        public Label Value { get; } = new();
        public Label Note { get; } = new();
        private readonly bool divider;

        public StatBlock(string caption, bool divider)
        {
            this.divider = divider;
            ColumnCount = 1;
            RowCount = 3;
            Dock = DockStyle.Fill;
            Margin = Padding.Empty;
            Padding = new Padding(24, 18, 24, 18);
            BackColor = Theme.Surface;

            var cap = Ui.Text(caption.ToUpperInvariant(), Theme.Caption, Theme.TextMuted);
            cap.Dock = DockStyle.Fill;
            cap.AutoSize = false;
            cap.Height = Theme.Caption.Height + 6;

            Value.Margin = Padding.Empty;
            Note.Margin = Padding.Empty;
            Value.Font = Theme.Stat;
            Value.ForeColor = Theme.Text;
            Value.AutoSize = false;
            Value.AutoEllipsis = true;
            Value.Dock = DockStyle.Fill;
            Value.Height = Theme.Stat.Height + 6;
            Value.Text = "—";

            Note.Font = Theme.Small;
            Note.ForeColor = Theme.TextMuted;
            Note.AutoSize = false;
            Note.AutoEllipsis = true;
            Note.Dock = DockStyle.Fill;
            Note.Height = Theme.Small.Height + 6;

            RowStyles.Add(new RowStyle(SizeType.Absolute, cap.Height));
            RowStyles.Add(new RowStyle(SizeType.Absolute, Value.Height));
            RowStyles.Add(new RowStyle(SizeType.Absolute, Note.Height));
            Controls.Add(cap, 0, 0);
            Controls.Add(Value, 0, 1);
            Controls.Add(Note, 0, 2);
            Height = Padding.Vertical + cap.Height + Value.Height + Note.Height;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!divider) return;
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, 12, 0, Height - 12);
        }
    }
}
