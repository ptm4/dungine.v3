using System.Collections.Generic;
using Dungine.Visual;
using UnityEngine;

namespace Dungine.World
{
    public class BuildingInfo
    {
        public GameObject go;
        public Vector3 doorWorld;       // point just outside the front door
        public Vector3 doorFacing;      // outward normal
        public List<Light> lights = new List<Light>();
    }

    /// <summary>Procedural Barovian architecture. Buildings face +Z (front door on the +Z wall).</summary>
    public static class Buildings
    {
        const int STONE = 0, PLASTER = 1, TIMBER = 2, ROOF = 3, WLIT = 4, WDARK = 5, IRON = 6, PLANK = 7, ASHLAR = 8, STAINED = 9;

        static Material[] Mats(bool slate = false, bool dark = false) => new[]
        {
            Pal.Stone, dark ? Pal.PlasterDark : Pal.Plaster, dark ? Pal.TimberDark : Pal.Timber, slate ? Pal.Slate : Pal.Shingle, Pal.WindowLit, Pal.Glass, Pal.Iron, Pal.Planks, Pal.Ashlar, Pal.StainedGlass
        };

        public class HouseSpec
        {
            public float w = 7, d = 6, floorH = 2.9f; public int floors = 2;
            public float roofPitch = 42f; public bool jetty = true; public bool chimney = true;
            public float litChance = 0.35f; public float boardedChance = 0.25f;
            public bool slate; public bool dark; public int seed;
            public bool sign; public string signText;
            public bool porch;
        }

        public static BuildingInfo House(HouseSpec s, Transform parent, Vector3 pos, float yaw, string name = "House")
        {
            var rnd = new System.Random(s.seed);
            var mb = new MeshBuilder(10) { uvScale = 0.5f };
            float found = 0.55f;
            float W = s.w, D = s.d;
            // foundation
            mb.AddRoughBox(STONE, new Vector3(0, found * 0.5f - 0.1f, 0), new Vector3(W + 0.2f, found + 0.2f, D + 0.2f), 0.04f, s.seed);
            float y = found;
            float jet = s.jetty ? 0.35f : 0f;
            for (int f = 0; f < s.floors; f++)
            {
                float fw = W + (f > 0 ? jet * 2 : 0) * 0 , fd = D + (f > 0 ? jet * 2 : 0);
                // walls (plaster body)
                mb.AddBox(PLASTER, new Vector3(0, y + s.floorH * 0.5f, 0), new Vector3(fw, s.floorH, fd), 1 | 2 | 16 | 32);
                // timber frame: corner posts, sill & top beams, mid posts, braces
                float t = 0.16f;
                Vector3 h = new Vector3(fw / 2, 0, fd / 2);
                foreach (var cx in new[] { -1, 1 }) foreach (var cz in new[] { -1, 1 })
                        mb.AddBox(TIMBER, new Vector3(cx * (h.x - t * 0.3f), y + s.floorH * 0.5f, cz * (h.z - t * 0.3f)), new Vector3(t + 0.02f, s.floorH, t + 0.02f));
                FrameFace(mb, new Vector3(0, y, h.z + 0.02f), fw, s.floorH, Vector3.right, Vector3.forward, rnd, f == 0);
                FrameFace(mb, new Vector3(0, y, -h.z - 0.02f), fw, s.floorH, Vector3.left, Vector3.back, rnd, false);
                FrameFace(mb, new Vector3(h.x + 0.02f, y, 0), fd, s.floorH, Vector3.back, Vector3.right, rnd, false);
                FrameFace(mb, new Vector3(-h.x - 0.02f, y, 0), fd, s.floorH, Vector3.forward, Vector3.left, rnd, false);
                // jetty beam ends
                if (f > 0 && s.jetty)
                    for (float bx = -fw / 2 + 0.5f; bx < fw / 2; bx += 0.9f)
                    {
                        mb.AddBox(TIMBER, new Vector3(bx, y - 0.1f, fd / 2 + 0.05f), new Vector3(0.14f, 0.18f, 0.3f));
                        mb.AddBox(TIMBER, new Vector3(bx, y - 0.1f, -fd / 2 - 0.05f), new Vector3(0.14f, 0.18f, 0.3f));
                    }
                // windows
                int nFront = Mathf.Max(1, Mathf.RoundToInt(fw / 2.4f));
                for (int i = 0; i < nFront; i++)
                {
                    float wx = -fw / 2 + fw * (i + 0.5f) / nFront;
                    if (f == 0 && Mathf.Abs(wx) < 1.0f) continue; // door space
                    Window(mb, new Vector3(wx, y + s.floorH * 0.55f, fd / 2 + 0.03f), Vector3.forward, rnd, s);
                    Window(mb, new Vector3(wx, y + s.floorH * 0.55f, -fd / 2 - 0.03f), Vector3.back, rnd, s);
                }
                int nSide = Mathf.Max(1, Mathf.RoundToInt(fd / 3f));
                for (int i = 0; i < nSide; i++)
                {
                    float wz = -fd / 2 + fd * (i + 0.5f) / nSide;
                    Window(mb, new Vector3(fw / 2 + 0.03f, y + s.floorH * 0.55f, wz), Vector3.right, rnd, s);
                    Window(mb, new Vector3(-fw / 2 - 0.03f, y + s.floorH * 0.55f, wz), Vector3.left, rnd, s);
                }
                // floor band
                mb.AddBox(TIMBER, new Vector3(0, y + s.floorH - 0.06f, 0), new Vector3(fw + 0.08f, 0.14f, fd + 0.08f), 1 | 2 | 16 | 32);
                y += s.floorH;
            }
            float topD = D + (s.floors > 1 ? jet * 2 : 0);
            // door
            Door(mb, new Vector3(0, found, D / 2 + 0.04f), 1.1f, 2.1f);
            // steps
            mb.AddRoughBox(STONE, new Vector3(0, found * 0.35f, D / 2 + 0.5f), new Vector3(1.6f, found * 0.7f, 0.6f), 0.03f, s.seed + 1);
            // roof
            float rise = Mathf.Tan(s.roofPitch * Mathf.Deg2Rad) * (topD / 2 + 0.4f);
            Roof(mb, new Vector3(0, y, 0), W + 0.7f, topD + 0.8f, rise);
            // gable ends
            for (int side = -1; side <= 1; side += 2)
            {
                float gx = side * (W / 2 + 0.01f);
                mb.Push();
                mb.Translate(new Vector3(gx, y, 0));
                mb.Rotate(Quaternion.Euler(0, side > 0 ? -90 : 90, 0));
                var a = new Vector3(-topD / 2, 0, 0); var b = new Vector3(topD / 2, 0, 0); var c = new Vector3(0, rise * (topD / 2) / (topD / 2 + 0.4f), 0);
                GableTri(mb, PLASTER, a, c, b);
                mb.AddBox(TIMBER, new Vector3(0, c.y * 0.45f, -0.03f), new Vector3(0.14f, c.y * 0.9f, 0.06f));
                mb.AddBox(TIMBER, new Vector3(0, 0.07f, -0.03f), new Vector3(topD, 0.14f, 0.06f));
                if (rnd.NextDouble() < 0.6) Window(mb, new Vector3(0.9f, c.y * 0.3f, -0.05f), Vector3.back, rnd, s, 0.6f);
                mb.Pop();
            }
            // chimney
            if (s.chimney)
            {
                float cx = (rnd.NextDouble() < 0.5 ? -1 : 1) * (W / 2 - 0.9f);
                mb.AddRoughBox(STONE, new Vector3(cx, y + rise * 0.7f, -topD * 0.15f), new Vector3(0.8f, rise * 1.6f + 1.2f, 0.8f), 0.03f, s.seed + 5);
                mb.AddBox(STONE, new Vector3(cx, y + rise * 1.5f + 0.7f, -topD * 0.15f), new Vector3(0.95f, 0.15f, 0.95f));
            }
            var go = Kit.FromBuilder(mb, Mats(s.slate, s.dark), parent, name);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0, y / 2, 0); bc.size = new Vector3(W, y, D);
            var info = new BuildingInfo { go = go };
            info.doorWorld = go.transform.TransformPoint(new Vector3(0, 0, D / 2 + 1.2f));
            info.doorFacing = go.transform.forward;
            if (s.sign) Sign(go.transform, new Vector3(1.2f, found + 2.6f, D / 2 + 0.1f), s.signText);
            return info;
        }

