using System;
using System.Collections.Generic;
using Dungine.Visual;
using UnityEngine;

namespace Dungine.World
{
    /// <summary>Helpers for building outdoor Barovian landscapes around a road.</summary>
    public class Outdoor
    {
        public readonly AreaContext ctx;
        public List<Vector2> road = new List<Vector2>();
        public float roadWidth = 3.2f;
        public Terrain terrain;
        public readonly List<Vector3> placed = new List<Vector3>();
        public readonly List<(Vector2 c, float r)> clearings = new List<(Vector2, float)>();
        public readonly List<(Vector2 c, float r)> flats = new List<(Vector2, float)>();
        public Func<float, float, float> extraHeight;
        public float baseHeight = 0f, hills = 7f, wallStart = 40f, wallSteep = 0.6f;
        public Vector3 size = new Vector3(240, 70, 240);
        public Vector3 origin = new Vector3(-120, -20, -120);
        public int seed = 1;

        /// <summary>Rectangles (building footprints, yards) kept clear of scatter.</summary>
        public readonly List<Rect> blockers = new List<Rect>();

        public Outdoor(AreaContext c) { ctx = c; }

        /// <summary>Secondary lanes, each with its own width; they count as road for flattening, texturing and scatter.</summary>
        public readonly List<(List<Vector2> pts, float width)> lanes = new List<(List<Vector2>, float)>();
        /// <summary>Cobbled areas (village squares); flattened and paved.</summary>
        public readonly List<(Vector2 c, float r)> paved = new List<(Vector2, float)>();
        public TexId roadTex = TexId.Mud;
        /// <summary>Distance from the road at which leaf litter takes over from grass.</summary>
        public float forestFloorStart = 12f;

        /// <summary>Distance to the nearest road, with narrower lanes offset so the same roadWidth thresholds apply.</summary>
        public float RoadDist(float x, float z)
        {
            var p = new Vector2(x, z);
            float d = road.Count > 1 ? Nature.DistToPolyline(p, road, out _) : 999f;
            foreach (var l in lanes) d = Mathf.Min(d, Nature.DistToPolyline(p, l.pts, out _) + (roadWidth - l.width));
            return d;
        }

        public void Lane(float width, params Vector2[] pts) => lanes.Add((new List<Vector2>(pts), width));

        float PavedWeight(float x, float z)
        {
            float w = 0;
            foreach (var pv in paved) w = Mathf.Max(w, Mathf.Clamp01(1 - (Vector2.Distance(new Vector2(x, z), pv.c) - pv.r) / 1.2f));
            return w;
        }

        float FlatWeight(float x, float z, out float flatH)
        {
            float w = 0; flatH = baseHeight;
            foreach (var f in flats)
            {
                float d = Vector2.Distance(new Vector2(x, z), f.c);
                float k = Mathf.Clamp01(1 - (d - f.r) / 6f);
                if (k > w) w = k;
            }
            return w;
        }

        public float Height(float x, float z)
        {
            float n = Noise.Fbm((x - origin.x) / size.x, (z - origin.z) / size.z, 3, 5, seed) - 0.5f;
            float h = baseHeight + n * hills * 2f;
            float rd = RoadDist(x, z);
            // valley walls away from the road
            if (rd > wallStart) h += (rd - wallStart) * wallSteep;
            // flatten the road bed
            float roadFlat = Mathf.Clamp01(1 - (rd - roadWidth) / 7f);
            h = Mathf.Lerp(h, baseHeight - 0.08f, roadFlat * 0.92f);
            FlatWeight(x, z, out _);
            foreach (var f in flats)
            {
                float d = Vector2.Distance(new Vector2(x, z), f.c);
                float k = Mathf.Clamp01(1 - (d - f.r) / 8f);
                h = Mathf.Lerp(h, baseHeight, k);
            }
            foreach (var pv in paved)
            {
                float d = Vector2.Distance(new Vector2(x, z), pv.c);
                h = Mathf.Lerp(h, baseHeight, Mathf.Clamp01(1 - (d - pv.r) / 8f));
            }
            if (extraHeight != null) h += extraHeight(x, z);
            return h;
        }

