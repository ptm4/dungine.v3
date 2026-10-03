using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace Dungine.Library
{
    /// <summary>
    /// The spring pass as a Burst job (2026-10-03, for crowds). It does the same sums as <c>Springs</c>: the main thread
    /// only reads each chain's parent bone and the colliders, and a Burst job runs every joint of the figure. Each figure is
    /// scheduled in its own Tick, so the figures run side by side on the worker threads. <see cref="LibrarySprings"/> then,
    /// once every figure's Tick has read its bones, writes the joints' local rotations with transform jobs. (Writing them
    /// from Tick would stall: every figure in an area hangs under the area's one root, so the next figure's first bone read
    /// would wait for the earlier figures' writes.)
    /// A rig whose skirt panels follow the hips (round-2 rigs without centre chains) keeps the managed pass.
    /// </summary>
    public partial class LibraryRigDriver
    {
        /// <summary>Dev switch: false runs the managed pass for every figure.</summary>
        public static bool BurstOn = true;

        struct JointDef
        {
            public quaternion restLocal; public float3 axis, gravityDir, restLocalPos;
            public float length, stiffness, drag, gravity, hit;
            public int parentJoint, colStart, colCount;
        }
        struct JointState { public float3 cur, prev, head; public quaternion worldRot; }
        struct ColWorld { public float3 a, b; public float r; public int capsule; }

        NativeArray<JointDef> nDefs; NativeArray<JointState> nState; NativeArray<int> nJointCols;
        NativeArray<ColWorld> nCols; NativeArray<RigidTransform> nRoots; NativeArray<quaternion> nLocal;
        TransformAccessArray nTransforms; JobHandle nHandle; bool nBuilt, nReady, nUsable, nStarted, nPending;

        void BuildNative()
        {
            nUsable = joints.Count > 0;
            foreach (var j in joints) if (j.followHips > 0) nUsable = false;
            if (!nUsable) return;
            var colIndex = new Dictionary<Collider, int>();
            for (int i = 0; i < allColliders.Count; i++) colIndex[allColliders[i]] = i;
            var flat = new List<int>();
            nDefs = new NativeArray<JointDef>(joints.Count, Allocator.Persistent);
            for (int i = 0; i < joints.Count; i++)
            {
                var j = joints[i];
                int start = flat.Count;
                foreach (var c in j.cols) if (colIndex.TryGetValue(c, out var ci)) flat.Add(ci);
                nDefs[i] = new JointDef
                {
                    restLocal = j.restLocal, axis = j.axis, gravityDir = j.gravityDir, restLocalPos = j.restLocalPos,
                    length = j.length, stiffness = j.stiffness, drag = j.drag, gravity = j.gravity, hit = j.hit,
                    parentJoint = j.parentJoint, colStart = start, colCount = flat.Count - start,
                };
            }
            nJointCols = new NativeArray<int>(flat.ToArray(), Allocator.Persistent);
            nState = new NativeArray<JointState>(joints.Count, Allocator.Persistent);
            nCols = new NativeArray<ColWorld>(Mathf.Max(1, allColliders.Count), Allocator.Persistent);
            nRoots = new NativeArray<RigidTransform>(joints.Count, Allocator.Persistent);
            nLocal = new NativeArray<quaternion>(joints.Count, Allocator.Persistent);
            var ts = new Transform[joints.Count];
            for (int i = 0; i < joints.Count; i++) ts[i] = joints[i].t;
            nTransforms = new TransformAccessArray(ts);
            nReady = true;
        }

        /// <summary>Schedules this figure's spring pass. Returns false when the managed pass should run instead.</summary>
        bool ScheduleSprings(float dt)
        {
            if (!BurstOn) return false;
            if (!nBuilt) { nBuilt = true; BuildNative(); }
            if (!nUsable || !nReady) return false;
            ApplySprings();   // (a pass still waiting from an earlier frame)
            dt = Mathf.Clamp(dt, 1e-4f, 1f / 20f);
            var space = transform;
            bool reset = !nStarted || (space.position - lastPos).sqrMagnitude > 1f;
            nStarted = true; started = false; lastPos = space.position;   // (started belongs to the managed pass: it resets if switched back)
            float scale = space.lossyScale.y;
            for (int i = 0; i < allColliders.Count; i++)
            {
                var c = allColliders[i];
                var m = c.node.localToWorldMatrix;
                Vector3 a = m.MultiplyPoint3x4(c.a), b = c.capsule ? m.MultiplyPoint3x4(c.b) : a;
                nCols[i] = new ColWorld { a = a, b = b, r = c.r * scale, capsule = c.capsule ? 1 : 0 };
            }
            for (int i = 0; i < joints.Count; i++)
            {
                if (joints[i].parentJoint >= 0) continue;
                var p = joints[i].t.parent;
                if (!p) { nRoots[i] = RigidTransform.identity; continue; }
                p.GetPositionAndRotation(out var pos, out var rot);
                nRoots[i] = new RigidTransform(rot, pos);
            }
            var job = new SpringJob
            {
                defs = nDefs, state = nState, jointCols = nJointCols, cols = nCols, roots = nRoots, localRot = nLocal,
                toWorld = space.localToWorldMatrix, toLocal = space.worldToLocalMatrix, dt = dt, scale = scale, reset = reset,
            };
            nHandle = job.Schedule();
            nPending = true;
            if (manual) ApplySprings(); else LibrarySprings.Add(this, nHandle);
            return true;
        }

        /// <summary>Waits for this figure's spring job and writes its joints' local rotations (main thread).</summary>
        internal void ApplySprings()
        {
            if (!nPending) return;
            nHandle.Complete();
            nPending = false;
            for (int i = 0; i < joints.Count; i++) joints[i].t.localRotation = nLocal[i];
        }

        /// <summary>Schedules the transform job that writes this figure's spring results, after its spring job.</summary>
        internal JobHandle ScheduleWrite()
        {
            if (!nPending) return default;
            nPending = false;
            nHandle = new WriteJob { localRot = nLocal }.Schedule(nTransforms, nHandle);
            return nHandle;
        }

        void OnDestroy()
        {
            nHandle.Complete();
            if (!nReady) return;
            nReady = false;
            nDefs.Dispose(); nState.Dispose(); nJointCols.Dispose(); nCols.Dispose(); nRoots.Dispose(); nLocal.Dispose();
            if (nTransforms.isCreated) nTransforms.Dispose();
        }

        [BurstCompile]
        struct SpringJob : IJob
        {
            [ReadOnly] public NativeArray<JointDef> defs;
            public NativeArray<JointState> state;
            [ReadOnly] public NativeArray<int> jointCols;
            [ReadOnly] public NativeArray<ColWorld> cols;
            [ReadOnly] public NativeArray<RigidTransform> roots;
            [WriteOnly] public NativeArray<quaternion> localRot;
            public float4x4 toWorld, toLocal;
            public float dt, scale;
            public bool reset;

            public void Execute()
            {
                for (int i = 0; i < defs.Length; i++)
                {
                    var d = defs[i];
                    quaternion parentRot; float3 head;
                    if (d.parentJoint >= 0)
                    {
                        var ps = state[d.parentJoint];
                        parentRot = ps.worldRot;
                        head = ps.head + math.mul(parentRot, d.restLocalPos * scale);
                    }
                    else
                    {
                        var r = roots[i];
                        parentRot = r.rot;
                        head = r.pos + math.mul(parentRot, d.restLocalPos * scale);
                    }
                    var restRot = math.mul(parentRot, d.restLocal);
                    var restDir = math.mul(restRot, d.axis);
                    float len = d.length * scale;
                    var s = state[i];
                    if (reset) { s.cur = math.transform(toLocal, head + restDir * len); s.prev = s.cur; }
                    float3 cur = math.transform(toWorld, s.cur), prev = math.transform(toWorld, s.prev);
                    var next = cur + (cur - prev) * (1f - d.drag) + restDir * (d.stiffness * dt) + d.gravityDir * (d.gravity * dt);
                    next = head + math.normalizesafe(next - head) * len;
                    for (int k = 0; k < d.colCount; k++) next = PushOut(cols[jointCols[d.colStart + k]], next, d.hit, head, len);
                    s.prev = s.cur; s.cur = math.transform(toLocal, next);
                    var rot = math.mul(FromTo(restDir, next - head), restRot);
                    s.worldRot = rot; s.head = head;
                    state[i] = s;
                    localRot[i] = math.mul(math.inverse(parentRot), rot);
                }
            }

            static float3 PushOut(ColWorld c, float3 p, float hit, float3 head, float len)
            {
                float3 centre = c.a;
                if (c.capsule != 0)
                {
                    float3 ab = c.b - c.a;
                    float t = math.saturate(math.dot(p - c.a, ab) / math.max(1e-6f, math.lengthsq(ab)));
                    centre = c.a + ab * t;
                }
                float r = c.r + hit;
                float3 dd = p - centre;
                float d2 = math.lengthsq(dd);
                if (d2 >= r * r || d2 < 1e-10f) return p;
                p = centre + dd * (r / math.sqrt(d2));
                return head + math.normalizesafe(p - head) * len;
            }

            /// <summary>The shortest turn taking direction a to direction b (Quaternion.FromToRotation).</summary>
            static quaternion FromTo(float3 a, float3 b)
            {
                a = math.normalizesafe(a); b = math.normalizesafe(b);
                float d = math.dot(a, b);
                if (d < -0.99999f)
                {
                    var axis = math.cross(new float3(1, 0, 0), a);
                    if (math.lengthsq(axis) < 1e-6f) axis = math.cross(new float3(0, 1, 0), a);
                    return quaternion.AxisAngle(math.normalize(axis), math.PI);
                }
                var c = math.cross(a, b);
                return math.normalize(new quaternion(c.x, c.y, c.z, 1f + d));
            }
        }

        [BurstCompile]
        struct WriteJob : IJobParallelForTransform
        {
            [ReadOnly] public NativeArray<quaternion> localRot;
            public void Execute(int index, TransformAccess t) => t.localRotation = localRot[index];
        }
    }

    /// <summary>Once a frame, after every library figure's Tick: waits for the spring jobs scheduled that frame and writes
    /// their results.</summary>
    [DefaultExecutionOrder(160)]
    public class LibrarySprings : MonoBehaviour
    {
        static LibrarySprings runner;
        static JobHandle pending;
        static readonly List<LibraryRigDriver> drivers = new List<LibraryRigDriver>();

        internal static void Add(LibraryRigDriver d, JobHandle h)
        {
            if (!runner)
            {
                var go = new GameObject("LibrarySprings");
                DontDestroyOnLoad(go);
                runner = go.AddComponent<LibrarySprings>();
            }
            pending = JobHandle.CombineDependencies(pending, h);
            drivers.Add(d);
            JobHandle.ScheduleBatchedJobs();
        }

        void LateUpdate()
        {
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            JobHandle all = pending;
            foreach (var d in drivers) if (d) all = JobHandle.CombineDependencies(all, d.ScheduleWrite());
            JobHandle.ScheduleBatchedJobs();
            all.Complete(); pending = default;
            drivers.Clear();
            long dtk = System.Diagnostics.Stopwatch.GetTimestamp() - t0;
            LibraryRigDriver.SpringTicks += dtk; LibraryRigDriver.WaitTicks += dtk;
        }
    }
}
