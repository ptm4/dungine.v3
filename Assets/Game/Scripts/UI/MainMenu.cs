using System.Collections.Generic;
using Dungine.Visual;
using Dungine.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dungine.UI
{
    /// <summary>The title screen UI.</summary>
    public static class MainMenuScreen
    {
        static VisualElement root;

        public static void Show(VisualElement parent)
        {
            Hide();
            root = UIB.El("layer", parent);
            var tw = UIB.El("menu-title-wrap", root); tw.pickingMode = PickingMode.Ignore;
            var t = UIB.Lbl("DUNGINE", "h1", tw); Theme.ApplyFont(t, true); t.style.fontSize = 110; t.style.letterSpacing = 22;
            var sub = UIB.Lbl("II  ·  THE CURSE OF STRAHD", "h2", tw); Theme.ApplyFont(sub, true); sub.style.letterSpacing = 10; sub.style.color = new Color(.72f, .18f, .16f); sub.style.unityTextAlign = TextAnchor.MiddleCenter;
            var tag = UIB.Lbl("A tale of Barovia, told in turns", "dim center", tw); tag.style.fontSize = 19; tag.style.marginTop = 6;
            var col = UIB.Col(root, "menu-buttons");
            bool hasSave = SaveSystem.Any();
            if (hasSave) UIB.Btn("CONTINUE", () => SaveSystem.LoadLatest(), "btn btn-menu", col);
            UIB.Btn("NEW GAME", () => Game.I.OpenCreation(), "btn btn-menu", col);
            var load = UIB.Btn("LOAD GAME", () => UIRoot.I.Win<SaveLoadWindow>("saveload").OpenMode(false), "btn btn-menu", col);
            load.SetEnabled(hasSave);
            UIB.Btn("OPTIONS", () => UIRoot.I.Open("options"), "btn btn-menu", col);
            UIB.Btn("CREDITS", () => ReadWindow.Show("Credits", Campaign.CreditsText), "btn btn-menu", col);
            UIB.Btn("QUIT", () => Application.Quit(), "btn btn-menu", col);
            foreach (var b in col.Children()) { b.style.unityTextAlign = TextAnchor.MiddleLeft; b.style.paddingLeft = 24; Theme.ApplyFont(b, true); }
            UIB.Lbl("A fan-made adventure for private play. Dungeons & Dragons, Baldur's Gate and Curse of Strahd belong to their respective owners.", "menu-footer", root);
        }

        public static void Hide() { root?.RemoveFromHierarchy(); root = null; }
    }

    /// <summary>The 3D backdrop behind the title screen.</summary>
    public static class MenuScene
    {
        static GameObject root;
        public static void Clear() { if (root) Object.Destroy(root); root = null; }

        public static void Build()
        {
            Clear();
            root = new GameObject("MenuScene");
            var atm = Atmosphere.BaroviaNight;
            atm.fogDensity = 0.018f; atm.exposure = 0.55f; atm.moonDir = new Vector3(0.15f, 0.32f, 1f);
            Atmosphere.Ensure().Apply(atm);
            var spec = new Nature.TerrainSpec
            {
                size = new Vector3(260, 80, 260), origin = new Vector3(-130, -10, -60),
                height = (x, z) =>
                {
                    float road = Mathf.Abs(x - Mathf.Sin(z * 0.03f) * 6f);
                    float h = Noise.Fbm((x + 500) / 260f, (z + 500) / 260f, 3, 4, 3) * 16f - 6f;
                    h += Mathf.Max(0, Mathf.Abs(x) - 20) * 0.25f;
                    h = Mathf.Lerp(h, 0f, Mathf.Clamp01(1 - road / 10f) * 0.8f);
                    return h + Mathf.Max(0, z - 90) * 0.4f;
                },
                splat = (x, z, h, slope) =>
                {
                    float road = Mathf.Abs(x - Mathf.Sin(z * 0.03f) * 6f);
                    float r = Mathf.Clamp01(1 - road / 3.5f);
                    return new[] { (1 - r) * 0.6f, r * 0.4f, r, slope > 30 ? 1 : 0, (1 - r) * 0.5f };
                }
            };
            var t = Nature.BuildTerrain(spec, root.transform);
            // castle on the horizon
            Buildings.CastleRavenloft(root.transform, new Vector3(30, 5, 260), 200, 1.1f);
            // dead trees and pines along the road
            var placed = new List<Vector3>();
            Nature.Scatter(t, root.transform, 70, 11, (x, z) => Mathf.Abs(x - Mathf.Sin(z * 0.03f) * 6f) > 6f ? 0.8f : 0f, v => Nature.Pine(v), "pine", 4, new Vector2(0.8f, 1.5f), Kit.ColliderKind.None, 4f, placed);
            Nature.Scatter(t, root.transform, 40, 12, (x, z) => Mathf.Abs(x - Mathf.Sin(z * 0.03f) * 6f) > 4.5f ? 0.8f : 0f, v => Nature.DeadTree(v + 100), "deadtree", 4, new Vector2(0.9f, 1.4f), Kit.ColliderKind.None, 3f, placed);
            Nature.Scatter(t, root.transform, 260, 13, (x, z) => 0.8f, v => Nature.GrassTuft(v), "grass", 3, new Vector2(0.8f, 1.5f), Kit.ColliderKind.None, 0f);
            // the gates
            Buildings.BaroviaGates(root.transform, new Vector3(Mathf.Sin(40 * 0.03f) * 6f, t.SampleHeight(new Vector3(0, 0, 40)) - 10f, 40), 0, out var l, out var r);
            l.localRotation = Quaternion.Euler(0, -35, 0); r.localRotation = Quaternion.Euler(0, 40, 0);
            var lamp = Kit.Place("lamppost", Props.LampPost, root.transform, new Vector3(4.5f, t.SampleHeight(new Vector3(4.5f, 0, 30)) - 10f, 30), 200);
            Kit.PointLight(root.transform, lamp.transform.position + new Vector3(0.45f, 2.9f, 0), new Color(1f, .7f, .4f), 4f, 12f, true);
            Kit.FogBank(root.transform, new Vector3(0, 0, 50), new Vector3(90, 3, 90), new Color(.55f, .6f, .72f, .22f), 110, 12f, 0.25f);
            Kit.FogBank(root.transform, new Vector3(0, 6, 120), new Vector3(200, 6, 90), new Color(.45f, .5f, .62f, .18f), 80, 26f, 0.4f);
            var cam = CameraRig.I.cam;
            var drift = root.AddComponent<MenuCameraDrift>();
            drift.cam = cam;
            drift.basePos = new Vector3(-3f, t.SampleHeight(new Vector3(-3, 0, 6)) - 10f + 2.2f, 6);
            drift.look = new Vector3(8, 18, 150);
            CameraRig.I.SetCinematic(drift.basePos, drift.look, 50f);
        }
    }

    public class MenuCameraDrift : MonoBehaviour
    {
        public Camera cam; public Vector3 basePos, look; float t;
        void Update()
        {
            t += Time.deltaTime;
            var p = basePos + new Vector3(Mathf.Sin(t * 0.05f) * 2.5f, Mathf.Sin(t * 0.07f) * 0.4f, Mathf.Sin(t * 0.03f) * 3f);
            CameraRig.I.SetCinematic(p, look + new Vector3(Mathf.Sin(t * 0.04f) * 6, 0, 0), 50f);
        }
    }
}
