using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dungine.Visual
{
    /// <summary>Hands and plate armour: separate, layered pieces over the body rather than a tinted skin.</summary>
    public static partial class HumanoidBuilder
    {
        // ------------------------------------------------------------------------------------------
        //  Hands: a palm, four jointed fingers curled as if about to grip, and an opposed thumb.
        //  Built in the arm's frame (-Y runs down the hand, +X is the arm's outer side, +Z the front).
        // ------------------------------------------------------------------------------------------
        static void BuildHand(MeshBuilder mb, int sub, int boneIndex, Vector3 wrist, Quaternion armRot, int side, float hs, bool gauntlet, bool claws, int clawSub)
        {
            float inward = -side;
            float g = gauntlet ? 1.16f : 1f;
            mb.Push();
            mb.Translate(wrist);
            mb.Rotate(armRot);
            var w = MeshBuilder.W(boneIndex);
            // palm: thick at the heel, broad across the knuckles, a little cupped
            var palm = new List<MeshBuilder.Ring>();
            float[] py = { 0.004f, -0.028f, -0.062f, -0.086f };
            float[] pt = { 0.014f, 0.017f, 0.016f, 0.013f };
            float[] pw = { 0.021f, 0.033f, 0.037f, 0.035f };
            for (int i = 0; i < py.Length; i++)
                palm.Add(new MeshBuilder.Ring
                {
                    center = new Vector3(inward * 0.002f, py[i], 0.003f) * hs, rot = Quaternion.identity, rx = pt[i] * hs * g, rz = pw[i] * hs * g, weight = w,
                    radial = a => { float c = Mathf.Cos(a) * inward; return c > 0 ? 0.88f : 1f; }   // palm side a touch flatter
                });
            mb.AddLoft(sub, palm, 10, true, true);
            // fingers: index, middle, ring, little (front to back)
            float[] fz = { 0.026f, 0.009f, -0.008f, -0.024f };
            float[] fl = { 1f, 1.08f, 1f, 0.82f };
            float[] seg = { 0.043f, 0.026f, 0.021f };
            float[] curl = { 22f, 34f, 26f };
            for (int f = 0; f < 4; f++)
            {
                var pts = new List<Vector3>(); var rr = new List<float>();
                Vector3 p = new Vector3(0, -0.085f, fz[f]) * hs;
                Vector3 dir = Vector3.down;
                pts.Add(p); rr.Add(0.0092f * hs * g * (f == 3 ? 0.88f : 1f));
                float spread = (f - 1.5f) * 3f;
                for (int k = 0; k < 3; k++)
                {
                    dir = Quaternion.AngleAxis(inward * curl[k] * (f == 3 ? 1.15f : 1f), Vector3.forward) * dir;
                    dir = Quaternion.AngleAxis(spread * (k == 0 ? 1 : 0), Vector3.right) * dir;
                    p += dir * seg[k] * fl[f] * hs;
                    pts.Add(p); rr.Add(Mathf.Lerp(0.0088f, 0.0068f, (k + 1) / 3f) * hs * g * (f == 3 ? 0.88f : 1f));
                }
                Finger(mb, sub, pts, rr, w);
                if (claws)
                {
                    mb.bone = boneIndex; mb.bone2 = -1;
                    mb.Push(); mb.Translate(pts[3]); mb.Rotate(Quaternion.FromToRotation(Vector3.up, (pts[3] - pts[2]).normalized));
                    mb.AddCone(clawSub, Vector3.zero, 0.005f * hs, 0.02f * hs, 5); mb.Pop();
                }
            }
            // thumb: from the heel of the palm, forward and across toward the fingers
            {
                var pts = new List<Vector3>(); var rr = new List<float>();
                Vector3 p = new Vector3(inward * 0.006f, -0.018f, 0.028f) * hs;
                Vector3 dir = new Vector3(inward * 0.45f, -0.62f, 0.64f).normalized;
                pts.Add(p); rr.Add(0.0115f * hs * g);
                float[] ts = { 0.032f, 0.026f, 0.02f };
                for (int k = 0; k < 3; k++)
                {
                    p += dir * ts[k] * hs;
                    pts.Add(p); rr.Add(Mathf.Lerp(0.0105f, 0.0078f, (k + 1) / 3f) * hs * g);
                    dir = Quaternion.AngleAxis(inward * 16f, new Vector3(0, 0.3f, 1f).normalized) * dir;
                }
                Finger(mb, sub, pts, rr, w);
            }
            if (gauntlet)
            {
                // plate over the back of the hand and a flared cuff
                var cuff = new List<MeshBuilder.Ring>
                {
                    new MeshBuilder.Ring { center = new Vector3(0, 0.035f, 0) * hs, rot = Quaternion.identity, rx = 0.026f * hs, rz = 0.028f * hs, weight = w },
                    new MeshBuilder.Ring { center = new Vector3(0, -0.012f, 0.002f) * hs, rot = Quaternion.identity, rx = 0.034f * hs, rz = 0.038f * hs, weight = w },
                };
                mb.AddLoft(sub, cuff, 14, false, false);
                var knuckle = new List<MeshBuilder.Ring>
                {
                    new MeshBuilder.Ring { center = new Vector3(-inward * 0.004f, -0.07f, 0.002f) * hs, rot = Quaternion.identity, rx = 0.017f * hs, rz = 0.041f * hs, weight = w },
                    new MeshBuilder.Ring { center = new Vector3(-inward * 0.004f, -0.088f, 0.002f) * hs, rot = Quaternion.identity, rx = 0.015f * hs, rz = 0.04f * hs, weight = w },
                };
                mb.AddLoft(TRIM, knuckle, 12, true, true);
            }
            mb.Pop();
        }

        static void Finger(MeshBuilder mb, int sub, List<Vector3> pts, List<float> rr, BoneWeight w)
        {
            var rings = new List<MeshBuilder.Ring>();
            for (int i = 0; i < pts.Count; i++)
            {
                Vector3 dir = (i < pts.Count - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1]).normalized;
                rings.Add(new MeshBuilder.Ring { center = pts[i], rot = Quaternion.FromToRotation(Vector3.up, dir), rx = rr[i], rz = rr[i] * 0.92f, weight = w });
            }
            mb.AddLoft(sub, rings, 7, false, true);
        }

        // ------------------------------------------------------------------------------------------
        //  Plate harness: cuirass with a centre ridge, fauld and tassets, layered pauldrons, gorget,
        //  rerebraces, couters, vambraces, cuisses, poleyns and greaves, with leather straps and trim.
        // ------------------------------------------------------------------------------------------
        static void BuildPlate(MeshBuilder mb, Appearance a, Dims d)
        {
            float s = d.s;
            Func<float, float> ridge = ang =>
            {
                float front = Mathf.Exp(-Mathf.Pow((Mathf.Repeat(ang, Mathf.PI * 2f) - Mathf.PI * 0.5f) / 0.32f, 2f));
                float back = Mathf.Sin(ang) < 0 ? 1f - 0.08f * Mathf.Pow(-Mathf.Sin(ang), 3f) : 1f;
                return back * (1f + 0.045f * front);
            };
            BoneWeight Wb(B b) => MeshBuilder.W(I(b));
            BoneWeight Wb2(B x, B y, float t) => MeshBuilder.W(I(x), I(y), t);

            // ---- cuirass (breast and back plates) ----
            var cu = new List<MeshBuilder.Ring>
            {
                R(new Vector3(0, d.hipsY + d.T * 0.16f, 0.004f), d.waistW * 1.2f, d.waistD * 1.24f, Wb2(B.Hips, B.Spine, 0.5f), null, 0.006f * s, ridge),
                R(new Vector3(0, d.hipsY + d.T * 0.3f, 0.006f), d.waistW * 1.13f, d.waistD * 1.2f, Wb(B.Spine), null, 0.006f * s, ridge),
                R(new Vector3(0, d.hipsY + d.T * 0.52f, 0.008f * s), d.chestW * 1.06f, d.chestD * 1.2f, Wb2(B.Spine, B.Chest, 0.5f), null, 0.01f * s, ridge),
                R(new Vector3(0, d.hipsY + d.T * 0.74f, 0.01f * s), d.chestW * 1.15f, d.chestD * 1.24f, Wb(B.Chest), null, 0.012f * s, ridge),
                R(new Vector3(0, d.shoulderY - 0.045f * s, 0.004f * s), d.shoulderW * 0.9f, d.chestD * 1.1f, Wb(B.Chest), null, 0.006f * s, ridge),
                R(new Vector3(0, d.shoulderY + 0.004f * s, -0.004f * s), d.shoulderW * 0.64f, d.shoulderD * 1.1f, Wb(B.Chest)),
                R(new Vector3(0, d.shoulderY + 0.028f * s, -0.008f * s), d.neckR * 1.95f, d.neckR * 1.75f, Wb(B.Chest)),
            };
            mb.AddLoft(METAL, cu, 26, false, false);
            // rolled edges: bottom of the breastplate and around the neck
            Band(mb, TRIM, new Vector3(0, d.hipsY + d.T * 0.16f, 0.004f), d.waistW * 1.23f, d.waistD * 1.27f, 0.012f * s, Wb2(B.Hips, B.Spine, 0.5f), ridge, 0.006f * s);
            Band(mb, TRIM, new Vector3(0, d.shoulderY + 0.03f * s, -0.008f * s), d.neckR * 2.02f, d.neckR * 1.82f, 0.01f * s, Wb(B.Chest), null, 0);

            // ---- gorget: two lames rising around the throat ----
            for (int k = 0; k < 2; k++)
            {
                float y0 = d.shoulderY + (0.02f + k * 0.03f) * s, y1 = y0 + 0.035f * s;
                var gr = new List<MeshBuilder.Ring>
                {
                    R(new Vector3(0, y0, -0.008f * s), d.neckR * (1.85f - k * 0.2f), d.neckR * (1.7f - k * 0.18f), Wb2(B.Chest, B.Neck, 0.3f + k * 0.3f)),
                    R(new Vector3(0, y1, -0.006f * s), d.neckR * (1.6f - k * 0.2f), d.neckR * (1.5f - k * 0.18f), Wb2(B.Chest, B.Neck, 0.5f + k * 0.3f)),
                };
                mb.AddLoft(METAL, gr, 18, false, false);
            }

            // ---- fauld: overlapping bands below the waist, then tassets over each thigh ----
            for (int k = 0; k < 2; k++)
            {
                float y0 = d.hipsY + d.T * 0.16f - k * 0.04f * s, y1 = y0 - 0.048f * s;
                var fr = new List<MeshBuilder.Ring>
                {
                    R(new Vector3(0, y0, 0.004f), Mathf.Lerp(d.waistW, d.hipW, 0.5f) * (1.16f + k * 0.04f), Mathf.Lerp(d.waistD, d.pelvisD, 0.5f) * (1.24f + k * 0.05f), Wb(B.Hips), null, 0.006f * s, ridge),
                    R(new Vector3(0, y1, 0.004f), d.hipW * (1.08f + k * 0.04f), d.pelvisD * (1.28f + k * 0.05f), Wb(B.Hips), null, 0.008f * s, ridge),
                };
                mb.AddLoft(METAL, fr, 24, false, false);
                Band(mb, TRIM, new Vector3(0, y1, 0.004f), d.hipW * (1.1f + k * 0.04f), d.pelvisD * (1.3f + k * 0.05f), 0.006f * s, Wb(B.Hips), ridge, 0.008f * s);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                B thigh = side < 0 ? B.ThighL : B.ThighR;
                Vector3 hip = side < 0 ? d.hipL : d.hipR;
                for (int k = 0; k < 2; k++)
                {
                    float top = d.hipsY + d.T * 0.16f - 0.09f * s - k * 0.062f * s;
                    Vector3 c = new Vector3(hip.x * 1.12f, top - 0.035f * s, d.pelvisD * 1.3f);
                    CurvedPlate(mb, METAL, c, Vector3.down, new Vector3(side * 0.4f, 0, 1f).normalized, 0.11f * s, 0.075f * s, d.thighR * 1.6f, 0.006f * s,
                        (u, v) => Wb2(B.Hips, thigh, Mathf.Lerp(0.3f, 0.65f, v) * (k == 0 ? 0.8f : 1f)));
                }
            }

            // ---- arms ----
            for (int side = -1; side <= 1; side += 2)
            {
                B clav = side < 0 ? B.ClavL : B.ClavR, up = side < 0 ? B.UpperArmL : B.UpperArmR, lo = side < 0 ? B.LowerArmL : B.LowerArmR;
                Vector3 sh = side < 0 ? d.shoulderL : d.shoulderR, el = side < 0 ? d.elbowL : d.elbowR, wr = side < 0 ? d.wristL : d.wristR;
                Vector3 dir = (el - sh).normalized;
                Quaternion rot = Quaternion.FromToRotation(Vector3.down, dir);
                // pauldron: a domed cap and three lames stepping down over the upper arm, each tucked under the one above
                // centred a little in from the joint so the cap spans the top of the shoulder to the cuirass
                Vector3 pc = sh + new Vector3(-side * 0.012f * s, 0.022f * s, -0.004f * s);
                float pr = d.deltR * 2.5f;
                for (int k = 0; k < 4; k++)
                {
                    float lat0 = k == 0 ? 0f : 30f + (k - 1) * 24f, lat1 = k == 0 ? 44f : lat0 + 32f;
                    float rk = pr * (1f - 0.035f * k);
                    float wgt = k == 0 ? 0.45f : Mathf.Lerp(0.7f, 0.95f, (k - 1) / 2f);
                    SpherePlate(mb, METAL, pc, rk, side, lat0, lat1, k == 0 ? 250f : 200f, 0.007f * s, Wb2(clav, up, wgt));
                    if (k > 0) SpherePlate(mb, TRIM, pc, rk * 1.012f, side, lat1 - 3f, lat1, k == 0 ? 250f : 200f, 0.004f * s, Wb2(clav, up, wgt));
                }
                // rerebrace, couter, vambrace
                Tube(mb, METAL, sh + dir * 0.1f * s, el - dir * 0.03f * s, d.upperArmR * 1.32f, d.upperArmR * 1.18f, Wb(up), Wb(up), rot);
                mb.bone = I(lo); mb.bone2 = I(up); mb.bone2Weight = 0.45f;
                mb.AddEllipsoid(METAL, el + rot * new Vector3(0, 0, -d.elbowRad * 0.8f), new Vector3(d.elbowRad * 1.45f, d.elbowRad * 1.35f, d.elbowRad * 1.1f), 7, 10);
                mb.Push(); mb.Translate(el + rot * new Vector3(side * d.elbowRad * 1.3f, 0, -d.elbowRad * 0.5f)); mb.Rotate(rot * Quaternion.Euler(0, 0, side * 90));
                mb.AddEllipsoid(METAL, Vector3.zero, new Vector3(d.elbowRad * 1.4f, d.elbowRad * 0.3f, d.elbowRad * 1.7f), 5, 10);
                mb.Pop(); mb.bone2 = -1;
                Tube(mb, METAL, el + dir * 0.035f * s, wr - dir * 0.012f * s, d.forearmR * 1.3f, d.wristRad * 1.5f, Wb(lo), Wb(lo), rot);
                Band(mb, LEATHER, el + dir * d.foreArmLen * 0.45f, d.forearmR * 1.36f, d.forearmR * 1.36f, 0.012f * s, Wb(lo), null, 0, rot);
            }

            // ---- legs ----
            for (int side = -1; side <= 1; side += 2)
            {
                B thigh = side < 0 ? B.ThighL : B.ThighR, shin = side < 0 ? B.ShinL : B.ShinR;
                Vector3 hip = side < 0 ? d.hipL : d.hipR, knee = side < 0 ? d.kneeL : d.kneeR_, ankle = side < 0 ? d.ankleL : d.ankleR_;
                Vector3 up = (hip - knee).normalized;
                // cuisse: a plate wrapped over the front and outside of the thigh
                Vector3 cn = new Vector3(side * 0.3f, 0, 1f).normalized;
                Vector3 cc = Vector3.Lerp(hip, knee, 0.56f) + cn * d.thighR * 1.14f;
                CurvedPlate(mb, METAL, cc, -up, cn, 0, Vector3.Distance(hip, knee) * 0.62f, d.thighR * 1.14f, 0.006f * s,
                    (u, v) => MeshBuilder.W(I(thigh)), 150f);
                // poleyn: a knee cop with a side wing
                mb.bone = I(shin); mb.bone2 = I(thigh); mb.bone2Weight = 0.5f;
                mb.AddEllipsoid(METAL, knee + new Vector3(0, 0.004f, d.kneeR * 0.72f), new Vector3(d.kneeR * 1.15f, d.kneeR * 1.25f, d.kneeR * 0.7f), 7, 10);
                mb.AddEllipsoid(METAL, knee + new Vector3(side * d.kneeR * 0.95f, 0.004f, d.kneeR * 0.25f), new Vector3(d.kneeR * 0.25f, d.kneeR * 0.95f, d.kneeR * 0.95f), 5, 9);
                mb.bone2 = -1;
                // greave: closed around the shin, flaring toward the ankle
                Quaternion lr = Quaternion.FromToRotation(Vector3.down, (ankle - knee).normalized);
                Tube(mb, METAL, knee + (ankle - knee) * 0.1f, ankle + Vector3.up * 0.03f * s, d.calfR * 1.3f, d.ankleR * 1.55f, Wb(shin), Wb(shin), lr, 0.02f * s);
                Band(mb, LEATHER, Vector3.Lerp(hip, knee, 0.3f), d.thighR * 1.18f, d.thighR * 1.14f, 0.014f * s, Wb(thigh), null, 0, Quaternion.FromToRotation(Vector3.down, -up));
            }

            // ---- a pendant on a thin chain ----
            mb.bone = I(B.Chest); mb.bone2 = -1;
            mb.AddEllipsoid(TRIM, new Vector3(0, d.hipsY + d.T * 0.68f, d.chestD * 1.33f + 0.012f * s), new Vector3(0.018f, 0.022f, 0.006f) * s, 5, 8);
        }

        /// <summary>A closed band (strap, rolled edge) around a centre.</summary>
        static void Band(MeshBuilder mb, int sub, Vector3 c, float rx, float rz, float height, BoneWeight w, Func<float, float> radial, float offZ, Quaternion? rot = null)
        {
            Quaternion q = rot ?? Quaternion.identity;
            var rings = new List<MeshBuilder.Ring>
            {
                R(c + q * Vector3.up * height * 0.5f, rx, rz, w, q, offZ, radial),
                R(c + q * Vector3.up * height * 0.5f, rx * 1.03f, rz * 1.03f, w, q, offZ, radial),
                R(c - q * Vector3.up * height * 0.5f, rx * 1.03f, rz * 1.03f, w, q, offZ, radial),
                R(c - q * Vector3.up * height * 0.5f, rx, rz, w, q, offZ, radial),
            };
            mb.AddLoft(sub, rings, 20, false, false);
        }

        /// <summary>A tube from a to b (plate around a limb) with a small flare lip at the lower end.</summary>
        static void Tube(MeshBuilder mb, int sub, Vector3 a, Vector3 b, float ra, float rb, BoneWeight wa, BoneWeight wb, Quaternion rot, float offZ = 0f)
        {
            var rings = new List<MeshBuilder.Ring>
            {
                R(a, ra * 0.96f, ra * 0.96f, wa, rot, offZ),
                R(Vector3.Lerp(a, b, 0.08f), ra, ra * 1.02f, wa, rot, offZ),
                R(Vector3.Lerp(a, b, 0.55f), Mathf.Lerp(ra, rb, 0.45f), Mathf.Lerp(ra, rb, 0.45f) * 1.04f, wb, rot, offZ),
                R(b, rb, rb * 1.04f, wb, rot, offZ),
                R(b + (b - a).normalized * 0.004f, rb * 1.08f, rb * 1.1f, wb, rot, offZ),
            };
            mb.AddLoft(sub, rings, 16, false, false);
        }

        /// <summary>
        /// A thick plate bent around an axis: centre c, running along 'down' for 'height', facing 'normal', 'width' across
        /// the curve of radius 'rad'. Optional 'arcDeg' limits how far it wraps.
        /// </summary>
        static void CurvedPlate(MeshBuilder mb, int sub, Vector3 c, Vector3 down, Vector3 normal, float width, float height, float rad, float thick, Func<float, float, BoneWeight> weight, float arcDeg = 0f)
        {
            Vector3 side = Vector3.Cross(down, normal).normalized;
            Vector3 n = Vector3.Cross(side, down).normalized;       // re-orthogonalised outward
            float arc = arcDeg > 0 ? arcDeg * Mathf.Deg2Rad : width / rad;
            Vector3 axis = c - n * rad;
            Func<float, float, Vector3> P = (u, v) =>
            {
                float th = (u - 0.5f) * arc;
                return axis + (n * Mathf.Cos(th) + side * Mathf.Sin(th)) * rad + down * (v - 0.5f) * height;
            };
            Shell(mb, sub, 8, 4, P, thick, weight, axis);
        }

        /// <summary>
        /// A band of a sphere around an upper arm: latitudes lat0..lat1 from the top, spanning 'span' degrees of longitude
        /// centred on the arm's outer side.
        /// </summary>
        static void SpherePlate(MeshBuilder mb, int sub, Vector3 c, float r, int side, float lat0, float lat1, float span, float thick, BoneWeight w)
        {
            Vector3 outDir = new Vector3(side, 0, 0);
            Func<float, float, Vector3> P = (u, v) =>
            {
                float lon = (u - 0.5f) * span * Mathf.Deg2Rad;
                float lat = Mathf.Lerp(lat0, lat1, v) * Mathf.Deg2Rad;
                Vector3 horiz = outDir * Mathf.Cos(lon) + Vector3.forward * Mathf.Sin(lon);
                return c + (Vector3.up * Mathf.Cos(lat) + horiz * Mathf.Sin(lat)) * r;
            };
            Shell(mb, sub, 12, 3, P, thick, (u, v) => w, c);
        }

        /// <summary>Builds a two-sided surface with thickness from a parametric patch; normals face away from 'inside'.</summary>
        static void Shell(MeshBuilder mb, int sub, int nu, int nv, Func<float, float, Vector3> P, float thick, Func<float, float, BoneWeight> weight, Vector3 inside)
        {
            var pos = new Vector3[nu + 1, nv + 1]; var nrm = new Vector3[nu + 1, nv + 1];
            for (int i = 0; i <= nu; i++)
                for (int j = 0; j <= nv; j++) pos[i, j] = P(i / (float)nu, j / (float)nv);
            for (int i = 0; i <= nu; i++)
                for (int j = 0; j <= nv; j++)
                {
                    Vector3 du = pos[Mathf.Min(nu, i + 1), j] - pos[Mathf.Max(0, i - 1), j];
                    Vector3 dv = pos[i, Mathf.Min(nv, j + 1)] - pos[i, Mathf.Max(0, j - 1)];
                    Vector3 n = Vector3.Cross(du, dv).normalized;
                    if (Vector3.Dot(n, pos[i, j] - inside) < 0) n = -n;
                    nrm[i, j] = n;
                }
            bool flip = Vector3.Dot(Vector3.Cross(pos[1, 0] - pos[0, 0], pos[0, 1] - pos[0, 0]), nrm[0, 0]) < 0;
            int outer = mb.verts.Count;
            for (int i = 0; i <= nu; i++)
                for (int j = 0; j <= nv; j++)
                    mb.AddVertexRaw(mb.M.MultiplyPoint3x4(pos[i, j]), mb.M.MultiplyVector(nrm[i, j]).normalized, new Vector2(i / (float)nu, j / (float)nv), weight(i / (float)nu, j / (float)nv));
            int inner = mb.verts.Count;
            for (int i = 0; i <= nu; i++)
                for (int j = 0; j <= nv; j++)
                    mb.AddVertexRaw(mb.M.MultiplyPoint3x4(pos[i, j] - nrm[i, j] * thick), mb.M.MultiplyVector(-nrm[i, j]).normalized, new Vector2(i / (float)nu, j / (float)nv), weight(i / (float)nu, j / (float)nv));
            int V(int b, int i, int j) => b + i * (nv + 1) + j;
            for (int i = 0; i < nu; i++)
                for (int j = 0; j < nv; j++)
                {
                    int a = V(outer, i, j), b = V(outer, i + 1, j), c = V(outer, i + 1, j + 1), d0 = V(outer, i, j + 1);
                    int ia = V(inner, i, j), ib = V(inner, i + 1, j), ic = V(inner, i + 1, j + 1), id = V(inner, i, j + 1);
                    if (!flip) { mb.Tri(sub, a, b, c); mb.Tri(sub, a, c, d0); mb.Tri(sub, ia, ic, ib); mb.Tri(sub, ia, id, ic); }
                    else { mb.Tri(sub, a, c, b); mb.Tri(sub, a, d0, c); mb.Tri(sub, ia, ib, ic); mb.Tri(sub, ia, ic, id); }
                }
            // rim around the edge so the plate reads as thick metal
            void Edge(int i0, int j0, int i1, int j1)
            {
                int a = V(outer, i0, j0), b = V(outer, i1, j1), ia = V(inner, i0, j0), ib = V(inner, i1, j1);
                mb.Tri(sub, a, ia, ib); mb.Tri(sub, a, ib, b);
                mb.Tri(sub, a, ib, ia); mb.Tri(sub, a, b, ib);
            }
            for (int i = 0; i < nu; i++) { Edge(i, 0, i + 1, 0); Edge(i, nv, i + 1, nv); }
            for (int j = 0; j < nv; j++) { Edge(0, j, 0, j + 1); Edge(nu, j, nu, j + 1); }
        }
    }
}
