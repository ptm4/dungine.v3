using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dungine.UI
{
    /// <summary>Anti-aliased vector icon painter. Every icon in the game is drawn here at runtime.</summary>
    public static class Icons
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        const int S = 128;

        abstract class Shape { public bool erase; public abstract float Dist(Vector2 p); }
        class Seg : Shape { public Vector2 a, b; public float w; public override float Dist(Vector2 p) { var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude)); return Vector2.Distance(p, a + ab * t) - w * 0.5f; } }
        class Circ : Shape { public Vector2 c; public float r; public override float Dist(Vector2 p) => Vector2.Distance(p, c) - r; }
        class RingS : Shape { public Vector2 c; public float r, w; public override float Dist(Vector2 p) => Mathf.Abs(Vector2.Distance(p, c) - r) - w * 0.5f; }
        class ArcS : Shape
        {
            public Vector2 c; public float r, w, a0, a1;
            public override float Dist(Vector2 p)
            {
                Vector2 d = p - c; float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                float mid = (a0 + a1) * 0.5f, half = (a1 - a0) * 0.5f;
                if (Mathf.Abs(Mathf.DeltaAngle(mid, ang)) <= half) return Mathf.Abs(d.magnitude - r) - w * 0.5f;
                Vector2 e0 = c + new Vector2(Mathf.Cos(a0 * Mathf.Deg2Rad), Mathf.Sin(a0 * Mathf.Deg2Rad)) * r;
                Vector2 e1 = c + new Vector2(Mathf.Cos(a1 * Mathf.Deg2Rad), Mathf.Sin(a1 * Mathf.Deg2Rad)) * r;
                return Mathf.Min(Vector2.Distance(p, e0), Vector2.Distance(p, e1)) - w * 0.5f;
            }
        }
        class Poly : Shape
        {
            public Vector2[] pts;
            public override float Dist(Vector2 p)
            {
                float d = float.MaxValue; bool inside = false;
                for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
                {
                    Vector2 a = pts[j], b = pts[i];
                    var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                    d = Mathf.Min(d, Vector2.Distance(p, a + ab * t));
                    if (((a.y > p.y) != (b.y > p.y)) && (p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y + 1e-9f) + a.x)) inside = !inside;
                }
                return inside ? -d : d;
            }
        }
        class Ell : Shape { public Vector2 c, r; public override float Dist(Vector2 p) { Vector2 q = new Vector2((p.x - c.x) / r.x, (p.y - c.y) / r.y); return (q.magnitude - 1f) * Mathf.Min(r.x, r.y); } }

        class Canvas
        {
            public readonly List<Shape> shapes = new List<Shape>();
            static Vector2 V(float x, float y) => new Vector2(x, y);
            public Canvas L(float x0, float y0, float x1, float y1, float w = 0.07f, bool erase = false) { shapes.Add(new Seg { a = V(x0, y0), b = V(x1, y1), w = w, erase = erase }); return this; }
            public Canvas C(float x, float y, float r, bool erase = false) { shapes.Add(new Circ { c = V(x, y), r = r, erase = erase }); return this; }
            public Canvas R(float x, float y, float r, float w = 0.06f, bool erase = false) { shapes.Add(new RingS { c = V(x, y), r = r, w = w, erase = erase }); return this; }
            public Canvas A(float x, float y, float r, float a0, float a1, float w = 0.06f) { shapes.Add(new ArcS { c = V(x, y), r = r, w = w, a0 = a0, a1 = a1 }); return this; }
            public Canvas P(bool erase, params float[] xy) { var p = new Vector2[xy.Length / 2]; for (int i = 0; i < p.Length; i++) p[i] = V(xy[i * 2], xy[i * 2 + 1]); shapes.Add(new Poly { pts = p, erase = erase }); return this; }
            public Canvas P(params float[] xy) => P(false, xy);
            public Canvas E(float x, float y, float rx, float ry, bool erase = false) { shapes.Add(new Ell { c = V(x, y), r = V(rx, ry), erase = erase }); return this; }
        }

        public static Texture2D Get(string id)
        {
            if (string.IsNullOrEmpty(id)) id = "star";
            if (cache.TryGetValue(id, out var t) && t) return t;
            var cv = new Canvas();
            Draw(id, cv);
            t = Rasterize(cv, id);
            cache[id] = t;
            return t;
        }

        static Texture2D Rasterize(Canvas cv, string name)
        {
            var px = new Color32[S * S];
            float aa = 1.2f / S;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    Vector2 p = new Vector2((x + 0.5f) / S, (y + 0.5f) / S);
                    float a = 0;
                    foreach (var s in cv.shapes)
                    {
                        float d = s.Dist(p);
                        float cov = Mathf.Clamp01(0.5f - d / aa);
                        if (s.erase) a = Mathf.Min(a, 1 - cov); else a = Mathf.Max(a, cov);
                    }
                    byte b = (byte)(a * 255);
                    px[y * S + x] = new Color32(255, 255, 255, b);
                }
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "icon_" + name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        static void Draw(string id, Canvas c)
        {
            switch (id)
            {
                case "sword": c.L(.26f, .26f, .8f, .8f, .09f).P(.8f, .8f, .86f, .88f, .74f, .84f).L(.2f, .44f, .44f, .2f, .07f).L(.14f, .14f, .3f, .3f, .08f).C(.12f, .12f, .05f); break;
                case "dagger": c.P(.3f, .3f, .38f, .26f, .74f, .66f, .78f, .78f, .66f, .74f, .26f, .38f).L(.22f, .42f, .42f, .22f, .06f).L(.14f, .14f, .28f, .28f, .08f); break;
                case "axe": c.L(.3f, .12f, .6f, .86f, .07f).P(.5f, .62f, .74f, .84f, .88f, .64f, .82f, .44f, .6f, .5f); break;
                case "mace": c.L(.3f, .12f, .55f, .62f, .07f).C(.62f, .72f, .14f); for (int i = 0; i < 8; i++) { float a = i * 45 * Mathf.Deg2Rad; c.L(.62f, .72f, .62f + Mathf.Cos(a) * .22f, .72f + Mathf.Sin(a) * .22f, .05f); } break;
                case "staff": c.L(.28f, .1f, .66f, .78f, .06f).C(.7f, .84f, .08f).R(.7f, .84f, .13f, .03f); break;
                case "spear": c.L(.2f, .12f, .7f, .74f, .05f).P(.66f, .7f, .86f, .9f, .74f, .64f); break;
                case "bow": c.A(.28f, .5f, .44f, -62, 62, .07f).L(.49f, .11f, .49f, .89f, .02f).L(.2f, .5f, .86f, .5f, .03f).P(.86f, .5f, .76f, .56f, .76f, .44f); break;
                case "crossbow": c.L(.5f, .1f, .5f, .8f, .08f).A(.5f, .42f, .36f, 20, 160, .06f).L(.16f, .54f, .5f, .38f, .02f).L(.84f, .54f, .5f, .38f, .02f); break;
                case "fist": c.E(.5f, .46f, .24f, .22f).L(.3f, .62f, .7f, .62f, .08f).L(.35f, .5f, .35f, .66f, .03f).L(.5f, .5f, .5f, .66f, .03f).L(.64f, .5f, .64f, .66f, .03f).L(.4f, .24f, .6f, .24f, .18f); break;
                case "dash": for (int i = 0; i < 3; i++) { float x = .2f + i * .22f; c.L(x, .78f, x + .18f, .5f, .07f).L(x + .18f, .5f, x, .22f, .07f); } break;
                case "wind": c.A(.46f, .56f, .18f, -40, 200, .06f).L(.1f, .38f, .46f, .38f, .06f).A(.62f, .42f, .12f, 90, 360, .05f).L(.1f, .24f, .6f, .24f, .05f).L(.1f, .72f, .4f, .72f, .05f); break;
                case "shield": c.P(.5f, .9f, .84f, .78f, .8f, .42f, .5f, .1f, .2f, .42f, .16f, .78f).P(true, .5f, .8f, .74f, .72f, .7f, .44f, .5f, .22f, .3f, .44f, .26f, .72f).L(.5f, .2f, .5f, .8f, .06f).L(.3f, .56f, .7f, .56f, .06f); break;
                case "eye": c.A(.5f, .2f, .45f, 45, 135, .06f).A(.5f, .8f, .45f, 225, 315, .06f).C(.5f, .5f, .12f).C(.5f, .5f, .05f, true); break;
                case "hand": c.E(.5f, .38f, .2f, .2f).L(.34f, .5f, .3f, .82f, .08f).L(.46f, .54f, .45f, .88f, .08f).L(.58f, .54f, .6f, .86f, .08f).L(.68f, .48f, .74f, .76f, .08f).L(.34f, .34f, .16f, .52f, .09f); break;
                case "boot": c.P(.3f, .88f, .56f, .88f, .56f, .42f, .84f, .3f, .86f, .14f, .26f, .14f); break;
                case "blood": c.C(.5f, .38f, .22f).P(.5f, .9f, .3f, .46f, .7f, .46f).C(.42f, .34f, .06f, true); break;
                case "fire": c.P(.5f, .92f, .72f, .62f, .78f, .34f, .66f, .14f, .34f, .14f, .22f, .34f, .3f, .62f, .38f, .5f).P(true, .5f, .6f, .6f, .4f, .56f, .22f, .44f, .22f, .4f, .4f); break;
                case "frost": for (int i = 0; i < 3; i++) { float a = i * 60 * Mathf.Deg2Rad; float dx = Mathf.Cos(a) * .4f, dy = Mathf.Sin(a) * .4f; c.L(.5f - dx, .5f - dy, .5f + dx, .5f + dy, .06f); for (int s = -1; s <= 1; s += 2) { float bx = .5f + dx * .6f * s, by = .5f + dy * .6f * s; float ba = a + Mathf.PI / 4 * s; c.L(bx, by, bx + Mathf.Cos(ba + Mathf.PI / 2) * .1f, by + Mathf.Sin(ba + Mathf.PI / 2) * .1f, .04f).L(bx, by, bx + Mathf.Cos(ba - Mathf.PI / 2) * .1f, by + Mathf.Sin(ba - Mathf.PI / 2) * .1f, .04f); } } break;
                case "lightning": c.P(.58f, .92f, .26f, .46f, .48f, .46f, .38f, .08f, .76f, .56f, .54f, .56f); break;
                case "skull": c.C(.5f, .58f, .3f).P(.32f, .4f, .68f, .4f, .64f, .18f, .36f, .18f).C(.4f, .56f, .08f, true).C(.6f, .56f, .08f, true).P(true, .5f, .46f, .45f, .38f, .55f, .38f).L(.44f, .18f, .44f, .28f, .03f).L(.56f, .18f, .56f, .28f, .03f); break;
                case "acid": c.C(.36f, .32f, .14f).P(.36f, .72f, .24f, .38f, .48f, .38f).C(.68f, .44f, .11f).P(.68f, .78f, .58f, .48f, .78f, .48f).C(.62f, .16f, .06f); break;
                case "poison": c.P(.4f, .86f, .6f, .86f, .6f, .66f, .78f, .4f, .74f, .16f, .26f, .16f, .22f, .4f, .4f, .66f).C(.42f, .36f, .06f, true).C(.58f, .3f, .05f, true).C(.54f, .46f, .04f, true); break;
                case "sun": c.C(.5f, .5f, .18f); for (int i = 0; i < 12; i++) { float a = i * 30 * Mathf.Deg2Rad; c.L(.5f + Mathf.Cos(a) * .26f, .5f + Mathf.Sin(a) * .26f, .5f + Mathf.Cos(a) * .42f, .5f + Mathf.Sin(a) * .42f, i % 2 == 0 ? .06f : .035f); } break;
                case "bell": c.P(.5f, .86f, .66f, .8f, .72f, .5f, .84f, .28f, .16f, .28f, .28f, .5f, .34f, .8f).C(.5f, .18f, .08f).L(.5f, .86f, .5f, .94f, .06f); break;
                case "force": c.P(.5f, .94f, .58f, .58f, .94f, .5f, .58f, .42f, .5f, .06f, .42f, .42f, .06f, .5f, .42f, .58f).C(.5f, .5f, .06f, true); break;
                case "music": c.E(.36f, .26f, .14f, .1f).L(.48f, .28f, .48f, .82f, .05f).P(.48f, .82f, .78f, .7f, .78f, .56f, .48f, .68f).E(.7f, .4f, .1f, .07f).L(.78f, .42f, .78f, .64f, .04f); break;
                case "leaf": c.P(.2f, .2f, .3f, .6f, .56f, .82f, .86f, .86f, .8f, .52f, .56f, .26f).L(.2f, .2f, .76f, .76f, .03f, true); break;
                case "star": { var pts = new List<float>(); for (int i = 0; i < 10; i++) { float a = (90 + i * 36) * Mathf.Deg2Rad; float r = i % 2 == 0 ? .42f : .17f; pts.Add(.5f + Mathf.Cos(a) * r); pts.Add(.5f + Mathf.Sin(a) * r); } c.P(pts.ToArray()); } break;
                case "heal": c.P(.4f, .86f, .6f, .86f, .6f, .6f, .86f, .6f, .86f, .4f, .6f, .4f, .6f, .14f, .4f, .14f, .4f, .4f, .14f, .4f, .14f, .6f, .4f, .6f); break;
                case "thunder": c.C(.2f, .5f, .07f).A(.2f, .5f, .2f, -50, 50, .05f).A(.2f, .5f, .36f, -45, 45, .05f).A(.2f, .5f, .52f, -40, 40, .05f).A(.2f, .5f, .68f, -35, 35, .05f); break;
                case "sleep": c.L(.2f, .7f, .44f, .7f, .06f).L(.44f, .7f, .2f, .42f, .06f).L(.2f, .42f, .44f, .42f, .06f).L(.56f, .5f, .76f, .5f, .05f).L(.76f, .5f, .56f, .26f, .05f).L(.56f, .26f, .76f, .26f, .05f); break;
                case "curse": c.A(.5f, .5f, .32f, 0, 300, .06f).A(.5f, .5f, .2f, 60, 360, .06f).C(.5f, .5f, .06f); break;
                case "holy": c.L(.5f, .14f, .5f, .86f, .1f).L(.26f, .62f, .74f, .62f, .1f).R(.5f, .62f, .32f, .03f); break;
                case "mark": c.R(.5f, .5f, .28f, .05f).R(.5f, .5f, .12f, .04f).L(.5f, .06f, .5f, .3f, .05f).L(.5f, .7f, .5f, .94f, .05f).L(.06f, .5f, .3f, .5f, .05f).L(.7f, .5f, .94f, .5f, .05f); break;
                case "chain": c.E(.34f, .5f, .2f, .12f).E(.34f, .5f, .12f, .05f, true).E(.66f, .5f, .2f, .12f).E(.66f, .5f, .12f, .05f, true); break;
                case "moon": c.C(.5f, .5f, .34f).C(.64f, .6f, .3f, true); break;
                case "teleport": c.A(.5f, .5f, .34f, 0, 270, .05f).A(.5f, .5f, .22f, 90, 360, .05f).C(.5f, .5f, .07f).C(.84f, .5f, .04f).C(.5f, .16f, .04f); break;
                case "web": for (int i = 0; i < 8; i++) { float a = i * 45 * Mathf.Deg2Rad; c.L(.5f, .5f, .5f + Mathf.Cos(a) * .42f, .5f + Mathf.Sin(a) * .42f, .025f); } c.R(.5f, .5f, .14f, .025f).R(.5f, .5f, .26f, .025f).R(.5f, .5f, .38f, .025f); break;
                case "heart": c.C(.36f, .6f, .18f).C(.64f, .6f, .18f).P(.18f, .54f, .82f, .54f, .5f, .16f); break;
                case "laugh": c.R(.5f, .5f, .36f, .06f).A(.5f, .48f, .2f, 200, 340, .06f).C(.38f, .6f, .05f).C(.62f, .6f, .05f); break;
                case "bless": c.R(.5f, .8f, .18f, .04f); { var pts = new List<float>(); for (int i = 0; i < 10; i++) { float a = (90 + i * 36) * Mathf.Deg2Rad; float r = i % 2 == 0 ? .3f : .12f; pts.Add(.5f + Mathf.Cos(a) * r); pts.Add(.4f + Mathf.Sin(a) * r); } c.P(pts.ToArray()); } break;
                case "rage": for (int i = 0; i < 10; i++) { float a = i * 36 * Mathf.Deg2Rad; c.P(.5f + Mathf.Cos(a - .25f) * .16f, .5f + Mathf.Sin(a - .25f) * .16f, .5f + Mathf.Cos(a) * .44f, .5f + Mathf.Sin(a) * .44f, .5f + Mathf.Cos(a + .25f) * .16f, .5f + Mathf.Sin(a + .25f) * .16f); } c.C(.5f, .5f, .2f); break;
                case "wolf": c.P(.2f, .9f, .34f, .62f, .5f, .56f, .66f, .62f, .8f, .9f, .82f, .5f, .62f, .28f, .56f, .1f, .44f, .1f, .38f, .28f, .18f, .5f).C(.4f, .5f, .04f, true).C(.6f, .5f, .04f, true); break;
                case "bear": c.C(.5f, .45f, .3f).C(.26f, .74f, .1f).C(.74f, .74f, .1f).E(.5f, .34f, .12f, .08f, true).C(.5f, .38f, .04f).C(.4f, .52f, .04f, true).C(.6f, .52f, .04f, true); break;
                case "surge": c.P(.5f, .92f, .82f, .6f, .62f, .6f, .62f, .36f, .38f, .36f, .38f, .6f, .18f, .6f).L(.38f, .2f, .62f, .2f, .08f); break;
                case "smite": c.L(.5f, .1f, .5f, .74f, .1f).P(.5f, .92f, .42f, .74f, .58f, .74f).L(.3f, .3f, .7f, .3f, .07f); for (int i = 0; i < 6; i++) { float a = (i * 60 + 30) * Mathf.Deg2Rad; c.L(.5f + Mathf.Cos(a) * .3f, .6f + Mathf.Sin(a) * .3f, .5f + Mathf.Cos(a) * .42f, .6f + Mathf.Sin(a) * .42f, .03f); } break;
                case "breath": c.C(.16f, .5f, .08f).A(.16f, .5f, .28f, -30, 30, .06f).A(.16f, .5f, .48f, -28, 28, .06f).A(.16f, .5f, .7f, -26, 26, .06f); break;
                case "flask": c.C(.5f, .38f, .26f).L(.5f, .62f, .5f, .84f, .12f).L(.4f, .86f, .6f, .86f, .06f).C(.44f, .34f, .06f, true); break;
                case "potion": c.P(.4f, .84f, .6f, .84f, .6f, .66f, .76f, .46f, .72f, .14f, .28f, .14f, .24f, .46f, .4f, .66f).L(.36f, .88f, .64f, .88f, .06f).P(true, .3f, .4f, .7f, .4f, .68f, .2f, .32f, .2f); break;
                case "scroll": c.P(.26f, .78f, .74f, .78f, .74f, .22f, .26f, .22f).C(.26f, .5f, .08f).C(.74f, .5f, .08f).L(.36f, .64f, .64f, .64f, .03f, true).L(.36f, .5f, .64f, .5f, .03f, true).L(.36f, .36f, .56f, .36f, .03f, true); break;
                case "food": c.E(.5f, .46f, .34f, .22f).L(.32f, .52f, .38f, .38f, .03f, true).L(.48f, .56f, .54f, .38f, .03f, true).L(.64f, .52f, .7f, .38f, .03f, true); break;
                case "armor": c.P(.24f, .84f, .4f, .84f, .5f, .74f, .6f, .84f, .76f, .84f, .84f, .62f, .74f, .56f, .72f, .14f, .28f, .14f, .26f, .56f, .16f, .62f).L(.5f, .7f, .5f, .2f, .03f, true); break;
                case "robe": c.P(.36f, .88f, .64f, .88f, .8f, .62f, .74f, .56f, .82f, .1f, .18f, .1f, .26f, .56f, .2f, .62f).L(.5f, .8f, .5f, .14f, .03f, true); break;
                case "gem": c.P(.3f, .72f, .7f, .72f, .86f, .52f, .5f, .12f, .14f, .52f).L(.14f, .52f, .86f, .52f, .03f, true).L(.5f, .12f, .38f, .52f, .025f, true).L(.5f, .12f, .62f, .52f, .025f, true); break;
                case "ring": c.R(.5f, .4f, .24f, .08f).P(.4f, .66f, .6f, .66f, .66f, .76f, .5f, .9f, .34f, .76f); break;
                case "amulet": c.A(.5f, .66f, .3f, 20, 160, .04f).L(.5f, .64f, .5f, .52f, .03f).P(.5f, .52f, .66f, .36f, .5f, .14f, .34f, .36f); break;
                case "cloak": c.P(.4f, .88f, .6f, .88f, .66f, .78f, .86f, .12f, .6f, .2f, .5f, .12f, .4f, .2f, .14f, .12f, .34f, .78f); break;
                case "boots": c.P(.22f, .86f, .42f, .86f, .42f, .4f, .6f, .3f, .62f, .16f, .18f, .16f).P(.52f, .86f, .7f, .86f, .7f, .4f, .86f, .32f, .88f, .2f, .66f, .2f, .6f, .36f, .52f, .4f); break;
                case "gloves": c.E(.5f, .36f, .2f, .2f).L(.36f, .5f, .32f, .82f, .09f).L(.48f, .54f, .47f, .88f, .09f).L(.6f, .52f, .62f, .84f, .09f).L(.34f, .32f, .16f, .48f, .1f).P(.3f, .2f, .7f, .2f, .7f, .08f, .3f, .08f); break;
                case "helm": c.P(.2f, .3f, .2f, .6f, .34f, .82f, .66f, .82f, .8f, .6f, .8f, .3f, .6f, .3f, .6f, .5f, .4f, .5f, .4f, .3f).L(.5f, .82f, .5f, .92f, .06f); break;
                case "key": c.R(.28f, .66f, .14f, .07f).L(.38f, .56f, .8f, .14f, .07f).L(.66f, .28f, .76f, .38f, .06f).L(.74f, .2f, .84f, .3f, .06f); break;
                case "letter": c.P(.14f, .74f, .86f, .74f, .86f, .26f, .14f, .26f).L(.14f, .74f, .5f, .44f, .04f, true).L(.86f, .74f, .5f, .44f, .04f, true).C(.5f, .44f, .07f); break;
                case "book": c.P(.24f, .86f, .78f, .86f, .78f, .14f, .24f, .14f).L(.32f, .86f, .32f, .14f, .03f, true).L(.42f, .7f, .7f, .7f, .03f, true).L(.42f, .6f, .66f, .6f, .03f, true); break;
                case "candle": c.P(.4f, .6f, .6f, .6f, .6f, .14f, .4f, .14f).P(.5f, .9f, .58f, .74f, .5f, .64f, .42f, .74f).L(.34f, .14f, .66f, .14f, .06f); break;
                case "torch": c.L(.36f, .1f, .56f, .6f, .08f).P(.6f, .94f, .72f, .74f, .66f, .56f, .5f, .56f, .44f, .72f); break;
                case "pelt": c.P(.2f, .8f, .36f, .7f, .5f, .84f, .64f, .7f, .8f, .8f, .74f, .5f, .84f, .24f, .6f, .3f, .5f, .14f, .4f, .3f, .16f, .24f, .26f, .5f); break;
                case "dice": c.P(.5f, .92f, .88f, .7f, .88f, .3f, .5f, .08f, .12f, .3f, .12f, .7f).P(true, .5f, .78f, .76f, .36f, .24f, .36f).L(.5f, .92f, .5f, .78f, .03f, true); break;
                case "fang": c.P(.3f, .84f, .7f, .84f, .66f, .56f, .52f, .12f, .46f, .12f, .34f, .56f); break;
                case "claw": c.A(.2f, .5f, .5f, -20, 60, .07f).A(.3f, .4f, .5f, -20, 60, .07f).A(.4f, .3f, .5f, -20, 60, .07f); break;
                case "end": c.P(.24f, .86f, .76f, .86f, .5f, .5f).P(.24f, .14f, .76f, .14f, .5f, .5f).P(true, .32f, .8f, .68f, .8f, .5f, .6f); break;
                case "bag": c.P(.22f, .62f, .78f, .62f, .84f, .2f, .16f, .2f).A(.5f, .62f, .16f, 0, 180, .07f).L(.3f, .46f, .7f, .46f, .04f, true); break;
                case "person": c.C(.5f, .74f, .14f).P(.24f, .1f, .76f, .1f, .7f, .5f, .5f, .58f, .3f, .5f); break;
                case "tent": c.P(.5f, .86f, .88f, .14f, .12f, .14f).P(true, .5f, .5f, .62f, .14f, .38f, .14f); break;
                case "gear": c.C(.5f, .5f, .26f).C(.5f, .5f, .1f, true); for (int i = 0; i < 8; i++) { float a = i * 45 * Mathf.Deg2Rad; c.L(.5f + Mathf.Cos(a) * .2f, .5f + Mathf.Sin(a) * .2f, .5f + Mathf.Cos(a) * .38f, .5f + Mathf.Sin(a) * .38f, .1f); } c.C(.5f, .5f, .1f, true); break;
                case "coins": c.E(.4f, .3f, .24f, .1f).E(.4f, .42f, .24f, .1f).E(.6f, .56f, .24f, .1f).E(.6f, .68f, .24f, .1f); break;
                default: c.C(.5f, .5f, .3f).C(.5f, .5f, .15f, true); break;
            }
        }
    }
}
