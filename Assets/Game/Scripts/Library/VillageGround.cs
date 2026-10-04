using System.Collections.Generic;
using Dungine.World;
using UnityEngine;

namespace Dungine.Library
{
    /// <summary>
    /// v3 phase 4: the library's ground for the Village of Barovia (review 43, agent E's E02; agent N's game export), in
    /// place of v2's terrain.
    ///
    /// The file is village_ground.json (Resources/Library/Village), with the terrain kit's modules in
    /// Resources/Library/Ground/&lt;kit&gt;/&lt;module&gt; and 99 far meshes (24 m squares at 15 cm) in Village/ground_far.
    ///   - Heights: the file's 'levels' (each 1.5 m grid corner, in 0.3 m steps) are written into v2's Unity terrain, which
    ///     then stops drawing but stays as the collider. So the navmesh, ctx.GroundY and everything v2 places afterwards
    ///     stand on the library's ground: the manor's 3 m hill, the church's 1.5 m rise. Outside the library ground, v2's
    ///     heights stay (it is never drawn).
    ///   - Tiles: placed by the kit's rule, at (x, z, y) / 100 turned 180 - 90 q, with absolute heights (the file's
    ///     origin is the village's ground at 0). Each tile has its own detail levels (<see cref="Distances"/>), culled
    ///     beyond the last; its 24 m square's far mesh takes over from there (a LODGroup that draws nothing near).
    ///   - Texture AO: the terrain meshes have no COLOR_0, which the material reads as no vertex AO.
    /// </summary>
    public static class VillageGround
    {
        public static bool On = true;
        /// <summary>A tile's levels by distance from the camera (m): 1.5 cm nearer than the first, 3 cm to the second,
        /// 6 cm to the third; beyond it the far meshes.</summary>
        public static float[] Distances = { 12f, 40f, 90f };
        public const string Folder = "Library/Ground/";
        public static string Report = "";

        /// <summary>The ground's height (m) at a v2 point from the file's levels, bilinear; NaN outside the file.</summary>
        public static float LevelAt(float x, float z)
        {
            if (levels == null) return float.NaN;
            float gx = (x * 100f - x0) / tile, gz = (z * 100f - y0) / tile;
            int i = Mathf.FloorToInt(gx), j = Mathf.FloorToInt(gz);
            if (i < 0 || j < 0 || i + 1 >= levels.GetLength(0) || j + 1 >= levels.GetLength(1)) return float.NaN;
            float fx = gx - i, fz = gz - j;
            float a = Mathf.Lerp(levels[i, j], levels[i + 1, j], fx), b = Mathf.Lerp(levels[i, j + 1], levels[i + 1, j + 1], fx);
            return Mathf.Lerp(a, b, fz) * step / 100f;
        }

        static float[,] levels;
        /// <summary>The library's ground was laid for the area being built.</summary>
        public static bool Active => levels != null;
        static float x0, y0, tile = 150f, step = 30f;
        static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

        /// <summary>After v2's terrain is built: its heights become the library's, and the library's ground is laid.</summary>
        public static bool Build(AreaContext ctx)
        {
            levels = null; Report = "ground off";
            if (!On) return false;
            var ta = Resources.Load<TextAsset>("Library/Village/village_ground");
            if (!ta) { Report = "no village_ground.json"; return false; }
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var data = MiniJson.Parse(ta.text);
            var grid = MiniJson.Obj(data, "grid");
            x0 = MiniJson.Num(grid, "x0_cm"); y0 = MiniJson.Num(grid, "y0_cm"); tile = MiniJson.Num(grid, "tile_cm", 150f); step = MiniJson.Num(grid, "level_step_cm", 30f);
            var rows = MiniJson.Arr(data, "levels");
            int ni = rows.Count, nj = ((List<object>)rows[0]).Count;
            levels = new float[ni, nj];
            for (int i = 0; i < ni; i++) { var r = (List<object>)rows[i]; for (int j = 0; j < nj; j++) levels[i, j] = System.Convert.ToSingle(r[j]); }
            Reshape(ctx);
            long tHeights = clock.ElapsedMilliseconds;

            var root = new GameObject("VillageGround").transform;
            root.SetParent(ctx.root, false);
            // far groups: which placement (tiles then props, counted together) each 24 m far mesh stands for
            var tiles = MiniJson.Arr(data, "tiles"); var props = MiniJson.Arr(data, "props") ?? new List<object>();
            var groupOf = new Dictionary<int, Transform>();
            int farCount = 0;
            foreach (var f in MiniJson.Arr(data, "ground_far") ?? new List<object>())
            {
                var name = MiniJson.Str(f, "group");
                var g = new GameObject("Ground_" + name).transform; g.SetParent(root, false);
                foreach (var idx in MiniJson.Arr(f, "placements") ?? new List<object>()) groupOf[System.Convert.ToInt32(idx)] = g;
                var farPrefab = Resources.Load<GameObject>("Library/Village/ground_far/" + System.IO.Path.GetFileNameWithoutExtension(MiniJson.Str(f, "glb")));
                if (farPrefab) { FarLod(g, farPrefab); farCount++; }
            }
            int placed = 0, missing = 0, index = -1;
            long tris0 = 0;
            foreach (var list in new[] { tiles, props })
                foreach (var p in list)
                {
                    index++;
                    var prefab = Prefab(MiniJson.Str(p, "glb"));
                    if (!prefab) { missing++; continue; }
                    var at = MiniJson.Floats(p, "at_cm");
                    int q = (int)MiniJson.Num(p, "quarter");
                    groupOf.TryGetValue(index, out var parent);
                    var go = Object.Instantiate(prefab, parent ? parent : root);
                    go.transform.SetPositionAndRotation(new Vector3(at[0] / 100f, at[2] / 100f, at[1] / 100f), Quaternion.Euler(0, 180f - 90f * q, 0));
                    TileLods(go, ref tris0);
                    placed++;
                }
            Report = $"ground: {placed} tiles and outcrops placed ({missing} missing a module), {farCount} far meshes, " +
                     $"{tris0 / 1000000f:F1} M triangles at 1.5 cm; heights {tHeights} ms, placing {clock.ElapsedMilliseconds - tHeights} ms";
            Debug.Log("[VillageGround] " + Report);
            return true;
        }

