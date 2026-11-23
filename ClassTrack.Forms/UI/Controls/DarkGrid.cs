namespace ClassTrack.UI.Controls
{
    /// <summary>A flat, hairline-separated table with a subtle row hover. Read-only, no selection highlight.</summary>
    public class DarkGrid : DataGridView
    {
        public DarkGrid()
        {
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            BackgroundColor = Theme.TableSurface;
            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            GridColor = Theme.TableLine;

            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeRows = false;
            ReadOnly = true;
            RowHeadersVisible = false;
            MultiSelect = false;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            EnableHeadersVisualStyles = false;
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            ColumnHeadersHeight = Theme.Caption.Height + 22;
            RowTemplate.Height = Theme.Body.Height + 26;

            var header = ColumnHeadersDefaultCellStyle;
            header.BackColor = Theme.TableSurface;
            header.ForeColor = Theme.TextMuted;
            header.Font = Theme.Caption;
            header.SelectionBackColor = Theme.TableSurface;
            header.SelectionForeColor = Theme.TextMuted;
            header.Padding = new Padding(12, 0, 0, 0);

            var cells = DefaultCellStyle;
            cells.BackColor = Theme.TableSurface;
            cells.ForeColor = Theme.Text;
            cells.Font = Theme.Body;
            cells.SelectionBackColor = Theme.TableSurface;
            cells.SelectionForeColor = Theme.Text;
            cells.Padding = new Padding(12, 0, 0, 0);

            // Hover highlight: set on the row, cleared on leave.
            CellMouseEnter += (_, e) => { if (e.RowIndex >= 0) Rows[e.RowIndex].DefaultCellStyle.BackColor = Theme.TableHover; };
            CellMouseLeave += (_, e) => { if (e.RowIndex >= 0 && e.RowIndex < Rows.Count) Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.Empty; };

            SelectionChanged += (_, _) => ClearSelection();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.ApplyDarkScrollbars(Handle);
            // The grid's scrollbars are child controls with their own handles.
            foreach (Control child in Controls)
            {
                if (child is not ScrollBar bar) continue;
                if (bar.IsHandleCreated) Theme.ApplyDarkScrollbars(bar.Handle);
                else bar.HandleCreated += (_, _) => Theme.ApplyDarkScrollbars(bar.Handle);
            }
        }

        /// <summary>Width that fits a header's text plus room for padding and a sort arrow.</summary>
        public static int HeaderWidth(string text, int extra = 44) =>
            TextRenderer.MeasureText(text, Theme.Caption).Width + extra;

        /// <summary>A text-link action column ("Edit", "Delete"...), clicked via CellContentClick.</summary>
        public static DataGridViewLinkColumn LinkColumn(string name, string text, Color color, string header = "")
        {
            return new DataGridViewLinkColumn
            {
                Name = name,
                HeaderText = header,
                Text = text,
                UseColumnTextForLinkValue = true,
                LinkColor = color,
                ActiveLinkColor = color,
                VisitedLinkColor = color,
                TrackVisitedState = false,
                LinkBehavior = LinkBehavior.HoverUnderline,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = TextRenderer.MeasureText(text, Theme.Body).Width + 24,
                Resizable = DataGridViewTriState.False,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
        }
    }
}
