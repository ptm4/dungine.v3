using System;
using System.Collections.Generic;
using Dungine.Visual;
using UnityEngine;

namespace Dungine.World
{
    public static class Nature
    {
        // ------------------------------------------------------------ trees
        public static (Mesh, Material[]) DeadTree(int seed)
        {
            var rnd = new System.Random(seed);
            var mb = new MeshBuilder(1);
            float R() => (float)rnd.NextDouble();
            float trunkH = 3.5f + R() * 3f;
            float trunkR = 0.22f + R() * 0.15f;
            // gnarled trunk
            var pts = new List<Vector3>(); var rad = new List<float>();
            Vector3 lean = new Vector3(R() - .5f, 0, R() - .5f) * 0.6f;
            int n = 7;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                pts.Add(new Vector3(Mathf.Sin(t * 5 + seed) * 0.12f, t * trunkH, Mathf.Cos(t * 4 + seed) * 0.12f) + lean * t * t);
                rad.Add(trunkR * Mathf.Lerp(1.35f, 0.45f, t) * (i == 0 ? 1.4f : 1f));
            }
            mb.AddTube(0, pts, rad, 9);
            // roots
            for (int k = 0; k < 4; k++)
            {
                float a = k * Mathf.PI * 0.5f + R();
                var rp = new List<Vector3> { new Vector3(0, 0.35f, 0), new Vector3(Mathf.Cos(a) * 0.45f, 0.08f, Mathf.Sin(a) * 0.45f), new Vector3(Mathf.Cos(a) * 0.8f, -0.05f, Mathf.Sin(a) * 0.8f) };
                mb.AddTube(0, rp, new List<float> { trunkR * 0.7f, trunkR * 0.4f, trunkR * 0.15f }, 6);
            }
            // branches
            void Branch(Vector3 start, Vector3 dir, float len, float r, int depth)
            {
                var bp = new List<Vector3>(); var br = new List<float>();
                Vector3 p = start; Vector3 d = dir.normalized;
                int seg = 4;
                for (int i = 0; i <= seg; i++)
                {
                    bp.Add(p); br.Add(r * Mathf.Lerp(1f, 0.3f, i / (float)seg));
                    d = (d + new Vector3(R() - .5f, (R() - .35f) * 0.6f, R() - .5f) * 0.5f).normalized;
                    p += d * len / seg;
                }
                mb.AddTube(0, bp, br, depth > 1 ? 6 : 4, true);
                if (depth > 0)
                {
                    int kids = 2 + (R() > 0.5f ? 1 : 0);
                    for (int k = 0; k < kids; k++)
                    {
                        int idx = 2 + rnd.Next(seg - 1);
                        Vector3 nd = (d + new Vector3(R() - .5f, R() * 0.8f, R() - .5f) * 1.4f).normalized;
                        Branch(bp[idx], nd, len * 0.6f, br[idx] * 0.7f, depth - 1);
                    }
                }
            }
            int mainB = 3 + rnd.Next(3);
            for (int k = 0; k < mainB; k++)
            {
                int idx = 3 + rnd.Next(n - 3);
                float a = R() * Mathf.PI * 2;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0.5f + R() * 0.8f, Mathf.Sin(a));
                Branch(pts[idx], dir, 1.6f + R() * 1.8f, rad[idx] * 0.55f, 2);
            }
            return (mb.Build("deadtree" + seed), new[] { Pal.Bark });
        }

        static Material needleLight, needleDark;
        static Material NeedleLight => needleLight ? needleLight : needleLight = MatLib.Lit(new Color(.40f, .52f, .44f), TexId.Needles, .08f, 0, 3f, 1f);
        static Material NeedleDark => needleDark ? needleDark : needleDark = MatLib.Lit(new Color(.20f, .28f, .23f), TexId.Needles, .05f, 0, 3f, 1f);

        /// <summary>A Barovian fir: tiers of drooping branch clumps that read as a feathered star from the tactical camera.</summary>
        public static (Mesh, Material[]) Pine(int seed)
        {
            var rnd = new System.Random(seed);
            float R() => (float)rnd.NextDouble();
            var mb = new MeshBuilder(3);
            float h = 8f + R() * 6f;
            mb.AddCylinder(0, Vector3.zero, 0.3f, 0.06f, h, 8);
            int tiers = 7 + rnd.Next(3);
            for (int i = 0; i < tiers; i++)
            {
                float t = i / (float)(tiers - 1);
                float y = Mathf.Lerp(h * 0.2f, h * 0.92f, t);
                float r = Mathf.Lerp(2.6f, 0.5f, Mathf.Pow(t, 0.9f)) * (0.85f + R() * 0.3f);
                // dark core so gaps between clumps don't show the sky
                mb.AddCylinder(2, new Vector3(0, y - 0.9f, 0), r * 0.62f, 0.05f, 1.6f, 9, true);
                int clumps = Mathf.Max(4, Mathf.RoundToInt(Mathf.Lerp(9, 4, t)));
                float a0 = R() * Mathf.PI * 2;
                for (int k = 0; k < clumps; k++)
                {
                    float a = a0 + k * Mathf.PI * 2 / clumps + (R() - .5f) * 0.4f;
                    float len = r * (0.8f + R() * 0.35f);
                    float droop = 14f + R() * 14f - t * 6f;
                    mb.Push();
                    mb.Translate(new Vector3(0, y + (R() - .5f) * 0.3f, 0));
                    mb.Rotate(Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0));
                    mb.Rotate(Quaternion.Euler(0, 0, -droop));
                    int sd = seed * 31 + i * 7 + k;
                    float hl = len * 0.55f;
                    mb.AddEllipsoid(1, new Vector3(len * 0.5f, 0, 0), new Vector3(hl, 0.26f + R() * 0.1f, len * 0.26f + 0.15f), 5, 8, p =>
                    {
                        // feathered, slightly upturned tip
                        float along = Mathf.InverseLerp(-hl, hl, p.x);
                        float wig = 1f + 0.22f * Mathf.Sin(p.x * 9f + sd) * along;
                        return new Vector3(p.x, p.y + along * along * 0.18f, p.z * wig * Mathf.Lerp(0.7f, 1.1f, Mathf.Sin(along * Mathf.PI)));
                    });
                    mb.Pop();
                }
            }
            mb.AddCone(1, new Vector3(0, h * 0.9f, 0), 0.32f, h * 0.14f, 7);
            return (mb.Build("pine" + seed), new[] { Pal.Bark, NeedleLight, NeedleDark });
        }

        /// <summary>A lichen-spotted boulder, sometimes with a smaller companion stone.</summary>
        public static (Mesh, Material[]) Rock(int seed, float size)
        {
            var mb = new MeshBuilder(1) { uvScale = 0.6f };
            var rnd = new System.Random(seed);
            float R() => (float)rnd.NextDouble();
            void Stone(Vector3 c, float s, int sd)
            {
                float sx = 0.8f + R() * 0.6f, sz = 0.7f + R() * 0.6f, sy = 0.5f + R() * 0.25f;
                float cut = s * (0.35f + R() * 0.2f);
                mb.AddEllipsoid(0, c + new Vector3(0, s * 0.18f, 0), new Vector3(s * sx, s * sy, s * sz), 7, 11, p =>
                {
                    // chunky facets: noise quantised in steps, flattened crown, buried base
                    float n = Noise.Fbm(p.x * 0.25f / s + sd * 0.13f, p.z * 0.25f / s + p.y * 0.2f / s, 2, 3, sd);
                    float q = Mathf.Round(n * 5f) / 5f;
                    p *= 0.78f + q * 0.5f;
                    if (p.y > cut) p.y = Mathf.Lerp(p.y, cut, 0.65f);
                    if (p.y < -s * 0.12f) p.y = -s * 0.12f;
                    return p;
                });
            }
            Stone(Vector3.zero, size, seed);
            if (R() < 0.55f) Stone(new Vector3(R() - .5f, 0, R() - .5f).normalized * size * 1.2f, size * (0.35f + R() * 0.25f), seed + 5);
            return (mb.Build("rock" + seed), new[] { Pal.Rock });
        }

        /// <summary>A low shrub: a clump of leafy lobes. The seed picks the palette — evergreen, rust, or dead brown.</summary>
        public static (Mesh, Material[]) Bush(int seed)
        {
            var rnd = new System.Random(seed);
            float R() => (float)rnd.NextDouble();
            var mb = new MeshBuilder(1) { uvScale = 1.2f };
            int lobes = 4 + rnd.Next(4);
            float spread = 0.5f + R() * 0.4f;
            for (int i = 0; i < lobes; i++)
            {
                float a = R() * Mathf.PI * 2, d = R() * spread;
                float r = 0.35f + R() * 0.4f;
                var c = new Vector3(Mathf.Cos(a) * d, r * 0.75f + R() * 0.2f, Mathf.Sin(a) * d);
                int sd = seed * 13 + i;
                mb.AddEllipsoid(0, c, new Vector3(r, r * 0.8f, r), 6, 9, p =>
                {
                    float n = Noise.Fbm(p.x * 0.8f + sd * 0.07f, p.z * 0.8f + p.y * 0.6f, 3, 3, sd);
                    return p * (0.75f + n * 0.55f);
                });
            }
            Color[] tints = { new Color(.46f, .56f, .36f), new Color(.74f, .46f, .26f), new Color(.62f, .52f, .36f), new Color(.38f, .48f, .36f) };
            var m = MatLib.Lit(tints[seed % tints.Length], TexId.Foliage, .08f, 0, 2f, 1f);
            return (mb.Build("bush" + seed), new[] { m });
        }

        public static (Mesh, Material[]) Stump(int seed)
        {
            var mb = new MeshBuilder(2);
            mb.AddCylinder(0, Vector3.zero, 0.35f, 0.28f, 0.45f, 9, false);
            mb.AddDisc(1, new Vector3(0, 0.45f, 0), 0.28f, 9, true);
            return (mb.Build("stump"), new[] { Pal.Bark, Pal.Planks });
        }

        public static (Mesh, Material[]) FallenLog(int seed)
        {
            var mb = new MeshBuilder(1);
            var pts = new List<Vector3> { new Vector3(-1.8f, 0.25f, 0), new Vector3(0, 0.3f, 0.1f), new Vector3(1.8f, 0.22f, 0) };
            mb.AddTube(0, pts, new List<float> { 0.28f, 0.26f, 0.2f }, 9);
            return (mb.Build("log"), new[] { Pal.Bark });
        }

        static Texture2D _grass;
        public static Texture2D GrassTex
        {
            get
            {
                if (_grass) return _grass;
                int w = 128, h = 128;
                _grass = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "grassblades", wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[w * h];
                var rnd = new System.Random(5);
                for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);
                for (int b = 0; b < 26; b++)
                {
                    float x0 = 8 + (float)rnd.NextDouble() * (w - 16);
                    float lean = ((float)rnd.NextDouble() - 0.5f) * 40;
                    float bh = h * (0.45f + (float)rnd.NextDouble() * 0.55f);
                    float bw = 2.2f + (float)rnd.NextDouble() * 2f;
                    Color c = Color.Lerp(new Color(.45f, .42f, .26f), new Color(.62f, .55f, .34f), (float)rnd.NextDouble());
                    for (int y = 0; y < bh; y++)
                    {
                        float t = y / bh;
                        float cx = x0 + lean * t * t;
                        float ww = bw * (1 - t);
                        for (int x = Mathf.FloorToInt(cx - ww); x <= Mathf.CeilToInt(cx + ww); x++)
                        {
                            if (x < 0 || x >= w) continue;
                            float a = Mathf.Clamp01(ww - Mathf.Abs(x - cx) + 0.5f);
                            Color cc = c * Mathf.Lerp(0.55f, 1.1f, t);
                            cc.a = a;
                            px[y * w + x] = Color.Lerp(px[y * w + x], cc, a);
                        }
                    }
                }
                _grass.SetPixels32(px); _grass.Apply(true, true);
                return _grass;
            }
        }

        /// <summary>A fan of blades leaning outward so it still reads from the high camera.</summary>
        public static (Mesh, Material[]) GrassTuft(int seed)
        {
            var mb = new MeshBuilder(1);
            var rnd = new System.Random(seed);
            float R() => (float)rnd.NextDouble();
            int blades = 5 + rnd.Next(3);
            float a0 = R() * 360f;
            for (int k = 0; k < blades; k++)
            {
                float a = a0 + k * 360f / blades + (R() - .5f) * 25f;
                float w = 0.32f + R() * 0.16f, hgt = 0.42f + R() * 0.3f;
                float lean = 28f + R() * 22f;
                mb.Push(); mb.Rotate(Quaternion.Euler(0, a, 0)); mb.Rotate(Quaternion.Euler(lean, 0, 0));
                mb.AddQuad(0, new Vector3(-w / 2, 0, 0), new Vector3(w / 2, 0, 0), new Vector3(w / 2, hgt, 0), new Vector3(-w / 2, hgt, 0), false);
                mb.Pop();
            }
            // light the fan as if it were part of the ground so it doesn't flicker as the camera turns
            for (int i = 0; i < mb.normals.Count; i++) mb.normals[i] = (mb.normals[i] * 0.2f + Vector3.up).normalized;
            Color[] tints = { new Color(.82f, .78f, .62f), new Color(.72f, .74f, .55f), new Color(.86f, .74f, .56f) };
            return (mb.Build("grass"), new[] { MatLib.Cutout(GrassTex, tints[seed % tints.Length]) });
        }

        static Texture2D _fern;
        static Texture2D FernTex
        {
            get
            {
                if (_fern) return _fern;
                int w = 64, h = 256;
                _fern = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "fernfrond", wrapMode = TextureWrapMode.Clamp };
                var px = new Color[w * h];
                for (int y = 0; y < h; y++)
                {
                    float t = y / (float)h;
                    float half = (w * 0.46f) * Mathf.Sin(Mathf.Clamp01(t * 1.05f) * Mathf.PI) * (1 - t * 0.35f);
                    for (int x = 0; x < w; x++)
                    {
                        float dx = Mathf.Abs(x + 0.5f - w / 2f);
                        // leaflets: diagonal bands on each side of the stem
                        float band = Mathf.Repeat(y * 0.11f + dx * 0.09f, 1f);
                        bool leaf = dx < half && band > 0.18f;
                        bool stem = dx < 1.3f && t < 0.97f;
                        float a = stem ? 1 : leaf ? Mathf.Clamp01((half - dx) * 0.6f) : 0;
                        Color c = Color.Lerp(new Color(.35f, .42f, .22f), new Color(.55f, .6f, .32f), band) * Mathf.Lerp(0.7f, 1.05f, dx / (w * 0.5f));
                        if (stem) c = new Color(.3f, .3f, .16f);
                        c.a = a; px[y * w + x] = c;
                    }
                }
                _fern.SetPixels(px); _fern.Apply(true, true);
                return _fern;
            }
        }

        /// <summary>A bracken fern: fronds arching out from a crown, laid low over the ground.</summary>
        public static (Mesh, Material[]) Fern(int seed)
        {
            var mb = new MeshBuilder(1);
            var rnd = new System.Random(seed);
            float R() => (float)rnd.NextDouble();
            int fronds = 6 + rnd.Next(4);
            for (int k = 0; k < fronds; k++)
            {
                float a = k * 360f / fronds + (R() - .5f) * 30f;
                float len = 0.7f + R() * 0.5f, w = 0.26f + R() * 0.08f;
                float rise = 35f + R() * 25f;
                Quaternion q = Quaternion.Euler(0, a, 0) * Quaternion.Euler(90 - rise, 0, 0);
                // frond as two quads: rising, then arching back down
                Vector3 up = q * Vector3.up, side = q * Vector3.right;
                Vector3 mid = up * len * 0.55f;
                Vector3 tip = mid + (q * Quaternion.Euler(35, 0, 0) * Vector3.up) * len * 0.5f;
                mb.AddQuad(0, -side * w * 0.3f, side * w * 0.3f, mid + side * w * 0.5f, mid - side * w * 0.5f, false);
                mb.AddQuad(0, mid - side * w * 0.5f, mid + side * w * 0.5f, tip + side * w * 0.15f, tip - side * w * 0.15f, false);
            }
            for (int i = 0; i < mb.normals.Count; i++) mb.normals[i] = (mb.normals[i] * 0.3f + Vector3.up).normalized;
            Color[] tints = { new Color(.8f, .85f, .7f), new Color(.95f, .75f, .5f), new Color(.85f, .8f, .6f) };
            return (mb.Build("fern"), new[] { MatLib.Cutout(FernTex, tints[seed % tints.Length], 0.45f) });
        }

        // ------------------------------------------------------------ terrain
        public class TerrainSpec
        {
            public Vector3 size = new Vector3(200, 60, 200);
            public Vector3 origin;
            public int res = 257;
            public Func<float, float, float> height;               // world x,z -> world y
            public Func<float, float, float, float, float[]> splat; // x,z,height,slope -> weights (5 layers)
            public TexId[] layers = { TexId.DeadGrass, TexId.Dirt, TexId.Mud, TexId.Rock, TexId.ForestFloor };
            public float[] tiles = { 6f, 5f, 4f, 8f, 6f };
        }

        public static Terrain BuildTerrain(TerrainSpec s, Transform parent)
        {
            var td = new TerrainData();
            td.heightmapResolution = s.res;
            td.size = s.size;
            int r = td.heightmapResolution;
            var h = new float[r, r];
            for (int z = 0; z < r; z++)
                for (int x = 0; x < r; x++)
                {
                    float wx = s.origin.x + x / (float)(r - 1) * s.size.x;
                    float wz = s.origin.z + z / (float)(r - 1) * s.size.z;
                    h[z, x] = Mathf.Clamp01((s.height(wx, wz) - s.origin.y) / s.size.y);
                }
            td.SetHeights(0, 0, h);
            var tls = new TerrainLayer[s.layers.Length];
            for (int i = 0; i < tls.Length; i++)
            {
                var tp = ProcTex.Get(s.layers[i]);
                tls[i] = new TerrainLayer { diffuseTexture = tp.albedo, normalMapTexture = tp.normal, tileSize = Vector2.one * s.tiles[i], normalScale = 0.8f, smoothness = s.layers[i] == TexId.Mud ? 0.22f : 0.06f, smoothnessSource = TerrainLayerSmoothnessSource.ConstantOnly, metallic = 0 };
            }
            td.terrainLayers = tls;
            int ar = 256;
            td.alphamapResolution = ar;
            var al = new float[ar, ar, tls.Length];
            for (int z = 0; z < ar; z++)
                for (int x = 0; x < ar; x++)
                {
                    float nx = x / (float)(ar - 1), nz = z / (float)(ar - 1);
                    float wx = s.origin.x + nx * s.size.x, wz = s.origin.z + nz * s.size.z;
                    float hy = td.GetInterpolatedHeight(nx, nz) + s.origin.y;
                    float slope = td.GetSteepness(nx, nz);
                    var w = s.splat(wx, wz, hy, slope);
                    float sum = 0; for (int i = 0; i < tls.Length; i++) sum += Mathf.Max(0, w[i]);
                    if (sum <= 0) { w[0] = 1; sum = 1; }
                    for (int i = 0; i < tls.Length; i++) al[z, x, i] = Mathf.Max(0, w[i]) / sum;
                }
            td.SetAlphamaps(0, 0, al);
            var go = Terrain.CreateTerrainGameObject(td);
            go.name = "Terrain";
            go.transform.SetParent(parent, false);
            go.transform.position = s.origin;
            go.layer = Layers.Ground;
            var t = go.GetComponent<Terrain>();
            t.materialTemplate = MatLib.TerrainTemplate;
            t.heightmapPixelError = 4;
            t.basemapDistance = 150;
            t.drawInstanced = false;
            t.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return t;
        }

        public static float DistToPolyline(Vector2 p, IList<Vector2> line, out float along)
        {
            float best = float.MaxValue; along = 0; float acc = 0;
            for (int i = 0; i < line.Count - 1; i++)
            {
                Vector2 a = line[i], b = line[i + 1];
                Vector2 ab = b - a; float len = ab.magnitude;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / (len * len));
                float d = Vector2.Distance(p, a + ab * t);
                if (d < best) { best = d; along = acc + t * len; }
                acc += len;
            }
            return best;
        }

        /// <summary>Scatter objects over terrain with rejection by a density function.</summary>
        public static void Scatter(Terrain t, Transform parent, int count, int seed, Func<float, float, float> density, Func<int, (Mesh, Material[])> make, string key, int variants, Vector2 scaleRange, Kit.ColliderKind col, float minSpacing, List<Vector3> placed = null, float sink = 0.05f)
        {
            var rnd = new System.Random(seed);
            placed ??= new List<Vector3>();
            var size = t.terrainData.size; var o = t.transform.position;
            int tries = count * 8, made = 0;
            while (tries-- > 0 && made < count)
            {
                float x = o.x + (float)rnd.NextDouble() * size.x, z = o.z + (float)rnd.NextDouble() * size.z;
                if ((float)rnd.NextDouble() > density(x, z)) continue;
                bool ok = true;
                if (minSpacing > 0) foreach (var p in placed) if ((p.x - x) * (p.x - x) + (p.z - z) * (p.z - z) < minSpacing * minSpacing) { ok = false; break; }
                if (!ok) continue;
                float y = t.SampleHeight(new Vector3(x, 0, z)) + o.y;
                int v = rnd.Next(variants);
                float sc = Mathf.Lerp(scaleRange.x, scaleRange.y, (float)rnd.NextDouble());
                var go = Kit.Place(key + v, () => make(v), parent, new Vector3(x, y - sink, z) - parent.position, (float)rnd.NextDouble() * 360, sc, col);
                if (key.StartsWith("pine") || key.StartsWith("dead")) Occluders.Register(go.GetComponent<Renderer>(), true);
                placed.Add(new Vector3(x, y, z));
                made++;
            }
        }
    }
}