        static GameObject Prefab(string glb)
        {
            if (string.IsNullOrEmpty(glb)) return null;
            // environment/terrain/<kit>/<module>/<module>.glb -> Library/Ground/<kit>/<module>
            var parts = glb.Split('/');
            string key = parts.Length >= 4 ? parts[2] + "/" + System.IO.Path.GetFileNameWithoutExtension(glb) : glb;
            if (!prefabs.TryGetValue(key, out var p)) prefabs[key] = p = Resources.Load<GameObject>(Folder + key);
            return p;
        }

        /// <summary>v2's terrain takes the library's heights and stops drawing; it stays the collider.</summary>
        static void Reshape(AreaContext ctx)
        {
            var t = ctx.terrain;
            if (!t) return;
            var td = t.terrainData;
            int res = td.heightmapResolution;
            var h = td.GetHeights(0, 0, res, res);
            Vector3 tp = t.transform.position, size = td.size;
            for (int j = 0; j < res; j++)
                for (int i = 0; i < res; i++)
                {
                    float wx = tp.x + i / (float)(res - 1) * size.x, wz = tp.z + j / (float)(res - 1) * size.z;
                    float y = LevelAt(wx, wz);
                    if (!float.IsNaN(y)) h[j, i] = Mathf.Clamp01((y - tp.y) / size.y);
                }
            td.SetHeights(0, 0, h);
            t.drawHeightmap = false;
            t.drawTreesAndFoliage = false;
        }

        /// <summary>A tile's own levels: 1.5, 3 and 6 cm by <see cref="Distances"/>, then nothing (its far mesh).</summary>
        static void TileLods(GameObject go, ref long tris0)
        {
            var byLevel = new List<Renderer>[3];
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!r.name.StartsWith("Body_LOD") || !int.TryParse(r.name.Substring(8), out int l)) continue;
                if (l > 2) { Object.Destroy(r.gameObject); continue; }   // the far mesh stands for the 15 cm level
                (byLevel[l] ??= new List<Renderer>()).Add(r);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // ground: no shadow of its own
                r.receiveShadows = true;
                LibraryFigures.ApplyVoxelMaterials(r);
                if (l == 0) { var mf = r.GetComponent<MeshFilter>(); if (mf && mf.sharedMesh) tris0 += mf.sharedMesh.GetIndexCount(0) / 3; }
            }
            if (byLevel[0] == null) return;
            var lods = new List<LOD>();
            for (int i = 0; i < 3; i++)
            {
                if (byLevel[i] == null) continue;
                lods.Add(new LOD(Height(1.5f, Distances[i]), byLevel[i].ToArray()));
            }
            var lg = go.AddComponent<LODGroup>();
            lg.fadeMode = LODFadeMode.None;
            lg.SetLODs(lods.ToArray());
            var b0 = byLevel[0][0].bounds;
            lg.localReferencePoint = go.transform.InverseTransformPoint(b0.center);
            lg.size = 1.5f;
        }

        /// <summary>A 24 m square's far mesh: nothing near, the far mesh from the tiles' last distance out.</summary>
        static void FarLod(Transform group, GameObject prefab)
        {
            var go = Object.Instantiate(prefab, group);
            go.name = "Far";
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 180f, 0));
            var r = go.GetComponentInChildren<MeshRenderer>(true);
            if (!r) return;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            LibraryFigures.ApplyVoxelMaterials(r);
            var b = r.bounds;
            float size = Mathf.Max(b.size.x, b.size.z);
            var lg = group.gameObject.AddComponent<LODGroup>();
            lg.fadeMode = LODFadeMode.None;
            lg.SetLODs(new[] { new LOD(Height(size, Distances[2]), new Renderer[0]), new LOD(LibraryFigures.CullHeight, new Renderer[] { r }) });
            lg.localReferencePoint = group.InverseTransformPoint(b.center);
            lg.size = size;
        }

        /// <summary>The LODGroup screen height at which an object of this size switches, at this distance (m).</summary>
        static float Height(float size, float distance)
        {
            var cam = Camera.main;
            float tan = Mathf.Tan(0.5f * Mathf.Deg2Rad * (cam ? cam.fieldOfView : 38f));
            return Mathf.Clamp(size * QualitySettings.lodBias / (2f * distance * tan), LibraryFigures.CullHeight * 1.5f, 0.999f);
        }
    }
}
