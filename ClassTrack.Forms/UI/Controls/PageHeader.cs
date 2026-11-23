namespace ClassTrack.UI.Controls
{
    /// <summary>The top of every page: big title, quiet subtitle, and a right-aligned slot for actions.</summary>
    public sealed class PageHeader : TablePanel
    {
        private readonly Label title;
        private readonly Label subtitle;
        private readonly FlowLayoutPanel actions;

        public PageHeader(string titleText, string subtitleText = "")
        {
            BackColor = Color.Transparent;
            Dock = DockStyle.Fill;
            AutoSize = true;
            ColumnCount = 2;
            RowCount = 1;
            Margin = new Padding(0, 0, 0, 22);
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            title = Ui.Text(titleText, Theme.H1, Theme.Text);
            subtitle = Ui.Text(subtitleText, Theme.Body, Theme.TextMuted, new Padding(2, 3, 0, 0));
            var stack = new FlowPanel
            {
                BackColor = Color.Transparent,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = Padding.Empty,
                Anchor = AnchorStyles.Left
            };
            stack.Controls.Add(title);
            stack.Controls.Add(subtitle);

            actions = new FlowPanel
            {
                BackColor = Color.Transparent,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty,
                Anchor = AnchorStyles.Right
            };

            Controls.Add(stack, 0, 0);
            Controls.Add(actions, 1, 0);
        }

        [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Title { get => title.Text; set => title.Text = value; }
        [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Subtitle { get => subtitle.Text; set => subtitle.Text = value; }

        /// <summary>Adds a button to the right-hand side, spaced from its neighbour.</summary>
        public void AddAction(Control button)
        {
            button.Margin = new Padding(actions.Controls.Count == 0 ? 0 : 10, 0, 0, 0);
            actions.Controls.Add(button);
        }
    }
}
