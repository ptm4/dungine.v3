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
            // sockets: the library's names, which match v2's (LookFrom is v2's Eyes)
            rig.socketHandR = T("SocketHandR"); rig.socketHandL = T("SocketHandL"); rig.socketShield = T("SocketShield");
            rig.socketBack = T("SocketBack"); rig.socketHipL = T("SocketHipL"); rig.socketHipR = T("SocketHipR");
            rig.headTop = T("HeadTop"); rig.eyes = T("LookFrom");

            // size, from the rest pose
            float top = rig.headTop ? rig.headTop.position.y - go.transform.position.y : 1.8f;
            var head = rig[B.Head];
            rig.height = top;
            rig.headSize = head ? Mathf.Max(0.12f, top - (head.position.y - go.transform.position.y)) : top / 7.4f;
            rig.scale = top / 1.78f;
            rig.look = new Appearance { race = Rules.RaceId.Human };
            rig.gear = gear ?? new GearLook();

            var smr = inst.GetComponentInChildren<SkinnedMeshRenderer>(true);
            rig.body = smr;
            if (smr)
            {
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                smr.updateWhenOffscreen = false;
                // room for crouches, falls and swinging cloth, so the figure isn't culled early
                var b = smr.localBounds; b.Expand(new Vector3(1.2f, 1.2f, 1.2f)); smr.localBounds = b;
                rig.renderers.Add(smr);
            }
            rig.hasTail = false; rig.hasCape = false;   // the library's tails and capes are spring chains, not v2's keyed bones

            var drv = go.AddComponent<LibraryRigDriver>();
            drv.Init(rig, T("UpperChest"), data, byName);
            HumanoidBuilder.ApplyGear(rig, rig.gear);
            return rig;
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
