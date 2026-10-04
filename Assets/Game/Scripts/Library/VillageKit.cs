using System.Collections.Generic;
using System.Linq;
using Dungine.World;
using UnityEngine;

namespace Dungine.Library
{
    /// <summary>
    /// v3: the library's gothic Village of Barovia in place of v2's procedural village buildings.
    ///
    /// The layout is agent E's (review 36, pixel3d/landmarks/village_of_barovia/village_set/village_layout.json and the
    /// night file), copied to Resources/Library/Village. The modules are agent N's game export of the E01 kit, copied to
    /// Resources/Library/Village/&lt;kit&gt;[_2]/&lt;module&gt;[_2]. Everything is placed from the JSON when the village is built, so a
    /// new layout or new modules only need copying in again.
    ///
    /// Frames: the layout is x east, y north, z up, in centimetres, with v2's metres times 100, so v2's (x, z) is the
    /// layout's (x, y) / 100. A module's frame is the same, exported to glTF as (x, z, -y), which glTFast imports as
    /// Unity (-x, z, -y). So a module at quarter turn q stands at Euler(0, 180 - 90 q, 0): the half turn undoes the import,
    /// and the layout's quarter turns are anticlockwise seen from above.
    ///
    /// What changes: v2's houses, tavern, shop, manor and church are switched off and the kit's stand in their place.
    /// Each kit building gets box colliders from its ground-floor modules (for the navmesh and the camera), and v2's
    /// door points move to the kit's doors, so every door, spawn and thing v2 sets by a door follows. The fences, walls,
    /// gateposts, well, stalls, notice board and statue are the kit's, while v2's own stay invisible in place so their
    /// colliders and uses still work. v2's ground, roads, lamps, fog and the reserved plot stay as they are.
    /// </summary>
    public class VillageKit
    {
        /// <summary>Dev switches: the kit at all; its site pieces (fences, walls, well...); its cobbles over v2's square.</summary>
        public static bool On = true, PlaceSite = true, PlaceSquare = true;
        /// <summary>The cobbles start at their 3 cm level (2.7 k triangles a 1.5 m tile instead of 37 k) and cast no shadow:
        /// at play zoom a 1.5 cm voxel is under 2 pixels, and the stones still read at 3 cm. False: the full 1.5 cm.</summary>
        public static bool LightCobbles = false;
        /// <summary>Without LightCobbles: how near the camera (m) a cobble tile shows its full 1.5 cm stones.</summary>
        public static float CobbleNear = 55f;
        public const string Folder = "Library/Village/";
        /// <summary>What the last build did (dev tools).</summary>
        public static string Report = "";
        /// <summary>Door moves, by building: v2's door point, the kit's, and the distance (dev tools and the README).</summary>
        public static readonly List<string> DoorLog = new List<string>();

        /// <summary>Where each group (a building, or a 24 m tile of the open site) switches detail, as distances from the
        /// camera in metres: the 1.5 cm modules nearer than the first, then the 3 cm ones, then the 6 cm ones, then the far
        /// mesh (the whole group merged at 15 cm, one draw). At play zoom the camera is about 18 m from the party.
        /// Turned into LODGroup heights for the game camera's field of view and the project's LOD bias.</summary>
        public static float[] LodDistances = { 24f, 55f, 110f };
        /// <summary>Dev: use the far meshes (else the modules' own 15 cm LOD).</summary>
        public static bool FarMeshes = true;

        class Building
        {
            public string id, faces; public int quarter;
            public Vector2 centre, v2Centre; public float baseY;
            public Transform root;
            public readonly List<(GameObject go, string module, int quarter, float z)> parts = new List<(GameObject, string, int, float)>();
        }

        /// <summary>A far group from the layout's 'far' list: a building's, or a site tile's (site_i_j).</summary>
        class FarGroup
        {
            public string name, glb; public Transform root;
            public readonly List<GameObject> modules = new List<GameObject>();
            public bool incomplete;   // some of its placements weren't placed (the cobbles while PlaceSquare is off)
            public Vector2 sum; public int n;
        }
        readonly Dictionary<string, FarGroup> farGroups = new Dictionary<string, FarGroup>();
        readonly Dictionary<int, FarGroup> farOf = new Dictionary<int, FarGroup>();
        bool night;
        /// <summary>The library's ground is down (VillageGround): heights are the layout's own.</summary>
        bool absolute;
        readonly List<(GameObject go, float top)> cobbleTiles = new List<(GameObject, float)>();
        string bedNote = "";

