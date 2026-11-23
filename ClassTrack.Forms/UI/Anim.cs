using System.Diagnostics;

namespace ClassTrack.UI
{
    /// <summary>
    /// One shared 15 ms clock drives every animation. Only runs while something is animating.
    /// Honours the Windows "Show animations" setting: with it off, values jump straight to the target.
    /// </summary>
    public static class Anim
    {
        private static readonly List<Tween> Active = new();
        private static readonly System.Windows.Forms.Timer Clock = new() { Interval = 15 };

        static Anim() => Clock.Tick += (_, _) => Tick();

        public static bool Enabled => SystemInformation.UIEffectsEnabled;

        /// <summary>Starts fast, settles gently: the right curve for things responding to the user.</summary>
        public static float EaseOut(float t) => 1f - (float)Math.Pow(1f - t, 3);

        internal static void Add(Tween t)
        {
            if (!Active.Contains(t)) Active.Add(t);
            if (!Clock.Enabled) Clock.Start();
        }

        private static void Tick()
        {
            for (int i = Active.Count - 1; i >= 0; i--)
                if (!Active[i].Step()) Active.RemoveAt(i);
            if (Active.Count == 0) Clock.Stop();
        }
    }

    /// <summary>An animated number (0..1 by default). Retargetable mid-flight: it continues from where it is.</summary>
    public sealed class Tween
    {
        private readonly Action onChange;
        private float from, to;
        private long startTicks;
        private int durationMs;

        public float Value { get; private set; }

        public Tween(Action onChange, float initial = 0f)
        {
            this.onChange = onChange;
            Value = to = from = initial;
        }

        /// <summary>Jumps to a value with no animation.</summary>
        public void Set(float v) { Value = to = from = v; }

        public void To(float target, int ms = 140)
        {
            if (!Anim.Enabled || ms <= 0)
            {
                Value = to = from = target;
                onChange();
                return;
            }
            from = Value;
            to = target;
            startTicks = Stopwatch.GetTimestamp();
            durationMs = ms;
            Anim.Add(this);
        }

        internal bool Step()
        {
            double elapsed = (Stopwatch.GetTimestamp() - startTicks) * 1000.0 / Stopwatch.Frequency;
            float t = (float)Math.Min(1.0, elapsed / durationMs);
            Value = t >= 1f ? to : from + (to - from) * Anim.EaseOut(t);
            onChange();
            return t < 1f;
        }
    }
}
