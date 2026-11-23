using System.Drawing.Drawing2D;
using ClassTrack.Services;
using ClassTrack.UI;
using ClassTrack.UI.Controls;

namespace ClassTrack
{
    /// <summary>
    /// Crop a picture to a square: drag to reposition, scroll or use the slider to zoom,
    /// watch the exact result in the preview, then Apply crop or Cancel.
    /// </summary>
    public sealed class CropDialog : DarkForm
    {
        private const int CropViewSize = 440;
        private const int PreviewSize = 168;

        private readonly CropControl crop = new() { Size = new Size(CropViewSize, CropViewSize), Margin = Padding.Empty };
        private readonly DarkSlider slider = new() { Dock = DockStyle.Fill };
        private readonly GlassPicture preview = new() { AccessibleName = "Crop preview", Size = new Size(PreviewSize, PreviewSize), Margin = Padding.Empty, BackColor = Color.Transparent };
        private readonly System.Windows.Forms.Timer previewTimer = new() { Interval = 40 };
        private bool previewDirty = true;
        private bool syncingZoom;

        /// <summary>The cropped square picture after Apply. The caller disposes it.</summary>
        public Bitmap? Result { get; private set; }

        /// <param name="source">The picture to crop. The dialog does not dispose it.</param>
        public CropDialog(Bitmap source)
        {
            Text = "Crop picture";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(14);

            var card = new GlassPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(26, 24, 26, 22), Radius = 24 };
            var layout = new TablePanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                BackColor = Color.Transparent
            };

            // Left: the crop stage and zoom control
            layout.Controls.Add(Ui.Text("Crop picture", Theme.H2, Theme.Text), 0, 0);
            layout.SetColumnSpan(layout.GetControlFromPosition(0, 0)!, 2);
            layout.Controls.Add(Ui.Text("Choose exactly which part of the photo is kept.", Theme.Small, Theme.TextMuted, new Padding(0, 2, 0, 16)), 0, 1);
            layout.SetColumnSpan(layout.GetControlFromPosition(0, 1)!, 2);
            layout.Controls.Add(crop, 0, 2);
            layout.SetRowSpan(crop, 2);
            layout.Controls.Add(BuildZoomRow(), 0, 4);

            // Right: live preview of exactly what will be saved
            var side = new FlowPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(26, 0, 0, 0)
            };
            side.Controls.Add(Ui.Text("PREVIEW", Theme.Caption, Theme.TextMuted, new Padding(0, 0, 0, 8)));
            side.Controls.Add(preview);
            side.Controls.Add(Ui.Text("This is the square picture\nthat will be saved.", Theme.Small, Theme.TextMuted, new Padding(0, 12, 0, 20)));
            side.Controls.Add(Ui.Text("Drag to move the picture.\nScroll or use the slider to zoom.\nArrow keys nudge it.", Theme.Small, Theme.TextFaint));
            if (Math.Min(source.Width, source.Height) < 256)
                side.Controls.Add(Ui.Text("This picture is small, so it may\nlook soft once cropped.", Theme.Small, Theme.Danger, new Padding(0, 18, 0, 0)));
            layout.Controls.Add(side, 1, 2);
            layout.SetRowSpan(side, 3);

            var btnApply = new FlatButton("Apply crop", ButtonKind.Primary) { Margin = new Padding(10, 0, 0, 0) };
            btnApply.Click += (_, _) => Apply();
            var btnCancel = new FlatButton("Cancel") { DialogResult = DialogResult.Cancel };
            var buttons = new FlowPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 20, 0, 0)
            };
            buttons.Controls.Add(btnApply);
            buttons.Controls.Add(btnCancel);
            layout.Controls.Add(buttons, 0, 5);
            layout.SetColumnSpan(buttons, 2);

            card.Controls.Add(layout);
            Controls.Add(card);
            AcceptButton = btnApply;
            CancelButton = btnCancel;

            // Wire up: slider <-> zoom, and a coalesced preview refresh
            crop.Source = source;
            slider.ValueChanged += (_, _) =>
            {
                if (syncingZoom) return;
                syncingZoom = true;
                crop.Zoom = (float)Math.Pow(CropControl.MaxZoom, slider.Value / 100.0);   // exponential: even feel
                syncingZoom = false;
            };
            crop.ZoomChanged += (_, _) =>
            {
                if (syncingZoom) return;
                syncingZoom = true;
                slider.Value = (int)Math.Round(100 * Math.Log(Math.Max(1, crop.Zoom)) / Math.Log(CropControl.MaxZoom));
                syncingZoom = false;
            };
            crop.CropChanged += (_, _) => previewDirty = true;
            previewTimer.Tick += (_, _) => RefreshPreview();
            previewTimer.Start();
            RefreshPreview();
        }

        private Control BuildZoomRow()
        {
            var row = new TablePanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, Margin = new Padding(0, 14, 0, 0), BackColor = Color.Transparent };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var zoomOut = Ui.Text("–", Theme.H2, Theme.TextMuted, new Padding(0, 0, 10, 0));
            var zoomIn = Ui.Text("+", Theme.H2, Theme.TextMuted, new Padding(10, 0, 0, 0));
            zoomOut.Anchor = zoomIn.Anchor = AnchorStyles.None;
            row.Controls.Add(zoomOut, 0, 0);
            row.Controls.Add(slider, 1, 0);
            row.Controls.Add(zoomIn, 2, 0);
            return row;
        }

        private void RefreshPreview()
        {
            if (!previewDirty) return;
            previewDirty = false;

            var old = preview.Image;
            using var raw = crop.GetCrop(PreviewSize, InterpolationMode.HighQualityBilinear);
            preview.Image = raw == null ? null : Avatar.CreateFrom(raw, "", PreviewSize, 18);
            old?.Dispose();
        }

        private void Apply()
        {
            Result = crop.GetCrop(ImageStore.OutputSize);
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                previewTimer.Dispose();
                preview.Image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
