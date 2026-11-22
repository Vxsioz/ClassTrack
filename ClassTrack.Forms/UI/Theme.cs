using System.Runtime.InteropServices;

namespace ClassTrack.UI
{
    /// <summary>Every color and font in the app lives here, so the look stays consistent.</summary>
    public static class Theme
    {
        // Surfaces, darkest to lightest
        public static readonly Color Bg = Color.FromArgb(14, 15, 18);
        public static readonly Color Sidebar = Color.FromArgb(17, 18, 22);
        public static readonly Color SidebarHover = Color.FromArgb(24, 26, 31);
        public static readonly Color Surface = Color.FromArgb(22, 24, 29);
        public static readonly Color SurfaceHover = Color.FromArgb(28, 30, 36);
        public static readonly Color Input = Color.FromArgb(27, 29, 35);

        // Lines
        public static readonly Color Border = Color.FromArgb(38, 41, 48);
        public static readonly Color BorderStrong = Color.FromArgb(58, 62, 72);

        // Text
        public static readonly Color Text = Color.FromArgb(230, 232, 235);
        public static readonly Color TextMuted = Color.FromArgb(139, 144, 154);
        public static readonly Color TextFaint = Color.FromArgb(92, 97, 107);

        // One restrained accent, used only for primary actions and the active nav item
        public static readonly Color Accent = Color.FromArgb(62, 99, 221);
        public static readonly Color AccentHover = Color.FromArgb(82, 116, 230);
        public static readonly Color AccentPressed = Color.FromArgb(51, 84, 194);
        public static readonly Color AccentSoft = Color.FromArgb(132, 160, 255);

        public static readonly Color Danger = Color.FromArgb(232, 92, 92);
        public static readonly Color DangerHover = Color.FromArgb(48, 26, 29);
        public static readonly Color DangerPressed = Color.FromArgb(64, 31, 35);
        public static readonly Color Success = Color.FromArgb(72, 187, 128);

        // A small, fixed type scale
        public static readonly Font Body = new("Segoe UI", 10F);
        public static readonly Font Small = new("Segoe UI", 9F);
        public static readonly Font Caption = new("Segoe UI Semibold", 8.5F);
        public static readonly Font H1 = new("Segoe UI Semibold", 20F);
        public static readonly Font H2 = new("Segoe UI Semibold", 12F);
        public static readonly Font Stat = new("Segoe UI Semibold", 22F);

        // ---- dark title bar (Windows 10 build 18985+ / Windows 11; silently skipped otherwise) ----

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        private static int ColorRef(Color c) => c.R | (c.G << 8) | (c.B << 16);

        private static void SetDwm(IntPtr handle, int attribute, int value)
        {
            try { DwmSetWindowAttribute(handle, attribute, ref value, sizeof(int)); }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException) { }
        }

        public static void ApplyDarkTitleBar(IntPtr handle)
        {
            SetDwm(handle, 20, 1);                      // use dark mode
            SetDwm(handle, 35, ColorRef(Bg));           // caption color
            SetDwm(handle, 36, ColorRef(Text));         // caption text
            SetDwm(handle, 34, ColorRef(Border));       // window border
        }
    }
}
