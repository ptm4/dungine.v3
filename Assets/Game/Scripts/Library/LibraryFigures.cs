using System.Collections.Generic;
using System.Linq;
using Dungine.Visual;
using UnityEngine;

namespace Dungine.Library
{
    /// <summary>
    /// Phase 3 (agent B's step G2): builds a v2 <see cref="HumanoidRig"/> from a rigged library figure, so v2's own
    /// <see cref="HumanoidAnimator"/> drives it unchanged.
    ///
    /// The rigged figures come from the library's game export (pixel3d rig_export.py): a skinned GLB whose bones carry
    /// Unity's and Godot's humanoid names and rest with no rotation (the kit's hang stance), plus a .rig.json with the
    /// spring chains, colliders, sockets and the figure's own pose. Copies live in Resources/Library/Rigs/&lt;id&gt;.glb and
    /// &lt;id&gt;.rig.json; glTFast imports the GLB with its skin.
    ///
    /// glTF is right-handed with the figure's left at +X; glTFast mirrors X, so in Unity the figure faces +Z with its left
    /// at -X, which is exactly v2's convention. Vectors read from the .rig.json are mirrored the same way here.
    /// </summary>
    public static class LibraryFigures
    {
        public const string Folder = "Library/Rigs/";

        /// <summary>v2 figures to replace, by the id an area gives them (AreaContext.NPC / Monster id) -> library figure id.</summary>
        public static readonly Dictionary<string, string> ByNpcId = new Dictionary<string, string>();

        /// <summary>v2 bone -> library bone. Tail and cape bones stay unmapped: the library's chains are run by springs.</summary>
        static readonly Dictionary<B, string> Names = new Dictionary<B, string>
        {
            [B.Root] = "Root", [B.Hips] = "Hips", [B.Spine] = "Spine", [B.Chest] = "Chest", [B.Neck] = "Neck", [B.Head] = "Head",
            [B.ClavL] = "LeftShoulder", [B.UpperArmL] = "LeftUpperArm", [B.LowerArmL] = "LeftLowerArm", [B.HandL] = "LeftHand",
            [B.ClavR] = "RightShoulder", [B.UpperArmR] = "RightUpperArm", [B.LowerArmR] = "RightLowerArm", [B.HandR] = "RightHand",
            [B.ThighL] = "LeftUpperLeg", [B.ShinL] = "LeftLowerLeg", [B.FootL] = "LeftFoot", [B.ToeL] = "LeftToes",
            [B.ThighR] = "RightUpperLeg", [B.ShinR] = "RightLowerLeg", [B.FootR] = "RightFoot", [B.ToeR] = "RightToes",
        };

        /// <summary>Chapter One's named people, as v2 places them, become their library figures (review 9). Only the
        /// people as the party first meets them: v2's other forms of anyone stay as they are.</summary>
        public static void UseChapterOne()
        {
            foreach (var k in new[] { "arik" }) ByNpcId[k] = "arik";
            foreach (var k in new[] { "ismark", "ismark_home", "ismark_g" }) ByNpcId[k] = "ismark_kolyanovich";
            foreach (var k in new[] { "ireena", "ireena_g", "ireena_f" }) ByNpcId[k] = "ireena_kolyana";
            foreach (var k in new[] { "donavich", "donavich_g" }) ByNpcId[k] = "donavich";
            ByNpcId["vistani0"] = "alenka"; ByNpcId["vistani1"] = "mirabel"; ByNpcId["vistani2"] = "sorvia";
            ByNpcId["bildrath"] = "bildrath_cantemir"; ByNpcId["parriwimple"] = "parriwimple";
            ByNpcId["rose"] = "rose_durst"; ByNpcId["thorn"] = "thorn_durst";
        }

        public static bool Exists(string id) => Resources.Load<TextAsset>(Folder + id + ".rig") != null;

        /// <summary>The library figure assigned to this v2 figure, built and ready for HumanoidAnimator; null if none.</summary>
        public static HumanoidRig TryBuild(string npcId, GearLook gear, string name)
        {
            if (string.IsNullOrEmpty(npcId) || !ByNpcId.TryGetValue(npcId, out var id)) return null;
            return Build(id, gear, name);
        }

        public static Vector3 FromGltf(float[] v) => v == null || v.Length < 3 ? Vector3.zero : new Vector3(-v[0], v[1], v[2]);
        public static Quaternion FromGltfRot(float[] q) => q == null || q.Length < 4 ? Quaternion.identity : new Quaternion(q[0], -q[1], -q[2], q[3]);

