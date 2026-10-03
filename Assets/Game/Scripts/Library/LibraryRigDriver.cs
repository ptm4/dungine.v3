using System.Collections.Generic;
using Dungine.Visual;
using UnityEngine;

namespace Dungine.Library
{
    /// <summary>
    /// Runs after v2's HumanoidAnimator (execution order 100) on a library figure, and adds what v2's skeleton lacks:
    ///   1. UpperChest: the library has three trunk bones where v2 has two, so v2's Chest turn is shared half and half
    ///      between Chest and UpperChest (the same total turn at the shoulders, bent over a longer stretch of back).
    ///   2. The figure's own pose (base_pose in the .rig.json: a bowed head, a stoop), laid over v2's motion on the trunk,
    ///      neck and head only, so the walk and the arms stay v2's.
    ///   3. Spring chains: skirts, capes, long hair and tails swing, following the VRM spring-bone rules the library
    ///      writes (stiffness, dragForce, gravityPower, gravityDir, hitRadius) and pushed out of the sphere and capsule
    ///      colliders on the body.
    /// </summary>
    [DefaultExecutionOrder(150)]
    public class LibraryRigDriver : MonoBehaviour
    {
        public HumanoidRig rig;
        public Transform upperChest;
        /// <summary>When set, nothing runs on its own: call Tick after the animator's Tick (dev captures).</summary>
        public bool manual;
        public bool springs = true, basePose = true;
        /// <summary>Dev switches for every library figure (tools/ev.sh).</summary>
        public static bool SpringsOn = true, PoseOn = true, ChestShareOn = true;
        public string id;

        static readonly string[] PostureBones = { "Spine", "Chest", "UpperChest", "Neck", "Head" };
        static readonly string[] ArmBones = { "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand", "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand" };
        readonly List<(Transform t, Quaternion q)> posture = new List<(Transform, Quaternion)>();
        /// <summary>The brief's arm hold (arms folded, a cup raised, hands clasped): taken up whenever the figure stands still,
        /// out of combat; v2's own arm motion takes over as soon as it walks or fights.</summary>
        readonly List<(Transform t, Quaternion q)> armHold = new List<(Transform, Quaternion)>();
        float holdW, speedS; Vector3 lastHoldPos; bool holdStarted;
        HumanoidAnimator ha; Actor actor;

        class Collider { public Transform node; public Vector3 a, b; public float r; public bool capsule; }
        class Joint
        {
            public Transform t; public Quaternion restLocal; public Vector3 axis; public float length;
            public float stiffness, drag, gravity, hit; public Vector3 gravityDir;
            public Vector3 cur, prev; public List<Collider> cols;   // cur, prev: in the figure's own space (see Springs)
            public Vector3 TailLocal;
            /// <summary>For a skirt panel's top joint: how far its rest hangs from the hips rather than from its thigh.</summary>
            public float followHips; public Transform hips;
            /// <summary>Rest position, under the parent bone and under the hips (a skirt joint is placed between the two).</summary>
            public Vector3 restLocalPos, restHipsPos;
        }
        /// <summary>Skirt panels hang between the thigh and the hips, so cloth spanning both legs doesn't split at each step.</summary>
        public static float SkirtFrontBackFollow = 0.7f, SkirtSideFollow = 0.4f;
        readonly List<Joint> joints = new List<Joint>();
        Vector3 lastPos; bool started;

