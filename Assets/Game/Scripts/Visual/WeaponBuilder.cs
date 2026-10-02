using System.Collections.Generic;
using Dungine.Rules;
using UnityEngine;

namespace Dungine.Visual
{
    /// <summary>Procedural weapon meshes. Origin at grip, blade/head along +Y.</summary>
    public static class WeaponBuilder
    {
        const int STEEL = 0, WOOD = 1, GRIP = 2, TRIM = 3, GEM = 4, DARK = 5;

        static MeshBuilder.Ring Rg(Vector3 c, float rx, float rz, Quaternion? r = null) =>
            new MeshBuilder.Ring { center = c, rx = rx, rz = rz, rot = r ?? Quaternion.identity, weight = MeshBuilder.W(0) };

        public static GameObject Build(WeaponVisual w, float scale = 1f)
        {
            var mb = new MeshBuilder(6);
            Color gemC = new Color(.4f, .7f, 1f);
            switch (w)
            {
                case WeaponVisual.Dagger: Blade(mb, 0.24f, 0.022f, 0.08f, 0.1f); break;
                case WeaponVisual.Shortsword: Blade(mb, 0.46f, 0.028f, 0.11f, 0.11f); break;
                case WeaponVisual.Longsword: Blade(mb, 0.78f, 0.03f, 0.13f, 0.14f); break;
                case WeaponVisual.Greatsword: Blade(mb, 1.05f, 0.04f, 0.2f, 0.26f); break;
                case WeaponVisual.Rapier: Blade(mb, 0.85f, 0.011f, 0.1f, 0.12f); mb.AddEllipsoid(TRIM, new Vector3(0, 0.02f, 0), new Vector3(0.045f, 0.03f, 0.045f), 6, 10); break;
                case WeaponVisual.Scimitar: CurvedBlade(mb, 0.7f, 0.035f, 0.18f); break;
                case WeaponVisual.Sickle: CurvedBlade(mb, 0.3f, 0.025f, 0.6f); break;
                case WeaponVisual.Handaxe: Haft(mb, 0.42f, 0.016f); AxeHead(mb, 0.36f, 0.09f, false); break;
                case WeaponVisual.Battleaxe: Haft(mb, 0.8f, 0.018f); AxeHead(mb, 0.72f, 0.14f, false); break;
                case WeaponVisual.Greataxe: Haft(mb, 1.25f, 0.022f, -0.3f); AxeHead(mb, 0.85f, 0.2f, true); break;
                case WeaponVisual.Mace: Haft(mb, 0.55f, 0.017f); Flanged(mb, 0.52f, 0.05f); break;
                case WeaponVisual.Morningstar: Haft(mb, 0.65f, 0.018f); SpikedBall(mb, 0.62f, 0.055f); break;
                case WeaponVisual.Warhammer: Haft(mb, 0.65f, 0.018f); mb.AddBox(STEEL, new Vector3(0.02f, 0.6f, 0), new Vector3(0.12f, 0.07f, 0.07f)); mb.Push(); mb.Translate(new Vector3(-0.05f, 0.6f, 0)); mb.Rotate(Quaternion.Euler(0, 0, 90)); mb.AddCone(STEEL, Vector3.zero, 0.025f, 0.09f, 6); mb.Pop(); break;
                case WeaponVisual.Maul: Haft(mb, 1.2f, 0.024f, -0.3f); mb.AddRoughBox(STEEL, new Vector3(0, 0.84f, 0), new Vector3(0.28f, 0.16f, 0.16f), 0.01f, 3); break;
                case WeaponVisual.Club: Club(mb, 0.6f, 0.05f); break;
                case WeaponVisual.Greatclub: Club(mb, 1.0f, 0.08f); break;
                case WeaponVisual.Quarterstaff: Haft(mb, 1.8f, 0.022f, -0.6f); mb.AddCylinder(TRIM, new Vector3(0, 1.15f, 0), 0.026f, 0.026f, 0.04f, 8); mb.AddCylinder(TRIM, new Vector3(0, -0.62f, 0), 0.026f, 0.026f, 0.04f, 8); break;
                case WeaponVisual.Staff:
                    Haft(mb, 1.75f, 0.022f, -0.55f, true);
                    for (int i = 0; i < 4; i++) { mb.Push(); mb.Translate(new Vector3(0, 1.18f, 0)); mb.Rotate(Quaternion.Euler(0, i * 90, 25)); mb.AddCone(WOOD, Vector3.zero, 0.015f, 0.14f, 5); mb.Pop(); }
                    mb.AddEllipsoid(GEM, new Vector3(0, 1.3f, 0), new Vector3(0.04f, 0.06f, 0.04f), 6, 8);
                    break;
                case WeaponVisual.Wand: Haft(mb, 0.32f, 0.01f, -0.05f, true); mb.AddEllipsoid(GEM, new Vector3(0, 0.29f, 0), new Vector3(0.018f, 0.03f, 0.018f), 5, 6); break;
                case WeaponVisual.Spear: case WeaponVisual.Javelin:
                    {
                        float len = w == WeaponVisual.Spear ? 1.9f : 1.4f;
                        Haft(mb, len, 0.017f, -0.6f);
                        var r = new List<MeshBuilder.Ring> { Rg(new Vector3(0, len - 0.62f, 0), 0.018f, 0.01f), Rg(new Vector3(0, len - 0.52f, 0), 0.04f, 0.008f), Rg(new Vector3(0, len - 0.38f, 0), 0.002f, 0.002f) };
                        mb.AddLoft(STEEL, r, 6, true, true);
                        break;
                    }
                case WeaponVisual.Glaive: case WeaponVisual.Halberd:
                    {
                        Haft(mb, 2.0f, 0.02f, -0.7f);
                        if (w == WeaponVisual.Glaive) { mb.Push(); mb.Translate(new Vector3(0, 1.3f, 0)); CurvedBlade(mb, 0.45f, 0.05f, 0.25f, false); mb.Pop(); }
                        else { AxeHead(mb, 1.22f, 0.18f, false); mb.Push(); mb.Translate(new Vector3(0, 1.3f, 0)); mb.AddCone(STEEL, Vector3.zero, 0.02f, 0.25f, 6); mb.Pop(); }
                        break;
                    }
                case WeaponVisual.Trident:
                    Haft(mb, 1.7f, 0.018f, -0.5f);
                    mb.AddBox(STEEL, new Vector3(0, 1.22f, 0), new Vector3(0.14f, 0.02f, 0.02f));
                    for (int i = -1; i <= 1; i++) { mb.AddCylinder(STEEL, new Vector3(i * 0.065f, 1.22f, 0), 0.008f, 0.008f, 0.2f, 5, false); mb.AddCone(STEEL, new Vector3(i * 0.065f, 1.42f, 0), 0.014f, 0.05f, 5); }
                    break;
                case WeaponVisual.Flail:
                    Haft(mb, 0.45f, 0.017f);
                    for (int i = 0; i < 5; i++) mb.AddEllipsoid(DARK, new Vector3(0.015f * i, 0.47f + i * 0.035f, 0), new Vector3(0.01f, 0.018f, 0.01f), 4, 6);
                    SpikedBall(mb, 0.68f, 0.05f, 0.08f);
                    break;
                case WeaponVisual.Shortbow: Bow(mb, 0.55f, 0.13f); break;
                case WeaponVisual.Longbow: Bow(mb, 0.8f, 0.16f); break;
                case WeaponVisual.LightCrossbow: Crossbow(mb, 0.6f, 0.3f); break;
                case WeaponVisual.HeavyCrossbow: Crossbow(mb, 0.8f, 0.4f); break;
                case WeaponVisual.HandCrossbow: Crossbow(mb, 0.28f, 0.16f); break;
                case WeaponVisual.Shield:
                    mb.AddCylinder(WOOD, new Vector3(0, -0.015f, 0), 0.29f, 0.29f, 0.03f, 20);
                    mb.AddCylinder(TRIM, new Vector3(0, -0.018f, 0), 0.305f, 0.305f, 0.036f, 20, false);
                    mb.AddEllipsoid(STEEL, new Vector3(0, 0.02f, 0), new Vector3(0.07f, 0.04f, 0.07f), 6, 10);
                    mb.AddBox(STEEL, new Vector3(0, 0.017f, 0), new Vector3(0.56f, 0.006f, 0.04f));
                    mb.AddBox(STEEL, new Vector3(0, 0.017f, 0), new Vector3(0.04f, 0.006f, 0.56f));
                    break;
                case WeaponVisual.Lute:
                    mb.AddEllipsoid(WOOD, new Vector3(0, 0.0f, 0), new Vector3(0.16f, 0.2f, 0.07f), 8, 12, p => new Vector3(p.x, p.y, p.z < 0 ? p.z : p.z * 0.25f));
                    mb.AddBox(WOOD, new Vector3(0, 0.34f, 0.02f), new Vector3(0.045f, 0.34f, 0.025f));
                    mb.AddBox(WOOD, new Vector3(0, 0.54f, 0.0f), new Vector3(0.06f, 0.1f, 0.02f));
                    mb.AddEllipsoid(DARK, new Vector3(0, 0.03f, 0.018f), new Vector3(0.04f, 0.04f, 0.002f), 4, 10);
                    break;
                case WeaponVisual.HolySymbol:
                    mb.AddCylinder(TRIM, new Vector3(0, 0, 0), 0.06f, 0.06f, 0.012f, 16);
                    for (int i = 0; i < 12; i++) { mb.Push(); mb.Rotate(Quaternion.Euler(0, i * 30, 0)); mb.Push(); mb.Translate(new Vector3(0.06f, 0.006f, 0)); mb.Rotate(Quaternion.Euler(0, 0, -90)); mb.AddCone(TRIM, Vector3.zero, 0.012f, 0.04f, 4); mb.Pop(); mb.Pop(); }
                    break;
                case WeaponVisual.Torch:
                    Haft(mb, 0.5f, 0.018f, -0.05f, true);
                    mb.AddCylinder(GRIP, new Vector3(0, 0.38f, 0), 0.03f, 0.035f, 0.12f, 8);
                    break;
            }
            var go = new GameObject(w.ToString());
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mb.Push();
            var mesh = mb.Build(w.ToString());
            mb.Pop();
            mf.sharedMesh = mesh;
            go.transform.localScale = Vector3.one * scale;
            mr.sharedMaterials = new[]
            {
                MatLib.Lit(new Color(.66f, .67f, .7f), TexId.Metal, .78f, .95f, 1f, .4f),
                MatLib.Lit(new Color(.38f, .26f, .16f), TexId.WoodPlank, .3f, 0, 0.5f, .8f),
                MatLib.Lit(new Color(.2f, .13f, .09f), TexId.Leather, .3f, 0, 2f, 1f),
                MatLib.Lit(new Color(.74f, .56f, .26f), TexId.Metal, .75f, 1f, 1f, .4f),
                MatLib.Emissive(gemC, gemC * 2.5f),
                MatLib.Lit(new Color(.08f, .07f, .06f), null, .3f),
            };
            return go;
        }

