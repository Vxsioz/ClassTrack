using System.Drawing.Drawing2D;
using ClassTrack.Services;

namespace ClassTrack.UI
{
    /// <summary>Renders a student's picture (or initials placeholder) as a small rounded image.</summary>
    public static class Avatar
    {
        /// <param name="cornerRadius">Use size / 2 for a circle.</param>
        public static Bitmap Create(Student student, int size, int cornerRadius)
        {
            using var photo = ImageStore.Load(student.PhotoPath);
            return CreateFrom(photo, Initials(student.FirstName, student.Surname), size, cornerRadius);
        }

        /// <summary>Draws the given photo, or an initials placeholder when photo is null.</summary>
        public static Bitmap CreateFrom(Image? photo, string initials, int size, int cornerRadius)
        {
            var bmp = new Bitmap(size, size);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var rect = new Rectangle(0, 0, size - 1, size - 1);
            using var path = RoundedRect(rect, cornerRadius);

            if (photo != null)
            {
                g.SetClip(path);
                g.DrawImage(photo, new Rectangle(0, 0, size, size));
                g.ResetClip();
            }
            else
            {
                using var fill = new SolidBrush(Color.FromArgb(36, 39, 47));
                g.FillPath(fill, path);

                using var font = new Font("Segoe UI Semibold", size * 0.34F, FontStyle.Regular, GraphicsUnit.Pixel);
                using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var text = new SolidBrush(Theme.TextMuted);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.DrawString(initials, font, text, new RectangleF(0, 0, size, size + 1), format);
            }

            using var border = new Pen(Theme.Border);
            g.DrawPath(border, path);
            return bmp;
        }

        public static string Initials(string firstName, string surname)
        {
            string a = firstName.Trim().Length > 0 ? firstName.Trim()[..1] : "";
            string b = surname.Trim().Length > 0 ? surname.Trim()[..1] : "";
            return (a + b).ToUpperInvariant();
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            if (d <= 0) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