        static void FrameFace(MeshBuilder mb, Vector3 baseCenter, float width, float h, Vector3 right, Vector3 normal, System.Random rnd, bool door)
        {
            float t = 0.14f;
            Quaternion q = Quaternion.LookRotation(normal);
            mb.Push();
            mb.Translate(baseCenter);
            mb.Rotate(q);
            // in this frame: +x along wall (right when looking at the wall from outside is -x), +z outward
            mb.AddBox(TIMBER, new Vector3(0, 0.08f, 0), new Vector3(width, t, 0.05f));
            mb.AddBox(TIMBER, new Vector3(0, h - 0.08f, 0), new Vector3(width, t, 0.05f));
            mb.AddBox(TIMBER, new Vector3(0, h * 0.5f, 0), new Vector3(width, t * 0.8f, 0.04f));
            int posts = Mathf.Max(2, Mathf.RoundToInt(width / 1.2f));
            for (int i = 1; i < posts; i++)
            {
                float x = -width / 2 + width * i / posts;
                if (door && Mathf.Abs(x) < 0.8f) continue;
                mb.AddBox(TIMBER, new Vector3(x, h * 0.5f, 0), new Vector3(t, h, 0.05f));
                if (rnd.NextDouble() < 0.45)
                {
                    float seg = width / posts;
                    Vector3 a = new Vector3(x - seg * 0.95f, 0.1f, 0), b = new Vector3(x, h * 0.5f, 0);
                    float len = Vector3.Distance(a, b); float ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                    mb.Push(); mb.Translate((a + b) * 0.5f); mb.Rotate(Quaternion.Euler(0, 0, ang));
                    mb.AddBox(TIMBER, Vector3.zero, new Vector3(len, t * 0.8f, 0.04f));
                    mb.Pop();
                }
            }
            mb.Pop();
        }