        public void Init(HumanoidRig r, Transform upper, object data, Dictionary<string, Transform> byName)
        {
            rig = r; upperChest = upper; id = MiniJson.Str(data, "id");
            Transform T(string n) => n != null && byName.TryGetValue(n, out var t) ? t : null;

            // the figure's own pose, trunk and head only
            var rots = MiniJson.Obj(MiniJson.Obj(data, "base_pose"), "rotations");
            if (rots != null)
            {
                foreach (var n in PostureBones)
                    if (rots.ContainsKey(n) && T(n)) posture.Add((T(n), LibraryFigures.FromGltfRot(MiniJson.Floats(rots, n))));
                foreach (var n in ArmBones)
                    if (rots.ContainsKey(n) && T(n)) armHold.Add((T(n), LibraryFigures.FromGltfRot(MiniJson.Floats(rots, n))));
            }

            // colliders, by group
            var cols = new Dictionary<string, Collider>();
            foreach (var c in MiniJson.Arr(data, "colliders") ?? new List<object>())
            {
                var node = T(MiniJson.Str(c, "node")); if (!node) continue;
                var shape = MiniJson.Obj(c, "shape");
                var cap = MiniJson.Obj(shape, "capsule"); var sph = MiniJson.Obj(shape, "sphere");
                var src = cap ?? sph; if (src == null) continue;
                cols[MiniJson.Str(c, "name")] = new Collider
                {
                    node = node, capsule = cap != null, r = MiniJson.Num(src, "radius"),
                    a = LibraryFigures.FromGltf(MiniJson.Floats(src, "offset")),
                    b = cap != null ? LibraryFigures.FromGltf(MiniJson.Floats(src, "tail")) : Vector3.zero,
                };
            }
            var groups = new Dictionary<string, List<Collider>>();
            foreach (var g in MiniJson.Arr(data, "colliderGroups") ?? new List<object>())
            {
                var list = new List<Collider>();
                foreach (var n in MiniJson.Arr(g, "colliders") ?? new List<object>()) if (n is string s && cols.TryGetValue(s, out var cc)) list.Add(cc);
                groups[MiniJson.Str(g, "name")] = list;
            }

            // chains: each joint aims at the next joint, the last at the chain's tip
            foreach (var ch in MiniJson.Arr(data, "chains") ?? new List<object>())
            {
                var names = MiniJson.Arr(ch, "bones"); var js = MiniJson.Arr(ch, "joints");
                if (names == null || js == null) continue;
                var chainCols = new List<Collider>();
                foreach (var gname in MiniJson.Arr(ch, "colliderGroups") ?? new List<object>())
                    if (gname is string s && groups.TryGetValue(s, out var gl)) chainCols.AddRange(gl);
                var tipLocal = LibraryFigures.FromGltf(MiniJson.Floats(ch, "tip_local"));
                string kind = MiniJson.Str(ch, "kind") ?? "", chainName = MiniJson.Str(ch, "name") ?? "";
                float follow = kind == "skirt" ? (chainName.Contains("Side") ? SkirtSideFollow : SkirtFrontBackFollow) : 0f;
                for (int k = 0; k < names.Count; k++)
                {
                    var t = T(names[k] as string); if (!t) continue;
                    var jd = k < js.Count ? js[k] : null;
                    Vector3 tail = k + 1 < names.Count && T(names[k + 1] as string) ? T(names[k + 1] as string).localPosition : tipLocal;
                    if (tail.sqrMagnitude < 1e-8f) tail = new Vector3(0, -0.1f, 0);
                    var gd = MiniJson.Floats(jd, "gravityDir");
                    joints.Add(new Joint
                    {
                        t = t, restLocal = t.localRotation, TailLocal = tail, axis = tail.normalized, length = tail.magnitude,
                        stiffness = MiniJson.Num(jd, "stiffness", 1f), drag = MiniJson.Num(jd, "dragForce", 0.4f),
                        gravity = MiniJson.Num(jd, "gravityPower", 0f), hit = MiniJson.Num(jd, "hitRadius", 0.02f),
                        gravityDir = gd != null ? LibraryFigures.FromGltf(gd).normalized : Vector3.down, cols = chainCols,
                        followHips = k == 0 ? follow : 0f, hips = T("Hips"), restLocalPos = t.localPosition,
                        restHipsPos = T("Hips") ? T("Hips").InverseTransformPoint(t.position) : Vector3.zero,
                    });
                }
            }
        }

        public int ChainJoints => joints.Count;

        void LateUpdate() { if (!manual) Tick(Time.deltaTime); }

        public void Tick(float dt)
        {
            if (!rig) return;
            // 1. share v2's chest turn with UpperChest
            var chest = rig[B.Chest];
            if (chest && upperChest && ChestShareOn)
            {
                var half = Quaternion.Slerp(Quaternion.identity, chest.localRotation, 0.5f);
                chest.localRotation = half;
                upperChest.localRotation = half;
            }
            // 2. the figure's own posture over v2's motion, and its arm hold while it stands still
            if (basePose && PoseOn) foreach (var (t, q) in posture) t.localRotation = t.localRotation * q;
            if (basePose && PoseOn && armHold.Count > 0)
            {
                if (!ha) ha = GetComponent<HumanoidAnimator>();
                if (!actor) actor = GetComponent<Actor>();
                var p = transform.position;
                float sp = holdStarted && dt > 0 ? new Vector2(p.x - lastHoldPos.x, p.z - lastHoldPos.z).magnitude / dt : 0;
                holdStarted = true; lastHoldPos = p;
                speedS = Mathf.Lerp(speedS, sp, 1 - Mathf.Exp(-dt * 8f));
                bool busy = (ha && (ha.Busy || ha.InStance)) || (actor && (actor.c == null || !actor.c.Active || (Combat.CombatManager.I && Combat.CombatManager.I.InCombat(actor.c))));
                float target = busy ? 0 : 1 - Mathf.Clamp01((speedS - 0.1f) / 0.4f);
                holdW = Mathf.MoveTowards(holdW, target, dt * 3f);
                if (holdW > 0.001f) foreach (var (t, q) in armHold) t.localRotation = Quaternion.Slerp(t.localRotation, q, holdW);
            }
            // 3. springs
            if (springs && SpringsOn && joints.Count > 0) Springs(dt);
            // 4. the arms may have moved since the animator placed a weapon for a cast
            if (!ha) ha = GetComponent<HumanoidAnimator>();
            if (ha) ha.HoldForCast();
        }

