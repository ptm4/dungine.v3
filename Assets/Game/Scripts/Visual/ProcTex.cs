using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dungine.Visual
{
    public enum TexId
    {
        Cobble, StoneBrick, WoodPlank, Plaster, Shingle, Dirt, DeadGrass, Rock, Bark, Fabric, Leather, Metal, Skin, Scales,
        Hair, ForestFloor, FloorBoards, Wallpaper, Mud, Slate, Thatch, Ashlar, Moss, Bone, Needles, Foliage
    }

    public struct TexPair { public Texture2D albedo, normal; }

    /// <summary>Generates every surface texture in the game from noise; results are cached per id.</summary>
    public static partial class ProcTex
    {
        static readonly Dictionary<TexId, TexPair> cache = new Dictionary<TexId, TexPair>();
        const int Size = 512;

        public static TexPair Get(TexId id)
        {
            if (cache.TryGetValue(id, out var p) && p.albedo && p.normal) return p;
            p = Generate(id);
            cache[id] = p;
            return p;
        }

        delegate void Pixel(float u, float v, out Color col, out float height);

        static TexPair Generate(TexId id)
        {
            Pixel fn; float normalStrength = 2f; int size = Size;
            switch (id)
            {
                case TexId.Cobble: fn = Cobble; normalStrength = 3f; break;
                case TexId.StoneBrick: fn = (float u, float v, out Color c, out float h) => Bricks(u, v, out c, out h, 4, 8, new Color(.50f, .48f, .45f), 11); normalStrength = 5f; break;
                case TexId.Ashlar: fn = (float u, float v, out Color c, out float h) => Bricks(u, v, out c, out h, 3, 5, new Color(.58f, .56f, .52f), 23); normalStrength = 4f; break;
                case TexId.WoodPlank: fn = (float u, float v, out Color c, out float h) => Planks(u, v, out c, out h, 5, new Color(.42f, .30f, .20f), false); normalStrength = 3f; break;
                case TexId.FloorBoards: fn = (float u, float v, out Color c, out float h) => Planks(u, v, out c, out h, 6, new Color(.34f, .23f, .15f), true); normalStrength = 3f; break;
                case TexId.Plaster: fn = Plaster; normalStrength = 1.5f; break;
                case TexId.Shingle: fn = (float u, float v, out Color c, out float h) => Shingles(u, v, out c, out h, new Color(.30f, .24f, .21f), 8, 10); normalStrength = 4f; break;
                case TexId.Slate: fn = (float u, float v, out Color c, out float h) => Shingles(u, v, out c, out h, new Color(.24f, .25f, .28f), 10, 14); normalStrength = 4f; break;
                case TexId.Thatch: fn = Thatch; normalStrength = 3f; break;
                case TexId.Dirt: fn = Dirt; normalStrength = 1.2f; break;
                case TexId.Mud: fn = Mud; normalStrength = 1f; break;
                case TexId.DeadGrass: fn = DeadGrass; normalStrength = 0.8f; break;
                case TexId.Rock: fn = Rock; normalStrength = 2.5f; break;
                case TexId.Bark: fn = Bark; normalStrength = 5f; break;
                case TexId.Fabric: fn = Fabric; normalStrength = 1.5f; size = 256; break;
                case TexId.Leather: fn = Leather; normalStrength = 1.5f; size = 256; break;
                case TexId.Metal: fn = Metal; normalStrength = 1f; size = 256; break;
                case TexId.Skin: fn = Skin; normalStrength = 0.6f; size = 256; break;
                case TexId.Scales: fn = Scales; normalStrength = 3f; size = 256; break;
                case TexId.Hair: fn = Hair; normalStrength = 2f; size = 256; break;
                case TexId.ForestFloor: fn = ForestFloor; normalStrength = 1f; break;
                case TexId.Wallpaper: fn = Wallpaper; normalStrength = 0.8f; break;
                case TexId.Moss: fn = Moss; normalStrength = 2f; break;
                case TexId.Bone: fn = BoneTex; normalStrength = 1.5f; size = 256; break;
                case TexId.Needles: fn = NeedlesBase; normalStrength = 1.2f; size = 256; break;
                case TexId.Foliage: fn = FoliageBase; normalStrength = 1.5f; size = 256; break;
                default: fn = Plaster; break;
            }
            var cols = new Color[size * size];
            var heights = new float[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = (y + 0.5f) / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    fn(u, v, out var c, out var h);
                    cols[y * size + x] = c;
                    heights[y * size + x] = h;
                }
            }
            PostFor(id)?.Invoke(cols, heights, size);
            var px32 = new Color32[cols.Length];
            for (int i = 0; i < cols.Length; i++) { var c = cols[i]; c.a = 1; px32[i] = c; }
            var albedo = new Texture2D(size, size, TextureFormat.RGBA32, true, false) { name = id + "_albedo", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
            albedo.SetPixels32(px32);
            albedo.Apply(true, true);
            var normal = NormalFromHeight(heights, size, normalStrength);
            normal.name = id + "_normal";
            return new TexPair { albedo = albedo, normal = normal };
        }

        public static Texture2D NormalFromHeight(float[] h, int size, float strength)
        {
            var n = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float l = h[y * size + (x - 1 + size) % size], r = h[y * size + (x + 1) % size];
                    float d = h[((y - 1 + size) % size) * size + x], u = h[((y + 1) % size) * size + x];
                    Vector3 nn = new Vector3((l - r) * strength, (d - u) * strength, 1f / size * 64f).normalized;
                    n[y * size + x] = new Color((nn.x * .5f + .5f), (nn.y * .5f + .5f), (nn.z * .5f + .5f), 1f);
                }
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
            t.SetPixels32(n);
            t.Apply(true, true);
            return t;
        }

        static Color Vary(Color c, float t, float amt) => new Color(c.r * (1 + (t - .5f) * amt), c.g * (1 + (t - .5f) * amt), c.b * (1 + (t - .5f) * amt), 1);

        // ------------------ generators ------------------

        static void Cobble(float u, float v, out Color col, out float h)
        {
            var cell = Noise.Voronoi(u, v, 12, 101, 0.85f);
            float edge = cell.f2 - cell.f1;
            float stone = MathX.Smoothstep(0.03f, 0.14f, edge);
            float dome = 1 - MathX.Smoothstep(0.1f, 0.55f, cell.f1);
            float rnd = Noise.Hash01(cell.id, 3, 5);
            float grime = Noise.Fbm(u, v, 5, 5, 7);
            Color stoneC = Color.Lerp(new Color(.40f, .38f, .35f), new Color(.55f, .51f, .46f), rnd);
            if (Noise.Hash01(cell.id, 9, 2) < 0.15f) stoneC = Color.Lerp(stoneC, new Color(.46f, .38f, .30f), 0.6f);   // the odd brown stone
            stoneC = Vary(stoneC, Noise.Fbm(u, v, 40, 3, 9), 0.3f) * Mathf.Lerp(0.88f, 1.06f, dome);
            Color gap = Color.Lerp(new Color(.22f, .19f, .15f), new Color(.30f, .26f, .20f), grime);
            col = Color.Lerp(gap, stoneC, stone);
            col = Color.Lerp(col, new Color(.26f, .22f, .17f), MathX.Smoothstep(.55f, .8f, grime) * 0.45f);
            h = Mathf.Sqrt(stone) * 0.7f + dome * 0.25f + Noise.Fbm(u, v, 32, 3, 13) * 0.08f;
        }

        static void Bricks(float u, float v, out Color col, out float h, int rows, int cols, Color baseC, int seed)
        {
            float rv = v * rows;
            int row = Mathf.FloorToInt(rv);
            float fu = u * cols + (row % 2) * 0.5f;
            int colI = Mathf.FloorToInt(fu);
            float bx = fu - colI, by = rv - row;
            float mortar = 0.05f;
            float wob = (Noise.Fbm(u, v, 16, 3, seed) - .5f) * 0.04f;
            float ex = Mathf.Min(bx, 1 - bx) + wob, ey = Mathf.Min(by, 1 - by) * cols / rows * 0.5f + wob;
            float e = Mathf.Min(ex, ey);
            float brick = MathX.Smoothstep(mortar * 0.6f, mortar * 1.6f, e);
            int bid = (colI % cols + cols) % cols * 131 + row;
            float rnd = Noise.Hash01(bid, seed, 9);
            Color b = Vary(baseC, rnd, 0.35f);
            b = Vary(b, Noise.Fbm(u, v, 20, 4, seed + 1), 0.45f);
            float chip = Noise.Ridged(u, v, 8, 3, seed + 2);
            Color mortarC = new Color(.26f, .24f, .21f);
            col = Color.Lerp(mortarC, b, brick);
            float grime = Noise.Fbm(u, v, 4, 4, seed + 3);
            col = Color.Lerp(col, col * 0.6f, MathX.Smoothstep(.5f, .8f, grime));
            h = brick * (0.8f + 0.2f * Noise.Fbm(u, v, 24, 3, seed + 4)) - chip * 0.08f;
        }

        static void Planks(float u, float v, out Color col, out float h, int planks, Color baseC, bool horizontal)
        {
            float a = horizontal ? v : u, b = horizontal ? u : v;
            float pa = a * planks;
            int p = Mathf.FloorToInt(pa);
            float fa = pa - p;
            // stagger plank ends
            float endOff = Noise.Hash01(p, 1, 77);
            float lb = b * 2 + endOff;
            int seg = Mathf.FloorToInt(lb);
            float fb = lb - seg;
            float gap = Mathf.Min(MathX.Smoothstep(0, 0.04f, fa), MathX.Smoothstep(0, 0.04f, 1 - fa));
            gap = Mathf.Min(gap, Mathf.Min(MathX.Smoothstep(0, 0.012f, fb), MathX.Smoothstep(0, 0.012f, 1 - fb)));
            float rnd = Noise.Hash01(p, seg, 5);
            float grain = Noise.Fbm(horizontal ? u * 0.25f : u * 4f, horizontal ? v * 4f : v * 0.25f, 8, 4, p * 3 + seg);
            float rings = Mathf.Sin((fa * 6 + grain * 8 + rnd * 10) * Mathf.PI) * 0.5f + 0.5f;
            Color c = Vary(baseC, rnd, 0.5f);
            c = Color.Lerp(c * 0.75f, c * 1.15f, rings * 0.6f + grain * 0.4f);
            float wear = Noise.Fbm(u, v, 5, 4, 91);
            c = Color.Lerp(c, c * 0.55f, MathX.Smoothstep(.55f, .85f, wear));
            col = Color.Lerp(new Color(.07f, .05f, .04f), c, gap);
            h = gap * (0.85f + rings * 0.1f + grain * 0.05f);
            // nail heads
            float nx = fa - 0.5f, ny = fb - 0.06f, ny2 = fb - 0.94f;
            float nd = Mathf.Min(nx * nx + ny * ny * 4, nx * nx + ny2 * ny2 * 4);
            if (horizontal && nd < 0.0016f) { col = new Color(.12f, .11f, .1f); h += 0.05f; }
        }

        static void Plaster(float u, float v, out Color col, out float h)
        {
            float n = Noise.Fbm(u, v, 4, 6, 201);
            float fine = Noise.Fbm(u, v, 32, 3, 202);
            float stain = Noise.Fbm(u, v, 3, 5, 203);
            Color c = Color.Lerp(new Color(.62f, .58f, .50f), new Color(.72f, .69f, .62f), n);
            c = Color.Lerp(c, new Color(.40f, .36f, .30f), MathX.Smoothstep(.55f, .85f, stain) * .7f);
            float crack = Noise.Ridged(u, v, 3, 5, 204);
            float cr = MathX.Smoothstep(.86f, .96f, crack);
            c = Color.Lerp(c, new Color(.25f, .22f, .19f), cr * .7f);
            col = Vary(c, fine, 0.12f);
            h = n * 0.3f + fine * 0.2f - cr * 0.4f;
        }

        static void Shingles(float u, float v, out Color col, out float h, Color baseC, int rows, int cols)
        {
            float rv = v * rows;
            int row = Mathf.FloorToInt(rv);
            float by = rv - row;
            float fu = u * cols + (row % 2) * 0.5f + Noise.Hash01(row, 0, 3) * 0.2f;
            int ci = Mathf.FloorToInt(fu);
            float bx = fu - ci;
            float rnd = Noise.Hash01(ci, row, 41);
            float side = MathX.Smoothstep(0, 0.06f, Mathf.Min(bx, 1 - bx));
            // tiles overlap: bottom edge (low by) is exposed edge
            float shade = Mathf.Lerp(0.55f, 1.05f, MathX.Smoothstep(0, 0.35f, by));
            Color c = Vary(baseC, rnd, 0.5f);
            c = Vary(c, Noise.Fbm(u, v, 16, 3, 43), 0.4f);
            float moss = Noise.Fbm(u, v, 3, 5, 44);
            c = Color.Lerp(c, new Color(.22f, .26f, .15f), MathX.Smoothstep(.6f, .85f, moss) * .6f);
            col = c * shade * Mathf.Lerp(0.4f, 1f, side); col.a = 1;
            h = by * 0.6f * side + Noise.Fbm(u, v, 24, 2, 45) * .15f;
        }

        static void Thatch(float u, float v, out Color col, out float h)
        {
            float streak = Noise.Fbm(u * 8, v * 0.5f, 8, 4, 301);
            float rows = Mathf.Repeat(v * 6, 1);
            Color c = Color.Lerp(new Color(.30f, .25f, .15f), new Color(.52f, .44f, .27f), streak);
            c *= Mathf.Lerp(0.6f, 1f, MathX.Smoothstep(0, .4f, rows));
            col = c; col.a = 1;
            h = streak * .6f + rows * .4f;
        }

        static void Bark(float u, float v, out Color col, out float h)
        {
            float r = Noise.Ridged(u * 1, v * 0.25f, 12, 4, 701);
            float n = Noise.Fbm(u, v, 6, 4, 702);
            Color c = Color.Lerp(new Color(.13f, .11f, .10f), new Color(.32f, .28f, .24f), r * .7f + n * .3f);
            col = c; h = r;
        }

        static void Fabric(float u, float v, out Color col, out float h)
        {
            float wx = Mathf.Sin(u * Mathf.PI * 2 * 96) * .5f + .5f;
            float wy = Mathf.Sin(v * Mathf.PI * 2 * 96) * .5f + .5f;
            float weave = (Mathf.Repeat(Mathf.Floor(u * 96) + Mathf.Floor(v * 96), 2) < 1) ? wx : wy;
            float n = Noise.Fbm(u, v, 4, 4, 801);
            float c = Mathf.Lerp(0.72f, 0.95f, weave * .5f + n * .5f);
            col = new Color(c, c, c); h = weave * .5f + n * .5f;
        }

        static void Leather(float u, float v, out Color col, out float h)
        {
            float n = Noise.Fbm(u, v, 8, 5, 901);
            float pores = Noise.Voronoi(u, v, 48, 902).f1;
            float c = Mathf.Lerp(0.7f, 0.95f, n) - (1 - MathX.Smoothstep(0.05f, 0.2f, pores)) * .1f;
            float scuff = MathX.Smoothstep(.6f, .85f, Noise.Fbm(u, v, 3, 4, 903));
            c = Mathf.Lerp(c, c * 0.7f, scuff);
            col = new Color(c, c, c); h = n * .6f + pores * .4f;
        }

        static void Metal(float u, float v, out Color col, out float h)
        {
            var dent = Noise.Voronoi(u, v, 22, 1001, 0.9f);
            float hammer = MathX.Smoothstep(0f, 0.5f, dent.f1);
            float n = Noise.Fbm(u, v, 4, 4, 1002);
            float fine = Noise.Fbm(u, v, 32, 2, 1004);
            float scratch = MathX.Smoothstep(.93f, .98f, Noise.Ridged(u, v, 5, 3, 1003));
            float c = Mathf.Lerp(0.78f, 0.95f, n * .6f + fine * .4f) - scratch * .08f - (1 - hammer) * 0.02f;
            col = new Color(c, c, c); h = hammer * .15f + n * .3f - scratch * .2f;
        }

        static void Skin(float u, float v, out Color col, out float h)
        {
            float n = Noise.Fbm(u, v, 12, 4, 1101);
            float c = Mathf.Lerp(0.9f, 1f, n);
            col = new Color(c, c * .985f, c * .975f); h = n;
        }

        static void Scales(float u, float v, out Color col, out float h)
        {
            var cell = Noise.Voronoi(u, v, 24, 1201, 0.5f);
            float e = MathX.Smoothstep(0.0f, 0.25f, cell.f2 - cell.f1);
            float c = Mathf.Lerp(0.55f, 1f, e) * Mathf.Lerp(0.9f, 1f, Noise.Hash01(cell.id, 1, 2));
            col = new Color(c, c, c); h = Mathf.Sqrt(e);
        }

        static void Hair(float u, float v, out Color col, out float h)
        {
            float s1 = Noise.Fbm(u, v * 0.06f, 48, 3, 1301);
            float s2 = Noise.Fbm(u, v * 0.1f, 96, 2, 1302);
            float c = Mathf.Lerp(0.72f, 1f, s1 * .6f + s2 * .4f);
            col = new Color(c, c, c); h = s1 * .7f + s2 * .3f;
        }

        static void Wallpaper(float u, float v, out Color col, out float h)
        {
            // Damask-like repeating motif: symmetric lobes in each tile
            float tu = Mathf.Repeat(u * 4, 1) - .5f, tv = Mathf.Repeat(v * 3, 1) - .5f;
            float au = Mathf.Abs(tu);
            float lobe = Mathf.Sin(au * 14 + Mathf.Cos(tv * 10) * 1.5f) * Mathf.Cos(tv * 9) ;
            float motif = MathX.Smoothstep(0.35f, 0.55f, lobe) * (1 - MathX.Smoothstep(0.35f, 0.5f, Mathf.Sqrt(au * au + tv * tv)));
            float stripe = MathX.Smoothstep(0.46f, 0.49f, Mathf.Abs(Mathf.Repeat(u * 8, 1) - .5f));
            Color bg = new Color(.22f, .08f, .09f), fg = new Color(.36f, .20f, .14f);
            Color c = Color.Lerp(bg, fg, motif * .8f + stripe * .3f);
            float stain = Noise.Fbm(u, v, 3, 5, 1501);
            c = Color.Lerp(c, new Color(.12f, .09f, .07f), MathX.Smoothstep(.5f, .85f, stain) * .8f);
            float peel = MathX.Smoothstep(.78f, .82f, Noise.Fbm(u, v, 5, 4, 1502));
            c = Color.Lerp(c, new Color(.45f, .40f, .33f), peel);
            col = c; h = motif * .3f - peel * .3f + stain * .1f;
        }

        static void Moss(float u, float v, out Color col, out float h)
        {
            float n = Noise.Fbm(u, v, 8, 5, 1601);
            Color c = Color.Lerp(new Color(.10f, .14f, .06f), new Color(.26f, .32f, .14f), n);
            col = c; h = n;
        }

        static void BoneTex(float u, float v, out Color col, out float h)
        {
            float n = Noise.Fbm(u, v, 6, 5, 1701);
            Color c = Color.Lerp(new Color(.62f, .58f, .48f), new Color(.86f, .83f, .72f), n);
            col = c; h = n;
        }

        // ---------------- utility textures ----------------

        static Texture2D _soft, _ring, _fog, _white, _grad, _spark, _dot;

        public static Texture2D White
        {
            get
            {
                if (_white) return _white;
                _white = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var c = new Color32[16]; for (int i = 0; i < 16; i++) c[i] = new Color32(255, 255, 255, 255);
                _white.SetPixels32(c); _white.Apply(); return _white;
            }
        }

        /// <summary>Radial soft disc (alpha falloff), for glows and blob shadows.</summary>
        public static Texture2D SoftDisc
        {
            get
            {
                if (_soft) return _soft;
                _soft = Radial(128, d => { float t = Mathf.Clamp01(1 - d); return t * t * (3 - 2 * t); });
                _soft.name = "SoftDisc";
                return _soft;
            }
        }

        public static Texture2D Dot
        {
            get
            {
                if (_dot) return _dot;
                _dot = Radial(64, d => Mathf.Clamp01((1 - d) * 3f));
                return _dot;
            }
        }

        /// <summary>Selection ring: bright thin circle with soft glow.</summary>
        public static Texture2D Ring
        {
            get
            {
                if (_ring) return _ring;
                _ring = Radial(256, d =>
                {
                    float core = Mathf.Exp(-Mathf.Pow((d - 0.86f) / 0.03f, 2));
                    float glow = Mathf.Exp(-Mathf.Pow((d - 0.84f) / 0.12f, 2)) * 0.35f;
                    float inner = d < 0.84f ? MathX.Smoothstep(0.3f, 0.84f, d) * 0.12f : 0;
                    return Mathf.Clamp01(core + glow + inner);
                });
                _ring.name = "Ring";
                return _ring;
            }
        }

        public static Texture2D Spark
        {
            get
            {
                if (_spark) return _spark;
                int s = 64; _spark = new Texture2D(s, s, TextureFormat.RGBA32, true);
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                    {
                        float dx = (x + .5f) / s * 2 - 1, dy = (y + .5f) / s * 2 - 1;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float star = Mathf.Max(0, 1 - Mathf.Abs(dx) * 8) * Mathf.Max(0, 1 - Mathf.Abs(dy)) + Mathf.Max(0, 1 - Mathf.Abs(dy) * 8) * Mathf.Max(0, 1 - Mathf.Abs(dx));
                        float a = Mathf.Clamp01(Mathf.Exp(-d * d * 12) + star * 0.6f);
                        px[y * s + x] = new Color(1, 1, 1, a);
                    }
                _spark.SetPixels32(px); _spark.Apply(true, true); _spark.wrapMode = TextureWrapMode.Clamp;
                return _spark;
            }
        }

        /// <summary>Soft cloudy puff for fog and smoke particles.</summary>
        public static Texture2D FogPuff
        {
            get
            {
                if (_fog) return _fog;
                int s = 128; _fog = new Texture2D(s, s, TextureFormat.RGBA32, true);
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                    {
                        float u = (x + .5f) / s, v = (y + .5f) / s;
                        float dx = u * 2 - 1, dy = v * 2 - 1;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float n = Noise.Fbm(u, v, 3, 5, 77);
                        float a = Mathf.Clamp01(1 - d) ; a = a * a * (3 - 2 * a);
                        a *= Mathf.Lerp(0.3f, 1.2f, n);
                        px[y * s + x] = new Color(1, 1, 1, Mathf.Clamp01(a));
                    }
                _fog.SetPixels32(px); _fog.Apply(true, true); _fog.wrapMode = TextureWrapMode.Clamp;
                return _fog;
            }
        }

        public static Texture2D Radial(int s, Func<float, float> alpha)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                {
                    float dx = (x + .5f) / s * 2 - 1, dy = (y + .5f) / s * 2 - 1;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    px[y * s + x] = new Color(1, 1, 1, Mathf.Clamp01(alpha(d)));
                }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        /// <summary>Vertical gradient texture (for UI and skies).</summary>
        public static Texture2D Gradient(Color bottom, Color top, int h = 64)
        {
            var t = new Texture2D(2, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++) { var c = Color.Lerp(bottom, top, y / (float)(h - 1)); t.SetPixel(0, y, c); t.SetPixel(1, y, c); }
            t.Apply();
            return t;
        }
    }
}
