using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dungine.Rules;
using Dungine.UI;
using Dungine.Visual;
using Dungine.World;
using UnityEngine;
using UnityEngine.AI;

namespace Dungine
{
    /// <summary>Prologue: the Old Svalich Road through the forest to the Gates of Barovia.</summary>
    public class AreaSvalichRoad : AreaDef
    {
        public override string Id => "svalich_road";
        public override string Title => "The Svalich Woods";
        public override string Subtitle => "Somewhere beyond the mists";
        public override AtmosphereProfile Atmos => Atmosphere.Mists;
        public override string Music => "explore";

        Transform leafL, leafR; NavMeshObstacle obsL, obsR;

        public override void Build(AreaContext ctx)
        {
            var o = new Outdoor(ctx) { seed = 3, hills = 6f, wallStart = 34f, wallSteep = 0.5f };
            o.road = new List<Vector2> { new Vector2(0, -118), new Vector2(6, -80), new Vector2(-4, -45), new Vector2(5, -12), new Vector2(0, 20), new Vector2(-3, 48), new Vector2(0, 70), new Vector2(2, 118) };
            o.clearings.Add((new Vector2(-16, 2), 7f));
            o.flats.Add((new Vector2(-16, 2), 6f));
            o.flats.Add((new Vector2(0, 62), 9f));
            o.BuildTerrain();
            o.Forest(900, 110, 4.5f);
            o.Grass(2200);
            o.RoadFog(22f, new Color(.78f, .74f, .76f, .16f));
            Kit.FogBank(ctx.root, new Vector3(0, 6, 150), new Vector3(240, 8, 60), new Color(.7f, .6f, .6f, .25f), 60, 30f, 0.3f);
            Buildings.CastleRavenloft(ctx.root, new Vector3(150, -4, 330), 210, 1.25f);

            var start = o.OnRoad(0.06f);
            ctx.Spawn("start", start, 5);
            ctx.Spawn("from_village", o.OnRoad(0.93f), 180);

            // --- the courier's wreck ---
            var wreck = new Vector3(6, 0, -62); wreck.y = ctx.GroundY(wreck.x, wreck.z);
            ctx.Obstacle("cart_broken", () => Props.Cart(true), wreck + new Vector3(2.5f, 0, 1f), 70);
            ctx.Prop("crate0.7", () => Props.Crate(0.7f), wreck + new Vector3(4.2f, 0, -0.8f), 20, 1, Kit.ColliderKind.Box);
            ctx.Prop("barrel", Props.Barrel, wreck + new Vector3(4f, 0.3f, 1.8f), 0).transform.rotation = Quaternion.Euler(90, 30, 0);
            ctx.Prop("sack", Props.Sack, wreck + new Vector3(3.2f, 0, -1.6f), 40);
            var courier = ctx.NPC("courier", "Dead Courier", new Appearance { race = RaceId.Human, skin = new Color(.62f, .55f, .5f), hair = new Color(.3f, .22f, .15f), hairStyle = 1, beardStyle = 1, cloth1 = new Color(.35f, .2f, .12f), cloth2 = new Color(.2f, .18f, .15f), gaunt = true }, new GearLook { armor = ArmorVisual.Cloth }, wreck + new Vector3(0.5f, 0, 0), 120);
            if (courier != null)
            {
                courier.c.hp = 0; courier.c.dead = true; courier.anim.SetLife(LifeState.Dead, true);
                courier.lootable = true; courier.talkable = false;
                if (!ctx.G.Flag("looted:" + courier.npcId)) { courier.gold = 12; courier.loot.Add(new ItemStack("potion_healing")); courier.loot.Add(new ItemStack("courier_note")); courier.loot.Add(new ItemStack("bread")); }
                else courier.looted = true;
                if (courier.agent) courier.agent.enabled = false;
            }
            ctx.Trigger("wreck_seen", wreck, 9f, () => { UIRoot.I.Bark(Game.I.Selected, "A wagon... and its driver. Wolves did this — or something wearing wolves."); });

            // --- wolves in the trees ---
            var w0 = new Vector3(-8, 0, -26); var w1 = new Vector3(9.5f, 0, -31); var w2 = new Vector3(-7, 0, -37);
            foreach (var (p, id) in new[] { (w0, "wolf1"), (w1, "wolf2"), (w2, "wolf3") })
            {
                var pp = ctx.G3(p.x, p.z);
                var w = ctx.Monster(id, "wolf", pp, 90, "wolfpack", true, 15f);
            }
            ctx.Trigger("wolves_howl", ctx.G3(3, -42), 10f, () => { Audio.Sfx.Play("howl", 0.9f); UIRoot.I.Bark(Game.I.Selected, "Did you hear that? Weapons out."); });

            // --- roadside shrine ---
            var sh = ctx.G3(9, 14);
            var shrine = BuildShrine(ctx, sh, -100);
            ctx.Use("shrine", "Shrine of the Morninglord", shrine, "Pray", (u, s) =>
            {
                if (Game.I.Flag("shrine_prayed_" + Game.I.GetFlag("rests"))) { UIRoot.I.Bark(u, "The shrine is silent. Perhaps tomorrow."); return; }
                Game.I.SetFlag("shrine_prayed_" + Game.I.GetFlag("rests"));
                foreach (var c in Game.I.party.Where(c => c.Active)) { c.AddCond(Cond.Blessed, 10, null); c.hp = Mathf.Min(c.MaxHPTotal, c.hp + 5); if (c.actor) FX.Burst(c.actor.Chest, FxKind.Holy, 1f); }
                Audio.Sfx.Play("revive");
                ReadWindow.Show("Shrine of the Morninglord", "A carved sun, its gilding long since flaked away, sits in a little stone niche. Someone has left fresh candles here — someone who still believes the sun will rise.\n\nYou say a quiet prayer. For a moment the air is warmer.\n\n<color=#e6c67a>The party is Blessed for the next battle and regains 5 hit points.</color>");
                Game.I.GiveXP(25, "a moment of faith");
            });
            var cache = ctx.Prop("rock", () => Nature.Rock(77, 0.6f), sh + new Vector3(2.4f, 0, 1.2f), 30);
            var cc = ctx.Stash("shrine_cache", "Loose Stones", cache, new[] { "alchemist_fire", "holy_water" }, 8);
            cc.hidden = true; cc.perceptionDC = 12;

            // --- campfire clearing ---
            var camp = ctx.G3(-16, 2);
            ctx.Prop("campfire", CampfireMesh, camp, 0);
            Kit.PointLight(ctx.root, camp + Vector3.up * 0.8f, new Color(1f, .6f, .3f), 5f, 10f, true);
            var fire = FX.MakePS("campfire", camp + Vector3.up * 0.25f, ProcTex.FogPuff, true, 0, 0.8f, 0.8f, 0.45f, new Color(1f, .55f, .2f, .9f), new Color(.4f, .1f, 0, 0), -0.8f, 0.2f, ParticleSystemShapeType.Circle, 5f, true, 24f);
            fire.transform.SetParent(ctx.root, true);
            ctx.Prop("log", () => Nature.FallenLog(1), camp + new Vector3(2.2f, 0, 0.4f), 80, 0.5f);
            ctx.Prop("log", () => Nature.FallenLog(1), camp + new Vector3(-1.6f, 0, 1.8f), 20, 0.5f);
            var restGo = new GameObject("CampRest"); restGo.transform.SetParent(ctx.root); restGo.transform.position = camp;
            Interactable.AddBoxCollider(restGo, Vector3.up * 0.4f, new Vector3(1.6f, 0.8f, 1.6f));
            ctx.Add<RestSpot>("camp", "Campfire", restGo, 2.2f);

            // --- signpost ---
            var sp = ctx.G3(4.5f, 36);
            var sign = ctx.Prop("signpost", Props.Signpost, sp, -60, 1, Kit.ColliderKind.Box);
            ctx.Note("signpost", "Signpost", sign, "A Weathered Signpost", "One arm points ahead: <b>BAROVIA</b>.\nThe other points up the mountainside: <b>RAVENLOFT</b>.\n\nBeneath them, gouged into the wood with a knife, someone has added a third word:\n\n<b>BACK</b>");

            // --- the Gates of Barovia ---
            var gp = ctx.G3(0, 62);
            Buildings.BaroviaGates(ctx.root, gp - ctx.root.position, 0, out leafL, out leafR);
            foreach (var l in new[] { leafL, leafR }) { var bc = l.GetComponentInChildren<BoxCollider>(); if (bc) bc.gameObject.layer = Layers.Interact; }
            obsL = MakeObstacle(leafL, -1); obsR = MakeObstacle(leafR, 1);
            bool open = ctx.G.Flag("gates_open");
            if (open) { leafL.localRotation = Quaternion.Euler(0, -95, 0); leafR.localRotation = Quaternion.Euler(0, 95, 0); obsL.enabled = obsR.enabled = false; }
            else ctx.Trigger("gates", gp - new Vector3(0, 0, 9), 7f, () => Game.I.StartCoroutine(OpenGates()), null);
            ctx.Trigger("into_village", ctx.G3(1.5f, 108), 6f, () => Game.I.GoToArea("village", "south"));
            var fogWall = Kit.FogBank(ctx.root, new Vector3(1, ctx.GroundY(1, 112) + 1.5f, 112), new Vector3(18, 3, 6), new Color(.8f, .78f, .8f, .5f), 40, 7f, 0.1f);
            // a few lanterns hung by earlier travellers
            o.RoadLamp(0.1f, 1);
            o.RoadLamp(0.45f, -1, false);
            o.RoadLamp(0.69f, -1);
            o.RoadLamp(0.69f, 1);
        }