        void Springs(float dt)
        {
            dt = Mathf.Clamp(dt, 1e-4f, 1f / 30f);
            // The tails are simulated in the figure's own space (VRM's "center"), so walking across the world doesn't drag
            // the cloth behind like a flag; turning, bobbing and the legs' swing still move it.
            var space = transform;
            bool reset = !started || (transform.position - lastPos).sqrMagnitude > 1f;   // first frame or a teleport
            started = true; lastPos = transform.position;
            foreach (var j in joints)
            {
                // the joint's rest orientation under wherever its parent is now (a skirt's top joint: part way to the hips)
                var parentRot = j.t.parent ? j.t.parent.rotation : Quaternion.identity;
                if (!ha) ha = GetComponent<HumanoidAnimator>();
                float follow = ha && ha.seated ? 0f : j.followHips;   // seated, the cloth lies on the thighs
                if (follow > 0 && j.hips && j.t.parent != j.hips)
                {
                    var hipsRot = j.hips.rotation;   // every bone rests unrotated, so the hips' turn is the rest frame
                    parentRot = Quaternion.Slerp(parentRot, hipsRot, follow);
                }
                var restRot = parentRot * j.restLocal;
                if (j.t.parent)
                    j.t.position = follow > 0 && j.hips ? Vector3.Lerp(j.t.parent.TransformPoint(j.restLocalPos), j.hips.TransformPoint(j.restHipsPos), follow)
                                                       : j.t.parent.TransformPoint(j.restLocalPos);
                j.t.rotation = restRot;
                var head = j.t.position;
                var restDir = restRot * j.axis;
                float len = j.length * j.t.lossyScale.y;
                if (reset) { j.cur = j.prev = space.InverseTransformPoint(head + restDir * len); }
                Vector3 cur = space.TransformPoint(j.cur), prev = space.TransformPoint(j.prev);
                var next = cur + (cur - prev) * (1f - j.drag)
                           + restDir * (j.stiffness * dt)
                           + j.gravityDir * (j.gravity * dt);
                next = head + (next - head).normalized * len;
                foreach (var c in j.cols) next = PushOut(c, next, j.hit, head, len);
                j.prev = j.cur; j.cur = space.InverseTransformPoint(next);
                j.t.rotation = Quaternion.FromToRotation(restDir, next - head) * restRot;
            }
        }

        static Vector3 PushOut(Collider c, Vector3 p, float hit, Vector3 head, float len)
        {
            Vector3 a = c.node.TransformPoint(c.a);
            Vector3 centre = a;
            if (c.capsule)
            {
                Vector3 b = c.node.TransformPoint(c.b), ab = b - a;
                float s = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                centre = a + ab * s;
            }
            float r = c.r * c.node.lossyScale.y + hit;
            Vector3 d = p - centre;
            if (d.sqrMagnitude >= r * r || d.sqrMagnitude < 1e-10f) return p;
            p = centre + d.normalized * r;
            return head + (p - head).normalized * len;
        }

        /// <summary>For the dev tools: how far, in degrees, the chains hang away from their rest.</summary>
        public string SpringReport()
        {
            float max = 0, sum = 0;
            foreach (var j in joints)
            {
                float a = Quaternion.Angle(j.t.localRotation, j.restLocal);
                max = Mathf.Max(max, a); sum += a;
            }
            return $"{id}: {joints.Count} spring joints, mean {(joints.Count > 0 ? sum / joints.Count : 0):F1} deg off rest, max {max:F1} deg";
        }
    }
}
