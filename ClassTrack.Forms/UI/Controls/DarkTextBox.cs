using System.Diagnostics.CodeAnalysis;

namespace ClassTrack.UI.Controls
{
    /// <summary>
    /// A text box with a dark fill and a 1px border that turns accent-colored on focus.
    /// (The native TextBox can't draw its own border, so it sits inside this panel.)
    /// </summary>
    public class DarkTextBox : Panel
    {
        private readonly TextBox box = new();

        public DarkTextBox()
        {
            BackColor = Theme.Input;
            Padding = new Padding(11, 8, 11, 8);
            Margin = Padding.Empty;

            box.BorderStyle = BorderStyle.None;
            box.BackColor = Theme.Input;
            box.ForeColor = Theme.Text;
            box.Font = Theme.Body;
            box.Dock = DockStyle.Fill;
            box.GotFocus += (_, _) => Invalidate();
            box.LostFocus += (_, _) => Invalidate();
            Controls.Add(box);

            Height = box.PreferredHeight + Padding.Vertical + 2;
            Click += (_, _) => box.Focus();
        }

        /// <summary>The inner TextBox, for key events and focus.</summary>
        public TextBox Inner => box;

        [AllowNull]
        public override string Text
        {
            get => box.Text;
            set => box.Text = value ?? "";
        }

        [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string PlaceholderText
        {
            get => box.PlaceholderText;
            set => box.PlaceholderText = value;
        }

        [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int MaxLength
        {
            get => box.MaxLength;
            set => box.MaxLength = value;
        }

        public new bool Focus() => box.Focus();

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(box.Focused ? Theme.Accent : Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }
}
