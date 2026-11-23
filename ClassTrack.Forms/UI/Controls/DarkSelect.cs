using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;

namespace ClassTrack.UI.Controls
{
    /// <summary>
    /// A glass drop-down list (the native ComboBox can't be fully darkened). Pick one string from Items.
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
                     ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            TabStop = true;
            Font = Theme.Body;
            Cursor = Cursors.Hand;
            Margin = Padding.Empty;
            AccessibleRole = AccessibleRole.ComboBox;

            using var probe = new TextBox { Font = Font };
            Height = probe.PreferredHeight + 22;   // same height as DarkTextBox
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


        protected override void OnPaintBackground(PaintEventArgs e) { }   // painted at the start of OnPaint instead (buffered controls ignore this)

        protected override void OnPaint(PaintEventArgs e)
        {
            Backdrop.PaintBehind(e.Graphics, this);   // what is behind us: backdrop and any glass around us
            var g = e.Graphics;
            Glass.PaintField(g, new Rectangle(2, 2, Width - 4, Height - 4), 11, open || (Focused && ShowFocusCues), false, hover);

            TextRenderer.DrawText(g, Text, Font, new Rectangle(14, 0, Width - 44, Height), Theme.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            int cx = Width - 22, cy = Height / 2;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var chevron = new Pen(Theme.TextMuted, 1.5F) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
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
                Renderer = new GlassMenuRenderer(),
                ShowImageMargin = false,
                ShowCheckMargin = false,
                BackColor = Color.FromArgb(30, 32, 38),
                Font = Font,
                MinimumSize = new Size(Width - 4, 0),
                Padding = new Padding(4, 6, 4, 6)
            };
            for (int i = 0; i < items.Count; i++)
            {
                int index = i;
                var item = new ToolStripMenuItem(items[i])
                {
                    ForeColor = i == selectedIndex ? Theme.AccentSoft : Theme.Text,
                    BackColor = Color.FromArgb(30, 32, 38),
                    Padding = new Padding(6, 6, 6, 6)
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
            menu.Show(this, new Point(2, Height - 2));
        }

        /// <summary>Flat dark look for the drop-down menu: rounded hover pill, hairline border.</summary>
        private sealed class GlassMenuRenderer : ToolStripProfessionalRenderer
        {
            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using var b = new SolidBrush(Color.FromArgb(30, 32, 38));
                e.Graphics.FillRectangle(b, e.AffectedBounds);
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using var pen = new Pen(Color.FromArgb(70, 255, 255, 255));
                e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (!e.Item.Selected) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = Glass.RoundedRect(new RectangleF(1, 1, e.Item.Width - 2, e.Item.Height - 2), 8);
                using var b = new SolidBrush(Color.FromArgb(36, 255, 255, 255));
                g.FillPath(b, path);
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = e.Item.ForeColor;
                base.OnRenderItemText(e);
            }
        }
    }
}
