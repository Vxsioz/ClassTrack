using System.Runtime.InteropServices;

namespace ClassTrack.UI
{
    /// <summary>Every color and font in the app lives here, so the look stays consistent.</summary>
    public static class Theme
    {
        // ---- Backdrop (what the glass sits on) --------------------------------
        public static readonly Color BackdropTop = Color.FromArgb(15, 16, 20);
        public static readonly Color BackdropBottom = Color.FromArgb(9, 10, 13);
        public static readonly Color Bg = Color.FromArgb(12, 13, 16);               // solid fallback

        // ---- Glass: translucent white over the backdrop ------------------------
        public const int GlassAlpha = 20;            // resting fill  (~8% white)
        public const int GlassAlphaHover = 34;       // hovered fill
        public static readonly Color GlassTint = Color.White;
        public static readonly Color GlassEdge = Color.FromArgb(34, 255, 255, 255);
        public static readonly Color GlassEdgeBright = Color.FromArgb(80, 255, 255, 255);

        // Denser surface for tables, where text density needs a calmer, opaque field.
        public static readonly Color TableSurface = Color.FromArgb(26, 28, 33);
        public static readonly Color TableHover = Color.FromArgb(35, 38, 45);
        public static readonly Color TableLine = Color.FromArgb(42, 45, 53);

        // Solid stand-ins used where a native control needs an opaque back color.
        public static readonly Color Sidebar = Color.FromArgb(17, 18, 22);
        public static readonly Color SidebarHover = Color.FromArgb(24, 26, 31);
        public static readonly Color Surface = Color.FromArgb(26, 28, 33);
        public static readonly Color SurfaceHover = Color.FromArgb(35, 38, 45);
        public static readonly Color Input = Color.FromArgb(29, 31, 37);

        // ---- Lines ------------------------------------------------------------
        public static readonly Color Border = Color.FromArgb(46, 49, 58);
        public static readonly Color BorderStrong = Color.FromArgb(72, 76, 88);

        // ---- Text --------------------------------------------------------------
        public static readonly Color Text = Color.FromArgb(241, 243, 247);
        public static readonly Color TextMuted = Color.FromArgb(152, 158, 171);
        public static readonly Color TextFaint = Color.FromArgb(101, 107, 120);

        // ---- One restrained cool accent ---------------------------------------
        public static readonly Color Accent = Color.FromArgb(58, 134, 255);
        public static readonly Color AccentHover = Color.FromArgb(88, 156, 255);
        public static readonly Color AccentPressed = Color.FromArgb(40, 110, 230);
        public static readonly Color AccentSoft = Color.FromArgb(125, 184, 255);
        public static readonly Color Cyan = Color.FromArgb(90, 208, 250);

        public static readonly Color Danger = Color.FromArgb(255, 110, 100);
        public static readonly Color DangerHover = Color.FromArgb(48, 26, 29);
        public static readonly Color DangerPressed = Color.FromArgb(64, 31, 35);
        public static readonly Color Success = Color.FromArgb(76, 217, 140);

        // Chart palette: cool ramp, then a neutral for "Unassigned".
        public static readonly Color[] ChartColors =
        {
            Color.FromArgb(58, 134, 255), Color.FromArgb(90, 208, 250), Color.FromArgb(124, 122, 255),
            Color.FromArgb(72, 214, 190), Color.FromArgb(160, 190, 255), Color.FromArgb(140, 150, 175)
        };

        // ---- Type scale --------------------------------------------------------
        private static Font Make(string preferred, string fallback, float size, FontStyle style = FontStyle.Regular)
        {
            using var probe = new System.Drawing.Text.InstalledFontCollection();
            string family = probe.Families.Any(f => f.Name == preferred) ? preferred : fallback;
            return new Font(family, size, style);
        }

        public static readonly Font Body = new("Segoe UI", 10F);
        public static readonly Font Small = new("Segoe UI", 9F);
        public static readonly Font Caption = new("Segoe UI Semibold", 8.5F);
        public static readonly Font H1 = Make("Segoe UI Semibold", "Segoe UI", 22F);
        public static readonly Font H2 = new("Segoe UI Semibold", 12F);
        public static readonly Font H3 = new("Segoe UI Semibold", 10.5F);
        public static readonly Font StatName = new("Segoe UI Semibold", 15F);
        public static readonly Font Stat = Make("Segoe UI Light", "Segoe UI", 28F);
        public static readonly Font Hero = Make("Segoe UI Light", "Segoe UI", 30F);

        public static Color WithAlpha(this Color c, int alpha) => Color.FromArgb(Math.Clamp(alpha, 0, 255), c);

        // ---- Windows integration (dark title bar, dark scrollbars) -------------

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        private static int ColorRef(Color c) => c.R | (c.G << 8) | (c.B << 16);

        private static void SetDwm(IntPtr handle, int attribute, int value)
        {
            try { DwmSetWindowAttribute(handle, attribute, ref value, sizeof(int)); }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException) { }
        }

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string? subAppName, string? subIdList);

        /// <summary>Gives a control's scrollbars the dark Windows look (skipped silently if unsupported).</summary>
        public static void ApplyDarkScrollbars(IntPtr handle)
        {
            try { SetWindowTheme(handle, "DarkMode_Explorer", null); }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException) { }
        }

        public static void ApplyDarkTitleBar(IntPtr handle)
        {
            SetDwm(handle, 20, 1);                      // dark mode
            SetDwm(handle, 35, ColorRef(BackdropTop));  // caption color (blends into the backdrop)
            SetDwm(handle, 36, ColorRef(Text));         // caption text
            SetDwm(handle, 34, ColorRef(Border));       // window border
            SetDwm(handle, 33, 2);                      // rounded window corners (Windows 11)
        }
    }
}
