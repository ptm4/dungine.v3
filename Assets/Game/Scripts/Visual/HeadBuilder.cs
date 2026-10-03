using System;
using System.Collections.Generic;
using Dungine.Rules;
using UnityEngine;

namespace Dungine.Visual
{
    /// <summary>
    /// Sculpted heads: a dense deformed sphere with facial landmarks (brow, sockets, cheekbones, nose, lips, chin),
    /// eyeballs with lids, conforming hair/beard shells, lock-based long hair, and a painted face texture.
    /// </summary>
    public static partial class HumanoidBuilder
    {
        class HeadShape
        {
            public Vector3 c;            // center (model space)
            public float rx, ry, rz;
            public float jawTaper = 0.16f, brow = 1f, cheek = 1f, gaunt, noseLen = 1f, noseW = 1f, chin = 1f, lips = 1f;
            public float eyeA = 0.34f, eyeE = 0.1f, browE = 0.25f, noseTipE = -0.23f, lipUpE = -0.38f, mouthE = -0.43f, lipLoE = -0.48f, chinE = -0.74f;
            public float snout;          // dragonborn muzzle
            public float underbite;      // half-orc
            public float cranium = 1f;
            public bool skeletal;
            public float hs;
        }

        const int LAT = 84, LON = 96;

        static float G(float a, float e, float a0, float e0, float sa, float se)
        {
            float da = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            float x = da / sa, y = (e - e0) / se;
            return Mathf.Exp(-(x * x + y * y));
        }
        static float G2(float a, float e, float a0, float e0, float sa, float se) => G(a, e, a0, e0, sa, se) + G(a, e, -a0, e0, sa, se);
        static float SmoothR(float edge0, float edge1, float x) { float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0)); return t * t * (3 - 2 * t); }

        static HeadShape MakeShape(Appearance a, Dims d, RaceLook look)
        {
            var h = new HeadShape { c = d.headC, rx = d.headRad.x, ry = d.headRad.y, rz = d.headRad.z, hs = d.hs };
            bool fem = a.bodyType == 1;
            switch (a.faceShape)
            {
                case 0: h.jawTaper = 0.1f; h.chin = 1.1f; h.cheek = 1.2f; break;
                case 1: h.jawTaper = 0.2f; h.chin = 0.8f; h.cheek = 0.8f; h.rx *= 1.03f; break;
                case 2: h.jawTaper = 0.18f; h.ry *= 1.04f; h.chin = 1.05f; break;
                case 3: h.jawTaper = 0.12f; h.brow = 1.8f; h.chin = 1.15f; break;
            }
            if (fem) { h.brow *= 0.55f; h.jawTaper += 0.06f; h.chin *= 0.8f; h.lips = 1.15f; h.noseLen *= 0.85f; h.noseW *= 0.9f; }
            switch (a.race)
            {
                case RaceId.Dwarf: h.jawTaper *= 0.55f; h.noseLen = 1.35f; h.noseW = 1.3f; h.brow *= 1.4f; break;
                case RaceId.Gnome: h.noseLen = 1.65f; h.noseW = 1.25f; h.cheek *= 1.3f; h.chin *= 0.8f; break;
                case RaceId.Halfling: h.noseLen *= 0.85f; h.cheek *= 1.3f; h.jawTaper += 0.06f; break;
                case RaceId.HalfOrc: h.jawTaper *= 0.45f; h.brow *= 1.8f; h.noseLen = 0.9f; h.noseW = 1.55f; h.underbite = 1f; h.lips = 1.2f; break;
                case RaceId.Elf: case RaceId.Drow: h.jawTaper += 0.06f; h.cheek *= 1.35f; h.noseW *= 0.85f; h.brow *= 0.7f; break;
                case RaceId.HalfElf: h.cheek *= 1.15f; h.noseW *= 0.92f; break;
                case RaceId.Githyanki: h.gaunt = 1f; h.noseLen = 0.35f; h.noseW = 0.8f; h.cheek *= 1.5f; h.brow *= 0.6f; h.jawTaper += 0.05f; h.lips = 0.7f; break;
                case RaceId.Tiefling: h.cheek *= 1.2f; break;
                case RaceId.Firbolg: h.noseLen = 1.55f; h.noseW = 1.4f; h.cheek *= 1.25f; h.brow *= 1.3f; h.jawTaper *= 0.8f; h.chin *= 1.1f; break;
                case RaceId.Dragonborn: h.snout = 1f; h.brow = 2.4f; h.noseLen = 0f; h.lips = 0f; h.chin = 0f; h.jawTaper = 0.1f; h.cranium = 0.95f; break;
            }
            if (a.gaunt) h.gaunt = Mathf.Max(h.gaunt, 0.8f);
            if (a.skeletal) { h.skeletal = true; h.gaunt = 1.6f; h.noseLen = 0; h.lips = 0; }
            h.gaunt += a.age * 0.4f;
            return h;
        }

        /// <summary>Surface point of the sculpted head in model space, plus its outward direction.</summary>
        static Vector3 Sculpt(HeadShape h, float a, float e, out Vector3 dir)
        {
            float ce = Mathf.Cos(e);
            dir = new Vector3(ce * Mathf.Sin(a), Mathf.Sin(e), ce * Mathf.Cos(a));
            Vector3 p = new Vector3(dir.x * h.rx, dir.y * h.ry, dir.z * h.rz);
            // skull: back of head fuller, face plane flatter
            if (p.z < 0) p.z *= 1.06f * h.cranium;
            else p.z *= 0.95f;
            if (p.y > 0) p.y *= 1.02f;
            // jaw taper and tuck
            float jaw = SmoothR(-0.12f, -1.15f, e);
            p.x *= 1 - h.jawTaper * jaw;
            if (p.z > 0) p.z += h.rz * (0.015f + 0.05f * h.underbite) * jaw * Mathf.Max(0, Mathf.Cos(a));
            else p.z *= 1 - 0.35f * jaw;
            // features as radial offsets (fraction of mean radius)
            float f = 0;
            if (h.snout > 0)
            {
                // dragon muzzle
                float mz = Mathf.Exp(-Mathf.Pow(Mathf.DeltaAngle(0, a * Mathf.Rad2Deg) * Mathf.Deg2Rad / 0.55f, 2)) * SmoothR(0.12f, -0.1f, e) * SmoothR(-0.95f, -0.6f, e);
                f += 0.95f * mz;
                f += 0.06f * h.brow * G2(a, e, 0.42f, 0.2f, 0.22f, 0.08f);
                f -= 0.05f * G2(a, e, 0.42f, 0.08f, 0.14f, 0.08f);
                f -= 0.035f * G(a, e, 0f, -0.42f, 0.9f, 0.025f) * SmoothR(0.6f, 0.2f, Mathf.Abs(a));   // mouth crease
                f += 0.05f * G2(a, e, 0.9f, -0.25f, 0.2f, 0.25f);   // cheek plates
            }
            else
            {
                f += 0.045f * h.brow * G2(a, e, 0.32f, h.browE, 0.26f, 0.07f) + 0.02f * h.brow * G(a, e, 0, h.browE - 0.02f, 0.2f, 0.06f);
                f -= 0.06f * G2(a, e, h.eyeA, h.eyeE, 0.16f, 0.11f);
                f += 0.05f * h.cheek * G2(a, e, 0.56f, -0.12f, 0.22f, 0.12f);
                f -= 0.05f * h.gaunt * G2(a, e, 0.62f, -0.36f, 0.2f, 0.16f);
                f -= 0.025f * G2(a, e, 1.0f, 0.35f, 0.25f, 0.2f);
                // nose
                if (e < 0.18f && h.noseLen > 0.01f)
                {
                    float tt = Mathf.Clamp01((0.16f - e) / (0.16f - h.noseTipE));
                    float prof = e >= h.noseTipE ? Mathf.Pow(tt, 1.7f) : Mathf.Exp(-Mathf.Pow((e - h.noseTipE) / 0.05f, 2));
                    float w = Mathf.Lerp(0.05f, 0.11f, tt) * h.noseW;
                    float da = Mathf.DeltaAngle(0, a * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                    f += h.noseLen * 0.3f * prof * Mathf.Exp(-(da / w) * (da / w));
                    f += 0.045f * h.noseW * h.noseLen * G2(a, e, 0.1f * h.noseW, h.noseTipE - 0.01f, 0.05f, 0.04f);
                }
                // lips & mouth
                f += 0.02f * h.lips * G(a, e, 0, h.lipUpE, 0.2f, 0.035f) + 0.026f * h.lips * G(a, e, 0, h.lipLoE, 0.17f, 0.04f);
                f -= 0.03f * G(a, e, 0, h.mouthE, 0.2f, 0.013f);
                f -= 0.02f * G2(a, e, 0.22f, h.mouthE, 0.04f, 0.04f);
                f -= 0.012f * G(a, e, 0, -0.31f, 0.035f, 0.04f);
                f += 0.045f * h.chin * G(a, e, 0, h.chinE, 0.32f, 0.12f);
                if (h.underbite > 0) f += 0.05f * G(a, e, 0, -0.62f, 0.5f, 0.12f);
            }
            f += 0.04f * G(a, e, Mathf.PI, 0.15f, 0.9f, 0.5f);
            f -= 0.03f * G2(a, e, 1.55f, 0.0f, 0.2f, 0.25f);
            if (h.skeletal) { f -= 0.12f * G2(a, e, h.eyeA, h.eyeE, 0.14f, 0.12f); f -= 0.1f * G(a, e, 0, -0.2f, 0.07f, 0.08f); }
            float rm = (h.rx + h.ry + h.rz) / 3f;
            Vector3 n = new Vector3(p.x / (h.rx * h.rx), p.y / (h.ry * h.ry), p.z / (h.rz * h.rz)).normalized;
            p += n * f * rm;
            // crisp jawline: flatten the underside of the head into a plane rising toward the back
            if (e < -0.45f && h.snout <= 0)
            {
                float zn = Mathf.Clamp(p.z / h.rz, -1f, 1f);
                float planeY = h.ry * (-0.8f + 0.42f * Mathf.Max(0, -zn) + 0.05f * Mathf.Max(0, zn));
                if (p.y < planeY) p.y = Mathf.Lerp(p.y, planeY, 0.92f);
            }
            if (h.snout > 0 && p.z > h.rz * 0.9f)
            {
                // taper the muzzle toward the tip
                float k = Mathf.Clamp01((p.z - h.rz * 0.9f) / (h.rz * 0.9f));
                p.x *= 1 - 0.38f * k;
                p.y = Mathf.Lerp(p.y, -h.ry * 0.22f, 0.3f * k);
            }
            dir = n;
            return h.c + p;
        }

        static float HairlineE(float absA, int style)
        {
            // piecewise hairline elevation by azimuth (0 front .. pi back)
            float[] A = { 0f, 0.7f, 1.12f, 1.32f, 1.5f, 1.72f, 2.1f, Mathf.PI };
            float[] E = { 0.56f, 0.5f, 0.32f, -0.08f, 0.22f, 0.12f, -0.12f, -0.55f };
            if (style == 1) { E[3] = 0.05f; E[7] = -0.42f; }
            if (style == 10) { E[0] = 0.62f; E[1] = 0.55f; E[3] = 0.1f; E[7] = -0.4f; }
            for (int i = 0; i < A.Length - 1; i++)
                if (absA <= A[i + 1]) return Mathf.Lerp(E[i], E[i + 1], (absA - A[i]) / (A[i + 1] - A[i]));
            return E[E.Length - 1];
        }

        static float ScalpMask(float a, float e, int style)
        {
            float absA = Mathf.Abs(Mathf.DeltaAngle(0, a * Mathf.Rad2Deg) * Mathf.Deg2Rad);
            float hl = HairlineE(absA, style);
            float m = SmoothR(hl - 0.03f, hl + 0.06f, e);
            if (style == 6) m *= SmoothR(0.2f, 0.11f, Mathf.Abs(Mathf.Sin(a)) * Mathf.Cos(e));
            return m;
        }

        static float BeardMask(HeadShape h, float a, float e, int style, out float moustache)
        {
            float da = Mathf.DeltaAngle(0, a * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            float absA = Mathf.Abs(da);
            moustache = Mathf.Exp(-(Mathf.Pow(da / 0.24f, 2) + Mathf.Pow((e - (h.lipUpE + 0.045f)) / 0.045f, 2)));
            if (style == 5) return 0;
            float lower = SmoothR(-0.12f, -0.3f, e) * SmoothR(1.5f, 1.28f, absA);
            float mouthHole = Mathf.Exp(-Mathf.Pow(Mathf.Pow(da / 0.25f, 2) + Mathf.Pow((e - h.mouthE) / 0.075f, 2), 2));
            float neckFade = SmoothR(-1.45f, -1.2f, e);
            float m = lower * (1 - mouthHole) * neckFade;
            if (style == 2) m *= SmoothR(1.4f, 1.1f, absA);
            return Mathf.Clamp01(m);
        }

        // -----------------------------------------------------------------------------------------
        static void BuildHeadSculpt(MeshBuilder mb, Appearance a, GearLook g, Dims d, RaceLook look)
        {
            var h = MakeShape(a, d, look);
            int head = I(B.Head);
            float hs = d.hs;
            // --- skin surface (FACE submesh, spherical UVs for painting) ---
            int start = mb.verts.Count;
            var grid = new Vector3[(LAT + 1) * (LON + 1)];
            var gdir = new Vector3[grid.Length];
            var ga = new float[grid.Length];
            var ge = new float[grid.Length];
            for (int y = 0; y <= LAT; y++)
            {
                float v = y / (float)LAT;
                float e = Mathf.PI / 2 - v * Mathf.PI;
                for (int x = 0; x <= LON; x++)
                {
                    float t = x / (float)LON * 2 - 1;
                    float av = Mathf.PI * Mathf.Sign(t) * Mathf.Pow(Mathf.Abs(t), 1.45f);
                    int idx = y * (LON + 1) + x;
                    grid[idx] = Sculpt(h, av, e, out gdir[idx]);
                    ga[idx] = av; ge[idx] = e;
                    mb.AddVertexRaw(grid[idx], gdir[idx], new Vector2(av / (2 * Mathf.PI) + 0.5f, e / Mathf.PI + 0.5f), MeshBuilder.W(head));
                }
            }
            for (int y = 0; y < LAT; y++)
                for (int x = 0; x < LON; x++)
                {
                    int i0 = start + y * (LON + 1) + x, i1 = i0 + 1, i2 = i0 + LON + 1, i3 = i2 + 1;
                    mb.Tri(FACE, i0, i3, i1); mb.Tri(FACE, i0, i2, i3);
                }
            mb.RecalcNormalsRange(start, mb.verts.Count, FACE);

            // --- eyes ---
            if (h.snout <= 0 || true)
            {
                bool solid = a.race == RaceId.Tiefling || (a.race == RaceId.Drow && a.subrace == SubraceId.LolthSworn) || a.glowEyes.a > 0 || look.dragon;
                float eyeR = (look.dragon ? 0.11f : 0.155f) * h.rx;
                float eA = look.dragon ? 0.5f : h.eyeA, eE = look.dragon ? 0.1f : h.eyeE;
                for (int side = -1; side <= 1; side += 2)
                {
                    var sp = Sculpt(h, side * eA, eE, out var n);
                    Vector3 ec = sp - n * eyeR * 0.3f;
                    Vector3 gaze = Vector3.Slerp(Vector3.forward, new Vector3(side, 0, 0), look.dragon ? 0.35f : 0.06f).normalized;
                    if (a.skeletal)
                    {
                        mb.bone = head;
                        mb.AddEllipsoid(DARK, ec, Vector3.one * eyeR * 1.1f, 6, 8);
                        if (a.glowEyes.a > 0) mb.AddEllipsoid(IRIS, ec + gaze * eyeR * 0.5f, Vector3.one * eyeR * 0.35f, 4, 6);
                        continue;
                    }
                    mb.bone = head; mb.bone2 = -1;
                    mb.Push();
                    mb.Translate(ec);
                    mb.Rotate(Quaternion.LookRotation(gaze, Vector3.up));
                    mb.AddEllipsoid(solid ? IRIS : EYEW, Vector3.zero, Vector3.one * eyeR, 8, 12);
                    if (!solid)
                    {
                        mb.AddEllipsoid(IRIS, new Vector3(0, 0, eyeR * 0.74f), new Vector3(eyeR * 0.64f, eyeR * 0.64f, eyeR * 0.32f), 7, 12);
                        mb.AddEllipsoid(DARK, new Vector3(0, 0, eyeR * 0.94f), new Vector3(eyeR * 0.26f, eyeR * 0.26f, eyeR * 0.1f), 4, 8);
                    }
                    else if (look.dragon)
                        mb.AddEllipsoid(DARK, new Vector3(0, 0, eyeR * 0.9f), new Vector3(eyeR * 0.12f, eyeR * 0.6f, eyeR * 0.15f), 4, 8);
                    // lids: upper covers top ~40%, lower thin rim
                    AddLid(mb, eyeR * 1.08f, 0, look.dragon ? 70 : 66, look.dragon ? SKIN : FACE, new Vector2(side * eA / (2 * Mathf.PI) + 0.5f, (eE + 0.07f) / Mathf.PI + 0.5f));
                    AddLid(mb, eyeR * 1.05f, 122, 180, look.dragon ? SKIN : FACE, new Vector2(side * eA / (2 * Mathf.PI) + 0.5f, (eE - 0.1f) / Mathf.PI + 0.5f));
                    mb.Pop();
                }
            }

            // --- ears ---
            if (look.ears != EarType.None && !a.skeletal && !look.dragon)
                for (int side = -1; side <= 1; side += 2)
                {
                    var ep = Sculpt(h, side * 1.53f, h.eyeE - 0.12f, out var n);
                    Vector3 er; Quaternion rot; Vector3 off;
                    switch (look.ears)
                    {
                        case EarType.LongElf: er = new Vector3(0.1f, 0.5f, 0.2f); rot = Quaternion.Euler(-50, 0, side * -24); off = new Vector3(0, 0.3f, 0); break;
                        case EarType.HalfElf: er = new Vector3(0.1f, 0.34f, 0.19f); rot = Quaternion.Euler(-28, 0, side * -12); off = new Vector3(0, 0.14f, 0); break;
                        case EarType.Gnome: er = new Vector3(0.1f, 0.44f, 0.22f); rot = Quaternion.Euler(-30, 0, side * -60); off = new Vector3(0, 0.25f, 0); break;
                        case EarType.Githyanki: er = new Vector3(0.08f, 0.52f, 0.17f); rot = Quaternion.Euler(-74, 0, side * -12); off = new Vector3(0, 0.3f, 0); break;
                        case EarType.Firbolg: er = new Vector3(0.1f, 0.8f, 0.26f); rot = Quaternion.Euler(-14, 0, side * -104); off = new Vector3(0, 0.5f, 0); break;
                        case EarType.Orc: er = new Vector3(0.11f, 0.3f, 0.2f); rot = Quaternion.Euler(-26, 0, side * -18); off = new Vector3(0, 0.1f, 0); break;
                        default: er = new Vector3(0.1f, 0.27f, 0.18f); rot = Quaternion.Euler(-8, 0, side * -8); off = Vector3.zero; break;
                    }
                    float rs = h.rx;
                    mb.bone = head;
                    mb.Push();
                    mb.Translate(ep - n * rs * 0.06f + new Vector3(0, 0, -rs * 0.05f));
                    mb.Rotate(rot);
                    Vector3 ers = er * rs;
                    mb.AddEllipsoid(SKIN, off * rs, ers, 7, 10, p =>
                    {
                        // cup the ear (hollow facing forward/out) and point the tip
                        float yn = (p.y - off.y * rs) / ers.y;
                        p.z *= 1 - 0.55f * Mathf.Max(0, yn);
                        if (p.x * side > 0) p.x -= side * ers.x * 0.6f * (1 - Mathf.Abs(yn));
                        return p;
                    });
                    mb.Pop();
                }

            // --- tusks ---
            if (look.tusks)
                for (int side = -1; side <= 1; side += 2)
                {
                    var tp = Sculpt(h, side * 0.17f, h.lipLoE, out var n);
                    mb.bone = head;
                    mb.Push(); mb.Translate(tp - n * 0.004f * hs); mb.Rotate(Quaternion.Euler(-14, 0, -side * 10));
                    mb.AddCone(HORN, Vector3.zero, 0.0055f * hs, 0.026f * hs, 7);
                    mb.Pop();
                }

            // --- dragon extras: nostrils, horns, frills, teeth ---
            if (look.dragon) DragonExtras(mb, a, h);

            // --- hair / hood / helmet ---
            if (a.hooded) BuildHood(mb, d);
            else if (g.helmet) BuildHelmet(mb, d, g);
            else if (!look.dragon) BuildHairSculpt(mb, a, d, h, grid, gdir, ga, ge);
            if (!g.helmet && !look.dragon && a.beardStyle >= 2) BuildBeardSculpt(mb, a, d, h, grid, gdir, ga, ge);
            if (a.beardStyle == 5) Moustache(mb, a, h);

            if (look.horns) BuildHornsSculpt(mb, a, h);
            if (a.crown)
            {
                mb.bone = head;
                var cr = new List<MeshBuilder.Ring>
                {
                    R(h.c + Vector3.up * h.ry * 0.5f, h.rx * 1.08f, h.rz * 1.08f, MeshBuilder.W(head)),
                    R(h.c + Vector3.up * h.ry * 0.66f, h.rx * 1.02f, h.rz * 1.02f, MeshBuilder.W(head)),
                };
                mb.AddLoft(TRIM, cr, 24, false, false);
                for (int i = 0; i < 10; i++)
                {
                    float ang = i / 10f * Mathf.PI * 2;
                    Vector3 p = h.c + new Vector3(Mathf.Cos(ang) * h.rx * 1.02f, h.ry * 0.64f, Mathf.Sin(ang) * h.rz * 1.02f);
                    mb.AddCone(TRIM, p, 0.008f * hs, 0.028f * hs, 5);
                }
            }
        }

        const int FACE_LID = SKIN;

        static void AddLid(MeshBuilder mb, float r, float phi0, float phi1, int sub, Vector2? fixedUV = null)
        {
            // partial sphere shell around local +Z gaze; phi measured from top (+Y) toward bottom
            int lat = 5, lon = 12, start = mb.verts.Count;
            for (int y = 0; y <= lat; y++)
            {
                float phi = Mathf.Lerp(phi0, phi1, y / (float)lat) * Mathf.Deg2Rad;
                for (int x = 0; x <= lon; x++)
                {
                    float th = Mathf.Lerp(-100, 100, x / (float)lon) * Mathf.Deg2Rad; // around the front
                    Vector3 dd = new Vector3(Mathf.Sin(phi) * Mathf.Sin(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Cos(th));
                    mb.AddVertex(dd * r, dd, fixedUV ?? new Vector2(x / (float)lon, y / (float)lat));
                }
            }
            for (int y = 0; y < lat; y++)
                for (int x = 0; x < lon; x++)
                {
                    int i0 = start + y * (lon + 1) + x, i1 = i0 + 1, i2 = i0 + lon + 1, i3 = i2 + 1;
                    mb.Tri(sub, i0, i3, i1); mb.Tri(sub, i0, i2, i3);
                }
        }

        static void BuildHairSculpt(MeshBuilder mb, Appearance a, Dims d, HeadShape h, Vector3[] grid, Vector3[] gdir, float[] ga, float[] ge)
        {
            int st = a.hairStyle;
            if (st == 0) return;
            int head = I(B.Head);
            float hs = d.hs;
            float thick;
            switch (st)
            {
                case 1: thick = 0.006f; break;
                case 6: thick = 0.03f; break;
                case 9: thick = 0.02f; break;
                case 2: case 8: thick = 0.011f; break;
                case 10: thick = 0.01f; break;
                default: thick = 0.009f; break;
            }
            thick *= hs;
            int start = mb.verts.Count;
            var mask = new float[grid.Length];
            for (int i = 0; i < grid.Length; i++)
            {
                mask[i] = ScalpMask(ga[i], ge[i], st);
                float top = 0.65f + 0.35f * Mathf.Max(0, Mathf.Sin(ge[i]));
                float off = thick * top * SmoothR(0f, 1f, mask[i]) - 0.0025f * hs;
                if (st == 9) off *= 1f + 0.25f * Mathf.Sin(ga[i] * 9) * Mathf.Sin(ge[i] * 7);
                if (st == 6) off = 0.004f * hs + thick * Mathf.Pow(mask[i], 2) * Mathf.Max(0, Mathf.Sin(ge[i] + 0.6f));
                mb.AddVertexRaw(grid[i] + gdir[i] * off, gdir[i], new Vector2(ga[i] / Mathf.PI * 3f, ge[i] * 2f), MeshBuilder.W(head));
            }
            for (int y = 0; y < LAT; y++)
                for (int x = 0; x < LON; x++)
                {
                    int k0 = y * (LON + 1) + x, k1 = k0 + 1, k2 = k0 + LON + 1, k3 = k2 + 1;
                    float mx = Mathf.Max(Mathf.Max(mask[k0], mask[k1]), Mathf.Max(mask[k2], mask[k3]));
                    if (mx < 0.01f) continue;
                    mb.Tri(HAIR, start + k0, start + k3, start + k1); mb.Tri(HAIR, start + k0, start + k2, start + k3);
                }
            mb.RecalcNormalsRange(start, mb.verts.Count, HAIR);

            var rnd = new System.Random(a.hairStyle * 131 + (int)(a.hair.r * 1000));
            switch (st)
            {
                case 2: case 8: case 9:
                    {
                        float len = (st == 8 ? 0.13f : (st == 9 ? 0.26f : 0.34f)) * d.s;
                        HairCurtain(mb, d, h, len, st == 9 ? 1.25f : 1f, st == 8);
                        break;
                    }
                case 3: // ponytail
                    {
                        var pts = new List<Vector3>(); var rr = new List<float>();
                        Vector3 b0 = h.c + new Vector3(0, h.ry * 0.25f, -h.rz * 1.05f);
                        for (int i = 0; i < 8; i++)
                        {
                            float t = i / 7f;
                            pts.Add(b0 + new Vector3(0, -t * 0.3f * d.s, -0.03f * d.s * Mathf.Sin(t * 2.2f)));
                            rr.Add(Mathf.Lerp(0.028f, 0.01f, t) * hs * (i == 0 ? 1.1f : 1f));
                        }
                        HairTube(mb, d, pts, rr);
                        mb.bone = head;
                        mb.AddCylinder(TRIM, b0 + new Vector3(0, -0.012f * hs, 0), 0.02f * hs, 0.02f * hs, 0.012f * hs, 10);
                        break;
                    }
                case 4: // topknot
                    mb.bone = head;
                    mb.AddEllipsoid(HAIR, h.c + new Vector3(0, h.ry * 1.08f, -h.rz * 0.1f), new Vector3(0.03f, 0.028f, 0.03f) * hs, 7, 10);
                    HairTube(mb, d, new List<Vector3> { h.c + new Vector3(0, h.ry * 1.12f, -h.rz * 0.2f), h.c + new Vector3(0, h.ry * 1.25f, -h.rz * 0.5f), h.c + new Vector3(0, h.ry * 1.05f, -h.rz * 1.1f), h.c + new Vector3(0, h.ry * 0.6f, -h.rz * 1.3f) }, new List<float> { 0.016f * hs, 0.015f * hs, 0.011f * hs, 0.005f * hs });
                    break;
                case 7: // bun
                    mb.bone = head;
                    mb.AddEllipsoid(HAIR, h.c + new Vector3(0, h.ry * 0.42f, -h.rz * 1.08f), new Vector3(0.045f, 0.042f, 0.04f) * hs, 8, 12);
                    break;
                case 10: // tousled: short tapered tufts from the crown, a fringe above the brows, spiky tips
                    {
                        int n = 58;
                        for (int k = 0; k < n; k++)
                        {
                            float u = (k + (float)rnd.NextDouble() * 0.6f) / n;
                            float az = Mathf.Lerp(-Mathf.PI, Mathf.PI, u);
                            float absA = Mathf.Abs(az);
                            float front = Mathf.Clamp01(1f - absA / 1.2f), back = Mathf.Clamp01((absA - 1.9f) / 1.2f);
                            float e0 = Mathf.Lerp(1.05f, 1.28f, (float)rnd.NextDouble());
                            // where the tuft ends: over the brow in front, above the ear at the sides, the nape behind
                            float e1 = Mathf.Lerp(Mathf.Lerp(0.14f, 0.34f, front), -0.42f, back) + ((float)rnd.NextDouble() - 0.5f) * 0.12f;
                            float drift = ((float)rnd.NextDouble() - 0.5f) * 0.5f + (front > 0 ? -Mathf.Sign(az) * 0.15f * front : 0f);
                            float wdt = Mathf.Lerp(0.024f, 0.036f, (float)rnd.NextDouble()) * hs;
                            float liftK = Mathf.Lerp(0.012f, 0.03f, (float)rnd.NextDouble()) * hs * (1f - back * 0.6f);
                            Tuft(mb, d, h, az, e0, az + drift, e1, wdt, liftK);
                        }
                        // a few cowlick spikes kicking up and back out of the crown
                        for (int k = 0; k < 7; k++)
                        {
                            float az = Mathf.PI + ((float)rnd.NextDouble() - 0.5f) * 2.2f;
                            Tuft(mb, d, h, az, 1.12f, az + ((float)rnd.NextDouble() - 0.5f) * 0.4f, 0.7f, 0.03f * hs, 0.05f * hs);
                        }
                        break;
                    }
                case 5: // braids
                    for (int side = -1; side <= 1; side += 2)
                    {
                        var pts = new List<Vector3>(); var rr = new List<float>();
                        Vector3 b0 = h.c + new Vector3(side * h.rx * 0.95f, -h.ry * 0.05f, -h.rz * 0.15f);
                        for (int i = 0; i < 12; i++)
                        {
                            float t = i / 11f;
                            pts.Add(b0 + new Vector3(side * 0.02f * t * d.s, -t * 0.3f * d.s, 0.06f * t * d.s));
                            rr.Add((0.015f + 0.0045f * Mathf.Sin(i * 2.4f)) * hs);
                        }
                        HairTube(mb, d, pts, rr);
                    }
                    break;
            }
        }

        /// <summary>Long hair as one sculpted volume: wraps the back and sides of the head and falls to the shoulders.</summary>
        static void HairCurtain(MeshBuilder mb, Dims d, HeadShape h, float length, float volume, bool bob)
        {
            var rings = new List<MeshBuilder.Ring>();
            float top = h.c.y + h.ry * 0.35f;
            float headBottom = h.c.y - h.ry * 0.55f;
            float end = headBottom - length;
            int n = 14;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                float y = Mathf.Lerp(top, end, t);
                float rx, rz, cz;
                if (y > headBottom)
                {
                    float dy = Mathf.Clamp((y - h.c.y) / (h.ry * 1.05f), -0.95f, 0.95f);
                    float k = Mathf.Sqrt(1 - dy * dy);
                    rx = h.rx * 1.2f * volume * Mathf.Max(0.62f, k);
                    rz = h.rz * 1.1f * volume * Mathf.Max(0.6f, k);
                    cz = h.c.z - h.rz * 0.05f;
                }
                else
                {
                    float u = Mathf.InverseLerp(headBottom, end, y);
                    float spread = bob ? 0.9f : Mathf.Lerp(0.72f, 1.05f, MathX.Smoothstep(0, 1, u));
                    rx = Mathf.Lerp(h.rx * 0.62f, d.shoulderW * 0.62f * spread, MathX.Smoothstep(0, 1, u)) * volume;
                    rz = Mathf.Lerp(h.rz * 0.6f, d.chestD * 0.75f, MathX.Smoothstep(0, 1, u)) * volume;
                    // start behind the jaw hinge so a turning jaw never pushes through the curtain
                    cz = Mathf.Lerp(h.c.z - h.rz * 0.4f, -d.chestD * 0.9f, MathX.Smoothstep(0, 1, u));
                }
                float inset = y > h.c.y - h.ry * 0.2f ? 0.78f : 0.3f;
                float endTaper = Mathf.Lerp(1f, 0.85f, MathX.Smoothstep(0.7f, 1f, t));
                var ring = new MeshBuilder.Ring
                {
                    center = new Vector3(h.c.x, y, cz),
                    rot = Quaternion.identity,
                    rx = rx * endTaper,
                    rz = rz * endTaper,
                    weight = HairWeight(d, y),
                    radial = ang =>
                    {
                        float sn = Mathf.Sin(ang);
                        float w = MathX.Smoothstep(0f, 1f, Mathf.InverseLerp(0.15f, 0.75f, sn));
                        float wave = 1f + 0.035f * Mathf.Sin(ang * 11f) + 0.02f * Mathf.Sin(ang * 23f);
                        return Mathf.Lerp(1f, inset, w) * wave;
                    }
                };
                rings.Add(ring);
            }
            mb.AddLoft(HAIR, rings, 28, false, true);
        }

        /// <summary>A short flat tuft that follows the skull from (az0,e0) to (az1,e1), its tip lifting away from the scalp.</summary>
        static void Tuft(MeshBuilder mb, Dims d, HeadShape h, float az0, float e0, float az1, float e1, float width, float lift)
        {
            const int steps = 7;
            var rings = new List<MeshBuilder.Ring>();
            var pts = new Vector3[steps + 1]; var nrm = new Vector3[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float az = Mathf.Lerp(az0, az1, t), e = Mathf.Lerp(e0, e1, t);
                Vector3 p = Sculpt(h, az, e, out var n);
                pts[i] = p + n * (0.013f * h.hs + lift * t * t);
                nrm[i] = n;
            }
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 dir = (i < steps ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1]).normalized;
                Vector3 z = (nrm[i] - Vector3.Dot(nrm[i], dir) * dir).normalized;
                if (z.sqrMagnitude < 0.01f) z = Vector3.back;
                float w = width * Mathf.Lerp(1f, 0.08f, Mathf.Pow(t, 1.3f));
                rings.Add(new MeshBuilder.Ring { center = pts[i], rot = Quaternion.LookRotation(z, dir), rx = w, rz = width * 0.32f * (1 - t * 0.7f), weight = MeshBuilder.W(I(B.Head)) });
            }
            mb.AddLoft(HAIR, rings, 6, false, true);
        }

        static void HairTube(MeshBuilder mb, Dims d, List<Vector3> pts, List<float> rr)
        {
            var rings = new List<MeshBuilder.Ring>();
            for (int i = 0; i < pts.Count; i++)
            {
                Vector3 dir = i < pts.Count - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1];
                rings.Add(new MeshBuilder.Ring { center = pts[i], rot = Quaternion.FromToRotation(Vector3.up, dir.normalized), rx = rr[i], rz = rr[i], weight = HairWeight(d, pts[i].y) });
            }
            mb.AddLoft(HAIR, rings, 8, true, true);
        }

        static BoneWeight HairWeight(Dims d, float y)
        {
            // hair hangs from the scalp, so it must turn with the head: if the part beside the jaw followed the neck,
            // the face would swing into it whenever a character looks around. Only the ends settle onto the shoulders.
            float t = Mathf.InverseLerp(d.chinY, d.shoulderY - 0.1f * d.s, y);
            if (t <= 0.45f) return MeshBuilder.W(I(B.Head));
            if (t < 0.85f) return MeshBuilder.W(I(B.Head), I(B.Neck), (t - 0.45f) / 0.4f);
            return MeshBuilder.W(I(B.Neck), I(B.Chest), Mathf.Clamp01((t - 0.85f) / 0.15f));
        }

        /// <summary>A flat tapered lock that starts on the scalp, follows the skull and falls with gravity.</summary>
        static void Lock(MeshBuilder mb, Dims d, HeadShape h, float az, float e0, float length, float width, System.Random rnd, float jitter)
        {
            var pts = new List<Vector3>();
            var outs = new List<Vector3>();
            float e = e0;
            Vector3 p = Sculpt(h, az, e, out var n);
            p += n * 0.012f * h.hs;
            pts.Add(p); outs.Add(n);
            float step = 0.03f * d.s;
            float travelled = 0;
            float wob = (float)rnd.NextDouble() * 6f;
            while (travelled < length && pts.Count < 22)
            {
                Vector3 next;
                if (e > -0.25f)
                {
                    // slide down the skull surface
                    e -= step / h.ry;
                    next = Sculpt(h, az, e, out n) + n * (0.014f * h.hs);
                }
                else
                {
                    next = p + Vector3.down * step;
                    Vector3 rel = next - h.c;
                    // keep clear of head/neck/shoulders
                    Vector2 xz = new Vector2(rel.x, rel.z);
                    float minR = next.y > d.shoulderY + 0.02f ? d.neckR * 1.9f + 0.01f * h.hs : d.shoulderW * 0.9f;
                    if (next.y < d.shoulderY + 0.02f && rel.z < 0) minR = Mathf.Max(minR, d.chestD * 1.2f + 0.015f);
                    if (xz.magnitude < minR) { xz = xz.normalized * minR; next.x = h.c.x + xz.x; next.z = h.c.z + xz.y; }
                    if (next.y < d.shoulderY + 0.03f && rel.z >= -0.02f)
                    {
                        // side locks drape over the front of the shoulders
                        next.z = Mathf.Max(next.z, d.chestD * 0.4f);
                    }
                    next += new Vector3(Mathf.Sin(wob + travelled * 20) * jitter * 0.01f, 0, Mathf.Cos(wob + travelled * 17) * jitter * 0.01f);
                    n = new Vector3(rel.x, 0, rel.z).normalized;
                }
                travelled += Vector3.Distance(p, next);
                p = next;
                pts.Add(p); outs.Add(n);
            }
            if (pts.Count < 3) return;
            var rings = new List<MeshBuilder.Ring>();
            for (int i = 0; i < pts.Count; i++)
            {
                float t = i / (float)(pts.Count - 1);
                Vector3 dir = (i < pts.Count - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1]).normalized;
                Vector3 z = (outs[i] - Vector3.Dot(outs[i], dir) * dir).normalized;
                if (z.sqrMagnitude < 0.01f) z = Vector3.back;
                var rot = Quaternion.LookRotation(z, dir);
                float w = width * Mathf.Lerp(1f, 0.25f, Mathf.Pow(t, 1.4f));
                rings.Add(new MeshBuilder.Ring { center = pts[i], rot = rot, rx = w, rz = width * 0.28f * (1 - t * 0.6f), weight = HairWeight(d, pts[i].y) });
            }
            mb.AddLoft(HAIR, rings, 6, false, true);
        }

        static void BuildBeardSculpt(MeshBuilder mb, Appearance a, Dims d, HeadShape h, Vector3[] grid, Vector3[] gdir, float[] ga, float[] ge)
        {
            int st = a.beardStyle;
            int head = I(B.Head);
            float hs = d.hs;
            float thick = (st == 2 ? 0.008f : 0.014f) * hs;
            float drop = st == 3 ? 0.05f * d.s : (st == 4 ? 0.07f * d.s : 0f);
            int start = mb.verts.Count;
            var mask = new float[grid.Length];
            for (int i = 0; i < grid.Length; i++)
            {
                float m = BeardMask(h, ga[i], ge[i], st, out float mo);
                if (st >= 3) m = Mathf.Max(m, mo * 0.9f);
                mask[i] = m;
                float off = thick * SmoothR(0, 1, m) - 0.002f * hs;
                Vector3 p = grid[i] + gdir[i] * off;
                float chinPull = SmoothR(-0.5f, -1.05f, ge[i]) * Mathf.Max(0, Mathf.Cos(ga[i])) * m;
                p += Vector3.down * drop * chinPull + Vector3.forward * drop * 0.35f * chinPull;
                mb.AddVertexRaw(p, gdir[i], new Vector2(ga[i] / Mathf.PI * 3f, ge[i] * 3f), MeshBuilder.W(head));
            }
            for (int y = 0; y < LAT; y++)
                for (int x = 0; x < LON; x++)
                {
                    int k0 = y * (LON + 1) + x, k1 = k0 + 1, k2 = k0 + LON + 1, k3 = k2 + 1;
                    float mx = Mathf.Max(Mathf.Max(mask[k0], mask[k1]), Mathf.Max(mask[k2], mask[k3]));
                    if (mx < 0.01f) continue;
                    mb.Tri(HAIR, start + k0, start + k3, start + k1); mb.Tri(HAIR, start + k0, start + k2, start + k3);
                }
            mb.RecalcNormalsRange(start, mb.verts.Count, HAIR);
            if (st == 4)
            {
                // long braided beard
                var pts = new List<Vector3>(); var rr = new List<float>();
                Vector3 b0 = Sculpt(h, 0, h.chinE - 0.15f, out _) + new Vector3(0, -drop * 0.8f, 0.005f);
                for (int i = 0; i < 8; i++)
                {
                    float t = i / 7f;
                    pts.Add(b0 + new Vector3(0, -t * 0.2f * d.s, 0.035f * d.s * Mathf.Sin(t * 1.6f)));
                    rr.Add(Mathf.Lerp(0.03f, 0.012f, t) * hs);
                }
                var rings = new List<MeshBuilder.Ring>();
                for (int i = 0; i < pts.Count; i++)
                {
                    Vector3 dir = i < pts.Count - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1];
                    rings.Add(new MeshBuilder.Ring { center = pts[i], rot = Quaternion.FromToRotation(Vector3.up, dir.normalized), rx = rr[i] * 1.2f, rz = rr[i], weight = HairWeight(d, pts[i].y) });
                }
                mb.AddLoft(HAIR, rings, 8, true, true);
                mb.bone = head;
                for (int b = 1; b < 3; b++) mb.AddEllipsoid(TRIM, pts[b * 2 + 1], new Vector3(rr[b * 2 + 1] * 1.35f, 0.007f * hs, rr[b * 2 + 1] * 1.2f), 4, 10);
            }
        }

        static void Moustache(MeshBuilder mb, Appearance a, HeadShape h)
        {
            if (a.skeletal) return;
            int head = I(B.Head);
            mb.bone = head;
            for (int side = -1; side <= 1; side += 2)
            {
                var pts = new List<Vector3>(); var rr = new List<float>();
                for (int i = 0; i < 6; i++)
                {
                    float t = i / 5f;
                    float az = side * Mathf.Lerp(0.02f, 0.3f, t);
                    float e = Mathf.Lerp(h.lipUpE + 0.05f, h.lipUpE - (a.beardStyle == 5 ? 0.12f : 0.04f), t * t);
                    var p = Sculpt(h, az, e, out var n);
                    pts.Add(p + n * 0.006f * h.hs);
                    rr.Add(Mathf.Lerp(0.0075f, 0.003f, t) * h.hs);
                }
                mb.AddTube(HAIR, pts, rr, 6, true, 0.7f);
            }
        }

        static void BuildHornsSculpt(MeshBuilder mb, Appearance a, HeadShape h)
        {
            int head = I(B.Head);
            mb.bone = head;
            float hs = h.hs;
            int st = a.hornStyle;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 b0 = Sculpt(h, side * 0.42f, 0.72f, out var n) - n * 0.004f * hs;
                var pts = new List<Vector3>(); var rad = new List<float>();
                int cnt = 12;
                switch (st)
                {
                    case 0: // ram curl sweeping back and around the ear
                        for (int i = 0; i <= cnt; i++)
                        {
                            float t = i / (float)cnt; float ang = t * Mathf.PI * 1.45f;
                            pts.Add(b0 + new Vector3(side * (0.015f + 0.05f * t), 0.055f * Mathf.Sin(ang), -0.07f * (1 - Mathf.Cos(ang))) * hs * 1.1f);
                            rad.Add(Mathf.Lerp(0.02f, 0.004f, t) * hs);
                        }
                        break;
                    case 1:
                        for (int i = 0; i <= cnt; i++)
                        {
                            float t = i / (float)cnt;
                            pts.Add(b0 + new Vector3(side * 0.025f * t, 0.06f * t - 0.03f * t * t, -0.17f * t) * hs);
                            rad.Add(Mathf.Lerp(0.018f, 0.002f, t) * hs);
                        }
                        break;
                    case 2:
                        for (int i = 0; i <= cnt; i++)
                        {
                            float t = i / (float)cnt;
                            pts.Add(b0 + new Vector3(side * 0.02f * t, 0.16f * t, -0.05f * t * t) * hs);
                            rad.Add(Mathf.Lerp(0.017f, 0.002f, t) * hs);
                        }
                        break;
                    case 3:
                        for (int i = 0; i <= 5; i++)
                        {
                            float t = i / 5f;
                            pts.Add(b0 + new Vector3(0, 0.032f * t, -0.012f * t) * hs);
                            rad.Add(Mathf.Lerp(0.015f, 0.003f, t) * hs);
                        }
                        break;
                    default:
                        for (int k = 0; k < 3; k++)
                        {
                            Vector3 p = Sculpt(h, side * (0.2f + k * 0.35f), 0.75f - k * 0.08f, out var nn);
                            mb.Push(); mb.Translate(p); mb.Rotate(Quaternion.FromToRotation(Vector3.up, (nn + Vector3.up).normalized));
                            mb.AddCone(HORN, Vector3.zero, 0.009f * hs, 0.045f * hs, 7); mb.Pop();
                        }
                        continue;
                }
                // ridged horn: modulate radius
                for (int i = 0; i < rad.Count; i++) rad[i] *= 1f + 0.08f * Mathf.Sin(i * 2.7f);
                mb.AddTube(HORN, pts, rad, 9);
            }
        }

        static void DragonExtras(MeshBuilder mb, Appearance a, HeadShape h)
        {
            int head = I(B.Head);
            mb.bone = head;
            float hs = h.hs;
            // nostrils near the muzzle tip
            for (int side = -1; side <= 1; side += 2)
            {
                var p = Sculpt(h, side * 0.1f, -0.22f, out var n);
                mb.AddEllipsoid(DARK, p + n * 0.001f, new Vector3(0.006f, 0.004f, 0.005f) * hs, 4, 6);
            }
            // teeth along the jaw line
            for (int i = 0; i < 5; i++)
                for (int side = -1; side <= 1; side += 2)
                {
                    var p = Sculpt(h, side * (0.12f + i * 0.09f), -0.44f, out var n);
                    mb.Push(); mb.Translate(p + n * 0.002f * hs); mb.Rotate(Quaternion.Euler(180, 0, 0));
                    mb.AddCone(HORN, Vector3.zero, 0.0035f * hs, 0.011f * hs, 4); mb.Pop();
                }
            // swept horns
            for (int side = -1; side <= 1; side += 2)
            {
                var pts = new List<Vector3>(); var rad = new List<float>();
                Vector3 b0 = Sculpt(h, side * 0.75f, 0.55f, out _);
                float len = a.hornStyle == 2 ? 1.4f : (a.hornStyle == 3 ? 0.6f : 1f);
                for (int i = 0; i <= 8; i++)
                {
                    float t = i / 8f;
                    pts.Add(b0 + new Vector3(side * 0.02f * t, 0.035f * t - 0.045f * t * t, -0.18f * t * len) * hs);
                    rad.Add(Mathf.Lerp(0.018f, 0.002f, t) * hs);
                }
                mb.AddTube(HORN, pts, rad, 8);
                for (int k = 0; k < 3; k++)
                {
                    var p = Sculpt(h, side * (1.45f + k * 0.2f), -0.1f - k * 0.18f, out var n);
                    mb.Push(); mb.Translate(p); mb.Rotate(Quaternion.FromToRotation(Vector3.up, (n + Vector3.back * 0.8f).normalized));
                    mb.AddCone(HORN, Vector3.zero, 0.007f * hs, 0.035f * hs, 5); mb.Pop();
                }
            }
            for (int k = 0; k < 5; k++)
            {
                var p = Sculpt(h, Mathf.PI, 0.6f - k * 0.35f, out var n);
                mb.Push(); mb.Translate(p); mb.Rotate(Quaternion.FromToRotation(Vector3.up, (n + Vector3.up * 0.3f).normalized));
                mb.AddCone(SKIN, Vector3.zero, 0.01f * hs, 0.03f * hs, 5); mb.Pop();
            }
        }

        // -----------------------------------------------------------------------------------------
        /// <summary>Paints the face texture: skin tone, eye shadow, brows, lips, blush, stubble, scalp tint.</summary>
        public static Texture2D PaintFace(Appearance a, int size = 512)
        {
            var look = RaceLooks.Looks[a.race];
            // landmarks in the same angular space as Sculpt()
            var shape = MakeShapeForPaint(a, look);
            Color skin = a.skin;
            if (a.rotting) skin = Color.Lerp(skin, new Color(.42f, .48f, .36f), 0.6f);
            if (a.skeletal) skin = new Color(.82f, .78f, .66f);
            bool dragon = look.dragon;
            Color hair = a.hair;
            Color lip = Color.Lerp(skin, new Color(.55f, .22f, .22f), a.bodyType == 1 ? 0.42f : 0.26f);
            if (a.race == RaceId.Drow || a.race == RaceId.Tiefling || a.race == RaceId.Githyanki) lip = Color.Lerp(skin, skin * 0.55f, 0.6f);
            Color shadow = skin * 0.62f; shadow.a = 1;
            Color blush = Color.Lerp(skin, new Color(.8f, .38f, .35f), 0.22f);
            var px = new Color32[size * size];
            var rnd = new System.Random((int)(a.skin.r * 997 + a.hair.g * 571 + a.faceShape * 13));
            float freckles = (a.race == RaceId.Human || a.race == RaceId.Halfling || a.race == RaceId.HalfElf) && rnd.NextDouble() < 0.3 ? 1 : 0;
            int stubbleStyle = a.beardStyle;
            bool fem = a.bodyType == 1;
            for (int y = 0; y < size; y++)
            {
                float e = ((y + 0.5f) / size - 0.5f) * Mathf.PI;
                for (int x = 0; x < size; x++)
                {
                    float av = ((x + 0.5f) / size - 0.5f) * 2 * Mathf.PI;
                    Color c = skin;
                    float n = Noise.Fbm((x + .5f) / size, (y + .5f) / size, 16, 4, 71);
                    c *= 0.94f + 0.1f * n;
                    if (dragon)
                    {
                        // lighter belly scales under jaw, darker crown
                        float belly = SmoothR(-0.3f, -0.8f, e) * Mathf.Max(0, Mathf.Cos(av));
                        c = Color.Lerp(c, Color.Lerp(skin, new Color(.9f, .85f, .7f), 0.45f), belly * 0.8f);
                        c = Color.Lerp(c, skin * 0.6f, SmoothR(0.3f, 1f, e) * 0.5f);
                        float crease = G(av, e, 0, -0.42f, 0.9f, 0.02f) * SmoothR(0.6f, 0.2f, Mathf.Abs(av));
                        c = Color.Lerp(c, new Color(.08f, .04f, .04f), crease * 0.9f);
                        float sock = G2(av, e, 0.5f, 0.1f, 0.18f, 0.12f);
                        c = Color.Lerp(c, skin * 0.45f, sock * 0.6f);
                        c.a = 1;
                        px[y * size + x] = c;
                        continue;
                    }
                    // eye shadow / socket depth
                    float sock2 = G2(av, e, shape.eyeA, shape.eyeE, 0.2f, 0.12f);
                    c = Color.Lerp(c, shadow, sock2 * (fem ? 0.5f : 0.4f));
                    if (fem) c = Color.Lerp(c, Color.Lerp(skin, new Color(.35f, .2f, .3f), 0.3f), G2(av, e, shape.eyeA + 0.04f, shape.eyeE + 0.07f, 0.13f, 0.05f) * 0.5f);
                    // lash line
                    c = Color.Lerp(c, new Color(.06f, .04f, .04f), Mathf.Clamp01(G2(av, e, shape.eyeA, shape.eyeE + 0.07f, 0.16f, 0.03f) * 1.3f));
                    // cheeks
                    c = Color.Lerp(c, blush, G2(av, e, 0.55f, -0.2f, 0.22f, 0.13f) * 0.7f);
                    // nose tip ruddy
                    c = Color.Lerp(c, blush, G(av, e, 0, shape.noseTipE, 0.08f, 0.06f) * 0.5f);
                    // lips
                    float lipsW = G(av, e, 0, shape.lipUpE - 0.02f, 0.17f, 0.035f) + G(av, e, 0, shape.lipLoE + 0.01f, 0.15f, 0.035f);
                    c = Color.Lerp(c, lip, Mathf.Clamp01(lipsW * 1.4f));
                    c = Color.Lerp(c, new Color(.15f, .06f, .06f), G(av, e, 0, shape.mouthE, 0.19f, 0.011f) * 0.9f);
                    // brows
                    for (int side = -1; side <= 1; side += 2)
                    {
                        float da = Mathf.DeltaAngle(side * shape.eyeA * Mathf.Rad2Deg, av * Mathf.Rad2Deg) * Mathf.Deg2Rad * side;
                        if (da > -0.2f && da < 0.24f)
                        {
                            float arch = shape.browE + 0.02f - 0.9f * (da - 0.02f) * (da - 0.02f) + (fem ? 0.02f : 0f);
                            float thick = (fem ? 0.02f : 0.032f) * (1 - Mathf.Abs(da - 0.0f) * 1.6f);
                            if (a.race == RaceId.Githyanki) thick = 0;
                            float bw = Mathf.Exp(-Mathf.Pow((e - arch) / Mathf.Max(0.002f, thick), 2));
                            float strands = 0.9f + 0.1f * Mathf.Sin((da * 120f) + e * 60f);
                            c = Color.Lerp(c, hair * 0.8f, bw * 0.95f * strands);
                        }
                    }
                    // stubble / beard base
                    if (!fem && (stubbleStyle >= 1 && stubbleStyle <= 4))
                    {
                        float bm = BeardMask(shape, av, e, stubbleStyle, out float mo);
                        float st = Mathf.Max(bm, mo * 0.8f) * (stubbleStyle == 1 ? 0.45f : 0.8f);
                        float grain = Noise.Hash01(x, y, 3) * 0.5f + 0.5f;
                        c = Color.Lerp(c, hair * 0.55f, st * grain);
                    }
                    else if (!fem && a.race != RaceId.Elf && a.race != RaceId.Drow && a.race != RaceId.Githyanki)
                    {
                        float bm = BeardMask(shape, av, e, 1, out float mo);
                        c = Color.Lerp(c, c * 0.88f, Mathf.Max(bm, mo) * 0.3f);
                    }
                    // painted form: contour shadows and highlights so faces read under flat light
                    float ao = 0;
                    ao += G2(av, e, 0.13f, -0.1f, 0.06f, 0.13f) * 0.35f;          // sides of the nose
                    ao += G(av, e, 0, shape.noseTipE - 0.07f, 0.1f, 0.03f) * 0.4f; // under the nose
                    ao += G(av, e, 0, shape.lipLoE - 0.07f, 0.16f, 0.035f) * 0.35f; // under lower lip
                    ao += SmoothR(-0.78f, -1.0f, e) * 0.55f;                       // under the jaw
                    ao += G2(av, e, 0.95f, -0.45f, 0.3f, 0.3f) * 0.25f;           // jaw sides
                    ao += G2(av, e, 1.0f, 0.3f, 0.25f, 0.25f) * 0.2f;             // temples
                    ao += G2(av, e, shape.eyeA - 0.1f, shape.eyeE + 0.02f, 0.06f, 0.08f) * 0.3f; // inner eye corners
                    c = Color.Lerp(c, c * 0.55f, Mathf.Clamp01(ao));
                    float hi = G(av, e, 0, 0.05f, 0.05f, 0.18f) * 0.12f + G2(av, e, 0.52f, -0.08f, 0.14f, 0.07f) * 0.1f + G(av, e, 0, 0.45f, 0.35f, 0.15f) * 0.06f;
                    c = Color.Lerp(c, c * 1.25f, hi);
                    // scalp
                    float sm = ScalpMask(av, e, Mathf.Max(1, a.hairStyle));
                    if (a.hairStyle == 0) c = Color.Lerp(c, Color.Lerp(c, hair * 0.6f, 0.5f), sm * 0.55f * (Noise.Hash01(x, y, 5) * 0.4f + 0.6f));
                    else c = Color.Lerp(c, hair * 0.7f, sm);
                    // freckles
                    if (freckles > 0 && Noise.Hash01(x / 3, y / 3, 11) > 0.93f) c = Color.Lerp(c, skin * 0.7f, G2(av, e, 0.4f, -0.12f, 0.35f, 0.18f) * 0.6f);
                    // githyanki / tiefling markings
                    if (a.race == RaceId.Githyanki) c = Color.Lerp(c, skin * 0.55f, Mathf.Clamp01(Mathf.Exp(-Mathf.Pow((Mathf.Abs(av) - 0.5f - e * 0.4f) / 0.015f, 2)) * SmoothR(0.4f, 0.1f, Mathf.Abs(e + 0.1f))) * 0.7f);
                    if (a.race == RaceId.Tiefling && a.faceShape % 2 == 0) c = Color.Lerp(c, skin * 0.6f, G(av, e, 0, 0.38f, 0.05f, 0.14f) * 0.6f);
                    if (a.age > 0.5f) c = Color.Lerp(c, c * 0.85f, (Mathf.Sin(e * 160f) * 0.5f + 0.5f) * G(av, e, 0, 0.4f, 0.5f, 0.08f) * 0.5f);
                    if (a.rotting) c = Color.Lerp(c, new Color(.25f, .2f, .15f), MathX.Smoothstep(.6f, .8f, Noise.Fbm((x + .5f) / size, (y + .5f) / size, 5, 4, 91)) * 0.8f);
                    c.a = 1;
                    px[y * size + x] = c;
                }
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, false) { name = "Face", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return tex;
        }

        static HeadShape MakeShapeForPaint(Appearance a, RaceLook look)
        {
            var d = new Dims { headC = Vector3.zero, headRad = new Vector3(0.085f, 0.12f, 0.106f), hs = 1, s = 1 };
            return MakeShape(a, d, look);
        }
    }
}
