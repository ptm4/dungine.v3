using UnityEngine;

using UnityEngine.UIElements;

namespace Dungine.UI
{
    /// <summary>Colours and fonts shared by every screen.</summary>
    public static class Theme
    {
        public static readonly Color Gold = new Color(0.86f, 0.71f, 0.42f);
        public static readonly Color GoldDim = new Color(0.55f, 0.45f, 0.27f);
        public static readonly Color Parchment = new Color(0.9f, 0.86f, 0.78f);
        public static readonly Color Text = new Color(0.88f, 0.85f, 0.8f);
        public static readonly Color TextDim = new Color(0.62f, 0.59f, 0.55f);
        public static readonly Color Panel = new Color(0.055f, 0.045f, 0.045f, 0.92f);
        public static readonly Color PanelLight = new Color(0.1f, 0.085f, 0.08f, 0.94f);
        public static readonly Color Blood = new Color(0.62f, 0.12f, 0.12f);
        public static readonly Color Hostile = new Color(0.9f, 0.25f, 0.22f);
        public static readonly Color Friendly = new Color(0.35f, 0.8f, 0.45f);
        public static readonly Color Neutral = new Color(0.85f, 0.8f, 0.45f);
        public static readonly Color Magic = new Color(0.55f, 0.6f, 1f);
        public static readonly Color ActionGreen = new Color(0.35f, 0.8f, 0.35f);
        public static readonly Color BonusOrange = new Color(0.95f, 0.6f, 0.2f);
        public static readonly Color MoveYellow = new Color(0.95f, 0.85f, 0.35f);
        public static readonly Color Success = new Color(0.4f, 0.85f, 0.45f);
        public static readonly Color Failure = new Color(0.9f, 0.3f, 0.25f);

        static Font body, title;
        static bool tried;

        // Fonts come from the player's own Windows install; nothing is shipped with the game.
        // A runtime FontAsset built from a file path has no source Font and breaks every label in
        // the panel, so these are dynamic OS fonts, with Segoe UI Symbol as the glyph fallback.
        static void Load()
        {
            if (tried && body) return;   // (body can be destroyed if statics outlive a play session)
            tried = true;
            var names = new System.Collections.Generic.HashSet<string>(Font.GetOSInstalledFontNames());
            string Pick(params string[] c) { foreach (var n in c) if (names.Contains(n)) return n; return null; }
            string b = Pick("Palatino Linotype", "Georgia", "Constantia", "Cambria", "Times New Roman");
            string sym = Pick("Segoe UI Symbol");
            Font Make(string n) => n == null ? null : Font.CreateDynamicFontFromOSFont(sym != null ? new[] { n, sym } : new[] { n }, 48);
            body = Make(b);
            title = body;
        }

        public static Font Body { get { Load(); return body; } }
        public static Font Title { get { Load(); return title; } }

        public static void ApplyFont(VisualElement e, bool titleFont = false)
        {
            var f = titleFont ? Title : Body;
            if (f != null) e.style.unityFontDefinition = FontDefinition.FromFont(f);
        }

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
    }
}
