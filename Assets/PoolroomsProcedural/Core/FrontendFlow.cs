using System;
using System.Globalization;

namespace Poolrooms.Core
{
    public enum FrontendPage { Home,Journey,Atlas,Options,Appearance,Credits }
    public enum FrontendPhase { Menu,Loading,Exploring,Suspended,Failed }
    public enum JourneyStart { Origin,Environment,SunlitPoolrooms,Mall,Meadows }

    public sealed class FrontendFlow
    {
        public FrontendPage Page { get; private set; }
        public FrontendPhase Phase { get; private set; }
        public int Ticket { get; private set; }
        public bool HasSession { get; private set; }
        public string Error { get; private set; } = "";
        public bool CanResume { get { return HasSession && Phase == FrontendPhase.Suspended; } }
        public bool Visible { get { return Phase != FrontendPhase.Exploring; } }

        public bool Navigate(FrontendPage page)
        {
            if (!Enum.IsDefined(typeof(FrontendPage), page) || Phase == FrontendPhase.Loading || !Visible) return false;
            Page = page;
            return true;
        }

        public int Begin()
        {
            if (Phase == FrontendPhase.Loading) return -1;
            Ticket++;
            Phase = FrontendPhase.Loading;
            HasSession = false;
            Error = "";
            return Ticket;
        }

        public bool IsCurrent(int ticket) { return ticket == Ticket && Phase == FrontendPhase.Loading; }

        public bool Complete(int ticket)
        {
            if (!IsCurrent(ticket)) return false;
            Phase = FrontendPhase.Exploring;
            HasSession = true;
            return true;
        }

        public bool Fail(int ticket, string error)
        {
            if (!IsCurrent(ticket)) return false;
            Ticket++;
            Phase = FrontendPhase.Failed;
            HasSession = false;
            Error = string.IsNullOrWhiteSpace(error) ? "No se pudo preparar el recorrido." : error;
            Page = FrontendPage.Home;
            return true;
        }

        public bool Suspend()
        {
            if (Phase != FrontendPhase.Exploring || !HasSession) return false;
            Phase = FrontendPhase.Suspended;
            Page = FrontendPage.Home;
            return true;
        }

        public bool Resume()
        {
            if (!CanResume) return false;
            Phase = FrontendPhase.Exploring;
            return true;
        }

        public static bool TrySeed(string text, int fallback, out int seed)
        {
            seed = fallback;
            text = (text ?? "").Trim();
            if (text.Length > 64) return false;
            if (text.Length == 0) return true;
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out seed)) return true;
            uint hash = 2166136261;
            foreach (char value in text) hash = unchecked((hash ^ value) * 16777619);
            seed = unchecked((int)hash);
            return true;
        }

        public static float Fade(float elapsed, bool reducedMotion)
        {
            if (reducedMotion) return 1;
            float t = Math.Max(0, Math.Min(1, elapsed / .42f));
            return t * t * (3 - 2 * t);
        }
    }
}