        static void Blade(MeshBuilder mb, float len, float width, float guard, float grip)
        {
            // grip + pommel
            mb.AddCylinder(GRIP, new Vector3(0, -grip * 0.5f, 0), 0.016f, 0.015f, grip, 8);
            mb.AddEllipsoid(TRIM, new Vector3(0, -grip * 0.5f - 0.015f, 0), Vector3.one * 0.022f, 5, 8);
            // crossguard
            mb.AddBox(TRIM, new Vector3(0, grip * 0.5f + 0.01f, 0), new Vector3(guard, 0.022f, 0.028f));
            // blade: diamond cross-section loft with tapering point
            float y0 = grip * 0.5f + 0.02f;
            var rings = new List<MeshBuilder.Ring>();
            int n = 6;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                float wdt = width * (t < 0.8f ? Mathf.Lerp(1f, 0.85f, t / 0.8f) : Mathf.Lerp(0.85f, 0.02f, (t - 0.8f) / 0.2f));
                rings.Add(Rg(new Vector3(0, y0 + len * t, 0), wdt, wdt * 0.22f));
            }
            var r = rings.ToArray();
            for (int i = 0; i < r.Length; i++) r[i].radial = a => Mathf.Abs(Mathf.Cos(a)) > 0.7f ? 1f : 0.6f;
            mb.AddLoft(STEEL, r, 4, true, true);
            // fuller groove
            mb.AddBox(DARK, new Vector3(0, y0 + len * 0.35f, 0), new Vector3(width * 0.2f, len * 0.55f, width * 0.46f));
        }

