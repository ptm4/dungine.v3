using UnityEngine;

namespace Dungine.Visual
{
    /// <summary>
    /// Ground textures seen from the high tactical camera. Each starts from a noise base and is then
    /// covered in stamped detail — grass blades, leaves, needles, pebbles — drawn with wrap-around so
    /// the result still tiles.
    /// </summary>
    public static partial class ProcTex
    {
        delegate void Post(Color[] col, float[] h, int size);

        static Post PostFor(TexId id)
        {
            switch (id)
            {
                case TexId.DeadGrass: return GrassBlades;
                case TexId.ForestFloor: return LeafLitter;
                case TexId.Dirt: return (c, h, s) => Pebbles(c, h, s, 1300, 405, 1.2f, 4.5f, new Color(.46f, .42f, .37f), new Color(.30f, .27f, .23f));
                case TexId.Mud: return (c, h, s) => Pebbles(c, h, s, 420, 455, 1f, 3.2f, new Color(.36f, .31f, .26f), new Color(.22f, .19f, .16f));
                case TexId.Needles: return NeedleStrokes;
                case TexId.Foliage: return LeafClusters;
                default: return null;
            }
        }

        // ------------------------------------------------------------------ bases

        static void Dirt(float u, float v, out Color col, out float h)
        {
            float macro = Noise.Fbm(u, v, 2, 4, 400);
            float n = Noise.Fbm(u, v, 6, 5, 401);
            float grain = Noise.Fbm(u, v, 64, 2, 403);
            float damp = MathX.Smoothstep(.5f, .72f, Noise.Fbm(u, v, 3, 4, 404));
            Color c = Color.Lerp(new Color(.30f, .25f, .19f), new Color(.45f, .39f, .30f), n);
            c = Color.Lerp(c, new Color(.40f, .34f, .27f), macro * .5f);
            c = Color.Lerp(c, c * .7f, damp * .6f);
            c = Vary(c, grain, .28f);
            col = c; h = n * .5f + grain * .25f - damp * .15f;
        }

        static void Mud(float u, float v, out Color col, out float h)
        {
            float n = Noise.Fbm(u, v, 4, 6, 451);
            float wet = MathX.Smoothstep(.52f, .66f, Noise.Fbm(u, v, 3, 5, 452));
            var vc = Noise.Voronoi(u, v, 36, 453, .9f);
            float crack = (1 - MathX.Smoothstep(0.0f, 0.05f, vc.f2 - vc.f1)) * MathX.Smoothstep(.45f, .6f, Noise.Fbm(u, v, 3, 3, 456));
            float grain = Noise.Fbm(u, v, 72, 2, 454);
            Color c = Color.Lerp(new Color(.25f, .20f, .15f), new Color(.38f, .31f, .23f), n);
            c = Color.Lerp(c, new Color(.17f, .13f, .10f), wet * .5f);
            c = Color.Lerp(c, c * .7f, crack * (1 - wet) * .45f);
            c = Vary(c, grain, .22f);
            col = c; h = n * .6f * (1 - wet * .7f) - crack * .2f + grain * .1f;
        }

        static void DeadGrass(float u, float v, out Color col, out float h)
        {
            float macro = Noise.Fbm(u, v, 2, 4, 500);
            float patches = Noise.Fbm(u, v, 5, 4, 503);
            float n = Noise.Fbm(u, v, 16, 3, 501);
            // soil showing through between the blades
            Color soil = Color.Lerp(new Color(.17f, .14f, .10f), new Color(.24f, .20f, .14f), n);
            Color under = GrassHue(macro, patches) * .55f;
            col = Color.Lerp(soil, under, .75f);
            h = n * .3f;
        }