        readonly AreaContext ctx;
        readonly Dictionary<string, Building> buildings = new Dictionary<string, Building>();
        readonly List<GameObject> retired = new List<GameObject>();
        readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        Transform root, site;
        int placed, missingModules; long tPlace, tLods;
        readonly HashSet<string> missing = new HashSet<string>();
        System.Diagnostics.Stopwatch clock;

        VillageKit(AreaContext c) { ctx = c; }

        /// <summary>Places the kit (after v2's terrain is built, before its buildings). Null when off or not installed.</summary>
        public static VillageKit Begin(AreaContext ctx)
        {
            DoorLog.Clear();
            if (!On) { Report = "village kit off"; return null; }
            bool night = Game.I && Game.I.nightfall;
            var ta = Resources.Load<TextAsset>(Folder + (night ? "village_layout_night" : "village_layout"));
            if (!ta) { Report = "no village layout in Resources/" + Folder; return null; }
            var kit = new VillageKit(ctx) { clock = System.Diagnostics.Stopwatch.StartNew(), night = night, absolute = VillageGround.Active };
            kit.Place(MiniJson.Parse(ta.text));
            return kit;
        }

        static Vector2 V2(object o, string key) { var f = MiniJson.Floats(o, key); return f != null && f.Length >= 2 ? new Vector2(f[0], f[1]) / 100f : Vector2.zero; }

        void Place(object data)
        {
            root = new GameObject("VillageKit").transform;
            root.SetParent(ctx.root, false);
            site = new GameObject("Kit_site").transform;
            site.SetParent(root, false);
            foreach (var b in MiniJson.Arr(data, "buildings") ?? new List<object>())
            {
                var kb = new Building
                {
                    id = MiniJson.Str(b, "id"), faces = MiniJson.Str(b, "faces"), quarter = (int)MiniJson.Num(b, "quarter"),
                    centre = V2(b, "centre_cm"), v2Centre = V2(b, "v2_centre_cm"),
                };
                // on the library's ground the layout's heights are absolute (its at_cm includes each building's lift);
                // on v2's ground each building stands where v2's ground is under its middle
                kb.baseY = absolute ? MiniJson.Num(b, "lift_cm", 0f) / 100f : ctx.GroundY(kb.centre.x, kb.centre.y) - 0.05f;
                kb.root = new GameObject("Kit_" + kb.id).transform;
                kb.root.SetParent(root, false);
                kb.root.position = new Vector3(kb.centre.x, kb.baseY, kb.centre.y);
                buildings[kb.id] = kb;
            }
            foreach (var f in MiniJson.Arr(data, "far") ?? new List<object>())
            {
                var g = new FarGroup { name = MiniJson.Str(f, "group"), glb = MiniJson.Str(f, "glb") };
                farGroups[g.name] = g;
                foreach (var i in MiniJson.Arr(f, "placements") ?? new List<object>()) farOf[System.Convert.ToInt32(i)] = g;
            }
            int index = -1;
            foreach (var p in MiniJson.Arr(data, "placements") ?? new List<object>())
            {
                index++;
                farOf.TryGetValue(index, out var fg);
                string kit = MiniJson.Str(p, "kit"), module = MiniJson.Str(p, "module"), bid = MiniJson.Str(p, "building");
                int look = (int)MiniJson.Num(p, "look", 1), q = (int)MiniJson.Num(p, "quarter");
                var at = MiniJson.Floats(p, "at_cm");
                if (at == null || at.Length < 3) continue;
                bool isSite = !buildings.TryGetValue(bid ?? "", out var kb);
                if (isSite && kit == "cobbled_square" ? !PlaceSquare : isSite && !PlaceSite) { if (fg != null) fg.incomplete = true; continue; }
                string sfx = look == 1 ? "" : "_" + look;
                var prefab = Prefab(kit + sfx, module + sfx);
                if (!prefab) { missingModules++; missing.Add(kit + sfx + "/" + module + sfx); continue; }
                float x = at[0] / 100f, z = at[1] / 100f, up = at[2] / 100f;
                float lift = MiniJson.Num(p, "lift_cm", 0f) / 100f;
                float y = absolute ? 0f : isSite ? ctx.GroundY(x, z) : kb.baseY;
                Transform parent = kb != null ? kb.root : site;
                if (isSite && fg != null)
                {
                    if (!fg.root) { fg.root = new GameObject("Kit_" + fg.name).transform; fg.root.SetParent(site, false); }
                    parent = fg.root; fg.sum += new Vector2(x, z); fg.n++;
                }
                var go = Object.Instantiate(prefab, parent);
                go.name = module + sfx;
                go.transform.SetPositionAndRotation(new Vector3(x, y + up, z), Quaternion.Euler(0, 180f - 90f * q, 0));
                bool ground = kit == "cobbled_square";
                if (ground && LightCobbles) StartAtLod1(go);
                if (ground && module.StartsWith("cobbles")) cobbleTiles.Add((go, y + up));
                // each cobble tile has its own detail levels: the full 1.5 cm only near the camera
                bool ownLods = ground && !LightCobbles && CobbleNear > 0;
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                {
                    r.shadowCastingMode = ground ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
                    LibraryFigures.ApplyVoxelMaterials(r);
                }
                if (ownLods) Lods(go, new[] { go }, null, new[] { CobbleNear, LodDistances[1], LodDistances[2] }, true);
                else if (fg != null) fg.modules.Add(go);
                if (!isSite) kb.parts.Add((go, module, q, at[2] - lift * 100f));
                else if (fg == null && !ownLods) Lods(go, new[] { go }, null);
                placed++;
            }
            tPlace = clock.ElapsedMilliseconds;
            foreach (var kb in buildings.Values)
            {
                farGroups.TryGetValue(kb.id, out var fg);
                Lods(kb.root.gameObject, kb.parts.Select(t => t.go).ToArray(), Far(fg, absolute ? 0f : kb.baseY));
                Colliders(kb);
            }
            foreach (var fg in farGroups.Values)
            {
                if (buildings.ContainsKey(fg.name) || !fg.root || fg.modules.Count == 0) continue;
                var c = fg.sum / Mathf.Max(1, fg.n);
                // a tile's far mesh stands at its middle's ground height; its pieces follow the ground one by one
                Lods(fg.root.gameObject, fg.modules.ToArray(), fg.incomplete ? null : Far(fg, absolute ? 0f : ctx.GroundY(c.x, c.y)));
            }
            tLods = clock.ElapsedMilliseconds;
        }

