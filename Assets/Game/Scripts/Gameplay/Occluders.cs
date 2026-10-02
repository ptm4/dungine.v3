using System.Collections.Generic;
using UnityEngine;

namespace Dungine
{
    /// <summary>
    /// Keeps the party visible, Baldur's Gate style. When a registered building stands between the camera and a party
    /// member it swaps to the Dungine/Occluder shader, which dithers a see-through hole around each character, cutting
    /// only the parts in front of them. The rest of the building stays solid and keeps casting its shadow. When the camera
    /// ends up among a building's walls the whole building dithers away instead. Trees that block the view dither away as
    /// a whole, leaving a faint trace. Everything eases in and out rather than popping.
    /// </summary>
    public class Occluders : MonoBehaviour
    {
        /// <summary>One building (all its renderers decide together) or one tree.</summary>
        class Occ
        {
            public Transform frame;          // space the bounds are measured in
            public Bounds bounds;
            public Renderer[] rs;
            public Material[][] original, faded;
            public bool tree, whole, swapped;
            public float fade, target;
            public float[] roof;             // highest surface over each cell of the footprint (buildings only)
        }

        const int RoofGrid = 20;

        static readonly List<Occ> all = new List<Occ>();
        static readonly List<Occ> active = new List<Occ>();
        static Shader shader;
        static readonly int HolesId = Shader.PropertyToID("_OccHoles");
        static readonly int FadeId = Shader.PropertyToID("_Parallax");
        static readonly int WholeId = Shader.PropertyToID("_ClearCoatSmoothness");
        const float FadeTime = 0.25f, TreeFade = 0.8f;

        readonly List<Vector3> targets = new List<Vector3>();
        readonly Vector4[] holes = new Vector4[4];
        float timer;

        /// <summary>A tree (or any single object that should fade whole).</summary>
        public static void Register(Renderer r, bool tree = false)
        {
            if (!r) return;
            all.Add(new Occ { frame = r.transform, bounds = r.localBounds, rs = new[] { r }, tree = tree });
        }

