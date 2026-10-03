using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dungine.Visual;
using Dungine.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dungine.LookTest
{
    /// <summary>
    /// Look test (phase 3, step 1): places library voxel models (baked from their GLBs by LookTestBake) into v2's areas
    /// as static models, and gives the dev tools hooks to compare looks and measure a crowd. It touches none of v2's code:
    /// it watches for an area being built and adds its models under that area's root, so they go when the area goes.
    ///   - the road where a new game starts: a small group by the party, so the library art is the first thing seen;
    ///   - the village square: the figures in a row facing the party, with props beside them.
    /// Dev hooks (call from tools/ev.sh): Pixelate(scale), Native(), Crowd(n), V2Crowd(n), ClearCrowd(), Measure(frames),
    /// Report, View(yaw, zoom).
    /// </summary>
    public class LookTest : MonoBehaviour
    {
        public static LookTest I;
        /// <summary>Turn the library models off (v2's look only).</summary>
        public static bool Enabled = true;
        public static string Report = "";

        static readonly string[] Figures = { "fighter", "arik", "bildrath_cantemir", "parriwimple", "barovian_commoner_m", "barovian_commoner_f", "barovian_noble_m", "guard" };
        const string Folder = "LookTest/Baked/";

        Transform lastRoot;
        readonly List<GameObject> crowd = new List<GameObject>();
        float origScale = 1f; UpscalingFilterSelection origFilter; bool pixelated;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I) return;
            var go = new GameObject("LookTest");
            DontDestroyOnLoad(go);
            I = go.AddComponent<LookTest>();
        }

        void Update()
        {
            var g = Game.I;
            if (!Enabled || g == null || g.area == null || !g.area.root || g.mode == GameMode.Loading) return;
            if (g.area.root == lastRoot) return;
            lastRoot = g.area.root;
            crowd.Clear();
            if (g.areaId == "village") PlaceVillage(g.area);
            else if (g.areaId == "svalich_road") PlaceRoad(g.area);
        }

        void OnDisable() => Native();

        // ------------------------------------------------------------------ placing
        public static GameObject Place(AreaContext ctx, string id, Vector3 p, float yaw, Transform parent = null)
        {
            var prefab = Resources.Load<GameObject>(Folder + id);
            if (!prefab) { Debug.LogWarning("[LookTest] no baked model " + id); return null; }
            var go = Instantiate(prefab, parent ? parent : ctx.root);
            go.name = "Library_" + id;
            go.transform.position = new Vector3(p.x, ctx.GroundY(p.x, p.z, p.y), p.z);
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            return go;
        }

        static (Vector3 pos, float yaw) SpawnOf(AreaContext ctx, string id)
        {
            ctx.spawns.TryGetValue(id, out var p);
            ctx.spawnYaw.TryGetValue(id, out var y);
            return (p, y);
        }

        void PlaceVillage(AreaContext ctx)
        {
            var (p, yaw) = SpawnOf(ctx, "square");
            var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            var right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            var row = p + fwd * 5f + right * 1.2f;
            float face = yaw + 180f;
            for (int i = 0; i < Figures.Length; i++)
                Place(ctx, Figures[i], row + right * ((i - (Figures.Length - 1) * 0.5f) * 1.25f), face);
            Place(ctx, "barrel", row - right * 6.4f + fwd * 0.2f, yaw + 20);
            Place(ctx, "keg", row - right * 7.4f - fwd * 0.6f, yaw - 30);
            var crate = Place(ctx, "crate", row - right * 6.6f - fwd * 1.4f, yaw + 5);
            if (crate) Place(ctx, "hanging_lantern", crate.transform.position + Vector3.up * 0.45f, yaw).transform.position = crate.transform.position + Vector3.up * 0.45f;
            Place(ctx, "lamppost", row + right * 6.3f, face);
            Place(ctx, "campfire", row + right * 7.3f - fwd * 2.2f, yaw);
            Place(ctx, "horse_draft", row + fwd * 2.6f - right * 8.2f, yaw);
            Report = $"village: {Figures.Length} figures and 7 props placed 5 m ahead of the square spawn";
        }

        void PlaceRoad(AreaContext ctx)
        {
            var (p, yaw) = SpawnOf(ctx, "start");
            var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            var right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            var row = p + fwd * 4f;
            Place(ctx, "fighter", row - right * 1.3f, yaw + 180);
            Place(ctx, "barovian_commoner_m", row, yaw + 180);
            Place(ctx, "barovian_commoner_f", row + right * 1.3f, yaw + 180);
            Place(ctx, "lamppost", row + right * 3f, yaw + 180);
            Place(ctx, "barrel", row - right * 3f, yaw);
        }

        // ------------------------------------------------------------------ crowd
        /// <summary>n library figures standing in a grid in front of the party.</summary>
        public static string Crowd(int n)
        {
            var g = Game.I; var ctx = g.area; ClearCrowd();
            var (c, yaw) = I.Center();
            var rnd = new System.Random(7);
            for (int i = 0; i < n; i++)
            {
                var pos = c + I.Grid(i, n, yaw);
                var go = Place(ctx, Figures[i % Figures.Length], pos, yaw + 180 + rnd.Next(-40, 40));
                if (go) I.crowd.Add(go);
            }
            return $"library crowd: {I.crowd.Count}";
        }

        /// <summary>n of v2's own procedural humanoids, animated as in the game, in the same grid. They are built one per
        /// frame (each takes tens of milliseconds), so watch Report for "v2 crowd: n".</summary>
        public static string V2Crowd(int n)
        {
            ClearCrowd();
            I.StartCoroutine(I.V2CrowdRoutine(n));
            return "building v2 crowd";
        }

        IEnumerator V2CrowdRoutine(int n)
        {
            Report = "building";
            var ctx = Game.I.area;
            var (c, yaw) = Center();
            var rnd = new System.Random(7);
            var races = new[] { Rules.RaceId.Human, Rules.RaceId.Human, Rules.RaceId.Human, Rules.RaceId.Dwarf, Rules.RaceId.Elf, Rules.RaceId.HalfElf };
            for (int i = 0; i < n; i++)
            {
                var look = RaceLooks.Randomize(races[i % races.Length], Rules.SubraceId.None, i % 2, rnd);
                var rig = HumanoidBuilder.Build(look, new GearLook(), "v2_" + i);
                var anim = rig.gameObject.AddComponent<HumanoidAnimator>(); anim.Init(rig, MotionStyle.Normal);
                var pos = c + Grid(i, n, yaw);
                rig.transform.SetParent(ctx.root, true);
                rig.transform.position = new Vector3(pos.x, ctx.GroundY(pos.x, pos.z, pos.y), pos.z);
                rig.transform.rotation = Quaternion.Euler(0, yaw + 180 + rnd.Next(-40, 40), 0);
                crowd.Add(rig.gameObject);
                yield return null;
            }
            Report = $"v2 crowd: {crowd.Count}";
        }

        public static string ClearCrowd()
        {
            foreach (var go in I.crowd) if (go) Destroy(go);
            int k = I.crowd.Count; I.crowd.Clear();
            return "cleared " + k;
        }

        (Vector3, float) Center()
        {
            var sel = Game.I.Selected;
            float yaw = CameraRig.I ? CameraRig.I.yaw : 0;
            var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            return ((sel ? sel.transform.position : Vector3.zero) + fwd * 9f, yaw);
        }

        Vector3 Grid(int i, int n, float yaw)
        {
            int cols = Mathf.CeilToInt(Mathf.Sqrt(n * 1.6f));
            int r = i / cols, c = i % cols;
            var local = new Vector3((c - (cols - 1) * 0.5f) * 1.3f, 0, (r - (n / cols) * 0.5f) * 1.4f + ((r % 2) * 0.3f));
            return Quaternion.Euler(0, yaw, 0) * local;
        }

        // ------------------------------------------------------------------ looks
        /// <summary>Renders the 3D scene at a fraction of the screen resolution, scaled up without smoothing. The UI stays sharp.</summary>
        public static string Pixelate(float scale)
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (!urp) return "no URP asset";
            if (!I.pixelated) { I.origScale = urp.renderScale; I.origFilter = urp.upscalingFilter; }
            urp.renderScale = scale;
            urp.upscalingFilter = UpscalingFilterSelection.Point;
            I.pixelated = true;
            return $"pixelated: render scale {scale} ({Mathf.RoundToInt(Screen.width * scale)} x {Mathf.RoundToInt(Screen.height * scale)}), nearest-neighbour upscale";
        }

        /// <summary>Back to native resolution (and restores the URP asset, which Play mode would otherwise keep changed).</summary>
        public static string Native()
        {
            if (!I || !I.pixelated) return "native";
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp) { urp.renderScale = I.origScale; urp.upscalingFilter = I.origFilter; }
            I.pixelated = false;
            return "native";
        }

        public static string View(float yaw, float zoom)
        {
            CameraRig.I.follow = true;
            CameraRig.I.SetView(yaw, zoom);
            return $"view yaw {yaw} zoom {zoom}";
        }

        // ------------------------------------------------------------------ measuring
        /// <summary>Average frame time over a number of frames, with vsync and the frame cap off while measuring.</summary>
        public static string Measure(int frames = 240)
        {
            I.StartCoroutine(I.MeasureRoutine(frames));
            return "measuring";
        }

        IEnumerator MeasureRoutine(int frames)
        {
            Report = "measuring";
            int vs = QualitySettings.vSyncCount, fr = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;
            for (int i = 0; i < 60; i++) yield return null;
            var t = new List<float>(frames);
            for (int i = 0; i < frames; i++) { yield return null; t.Add(Time.unscaledDeltaTime * 1000f); }
            QualitySettings.vSyncCount = vs; Application.targetFrameRate = fr;
            t.Sort();
            string stats = "";
#if UNITY_EDITOR
            stats = $" | {UnityEditor.UnityStats.triangles / 1000}k tris, {UnityEditor.UnityStats.vertices / 1000}k verts, {UnityEditor.UnityStats.drawCalls} draw calls, {UnityEditor.UnityStats.setPassCalls} set-pass, {UnityEditor.UnityStats.visibleSkinnedMeshes} skinned";
#endif
            Report = $"{frames} frames: mean {t.Average():F2} ms, median {t[t.Count / 2]:F2} ms, 95th {t[(int)(t.Count * 0.95f)]:F2} ms{stats}";
        }
    }
}
