using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dungine.LookTest.EditorTools
{
    /// <summary>
    /// Look test (phase 3): turns the library GLBs that glTFast imported (Assets/LookTest/Models) into prefabs the game
    /// can place (Assets/LookTest/Resources/LookTest/Baked). Each prefab is one static mesh with the Dungine/Voxel material,
    /// plus one glow material per glow colour the GLB carries. Figures lose the round ground disc their review renders
    /// stand on: the disc's voxels (below the feet) go, and so does anything left standing on it but not joined to the body.
    /// </summary>
    public static class LookTestBake
    {
        const string ModelDir = "Assets/LookTest/Models";
        const string OutDir = "Assets/LookTest/Resources/LookTest/Baked";
        const float Vox = 0.015f;
        const float GlowBoost = 4f;   // the GLB's emissive factor is at most 1; the library renders glow at strength 1.5-6

        [MenuItem("Dungine/Look Test/Bake Library Models")]
        static void BakeMenu() => Debug.Log(BakeAll());

        public static string BakeAll()
        {
            var report = Ids().Select(BakeOne).ToList();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return string.Join("\n", report);
        }

        /// <summary>The model ids waiting in Assets/LookTest/Models.</summary>
        public static string[] Ids() => Directory.GetFiles(ModelDir, "*.glb").Select(Path.GetFileNameWithoutExtension).OrderBy(p => p).ToArray();

        /// <summary>Bakes one model (the CLI's eval has a 5-second limit, so the tools bake one at a time).</summary>
        public static string BakeOne(string id)
        {
            Directory.CreateDirectory(OutDir);
            var shader = Shader.Find("Dungine/Voxel");
            if (!shader) return "Dungine/Voxel shader not found";
            var baseMat = MakeMaterial(shader, "voxel_base", Color.black);
            var path = $"{ModelDir}/{id}.glb";
            var root = AssetDatabase.LoadMainAssetAtPath(path) as GameObject;
            if (!root) return id + ": not imported";
            bool figure = HasGroundDisc(Path.ChangeExtension(path, ".json"));
            var s = Bake(id, root, figure, shader, baseMat);
            AssetDatabase.SaveAssets();
            return s;
        }

        /// <summary>Figures from the library's reviews start 4 voxels below the feet (origin z = -4): that's the disc.</summary>
        static bool HasGroundDisc(string jsonPath)
        {
            if (!File.Exists(jsonPath)) return false;
            var txt = File.ReadAllText(jsonPath);
            int i = txt.IndexOf("\"origin\"");
            if (i < 0) return false;
            var nums = txt.Substring(i, txt.IndexOf(']', i) - i).Split('[')[1].Split(',');
            return nums.Length == 3 && int.Parse(nums[2].Trim()) < 0;
        }

        static Material MakeMaterial(Shader shader, string name, Color glow)
        {
            var p = $"{OutDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, p); }
            m.shader = shader;
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.15f);
            m.SetFloat("_Metallic", 0f);
            m.SetColor("_EmissionColor", glow);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
            return m;
        }

        static string Bake(string id, GameObject root, bool figure, Shader shader, Material baseMat)
        {
            // gather every mesh in the imported hierarchy, in the root's space
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var cols = new List<Color>();
            var subTris = new List<List<int>>(); var subMats = new List<Material>();
            int srcTris = 0;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh; if (!mesh) continue;
                var mr = mf.GetComponent<MeshRenderer>();
                var toRoot = root.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                int baseIndex = verts.Count;
                var v = mesh.vertices; var n = mesh.normals; var c = mesh.colors;
                for (int i = 0; i < v.Length; i++)
                {
                    verts.Add(toRoot.MultiplyPoint3x4(v[i]));
                    norms.Add(n.Length == v.Length ? toRoot.MultiplyVector(n[i]).normalized : Vector3.up);
                    cols.Add(c.Length == v.Length ? c[i] : Color.white);
                }
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    var tris = mesh.GetTriangles(s);
                    srcTris += tris.Length / 3;
                    var src = mr && s < mr.sharedMaterials.Length ? mr.sharedMaterials[s] : null;
                    var glow = GlowOf(src);
                    Material mat = glow.maxColorComponent > 0.001f
                        ? MakeMaterial(shader, "voxel_glow_" + ColorUtility.ToHtmlStringRGB(glow), glow * GlowBoost)
                        : baseMat;
                    int slot = subMats.IndexOf(mat);
                    if (slot < 0) { subMats.Add(mat); subTris.Add(new List<int>()); slot = subMats.Count - 1; }
                    subTris[slot].AddRange(tris.Select(t => t + baseIndex));
                }
            }
            if (verts.Count == 0) return id + ": no mesh";

            int dropped = 0;
            if (figure) dropped = StripGroundDisc(verts, norms, subTris);

            // compact the vertex list to what's still used
            var remap = new Dictionary<int, int>();
            var nv = new List<Vector3>(); var nn = new List<Vector3>(); var nc = new List<Color>();
            foreach (var list in subTris)
                for (int i = 0; i < list.Count; i++)
                {
                    int old = list[i];
                    if (!remap.TryGetValue(old, out int ni)) { ni = nv.Count; remap[old] = ni; nv.Add(verts[old]); nn.Add(norms[old]); nc.Add(cols[old]); }
                    list[i] = ni;
                }
            var outMesh = new Mesh { name = id + "_voxels", indexFormat = IndexFormat.UInt32 };
            outMesh.SetVertices(nv); outMesh.SetNormals(nn); outMesh.SetColors(nc);
            outMesh.subMeshCount = subTris.Count;
            for (int s = 0; s < subTris.Count; s++) outMesh.SetTriangles(subTris[s], s, false);
            outMesh.RecalculateBounds();
            outMesh.UploadMeshData(false);
            var meshPath = $"{OutDir}/{id}_mesh.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing) { EditorUtility.CopySerialized(outMesh, existing); outMesh = existing; } else AssetDatabase.CreateAsset(outMesh, meshPath);

            var go = new GameObject(id);
            go.AddComponent<MeshFilter>().sharedMesh = outMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = subMats.ToArray();
            r.shadowCastingMode = ShadowCastingMode.On;
            PrefabUtility.SaveAsPrefabAsset(go, $"{OutDir}/{id}.prefab");
            Object.DestroyImmediate(go);
            int outTris = subTris.Sum(l => l.Count) / 3;
            var b = outMesh.bounds;
            return $"{id}: {srcTris} tris in, {outTris} out{(figure ? $" (ground disc: {dropped} tris removed)" : "")}, {subMats.Count} material(s), size {b.size.x:F2} x {b.size.y:F2} x {b.size.z:F2} m";
        }

        static Color GlowOf(Material m)
        {
            if (!m) return Color.black;
            foreach (var prop in new[] { "_EmissionColor", "emissiveFactor", "_EmissiveFactor" })
                if (m.HasProperty(prop)) { var c = m.GetColor(prop); c.a = 1; return c; }
            return Color.black;
        }

        /// <summary>Removes the disc's voxels (below y = 0) and everything no longer joined to the body (grass, pebbles).</summary>
        static int StripGroundDisc(List<Vector3> v, List<Vector3> n, List<List<int>> subTris)
        {
            // every triangle belongs to the voxel behind its face
            Vector3Int CellOf(int a, int b, int c)
            {
                var centre = (v[a] + v[b] + v[c]) / 3f;
                var inside = centre - n[a] * (Vox * 0.5f);
                return new Vector3Int(Mathf.FloorToInt(inside.x / Vox + 1e-3f), Mathf.FloorToInt(inside.y / Vox + 1e-3f), Mathf.FloorToInt(inside.z / Vox + 1e-3f));
            }
            var cells = new HashSet<Vector3Int>();
            foreach (var list in subTris)
                for (int i = 0; i < list.Count; i += 3)
                {
                    var cell = CellOf(list[i], list[i + 1], list[i + 2]);
                    if (cell.y >= 0) cells.Add(cell);
                }
            if (cells.Count == 0) return 0;
            // flood from the top of the head through face/edge/corner neighbours
            var seed = cells.OrderByDescending(c => c.y).First();
            var keep = new HashSet<Vector3Int> { seed };
            var queue = new Queue<Vector3Int>(); queue.Enqueue(seed);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            var k = new Vector3Int(c.x + dx, c.y + dy, c.z + dz);
                            if (cells.Contains(k) && keep.Add(k)) queue.Enqueue(k);
                        }
            }
            int dropped = 0;
            foreach (var list in subTris)
            {
                var kept = new List<int>(list.Count);
                for (int i = 0; i < list.Count; i += 3)
                {
                    if (keep.Contains(CellOf(list[i], list[i + 1], list[i + 2]))) { kept.Add(list[i]); kept.Add(list[i + 1]); kept.Add(list[i + 2]); }
                    else dropped++;
                }
                list.Clear(); list.AddRange(kept);
            }
            return dropped;
        }
    }
}