        static void Window(MeshBuilder mb, Vector3 c, Vector3 n, System.Random rnd, HouseSpec s, float scale = 1f)
        {
            mb.Push();
            mb.Translate(c);
            mb.Rotate(Quaternion.LookRotation(n));
            float w = 0.75f * scale, h = 1.0f * scale;
            double roll = rnd.NextDouble();
            bool lit = roll < s.litChance;
            bool boarded = !lit && roll < s.litChance + s.boardedChance;
            mb.AddBox(lit ? WLIT : WDARK, new Vector3(0, 0, 0.0f), new Vector3(w, h, 0.04f), 16);
            mb.AddBox(TIMBER, new Vector3(0, h / 2 + 0.05f, 0.04f), new Vector3(w + 0.2f, 0.1f, 0.1f));
            mb.AddBox(TIMBER, new Vector3(0, -h / 2 - 0.05f, 0.05f), new Vector3(w + 0.3f, 0.1f, 0.14f));
            mb.AddBox(TIMBER, new Vector3(-w / 2 - 0.04f, 0, 0.04f), new Vector3(0.08f, h, 0.08f));
            mb.AddBox(TIMBER, new Vector3(w / 2 + 0.04f, 0, 0.04f), new Vector3(0.08f, h, 0.08f));
            mb.AddBox(TIMBER, new Vector3(0, 0, 0.03f), new Vector3(0.04f, h, 0.04f));
            mb.AddBox(TIMBER, new Vector3(0, 0, 0.03f), new Vector3(w, 0.04f, 0.04f));
            if (boarded)
            {
                for (int i = 0; i < 3; i++)
                {
                    mb.Push(); mb.Translate(new Vector3(0, (i - 1) * 0.3f * scale, 0.09f)); mb.Rotate(Quaternion.Euler(0, 0, (float)(rnd.NextDouble() - .5) * 20));
                    mb.AddBox(PLANK, Vector3.zero, new Vector3(w + 0.3f, 0.14f, 0.03f)); mb.Pop();
                }
            }
            else if (!lit || rnd.NextDouble() < 0.5)
            {
                // shutters, open at an angle
                for (int side = -1; side <= 1; side += 2)
                {
                    mb.Push(); mb.Translate(new Vector3(side * (w / 2 + 0.06f), 0, 0.06f)); mb.Rotate(Quaternion.Euler(0, side * (lit ? 70 : 20 + (float)rnd.NextDouble() * 40), 0));
                    mb.AddBox(PLANK, new Vector3(side * w * 0.25f, 0, 0), new Vector3(w * 0.5f, h, 0.04f)); mb.Pop();
                }
            }
            mb.Pop();
        }

        static void Door(MeshBuilder mb, Vector3 c, float w, float h)
        {
            mb.AddBox(TIMBER, new Vector3(c.x, c.y + h / 2, c.z), new Vector3(w + 0.25f, h + 0.15f, 0.08f));
            mb.AddBox(PLANK, new Vector3(c.x, c.y + h / 2, c.z + 0.05f), new Vector3(w, h, 0.06f));
            for (int i = 0; i < 2; i++) mb.AddBox(IRON, new Vector3(c.x - w * 0.15f, c.y + h * (0.25f + i * 0.5f), c.z + 0.09f), new Vector3(w * 0.7f, 0.06f, 0.02f));
            mb.AddEllipsoid(IRON, new Vector3(c.x + w * 0.35f, c.y + h * 0.5f, c.z + 0.1f), Vector3.one * 0.04f, 4, 6);
        }

        static void Roof(MeshBuilder mb, Vector3 top, float len, float depth, float rise)
        {
            float half = depth / 2;
            float slope = Mathf.Sqrt(half * half + rise * rise);
            float ang = Mathf.Atan2(rise, half) * Mathf.Rad2Deg;
            for (int side = -1; side <= 1; side += 2)
            {
                mb.Push();
                mb.Translate(top + new Vector3(0, rise * 0.5f, side * half * 0.5f));
                mb.Rotate(Quaternion.Euler(side * ang, 0, 0));
                mb.AddBox(ROOF, Vector3.zero, new Vector3(len, 0.12f, slope + 0.05f));
                mb.Pop();
            }
            mb.AddBox(TIMBER, top + new Vector3(0, rise + 0.02f, 0), new Vector3(len + 0.1f, 0.14f, 0.2f));
        }

        static void GableTri(MeshBuilder mb, int sub, Vector3 a, Vector3 c, Vector3 b)
        {
            int i0 = mb.AddVertex(a, Vector3.forward, new Vector2(a.x, a.y) * 0.5f);
            int i1 = mb.AddVertex(c, Vector3.forward, new Vector2(c.x, c.y) * 0.5f);
            int i2 = mb.AddVertex(b, Vector3.forward, new Vector2(b.x, b.y) * 0.5f);
            mb.Tri(sub, i0, i1, i2);
        }

        public static void Sign(Transform parent, Vector3 local, string text)
        {
            var mb = new MeshBuilder(2);
            mb.AddBox(0, new Vector3(0.4f, 0, 0), new Vector3(0.8f, 0.05f, 0.05f));
            mb.AddBox(1, new Vector3(0.55f, -0.35f, 0), new Vector3(0.7f, 0.5f, 0.05f));
            mb.AddBox(0, new Vector3(0.3f, -0.1f, 0), new Vector3(0.03f, 0.2f, 0.03f));
            var go = Kit.FromBuilder(mb, new[] { Pal.Iron, Pal.Timber }, parent, "Sign");
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.Euler(0, -90, 0);
            go.AddComponent<Swing>();
        }

