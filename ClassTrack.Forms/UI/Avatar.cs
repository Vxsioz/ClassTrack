using System.Drawing.Drawing2D;
using ClassTrack.Services;

namespace ClassTrack.UI
{
    /// <summary>Renders a student's picture (or an initials placeholder) as a small rounded image with a glass ring.</summary>
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

            var rect = new RectangleF(0.5f, 0.5f, size - 1.5f, size - 1.5f);
            using var path = Glass.RoundedRect(rect, cornerRadius);

            if (photo != null)
            {
                g.SetClip(path);
                g.DrawImage(photo, new Rectangle(0, 0, size, size));
                g.ResetClip();
            }
            else
            {
                using var fill = new LinearGradientBrush(rect, Color.FromArgb(64, 78, 112), Color.FromArgb(34, 38, 54), LinearGradientMode.ForwardDiagonal);
                g.FillPath(fill, path);

                using var font = new Font("Segoe UI Semibold", size * 0.34F, FontStyle.Regular, GraphicsUnit.Pixel);
                using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var text = new SolidBrush(Color.FromArgb(215, 224, 245));
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.DrawString(initials, font, text, new RectangleF(0, 0, size, size + 1), format);
            }

            // Glass ring: brighter at the top-left.
            using var ring = new LinearGradientBrush(rect, Color.FromArgb(120, 255, 255, 255), Color.FromArgb(24, 255, 255, 255), LinearGradientMode.ForwardDiagonal);
            using var pen = new Pen(ring, size >= 100 ? 1.5f : 1f);
            g.DrawPath(pen, path);
            return bmp;
        }

        public static string Initials(string firstName, string surname)
        {
            string a = firstName.Trim().Length > 0 ? firstName.Trim()[..1] : "";
            string b = surname.Trim().Length > 0 ? surname.Trim()[..1] : "";
            return (a + b).ToUpperInvariant();
        }
    }
}
