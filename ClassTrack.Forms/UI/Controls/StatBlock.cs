namespace ClassTrack.UI.Controls
{
    /// <summary>One figure in the dashboard overview: caption, large light-weight value, quiet note. Optional left divider.</summary>
    public sealed class StatBlock : TablePanel
    {
        public Label Value { get; }
        public Label Note { get; }
        private readonly bool divider;

        public StatBlock(string caption, bool divider, bool text = false)
        {
            this.divider = divider;
            BackColor = Color.Transparent;
            ColumnCount = 1;
            RowCount = 3;
            Dock = DockStyle.Fill;
            AutoSize = true;
            Margin = Padding.Empty;
            Padding = new Padding(divider ? 28 : 4, 4, 16, 4);

            var cap = Ui.Text(caption.ToUpperInvariant(), Theme.Caption, Theme.TextMuted, new Padding(0, 0, 0, 6));
            Value = Ui.Text("—", text ? Theme.StatName : Theme.Stat, Theme.Text);
            Value.AutoSize = false;
            Value.AutoEllipsis = true;
            Value.Dock = DockStyle.Fill;
            Value.Height = Theme.Stat.Height + 6;
            Value.TextAlign = text ? ContentAlignment.BottomLeft : ContentAlignment.MiddleLeft;
            Note = Ui.Text("", Theme.Small, Theme.TextMuted);
            Note.AutoSize = false;
            Note.AutoEllipsis = true;
            Note.Dock = DockStyle.Fill;
            Note.Height = Theme.Small.Height + 4;

            Controls.Add(cap, 0, 0);
            Controls.Add(Value, 0, 1);
            Controls.Add(Note, 0, 2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!divider) return;
            using var pen = new Pen(Color.FromArgb(30, 255, 255, 255));
            e.Graphics.DrawLine(pen, 0, 6, 0, Height - 6);
        }
    }
}
