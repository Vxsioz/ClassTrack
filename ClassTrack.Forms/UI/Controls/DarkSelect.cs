using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace ClassTrack.UI.Controls
{
    /// <summary>
    /// A dark drop-down list (the native ComboBox can't be fully darkened). Pick one string from Items.
    /// Opens a themed menu on click, Enter, Space or F4; Up/Down change the choice when closed.
    /// </summary>
    public class DarkSelect : Control
    {
        private readonly List<string> items = new();
        private int selectedIndex = -1;
        private bool hover;
        private bool open;
        private DateTime lastClosed = DateTime.MinValue;

        public event EventHandler? SelectedIndexChanged;

        public DarkSelect()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            TabStop = true;
            Font = Theme.Body;
            Cursor = Cursors.Hand;
            Margin = Padding.Empty;

            using var probe = new TextBox { Font = Font };
            Height = probe.PreferredHeight + 18;   // same height as DarkTextBox
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<string> Items => items;

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                int v = items.Count == 0 ? -1 : Math.Clamp(value, -1, items.Count - 1);
                if (v == selectedIndex) return;
                selectedIndex = v;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>The selected item's text, or "" if nothing is selected.</summary>
        [AllowNull]
        public override string Text
        {
            get => selectedIndex >= 0 && selectedIndex < items.Count ? items[selectedIndex] : "";
            set { }
        }

        // ---- painting ---------------------------------------------------------

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.Input);

            Color border = open || (Focused && ShowFocusCues) ? Theme.Accent : hover ? Theme.BorderStrong : Theme.Border;
            using (var pen = new Pen(border)) g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

            TextRenderer.DrawText(g, Text, Font, new Rectangle(11, 0, Width - 40, Height), Theme.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            // chevron
            int cx = Width - 20, cy = Height / 2;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var chevron = new Pen(Theme.TextMuted, 1.5F);
            g.DrawLines(chevron, new[] { new Point(cx - 4, cy - 2), new Point(cx, cy + 2), new Point(cx + 4, cy - 2) });
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        // ---- interaction ------------------------------------------------------

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            // Clicking the box while its menu is open closes the menu; don't instantly reopen it.
            if (e.Button == MouseButtons.Left && (DateTime.UtcNow - lastClosed).TotalMilliseconds > 250)
                OpenMenu();
        }

        protected override bool IsInputKey(Keys keyData) =>
            keyData is Keys.Up or Keys.Down || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.Enter or Keys.Space or Keys.F4:
                    e.Handled = true; OpenMenu(); break;
                case Keys.Down:
                    e.Handled = true; SelectedIndex = selectedIndex + 1; break;
                case Keys.Up:
                    e.Handled = true; SelectedIndex = Math.Max(0, selectedIndex - 1); break;
            }
        }

        private void OpenMenu()
        {
            if (items.Count == 0) return;

            var menu = new ToolStripDropDownMenu
            {
                Renderer = new DarkMenuRenderer(),
                ShowImageMargin = false,
                ShowCheckMargin = false,
                BackColor = Theme.Surface,
                Font = Font,
                MinimumSize = new Size(Width, 0),
                Padding = new Padding(0, 4, 0, 4)
            };
            for (int i = 0; i < items.Count; i++)
            {
                int index = i;
                var item = new ToolStripMenuItem(items[i])
                {
                    ForeColor = i == selectedIndex ? Theme.AccentSoft : Theme.Text,
                    BackColor = Theme.Surface,
                    Padding = new Padding(4, 5, 4, 5)
                };
                item.Click += (_, _) => SelectedIndex = index;
                menu.Items.Add(item);
            }
            menu.Closed += (_, _) =>
            {
                open = false;
                lastClosed = DateTime.UtcNow;
                Invalidate();
                BeginInvoke(menu.Dispose);   // not now: the menu is still finishing the click that closed it
            };

            open = true;
            Invalidate();
            menu.Show(this, new Point(0, Height + 2));
        }

        /// <summary>Flat dark look for the drop-down menu: hover highlight, hairline border.</summary>
        private sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
        {
            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using var b = new SolidBrush(Theme.Surface);
                e.Graphics.FillRectangle(b, e.AffectedBounds);
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using var pen = new Pen(Theme.BorderStrong);
                e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                using var b = new SolidBrush(e.Item.Selected ? Theme.SurfaceHover : Theme.Surface);
                e.Graphics.FillRectangle(b, new Rectangle(1, 0, e.Item.Width - 2, e.Item.Height));
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = e.Item.ForeColor;
                base.OnRenderItemText(e);
            }
        }
    }
}