        // ------------------------------------------------------------------ church
        public static BuildingInfo Church(Transform parent, Vector3 pos, float yaw)
        {
            var mb = new MeshBuilder(10) { uvScale = 0.4f };
            float W = 9f, L = 16f, H = 7f;
            mb.AddRoughBox(STONE, new Vector3(0, 0.3f, 0), new Vector3(W + 0.4f, 0.8f, L + 0.4f), 0.05f, 3);
            mb.AddBox(ASHLAR, new Vector3(0, H / 2 + 0.5f, 0), new Vector3(W, H, L));
            // buttresses
            for (float z = -L / 2 + 2; z <= L / 2 - 2; z += 3.2f)
                foreach (var sx in new[] { -1, 1 })
                {
                    mb.AddBox(ASHLAR, new Vector3(sx * (W / 2 + 0.35f), 2.6f, z), new Vector3(0.7f, 5f, 0.9f));
                    mb.Push(); mb.Translate(new Vector3(sx * (W / 2 + 0.3f), 5.3f, z)); mb.Rotate(Quaternion.Euler(0, 0, sx * -35));
                    mb.AddBox(ASHLAR, Vector3.zero, new Vector3(0.6f, 1.4f, 0.9f)); mb.Pop();
                }
            // lancet windows (stained)
            for (float z = -L / 2 + 3.6f; z <= L / 2 - 3.6f; z += 3.2f)
                foreach (var sx in new[] { -1, 1 })
                {
                    mb.Push(); mb.Translate(new Vector3(sx * (W / 2 + 0.02f), 4.2f, z)); mb.Rotate(Quaternion.Euler(0, sx * 90, 0));
                    mb.AddBox(STAINED, Vector3.zero, new Vector3(1.0f, 2.6f, 0.05f));
                    mb.AddCone(STAINED, new Vector3(0, 1.3f, 0), 0.5f, 0.6f, 4);
                    mb.AddBox(STONE, new Vector3(0, -1.4f, 0.05f), new Vector3(1.3f, 0.15f, 0.2f));
                    mb.Pop();
                }
            // roof
            Roof(mb, new Vector3(0, H + 0.5f, 0), L + 0.6f, W + 1.2f, 4f, true);
            // tower at the front
            float tw = 4f, th = 15f;
            Vector3 tc = new Vector3(0, 0, L / 2 + tw / 2 - 0.5f);
            mb.AddBox(ASHLAR, tc + new Vector3(0, th / 2, 0), new Vector3(tw, th, tw));
            mb.AddBox(STONE, tc + new Vector3(0, th + 0.2f, 0), new Vector3(tw + 0.4f, 0.4f, tw + 0.4f));
            for (int s = 0; s < 4; s++)
            {
                mb.Push(); mb.Translate(tc + new Vector3(0, th - 2f, 0)); mb.Rotate(Quaternion.Euler(0, s * 90, 0));
                mb.AddBox(WDARK, new Vector3(0, 0, tw / 2 + 0.01f), new Vector3(1.1f, 2.2f, 0.05f));
                mb.AddCone(WDARK, new Vector3(0, 1.1f, tw / 2 + 0.01f), 0.55f, 0.6f, 4);
                mb.Pop();
            }
            // spire
            mb.Push(); mb.Translate(tc + new Vector3(0, th + 0.4f, 0)); mb.Rotate(Quaternion.Euler(0, 45, 0));
            mb.AddCylinder(ROOF, Vector3.zero, tw * 0.72f, 0.02f, 9f, 4, true);
            mb.Pop();
            // sun symbol
            mb.AddCylinder(IRON, tc + new Vector3(0, th + 9.2f, 0), 0.04f, 0.04f, 1.2f, 6);
            mb.Push(); mb.Translate(tc + new Vector3(0, th + 10.1f, 0)); mb.Rotate(Quaternion.Euler(90, 0, 0));
            mb.AddCylinder(IRON, new Vector3(0, -0.02f, 0), 0.3f, 0.3f, 0.04f, 12);
            mb.Pop();
            // front door
            mb.Push(); mb.Translate(tc + new Vector3(0, 0.5f, tw / 2 + 0.03f));
            mb.AddBox(STONE, new Vector3(0, 1.6f, 0), new Vector3(2.6f, 3.4f, 0.15f));
            mb.AddBox(PLANK, new Vector3(0, 1.4f, 0.1f), new Vector3(2.0f, 2.8f, 0.08f));
            mb.AddCone(PLANK, new Vector3(0, 2.8f, 0.1f), 1.0f, 0.8f, 4);
            mb.AddBox(IRON, new Vector3(0, 1.4f, 0.15f), new Vector3(0.05f, 2.7f, 0.02f));
            mb.Pop();
            mb.AddRoughBox(STONE, tc + new Vector3(0, 0.25f, tw / 2 + 0.9f), new Vector3(3f, 0.5f, 1.2f), 0.04f, 4);
            var go = Kit.FromBuilder(mb, Mats(true), parent, "Church");
            go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0, H / 2, 0.8f); bc.size = new Vector3(W + 1f, H, L + tw);
            return new BuildingInfo { go = go, doorWorld = go.transform.TransformPoint(tc + new Vector3(0, 0, tw / 2 + 2f)), doorFacing = go.transform.forward };
        }

        static void Roof(MeshBuilder mb, Vector3 top, float len, float depth, float rise, bool alongZ)
        {
            // ridge along Z
            float half = depth / 2;
            float slope = Mathf.Sqrt(half * half + rise * rise);
            float ang = Mathf.Atan2(rise, half) * Mathf.Rad2Deg;
            for (int side = -1; side <= 1; side += 2)
            {
                mb.Push();
                mb.Translate(top + new Vector3(side * half * 0.5f, rise * 0.5f, 0));
                mb.Rotate(Quaternion.Euler(0, 0, -side * ang));
                mb.AddBox(ROOF, Vector3.zero, new Vector3(slope + 0.05f, 0.15f, len));
                mb.Pop();
            }
            // gable walls
            for (int s = -1; s <= 1; s += 2)
            {
                mb.Push(); mb.Translate(top + new Vector3(0, 0, s * (len / 2 - 0.35f)));
                mb.Rotate(Quaternion.Euler(0, s > 0 ? 180 : 0, 0));
                GableTri(mb, ASHLAR, new Vector3(-half + 0.5f, 0, 0), new Vector3(0, rise - 0.2f, 0), new Vector3(half - 0.5f, 0, 0));
                mb.Pop();
            }
        }

        // ------------------------------------------------------------------ mansion
        public static BuildingInfo Mansion(Transform parent, Vector3 pos, float yaw)
        {
            var rnd = new System.Random(77);
            var mb = new MeshBuilder(10) { uvScale = 0.45f };
            float W = 16f, D = 10f, fh = 3.4f;
            var spec = new HouseSpec { litChance = 0.55f, boardedChance = 0.05f, seed = 77 };
            mb.AddRoughBox(STONE, new Vector3(0, 0.45f, 0), new Vector3(W + 0.4f, 1.1f, D + 0.4f), 0.04f, 7);
            mb.AddBox(ASHLAR, new Vector3(0, 0.9f + fh / 2, 0), new Vector3(W, fh, D));
            mb.AddBox(PLASTER, new Vector3(0, 0.9f + fh * 1.5f, 0), new Vector3(W, fh, D));
            mb.AddBox(STONE, new Vector3(0, 0.9f + fh, 0), new Vector3(W + 0.3f, 0.25f, D + 0.3f));
            for (int f = 0; f < 2; f++)
                for (int i = 0; i < 6; i++)
                {
                    float x = -W / 2 + W * (i + 0.5f) / 6f;
                    if (f == 0 && (i == 2 || i == 3)) continue;
                    Window(mb, new Vector3(x, 0.9f + fh * (f + 0.55f), D / 2 + 0.03f), Vector3.forward, rnd, spec, 1.15f);
                    Window(mb, new Vector3(x, 0.9f + fh * (f + 0.55f), -D / 2 - 0.03f), Vector3.back, rnd, spec, 1.15f);
                }
            // porch with columns
            mb.AddBox(STONE, new Vector3(0, 0.9f + fh - 0.1f, D / 2 + 1.4f), new Vector3(4.4f, 0.3f, 2.8f));
            foreach (var cx in new[] { -1.9f, 1.9f }) mb.AddCylinder(ASHLAR, new Vector3(cx, 0.9f, D / 2 + 2.5f), 0.22f, 0.2f, fh - 0.2f, 10);
            for (int st = 0; st < 3; st++) mb.AddBox(STONE, new Vector3(0, 0.15f + st * 0.25f, D / 2 + 3.2f - st * 0.45f), new Vector3(4f, 0.25f, 0.45f));
            Door(mb, new Vector3(0, 0.9f, D / 2 + 0.04f), 1.8f, 2.6f);
            float rise = 3.4f;
            Roof(mb, new Vector3(0, 0.9f + fh * 2, 0), W + 0.8f, D + 1.0f, rise);
            foreach (var sx in new[] { -1, 1 })
            {
                mb.Push(); mb.Translate(new Vector3(sx * (W / 2), 0.9f + fh * 2, 0)); mb.Rotate(Quaternion.Euler(0, sx > 0 ? -90 : 90, 0));
                GableTri(mb, PLASTER, new Vector3(-D / 2, 0, 0), new Vector3(0, rise * (D / 2) / (D / 2 + 0.5f), 0), new Vector3(D / 2, 0, 0));
                mb.Pop();
                mb.AddRoughBox(STONE, new Vector3(sx * (W / 2 - 1.2f), 0.9f + fh * 2 + rise, -1f), new Vector3(0.9f, 3f, 0.9f), 0.03f, 8 + sx);
            }
            var go = Kit.FromBuilder(mb, Mats(true), parent, "Mansion");
            go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0, fh, 0); bc.size = new Vector3(W, fh * 2, D);
            var bc2 = go.AddComponent<BoxCollider>(); bc2.center = new Vector3(0, fh, D / 2 + 2.5f); bc2.size = new Vector3(4.4f, fh * 2, 0.5f); bc2.enabled = false;
            return new BuildingInfo { go = go, doorWorld = go.transform.TransformPoint(new Vector3(0, 0, D / 2 + 4.2f)), doorFacing = go.transform.forward };
        }

        // ------------------------------------------------------------------ death house
        public static BuildingInfo DeathHouse(Transform parent, Vector3 pos, float yaw)
        {
            var rnd = new System.Random(13);
            var mb = new MeshBuilder(10) { uvScale = 0.5f };
            float W = 7.5f, D = 9f, fh = 3.1f;
            var spec = new HouseSpec { litChance = 0.2f, boardedChance = 0.0f, seed = 13, dark = true };
            mb.AddRoughBox(STONE, new Vector3(0, 0.4f, 0), new Vector3(W + 0.3f, 1.0f, D + 0.3f), 0.04f, 13);
            for (int f = 0; f < 3; f++)
            {
                float y = 0.9f + fh * f;
                float grow = f * 0.25f;
                mb.AddBox(f == 0 ? ASHLAR : PLASTER, new Vector3(0, y + fh / 2, 0), new Vector3(W + grow, fh, D + grow), 1 | 2 | 16 | 32);
                if (f > 0)
                {
                    FrameFace(mb, new Vector3(0, y, (D + grow) / 2 + 0.02f), W + grow, fh, Vector3.right, Vector3.forward, rnd, false);
                    FrameFace(mb, new Vector3(0, y, -(D + grow) / 2 - 0.02f), W + grow, fh, Vector3.left, Vector3.back, rnd, false);
                    FrameFace(mb, new Vector3((W + grow) / 2 + 0.02f, y, 0), D + grow, fh, Vector3.back, Vector3.right, rnd, false);
                    FrameFace(mb, new Vector3(-(W + grow) / 2 - 0.02f, y, 0), D + grow, fh, Vector3.forward, Vector3.left, rnd, false);
                }
                for (int i = 0; i < 3; i++)
                {
                    float x = -(W + grow) / 2 + (W + grow) * (i + 0.5f) / 3f;
                    if (f == 0 && i == 1) continue;
                    Window(mb, new Vector3(x, y + fh * 0.55f, (D + grow) / 2 + 0.03f), Vector3.forward, rnd, spec, 1.1f);
                    Window(mb, new Vector3(x, y + fh * 0.55f, -(D + grow) / 2 - 0.03f), Vector3.back, rnd, spec, 1.1f);
                }
                mb.AddBox(TIMBER, new Vector3(0, y + fh - 0.05f, 0), new Vector3(W + grow + 0.1f, 0.14f, D + grow + 0.1f), 1 | 2 | 16 | 32);
            }
            // steep roof + dormer
            float top = 0.9f + fh * 3;
            Roof(mb, new Vector3(0, top, 0), W + 1.2f, D + 1.4f, 5.2f);
            foreach (var sx in new[] { -1, 1 })
            {
                mb.Push(); mb.Translate(new Vector3(sx * (W / 2 + 0.25f), top, 0)); mb.Rotate(Quaternion.Euler(0, sx > 0 ? -90 : 90, 0));
                GableTri(mb, PLASTER, new Vector3(-(D + 0.5f) / 2, 0, 0), new Vector3(0, 5.2f * ((D + 0.5f) / 2) / ((D + 0.5f) / 2 + 0.45f), 0), new Vector3((D + 0.5f) / 2, 0, 0));
                mb.Pop();
            }
            Window(mb, new Vector3(0, top + 1.4f, (D + 1.4f) / 2 - 0.9f), Vector3.forward, rnd, new HouseSpec { litChance = 1f }, 0.8f);
            mb.AddRoughBox(STONE, new Vector3(-W / 2 + 1f, top + 3f, -1.5f), new Vector3(0.9f, 4f, 0.9f), 0.03f, 99);
            // portico
            Door(mb, new Vector3(0, 0.9f, D / 2 + 0.04f), 1.4f, 2.4f);
            foreach (var cx in new[] { -1.2f, 1.2f }) mb.AddCylinder(STONE, new Vector3(cx, 0.9f, D / 2 + 1.1f), 0.15f, 0.13f, 2.6f, 8);
            mb.AddBox(STONE, new Vector3(0, 3.6f, D / 2 + 0.7f), new Vector3(3f, 0.2f, 1.4f));
            for (int st = 0; st < 3; st++) mb.AddBox(STONE, new Vector3(0, 0.15f + st * 0.25f, D / 2 + 2f - st * 0.4f), new Vector3(2.6f, 0.25f, 0.4f));
            var go = Kit.FromBuilder(mb, Mats(true, true), parent, "DeathHouse");
            go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0, top / 2, 0); bc.size = new Vector3(W, top, D);
            return new BuildingInfo { go = go, doorWorld = go.transform.TransformPoint(new Vector3(0, 0, D / 2 + 2.8f)), doorFacing = go.transform.forward };
        }

        // ------------------------------------------------------------------ fences, gates, walls
        public static void IronFence(Transform parent, Vector3 a, Vector3 b, float h = 1.8f, bool gapMiddle = false)
        {
            var mb = new MeshBuilder(1);
            Vector3 d = b - a; float len = d.magnitude; Vector3 dir = d / len;
            Quaternion q = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z).normalized, Vector3.up);
            int bars = Mathf.CeilToInt(len / 0.18f);
            for (int i = 0; i <= bars; i++)
            {
                float t = i / (float)bars;
                if (gapMiddle && Mathf.Abs(t - 0.5f) * len < 1.3f) continue;
                Vector3 p = a + d * t;
                mb.AddCylinder(0, p, 0.018f, 0.018f, h, 5, false);
                mb.AddCone(0, p + Vector3.up * h, 0.035f, 0.14f, 4);
            }
            for (int k = 0; k < 2; k++)
            {
                float y = k == 0 ? 0.15f : h - 0.2f;
                mb.Push(); mb.Translate(a + d * 0.5f + Vector3.up * y); mb.Rotate(q);
                mb.AddBox(0, Vector3.zero, new Vector3(0.03f, 0.05f, len));
                mb.Pop();
            }
            var go = Kit.FromBuilder(mb, new[] { Pal.Iron }, parent, "IronFence");
            // axis-aligned colliders; with a gate gap, one on each side of it
            void Col(Vector3 p0, Vector3 p1)
            {
                var bc = go.AddComponent<BoxCollider>();
                Vector3 dd = p1 - p0;
                bc.center = p0 + dd * 0.5f + Vector3.up * h / 2; bc.size = new Vector3(Mathf.Abs(dd.x) + 0.1f, h, Mathf.Abs(dd.z) + 0.1f);
            }
            if (gapMiddle)
            {
                float g = 1.3f / len;
                Col(a, a + d * (0.5f - g));
                Col(a + d * (0.5f + g), b);
            }
            else Col(a, b);
        }

        public static void WoodFence(Transform parent, Vector3 a, Vector3 b, int seed)
        {
            var rnd = new System.Random(seed);
            var mb = new MeshBuilder(1) { uvScale = 0.6f };
            Vector3 d = b - a; float len = d.magnitude; Vector3 dir = d / len;
            int posts = Mathf.CeilToInt(len / 2f);
            for (int i = 0; i <= posts; i++)
            {
                Vector3 p = a + d * (i / (float)posts);
                mb.Push(); mb.Translate(p); mb.Rotate(Quaternion.Euler((float)(rnd.NextDouble() - .5) * 8, (float)rnd.NextDouble() * 20, (float)(rnd.NextDouble() - .5) * 8));
                mb.AddBox(0, new Vector3(0, 0.6f, 0), new Vector3(0.12f, 1.2f, 0.12f)); mb.Pop();
            }
            float ang = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            for (int k = 0; k < 2; k++)
            {
                if (rnd.NextDouble() < 0.15) continue;
                mb.Push(); mb.Translate(a + d * 0.5f + Vector3.up * (0.45f + k * 0.45f)); mb.Rotate(Quaternion.Euler((float)(rnd.NextDouble() - .5) * 4, ang, 0));
                mb.AddBox(0, Vector3.zero, new Vector3(0.05f, 0.1f, len)); mb.Pop();
            }
            var go = Kit.FromBuilder(mb, new[] { Pal.TimberDark }, parent, "WoodFence");
            var bc = go.AddComponent<BoxCollider>(); bc.center = a + d * 0.5f + Vector3.up * 0.6f; bc.size = new Vector3(Mathf.Abs(dir.x) * len + 0.15f, 1.2f, Mathf.Abs(dir.z) * len + 0.15f);
        }

        public static void StoneWall(Transform parent, Vector3 a, Vector3 b, float h, int seed)
        {
            var mb = new MeshBuilder(1) { uvScale = 0.5f };
            Vector3 d = b - a; float len = d.magnitude;
            float ang = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            mb.Push(); mb.Translate(a + d * 0.5f); mb.Rotate(Quaternion.Euler(0, ang, 0));
            mb.AddRoughBox(0, new Vector3(0, h / 2, 0), new Vector3(0.7f, h, len), 0.08f, seed);
            mb.AddRoughBox(0, new Vector3(0, h + 0.1f, 0), new Vector3(0.85f, 0.2f, len + 0.1f), 0.05f, seed + 1);
            mb.Pop();
            Kit.FromBuilder(mb, new[] { Pal.Stone }, parent, "StoneWall", Kit.ColliderKind.Box);
        }

        /// <summary>The Gates of Barovia: two great pillars with headless guardian statues and an iron double gate.</summary>
        public static GameObject BaroviaGates(Transform parent, Vector3 pos, float yaw, out Transform leafL, out Transform leafR)
        {
            var root = new GameObject("BaroviaGates").transform;
            root.SetParent(parent, false); root.localPosition = pos; root.localRotation = Quaternion.Euler(0, yaw, 0);
            var mb = new MeshBuilder(2) { uvScale = 0.5f };
            foreach (var sx in new[] { -1, 1 })
            {
                Vector3 c = new Vector3(sx * 4.2f, 0, 0);
                mb.AddRoughBox(0, c + new Vector3(0, 3.5f, 0), new Vector3(1.6f, 7f, 1.6f), 0.08f, 20 + sx);
                mb.AddRoughBox(0, c + new Vector3(0, 7.2f, 0), new Vector3(2.0f, 0.5f, 2.0f), 0.05f, 22 + sx);
                // headless armoured statue
                mb.AddCylinder(0, c + new Vector3(0, 7.45f, 0), 0.45f, 0.4f, 1.3f, 10);
                mb.AddEllipsoid(0, c + new Vector3(0, 9.3f, 0), new Vector3(0.62f, 0.9f, 0.42f), 8, 10);
                mb.AddEllipsoid(0, c + new Vector3(sx * -0.2f, 10.0f, 0), new Vector3(0.75f, 0.3f, 0.45f), 6, 8);
                mb.AddCylinder(0, c + new Vector3(sx * 0.7f, 7.6f, 0.2f), 0.09f, 0.09f, 3.2f, 6);
                mb.AddBox(0, c + new Vector3(sx * 0.7f, 10.9f, 0.2f), new Vector3(0.08f, 1.0f, 0.4f));
                mb.AddEllipsoid(0, c + new Vector3(0, 10.25f, 0), new Vector3(0.22f, 0.12f, 0.22f), 5, 8, p => new Vector3(p.x, Mathf.Min(p.y, 0.02f), p.z));
            }
            Kit.FromBuilder(mb, new[] { Pal.Rock, Pal.Iron }, root, "Pillars", Kit.ColliderKind.Mesh);
            Transform Leaf(int sx)
            {
                var hinge = new GameObject(sx < 0 ? "LeafL" : "LeafR").transform;
                hinge.SetParent(root, false); hinge.localPosition = new Vector3(sx * 3.35f, 0, 0);
                var lm = new MeshBuilder(1);
                float w = 3.3f;
                for (int i = 0; i <= 14; i++) { float x = -sx * w * i / 14f; lm.AddCylinder(0, new Vector3(x, 0.05f, 0), 0.03f, 0.03f, 4.6f, 5, false); lm.AddCone(0, new Vector3(x, 4.65f, 0), 0.06f, 0.2f, 4); }
                foreach (var y in new[] { 0.3f, 2.2f, 4.3f }) lm.AddBox(0, new Vector3(-sx * w / 2, y, 0), new Vector3(w, 0.08f, 0.06f));
                for (int k = 0; k < 6; k++) { float a = k / 6f * Mathf.PI * 2; lm.AddCylinder(0, new Vector3(-sx * w / 2 + Mathf.Cos(a) * 0.6f, 3.2f + Mathf.Sin(a) * 0.6f, 0), 0.02f, 0.02f, 0.3f, 4, false); }
                var g = Kit.FromBuilder(lm, new[] { Pal.Iron }, hinge, "Leaf");
                var bc = g.AddComponent<BoxCollider>(); bc.center = new Vector3(-sx * w / 2, 2.3f, 0); bc.size = new Vector3(w, 4.6f, 0.2f);
                return hinge;
            }
            leafL = Leaf(-1); leafR = Leaf(1);
            return root.gameObject;
        }

        /// <summary>Castle Ravenloft on its pillar of rock — a distant silhouette with burning windows.</summary>
        public static GameObject CastleRavenloft(Transform parent, Vector3 pos, float yaw, float scale)
        {
            var mb = new MeshBuilder(4) { uvScale = 0.15f };
            var rnd = new System.Random(1476);
            // the rock pillar
            mb.AddEllipsoid(0, new Vector3(0, 20, 0), new Vector3(55, 60, 45), 12, 16, p =>
            {
                float n = Noise.Fbm(p.x * 0.01f + 3, p.z * 0.01f + p.y * 0.008f, 3, 4, 44);
                p.x *= 0.7f + n * 0.6f; p.z *= 0.7f + n * 0.6f;
                if (p.y > 40) p.y = 40 + (p.y - 40) * 0.1f;
                return p;
            });
            float baseY = 60;
            void Tower(Vector3 c, float r, float h, float cap)
            {
                mb.AddCylinder(1, c, r, r * 0.95f, h, 12, false);
                mb.AddCylinder(1, c + Vector3.up * h, r * 1.15f, r * 1.15f, 1.2f, 12, true);
                mb.AddCone(2, c + Vector3.up * (h + 1.2f), r * 1.2f, cap, 12);
                for (int k = 0; k < 3; k++)
                {
                    float a = (float)rnd.NextDouble() * Mathf.PI * 2;
                    float y = h * (0.3f + (float)rnd.NextDouble() * 0.6f);
                    mb.Push(); mb.Translate(c + new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r)); mb.Rotate(Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a))));
                    mb.AddBox(rnd.NextDouble() < 0.6 ? 3 : 1, Vector3.zero, new Vector3(0.9f, 1.8f, 0.2f)); mb.Pop();
                }
            }
            // curtain wall
            mb.AddBox(1, new Vector3(0, baseY + 6, 0), new Vector3(60, 12, 50), 1 | 2 | 4 | 16 | 32);
            // keep
            mb.AddBox(1, new Vector3(0, baseY + 18, -4), new Vector3(30, 24, 26));
            // high tower (Tower of Strahd)
            Tower(new Vector3(8, baseY + 30, -8), 5f, 40, 16);
            Tower(new Vector3(-12, baseY + 30, 4), 4f, 22, 12);
            Tower(new Vector3(28, baseY, 23), 4.5f, 22, 10);
            Tower(new Vector3(-28, baseY, 23), 4.5f, 22, 10);
            Tower(new Vector3(28, baseY, -23), 4.5f, 20, 10);
            Tower(new Vector3(-28, baseY, -23), 4.5f, 20, 10);
            Tower(new Vector3(0, baseY + 30, 10), 3f, 18, 14);
            Tower(new Vector3(-6, baseY + 42, -10), 2.2f, 14, 10);
            // keep roof
            mb.Push(); mb.Translate(new Vector3(0, baseY + 30, -4));
            mb.AddPrism(2, new Vector3(-15, 0, -13), new Vector3(0, 10, -13), new Vector3(15, 0, -13), 26);
            mb.Pop();
            // lit windows on the keep
            for (int i = 0; i < 18; i++)
            {
                float x = -13 + 26 * (i % 6) / 5f; float y = baseY + 10 + (i / 6) * 6;
                mb.AddBox((i * 7) % 5 < 2 ? 3 : 1, new Vector3(x, y, 9.05f), new Vector3(1.0f, 2.2f, 0.2f));
            }
            var go = Kit.FromBuilder(mb, new[] { Pal.Rock, MatLib.Lit(new Color(.3f, .3f, .32f), TexId.StoneBrick, .2f, 0, 1f, 1f), Pal.Slate, MatLib.Emissive(new Color(.9f, .5f, .2f), new Color(3f, 1.2f, .3f)) }, parent, "CastleRavenloft", Kit.ColliderKind.None, false);
            go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0, yaw, 0); go.transform.localScale = Vector3.one * scale;
            return go;
        }
    }

    public class Swing : MonoBehaviour
    {
        float seed; Quaternion r0;
        void Start() { seed = Random.value * 10; r0 = transform.localRotation; }
        void Update() { transform.localRotation = r0 * Quaternion.Euler(Mathf.Sin(Time.time * 1.3f + seed) * 4f, 0, 0); }
    }
}