        public static HumanoidRig Build(string id, GearLook gear, string name = null)
        {
            var prefab = Resources.Load<GameObject>(Folder + id);
            var json = Resources.Load<TextAsset>(Folder + id + ".rig");
            if (!prefab || !json) { Debug.LogWarning($"[Library] no rigged figure '{id}'"); return null; }
            var data = MiniJson.Parse(json.text);

            var go = new GameObject(name ?? id);
            var rig = go.AddComponent<HumanoidRig>();
            var model = new GameObject("Model").transform;
            model.SetParent(go.transform, false);
            var inst = Object.Instantiate(prefab, model, false);
            inst.name = id;
            var byName = new Dictionary<string, Transform>();
            foreach (var t in inst.GetComponentsInChildren<Transform>(true)) if (!byName.ContainsKey(t.name)) byName[t.name] = t;
            Transform T(string n) => n != null && byName.TryGetValue(n, out var t) ? t : null;

            // bones and their rest positions (identity rotations at rest, as v2's builder makes them)
            foreach (var kv in Names)
            {
                var t = T(kv.Value);
                rig.bones[(int)kv.Key] = t;
                if (t) { rig.bindPos[(int)kv.Key] = t.localPosition; t.localRotation = Quaternion.identity; }
            }
            // sockets: where the library puts them on this body, turned the way v2's weapons and clips expect. (The
            // library's hand sockets hang a weapon down along the arm, its 'hang' grip; v2's point it forward from the fist,
            // and v2's stances and strikes are keyed for that.) The library's own sockets stay for held things.
            Transform V2Socket(string lib, B bone, string parentName, Quaternion rot)
            {
                var src = T(lib); var parent = T(parentName) ?? rig[bone];
                if (!parent) return src;
                var s = new GameObject("v2" + lib).transform;
                s.SetParent(parent, false);
                s.position = src ? src.position : parent.position;
                s.localRotation = rot;
                return s;
            }
            rig.socketHandR = V2Socket("SocketHandR", B.HandR, null, Quaternion.Euler(90, 0, 0));
            rig.socketHandL = V2Socket("SocketHandL", B.HandL, null, Quaternion.Euler(90, 0, 0));
            rig.socketShield = V2Socket("SocketShield", B.LowerArmL, null, Quaternion.Euler(0, 0, 90) * Quaternion.Euler(0, 90, 0));
            rig.socketBack = V2Socket("SocketBack", B.Chest, "UpperChest", Quaternion.Euler(0, 0, 35) * Quaternion.Euler(0, 180, 0));
            rig.socketHipL = V2Socket("SocketHipL", B.Hips, null, Quaternion.Euler(-160, 0, 8));
            rig.socketHipR = V2Socket("SocketHipR", B.Hips, null, Quaternion.Euler(-160, 0, -8));
            rig.headTop = T("HeadTop"); rig.eyes = T("LookFrom");

            // size, from the rest pose
            float top = rig.headTop ? rig.headTop.position.y - go.transform.position.y : 1.8f;
            var head = rig[B.Head];
            rig.height = top;
            rig.headSize = head ? Mathf.Max(0.12f, top - (head.position.y - go.transform.position.y)) : top / 7.4f;
            rig.scale = top / 1.78f;
            rig.look = new Appearance { race = Rules.RaceId.Human };
            rig.gear = gear ?? new GearLook();

            // the game export: Body_LOD0..2 (1.5, 3 and 6 cm voxels) on one skin, in a LODGroup; older rigs have one mesh
            var smrs = inst.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var lodMeshes = smrs.Where(s => s.name.StartsWith("Body_LOD")).OrderBy(s => s.name).ToArray();
            rig.body = lodMeshes.Length > 0 ? lodMeshes[0] : smrs.FirstOrDefault();
            foreach (var smr in smrs)
            {
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                smr.updateWhenOffscreen = false;
                // room for crouches, falls and swinging cloth, so the figure isn't culled early
                var b = smr.localBounds; b.Expand(new Vector3(1.2f, 1.2f, 1.2f)); smr.localBounds = b;
                var mats = smr.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = VoxelMaterial(mats[i]);
                smr.sharedMaterials = mats;
                rig.renderers.Add(smr);
            }
            if (lodMeshes.Length > 1) SetUpLods(inst, lodMeshes, MiniJson.Obj(data, "game"), top);
            rig.hasTail = false; rig.hasCape = false;   // the library's tails and capes are spring chains, not v2's keyed bones

            var drv = go.AddComponent<LibraryRigDriver>();
            drv.Init(rig, T("UpperChest"), data, byName);
            HumanoidBuilder.ApplyGear(rig, rig.gear);
            LibraryProps.Weaponise(rig, data);
            LibraryProps.Hold(rig, id, byName);
            return rig;
        }

        // ------------------------------------------------------------------ the export's LODs and material

        /// <summary>Dev switches: LODs on (else LOD0 always), the export's switch heights scaled, a LOD forced (-1: none),
        /// and the Dungine/VoxelAtlas shader (else glTFast's own material).</summary>
        public static bool LodsOn = true, AtlasShaderOn = true;
        public static float LodScale = 1f;
        public static int ForceLod = -1;
        /// <summary>Below this share of the screen's height a figure isn't drawn at all.</summary>
        public const float CullHeight = 0.002f;

