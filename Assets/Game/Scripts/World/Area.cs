using System;
using System.Collections.Generic;
using System.Linq;
using Dungine.Rules;
using Dungine.Visual;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Dungine.World
{
    /// <summary>A location in the campaign. Subclasses build geometry and populate it.</summary>
    public abstract class AreaDef
    {
        public abstract string Id { get; }
        public abstract string Title { get; }
        public virtual string Subtitle => "";
        public virtual bool Interior => false;
        public virtual AtmosphereProfile Atmos => Atmosphere.BaroviaDay;
        public virtual string Music => Interior ? "interior" : "explore";
        public virtual string Ambience => Interior ? "interior" : "wind";
        public abstract void Build(AreaContext ctx);
        public virtual void OnEnter(AreaContext ctx, bool firstVisit) { }
        public virtual float CameraMaxZoom => Interior ? 16f : 28f;
        /// <summary>Somewhere too dangerous to sleep (a haunted house, a vampire's cellar).</summary>
        public virtual bool RestUnsafe => false;
    }

    /// <summary>Everything an area creates, plus helpers for building it.</summary>
    public class AreaContext
    {
        public AreaDef def;
        public Transform root;
        public Cutaway cutaway;
        public Terrain terrain;
        public readonly Dictionary<string, Vector3> spawns = new Dictionary<string, Vector3>();
        public readonly Dictionary<string, float> spawnYaw = new Dictionary<string, float>();
        public readonly List<Actor> npcs = new List<Actor>();
        public readonly List<Interactable> interactables = new List<Interactable>();
        public readonly Dictionary<string, Actor> byId = new Dictionary<string, Actor>();
        public readonly Dictionary<string, List<Actor>> encounters = new Dictionary<string, List<Actor>>();
        public readonly Dictionary<string, Transform> markers = new Dictionary<string, Transform>();
        public Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 200);

        public Game G => Game.I;

        public float GroundY(float x, float z, float fallback = 0)
        {
            if (terrain) return terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y;
            if (Physics.Raycast(new Vector3(x, 60, z), Vector3.down, out var hit, 200, Layers.GroundMask, QueryTriggerInteraction.Ignore)) return hit.point.y;
            return fallback;
        }

        public Vector3 G3(float x, float z) => new Vector3(x, GroundY(x, z), z);

        public void Spawn(string id, Vector3 pos, float yaw = 0) { spawns[id] = pos; spawnYaw[id] = yaw; }

        public Transform Marker(string id, Vector3 pos)
        {
            var t = new GameObject("Marker_" + id).transform; t.SetParent(root, false); t.position = pos; markers[id] = t; return t;
        }

        // ---------------- actors ----------------
        public Actor NPC(string id, string name, Appearance look, GearLook gear, Vector3 pos, float yaw, string dialogue = null, MotionStyle style = MotionStyle.Normal, Faction faction = Faction.Neutral)
        {
            if (G.GetFlag("dead:" + Key(id)) > 0 || G.GetFlag("gone:" + Key(id)) > 0) return null;
            var m = new MonsterDef { id = id, name = name, type = CreatureType.Humanoid, faction = faction, ac = 11, hp = 12, look = look, gear = gear ?? new GearLook(), motion = style, abil = new[] { 10, 10, 10, 10, 10, 10 }, actions = new[] { "mace_guard" } };
            var c = Creature.FromMonster(m);
            c.faction = faction;
            var a = ActorFactory.Spawn(c, pos, Quaternion.Euler(0, yaw, 0));
            a.transform.SetParent(root, true);
            a.npcId = Key(id); a.dialogue = dialogue; a.talkable = dialogue != null; a.displayName = name;
            npcs.Add(a); byId[id] = a;
            return a;
        }

        public Actor Monster(string id, string monsterId, Vector3 pos, float yaw, string encounter = null, bool hostileOnSight = true, float sight = 12f)
        {
            if (G.GetFlag("dead:" + Key(id)) > 0) return null;
            var md = Monsters.Get(monsterId);
            var c = Creature.FromMonster(md);
            var a = ActorFactory.Spawn(c, pos, Quaternion.Euler(0, yaw, 0));
            a.transform.SetParent(root, true);
            a.npcId = Key(id); a.hostileOnSight = hostileOnSight; a.sightRange = sight; a.encounterId = encounter;
            foreach (var l in md.loot) a.loot.Add(new ItemStack(l));
            npcs.Add(a); byId[id] = a;
            if (encounter != null) { if (!encounters.TryGetValue(encounter, out var list)) encounters[encounter] = list = new List<Actor>(); list.Add(a); }
            return a;
        }

        public string Key(string id) => def.Id + "." + id;

        // ---------------- interactables ----------------
        public T Add<T>(string id, string label, GameObject go, float useRange = 1.6f) where T : Interactable
        {
            var it = go.AddComponent<T>();
            it.id = Key(id); it.label = label; it.useRange = useRange;
            if (G.GetFlag("used:" + it.id) > 0) it.used = true;
            interactables.Add(it);
            if (go.GetComponent<Collider>() == null)
            {
                var r = go.GetComponentInChildren<Renderer>();
                if (r) Interactable.AddBoxCollider(go, go.transform.InverseTransformPoint(r.bounds.center), r.bounds.size);
            }
            return it;
        }

        public Container Chest(string id, string label, Vector3 pos, float yaw, IEnumerable<string> loot, int gold = 0, bool locked = false, int lockDC = 12, string key = null)
        {
            var go = new GameObject("Chest_" + id);
            go.transform.SetParent(root, false); go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var closed = Kit.Place("chest", () => Props.Chest(false), go.transform, Vector3.zero, 0, 1, Kit.ColliderKind.None);
            var open = Kit.Place("chest_open", () => Props.Chest(true), go.transform, Vector3.zero, 0, 1, Kit.ColliderKind.None);
            open.SetActive(false);
            var obs = go.AddComponent<NavMeshObstacle>(); obs.carving = false; obs.size = new Vector3(0.9f, 0.6f, 0.55f); obs.center = new Vector3(0, 0.3f, 0);
            var c = Add<Container>(id, label, go, 1.4f);
            Interactable.AddBoxCollider(go, new Vector3(0, 0.3f, 0), new Vector3(1f, 0.7f, 0.7f));
            c.closedModel = closed; c.openModel = open;
            c.useOffset = new Vector3(0, 0, 0.9f);
            c.locked = locked && G.GetFlag("unlocked:" + c.id) <= 0; c.lockDC = lockDC; c.keyId = key;
            if (G.GetFlag("opened:" + c.id) <= 0 && G.GetFlag("emptied:" + c.id) <= 0) { foreach (var l in loot) c.items.Add(new ItemStack(l)); c.gold = gold; }
            c.SyncOpen();
            return c;
        }

        public Container Stash(string id, string label, GameObject visual, IEnumerable<string> loot, int gold = 0)
        {
            var c = Add<Container>(id, label, visual, 1.6f);
            if (G.GetFlag("emptied:" + c.id) <= 0) { foreach (var l in loot) c.items.Add(new ItemStack(l)); c.gold = gold; }
            return c;
        }

        public Transition Door(string id, string label, Vector3 pos, float yaw, string area, string spawn, Vector3 size, bool locked = false, string key = null)
        {
            var go = new GameObject("Door_" + id);
            go.transform.SetParent(root, false); go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            Interactable.AddBoxCollider(go, new Vector3(0, size.y / 2, 0), size);
            var t = Add<Transition>(id, label, go, 1.8f);
            t.targetArea = area; t.targetSpawn = spawn;
            t.locked = locked && G.GetFlag("unlocked:" + t.id) <= 0; t.keyId = key;
            t.useOffset = new Vector3(0, 0, 0.9f);
            return t;
        }

        public Readable Note(string id, string label, GameObject visual, string title, string text, string giveItem = null)
        {
            var r = Add<Readable>(id, label, visual, 1.5f);
            r.title = title; r.text = text; r.giveItem = giveItem; r.loreKey = id;
            return r;
        }

        public ScriptedUse Use(string id, string label, GameObject visual, string verb, Action<Actor, ScriptedUse> onUse, bool once = false)
        {
            var s = Add<ScriptedUse>(id, label, visual, 1.7f);
            s.verb = verb; s.onUse = onUse; s.once = once;
            return s;
        }

        public TriggerZone Trigger(string id, Vector3 pos, float radius, Action onEnter, string requireFlag = null)
        {
            if (G.GetFlag("trig:" + Key(id)) > 0) return null;
            var go = new GameObject("Trigger_" + id); go.transform.SetParent(root, false); go.transform.position = pos;
            var t = go.AddComponent<TriggerZone>(); t.id = Key(id); t.radius = radius; t.onEnter = onEnter; t.requireFlag = requireFlag;
            return t;
        }

        public GameObject Prop(string key, Func<(Mesh, Material[])> make, Vector3 worldPos, float yaw = 0, float scale = 1, Kit.ColliderKind col = Kit.ColliderKind.None)
        {
            var go = Kit.Place(key, make, root, Vector3.zero, yaw, scale, col);
            go.transform.position = worldPos;
            return go;
        }

        public GameObject Obstacle(string key, Func<(Mesh, Material[])> make, Vector3 worldPos, float yaw = 0, float scale = 1)
            => Prop(key, make, worldPos, yaw, scale, Kit.ColliderKind.Box);
    }

    /// <summary>Loads/unloads areas and bakes their navmesh.</summary>
    public static class AreaLoader
    {
        public static readonly Dictionary<string, AreaDef> Areas = new Dictionary<string, AreaDef>();
        public static void Register(AreaDef a) => Areas[a.Id] = a;

        public static AreaContext Build(AreaDef def)
        {
            var ctx = new AreaContext { def = def };
            var root = new GameObject("Area_" + def.Id);
            ctx.root = root.transform;
            if (def.Interior) ctx.cutaway = root.AddComponent<Cutaway>();
            Atmosphere.Ensure().Apply(def.Atmos);
            def.Build(ctx);
            BakeNav(ctx);
            // snap actors to the fresh navmesh
            foreach (var a in ctx.npcs)
            {
                if (!a) continue;
                if (a.sitting || a.c.hp <= 0 || a.agent == null || !a.gameObject.activeInHierarchy) continue;
                if (NavMesh.SamplePosition(a.transform.position, out var hit, 3f, NavMesh.AllAreas)) { a.agent.enabled = true; a.agent.Warp(hit.position); }
            }
            foreach (var it in ctx.interactables) it.SetHiddenVisual();
            return ctx;
        }

        public static void BakeNav(AreaContext ctx)
        {
            var surf = ctx.root.gameObject.AddComponent<NavMeshSurface>();
            surf.collectObjects = CollectObjects.Children;
            surf.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surf.layerMask = Layers.Mask(Layers.Default, Layers.Ground, Layers.Walls);
            surf.overrideVoxelSize = true;
            surf.voxelSize = 0.08f;
            surf.defaultArea = 0;
            surf.BuildNavMesh();
        }
    }
}
