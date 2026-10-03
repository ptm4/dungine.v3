using System.Collections.Generic;
using Dungine.Rules;
using Dungine.Visual;
using UnityEngine;

namespace Dungine.Library
{
    /// <summary>
    /// Phase 3: the library's weapons and held things on library figures.
    ///
    /// Weapons. v2 still decides what a figure carries (GearLook), where it hangs (hand, back or hip, drawn or sheathed)
    /// and how the arms hold it (its stances, clips and two-handed IK). Only the look changes: the procedural mesh v2
    /// builds is hidden and the library's model of the same weapon takes its place, with the library weapon's grip on
    /// v2's grip point and its business end along v2's +Y.
    ///
    /// The library's standalone weapons stand upright (kit +z along the weapon) with the weapon's frame origin
    /// round(-a0 / 1.5) + 1.5 voxels above the bottom of the model (kit/weapons.py, item_review.build_item); a hand holds
    /// it 'handA' centimetres along the weapon from that origin.
    ///
    /// Held things (a cup, a jug, sacks) follow the people's briefs and sit at the library's own hand sockets, kept upright.
    /// </summary>
    public static class LibraryProps
    {
        public const string Folder = "LookTest/Baked/";   // the look test bakes library GLBs into prefabs here

        struct Wpn { public string id; public float a0, handA; public Wpn(string i, float a, float h) { id = i; a0 = a; handA = h; } }

        // a0: the start of the weapon along its own axis (kit/weapons.py EXTENT, cm); handA: where v2's main hand holds it
        static readonly Dictionary<WeaponVisual, Wpn> Weapons = new Dictionary<WeaponVisual, Wpn>
        {
            [WeaponVisual.Longsword] = new Wpn("longsword", -22, -7),
            [WeaponVisual.Greatsword] = new Wpn("greatsword", -30, -8),
            [WeaponVisual.Rapier] = new Wpn("rapier", -17, -7),
            [WeaponVisual.Scimitar] = new Wpn("scimitar", -16, -7),
            [WeaponVisual.Dagger] = new Wpn("dagger", -13, -5),
            [WeaponVisual.Mace] = new Wpn("mace", -18, -9),
            [WeaponVisual.Quarterstaff] = new Wpn("quarterstaff", -93, -38),
            [WeaponVisual.Staff] = new Wpn("quarterstaff", -93, -38),
            [WeaponVisual.Longbow] = new Wpn("longbow", -100, 0),
        };

        /// <summary>The weapon's grip in the baked model's own space (Unity: kit x mirrored, kit z up).</summary>
        static Vector3 Grip(Wpn w)
        {
            float k = 1f / 1.5f;                                        // voxels per cm at true size
            float zo = Mathf.Round(-w.a0 * k) + 1.5f;                   // the frame origin, in voxels above the bottom
            float z = zo + w.handA * k;
            return new Vector3(-0.5f, z, -0.5f) * 0.015f;               // the centre line runs through x.5, y.5
        }

        public static GameObject Load(string id) => Resources.Load<GameObject>(Folder + id);

        /// <summary>Swaps v2's procedural weapon meshes on this rig for the library's models, where the library has one.</summary>
        public static void Weaponise(HumanoidRig rig)
        {
            if (rig == null || rig.gear == null) return;
            Swap(rig, rig.mainWeapon, rig.gear.main);
            Swap(rig, rig.offWeapon, rig.gear.off);
        }

        static void Swap(HumanoidRig rig, GameObject weapon, WeaponVisual kind)
        {
            if (!weapon || !Weapons.TryGetValue(kind, out var w)) return;
            var prefab = Load(w.id);
            if (!prefab) return;
            foreach (var r in weapon.GetComponentsInChildren<Renderer>(true)) { r.enabled = false; rig.renderers.Remove(r); }
            var m = Object.Instantiate(prefab, weapon.transform, false);
            m.name = "Library_" + w.id;
            m.transform.localPosition = -Grip(w);
            m.transform.localRotation = Quaternion.identity;
            foreach (var r in m.GetComponentsInChildren<Renderer>()) { rig.renderers.Add(r); r.gameObject.layer = weapon.layer; }
        }

        // ---------------------------------------------------------------- held things from the briefs
        struct Held { public string item, socket; public Vector3 offset; public Held(string i, string s, Vector3 o) { item = i; socket = s; offset = o; } }

        static readonly Dictionary<string, Held[]> HeldBy = new Dictionary<string, Held[]>
        {
            ["arik"] = new[] { new Held("ale_mug", "SocketHandR", new Vector3(0, -0.06f, 0.03f)) },                     // a cup in one hand
            ["alenka"] = new[] { new Held("jug_or_pitcher", "SocketHandL", new Vector3(0, -0.12f, 0.02f)) },           // a wine jug on her hip
            ["parriwimple"] = new[] { new Held("sack", "LeftUpperArm", new Vector3(0, 0.05f, -0.02f)),                 // a sack on each shoulder
                                      new Held("sack", "RightUpperArm", new Vector3(0, 0.05f, -0.02f)) },
        };

        /// <summary>Puts the brief's held things in a library figure's hands (or on its shoulders).</summary>
        public static void Hold(HumanoidRig rig, string id, Dictionary<string, Transform> byName)
        {
            if (!HeldBy.TryGetValue(id, out var list)) return;
            foreach (var h in list)
            {
                var prefab = Load(h.item);
                if (!prefab || !byName.TryGetValue(h.socket, out var at)) continue;
                var m = Object.Instantiate(prefab, at, false);
                m.name = "Held_" + h.item;
                m.transform.localPosition = h.offset;
                m.AddComponent<KeepUpright>();
                foreach (var r in m.GetComponentsInChildren<Renderer>()) rig.renderers.Add(r);
            }
        }
    }

    /// <summary>A held cup, jug or sack stays upright, turning only with the figure (runs after the springs).</summary>
    [DefaultExecutionOrder(200)]
    public class KeepUpright : MonoBehaviour
    {
        Transform root;
        void Start() { var rig = GetComponentInParent<HumanoidRig>(); root = rig ? rig.transform : transform.root; }
        void LateUpdate() { if (root) transform.rotation = Quaternion.Euler(0, root.eulerAngles.y, 0); }
    }
}