        static Color GrassHue(float macro, float patch)
        {
            // olive-green through straw to russet: late autumn in a valley with no sun
            Color olive = new Color(.34f, .36f, .18f), straw = new Color(.52f, .46f, .27f), russet = new Color(.42f, .30f, .17f), sage = new Color(.30f, .34f, .24f);
            Color c = Color.Lerp(olive, straw, MathX.Smoothstep(.35f, .75f, macro));
            c = Color.Lerp(c, russet, MathX.Smoothstep(.55f, .8f, patch) * .6f);
            c = Color.Lerp(c, sage, MathX.Smoothstep(.6f, .2f, patch) * .35f);
            return c;
        }

        static void ForestFloor(float u, float v, out Color col, out float h)
        {
            float n = Noise.Fbm(u, v, 5, 5, 1401);
            float grain = Noise.Fbm(u, v, 64, 2, 1403);
            Color soil = Color.Lerp(new Color(.14f, .11f, .08f), new Color(.25f, .20f, .14f), n);
            col = Vary(soil, grain, .3f); h = n * .4f + grain * .2f;
        }

        static void Rock(float u, float v, out Color col, out float h)
        {
            float n = Noise.Fbm(u, v, 3, 6, 601);
            float warp = Noise.Fbm(u, v, 2, 3, 604) * 2f;
            float strata = Mathf.Sin((v * 9 + warp * 2.2f + n * 1.5f) * Mathf.PI) * .5f + .5f;
            float cracks = Noise.Ridged(u, v, 5, 5, 602);
            float crack = MathX.Smoothstep(.72f, .92f, cracks);
            var cell = Noise.Voronoi(u, v, 7, 605, .9f);
            float facet = Noise.Hash01(cell.id, 5, 5);
            Color c = Color.Lerp(new Color(.36f, .35f, .34f), new Color(.56f, .55f, .52f), n);
            c *= Mathf.Lerp(.86f, 1.1f, facet);
            c = Color.Lerp(c, c * .78f, strata * .35f);
            c = Color.Lerp(c, new Color(.16f, .16f, .15f), crack * .85f);
            float lichen = MathX.Smoothstep(.62f, .74f, Noise.Fbm(u, v, 9, 4, 603));
            float lichen2 = MathX.Smoothstep(.7f, .78f, Noise.Fbm(u, v, 13, 3, 606));
            c = Color.Lerp(c, new Color(.46f, .5f, .36f), lichen * .55f);
            c = Color.Lerp(c, new Color(.62f, .5f, .3f), lichen2 * .4f);
            col = c; h = n * .45f + facet * .2f + strata * .1f - crack * .45f + lichen * .05f;
        }

        // Needles and Foliage are near-white so the material colour tints them: one texture serves
        // blue-green firs, autumn bushes and dead brown shrubs alike.
        static void NeedlesBase(float u, float v, out Color col, out float h)
        {
            float n = Noise.Fbm(u, v, 4, 4, 1801);
            col = Color.Lerp(new Color(.42f, .44f, .42f), new Color(.6f, .62f, .6f), n); h = n * .3f;
        }

        static void FoliageBase(float u, float v, out Color col, out float h)
        {
            float n = Noise.Fbm(u, v, 5, 4, 1901);
            col = Color.Lerp(new Color(.35f, .35f, .35f), new Color(.55f, .55f, .55f), n); h = n * .3f;
        }

        static void NeedleStrokes(Color[] col, float[] h, int size)
        {
            var rnd = new System.Random(1811);
            float R() => (float)rnd.NextDouble();
            for (int k = 0; k < size * size / 14; k++)
            {
                float x = R() * size, y = R() * size;
                // needles run mostly along v (down the branch) with some spread
                float ang = Mathf.PI * .5f + (R() - .5f) * 1.1f;
                float g = Mathf.Lerp(.55f, 1.25f, R());
                Stroke(col, h, size, x, y, ang, 4 + R() * 6, .8f, .35f, new Color(.7f, .74f, .7f) * g, .5f + R() * .5f);
            }
        }

