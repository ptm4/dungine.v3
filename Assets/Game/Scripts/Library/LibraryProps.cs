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
            [WeaponVisual.Glaive] = new Wpn("glaive", -136, -80),   // the right hand 56 cm above the butt, as the quarterstaff
        };

        /// <summary>The weapon's grip in the baked model's own space (Unity: kit x mirrored, kit z up).</summary>
        static Vector3 Grip(Wpn w)
        {
            float k = 1f / 1.5f;                                        // voxels per cm at true size
            float zo = Mathf.Round(-w.a0 * k) + 1.5f;                   // the frame origin, in voxels above the bottom
            float z = zo + w.handA * k;
            return new Vector3(-0.5f, z, -0.5f) * 0.015f;               // the centre line runs through x.5, y.5
        }

        /// <summary>Dev: keep v2's own weapon mesh showing beside the library's, to compare how they sit in the hand.</summary>
        public static bool ShowV2Too;

        public static GameObject Load(string id) => Resources.Load<GameObject>(Folder + id);

        /// <summary>Swaps v2's procedural weapon meshes on this rig for the library's models, where the library has one.
        /// When the figure's own body already wears a sheathed weapon (a sword at the hip, a greatsword on the back: the
        /// rig's parts), v2's copy is shown only while drawn, so a sheathed sword isn't there twice.</summary>
        public static void Weaponise(HumanoidRig rig, object rigData = null)
        {
            if (rig == null || rig.gear == null) return;
            Swap(rig, rig.mainWeapon, rig.gear.main);
            Swap(rig, rig.offWeapon, rig.gear.off);
            if (rig.mainWeapon && rig.gear.main != WeaponVisual.Shield && WearsSheathedWeapon(rigData))
                rig.mainWeapon.AddComponent<ShowWhenDrawn>().rig = rig;
        }

        static readonly string[] WeaponWords = { "sword", "axe", "mace", "dagger", "rapier", "scimitar", "hammer", "club", "bow", "staff", "spear" };

        static bool WearsSheathedWeapon(object rigData)
        {
            foreach (var p in MiniJson.Arr(rigData, "parts") ?? new List<object>())
            {
                var n = MiniJson.Str(p, "part") ?? "";
                if (!(n.EndsWith("_hip") || n.EndsWith("_back"))) continue;
                foreach (var w in WeaponWords) if (n.Contains(w)) return true;
            }
            return false;
        }

        static void Swap(HumanoidRig rig, GameObject weapon, WeaponVisual kind)
        {
            if (!weapon || !Weapons.TryGetValue(kind, out var w)) return;
            // agent N's game export where it has the weapon (the glaive), else the look test's bake
            var exp = Resources.Load<GameObject>("LookTest/Export/" + w.id);
            var prefab = exp ? exp : Load(w.id);
            if (!prefab) return;
            if (!ShowV2Too) foreach (var r in weapon.GetComponentsInChildren<Renderer>(true)) { r.enabled = false; rig.renderers.Remove(r); }
            var m = Object.Instantiate(prefab, weapon.transform, false);
            m.name = "Library_" + w.id;
            m.transform.localRotation = Quaternion.identity;
            if (exp)
            {
                // an export stands on its butt (the glaive 6 cm up, as in the library): the grip is (handA - a0) cm above
                // the model's bottom, on its middle line
                LibraryFigures.PrepareStatic(m, null);
                m.transform.localPosition = Vector3.zero;
                Bounds b = default; bool any = false;
                foreach (var r in m.GetComponentsInChildren<Renderer>(true))
                    if (r.name == "Body_LOD0" || !r.name.StartsWith("Body_LOD")) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                if (any)
                {
                    var bottom = m.transform.InverseTransformPoint(new Vector3(b.center.x, b.min.y, b.center.z));
                    var mid = m.transform.InverseTransformPoint(b.center);
                    m.transform.localPosition = -new Vector3(mid.x, bottom.y + (w.handA - w.a0) / 100f, mid.z);
                }
            }
            else m.transform.localPosition = -Grip(w);
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

    /// <summary>A weapon the figure's body already wears sheathed: v2's copy shows only while it is drawn.</summary>
    public class ShowWhenDrawn : MonoBehaviour
    {
        public HumanoidRig rig;
        bool? shown;
        void LateUpdate()
        {
            bool on = rig && rig.gear != null && rig.gear.drawn;
            if (shown == on) return;
            shown = on;
            // the library's model when Weaponise swapped one in (v2's own meshes stay off), else v2's meshes
            var rs = GetComponentsInChildren<Renderer>(true);
            bool swapped = false;
            foreach (var r in rs) if (IsLibrary(r)) swapped = true;
            foreach (var r in rs) if (!swapped || IsLibrary(r)) r.enabled = on;
        }
        bool IsLibrary(Renderer r) { for (var t = r.transform; t && t != transform; t = t.parent) if (t.name.StartsWith("Library_")) return true; return false; }
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
