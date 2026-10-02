using System.Collections.Generic;
using Dungine.Visual;
using UnityEngine;

namespace Dungine.World
{
    /// <summary>A wall segment that lowers itself when it stands between the camera and the room (BG3-style cutaway).</summary>
    public class WallPiece : MonoBehaviour
    {
        public Vector3 outward;
        public float fullHeight = 3f;
        public float cur = 1f;
        public bool neverCut;
    }

    public class Cutaway : MonoBehaviour
    {
        public readonly List<WallPiece> walls = new List<WallPiece>();
        public static Cutaway Active;
        void OnEnable() => Active = this;
        void OnDisable() { if (Active == this) Active = null; }

        void LateUpdate()
        {
            var cam = Camera.main; if (!cam) return;
            Vector3 f = cam.transform.forward; f.y = 0; f.Normalize();
            Vector3 camPos = cam.transform.position;
            foreach (var w in walls)
            {
                if (!w) continue;
                float target = 1f;
                if (!w.neverCut)
                {
                    float facing = Vector3.Dot(w.outward, -f);
                    Vector3 toWall = w.transform.position - camPos; toWall.y = 0;
                    if (facing > 0.25f) target = 0.1f;
                }
                w.cur = Mathf.MoveTowards(w.cur, target, Time.deltaTime * 4f);
                w.transform.localScale = new Vector3(1, Mathf.Max(0.02f, w.cur), 1);
            }
        }
    }

    public struct Opening
    {
        public int side;      // 0 = +Z (north), 1 = +X (east), 2 = -Z (south), 3 = -X (west)
        public float center;  // offset along the wall from its middle
        public float width;
        public bool window;
        public float sill, top;
        public Opening(int side, float center, float width, bool window = false) { this.side = side; this.center = center; this.width = width; this.window = window; sill = window ? 1.0f : 0; top = window ? 2.2f : 2.3f; }
    }

    public static class Interiors
    {
        public static GameObject Floor(Transform parent, Vector3 center, Vector2 size, Material m, string name = "Floor", float thickness = 0.2f)
        {
            var mb = new MeshBuilder(1) { uvScale = 0.5f };
            mb.AddBox(0, center + new Vector3(0, -thickness / 2, 0), new Vector3(size.x, thickness, size.y), 63);
            var go = Kit.FromBuilder(mb, new[] { m }, parent, name, Kit.ColliderKind.Box, false);
            go.layer = Layers.Ground;
            return go;
        }