        /// <summary>A building: every renderer under root fades together, judged by the building's overall box.</summary>
        public static void RegisterGroup(Transform root)
        {
            var rs = root.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var b = new Bounds(); bool any = false;
            foreach (var r in rs)
            {
                var lb = r.localBounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = lb.center + Vector3.Scale(lb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = root.InverseTransformPoint(r.transform.TransformPoint(corner));
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            all.Add(new Occ { frame = root, bounds = b, rs = rs, roof = RoofMap(root, rs, b) });
        }

        /// <summary>
        /// Rasterises the building's triangles onto a coarse grid over its footprint, keeping the highest surface in
        /// each cell, so "is there roof above the camera?" is a lookup.
        /// </summary>
        static float[] RoofMap(Transform root, Renderer[] rs, Bounds b)
        {
            var map = new float[RoofGrid * RoofGrid];
            for (int i = 0; i < map.Length; i++) map[i] = float.NegativeInfinity;
            float cw = b.size.x / RoofGrid, cd = b.size.z / RoofGrid;
            foreach (var r in rs)
            {
                var mf = r.GetComponent<MeshFilter>();
                if (!mf || !mf.sharedMesh || !mf.sharedMesh.isReadable) continue;
                var mesh = mf.sharedMesh;
                var v = mesh.vertices; var tri = mesh.triangles;
                var toRoot = root.worldToLocalMatrix * r.transform.localToWorldMatrix;
                for (int i = 0; i < v.Length; i++) v[i] = toRoot.MultiplyPoint3x4(v[i]);
                for (int t = 0; t < tri.Length; t += 3)
                {
                    Vector3 a = v[tri[t]], c1 = v[tri[t + 1]], c2 = v[tri[t + 2]];
                    int x0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.x, Mathf.Min(c1.x, c2.x)) - b.min.x) / cw), 0, RoofGrid - 1);
                    int x1 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Max(a.x, Mathf.Max(c1.x, c2.x)) - b.min.x) / cw), 0, RoofGrid - 1);
                    int z0 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(a.z, Mathf.Min(c1.z, c2.z)) - b.min.z) / cd), 0, RoofGrid - 1);
                    int z1 = Mathf.Clamp(Mathf.FloorToInt((Mathf.Max(a.z, Mathf.Max(c1.z, c2.z)) - b.min.z) / cd), 0, RoofGrid - 1);
                    for (int gz = z0; gz <= z1; gz++)
                        for (int gx = x0; gx <= x1; gx++)
                        {
                            // highest point of the triangle within this cell, sampled at the cell centre and the triangle's own corners
                            float px = b.min.x + (gx + 0.5f) * cw, pz = b.min.z + (gz + 0.5f) * cd;
                            float h = float.NegativeInfinity;
                            if (Bary(new Vector2(px, pz), a, c1, c2, out float y)) h = y;
                            foreach (var q in new[] { a, c1, c2 })
                                if (Mathf.FloorToInt((q.x - b.min.x) / cw) == gx && Mathf.FloorToInt((q.z - b.min.z) / cd) == gz) h = Mathf.Max(h, q.y);
                            int k = gz * RoofGrid + gx;
                            if (h > map[k]) map[k] = h;
                        }
                }
            }
            return map;
        }

        static bool Bary(Vector2 p, Vector3 a, Vector3 b, Vector3 c, out float y)
        {
            y = 0;
            Vector2 v0 = new Vector2(b.x - a.x, b.z - a.z), v1 = new Vector2(c.x - a.x, c.z - a.z), v2 = new Vector2(p.x - a.x, p.y - a.z);
            float den = v0.x * v1.y - v1.x * v0.y;
            if (Mathf.Abs(den) < 1e-6f) return false;
            float u = (v2.x * v1.y - v1.x * v2.y) / den, w = (v0.x * v2.y - v2.x * v0.y) / den;
            if (u < 0 || w < 0 || u + w > 1) return false;
            y = a.y + u * (b.y - a.y) + w * (c.y - a.y);
            return true;
        }

        /// <summary>Is the camera among the walls or under the roof (rather than out in the open air above or beside it)?</summary>
        static bool UnderRoof(Occ o, Vector3 lo)
        {
            var b = o.bounds;
            if (o.roof == null || lo.x < b.min.x || lo.x > b.max.x || lo.z < b.min.z || lo.z > b.max.z || lo.y < b.min.y) return false;
            int gx = Mathf.Clamp(Mathf.FloorToInt((lo.x - b.min.x) / b.size.x * RoofGrid), 0, RoofGrid - 1);
            int gz = Mathf.Clamp(Mathf.FloorToInt((lo.z - b.min.z) / b.size.z * RoofGrid), 0, RoofGrid - 1);
            return o.roof[gz * RoofGrid + gx] > lo.y + 0.1f;
        }

        public static void Clear()
        {
            foreach (var o in all) Restore(o, true);
            all.Clear(); active.Clear();
        }

        static Shader OccShader => shader ? shader : (shader = Shader.Find("Dungine/Occluder"));

        void LateUpdate()
        {
            var cam = CameraRig.I ? CameraRig.I.cam : null;
            if (!cam || Game.I == null || Game.I.area == null) { Shader.SetGlobalVectorArray(HolesId, holes); return; }

            // who must stay visible: the party, and whoever's turn it is
            targets.Clear();
            foreach (var a in Game.I.PartyActors) if (a) targets.Add(a.Chest);
            var cur = Combat.CombatManager.I != null && Combat.CombatManager.I.Active ? Combat.CombatManager.I.Current : null;
            if (cur != null && cur.actor && !cur.isPC) targets.Add(cur.actor.Chest);

            // holes follow the characters every frame (screen position, size and depth)
            float tanHalf = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            for (int i = 0; i < holes.Length; i++)
            {
                holes[i] = Vector4.zero;
                if (i >= targets.Count) continue;
                Vector3 vp = cam.WorldToViewportPoint(targets[i]);
                if (vp.z <= 0.1f) continue;
                float radius = Mathf.Clamp(1.6f / (2f * vp.z * tanHalf), 0.09f, 0.5f);   // a little more than a character's height on screen
                holes[i] = new Vector4(vp.x, vp.y, radius, vp.z);
            }
            Shader.SetGlobalVectorArray(HolesId, holes);

            // which occluders are in the way (a few times a second)
            timer -= Time.unscaledDeltaTime;
            if (timer <= 0)
            {
                timer = 0.08f;
                Vector3 cp = cam.transform.position;
                for (int i = all.Count - 1; i >= 0; i--)
                {
                    var o = all[i];
                    if (!o.frame) { all.RemoveAt(i); active.Remove(o); continue; }
                    bool inside = false;
                    bool block = o.frame.gameObject.activeInHierarchy && Blocks(o, cp, out inside);
                    // a camera among a building's walls would see a skeleton of back-faced walls: fade all of it instead
                    bool whole = o.tree || (block && inside);
                    if (whole != o.whole)
                    {
                        o.whole = whole;
                        if (o.faded != null) foreach (var set in o.faded) foreach (var m in set) if (m && m.shader == shader) m.SetFloat(WholeId, whole ? 1f : 0f);
                    }
                    float want = block ? (o.tree ? TreeFade : 1f) : 0f;
                    if (want != o.target)
                    {
                        o.target = want;
                        if (want > 0 && !active.Contains(o)) active.Add(o);
                    }
                }
            }

            // ease every changing occluder toward its target
            float step = Time.unscaledDeltaTime / FadeTime;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var o = active[i];
                if (!o.frame) { active.RemoveAt(i); continue; }
                if (o.target > 0 && !o.swapped) Swap(o);
                o.fade = Mathf.MoveTowards(o.fade, o.target, step);
                if (o.faded != null) foreach (var set in o.faded) foreach (var m in set) if (m && m.shader == shader) m.SetFloat(FadeId, o.fade);
                if (o.fade <= 0f && o.target <= 0f) { Restore(o, false); active.RemoveAt(i); }
            }
        }

        bool Blocks(Occ o, Vector3 cp, out bool inside)
        {
            var lb = o.bounds;
            if (o.tree)
            {
                // the canopy, not the full trunk-to-tip box: skip the bare lower trunk
                var c = lb.center; var s = lb.size;
                float cut = s.y * 0.18f;
                lb = new Bounds(new Vector3(c.x, c.y + cut * 0.5f, c.z), new Vector3(s.x * 0.8f, s.y - cut, s.z * 0.8f));
            }
            var t = o.frame;
            Vector3 lo = t.InverseTransformPoint(cp);
            // "inside" means among the walls or under the roof; above the roof slopes a hole reads better
            inside = !o.tree && UnderRoof(o, lo);
            // buildings may be tested generously: the shader only cuts pixels that are in front of a character and near them
            if (!o.tree) lb.Expand(0.6f);
            foreach (var target in targets)
            {
                Vector3 lt = t.InverseTransformPoint(target);
                Vector3 d = lt - lo; float len = d.magnitude;
                if (len < 0.1f) continue;
                if (lb.IntersectRay(new Ray(lo, d / len), out float hit) && hit < len - 0.4f) return true;
            }
            // a camera among the walls hides the party even when the line to them leaves the box first
            return inside;
        }

        static void Swap(Occ o)
        {
            var sh = OccShader;
            if (!sh) return;
            if (o.original == null) o.original = new Material[o.rs.Length][];
            bool build = o.faded == null;
            if (build) o.faded = new Material[o.rs.Length][];
            for (int k = 0; k < o.rs.Length; k++)
            {
                var r = o.rs[k];
                if (!r) continue;
                o.original[k] = r.sharedMaterials;
                if (build)
                {
                    var src = o.original[k];
                    var dst = new Material[src.Length];
                    for (int i = 0; i < src.Length; i++)
                    {
                        var m0 = src[i];
                        if (!m0 || m0.shader == null || m0.shader.name != "Universal Render Pipeline/Lit") { dst[i] = m0; continue; }
                        var m = new Material(sh) { name = m0.name + " (occluder)" };
                        m.CopyPropertiesFromMaterial(m0);
                        if (m0.IsKeywordEnabled("_NORMALMAP")) m.EnableKeyword("_NORMALMAP");
                        if (m0.IsKeywordEnabled("_ALPHATEST_ON")) m.EnableKeyword("_ALPHATEST_ON");
                        m.SetFloat(FadeId, 0f);
                        m.SetFloat(WholeId, o.whole ? 1f : 0f);
                        dst[i] = m;
                    }
                    o.faded[k] = dst;
                }
                if (o.faded[k] != null) r.sharedMaterials = o.faded[k];
            }
            o.swapped = true;
        }

        static void Restore(Occ o, bool destroyTwins)
        {
            if (o.swapped && o.original != null)
                for (int k = 0; k < o.rs.Length; k++)
                    if (o.rs[k] && o.original[k] != null) o.rs[k].sharedMaterials = o.original[k];
            o.swapped = false; o.fade = 0; o.target = 0;
            if (destroyTwins && o.faded != null)
            {
                foreach (var set in o.faded) if (set != null) foreach (var m in set) if (m && m.shader == shader) Destroy(m);
                o.faded = null;
            }
        }
    }
}