        public Terrain BuildTerrain(TexId grass = TexId.DeadGrass)
        {
            var spec = new Nature.TerrainSpec
            {
                size = size, origin = origin, res = 257,
                height = Height,
                layers = new[] { grass, TexId.Dirt, roadTex, TexId.Rock, TexId.ForestFloor, TexId.Cobble },
                tiles = new[] { 6f, 5f, 7f, 9f, 6f, 3.2f },
                splat = (x, z, h, slope) =>
                {
                    float rd = RoadDist(x, z);
                    float road = Mathf.Clamp01(1 - (rd - roadWidth * 0.6f) / 1.5f);
                    float shoulder = Mathf.Clamp01(1 - (rd - roadWidth) / 4f) * (1 - road);
                    float rock = MathX.Smoothstep(0, 1, (slope - 24f) / 14f);
                    float n = Noise.Fbm((x + 300) / 60f, (z + 300) / 60f, 2, 3, seed + 5);
                    float forest = Mathf.Clamp01((rd - forestFloorStart) / 10f) * (0.5f + n);
                    float open = 1 - forest;
                    foreach (var c in clearings) { float d = Vector2.Distance(new Vector2(x, z), c.c); if (d < c.r) { forest *= d / c.r; open = 1 - forest; } }
                    float pave = PavedWeight(x, z);
                    float keep = 1 - pave;
                    return new[] { open * (1 - rock) * (1 - road) * (1 - shoulder) * 0.9f * keep, (shoulder * 0.9f + open * 0.1f * n) * keep, road * keep, rock * 1.5f * keep, forest * (1 - rock) * (1 - road) * keep, pave * 1.5f };
                }
            };
            terrain = Nature.BuildTerrain(spec, ctx.root);
            ctx.terrain = terrain;
            return terrain;
        }

        public bool Blocked(float x, float z, float margin)
        {
            if (RoadDist(x, z) < roadWidth + margin) return true;
            foreach (var c in clearings) if (Vector2.Distance(new Vector2(x, z), c.c) < c.r) return true;
            foreach (var f in flats) if (Vector2.Distance(new Vector2(x, z), f.c) < f.r + margin) return true;
            foreach (var pv in paved) if (Vector2.Distance(new Vector2(x, z), pv.c) < pv.r + margin) return true;
            foreach (var r in blockers) if (r.Contains(new Vector2(x, z))) return true;
            return false;
        }

        public void Forest(int pines, int dead, float nearRoad = 7f)
        {
            var t = terrain;
            // trees crowd right up to the verge; density ramps up over the next ten metres
            Nature.Scatter(t, ctx.root, pines, seed + 10, (x, z) => Blocked(x, z, nearRoad) ? 0 : ForestDensity(RoadDist(x, z), nearRoad), v => Nature.Pine(v + seed * 10), "pine" + seed, 6, new Vector2(0.75f, 1.5f), Kit.ColliderKind.Capsule, 3.0f, placed, 0.3f);
            Nature.Scatter(t, ctx.root, dead, seed + 11, (x, z) => Blocked(x, z, 2.5f) ? 0 : 0.7f, v => Nature.DeadTree(v + seed * 20), "dead" + seed, 6, new Vector2(0.8f, 1.35f), Kit.ColliderKind.Capsule, 3.5f, placed, 0.2f);
            Nature.Scatter(t, ctx.root, pines / 5, seed + 12, (x, z) => Blocked(x, z, 2f) ? 0 : 0.6f, v => Nature.Rock(v + seed, 0.6f + (v % 3) * 0.35f), "rock" + seed, 6, new Vector2(0.7f, 1.6f), Kit.ColliderKind.Box, 2f, placed, 0.15f);
            Nature.Scatter(t, ctx.root, pines / 8, seed + 13, (x, z) => Blocked(x, z, 2f) ? 0 : 0.5f, v => Nature.Stump(v), "stump", 1, new Vector2(0.8f, 1.2f), Kit.ColliderKind.Box, 2f, placed, 0.1f);
            Nature.Scatter(t, ctx.root, pines / 10, seed + 14, (x, z) => Blocked(x, z, 3f) ? 0 : 0.5f, v => Nature.FallenLog(v), "log", 1, new Vector2(0.8f, 1.2f), Kit.ColliderKind.Box, 3f, placed, 0.1f);
            // undergrowth: no colliders, it doesn't block movement
            Nature.Scatter(t, ctx.root, pines, seed + 15, (x, z) => RoadDist(x, z) < roadWidth + 1.2f || InClearing(x, z) || InBlocker(x, z) || PavedWeight(x, z) > 0 ? 0 : 0.7f, v => Nature.Bush(v + seed * 3), "bush" + seed, 8, new Vector2(0.7f, 1.4f), Kit.ColliderKind.None, 0.8f, null, 0.1f);
            Nature.Scatter(t, ctx.root, pines * 2, seed + 16, (x, z) => RoadDist(x, z) < roadWidth + 1.5f || InBlocker(x, z) || PavedWeight(x, z) > 0 ? 0 : Mathf.Clamp01((RoadDist(x, z) - roadWidth) / 8f), v => Nature.Fern(v + seed * 5), "fern" + seed, 6, new Vector2(0.8f, 1.5f), Kit.ColliderKind.None, 0f, null, 0.05f);
        }