        /// <summary>The group's far mesh, placed (village coordinates: at the origin, turned like a module at quarter 0).</summary>
        Renderer Far(FarGroup fg, float y)
        {
            if (!FarMeshes || fg == null || string.IsNullOrEmpty(fg.glb)) return null;
            var prefab = Resources.Load<GameObject>(Folder + (night ? "far_night/" : "far/") + System.IO.Path.GetFileNameWithoutExtension(fg.glb));
            if (!prefab) return null;
            var parent = buildings.TryGetValue(fg.name, out var kb) ? kb.root : fg.root;
            var go = Object.Instantiate(prefab, parent);
            go.name = "Far_" + fg.name;
            go.transform.SetPositionAndRotation(new Vector3(0, y, 0), Quaternion.Euler(0, 180f, 0));
            var r = go.GetComponentInChildren<MeshRenderer>(true);
            if (!r) return null;
            LibraryFigures.ApplyVoxelMaterials(r);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return r;
        }

        /// <summary>The kit's cobbles sit with their stones' tops at the square's level and their joints below it. v2's ground
        /// is lowered 15 cm under each tile so it doesn't fill the joints, and a flat floor at the stones' level is laid
        /// for walking (the navmesh, clicks and the camera use colliders).</summary>
        void SquareBed()
        {
            if (cobbleTiles.Count == 0) return;
            var floor = new GameObject("Kit_square_floor");
            floor.transform.SetParent(root, false);
            floor.layer = Layers.Ground;
            var rects = new List<Rect>();
            foreach (var (go, top) in cobbleTiles)
            {
                Bounds b = default; bool any = false;
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                    if (r.name.StartsWith("Body_LOD")) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                if (!any) continue;
                var rect = new Rect(b.min.x, b.min.z, b.size.x, b.size.z);
                rects.Add(rect);
                var bc = floor.AddComponent<BoxCollider>();
                bc.center = new Vector3(rect.center.x, top - 0.05f, rect.center.y);
                bc.size = new Vector3(rect.width, 0.1f, rect.height);
            }
            var t = ctx.terrain;
            if (!t) return;
            var td = t.terrainData;
            int res = td.heightmapResolution;
            var h = td.GetHeights(0, 0, res, res);
            Vector3 tp = t.transform.position, size = td.size;
            float drop = 0.15f / size.y;
            int lowered = 0;
            for (int j = 0; j < res; j++)
                for (int i = 0; i < res; i++)
                {
                    float wx = tp.x + i / (float)(res - 1) * size.x, wz = tp.z + j / (float)(res - 1) * size.z;
                    var pt = new Vector2(wx, wz);
                    foreach (var r in rects) if (r.Contains(pt)) { h[j, i] -= drop; lowered++; break; }
                }
            td.SetHeights(0, 0, h);
            bedNote = $" Square bed: {rects.Count} cobble tiles, {lowered} ground samples lowered.";
        }

