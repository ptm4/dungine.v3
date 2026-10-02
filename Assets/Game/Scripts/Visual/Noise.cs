using UnityEngine;

namespace Dungine.Visual
{
    /// <summary>Tileable (periodic) noise functions used by texture generation.</summary>
    public static class Noise
    {
        static int Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 144665;
                h = (h ^ (h >> 13)) * 1274126177;
                return h ^ (h >> 16);
            }
        }

        public static float Hash01(int x, int y, int seed) => (Hash(x, y, seed) & 0xFFFFFF) / 16777215f;

        static int Mod(int a, int p) { int r = a % p; return r < 0 ? r + p : r; }

        static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);

        /// <summary>Periodic gradient noise in [-1,1] (approx). u,v in cells; period in cells.</summary>
        public static float Gradient(float u, float v, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(u), y0 = Mathf.FloorToInt(v);
            float fx = u - x0, fy = v - y0;
            float n00 = Grad(Mod(x0, period), Mod(y0, period), seed, fx, fy);
            float n10 = Grad(Mod(x0 + 1, period), Mod(y0, period), seed, fx - 1, fy);
            float n01 = Grad(Mod(x0, period), Mod(y0 + 1, period), seed, fx, fy - 1);
            float n11 = Grad(Mod(x0 + 1, period), Mod(y0 + 1, period), seed, fx - 1, fy - 1);
            float sx = Fade(fx), sy = Fade(fy);
            return Mathf.Lerp(Mathf.Lerp(n00, n10, sx), Mathf.Lerp(n01, n11, sx), sy) * 1.41f;
        }

        static float Grad(int x, int y, int seed, float dx, float dy)
        {
            float a = Hash01(x, y, seed) * Mathf.PI * 2;
            return Mathf.Cos(a) * dx + Mathf.Sin(a) * dy;
        }

        /// <summary>Fractal periodic noise in ~[0,1]. uv in [0,1), baseFreq cells across the tile.</summary>
        public static float Fbm(float u, float v, int baseFreq, int octaves, int seed, float gain = 0.5f)
        {
            float sum = 0, amp = 0.5f, norm = 0;
            int f = baseFreq;
            for (int o = 0; o < octaves; o++)
            {
                sum += Gradient(u * f, v * f, f, seed + o * 31) * amp;
                norm += amp; amp *= gain; f *= 2;
            }
            return Mathf.Clamp01(sum / norm * 0.5f + 0.5f);
        }

        /// <summary>Ridged fractal noise (cracks, veins) in [0,1].</summary>
        public static float Ridged(float u, float v, int baseFreq, int octaves, int seed)
        {
            float sum = 0, amp = 0.5f, norm = 0; int f = baseFreq;
            for (int o = 0; o < octaves; o++)
            {
                float n = 1 - Mathf.Abs(Gradient(u * f, v * f, f, seed + o * 17));
                sum += n * n * amp; norm += amp; amp *= 0.5f; f *= 2;
            }
            return sum / norm;
        }

        public struct Cell { public float f1, f2; public int id; public Vector2 center; }

        /// <summary>Periodic Voronoi with jittered points. cells across tile.</summary>
        public static Cell Voronoi(float u, float v, int cells, int seed, float jitter = 0.85f, float stretchX = 1f)
        {
            float x = u * cells, y = v * cells;
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            var c = new Cell { f1 = 99, f2 = 99 };
            for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    int cx = xi + i, cy = yi + j;
                    int px = Mod(cx, cells), py = Mod(cy, cells);
                    float ox = 0.5f + (Hash01(px, py, seed) - 0.5f) * jitter;
                    float oy = 0.5f + (Hash01(px, py, seed + 7) - 0.5f) * jitter;
                    float dx = (cx + ox - x) * stretchX, dy = cy + oy - y;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d < c.f1) { c.f2 = c.f1; c.f1 = d; c.id = px * 7919 + py; c.center = new Vector2((cx + ox) / cells, (cy + oy) / cells); }
                    else if (d < c.f2) c.f2 = d;
                }
            return c;
        }
    }
}