        NavMeshObstacle MakeObstacle(Transform leaf, int side)
        {
            var o = leaf.gameObject.AddComponent<NavMeshObstacle>();
            o.carving = true; o.shape = NavMeshObstacleShape.Box;
            o.center = new Vector3(-side * 1.65f, 2.3f, 0); o.size = new Vector3(3.3f, 4.6f, 0.4f);
            return o;
        }

        IEnumerator OpenGates()
        {
            Game.I.SetFlag("gates_open");
            Audio.Sfx.Play("door", 1f, 0.6f);
            UIRoot.I.Bark(Game.I.Selected, "...The gates are opening. By themselves.");
            float t = 0;
            while (t < 3.5f && leafL)
            {
                t += Time.deltaTime;
                float k = MathX.Smoothstep(0, 1, t / 3.5f);
                leafL.localRotation = Quaternion.Euler(0, -95 * k, 0);
                leafR.localRotation = Quaternion.Euler(0, 95 * k, 0);
                yield return null;
            }
            if (obsL) obsL.enabled = false; if (obsR) obsR.enabled = false;
            Game.I.journal.Update("mists", "Into the Mists", "Beyond the Gates of Barovia the road runs on toward a village. The gates opened for us by themselves — which is not the same as welcoming us.");
        }

        public static GameObject BuildShrine(AreaContext ctx, Vector3 p, float yaw)
        {
            var mb = new MeshBuilder(3) { uvScale = 0.8f };
            mb.AddRoughBox(0, new Vector3(0, 0.6f, 0), new Vector3(0.9f, 1.2f, 0.7f), 0.04f, 5);
            mb.AddRoughBox(0, new Vector3(0, 1.6f, 0), new Vector3(0.7f, 0.8f, 0.5f), 0.03f, 6);
            mb.Push(); mb.Translate(new Vector3(0, 2.0f, -0.35f)); mb.AddPrism(1, new Vector3(-0.6f, 0, 0), new Vector3(0, 0.45f, 0), new Vector3(0.6f, 0, 0), 0.7f); mb.Pop();
            mb.Push(); mb.Translate(new Vector3(0, 1.6f, 0.26f)); mb.Rotate(Quaternion.Euler(90, 0, 0)); mb.AddCylinder(2, Vector3.zero, 0.22f, 0.22f, 0.03f, 16); mb.Pop();
            for (int i = 0; i < 3; i++) mb.AddCylinder(2, new Vector3(-0.2f + i * 0.2f, 1.2f, 0.3f), 0.025f, 0.025f, 0.12f + i * 0.03f, 6);
            var go = Kit.FromBuilder(mb, new[] { Pal.Rock, Pal.Shingle, Pal.Gold }, ctx.root, "Shrine", Kit.ColliderKind.Box);
            go.transform.position = p; go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            for (int i = 0; i < 3; i++) Kit.Flame(go.transform, new Vector3(-0.2f + i * 0.2f, 1.34f + i * 0.03f, 0.3f), 0.03f);
            Kit.PointLight(go.transform, new Vector3(0, 1.5f, 0.6f), new Color(1f, .8f, .5f), 1.6f, 4f, false);
            return go;
        }