        static void LeafClusters(Color[] col, float[] h, int size)
        {
            var rnd = new System.Random(1911);
            float R() => (float)rnd.NextDouble();
            for (int k = 0; k < size * size / 40; k++)
            {
                float x = R() * size, y = R() * size;
                float s = 2.5f + R() * 3.5f;
                Blob(col, h, size, x, y, s, s * (.5f + R() * .2f), R() * 6.3f, new Color(.72f, .72f, .72f) * Mathf.Lerp(.6f, 1.25f, R()), .5f + R() * .5f, true, .45f);
            }
        }

        // ------------------------------------------------------------------ stamping

        /// <summary>Blends a tapered stroke (a blade, needle or twig) into the buffers, wrapping at the edges.</summary>
        static void Stroke(Color[] col, float[] hb, int size, float x0, float y0, float ang, float len, float w0, float w1, Color c, float hAdd, float alpha = 1f)
        {
            float dx = Mathf.Cos(ang), dy = Mathf.Sin(ang);
            float x1 = x0 + dx * len, y1 = y0 + dy * len;
            int minX = Mathf.FloorToInt(Mathf.Min(x0, x1) - w0 - 1), maxX = Mathf.CeilToInt(Mathf.Max(x0, x1) + w0 + 1);
            int minY = Mathf.FloorToInt(Mathf.Min(y0, y1) - w0 - 1), maxY = Mathf.CeilToInt(Mathf.Max(y0, y1) + w0 + 1);
            for (int py = minY; py <= maxY; py++)
                for (int px = minX; px <= maxX; px++)
                {
                    float rx = px + .5f - x0, ry = py + .5f - y0;
                    float t = Mathf.Clamp01((rx * dx + ry * dy) / len);
                    float ex = rx - dx * len * t, ey = ry - dy * len * t;
                    float d = Mathf.Sqrt(ex * ex + ey * ey);
                    float w = Mathf.Lerp(w0, w1, t);
                    float a = Mathf.Clamp01(w - d + .5f) * alpha;
                    if (a <= 0) continue;
                    int ix = ((px % size) + size) % size, iy = ((py % size) + size) % size, i = iy * size + ix;
                    Color cc = c * Mathf.Lerp(.75f, 1.12f, t);
                    col[i] = Color.Lerp(col[i], cc, a);
                    hb[i] = Mathf.Lerp(hb[i], hb[i] * .3f + hAdd * (.4f + t * .6f), a);
                }
        }

        /// <summary>A filled, shaded ellipse (pebble or leaf) blended into the buffers.</summary>
        static void Blob(Color[] col, float[] hb, int size, float cx, float cy, float rx, float ry, float ang, Color c, float hAdd, bool midrib, float shade = .35f)
        {
            float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
            float r = Mathf.Max(rx, ry) + 1;
            for (int py = Mathf.FloorToInt(cy - r); py <= Mathf.CeilToInt(cy + r); py++)
                for (int px = Mathf.FloorToInt(cx - r); px <= Mathf.CeilToInt(cx + r); px++)
                {
                    float ox = px + .5f - cx, oy = py + .5f - cy;
                    float lx = (ox * ca + oy * sa) / rx, ly = (-ox * sa + oy * ca) / ry;
                    float d = lx * lx + ly * ly;
                    if (d > 1.15f) continue;
                    float a = Mathf.Clamp01((1.15f - d) * 4f);
                    int ix = ((px % size) + size) % size, iy = ((py % size) + size) % size, i = iy * size + ix;
                    // light from the upper left
                    float lit = 1 + (-ox / r * .5f + oy / r * .5f) * shade;
                    Color cc = c * lit;
                    if (midrib && Mathf.Abs(ly) < .12f) cc *= .72f;
                    col[i] = Color.Lerp(col[i], cc, a);
                    hb[i] = Mathf.Lerp(hb[i], hAdd * (1 - d * .6f), a);
                }
        }