        static void CurvedBlade(MeshBuilder mb, float len, float width, float curve, bool withHilt = true)
        {
            if (withHilt)
            {
                mb.AddCylinder(GRIP, new Vector3(0, -0.06f, 0), 0.016f, 0.015f, 0.12f, 8);
                mb.AddBox(TRIM, new Vector3(0, 0.065f, 0), new Vector3(0.09f, 0.02f, 0.026f));
            }
            var rings = new List<MeshBuilder.Ring>();
            int n = 8;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                float x = curve * t * t * len;
                float wdt = width * (t < 0.75f ? 1f : Mathf.Lerp(1f, 0.05f, (t - 0.75f) / 0.25f));
                Vector3 dir = new Vector3(2 * curve * t * len, len, 0).normalized;
                rings.Add(Rg(new Vector3(x, 0.08f + len * t, 0), wdt, 0.005f, Quaternion.FromToRotation(Vector3.up, dir)));
            }
            mb.AddLoft(STEEL, rings, 4, true, true);
        }

        static void Haft(MeshBuilder mb, float len, float r, float start = -0.08f, bool gnarled = false)
        {
            var pts = new List<Vector3>(); var rr = new List<float>();
            int n = gnarled ? 8 : 2;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                pts.Add(new Vector3(gnarled ? Mathf.Sin(t * 9) * 0.01f : 0, start + len * t, gnarled ? Mathf.Cos(t * 7) * 0.008f : 0));
                rr.Add(r * (gnarled ? 1f + 0.2f * Mathf.Sin(t * 13) : 1f));
            }
            mb.AddTube(WOOD, pts, rr, 8);
            mb.AddCylinder(GRIP, new Vector3(0, -0.07f, 0), r * 1.15f, r * 1.15f, 0.16f, 8, false);
        }

        static void AxeHead(MeshBuilder mb, float y, float size, bool doubleHead)
        {
            for (int side = doubleHead ? -1 : 1; side <= 1; side += 2)
                mb.AddLoft(STEEL, AxeRings(y, size, side), 4, true, true);
            if (!doubleHead)
            {
                mb.Push(); mb.Translate(new Vector3(-0.03f, y, 0)); mb.Rotate(Quaternion.Euler(0, 0, 90));
                mb.AddCone(STEEL, Vector3.zero, 0.02f, 0.06f, 5);
                mb.Pop();
            }
            mb.AddBox(STEEL, new Vector3(0, y, 0), new Vector3(0.05f, 0.08f, 0.045f));
        }

        static List<MeshBuilder.Ring> AxeRings(float y, float size, int side)
        {
            var rings = new List<MeshBuilder.Ring>();
            for (int i = 0; i <= 5; i++)
            {
                float t = i / 5f;
                float h = Mathf.Lerp(0.04f, size * 1.15f, Mathf.Pow(t, 1.6f));
                var rot = Quaternion.Euler(0, 0, -90 * side); // ring axis points along +-X
                rings.Add(new MeshBuilder.Ring { center = new Vector3(side * (0.02f + size * 0.9f * t), y - size * 0.1f * t, 0), rot = rot, rx = h * 0.5f, rz = Mathf.Lerp(0.018f, 0.003f, t), weight = MeshBuilder.W(0) });
            }
            return rings;
        }

        static void Flanged(MeshBuilder mb, float y, float r)
        {
            mb.AddEllipsoid(STEEL, new Vector3(0, y, 0), new Vector3(r * 0.7f, r * 1.1f, r * 0.7f), 6, 8);
            for (int i = 0; i < 6; i++)
            {
                mb.Push(); mb.Rotate(Quaternion.Euler(0, i * 60, 0));
                mb.AddBox(STEEL, new Vector3(r * 0.75f, y, 0), new Vector3(r * 0.6f, r * 1.8f, 0.012f));
                mb.Pop();
            }
        }

        static void SpikedBall(MeshBuilder mb, float y, float r, float xOff = 0)
        {
            Vector3 c = new Vector3(xOff, y, 0);
            mb.AddEllipsoid(STEEL, c, Vector3.one * r, 8, 10);
            Vector3[] dirs = { Vector3.up, Vector3.left, Vector3.right, Vector3.forward, Vector3.back, new Vector3(1, 1, 1), new Vector3(-1, 1, 1), new Vector3(1, 1, -1), new Vector3(-1, 1, -1), new Vector3(1, -1, 1), new Vector3(-1, -1, -1) };
            foreach (var d in dirs)
            {
                mb.Push(); mb.Translate(c + d.normalized * r * 0.8f); mb.Rotate(Quaternion.FromToRotation(Vector3.up, d.normalized));
                mb.AddCone(STEEL, Vector3.zero, r * 0.28f, r * 0.7f, 5);
                mb.Pop();
            }
        }

        static void Club(MeshBuilder mb, float len, float r)
        {
            var pts = new List<Vector3>(); var rr = new List<float>();
            for (int i = 0; i <= 6; i++)
            {
                float t = i / 6f;
                pts.Add(new Vector3(0, -0.08f + len * t, 0));
                rr.Add(Mathf.Lerp(0.018f, r, t * t) * (1 + 0.15f * Mathf.Sin(i * 3.1f)));
            }
            mb.AddTube(WOOD, pts, rr, 8);
            mb.AddCylinder(GRIP, new Vector3(0, -0.07f, 0), 0.022f, 0.022f, 0.14f, 8, false);
        }

        static void Bow(MeshBuilder mb, float half, float depth)
        {
            // limbs curve toward -Z (away from archer's face is +Z = forward)
            var pts = new List<Vector3>(); var rr = new List<float>();
            int n = 12;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n * 2 - 1;
                float y = t * half;
                float z = depth * (1 - t * t) - depth * 0.1f;
                pts.Add(new Vector3(0, y, z));
                rr.Add(Mathf.Lerp(0.02f, 0.008f, Mathf.Abs(t)) );
            }
            mb.AddTube(WOOD, pts, rr, 6);
            mb.AddCylinder(GRIP, new Vector3(0, -0.07f, depth * 0.9f), 0.022f, 0.022f, 0.14f, 8, false);
            // string
            var sp = new List<Vector3> { new Vector3(0, -half, -depth * 0.1f), new Vector3(0, half, -depth * 0.1f) };
            mb.AddTube(DARK, sp, new List<float> { 0.003f, 0.003f }, 4, false);
        }

        static void Crossbow(MeshBuilder mb, float stock, float prod)
        {
            // stock along +Y (points forward when aimed), prod across X
            mb.AddBox(WOOD, new Vector3(0, stock * 0.35f, -0.01f), new Vector3(0.04f, stock, 0.05f));
            var pts = new List<Vector3>(); var rr = new List<float>();
            for (int i = 0; i <= 8; i++)
            {
                float t = i / 8f * 2 - 1;
                pts.Add(new Vector3(t * prod, stock * 0.8f - (1 - t * t) * -0.04f - 0.04f, 0.0f));
                rr.Add(0.012f);
            }
            mb.AddTube(STEEL, pts, rr, 6);
            mb.AddTube(DARK, new List<Vector3> { new Vector3(-prod, stock * 0.76f, 0), new Vector3(0, stock * 0.55f, 0), new Vector3(prod, stock * 0.76f, 0) }, new List<float> { 0.003f, 0.003f, 0.003f }, 4, false);
            mb.AddBox(DARK, new Vector3(0, stock * 0.1f, -0.035f), new Vector3(0.012f, 0.05f, 0.03f));
        }
    }
}