        /// <summary>Drops a module's 1.5 cm level and moves the others up one, so its 3 cm level shows nearest.</summary>
        static void StartAtLod1(GameObject go)
        {
            var rs = go.GetComponentsInChildren<MeshRenderer>(true);
            if (!rs.Any(r => r.name == "Body_LOD1")) return;
            foreach (var r in rs.OrderBy(r => r.name))
            {
                if (!r.name.StartsWith("Body_LOD") || !int.TryParse(r.name.Substring(8), out int l)) continue;
                if (l == 0) Object.DestroyImmediate(r.gameObject);
                else r.name = "Body_LOD" + (l - 1);
            }
        }

        GameObject Prefab(string kit, string module)
        {
            string key = kit + "/" + module;
            if (!prefabs.TryGetValue(key, out var p)) prefabs[key] = p = Resources.Load<GameObject>(Folder + key);
            return p;
        }

        /// <summary>One LODGroup over the given modules: their Body_LOD0 renderers together, then LOD1, then LOD2, then the
        /// far mesh (or, without one, the modules' LOD3), switching at <see cref="LodDistances"/>.</summary>
        static void Lods(GameObject host, GameObject[] modules, Renderer far, float[] distances = null, bool cullAfterLast = false)
        {
            distances = distances ?? LodDistances;
            var byLevel = new List<List<Renderer>>();
            var bounds = new Bounds(); bool any = false;
            foreach (var m in modules)
                foreach (var r in m.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!r.name.StartsWith("Body_LOD") || !int.TryParse(r.name.Substring(8), out int level)) continue;
                    while (byLevel.Count <= level) byLevel.Add(new List<Renderer>());
                    byLevel[level].Add(r);
                    if (level == 0) { if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds); }
                }
            if (byLevel.Count < 2 || !any) return;
            if (cullAfterLast)
            {
                // the levels the distances name, then nothing (a group's far mesh stands in further out)
                while (byLevel.Count > distances.Length) { foreach (var r in byLevel[byLevel.Count - 1]) Object.DestroyImmediate(r.gameObject); byLevel.RemoveAt(byLevel.Count - 1); }
            }
            // the levels: the modules' 0, 1 and 2, then the far mesh instead of their 3 (whose renderers go)
            if (far && byLevel.Count > 3)
            {
                foreach (var r in byLevel[3]) Object.DestroyImmediate(r.gameObject);
                byLevel[3] = new List<Renderer> { far };
            }
            else if (far) byLevel.Add(new List<Renderer> { far });
            float size = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            // the relative height at distance d, as the LODGroup measures it before the LOD bias multiplies it
            var cam = Camera.main;
            float tan = Mathf.Tan(0.5f * Mathf.Deg2Rad * (cam ? cam.fieldOfView : 38f));
            float bias = QualitySettings.lodBias;
            var lods = new LOD[byLevel.Count];
            float prev = 1f;
            for (int i = 0; i < lods.Length; i++)
            {
                bool last = i == lods.Length - 1 && !cullAfterLast;
                float h = last ? LibraryFigures.CullHeight
                        : i < distances.Length ? size * bias / (2f * distances[i] * tan) : prev * 0.5f;
                if (!last) h = Mathf.Clamp(Mathf.Min(h, prev * 0.99f), LibraryFigures.CullHeight * 1.5f, 0.999f);
                else h = Mathf.Min(h, prev * 0.99f);
                prev = h;
                lods[i] = new LOD(h, byLevel[i].ToArray());
            }
            var lg = host.AddComponent<LODGroup>();
            lg.fadeMode = LODFadeMode.None;
            lg.SetLODs(lods);
            lg.localReferencePoint = host.transform.InverseTransformPoint(bounds.center);
            lg.size = size;
        }

        /// <summary>Box colliders over the building's ground-floor modules (up to 3 m), on the building's own object, as v2's
        /// buildings have, so the navmesh, the camera and clicks treat it as v2's solid block.</summary>
        static void Colliders(Building kb)
        {
            foreach (var (go, module, q, z) in kb.parts)
            {
                if (z > 100f) continue;   // only what stands on the ground floor
                Bounds b = default; bool any = false;
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                    if (r.name == "Body_LOD0") { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                if (!any || b.size.x < 0.01f || b.size.z < 0.01f) continue;
                var bc = kb.root.gameObject.AddComponent<BoxCollider>();
                var top = Mathf.Min(b.max.y, kb.baseY + 3f);
                var c = kb.root.InverseTransformPoint(new Vector3(b.center.x, (b.min.y + top) * 0.5f, b.center.z));
                bc.center = c;
                bc.size = new Vector3(b.size.x, Mathf.Max(0.2f, top - b.min.y), b.size.z);
            }
        }

        static Vector3 Outward(int quarter)
        {
            // a wall's outside is the module's -y; quarter turns are anticlockwise seen from above (east to north)
            switch (((quarter % 4) + 4) % 4)
            {
                case 0: return Vector3.back;      // south
                case 1: return Vector3.right;     // east
                case 2: return Vector3.forward;   // north
                default: return Vector3.left;     // west
            }
        }

        static readonly string[] DoorLeaves = { "church_doors", "tavern_doors", "manor_door", "door_pointed" };

        /// <summary>
        /// Takes over one of v2's buildings: finds the kit building standing on its plot (by v2's centre), switches v2's
        /// building off, and moves its door point to the kit's front door. 'v2Out' is how far in front of its door face v2
        /// puts that building's door point (1.2 m for houses, the tavern and the shop, 2 m for the church, 3.05 m for the
        /// manor), so whatever v2 sets relative to it (the door's trigger, the spawn, barrels by the door) keeps its place
        /// relative to the kit's door.
        /// </summary>
        public void Adopt(BuildingInfo info, Vector2 v2Centre, float v2Out, string label)
        {
            if (info == null) return;
            var kb = buildings.Values.OrderBy(b => (b.v2Centre - v2Centre).sqrMagnitude).FirstOrDefault();
            if (kb == null || (kb.v2Centre - v2Centre).magnitude > 1.5f) { DoorLog.Add($"{label}: no kit building on v2's plot at {v2Centre}"); return; }
            if (info.go) { info.go.SetActive(false); retired.Add(info.go); }
            // the front door: a door leaf facing the way the building faces, the nearest to the middle of that face
            var facing = FacesVector(kb.faces);
            GameObject leaf = null; float best = float.MaxValue;
            foreach (var (go, module, q, z) in kb.parts)
            {
                if (!DoorLeaves.Any(d => module == d || module.StartsWith(d + "_"))) continue;
                if (Vector3.Dot(Outward(q), facing) < 0.9f) continue;
                var c = Centre(go);
                float d = Vector3.ProjectOnPlane(c - kb.root.position, facing).sqrMagnitude;
                if (d < best) { best = d; leaf = go; }
            }
            if (!leaf) { DoorLog.Add($"{label} ({kb.id}): no front door found in the kit; v2's door point kept"); return; }
            // the door's face: out to the front of the ground-floor walls beside it (jambs, frames and steps stand proud
            // of the leaf), so the door point sits just outside what the colliders block
            var face = Centre(leaf);
            var side = Vector3.Cross(Vector3.up, facing);
            float outer = Vector3.Dot(face, facing), lateral = Vector3.Dot(face, side);
            foreach (var (go, module, q, z) in kb.parts)
            {
                if (z > 100f) continue;
                var b = Bounds0(go); if (b.size == Vector3.zero) continue;
                float lc = Vector3.Dot(b.center, side), le = Vector3.Dot(b.extents, Abs(side));
                if (lateral < lc - le - 0.3f || lateral > lc + le + 0.3f) continue;
                outer = Mathf.Max(outer, Vector3.Dot(b.center, facing) + Vector3.Dot(b.extents, Abs(facing)));
            }
            face += facing * (outer - Vector3.Dot(face, facing));
            face.y = kb.baseY + 0.05f;
            var oldWorld = info.doorWorld;
            info.doorWorld = ctx.G3(face.x + facing.x * v2Out, face.z + facing.z * v2Out);
            info.doorFacing = facing;
            DoorLog.Add($"{label} ({kb.id}): door point moved {Vector3.Distance(Flat(oldWorld), Flat(info.doorWorld)):F2} m, from ({oldWorld.x:F1}, {oldWorld.z:F1}) to ({info.doorWorld.x:F1}, {info.doorWorld.z:F1})");
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);
        static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        static Bounds Bounds0(GameObject go)
        {
            Bounds b = default; bool any = false;
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                if (r.name == "Body_LOD0") { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            return any ? b : new Bounds(go.transform.position, Vector3.zero);
        }

        static Vector3 Centre(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) if (r.name == "Body_LOD0") return r.bounds.center;
            return go.transform.position;
        }

        static Vector3 FacesVector(string faces)
        {
            switch (faces)
            {
                case "east": return Vector3.right;
                case "west": return Vector3.left;
                case "north": return Vector3.forward;
                default: return Vector3.back;
            }
        }

        /// <summary>After v2's village is built: v2's site pieces the kit replaces go invisible (their colliders and uses
        /// stay), and the retired buildings are destroyed. Call before the navmesh is baked.</summary>
        public void Finish(Rect reserved)
        {
            SquareBed();
            int hidden = 0;
            if (PlaceSite)
            {
                var names = new HashSet<string> { "IronFence", "WoodFence", "StoneWall", "gatepost", "well", "noticeboard", "stall_r", "stall_g", "stall_b", "statue" };
                foreach (Transform ch in ctx.root)
                {
                    if (!names.Contains(ch.name)) continue;
                    var p = ch.GetComponentInChildren<Renderer>() ? ch.GetComponentInChildren<Renderer>().bounds.center : ch.position;
                    if (reserved.Contains(new Vector2(p.x, p.z))) continue;   // the reserved plot stays exactly as v2 has it
                    foreach (var r in ch.GetComponentsInChildren<Renderer>()) { r.enabled = false; hidden++; }
                }
            }
            foreach (var go in retired) if (go) Object.Destroy(go);
            // the kit's buildings open a see-through hole round the party, as v2's did (Occluders, VoxelAtlasOccluder)
            long tOcc0 = clock.ElapsedMilliseconds;
            foreach (var kb in buildings.Values)
            {
                var rs = kb.root.GetComponentsInChildren<Renderer>(true);
                // the roof map from the far mesh (one merged 15 cm model), else the coarsest modules
                var roof = rs.Where(r => r.transform.parent && r.transform.parent.name.StartsWith("Far_") || r.name.StartsWith("Far")).ToArray();
                if (roof.Length == 0)
                {
                    int coarsest = 0;
                    foreach (var r in rs) if (r.name.StartsWith("Body_LOD") && int.TryParse(r.name.Substring(8), out int l)) coarsest = Mathf.Max(coarsest, l);
                    roof = rs.Where(r => r.name == "Body_LOD" + coarsest).ToArray();
                }
                Occluders.RegisterGroup(kb.root, rs, roof);
            }
            long tOcc = clock.ElapsedMilliseconds - tOcc0;
            Report = $"village kit: {placed} modules placed in {buildings.Count} buildings and the site, {retired.Count} of v2's buildings retired, " +
                     $"{hidden} of v2's site renderers hidden, {missingModules} placements missing a module ({string.Join(", ", missing.Take(6))}), {clock.ElapsedMilliseconds} ms (placing {tPlace}, LODs and colliders {tLods - tPlace}, the rest of v2's build {tOcc0 - tLods}, see-through set-up {tOcc})" + bedNote;
            Debug.Log("[VillageKit] " + Report);
        }

        /// <summary>v2's NPCs and spawns standing inside a kit building's colliders (dev tools: what needs moving).</summary>
        public static string Crowding(AreaContext ctx)
        {
            var root = ctx.root.Find("VillageKit");
            if (!root) return "no kit";
            var cols = root.GetComponentsInChildren<BoxCollider>().Where(c => c.gameObject.name != "Kit_square_floor").ToArray();
            var hits = new List<string>();
            bool Inside(Vector3 p) => cols.Any(c => c.bounds.Contains(new Vector3(p.x, c.bounds.center.y, p.z)));
            foreach (var a in ctx.npcs) if (a && Inside(a.transform.position)) hits.Add("NPC " + a.npcId + " at " + a.transform.position.ToString("F1"));
            foreach (var kv in ctx.spawns) if (Inside(kv.Value)) hits.Add("spawn " + kv.Key + " at " + kv.Value.ToString("F1"));
            foreach (var it in ctx.interactables) if (it && Inside(it.transform.position)) hits.Add("interactable " + it.name + " at " + it.transform.position.ToString("F1"));
            return hits.Count == 0 ? "nothing inside the kit's buildings" : string.Join("; ", hits);
        }
    }
}