        static void GrassBlades(Color[] col, float[] h, int size)
        {
            var rnd = new System.Random(511);
            float R() => (float)rnd.NextDouble();
            int count = size * size / 26;
            for (int k = 0; k < count; k++)
            {
                float x = R() * size, y = R() * size;
                float u = x / size, v = y / size;
                float macro = Noise.Fbm(u, v, 2, 4, 500), patch = Noise.Fbm(u, v, 5, 4, 503);
                // clumps: blades radiate loosely from shared roots
                float ang = R() * Mathf.PI * 2;
                float len = 5f + R() * 11f;
                Color c = GrassHue(macro, patch) * Mathf.Lerp(.72f, 1.18f, R());
                if (R() < .08f) c = Color.Lerp(c, new Color(.62f, .56f, .38f), .6f);   // pale dry blade
                Stroke(col, h, size, x, y, ang, len, 1.3f + R() * .5f, .25f, c, .6f + R() * .4f);
            }
        }

        static void LeafLitter(Color[] col, float[] h, int size)
        {
            var rnd = new System.Random(1411);
            float R() => (float)rnd.NextDouble();
            Color[] leafCols = { new Color(.48f, .26f, .10f), new Color(.40f, .30f, .13f), new Color(.30f, .18f, .09f), new Color(.52f, .40f, .17f), new Color(.24f, .17f, .11f), new Color(.46f, .20f, .10f) };
            // pine needles first (under the leaves)
            for (int k = 0; k < size * size / 60; k++)
            {
                float x = R() * size, y = R() * size;
                Color c = Color.Lerp(new Color(.36f, .22f, .12f), new Color(.24f, .16f, .10f), R());
                Stroke(col, h, size, x, y, R() * Mathf.PI * 2, 8 + R() * 10, .7f, .5f, c, .35f, .9f);
            }
            // twigs
            for (int k = 0; k < 40; k++)
            {
                float x = R() * size, y = R() * size;
                Stroke(col, h, size, x, y, R() * Mathf.PI * 2, 20 + R() * 40, 1.6f, 1f, new Color(.19f, .14f, .10f), .8f);
            }
            // leaves
            for (int k = 0; k < size * size / 110; k++)
            {
                float x = R() * size, y = R() * size;
                float u = x / size, v = y / size;
                if (Noise.Fbm(u, v, 4, 3, 1405) < .38f && R() < .7f) continue;   // bare soil patches
                Color c = leafCols[rnd.Next(leafCols.Length)] * Mathf.Lerp(.8f, 1.15f, R());
                float s = 3.5f + R() * 4.5f;
                Blob(col, h, size, x, y, s, s * (.45f + R() * .2f), R() * Mathf.PI * 2, c, .6f + R() * .3f, true, .25f);
            }
            // moss tufts
            for (int k = 0; k < 90; k++)
            {
                float x = R() * size, y = R() * size;
                Blob(col, h, size, x, y, 4 + R() * 7, 4 + R() * 7, R() * 6, new Color(.22f, .27f, .13f) * Mathf.Lerp(.8f, 1.2f, R()), .5f, false, .15f);
            }
        }

        static void Pebbles(Color[] col, float[] h, int size, int count, int seed, float rMin, float rMax, Color light, Color dark)
        {
            var rnd = new System.Random(seed);
            float R() => (float)rnd.NextDouble();
            for (int k = 0; k < count; k++)
            {
                float x = R() * size, y = R() * size;
                float r = Mathf.Lerp(rMin, rMax, R() * R());
                Color c = Color.Lerp(dark, light, R());
                Blob(col, h, size, x, y, r, r * (.6f + R() * .4f), R() * 6, c, .9f, false, .6f);
            }
        }

        static void Hoofprints(Color[] col, float[] h, int size)
        {
            var rnd = new System.Random(457);
            float R() => (float)rnd.NextDouble();
            for (int k = 0; k < 26; k++)
            {
                float x = R() * size, y = R() * size;
                float a = R() * 6;
                Blob(col, h, size, x, y, 9, 7, a, new Color(.12f, .09f, .07f), -.4f, false, -.4f);
            }
        }
    }
}
