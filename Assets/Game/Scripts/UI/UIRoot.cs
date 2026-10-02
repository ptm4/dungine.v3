using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dungine.Combat;
using Dungine.Rules;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Dungine.UI
{
    /// <summary>Small fluent helpers for building UI Toolkit trees in code.</summary>
    public static class UIB
    {
        public static VisualElement El(string cls = null, VisualElement parent = null)
        {
            var e = new VisualElement();
            if (!string.IsNullOrEmpty(cls)) foreach (var c in cls.Split(' ')) e.AddToClassList(c);
            parent?.Add(e);
            return e;
        }
        public static Label Lbl(string text, string cls = null, VisualElement parent = null)
        {
            var l = new Label(text) { enableRichText = true };
            if (!string.IsNullOrEmpty(cls)) foreach (var c in cls.Split(' ')) l.AddToClassList(c);
            parent?.Add(l);
            return l;
        }
        public static Button Btn(string text, Action onClick, string cls = "btn", VisualElement parent = null)
        {
            var b = new Button(() => { Audio.Sfx.Play("click"); onClick?.Invoke(); }) { text = text };
            b.enableRichText = true;
            foreach (var c in (cls ?? "btn").Split(' ')) b.AddToClassList(c);
            b.RegisterCallback<MouseEnterEvent>(_ => Audio.Sfx.Play("hover"));
            parent?.Add(b);
            return b;
        }
        public static VisualElement Row(VisualElement parent = null, string cls = null) => El("row" + (cls != null ? " " + cls : ""), parent);
        public static VisualElement Col(VisualElement parent = null, string cls = null) => El("col" + (cls != null ? " " + cls : ""), parent);
        public static VisualElement Img(Texture tex, string cls = null, VisualElement parent = null, Color? tint = null)
        {
            var e = El(cls, parent);
            if (tex is Texture2D t2) e.style.backgroundImage = new StyleBackground(t2);
            else if (tex is RenderTexture rt) e.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt));
            if (tint.HasValue) e.style.unityBackgroundImageTintColor = tint.Value;
            return e;
        }
        public static ScrollView Scroll(VisualElement parent = null, string cls = "scroll")
        {
            var s = new ScrollView(ScrollViewMode.Vertical);
            if (cls != null) foreach (var c in cls.Split(' ')) s.AddToClassList(c);
            s.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            parent?.Add(s);
            return s;
        }
        public static void Pos(VisualElement e, float? left = null, float? top = null, float? right = null, float? bottom = null)
        {
            e.style.position = Position.Absolute;
            if (left.HasValue) e.style.left = left.Value;
            if (top.HasValue) e.style.top = top.Value;
            if (right.HasValue) e.style.right = right.Value;
            if (bottom.HasValue) e.style.bottom = bottom.Value;
        }
        public static void Size(VisualElement e, float w, float h) { e.style.width = w; e.style.height = h; }
        public static void Bg(VisualElement e, Color c) => e.style.backgroundColor = c;
        public static void Border(VisualElement e, Color c, float w = 1) { e.style.borderLeftColor = e.style.borderRightColor = e.style.borderTopColor = e.style.borderBottomColor = c; e.style.borderLeftWidth = e.style.borderRightWidth = e.style.borderTopWidth = e.style.borderBottomWidth = w; }
        public static VisualElement IconCircle(string icon, Color bg, float size, VisualElement parent = null)
        {
            var e = El(null, parent);
            Size(e, size, size);
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = size * 0.22f;
            e.style.backgroundColor = new Color(bg.r * 0.35f, bg.g * 0.35f, bg.b * 0.35f, 1f);
            Border(e, bg * 0.9f, 1);
            var ic = Img(Icons.Get(icon), null, e, Color.Lerp(bg, Color.white, 0.55f));
            ic.style.position = Position.Absolute; ic.style.left = ic.style.top = ic.style.right = ic.style.bottom = size * 0.12f;
            return e;
        }
    }

    /// <summary>Root of all UI: layers, overlays, hover labels, toasts, window management.</summary>
    public class UIRoot : MonoBehaviour
    {
        public static UIRoot I;
        UIDocument doc;
        public VisualElement root, hudLayer, winLayer, modalLayer, overlayLayer, floatLayer, tipLayer;
        Label hoverLabel, cursorText, targetingHint, banner;
        VisualElement toastCol;
        VisualElement fade; Label fadeTitle, fadeSub, narrationLbl;
        public HUD hud;
        readonly Dictionary<string, Window> windows = new Dictionary<string, Window>();
        public bool showLabels;
        readonly List<(Label l, Interactable it)> worldLabels = new List<(Label, Interactable)>();
        string lastHover;

        public static bool PointerOverUI { get; private set; }
        public static bool ShiftHeld => Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
        public bool TextFieldFocused => root?.panel?.focusController?.focusedElement is TextField;
        public bool ModalOpen => modalLayer != null && modalLayer.childCount > 0 || windows.Values.Any(w => w.IsOpen && w.blocksWorld);

        void Awake()
        {
            I = this;
            doc = gameObject.AddComponent<UIDocument>();
            doc.panelSettings = Resources.Load<PanelSettings>("UI/PanelSettings");
            if (doc.panelSettings == null) { doc.panelSettings = ScriptableObject.CreateInstance<PanelSettings>(); doc.panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize; doc.panelSettings.referenceResolution = new Vector2Int(1920, 1080); }
        }

        void Start() => Build();

        void Build()
        {
            root = doc.rootVisualElement;
            root.Clear();
            var ss = Resources.Load<StyleSheet>("UI/Dungine");
            if (ss) root.styleSheets.Add(ss);
            root.AddToClassList("root");
            root.pickingMode = PickingMode.Ignore;
            Theme.ApplyFont(root);
            hudLayer = Layer("hud"); winLayer = Layer("windows"); floatLayer = Layer("float"); modalLayer = Layer("modal"); tipLayer = Layer("tips"); overlayLayer = Layer("overlay");
            hoverLabel = UIB.Lbl("", "hover-label", tipLayer); hoverLabel.pickingMode = PickingMode.Ignore; hoverLabel.style.display = DisplayStyle.None;
            cursorText = UIB.Lbl("", "cursor-text", tipLayer); cursorText.pickingMode = PickingMode.Ignore;
            targetingHint = UIB.Lbl("", "hover-label", tipLayer); targetingHint.pickingMode = PickingMode.Ignore; targetingHint.style.display = DisplayStyle.None;
            UIB.Pos(targetingHint, null, null, null, 190); targetingHint.style.alignSelf = Align.Center;
            toastCol = UIB.El("toast-col", tipLayer); toastCol.pickingMode = PickingMode.Ignore;
            banner = UIB.Lbl("", "banner", tipLayer); banner.pickingMode = PickingMode.Ignore; banner.style.opacity = 0;
            fade = UIB.El("fade", overlayLayer);
            fadeTitle = UIB.Lbl("", "fade-title", fade); Theme.ApplyFont(fadeTitle, true);
            fadeSub = UIB.Lbl("", "fade-sub", fade);
            narrationLbl = UIB.Lbl("", "narration", fade);
            fade.style.opacity = 1; fade.pickingMode = PickingMode.Ignore;
            PortraitRenderer.Ensure();
            hud = new HUD(hudLayer);
            hud.Hide();
            Register(new InventoryWindow()); Register(new CharacterWindow()); Register(new JournalWindow());
            Register(new PauseWindow()); Register(new OptionsWindow()); Register(new SaveLoadWindow());
            Register(new LootWindow()); Register(new ReadWindow()); Register(new TradeWindow()); Register(new RestWindow()); Register(new LevelUpWindow());
        }

        VisualElement Layer(string n)
        {
            var e = UIB.El("layer", root); e.name = n; e.pickingMode = PickingMode.Ignore; return e;
        }

        void Register(Window w) { w.Build(winLayer); windows[w.Id] = w; }
        public T Win<T>(string id) where T : Window => windows.TryGetValue(id, out var w) ? (T)w : null;
        public void Toggle(string id) { if (windows.TryGetValue(id, out var w)) { if (w.IsOpen) w.Close(); else w.Open(); } }
        public void Open(string id) { if (windows.TryGetValue(id, out var w)) w.Open(); }
        public void CloseAll() { foreach (var w in windows.Values) if (w.IsOpen) w.Close(); }

        public void OnEscape()
        {
            if (Targeting.I != null && Targeting.I.Active) { Targeting.I.Cancel(); return; }
            if (modalLayer.childCount > 0) return;
            var open = windows.Values.Where(w => w.IsOpen).ToList();
            if (open.Count > 0) { foreach (var w in open) w.Close(); return; }
            var g = Game.I;
            if (g.mode == GameMode.Explore || g.mode == GameMode.Combat) Open("pause");
            else if (g.mode == GameMode.Creation) { }
        }

        void Update()
        {
            if (root == null || root.panel == null) return;
            var m = Mouse.current;
            if (m != null)
            {
                Vector2 sp = m.position.ReadValue();
                sp.y = Screen.height - sp.y;
                var pp = RuntimePanelUtils.ScreenToPanel(root.panel, sp);
                var picked = root.panel.Pick(pp);
                PointerOverUI = picked != null && picked != root && picked.pickingMode != PickingMode.Ignore;
                float w = root.layout.width, h = root.layout.height;
                if (hoverLabel.style.display == DisplayStyle.Flex)
                {
                    float lw = Mathf.Max(hoverLabel.layout.width, 60), lh = Mathf.Max(hoverLabel.layout.height, 20);
                    hoverLabel.style.left = Mathf.Clamp(pp.x + 22, 0, w - lw);
                    hoverLabel.style.top = Mathf.Clamp(pp.y + 20, 0, h - lh);
                }
                cursorText.style.left = pp.x + 18; cursorText.style.top = pp.y - 28;
            }
            UpdateWorldLabels();
            hud?.Tick();
            foreach (var win in windows.Values) if (win.IsOpen) win.Tick();
        }

        // ---------------------------------------------------------------- hover / hints
        public void SetHoverLabel(string text, Color c)
        {
            if (string.IsNullOrEmpty(text)) { hoverLabel.style.display = DisplayStyle.None; lastHover = null; return; }
            hoverLabel.style.display = DisplayStyle.Flex;
            if (text != lastHover) { hoverLabel.text = text; lastHover = text; }
            hoverLabel.style.color = c;
            UIB.Border(hoverLabel, c * 0.7f, 1);
        }
        public void SetCursorText(string t) { cursorText.text = t ?? ""; }
        public void SetCursor(string kind) { }
        public void SetTargetingHint(string t) { targetingHint.style.display = string.IsNullOrEmpty(t) ? DisplayStyle.None : DisplayStyle.Flex; targetingHint.text = t ?? ""; }

        public void SetShowLabels(bool on) { showLabels = on; if (!on) { foreach (var (l, _) in worldLabels) l.RemoveFromHierarchy(); worldLabels.Clear(); } }

        void UpdateWorldLabels()
        {
            if (!showLabels || Game.I?.area == null) return;
            var cam = CameraRig.I?.cam; if (!cam) return;
            if (worldLabels.Count == 0)
            {
                foreach (var it in Game.I.area.interactables.Where(i => i && i.Available))
                {
                    var l = UIB.Lbl(it.label, "hover-label small", floatLayer); l.pickingMode = PickingMode.Ignore;
                    worldLabels.Add((l, it));
                }
                foreach (var a in Game.I.area.npcs.Where(a => a && !a.c.Active && a.lootable && !a.looted))
                {
                    var l = UIB.Lbl(a.Name + " (search)", "hover-label small", floatLayer); l.pickingMode = PickingMode.Ignore;
                    worldLabels.Add((l, null));
                    l.userData = a;
                }
            }
            foreach (var (l, it) in worldLabels)
            {
                Vector3 wp = it ? it.transform.position + Vector3.up * 1.2f : ((Actor)l.userData).transform.position + Vector3.up * 0.6f;
                var sp = cam.WorldToScreenPoint(wp);
                if (sp.z < 0 || Vector3.Distance(cam.transform.position, wp) > 35) { l.style.display = DisplayStyle.None; continue; }
                l.style.display = DisplayStyle.Flex;
                var pp = RuntimePanelUtils.CameraTransformWorldToPanel(root.panel, wp, cam);
                l.style.left = pp.x - 40; l.style.top = pp.y;
            }
        }

        public void Bark(Actor a, string text)
        {
            if (a == null) return;
            FloatingText.Bark(a, text);
        }

        public void ShowContext(Actor a)
        {
            if (a == null || a.c == null) return;
            var d = a.c.mdef;
            string t = $"<b>{a.Name}</b>";
            if (d != null && !a.IsPC && a.c.faction == Faction.Hostile) t += $"\n{a.c.type} · AC {a.c.AC} · {a.c.hp}/{a.c.MaxHPTotal} HP\n<size=85%>{d.desc}</size>";
            Toast.Show(t, Theme.Parchment, 4f);
        }

        // ---------------------------------------------------------------- banners
        public void ShowCombatBanner(string text) => StartCoroutine(BannerRoutine(text, text == "Victory" ? Theme.Gold : Theme.Hostile));
        IEnumerator BannerRoutine(string text, Color c)
        {
            banner.text = text.ToUpperInvariant(); banner.style.color = c;
            banner.style.opacity = 1; banner.style.scale = new Scale(new Vector3(1.15f, 1.15f, 1));
            yield return new WaitForSeconds(0.05f);
            banner.style.scale = new Scale(Vector3.one);
            yield return new WaitForSeconds(1.2f);
            banner.style.opacity = 0;
        }

        public void ShowYourTurn(Creature c) { hud?.OnTurn(c); }

        // ---------------------------------------------------------------- fades & narration
        public IEnumerator FadeOut(float dur, string title = null, string sub = null)
        {
            fade.pickingMode = PickingMode.Position;
            fadeTitle.text = title ?? ""; fadeSub.text = sub ?? ""; narrationLbl.text = "";
            fadeTitle.style.opacity = 0; fadeSub.style.opacity = 0;
            float t = 0; float start = fade.resolvedStyle.opacity;
            while (t < dur) { t += Time.unscaledDeltaTime; fade.style.opacity = Mathf.Lerp(start, 1, t / dur); yield return null; }
            fade.style.opacity = 1;
            if (!string.IsNullOrEmpty(title))
            {
                t = 0;
                while (t < 0.5f) { t += Time.unscaledDeltaTime; fadeTitle.style.opacity = t / 0.5f; fadeSub.style.opacity = t / 0.5f; yield return null; }
            }
        }

        public IEnumerator FadeIn(float dur)
        {
            float t = 0;
            while (t < dur) { t += Time.unscaledDeltaTime; fade.style.opacity = 1 - t / dur; yield return null; }
            fade.style.opacity = 0;
            fade.pickingMode = PickingMode.Ignore;
            fadeTitle.text = ""; fadeSub.text = "";
        }

        /// <summary>End-of-chapter card over the black overlay: the party, their levels, and a few numbers. Click to continue.</summary>
        public IEnumerator ChapterSummary()
        {
            var g = Game.I;
            fadeTitle.text = ""; narrationLbl.text = ""; fadeSub.text = "";
            var card = UIB.Col(fade, "chapter-card");
            card.style.position = Position.Absolute; card.style.left = new Length(50, LengthUnit.Percent); card.style.top = new Length(50, LengthUnit.Percent);
            card.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
            card.style.width = 980; card.style.alignItems = Align.Center;
            var t = UIB.Lbl("CHAPTER ONE · THE VILLAGE OF BAROVIA", "h2 center", card); Theme.ApplyFont(t, true); t.style.letterSpacing = 6;
            UIB.Lbl("Complete", "dim center", card).style.marginBottom = 24;
            var row = UIB.Row(card); row.style.justifyContent = Justify.Center;
            foreach (var c in g.party)
            {
                var col = UIB.Col(row, "card"); col.style.width = 280; col.style.marginLeft = 10; col.style.marginRight = 10; col.style.alignItems = Align.Center;
                var img = UIB.El("portrait-big", col); img.style.width = 160; img.style.height = 200;
                var tex = PortraitRenderer.I?.Get(c); if (tex) img.style.backgroundImage = Background.FromRenderTexture(tex);
                var n = UIB.Lbl(c.name, "gold center", col); n.style.fontSize = 24; n.style.marginTop = 8;
                UIB.Lbl($"{c.sheet.Race.name} {c.sheet.Class.name}", "small center", col);
                UIB.Lbl($"Level {c.sheet.level}" + (c.dead ? " · <color=#ff5050>fallen</color>" : ""), "small dim center", col);
            }
            var stats = UIB.Col(card); stats.style.marginTop = 26; stats.style.alignItems = Align.Center;
            int mins = Mathf.RoundToInt(g.playTime / 60f);
            UIB.Lbl($"Time in Barovia: {mins / 60}h {mins % 60:00}m   ·   Foes laid to rest: {g.GetFlag("stat_kills")}   ·   Gold: {g.gold}", "center", stats);
            int done = g.journal.quests.Count(q => q.state == QuestState.Done);
            UIB.Lbl($"Quests completed: {done}   ·   Secrets and lore found: {g.journal.lore.Count}", "center dim", stats);
            var hint = UIB.Lbl("click to continue", "dim center", card); hint.style.marginTop = 30; hint.style.fontSize = 14;
            card.style.opacity = 0;
            float tt = 0;
            while (tt < 1f) { tt += Time.unscaledDeltaTime; card.style.opacity = tt; yield return null; }
            yield return new WaitForSecondsRealtime(0.5f);
            if (Dialogue.DialogueRunner.DevFast) yield return new WaitForSecondsRealtime(3f);
            else while (!AnyPress()) yield return null;
            card.RemoveFromHierarchy();
        }

        public void SetFadeImmediate(float a) { fade.style.opacity = a; fade.pickingMode = a > 0.5f ? PickingMode.Position : PickingMode.Ignore; }

        /// <summary>Full-screen narration, advanced by click or key.</summary>
        public IEnumerator Narration(string[] pages, string title)
        {
            fade.pickingMode = PickingMode.Position;
            float t = 0;
            while (t < 0.6f) { t += Time.unscaledDeltaTime; fade.style.opacity = Mathf.Max(fade.resolvedStyle.opacity, t / 0.6f); yield return null; }
            fadeTitle.text = title ?? ""; fadeTitle.style.opacity = 1; fadeSub.text = "";
            foreach (var p in pages)
            {
                narrationLbl.text = "";
                narrationLbl.style.opacity = 1;
                // typewriter
                float cps = 60f * Settings.TextSpeed;
                float shown = 0; bool skip = false;
                while (shown < p.Length)
                {
                    shown += Time.unscaledDeltaTime * cps;
                    narrationLbl.text = p.Substring(0, Mathf.Min(p.Length, (int)shown));
                    if (AnyPress() || Dialogue.DialogueRunner.DevFast) { skip = true; break; }
                    yield return null;
                }
                narrationLbl.text = p;
                if (skip) yield return null;
                fadeSub.text = "<color=#7a7066>click to continue</color>";
                if (Dialogue.DialogueRunner.DevFast) yield return new WaitForSecondsRealtime(0.6f);
                else while (!AnyPress()) yield return null;
                fadeSub.text = "";
                Audio.Sfx.Play("page");
                yield return null;
            }
            narrationLbl.text = ""; fadeTitle.text = "";
        }

        static bool AnyPress()
        {
            var m = Mouse.current; var k = Keyboard.current;
            return (m != null && m.leftButton.wasPressedThisFrame) || (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame));
        }

        // ---------------------------------------------------------------- screens
        public void ShowMainMenu()
        {
            CloseAll(); hud.Hide(); modalLayer.Clear();
            MainMenuScreen.Show(winLayer);
            StartCoroutine(FadeIn(1.2f));
        }

        public void ShowCreation()
        {
            CloseAll(); hud.Hide();
            MainMenuScreen.Hide();
            CreationScreen.Show(winLayer);
        }

        public void ShowHUD()
        {
            MainMenuScreen.Hide(); CreationScreen.Hide();
            hud.Show();
        }

        public void ShowGameOver()
        {
            hud.Hide();
            var m = UIB.El("dice-modal", modalLayer);
            var p = UIB.El("dice-panel", m); p.style.width = 640;
            var t = UIB.Lbl("THE MISTS CLAIM YOU", "h2 center", p); t.style.color = Theme.Blood; Theme.ApplyFont(t, true);
            UIB.Lbl("Your journey in Barovia ends here. Somewhere above the village, in a castle of black stone, someone raises a glass.", "center", p).style.marginTop = 12;
            var row = UIB.Row(p); row.style.marginTop = 18;
            UIB.Btn("Load Last Save", () => { modalLayer.Clear(); SaveSystem.LoadLatest(); }, "btn btn-primary", row);
            UIB.Btn("Main Menu", () => { modalLayer.Clear(); Game.I.GoToMainMenu(); }, "btn", row);
        }

        public void HotbarKey(int k) => hud?.HotbarKey(k);

        public Coroutine Run(IEnumerator r) => StartCoroutine(r);
    }

    /// <summary>Messages that appear briefly at the top of the screen.</summary>
    public static class Toast
    {
        public static void Show(string text, Color c, float dur = 2.6f)
        {
            if (UIRoot.I == null || UIRoot.I.root == null) { Debug.Log("[Toast] " + text); return; }
            var col = UIRoot.I.root.Q(className: "toast-col");
            if (col == null) return;
            var l = UIB.Lbl(text, "toast", col);
            l.pickingMode = PickingMode.Ignore;
            l.style.color = c;
            UIB.Border(l, c * 0.6f, 1);
            if (col.childCount > 5) col.RemoveAt(0);
            UIRoot.I.StartCoroutine(Fade(l, dur));
        }

        static IEnumerator Fade(Label l, float dur)
        {
            yield return new WaitForSecondsRealtime(dur);
            l.style.opacity = 0;
            yield return new WaitForSecondsRealtime(0.5f);
            l.RemoveFromHierarchy();
        }
    }

    /// <summary>Damage numbers, status words and speech barks that float in world space.</summary>
    public class FloatingText
    {
        public static void Show(Vector3 world, string text, Color c, float scale = 1f)
        {
            if (UIRoot.I?.floatLayer == null) return;
            var l = UIB.Lbl(text, "float-text", UIRoot.I.floatLayer);
            l.pickingMode = PickingMode.Ignore;
            l.style.color = c;
            l.style.fontSize = 26 * scale;
            Theme.ApplyFont(l, true);
            UIRoot.I.StartCoroutine(Anim(l, world + UnityEngine.Random.insideUnitSphere * 0.15f, 1.4f, 0.9f));
        }

        public static void Bark(Actor a, string text)
        {
            if (UIRoot.I?.floatLayer == null) return;
            var l = UIB.Lbl(text, "hover-label", UIRoot.I.floatLayer);
            l.pickingMode = PickingMode.Ignore;
            l.style.color = Theme.Parchment; l.style.fontSize = 17; l.style.maxWidth = 360;
            UIRoot.I.StartCoroutine(Follow(l, a, 3.5f));
        }

        static IEnumerator Anim(Label l, Vector3 world, float dur, float rise)
        {
            float t = 0;
            var cam = CameraRig.I?.cam;
            while (t < dur && cam)
            {
                t += Time.deltaTime;
                Vector3 p = world + Vector3.up * rise * Mathf.Sqrt(t / dur);
                var sp = cam.WorldToScreenPoint(p);
                if (sp.z > 0)
                {
                    var pp = RuntimePanelUtils.CameraTransformWorldToPanel(UIRoot.I.root.panel, p, cam);
                    l.style.left = pp.x; l.style.top = pp.y;
                    l.style.display = DisplayStyle.Flex;
                }
                else l.style.display = DisplayStyle.None;
                l.style.opacity = t < dur * 0.6f ? 1 : 1 - (t - dur * 0.6f) / (dur * 0.4f);
                float s = t < 0.12f ? 1.3f - t * 2.5f : 1f;
                l.style.scale = new Scale(new Vector3(s, s, 1));
                yield return null;
            }
            l.RemoveFromHierarchy();
        }

        static IEnumerator Follow(Label l, Actor a, float dur)
        {
            float t = 0; var cam = CameraRig.I?.cam;
            while (t < dur && a && cam)
            {
                t += Time.deltaTime;
                Vector3 p = a.HeadPos + Vector3.up * 0.45f;
                var pp = RuntimePanelUtils.CameraTransformWorldToPanel(UIRoot.I.root.panel, p, cam);
                l.style.left = pp.x - l.layout.width * 0.5f; l.style.top = pp.y - l.layout.height;
                l.style.opacity = t > dur - 0.5f ? (dur - t) / 0.5f : 1;
                yield return null;
            }
            l.RemoveFromHierarchy();
        }
    }

    /// <summary>The scrolling combat & event log.</summary>
    public static class CombatLog
    {
        public static readonly List<string> lines = new List<string>();
        public static event Action<string> OnLine;
        public static void Add(string s)
        {
            lines.Add(s);
            if (lines.Count > 200) lines.RemoveAt(0);
            OnLine?.Invoke(s);
        }
    }
}
