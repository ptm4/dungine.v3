using System.Collections.Generic;
using Dungine.Rules;
using Dungine.Visual;
using UnityEngine;
using UnityEngine.AI;

namespace Dungine
{
    /// <summary>Turns a Creature into a fully visualised, navigating Actor.</summary>
    public static class ActorFactory
    {
        public static GearLook GearFor(Creature c)
        {
            var g = new GearLook();
            var armor = c.eq.armor?.Def;
            g.armor = armor != null ? armor.avisual : ArmorVisual.Cloth;
            if (armor == null && c.sheet != null && c.IsClass(ClassId.Monk)) g.armor = ArmorVisual.None;
            var main = c.eq.main?.Def;
            var ranged = c.eq.ranged?.Def;
            g.main = main != null ? main.visual : ranged != null ? ranged.visual : WeaponVisual.None;
            var off = c.eq.off?.Def;
            if (off != null) g.off = off.kind == ItemKind.Shield ? WeaponVisual.Shield : off.visual;
            g.helmet = c.eq.helmet != null;
            if (c.sheet != null && c.sheet.look != null && c.sheet.look.armorLook >= 0 && armor != null) g.armor = (ArmorVisual)c.sheet.look.armorLook;
            return g;
        }

        public static Actor Spawn(Creature c, Vector3 pos, Quaternion rot, int faceRes = 256)
        {
            GameObject go; IAnimDriver anim; List<Renderer> rends; HumanoidRig rig = null; float radius = 0.35f, height = 1.8f;
            var m = c.mdef;
            BodyKind body = c.isPC && c.wildshapeBackup == null ? BodyKind.Humanoid : (m != null ? m.body : BodyKind.Humanoid);
            switch (body)
            {
                case BodyKind.Wolf: case BodyKind.DireWolf: case BodyKind.Bear: case BodyKind.Rat:
                    {
                        var q = BeastBuilder.Build(body, m.tint, m.scale, c.name);
                        go = q.gameObject; var a = go.AddComponent<BeastAnimator>(); a.Init(q); anim = a; rends = q.renderers;
                        radius = 0.45f * m.scale; height = q.height * 1.2f;
                        break;
                    }
                case BodyKind.Swarm:
                    { var s = SwarmAnimator.Build(c.name, 26, m.tint); go = s.gameObject; anim = s; rends = s.renderers; radius = 0.6f; height = 1.8f; break; }
                case BodyKind.Mound:
                    { var s = MoundAnimator.Build(c.name, m.tint, m.scale); go = s.gameObject; anim = s; rends = s.renderers; radius = 1.0f * m.scale; height = 2.2f * m.scale; break; }
                case BodyKind.Grick:
                    { var s = SimpleBodyAnimator.BuildGrick(c.name, m.tint); go = s.gameObject; anim = s; rends = s.renderers; radius = 0.5f; height = 1.2f; break; }
                case BodyKind.Broom:
                    { var s = SimpleBodyAnimator.BuildBroom(c.name); go = s.gameObject; anim = s; rends = s.renderers; radius = 0.3f; height = 1.2f; break; }
                default:
                    {
                        Appearance look; GearLook gear; MotionStyle style = MotionStyle.Normal;
                        if (c.isPC) { look = c.sheet.look; gear = GearFor(c); }
                        else { look = m.look ?? new Appearance(); gear = m.gear ?? new GearLook(); style = m.motion; }
                        rig = HumanoidBuilder.Build(look, gear, c.name, faceRes);
                        go = rig.gameObject;
                        var a = go.AddComponent<HumanoidAnimator>(); a.Init(rig, style); anim = a; rends = rig.renderers;
                        radius = Mathf.Clamp(rig.height * 0.2f, 0.25f, 0.45f); height = rig.height;
                        if (m != null && m.tint != Color.white && look.ghostly)
                        {
                            var gm = MatLib.Ghost(new Color(m.tint.r, m.tint.g, m.tint.b, 0.55f), new Color(0.25f, 0.25f, 0.35f, 1f));
                            var mats = rig.body.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = gm; rig.body.sharedMaterials = mats;
                        }
                        break;
                    }
            }
            go.name = c.name;
            go.transform.SetPositionAndRotation(pos, rot);
            go.layer = Layers.Actors;
            foreach (var r in rends) if (r) r.gameObject.layer = Layers.Actors;
            var col = go.AddComponent<CapsuleCollider>();
            col.radius = radius; col.height = height; col.center = new Vector3(0, height * 0.5f, 0);
            col.isTrigger = true;
            // before the area's navmesh is baked there is nothing to attach to: add the agent disabled
            bool navReady = NavMesh.SamplePosition(go.transform.position, out _, 3f, NavMesh.AllAreas);
            bool wasActive = go.activeSelf;
            if (!navReady) go.SetActive(false);
            var agent = go.AddComponent<NavMeshAgent>();
            if (!navReady) { agent.enabled = false; go.SetActive(wasActive); }
            agent.radius = Mathf.Min(radius, 0.5f);
            agent.height = height;
            agent.speed = 3.6f;
            agent.angularSpeed = 900;
            agent.acceleration = 24;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.avoidancePriority = c.isPC ? 40 : 50;
            var actor = go.AddComponent<Actor>();
            actor.anim = anim; actor.rig = rig; actor.renderers = rends; actor.radius = radius;
            actor.Init(c);
            if (!NavMesh.SamplePosition(pos, out var hit, 3f, NavMesh.AllAreas)) agent.enabled = false;
            else agent.Warp(hit.position);
            actor.RefreshLife();
            return actor;
        }

        /// <summary>Rebuild a PC's visuals after equipment/appearance changes, keeping position.</summary>
        public static Actor Rebuild(Actor a)
        {
            var c = a.c; var pos = a.transform.position; var rot = a.transform.rotation;
            var ringOn = a.ring != null && a.ring.gameObject.activeSelf;
            Object.Destroy(a.gameObject);
            var na = Spawn(c, pos, rot, c.isPC ? 512 : 256);
            if (ringOn) na.SetRing(true, UI.Theme.Gold);
            return na;
        }
    }
}