        static void SetUpLods(GameObject inst, SkinnedMeshRenderer[] meshes, object game, float height)
        {
            // the export suggests switching when the figure fills less than 25% and then 8% of the screen's height
            var heights = new List<float>();
            foreach (var l in MiniJson.Arr(game, "lods") ?? new List<object>()) heights.Add(MiniJson.Num(l, "screen_height_min", 0f));
            if (heights.Count < meshes.Length) heights = new List<float> { 0.25f, 0.08f, 0f };
            if (!LodsOn) { for (int i = 1; i < meshes.Length; i++) meshes[i].enabled = false; return; }
            var lods = new LOD[meshes.Length];
            float prev = 1f;
            for (int i = 0; i < meshes.Length; i++)
            {
                float h = i == meshes.Length - 1 ? CullHeight : Mathf.Clamp(heights[i] * LodScale, CullHeight * 2, 0.99f);
                h = Mathf.Min(h, prev * 0.99f); prev = h;
                lods[i] = new LOD(h, new Renderer[] { meshes[i] });
            }
            var lg = inst.AddComponent<LODGroup>();
            lg.fadeMode = LODFadeMode.None;
            lg.SetLODs(lods);
            // measured against the figure's height, not its renderers' bounds (those are padded for swinging cloth)
            lg.localReferencePoint = new Vector3(0, height * 0.5f, 0);
            lg.size = height;
            if (ForceLod >= 0) lg.ForceLOD(Mathf.Min(ForceLod, meshes.Length - 1));
        }

        /// <summary>The dials' starting values (2026-10-03): a wrap of 0.6, 25% more ambient and the AO at 0.7 soften the dark
        /// sides of the voxel steps without flattening the blocks. Lambert, plain ambient and full AO are 0, 1 and 1.</summary>
        public static float DefaultWrap = 0.6f, DefaultFill = 1.25f, DefaultAO = 0.7f;
        static bool dialsSet;
        static Shader atlasShader;
        static readonly Dictionary<Material, Material> atlasMats = new Dictionary<Material, Material>();

        /// <summary>glTFast's material for an export model, as a Dungine/VoxelAtlas material (one per source material).</summary>
        public static Material VoxelMaterial(Material src)
        {
            if (!AtlasShaderOn || !src || !src.HasProperty("baseColorTexture")) return src;
            if (atlasMats.TryGetValue(src, out var m) && m) return m;
            if (!atlasShader) atlasShader = Shader.Find("Dungine/VoxelAtlas");
            if (!atlasShader) return src;
            if (!dialsSet) { Dials(DefaultWrap, DefaultFill, DefaultAO); dialsSet = true; }
            m = new Material(atlasShader) { name = src.name + " (Dungine)" };
            var tex = src.GetTexture("baseColorTexture");
            if (tex) m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", src.GetColor("baseColorFactor"));
            var mr = src.HasProperty("metallicRoughnessTexture") ? src.GetTexture("metallicRoughnessTexture") : null;
            if (mr) m.SetTexture("_MetallicGlossMap", mr);
            m.SetFloat("_Metallic", src.HasProperty("metallicFactor") ? src.GetFloat("metallicFactor") : 1f);
            m.SetFloat("_Smoothness", 1f - (src.HasProperty("roughnessFactor") ? src.GetFloat("roughnessFactor") : 1f));
            var glow = src.HasProperty("emissiveTexture") ? src.GetTexture("emissiveTexture") : null;
            m.SetTexture("_EmissionMap", glow ? glow : Texture2D.blackTexture);
            m.SetColor("_SpecColor", glow && src.HasProperty("emissiveFactor") ? src.GetColor("emissiveFactor") : Color.black);
            m.SetColor("_EmissionColor", Color.black);
            m.enableInstancing = true;
            atlasMats[src] = m;
            return m;
        }

        /// <summary>The shader's global dials (see DungineVoxelAtlas.shader): wrap diffuse, ambient fill, AO strength.</summary>
        public static void Dials(float wrap, float fill, float aoPower)
        {
            Shader.SetGlobalFloat("_VoxWrap", wrap);
            Shader.SetGlobalFloat("_VoxFill", fill);
            Shader.SetGlobalFloat("_VoxAOPower", aoPower);
        }

        /// <summary>The rest stance's arm angles, for checking against v2's (arms 11 degrees out, forearms in line).</summary>
        public static string RestReport(string id)
        {
            var json = Resources.Load<TextAsset>(Folder + id + ".rig");
            if (!json) return id + ": no rig";
            var d = MiniJson.Parse(json.text);
            var joints = MiniJson.Arr(d, "bones").ToDictionary(b => MiniJson.Str(b, "name"), b => FromGltf(MiniJson.Floats(b, "joint")));
            float Angle(string a, string b) { var v = joints[b] - joints[a]; return Mathf.Atan2(Mathf.Abs(v.x), -v.y) * Mathf.Rad2Deg; }
            float arm = Angle("LeftUpperArm", "LeftLowerArm"), fore = Angle("LeftLowerArm", "LeftHand");
            float h = joints.TryGetValue("Head", out var hd) ? hd.y : 0;
            return $"{id}: upper arm {arm:F1} deg out, forearm {fore:F1} deg out, head joint at {h:F2} m";
        }
    }
}
