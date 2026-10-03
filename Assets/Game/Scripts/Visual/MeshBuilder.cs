using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dungine.Visual
{
    /// <summary>
    /// Procedural mesh construction: boxes, cylinders, ellipsoids and lofts with optional bone weights,
    /// multiple submeshes and a transform stack. Everything the game renders is built through this.
    /// </summary>
    public class MeshBuilder
    {
        public readonly List<Vector3> verts = new List<Vector3>(4096);
        public readonly List<Vector3> normals = new List<Vector3>(4096);
        public readonly List<Vector2> uvs = new List<Vector2>(4096);
        public readonly List<Color> colors = new List<Color>(4096);
        public readonly List<BoneWeight> weights = new List<BoneWeight>(4096);
        readonly List<List<int>> subTris = new List<List<int>>();

        public Matrix4x4 M = Matrix4x4.identity;
        readonly Stack<Matrix4x4> stack = new Stack<Matrix4x4>();
        public int bone = 0;
        public int bone2 = -1;
        public float bone2Weight = 0;
        public Color color = Color.white;
        public float uvScale = 1f;
        public bool useWeights;

        public int VertexCount => verts.Count;

        public MeshBuilder(int submeshes = 1, bool skinned = false)
        {
            for (int i = 0; i < submeshes; i++) subTris.Add(new List<int>());
            useWeights = skinned;
        }

        public void Push() => stack.Push(M);
        public void Pop() => M = stack.Pop();
        public void Translate(Vector3 t) => M = M * Matrix4x4.Translate(t);
        public void Rotate(Quaternion q) => M = M * Matrix4x4.Rotate(q);
        public void Scale(Vector3 s) => M = M * Matrix4x4.Scale(s);
        public void TRS(Vector3 t, Quaternion r, Vector3 s) => M = M * Matrix4x4.TRS(t, r, s);

        List<int> Tris(int sub)
        {
            while (subTris.Count <= sub) subTris.Add(new List<int>());
            return subTris[sub];
        }

        BoneWeight CurrentWeight()
        {
            var bw = new BoneWeight();
            if (bone2 >= 0 && bone2Weight > 0.001f)
            {
                bw.boneIndex0 = bone; bw.weight0 = 1 - bone2Weight;
                bw.boneIndex1 = bone2; bw.weight1 = bone2Weight;
            }
            else { bw.boneIndex0 = bone; bw.weight0 = 1; }
            return bw;
        }

        public int AddVertex(Vector3 localPos, Vector3 localNormal, Vector2 uv)
        {
            verts.Add(M.MultiplyPoint3x4(localPos));
            normals.Add(M.MultiplyVector(localNormal).normalized);
            uvs.Add(uv);
            colors.Add(color);
            if (useWeights) weights.Add(CurrentWeight());
            return verts.Count - 1;
        }

        public int AddVertexRaw(Vector3 pos, Vector3 n, Vector2 uv, BoneWeight bw)
        {
            verts.Add(pos); normals.Add(n); uvs.Add(uv); colors.Add(color);
            if (useWeights) weights.Add(bw);
            return verts.Count - 1;
        }

        public void Tri(int sub, int a, int b, int c) { var t = Tris(sub); t.Add(a); t.Add(b); t.Add(c); }

        /// <summary>Index count of a submesh so far (mark it before adding a part, to recolour just that part later).</summary>
        public int TriCount(int sub) => Tris(sub).Count;

        /// <summary>Moves triangles of fromSub (from index startIndex on) whose centre and normal pass the test into toSub.</summary>
        public void Reassign(int fromSub, int toSub, int startIndex, Func<Vector3, Vector3, bool> test)
        {
            var from = Tris(fromSub); var to = Tris(toSub);
            var keep = new List<int>(from.Count);
            for (int i = 0; i < startIndex && i < from.Count; i++) keep.Add(from[i]);
            for (int i = startIndex; i + 2 < from.Count; i += 3)
            {
                int a = from[i], b = from[i + 1], c = from[i + 2];
                Vector3 centre = (verts[a] + verts[b] + verts[c]) / 3f;
                Vector3 n = (normals[a] + normals[b] + normals[c]).normalized;
                if (test(centre, n)) { to.Add(a); to.Add(b); to.Add(c); }
                else { keep.Add(a); keep.Add(b); keep.Add(c); }
            }
            from.Clear(); from.AddRange(keep);
        }
        public void Quad(int sub, int a, int b, int c, int d) { Tri(sub, a, b, c); Tri(sub, a, c, d); }

        // ---------- Primitives ----------

        /// <summary>Flat-shaded quad given 4 local corners (counter-clockwise when viewed from the front).</summary>
        public void AddQuad(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool worldUV = true, float uvRot = 0)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            Vector3 wa = M.MultiplyPoint3x4(a), wb = M.MultiplyPoint3x4(b), wc = M.MultiplyPoint3x4(c), wd = M.MultiplyPoint3x4(d);
            Vector3 wn = M.MultiplyVector(n).normalized;
            Vector2 ua, ub, uc, ud;
            if (worldUV)
            {
                PlanarBasis(wn, out var tu, out var tv);
                if (uvRot != 0) { var q = Quaternion.AngleAxis(uvRot, wn); tu = q * tu; tv = q * tv; }
                ua = new Vector2(Vector3.Dot(wa, tu), Vector3.Dot(wa, tv)) * uvScale;
                ub = new Vector2(Vector3.Dot(wb, tu), Vector3.Dot(wb, tv)) * uvScale;
                uc = new Vector2(Vector3.Dot(wc, tu), Vector3.Dot(wc, tv)) * uvScale;
                ud = new Vector2(Vector3.Dot(wd, tu), Vector3.Dot(wd, tv)) * uvScale;
            }
            else { ua = new Vector2(0, 0); ub = new Vector2(1, 0); uc = new Vector2(1, 1); ud = new Vector2(0, 1); }
            var bw = CurrentWeight();
            int i0 = AddVertexRaw(wa, wn, ua, bw), i1 = AddVertexRaw(wb, wn, ub, bw), i2 = AddVertexRaw(wc, wn, uc, bw), i3 = AddVertexRaw(wd, wn, ud, bw);
            Quad(sub, i0, i1, i2, i3);
        }

        public static void PlanarBasis(Vector3 n, out Vector3 u, out Vector3 v)
        {
            Vector3 an = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
            if (an.y >= an.x && an.y >= an.z) { u = Vector3.right; v = Vector3.forward; }
            else if (an.x >= an.z) { u = Vector3.forward * Mathf.Sign(n.x); v = Vector3.up; }
            else { u = Vector3.right * -Mathf.Sign(n.z); v = Vector3.up; }
        }

        /// <summary>Axis-aligned box in the current transform. faces bitmask: 1=+x 2=-x 4=+y 8=-y 16=+z 32=-z.</summary>
        public void AddBox(int sub, Vector3 center, Vector3 size, int faces = 63, bool worldUV = true)
        {
            Vector3 h = size * 0.5f, c = center;
            Vector3 p000 = c + new Vector3(-h.x, -h.y, -h.z), p100 = c + new Vector3(h.x, -h.y, -h.z);
            Vector3 p010 = c + new Vector3(-h.x, h.y, -h.z), p110 = c + new Vector3(h.x, h.y, -h.z);
            Vector3 p001 = c + new Vector3(-h.x, -h.y, h.z), p101 = c + new Vector3(h.x, -h.y, h.z);
            Vector3 p011 = c + new Vector3(-h.x, h.y, h.z), p111 = c + new Vector3(h.x, h.y, h.z);
            if ((faces & 1) != 0) AddQuad(sub, p100, p110, p111, p101, worldUV);
            if ((faces & 2) != 0) AddQuad(sub, p001, p011, p010, p000, worldUV);
            if ((faces & 4) != 0) AddQuad(sub, p010, p011, p111, p110, worldUV);
            if ((faces & 8) != 0) AddQuad(sub, p000, p100, p101, p001, worldUV);
            if ((faces & 16) != 0) AddQuad(sub, p101, p111, p011, p001, worldUV);
            if ((faces & 32) != 0) AddQuad(sub, p000, p010, p110, p100, worldUV);
        }

        /// <summary>Box with slightly bevelled look achieved via random per-vertex jitter (for rough stone).</summary>
        public void AddRoughBox(int sub, Vector3 center, Vector3 size, float jitter, int seed)
        {
            int start = verts.Count;
            AddBox(sub, center, size);
            var rnd = new System.Random(seed);
            // Jitter shared corners consistently by position hash so the box stays closed.
            for (int i = start; i < verts.Count; i++)
            {
                Vector3 p = verts[i];
                int hsh = (Mathf.RoundToInt(p.x * 1000) * 73856093) ^ (Mathf.RoundToInt(p.y * 1000) * 19349663) ^ (Mathf.RoundToInt(p.z * 1000) * 83492791) ^ seed;
                var r2 = new System.Random(hsh);
                verts[i] = p + new Vector3((float)r2.NextDouble() - .5f, (float)r2.NextDouble() - .5f, (float)r2.NextDouble() - .5f) * jitter;
            }
        }

        /// <summary>Cylinder / frustum along local Y from y0 to y1.</summary>
        public void AddCylinder(int sub, Vector3 baseCenter, float r0, float r1, float height, int sides = 12, bool caps = true, bool smooth = true, float uvU = 1f)
        {
            var bw = CurrentWeight();
            int ring0 = verts.Count;
            float slope = (r0 - r1) / Mathf.Max(0.0001f, height);
            for (int i = 0; i <= sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 n = (dir + Vector3.up * slope).normalized;
                AddVertex(baseCenter + dir * r0, n, new Vector2(i / (float)sides * uvU, 0));
                AddVertex(baseCenter + dir * r1 + Vector3.up * height, n, new Vector2(i / (float)sides * uvU, height * uvScale));
            }
            for (int i = 0; i < sides; i++)
            {
                int a = ring0 + i * 2, b = a + 1, c = a + 3, d = a + 2;
                Tri(sub, a, b, c); Tri(sub, a, c, d);
            }
            if (caps)
            {
                AddDisc(sub, baseCenter + Vector3.up * height, r1, sides, true);
                if (r0 > 0.0001f) AddDisc(sub, baseCenter, r0, sides, false);
            }
        }

        public void AddDisc(int sub, Vector3 center, float r, int sides, bool up)
        {
            Vector3 n = up ? Vector3.up : Vector3.down;
            int c = AddVertex(center, n, new Vector2(0.5f, 0.5f));
            int first = verts.Count;
            for (int i = 0; i <= sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2;
                AddVertex(center + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), n, new Vector2(0.5f + Mathf.Cos(a) * .5f, 0.5f + Mathf.Sin(a) * .5f));
            }
            for (int i = 0; i < sides; i++)
            {
                if (up) Tri(sub, c, first + i + 1, first + i);
                else Tri(sub, c, first + i, first + i + 1);
            }
        }

        /// <summary>Ellipsoid with optional deformation of unit-sphere directions.</summary>
        public void AddEllipsoid(int sub, Vector3 center, Vector3 radii, int lat = 10, int lon = 14, Func<Vector3, Vector3> deform = null, Func<Vector3, BoneWeight> weightFn = null)
        {
            int start = verts.Count;
            for (int y = 0; y <= lat; y++)
            {
                float v = y / (float)lat;
                float phi = v * Mathf.PI;
                for (int x = 0; x <= lon; x++)
                {
                    float u = x / (float)lon;
                    float th = u * Mathf.PI * 2;
                    Vector3 d = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                    Vector3 p = Vector3.Scale(d, radii);
                    if (deform != null) p = deform(p);
                    Vector3 wp = M.MultiplyPoint3x4(center + p);
                    var bw = weightFn != null ? weightFn(center + p) : CurrentWeight();
                    AddVertexRaw(wp, M.MultiplyVector(new Vector3(d.x / radii.x, d.y / radii.y, d.z / radii.z)).normalized, new Vector2(u, 1 - v), bw);
                }
            }
            for (int y = 0; y < lat; y++)
                for (int x = 0; x < lon; x++)
                {
                    int a = start + y * (lon + 1) + x, b = a + 1, c = a + lon + 1, d = c + 1;
                    Tri(sub, a, b, d); Tri(sub, a, d, c);
                }
            if (deform != null) RecalcNormalsRange(start, verts.Count, sub);
        }

        /// <summary>Recompute smooth normals for a vertex range using the triangles of one submesh.</summary>
        public void RecalcNormalsRange(int start, int end, int sub)
        {
            var acc = new Vector3[end - start];
            var t = Tris(sub);
            for (int i = 0; i < t.Count; i += 3)
            {
                int a = t[i], b = t[i + 1], c = t[i + 2];
                if (a < start || a >= end) continue;
                Vector3 n = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
                acc[a - start] += n; acc[b - start] += n; acc[c - start] += n;
            }
            // weld seams by position so UV seams don't show lighting cracks
            var map = new Dictionary<Vector3Int, Vector3>();
            for (int i = 0; i < acc.Length; i++)
            {
                var k = Key(verts[start + i]);
                map.TryGetValue(k, out var s); map[k] = s + acc[i];
            }
            for (int i = 0; i < acc.Length; i++)
            {
                var n = map[Key(verts[start + i])];
                if (n.sqrMagnitude > 1e-12f) normals[start + i] = n.normalized;
            }
        }

        static Vector3Int Key(Vector3 p) => new Vector3Int(Mathf.RoundToInt(p.x * 10000), Mathf.RoundToInt(p.y * 10000), Mathf.RoundToInt(p.z * 10000));

        public struct Ring
        {
            public Vector3 center;      // local position
            public Quaternion rot;      // ring plane orientation (ring lies in rot*XZ plane, axis = rot*Y)
            public float rx, rz;        // radii along ring X and Z
            public BoneWeight weight;
            public Func<float, float> radial; // optional radius multiplier by angle (radians)
            public float offsetZ;      // shifts ring center forward (for bellies, chests)
            public Func<float, BoneWeight> weightFn; // optional per-angle weights (skirts)
        }

        public static BoneWeight W(int b0) => new BoneWeight { boneIndex0 = b0, weight0 = 1 };
        public static BoneWeight W(int b0, int b1, float t)
        {
            if (t <= 0.001f) return W(b0);
            if (t >= 0.999f) return W(b1);
            return new BoneWeight { boneIndex0 = b0, weight0 = 1 - t, boneIndex1 = b1, weight1 = t };
        }

        /// <summary>
        /// Loft a smooth tube through rings. Caps optional. Rings are in local space (transformed by M). The surface faces
        /// outward whichever way the rings run along their axis; pass inward for linings seen from inside.
        /// </summary>
        public void AddLoft(int sub, IList<Ring> rings, int sides = 12, bool capStart = true, bool capEnd = true, float vScale = 1f, bool inward = false)
        {
            // rings listed against their own axis (e.g. a limb built top-down with its axis pointing up) would wind inside out
            float along = 0;
            for (int r = 0; r < rings.Count - 1; r++) along += Vector3.Dot(rings[r].rot * Vector3.up, rings[r + 1].center - rings[r].center);
            bool flip = (along < 0) != inward;
            int start = verts.Count;
            float vAcc = 0;
            for (int r = 0; r < rings.Count; r++)
            {
                var ring = rings[r];
                if (r > 0) vAcc += Vector3.Distance(rings[r - 1].center, ring.center) * vScale;
                for (int i = 0; i <= sides; i++)
                {
                    float a = i / (float)sides * Mathf.PI * 2;
                    float m = ring.radial != null ? ring.radial(a) : 1f;
                    Vector3 lp = new Vector3(Mathf.Cos(a) * ring.rx * m, 0, Mathf.Sin(a) * ring.rz * m + ring.offsetZ);
                    Vector3 p = ring.center + ring.rot * lp;
                    Vector3 wp = M.MultiplyPoint3x4(p);
                    AddVertexRaw(wp, Vector3.up, new Vector2(i / (float)sides, vAcc), ring.weightFn != null ? ring.weightFn(a) : ring.weight);
                }
            }
            int stride = sides + 1;
            for (int r = 0; r < rings.Count - 1; r++)
                for (int i = 0; i < sides; i++)
                {
                    int a = start + r * stride + i, b = a + 1, c = a + stride, d = c + 1;
                    if (!flip) { Tri(sub, a, c, d); Tri(sub, a, d, b); }
                    else { Tri(sub, a, d, c); Tri(sub, a, b, d); }
                }
            int end = verts.Count;
            RecalcNormalsRange(start, end, sub);
            bool reversed = along < 0;
            if (capStart) CapRing(sub, rings[0], sides, reversed);
            if (capEnd) CapRing(sub, rings[rings.Count - 1], sides, !reversed);
        }

        void CapRing(int sub, Ring ring, int sides, bool top)
        {
            Vector3 axis = M.MultiplyVector(ring.rot * Vector3.up).normalized * (top ? 1 : -1);
            // dome the cap slightly so ends look rounded
            float dome = Mathf.Min(ring.rx, ring.rz) * 0.5f;
            Vector3 cLocal = ring.center + ring.rot * (Vector3.up * (top ? dome : -dome) + Vector3.forward * ring.offsetZ);
            int c = AddVertexRaw(M.MultiplyPoint3x4(cLocal), axis, new Vector2(0.5f, 0.5f), ring.weight);
            int first = verts.Count;
            for (int i = 0; i <= sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2;
                float m = ring.radial != null ? ring.radial(a) : 1f;
                Vector3 lp = new Vector3(Mathf.Cos(a) * ring.rx * m, 0, Mathf.Sin(a) * ring.rz * m + ring.offsetZ);
                Vector3 wp = M.MultiplyPoint3x4(ring.center + ring.rot * lp);
                Vector3 n = (M.MultiplyVector(ring.rot * lp).normalized * 0.6f + axis).normalized;
                AddVertexRaw(wp, n, new Vector2(0.5f + Mathf.Cos(a) * .5f, 0.5f + Mathf.Sin(a) * .5f), ring.weight);
            }
            for (int i = 0; i < sides; i++)
            {
                if (top) Tri(sub, c, first + i + 1, first + i);
                else Tri(sub, c, first + i, first + i + 1);
            }
        }

        /// <summary>Simple tube along a polyline of points with radii, all weighted to current bone.</summary>
        public void AddTube(int sub, IList<Vector3> pts, IList<float> radii, int sides = 8, bool caps = true, float flatten = 1f)
        {
            var rings = new List<Ring>(pts.Count);
            var bw = CurrentWeight();
            for (int i = 0; i < pts.Count; i++)
            {
                Vector3 dir = i < pts.Count - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1];
                if (i > 0 && i < pts.Count - 1) dir = pts[i + 1] - pts[i - 1];
                var rot = Quaternion.FromToRotation(Vector3.up, dir.normalized);
                rings.Add(new Ring { center = pts[i], rot = rot, rx = radii[i], rz = radii[i] * flatten, weight = bw });
            }
            AddLoft(sub, rings, sides, caps, caps);
        }

        public void AddCone(int sub, Vector3 baseCenter, float r, float height, int sides = 10)
        {
            AddCylinder(sub, baseCenter, r, 0.0005f, height, sides, true);
        }

        /// <summary>Wedge / triangular prism (for roofs): triangle in XY extruded along Z.</summary>
        public void AddPrism(int sub, Vector3 a, Vector3 b, Vector3 c, float depth)
        {
            Vector3 dz = new Vector3(0, 0, depth);
            AddQuad(sub, a, b, b + dz, a + dz);
            AddQuad(sub, b, c, c + dz, b + dz);
            AddQuad(sub, c, a, a + dz, c + dz);
            // end caps
            var bw = CurrentWeight();
            Vector3 n0 = M.MultiplyVector(Vector3.back).normalized, n1 = -n0;
            int i0 = AddVertexRaw(M.MultiplyPoint3x4(a), n0, new Vector2(a.x, a.y) * uvScale, bw);
            int i1 = AddVertexRaw(M.MultiplyPoint3x4(c), n0, new Vector2(c.x, c.y) * uvScale, bw);
            int i2 = AddVertexRaw(M.MultiplyPoint3x4(b), n0, new Vector2(b.x, b.y) * uvScale, bw);
            Tri(sub, i0, i1, i2);
            int j0 = AddVertexRaw(M.MultiplyPoint3x4(a + dz), n1, new Vector2(a.x, a.y) * uvScale, bw);
            int j1 = AddVertexRaw(M.MultiplyPoint3x4(b + dz), n1, new Vector2(b.x, b.y) * uvScale, bw);
            int j2 = AddVertexRaw(M.MultiplyPoint3x4(c + dz), n1, new Vector2(c.x, c.y) * uvScale, bw);
            Tri(sub, j0, j1, j2);
        }

        /// <summary>Append another builder's geometry (same submesh layout) under the current transform.</summary>
        public void Append(MeshBuilder o)
        {
            int baseIdx = verts.Count;
            for (int i = 0; i < o.verts.Count; i++)
            {
                verts.Add(M.MultiplyPoint3x4(o.verts[i]));
                normals.Add(M.MultiplyVector(o.normals[i]).normalized);
                uvs.Add(o.uvs[i]); colors.Add(o.colors[i]);
                if (useWeights) weights.Add(o.useWeights ? o.weights[i] : CurrentWeight());
            }
            for (int s = 0; s < o.subTris.Count; s++)
            {
                var t = Tris(s);
                foreach (var idx in o.subTris[s]) t.Add(idx + baseIdx);
            }
        }

        public int SubmeshCount => subTris.Count;

        public Mesh Build(string name = "proc", bool tangents = true)
        {
            var m = new Mesh { name = name };
            if (verts.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(verts);
            m.SetNormals(normals);
            m.SetUVs(0, uvs);
            m.SetColors(colors);
            // Trim trailing empty submeshes but keep indices aligned with materials
            m.subMeshCount = subTris.Count;
            for (int i = 0; i < subTris.Count; i++) m.SetTriangles(subTris[i], i, false);
            if (useWeights && weights.Count == verts.Count) m.boneWeights = weights.ToArray();
            m.RecalculateBounds();
            if (tangents) m.RecalculateTangents();
            return m;
        }
    }
}