        /// <summary>Thick woods along the road, thinning out up the valley walls the camera rarely sees.</summary>
        static float ForestDensity(float rd, float nearRoad)
        {
            float ramp = Mathf.Clamp01((rd - nearRoad) / 6f) * 0.75f + 0.25f;
            float far = Mathf.Lerp(1f, 0.18f, Mathf.Clamp01((rd - 35f) / 25f));
            return ramp * far;
        }

        bool InBlocker(float x, float z)
        {
            foreach (var r in blockers) if (r.Contains(new Vector2(x, z))) return true;
            return false;
        }

        bool InClearing(float x, float z)
        {
            foreach (var c in clearings) if (Vector2.Distance(new Vector2(x, z), c.c) < c.r) return true;
            return false;
        }

        public void Grass(int count, float roadClear = 1.2f)
        {
            Nature.Scatter(terrain, ctx.root, count, seed + 20, (x, z) => RoadDist(x, z) < roadWidth + roadClear || InBlocker(x, z) || PavedWeight(x, z) > 0 ? 0 : 0.85f, v => Nature.GrassTuft(v), "grass", 6, new Vector2(0.8f, 1.7f), Kit.ColliderKind.None, 0f, null, 0.02f);
        }

        public void RoadFog(float spacing = 30f, Color? col = null)
        {
            var c = col ?? new Color(.62f, .65f, .72f, .2f);
            float acc = 0;
            for (int i = 0; i < road.Count - 1; i++)
            {
                Vector2 a = road[i], b = road[i + 1];
                float len = Vector2.Distance(a, b);
                for (float t = 0; t < len; t += spacing)
                {
                    Vector2 p = Vector2.Lerp(a, b, t / len);
                    Kit.FogBank(ctx.root, new Vector3(p.x, ctx.GroundY(p.x, p.y) + 0.8f, p.y), new Vector3(34, 2.2f, 34), c, 26, 9f, 0.2f);
                    acc += spacing;
                }
            }
        }

        public void Lamp(float x, float z, float yaw, bool lit = true)
        {
            var p = ctx.G3(x, z);
            ctx.Prop("lamppost", Props.LampPost, p, yaw, 1, Kit.ColliderKind.Capsule);
            if (lit) Kit.PointLight(ctx.root, p + Quaternion.Euler(0, yaw, 0) * new Vector3(0.45f, 2.85f, 0), new Color(1f, .72f, .42f), 3.2f, 11f, false);
        }

        /// <summary>A lamppost on the verge at a fraction along the road, side -1 = left, +1 = right, arm over the road.</summary>
        public void RoadLamp(float t01, int side, bool lit = true)
        {
            var c = OnRoad(t01); var d = RoadDir(t01);
            var right = new Vector3(d.z, 0, -d.x);
            var p = c + right * side * (roadWidth + 1.2f);
            float yaw = Quaternion.LookRotation(-right * side).eulerAngles.y - 90f;
            Lamp(p.x, p.z, yaw, lit);
        }

        public Vector3 RoadDir(float t01)
        {
            var a = OnRoad(Mathf.Max(0, t01 - 0.01f)); var b = OnRoad(Mathf.Min(1, t01 + 0.01f));
            var d = b - a; d.y = 0; return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward;
        }

        public Vector3 OnRoad(float t01)
        {
            float total = 0; for (int i = 0; i < road.Count - 1; i++) total += Vector2.Distance(road[i], road[i + 1]);
            float target = total * t01, acc = 0;
            for (int i = 0; i < road.Count - 1; i++)
            {
                float l = Vector2.Distance(road[i], road[i + 1]);
                if (acc + l >= target) { var p = Vector2.Lerp(road[i], road[i + 1], (target - acc) / l); return ctx.G3(p.x, p.y); }
                acc += l;
            }
            var e = road[road.Count - 1]; return ctx.G3(e.x, e.y);
        }
    }
}
