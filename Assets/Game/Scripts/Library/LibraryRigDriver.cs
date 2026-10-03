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
    public partial class LibraryRigDriver : MonoBehaviour
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

        class Collider
        {
            public Transform node; public Vector3 a, b; public float r; public bool capsule;
            public Vector3 wa, wb; public float wr;   // this frame, in the world (worked out once per frame, not per joint)
        }
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
            /// <summary>The joint's parent when that is a spring joint updated earlier in the same pass (-1: a body bone).</summary>
            public int parentJoint = -1;
            public Quaternion worldRot; public Vector3 head;   // this frame's result, for the joints hanging below it
        }
        readonly List<Collider> allColliders = new List<Collider>();

        /// <summary>Cost control (2026-10-03): springs run every frame while the figure's full-detail LOD shows, every 2nd
        /// frame at the middle LOD, every 4th at the far LOD, and not at all while no LOD is drawn (off screen and casting
        /// no visible shadow). The cloth keeps its last shape meanwhile. Captures (manual) always run every frame.</summary>
        public static bool ThrottleOn = true;
        /// <summary>Dev counters: spring passes run and skipped since the last reset (all library figures).</summary>
        public static int PassesRun, PassesSkipped;
        /// <summary>Dev: main-thread time spent on springs (scheduling or the managed pass, and LibrarySprings' wait and
        /// writes) since ResetCost, in Stopwatch ticks, and the frame it started.</summary>
        public static long SpringTicks, WaitTicks; public static int CostFrom;
        public static void ResetCost() { SpringTicks = WaitTicks = 0; PassesRun = PassesSkipped = 0; CostFrom = Time.frameCount; }
        public static string Cost()
        {
            int frames = Mathf.Max(1, Time.frameCount - CostFrom);
            double ms = SpringTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            double wait = WaitTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            return $"springs: {ms / frames:F3} ms a frame on the main thread ({wait / frames:F3} of it waiting for and writing the jobs) over {frames} frames; passes run {PassesRun}, skipped {PassesSkipped}";
        }
        Renderer[] lodRenderers; float springDt; int springFrame;
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

            // chains: each joint aims at the next joint, the last at the chain's tip.
            // The game export (2026-10-03) hangs cloth spanning both legs on centre chains from Hips and lets the leg panels
            // ride their legs, as designed; only rigs without centre chains need the leg panels pulled toward the hips.
            bool centreChains = false;
            foreach (var ch in MiniJson.Arr(data, "chains") ?? new List<object>())
                if ((MiniJson.Str(ch, "name") ?? "").StartsWith("SkirtCentre")) centreChains = true;
            foreach (var ch in MiniJson.Arr(data, "chains") ?? new List<object>())
            {
                var names = MiniJson.Arr(ch, "bones"); var js = MiniJson.Arr(ch, "joints");
                if (names == null || js == null) continue;
                var chainCols = new List<Collider>();
                foreach (var gname in MiniJson.Arr(ch, "colliderGroups") ?? new List<object>())
                    if (gname is string s && groups.TryGetValue(s, out var gl)) chainCols.AddRange(gl);
                var tipLocal = LibraryFigures.FromGltf(MiniJson.Floats(ch, "tip_local"));
                string kind = MiniJson.Str(ch, "kind") ?? "", chainName = MiniJson.Str(ch, "name") ?? "";
                string parentBone = MiniJson.Str(ch, "parent") ?? "";
                float follow = kind == "skirt" && !centreChains && parentBone.EndsWith("UpperLeg") ? (chainName.Contains("Side") ? SkirtSideFollow : SkirtFrontBackFollow) : 0f;
                for (int k = 0; k < names.Count; k++)
                {
                    var t = T(names[k] as string); if (!t) continue;
                    var jd = k < js.Count ? js[k] : null;
                    Vector3 tail = k + 1 < names.Count && T(names[k + 1] as string) ? T(names[k + 1] as string).localPosition : tipLocal;
                    if (tail.sqrMagnitude < 1e-8f) tail = new Vector3(0, -0.1f, 0);
                    var gd = MiniJson.Floats(jd, "gravityDir");
                    int parentJoint = joints.FindIndex(o => o.t == t.parent);
                    joints.Add(new Joint
                    {
                        t = t, restLocal = t.localRotation, TailLocal = tail, axis = tail.normalized, length = tail.magnitude,
                        stiffness = MiniJson.Num(jd, "stiffness", 1f), drag = MiniJson.Num(jd, "dragForce", 0.4f),
                        gravity = MiniJson.Num(jd, "gravityPower", 0f), hit = MiniJson.Num(jd, "hitRadius", 0.02f),
                        gravityDir = gd != null ? LibraryFigures.FromGltf(gd).normalized : Vector3.down, cols = chainCols,
                        followHips = k == 0 ? follow : 0f, hips = T("Hips"), restLocalPos = t.localPosition,
                        restHipsPos = T("Hips") ? T("Hips").InverseTransformPoint(t.position) : Vector3.zero,
                        parentJoint = parentJoint,
                    });
                }
            }
            foreach (var c in cols.Values) allColliders.Add(c);
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
            // 3. springs, as often as the figure's size on screen needs
            if (springs && SpringsOn && joints.Count > 0)
            {
                int every = manual || !ThrottleOn ? 1 : SpringEvery();
                springDt += dt; springFrame++;
                if (every == 0) { springDt = 0; PassesSkipped++; }
                else if (springFrame % every != 0) PassesSkipped++;
                else
                {
                    long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
                    if (!ScheduleSprings(springDt)) Springs(springDt);
                    SpringTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
                    springDt = 0; PassesRun++;
                }
            }
            // 4. the arms may have moved since the animator placed a weapon for a cast
            if (!ha) ha = GetComponent<HumanoidAnimator>();
            if (ha) ha.HoldForCast();
        }

        /// <summary>1 every frame, 2 or 4 for the middle and far LODs, 0 while nothing of the figure is drawn.</summary>
        int SpringEvery()
        {
            if (lodRenderers == null)
            {
                var list = new List<Renderer>();
                foreach (var r in rig.renderers) if (r && r.name.StartsWith("Body_LOD")) list.Add(r);
                list.Sort((x, y) => string.CompareOrdinal(x.name, y.name));
                if (list.Count == 0 && rig.body) list.Add(rig.body);
                lodRenderers = list.ToArray();
            }
            int[] rates = { 1, 2, 4 };
            for (int i = 0; i < lodRenderers.Length; i++)
                if (lodRenderers[i] && lodRenderers[i].isVisible) return rates[Mathf.Min(i, rates.Length - 1)];
            return lodRenderers.Length == 0 ? 1 : 0;
        }

        void Springs(float dt)
        {
            dt = Mathf.Clamp(dt, 1e-4f, 1f / 20f);
            // The tails are simulated in the figure's own space (VRM's "center"), so walking across the world doesn't drag
            // the cloth behind like a flag; turning, bobbing and the legs' swing still move it.
            var space = transform;
            Matrix4x4 toWorld = space.localToWorldMatrix, toLocal = space.worldToLocalMatrix;
            bool reset = !started || (space.position - lastPos).sqrMagnitude > 1f;   // first frame or a teleport
            started = true; lastPos = space.position;
            if (!ha) ha = GetComponent<HumanoidAnimator>();
            bool seated = ha && ha.seated;
            float scale = space.lossyScale.y;   // the bones themselves are never scaled
            // the colliders where they are this frame, once
            foreach (var c in allColliders)
            {
                var m = c.node.localToWorldMatrix;
                c.wa = m.MultiplyPoint3x4(c.a);
                c.wb = c.capsule ? m.MultiplyPoint3x4(c.b) : c.wa;
                c.wr = c.r * scale;
            }
            // Joints come chain by chain, top first. A joint below another spring joint takes its parent's new pose from
            // that joint's result instead of reading it back from the transform, so each joint costs one write.
            for (int i = 0; i < joints.Count; i++)
            {
                var j = joints[i];
                Quaternion parentRot, actualParentRot; Vector3 head;
                if (j.parentJoint >= 0)
                {
                    var pj = joints[j.parentJoint];
                    actualParentRot = parentRot = pj.worldRot;
                    head = pj.head + parentRot * (j.restLocalPos * scale);
                }
                else
                {
                    var p = j.t.parent;
                    if (!p) continue;
                    p.GetPositionAndRotation(out var pp, out actualParentRot);
                    parentRot = actualParentRot;
                    head = pp + actualParentRot * (j.restLocalPos * scale);
                    // the joint's rest under wherever its parent is now (a skirt's top joint: part way to the hips)
                    float follow = seated ? 0f : j.followHips;   // seated, the cloth lies on the thighs
                    if (follow > 0 && j.hips && p != j.hips)
                    {
                        parentRot = Quaternion.Slerp(parentRot, j.hips.rotation, follow);
                        head = Vector3.Lerp(head, j.hips.TransformPoint(j.restHipsPos), follow);
                        j.t.position = head;
                    }
                }
                var restRot = parentRot * j.restLocal;
                var restDir = restRot * j.axis;
                float len = j.length * scale;
                if (reset) j.cur = j.prev = toLocal.MultiplyPoint3x4(head + restDir * len);
                Vector3 cur = toWorld.MultiplyPoint3x4(j.cur), prev = toWorld.MultiplyPoint3x4(j.prev);
                var next = cur + (cur - prev) * (1f - j.drag)
                           + restDir * (j.stiffness * dt)
                           + j.gravityDir * (j.gravity * dt);
                next = head + (next - head).normalized * len;
                for (int k = 0; k < j.cols.Count; k++) next = PushOut(j.cols[k], next, j.hit, head, len);
                j.prev = j.cur; j.cur = toLocal.MultiplyPoint3x4(next);
                var rot = Quaternion.FromToRotation(restDir, next - head) * restRot;
                j.worldRot = rot; j.head = head;
                j.t.localRotation = Quaternion.Inverse(actualParentRot) * rot;
            }
        }

        static Vector3 PushOut(Collider c, Vector3 p, float hit, Vector3 head, float len)
        {
            Vector3 centre = c.wa;
            if (c.capsule)
            {
                Vector3 ab = c.wb - c.wa;
                float s = Mathf.Clamp01(Vector3.Dot(p - c.wa, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                centre = c.wa + ab * s;
            }
            float r = c.wr + hit;
            Vector3 d = p - centre;
            float d2 = d.sqrMagnitude;
            if (d2 >= r * r || d2 < 1e-10f) return p;
            p = centre + d * (r / Mathf.Sqrt(d2));
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
