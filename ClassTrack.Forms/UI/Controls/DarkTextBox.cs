using System.Diagnostics.CodeAnalysis;

namespace ClassTrack.UI.Controls
{
    /// <summary>
    /// A rounded glass input. The native TextBox sits inside it (it can't draw its own border);
    /// the field around it glows accent on focus and red when <see cref="Invalid"/>.
    /// </summary>
    public class DarkTextBox : Control
    {
        private readonly TextBox box = new();
        private bool hover;
        private bool invalid;
        private IconKind? leftIcon;

        public DarkTextBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Margin = Padding.Empty;
            Padding = new Padding(13, 0, 13, 0);

            box.BorderStyle = BorderStyle.None;
            box.BackColor = Theme.Input;
            box.ForeColor = Theme.Text;
            box.Font = Theme.Body;
            box.GotFocus += (_, _) => Invalidate();
            box.LostFocus += (_, _) => Invalidate();
            box.MouseEnter += (_, _) => { hover = true; Invalidate(); };
            box.MouseLeave += (_, _) => { hover = false; Invalidate(); };
            box.TextChanged += (_, _) => { if (invalid) { invalid = false; Invalidate(); } };
            Controls.Add(box);

            Height = box.PreferredHeight + 22;
            Click += (_, _) => box.Focus();
        }

        /// <summary>The field's name for screen readers and UI automation (applied to the inner text box too).</summary>
        [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Caption { set { AccessibleName = value; box.AccessibleName = value; } }

        /// <summary>The inner TextBox, for key events and focus.</summary>
        public TextBox Inner => box;

        [AllowNull]
        public override string Text
        {
            get => box.Text;
            set => box.Text = value ?? "";
        }

        [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string PlaceholderText { get => box.PlaceholderText; set => box.PlaceholderText = value; }

        [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int MaxLength { get => box.MaxLength; set => box.MaxLength = value; }

        /// <summary>Marks the field red until the user edits it again.</summary>
        [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool Invalid { get => invalid; set { invalid = value; Invalidate(); } }

        [System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public IconKind? LeftIcon { get => leftIcon; set { leftIcon = value; PerformLayout(); Invalidate(); } }

        public new bool Focus() => box.Focus();

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            int left = Padding.Left + (leftIcon != null ? 26 : 0);
            int w = Math.Max(10, Width - left - Padding.Right);
            box.SetBounds(left, (Height - box.PreferredHeight) / 2, w, box.PreferredHeight);
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaintBackground(PaintEventArgs e) { }   // painted at the start of OnPaint instead (buffered controls ignore this)

        protected override void OnPaint(PaintEventArgs e)
        {
            Backdrop.PaintBehind(e.Graphics, this);   // what is behind us: backdrop and any glass around us
            var field = new Rectangle(2, 2, Width - 4, Height - 4);   // leaves room for the focus glow
            Glass.PaintField(e.Graphics, field, 11, box.Focused, invalid, hover);
            if (leftIcon != null)
                Icons.Draw(e.Graphics, leftIcon.Value, new RectangleF(Padding.Left - 1, (Height - 16) / 2f, 16, 16),
                    box.Focused ? Theme.AccentSoft : Theme.TextMuted, 1.5f);
        }
    }
}