        public static (Mesh, Material[]) CampfireMesh()
        {
            var mb = new MeshBuilder(2);
            for (int i = 0; i < 9; i++) { float a = i / 9f * Mathf.PI * 2; mb.AddEllipsoid(0, new Vector3(Mathf.Cos(a) * 0.55f, 0.08f, Mathf.Sin(a) * 0.55f), new Vector3(0.16f, 0.12f, 0.14f), 5, 6); }
            for (int i = 0; i < 4; i++) { mb.Push(); mb.Rotate(Quaternion.Euler(0, i * 45, 70)); mb.AddCylinder(1, new Vector3(0, -0.35f, 0), 0.05f, 0.04f, 0.7f, 6); mb.Pop(); }
            return (mb.Build("campfire"), new[] { Pal.Rock, MatLib.Emissive(new Color(.2f, .1f, .05f), new Color(1.2f, .35f, .05f), TexId.Bark) });
        }

        public override void OnEnter(AreaContext ctx, bool first)
        {
            if (!first) return;
            Game.I.StartCoroutine(Intro());
        }

        IEnumerator Intro()
        {
            yield return new WaitForSeconds(1.2f);
            Game.I.journal.Update("mists", "Into the Mists", "A letter from a burgomaster named Kolyan Indirovich begged for help for his daughter. We followed the road east into a forest, and the mists closed behind us. The only way is forward.");
            Toast.Show("Click the ground to move. The party follows the selected character.", Theme.Parchment, 5f);
            yield return new WaitForSeconds(5.5f);
            Toast.Show("W A S D pans the camera, Q / E rotates, the mouse wheel zooms. Hold Tab to highlight things.", Theme.Parchment, 5f);
            yield return new WaitForSeconds(5.5f);
            var sel = Game.I.Selected;
            if (sel) UIRoot.I.Bark(sel, "The road's gone behind us. Just the mist... and the sound of our own hearts.");
        }
    }
}