        /// <summary>A room: floor plus four walls with openings. Walls cut away toward the camera.</summary>
        public static void Room(Transform parent, Cutaway cut, Vector3 center, Vector2 size, float height, Material wallIn, Material wallOut, Material floor, IList<Opening> openings, bool trim = true, Material trimMat = null)
        {
            if (floor != null) Floor(parent, center, size, floor);
            trimMat ??= Pal.TimberDark;
            for (int side = 0; side < 4; side++)
            {
                float len = side % 2 == 0 ? size.x : size.y;
                Vector3 outward = side == 0 ? Vector3.forward : side == 1 ? Vector3.right : side == 2 ? Vector3.back : Vector3.left;
                Vector3 along = side == 0 ? Vector3.right : side == 1 ? Vector3.back : side == 2 ? Vector3.left : Vector3.forward;
                Vector3 wc = center + outward * ((side % 2 == 0 ? size.y : size.x) / 2);
                var ops = new List<Opening>();
                if (openings != null) foreach (var o in openings) if (o.side == side) ops.Add(o);
                ops.Sort((a, b) => a.center.CompareTo(b.center));
                // build segments between openings
                float cursor = -len / 2;
                var segs = new List<(float a, float b, float y0, float y1)>();
                foreach (var o in ops)
                {
                    float a = o.center - o.width / 2, b = o.center + o.width / 2;
                    if (a > cursor) segs.Add((cursor, a, 0, height));
                    if (o.sill > 0) segs.Add((a, b, 0, o.sill));
                    segs.Add((a, b, o.top, height));
                    cursor = b;
                }
                if (cursor < len / 2) segs.Add((cursor, len / 2, 0, height));
                var holder = new GameObject("Wall" + side);
                holder.transform.SetParent(parent, false);
                holder.transform.localPosition = wc;
                var wp = holder.AddComponent<WallPiece>(); wp.outward = outward; wp.fullHeight = height;
                cut?.walls.Add(wp);
                var mb = new MeshBuilder(3) { uvScale = 0.5f };
                Quaternion q = Quaternion.LookRotation(outward);
                foreach (var s in segs)
                {
                    if (s.b - s.a < 0.01f) continue;
                    float mid = (s.a + s.b) / 2;
                    Vector3 lp = Quaternion.Inverse(Quaternion.identity) * (along * mid);
                    mb.Push();
                    mb.Translate(lp + Vector3.up * (s.y0 + s.y1) / 2);
                    mb.Rotate(q);
                    Vector3 sz = new Vector3(s.b - s.a, s.y1 - s.y0, 0.25f);
                    mb.AddBox(0, new Vector3(0, 0, -0.01f), new Vector3(sz.x, sz.y, 0.02f), 32);  // inside face
                    mb.AddBox(1, new Vector3(0, 0, 0.1f), new Vector3(sz.x, sz.y, 0.2f), 1 | 2 | 4 | 8 | 16);
                    mb.Pop();
                    if (trim && s.y0 < 0.01f)
                    {
                        mb.Push(); mb.Translate(lp + Vector3.up * 0.08f); mb.Rotate(q);
                        mb.AddBox(2, new Vector3(0, 0, -0.04f), new Vector3(s.b - s.a, 0.16f, 0.06f));
                        mb.Pop();
                    }
                }
                // door / window frames
                foreach (var o in ops)
                {
                    mb.Push(); mb.Translate(along * o.center); mb.Rotate(q);
                    foreach (var sx in new[] { -1, 1 }) mb.AddBox(2, new Vector3(sx * (o.width / 2 + 0.05f), (o.sill + o.top) / 2, 0.08f), new Vector3(0.1f, o.top - o.sill, 0.32f));
                    mb.AddBox(2, new Vector3(0, o.top + 0.05f, 0.08f), new Vector3(o.width + 0.2f, 0.1f, 0.32f));
                    if (o.window) { mb.AddBox(2, new Vector3(0, o.sill - 0.03f, 0.08f), new Vector3(o.width + 0.2f, 0.06f, 0.36f)); }
                    mb.Pop();
                    if (o.window)
                    {
                        var wmb = new MeshBuilder(1);
                        wmb.Push(); wmb.Translate(along * o.center + Vector3.up * (o.sill + o.top) / 2); wmb.Rotate(q);
                        wmb.AddBox(0, new Vector3(0, 0, 0.12f), new Vector3(o.width, o.top - o.sill, 0.02f));
                        wmb.Pop();
                        var wgo = Kit.FromBuilder(wmb, new[] { MatLib.Emissive(new Color(.12f, .14f, .2f), new Color(.1f, .14f, .24f)) }, holder.transform, "Window", Kit.ColliderKind.None, false);
                    }
                }
                var go = Kit.FromBuilder(mb, new[] { wallIn, wallOut, trimMat }, holder.transform, "Segments", Kit.ColliderKind.None, true);
                go.layer = Layers.Walls;
                // colliders per segment (full height, so cutaway doesn't open the navmesh)
                foreach (var s in segs)
                {
                    if (s.y0 > 0.01f || s.b - s.a < 0.01f) continue;
                    var cgo = new GameObject("Col"); cgo.transform.SetParent(parent, false);
                    cgo.layer = Layers.Walls;
                    var bc = cgo.AddComponent<BoxCollider>();
                    float mid = (s.a + s.b) / 2;
                    cgo.transform.localPosition = wc + along * mid + Vector3.up * height / 2 + outward * 0.1f;
                    cgo.transform.localRotation = q;
                    bc.size = new Vector3(s.b - s.a, height, 0.3f);
                }
            }
        }

        public static void Beam(Transform parent, Vector3 a, Vector3 b, float thick, Material m)
        {
            var mb = new MeshBuilder(1) { uvScale = 1f };
            Vector3 d = b - a;
            mb.Push(); mb.Translate((a + b) / 2); mb.Rotate(Quaternion.LookRotation(d.normalized));
            mb.AddBox(0, Vector3.zero, new Vector3(thick, thick, d.magnitude)); mb.Pop();
            Kit.FromBuilder(mb, new[] { m }, parent, "Beam", Kit.ColliderKind.None);
        }
    }
}
