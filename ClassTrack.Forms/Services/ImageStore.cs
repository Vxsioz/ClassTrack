using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ClassTrack.Services
{
    /// <summary>
    /// Saves cropped student pictures under %LocalAppData%\ClassTrack\ and hands back a
    /// relative path ("students/&lt;guid&gt;.jpg") that is stored in the database.
    /// Every picture gets a unique name, so one student's file can never be overwritten
    /// by another's.
    /// </summary>
    public static class ImageStore
    {
        public const int OutputSize = 512;                 // saved pictures are 512 x 512
        public const long MaxFileBytes = 10 * 1024 * 1024; // reject source files above 10 MB

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };

        public static string RootFolder { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClassTrack");

        private static string StudentsFolder => Path.Combine(RootFolder, "students");

        /// <summary>Checks extension and size. Returns an error message, or null if the file is acceptable.</summary>
        public static string? ValidateSourceFile(string path)
        {
            if (!AllowedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
                return "Please choose a JPG, JPEG or PNG image.";
            if (!File.Exists(path))
                return "That file could not be found.";
            if (new FileInfo(path).Length > MaxFileBytes)
                return "That image is larger than 10 MB. Please choose a smaller one.";
            return null;
        }

        private const int MaxSourceSide = 4096;   // larger photos are scaled down on load to bound memory

        /// <summary>
        /// Loads an image fully into memory so the file is not left locked, upright according to its
        /// EXIF orientation (phone photos), and no larger than 4096 px on the long side.
        /// Throws if the file is not a readable image (caller shows the message).
        /// </summary>
        public static Bitmap LoadSource(string path)
        {
            using var stream = File.OpenRead(path);
            using var img = Image.FromStream(stream);

            Bitmap bmp;
            int longSide = Math.Max(img.Width, img.Height);
            if (longSide > MaxSourceSide)
            {
                double f = (double)MaxSourceSide / longSide;
                bmp = new Bitmap((int)Math.Max(1, img.Width * f), (int)Math.Max(1, img.Height * f));
                using var g = Graphics.FromImage(bmp);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(img, 0, 0, bmp.Width, bmp.Height);
            }
            else
            {
                bmp = new Bitmap(img);
            }

            if (img.PropertyIdList.Contains(0x0112))   // EXIF orientation
            {
                bmp.RotateFlip(BitConverter.ToUInt16(img.GetPropertyItem(0x0112)!.Value!, 0) switch
                {
                    2 => RotateFlipType.RotateNoneFlipX,
                    3 => RotateFlipType.Rotate180FlipNone,
                    4 => RotateFlipType.RotateNoneFlipY,
                    5 => RotateFlipType.Rotate90FlipX,
                    6 => RotateFlipType.Rotate90FlipNone,
                    7 => RotateFlipType.Rotate270FlipX,
                    8 => RotateFlipType.Rotate270FlipNone,
                    _ => RotateFlipType.RotateNoneFlipNone
                });
            }
            return bmp;
        }

        /// <summary>Writes the square picture as a JPEG and returns its relative path.</summary>
        public static string Save(Image square)
        {
            Directory.CreateDirectory(StudentsFolder);

            string relative = $"students/{Guid.NewGuid():N}.jpg";
            using var output = new Bitmap(OutputSize, OutputSize);
            using (var g = Graphics.FromImage(output))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(square, 0, 0, OutputSize, OutputSize);
            }

            var jpeg = ImageCodecInfo.GetImageEncoders().First(e => e.FormatID == ImageFormat.Jpeg.Guid);
            using var parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(Encoder.Quality, 90L);
            output.Save(ToFullPath(relative)!, jpeg, parameters);
            return relative;
        }

        /// <summary>Loads a stored picture (a copy, so the file is never locked), or null if missing/unreadable.</summary>
        public static Bitmap? Load(string? relativePath)
        {
            try
            {
                string? full = ToFullPath(relativePath);
                return full != null && File.Exists(full) ? LoadSource(full) : null;
            }
            catch (Exception ex) when (ex is IOException or ArgumentException or OutOfMemoryException)
            {
                return null;
            }
        }

        /// <summary>Deletes a stored picture. Never throws; a leftover file is harmless.</summary>
        public static void Delete(string? relativePath)
        {
            try
            {
                string? full = ToFullPath(relativePath);
                if (full != null && File.Exists(full)) File.Delete(full);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }

        /// <summary>
        /// Turns a stored relative path into a full path, but only inside the students folder
        /// (guards against a tampered database value pointing elsewhere).
        /// </summary>
        private static string? ToFullPath(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return null;

            string full = Path.GetFullPath(Path.Combine(RootFolder, relativePath));
            string root = Path.GetFullPath(StudentsFolder) + Path.DirectorySeparatorChar;
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full : null;
        }
    }
}
