using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dungine.Rules;
using Dungine.UI;
using Dungine.Visual;
using Dungine.World;
using UnityEngine;

namespace Dungine
{
    /// <summary>
    /// The Village of Barovia: the hub. A muddy high street runs north from the southern road to a cobbled square,
    /// then bends west toward the valley; a lane climbs east to the church, another runs to the house on Mad Mary's
    /// lane, and the burgomaster's manor stands behind its iron fence on the west side.
    /// </summary>
    public class AreaVillage : AreaDef
    {
        public override string Id => "village";
        public override string Title => "Village of Barovia";
        public override string Subtitle => "Where the sun never quite rises";
        public override AtmosphereProfile Atmos => Game.I.nightfall ? Atmosphere.BaroviaNight : Atmosphere.BaroviaDay;
        public override string Music => Game.I.nightfall ? "strahd" : "explore";

        static readonly Vector2 Square = new Vector2(0, 0);
        public static readonly Vector3 GravePos = new Vector3(63f, 0, 34f);

        struct Plot { public Vector2 c; public float yaw; public Buildings.HouseSpec spec; public string name; }

        public override void Build(AreaContext ctx)
        {
            var o = new Outdoor(ctx) { seed = 7, hills = 2.2f, wallStart = 55f, wallSteep = 0.45f, roadWidth = 3.4f, forestFloorStart = 34f };
            o.road = new List<Vector2> { new Vector2(0, -118), new Vector2(1.5f, -80), new Vector2(0, -45), new Vector2(0, -12), Square, new Vector2(0, 14), new Vector2(-3, 38), new Vector2(-20, 58), new Vector2(-60, 68), new Vector2(-118, 72) };
            o.Lane(3.0f, new Vector2(12, 3), new Vector2(40, 12), new Vector2(72, 26), new Vector2(118, 38));     // east road toward the castle
            o.Lane(2.2f, new Vector2(40, 12), new Vector2(47, 22), new Vector2(48, 29));                            // church path
            o.Lane(2.4f, new Vector2(0, -50), new Vector2(20, -50), new Vector2(40, -50));                          // Mad Mary's lane
            o.Lane(2.2f, new Vector2(-2, 30), new Vector2(-37, 30));                                                // manor drive
            o.paved.Add((Square, 14f));
            o.paved.Add((new Vector2(-37 - 3, 30), 3.2f));
            // the church stands on a low rise
            o.extraHeight = (x, z) => 1.6f * MathX.Smoothstep(26f, 12f, Vector2.Distance(new Vector2(x, z), new Vector2(52, 44)));

            var plots = new List<Plot>();
            void P(float x, float z, float yaw, int seed, float w = 7, float d = 6, int floors = 2, bool dark = false, bool slate = false, float lit = .3f)
                => plots.Add(new Plot { c = new Vector2(x, z), yaw = yaw, spec = new Buildings.HouseSpec { w = w, d = d, floors = floors, seed = seed, dark = dark, slate = slate, litChance = Game.I.nightfall ? lit + .2f : lit, boardedChance = .35f } });
            // high street, south of the square
            P(-10.5f, -26, 90, 101); P(10.5f, -23, -90, 102, 8, 6); P(-11, -64, 90, 103, 6.5f, 6, 1); P(11.5f, -70, -90, 104); P(-10, -93, 88, 105, 7, 6, 1);
            P(10, -100, -92, 106, 6, 5.5f, 1, true);
            // around the square
            P(-6, 20, 180, 107, 7, 6); P(12, 18, -140, 108, 6.5f, 6, 2, false, true);
            // Mad Mary's lane
            P(8.5f, -39, 180, 109, 6, 5.5f); P(22, -60.5f, 0, 110, 7, 6, 2); P(30, -39.5f, 180, 111, 7, 6, 2, true);
            // west road
            P(-22, 72, 205, 112, 7, 6); P(-45, 78, 185, 113, 6.5f, 5.5f, 1); P(-80, 81, 180, 114); P(-60, 58, 5, 115, 7, 6, 1, true);
            // east road
            P(27, 20, 200, 116, 6.5f, 6); P(62, 13, 20, 117, 7, 6, 1);
            { var mm = plots[9]; mm.name = "MadMary"; plots[9] = mm; }

            var tavernC = new Vector2(-20f, 3f); var shopC = new Vector2(19.5f, -6f);
            var manorC = new Vector2(-50f, 30f); var churchC = new Vector2(52f, 44f); var dhC = new Vector2(48.5f, -50f);
            foreach (var p in plots) o.flats.Add((p.c, Mathf.Max(p.spec.w, p.spec.d) * 0.62f));
            o.flats.Add((tavernC, 6.5f)); o.flats.Add((shopC, 5.5f)); o.flats.Add((manorC, 12f)); o.flats.Add((dhC, 6f));
            foreach (var p in plots) o.blockers.Add(new Rect(p.c.x - 5, p.c.y - 5, 10, 10));
            o.blockers.Add(new Rect(manorC.x - 12, manorC.y - 16, 26, 32));
            o.blockers.Add(new Rect(churchC.x - 18, churchC.y - 20, 36, 38));
            o.blockers.Add(new Rect(dhC.x - 8, dhC.y - 8, 16, 16));
            o.blockers.Add(new Rect(tavernC.x - 8, tavernC.y - 7, 14, 14));
            o.blockers.Add(new Rect(shopC.x - 6, shopC.y - 6, 12, 12));
            o.BuildTerrain();

            // ---------------------------------------------------------------- buildings
            var root = ctx.root;
            foreach (var p in plots)
            {
                var pos = ctx.G3(p.c.x, p.c.y);
                var info = Buildings.House(p.spec, root, pos - Vector3.up * 0.1f, p.yaw, p.name ?? "House");
                if (p.name == "MadMary") MadMaryDoor(ctx, info);
                else KnockDoor(ctx, info, p.spec.seed);
            }

            // Blood of the Vine tavern
            var tav = Buildings.House(new Buildings.HouseSpec { w = 11, d = 9, floors = 2, seed = 201, litChance = .9f, boardedChance = 0, sign = true, signText = "Blood of the Vine", porch = true }, root, ctx.G3(tavernC.x, tavernC.y) - Vector3.up * 0.1f, 90, "Tavern");
            ctx.Door("tavern_door", "Blood of the Vine Tavern", tav.doorWorld - tav.doorFacing * 0.9f, 90, "tavern", "door", new Vector3(1.6f, 2.4f, 0.6f));
            ctx.Spawn("tavern", tav.doorWorld + tav.doorFacing * 0.6f, 90);
            ctx.Prop("barrel", Props.Barrel, tav.doorWorld + new Vector3(0.3f, 0, 2.2f), 10, 1, Kit.ColliderKind.Capsule);
            ctx.Prop("barrel", Props.Barrel, tav.doorWorld + new Vector3(-0.5f, 0, 2.7f), 50, 1, Kit.ColliderKind.Capsule);
            ctx.Prop("bench1.8", () => Props.Bench(1.8f), tav.doorWorld + new Vector3(0.4f, 0, -2.4f), 0, 1, Kit.ColliderKind.Box);

            // Bildrath's Mercantile
            var shop = Buildings.House(new Buildings.HouseSpec { w = 9, d = 8, floors = 2, seed = 202, litChance = .7f, boardedChance = 0, sign = true, signText = "Bildrath's Mercantile", slate = true }, root, ctx.G3(shopC.x, shopC.y) - Vector3.up * 0.1f, -90, "Mercantile");
            ctx.Door("shop_door", "Bildrath's Mercantile", shop.doorWorld - shop.doorFacing * 0.9f, -90, "shop", "door", new Vector3(1.6f, 2.4f, 0.6f));
            ctx.Spawn("shop", shop.doorWorld + shop.doorFacing * 0.6f, -90);
            ctx.Prop("crate0.8", () => Props.Crate(0.8f), shop.doorWorld + new Vector3(-0.2f, 0, 2.3f), 15, 1, Kit.ColliderKind.Box);
            ctx.Prop("crate0.6", () => Props.Crate(0.6f), shop.doorWorld + new Vector3(-0.3f, 0.8f, 2.3f), 40);

            // the burgomaster's manor
            var manor = Buildings.Mansion(root, ctx.G3(manorC.x, manorC.y) - Vector3.up * 0.2f, 90);
            ctx.Door("manor_door", "Burgomaster's Manor", manor.doorWorld - manor.doorFacing * 1.6f, 90, "mansion", "door", new Vector3(2f, 2.6f, 0.8f));
            ctx.Spawn("mansion", manor.doorWorld + manor.doorFacing * 0.4f, 90);
            float fx0 = -64, fx1 = -36.5f, fz0 = 14, fz1 = 46;
            Buildings.IronFence(root, ctx.G3(fx1, fz0), ctx.G3(fx1, fz1), 1.9f, true);
            Buildings.IronFence(root, ctx.G3(fx0, fz0), ctx.G3(fx1, fz0), 1.9f);
            Buildings.IronFence(root, ctx.G3(fx0, fz1), ctx.G3(fx1, fz1), 1.9f);
            Buildings.IronFence(root, ctx.G3(fx0, fz0), ctx.G3(fx0, fz1), 1.9f);
            foreach (var gz in new[] { 28.6f, 31.4f }) ctx.Prop("gatepost", GatePost, ctx.G3(fx1, gz), 0, 1, Kit.ColliderKind.Box);
            var cage = ctx.Prop("garlic", GarlicStrings, manor.doorWorld + Vector3.up * 3.1f - manor.doorFacing * 2.6f, 90);
            ctx.Note("manor_marks", "Scored Door", ctx.Prop("scratches", Scratches, manor.doorWorld - manor.doorFacing * 3.05f + Vector3.up * 1.3f, 90), "The Manor Door",
                "The heavy oak door is scored with claw marks, hundreds of them, layered over one another like the rings of a tree. Some are old and grey. Some are pale and fresh. None of them reach higher than a wolf could stand.\n\nGarlic hangs over the lintel in braids gone black with age.");

            // the church on the rise
            var church = Buildings.Church(root, ctx.G3(churchC.x, churchC.y) - Vector3.up * 0.3f, 180);
            ctx.Door("church_door", "Church", church.doorWorld - church.doorFacing * 1.5f, 180, "church", "door", new Vector3(2.2f, 3f, 0.8f));
            ctx.Spawn("church", church.doorWorld + church.doorFacing * 0.4f, 180);
            Graveyard(ctx, churchC);

            // the house on Mad Mary's lane
            var dh = Buildings.DeathHouse(root, ctx.G3(dhC.x, dhC.y) - Vector3.up * 0.15f, -90);
            var dhDoor = ctx.Door("dh_door", "The Durst House", dh.doorWorld - dh.doorFacing * 2.2f, -90, "deathhouse", "door", new Vector3(1.6f, 2.4f, 0.8f));
            dhDoor.requireFlag = "dh_open";
            dhDoor.requireText = "The door is shut tight. No handle turns, no key fits — as though the house is not ready to receive you.";
            ctx.Spawn("deathhouse", dh.doorWorld + dh.doorFacing * 0.3f, -90);
            Buildings.IronFence(root, ctx.G3(42.2f, -56f), ctx.G3(42.2f, -44f), 1.5f, true);
            ctx.Prop("deadtree", () => Nature.DeadTree(41), ctx.G3(44.5f, -57.5f), 30, 0.8f, Kit.ColliderKind.Capsule);

            // buildings open a see-through hole around the party when they would hide it
            foreach (Transform ch in root)
                if (ch.name == "House" || ch.name == "MadMary" || ch.name == "Tavern" || ch.name == "Mercantile" || ch.name == "Mansion" || ch.name == "Church" || ch.name == "DeathHouse")
                    Occluders.RegisterGroup(ch);

            // ---------------------------------------------------------------- square
            var wellP = ctx.G3(0, 0);
            var well = ctx.Prop("well", Props.Well, wellP, 20, 1, Kit.ColliderKind.Capsule);
            ctx.Use("well", "Village Well", well, "Look in", (u, s) =>
            {
                if (!Game.I.Flag("well_ring"))
                {
                    UIRoot.I.Bark(u, "Something glints down there, caught in the bucket chain...");
                    if (u.c.SkillBonus(Skill.SleightOfHand) + UnityEngine.Random.Range(1, 21) >= 10)
                    {
                        Game.I.SetFlag("well_ring"); Game.I.stash.Add("gold_ring"); Audio.Sfx.Play("pickup");
                        Toast.Show($"{u.Name} fishes out a Gold Signet.", Theme.Gold);
                    }
                    else Toast.Show("It slips back into the dark. Try again.", Theme.TextDim);
                }
                else UIRoot.I.Bark(u, "Just black water, a long way down.");
            });
            var board = ctx.Prop("noticeboard", Props.NoticeBoard, ctx.G3(-5.5f, -9.5f), 200, 1, Kit.ColliderKind.Box);
            ctx.Note("notices", "Notice Board", board, "Notices",
                "<b>BY ORDER OF THE BURGOMASTER</b>\nAll doors barred at dusk. No lights to be shown in windows facing the castle road.\n\n" +
                "<b>WANTED</b>\nWolf pelts, 2 coppers each. Enquire at the Mercantile.\n\n" +
                "<b>LOST</b>\nGrey goat, one horn. Answers to nothing. — Yefim\n\n" +
                "Across the bottom, scrawled in charcoal and half torn away: <i>HE IS ALWAYS WATCHING</i>");
            ctx.Obstacle("stall_r", () => Props.MarketStall(new Color(.45f, .12f, .1f)), ctx.G3(7.5f, 8.5f), 160);
            ctx.Obstacle("stall_g", () => Props.MarketStall(new Color(.25f, .3f, .2f)), ctx.G3(-8.5f, 8f), 200);
            ctx.Obstacle("stall_b", () => Props.MarketStall(new Color(.2f, .22f, .3f)), ctx.G3(9f, -9f), 20);
            var stallCrate = ctx.Prop("crate0.7", () => Props.Crate(0.7f), ctx.G3(9.8f, -7.6f), 10, 1, Kit.ColliderKind.Box);
            ctx.Stash("stall_crate", "Abandoned Stall Crate", stallCrate, new[] { "garlic", "garlic", "candle" }, 3);
            ctx.Prop("cart", () => Props.Cart(false), ctx.G3(-9f, -3f), 75, 1, Kit.ColliderKind.Box);
            ctx.Prop("haybale", Props.Haybale, ctx.G3(-10.5f, -5.2f), 10, 1, Kit.ColliderKind.Box);
            ctx.Prop("statue", () => Props.Statue(true), ctx.G3(4f, 4.5f), 200, 0.9f, Kit.ColliderKind.Capsule);

            // lamps along the streets
            foreach (var (t, side) in new[] { (0.08f, 1), (0.22f, -1), (0.36f, 1), (0.46f, -1), (0.58f, 1), (0.68f, -1), (0.8f, 1), (0.93f, -1) }) o.RoadLamp(t, side, !Game.I.nightfall || t < 0.7f);
            foreach (var lp in new[] { new Vector2(12.5f, 9), new Vector2(-12, -10.5f), new Vector2(-12.5f, 10), new Vector2(12, -12) }) o.Lamp(lp.x, lp.y, Mathf.Atan2(-lp.x, -lp.y) * Mathf.Rad2Deg - 90);

            // fences, woodpiles and clutter between houses
            Buildings.WoodFence(root, ctx.G3(-16, -35), ctx.G3(-16, -18), 1);
            Buildings.WoodFence(root, ctx.G3(16, -32), ctx.G3(16, -14), 2);
            Buildings.WoodFence(root, ctx.G3(15, -76), ctx.G3(15, -62), 3);
            Buildings.WoodFence(root, ctx.G3(-15, -70), ctx.G3(-15, -57), 4);
            ctx.Prop("woodpile", Props.Woodpile, ctx.G3(-14.5f, -30), 90, 1, Kit.ColliderKind.Box);
            ctx.Prop("woodpile", Props.Woodpile, ctx.G3(15.5f, -66), -90, 1, Kit.ColliderKind.Box);
            ctx.Prop("cart_broken", () => Props.Cart(true), ctx.G3(-14, 45), 20, 1, Kit.ColliderKind.Box);
            ctx.Prop("haybale", Props.Haybale, ctx.G3(-49, 67), 60, 1, Kit.ColliderKind.Box);

            // nature at the edges
            o.Forest(520, 70, 12f);
            o.Grass(1600);
            o.RoadFog(40f, new Color(.72f, .72f, .76f, .07f));
            Buildings.CastleRavenloft(root, new Vector3(190, 20, 190), 225, 1.25f);

            // ---------------------------------------------------------------- spawns & exits
            ctx.Spawn("south", o.OnRoad(0.05f), 0);
            ctx.Spawn("west", ctx.G3(-100, 71.5f), 95);
            ctx.Spawn("square", ctx.G3(0, -8), 0);
            ctx.Spawn("graveyard", ctx.G3(48f, 24f), 20);
            ctx.Trigger("exit_south", ctx.G3(0.5f, -114), 5f, () => Game.I.GoToArea("svalich_road", "from_village")).repeat = true;
            var westExit = ctx.Trigger("exit_west", ctx.G3(-110, 72), 5f, TryLeaveWest);
            if (westExit) westExit.repeat = true;
            var east = ctx.Trigger("exit_east", ctx.G3(108, 36), 6f, () =>
            {
                UIRoot.I.Bark(Game.I.Selected, "The castle road. The fog up there is thick as wool, and it moves against the wind. Not yet.");
                PartyController.I.MoveParty(ctx.G3(98, 33));
            });
            if (east) east.repeat = true;
            Kit.FogBank(root, new Vector3(0, ctx.GroundY(0, -119) + 1f, -119), new Vector3(30, 3, 6), new Color(.8f, .8f, .84f, .5f), 40, 8f, 0.1f);
            Kit.FogBank(root, new Vector3(-118, ctx.GroundY(-118, 72) + 1f, 72), new Vector3(6, 3, 30), new Color(.8f, .8f, .84f, .5f), 40, 8f, 0.1f);
            Kit.FogBank(root, new Vector3(116, ctx.GroundY(116, 38) + 1.5f, 38), new Vector3(8, 6, 34), new Color(.75f, .75f, .8f, .6f), 50, 12f, 0.1f);

            // ---------------------------------------------------------------- people
            BuildPeople(ctx);
        }

        // ---------------------------------------------------------------------------------- pieces

        static void Graveyard(AreaContext ctx, Vector2 churchC)
        {
            var root = ctx.root;
            float x0 = 30, x1 = 74, z0 = 26, z1 = 64;
            Buildings.StoneWall(root, ctx.G3(x0, z0), ctx.G3(x0, z1), 1.0f, 11);
            Buildings.StoneWall(root, ctx.G3(x0, z1), ctx.G3(x1, z1), 1.0f, 12);
            Buildings.StoneWall(root, ctx.G3(x1, z0), ctx.G3(x1, z1), 1.0f, 13);
            Buildings.StoneWall(root, ctx.G3(x0, z0), ctx.G3(44, z0), 1.0f, 14);
            Buildings.StoneWall(root, ctx.G3(52, z0), ctx.G3(x1, z0), 1.0f, 15);
            var rnd = new System.Random(66);
            string[] epitaphs =
            {
                "PETYA — HE WENT TO SEE THE SUNRISE",
                "Here lies Anca, who kept her lamp lit. It did not help.",
                "VADIM, AGED 9. RETURNED TO US. BURIED AGAIN.",
                "Beloved husband. We did not open the door.",
                "Taken by the mists. Stone set without him.",
                "Olya and Mischa, together as they wished.",
            };
            int ep = 0;
            for (float gx = 34; gx < 72; gx += 3.2f)
                for (float gz = 29; gz < 62; gz += 3.4f)
                {
                    if (Vector2.Distance(new Vector2(gx, gz), churchC) < 12.5f) continue;
                    if (gx > 44 && gx < 52 && gz < 34) continue; // path
                    if (Vector2.Distance(new Vector2(gx, gz), new Vector2(GravePos.x, GravePos.z)) < 3f) continue;
                    if (rnd.NextDouble() < 0.28) continue;
                    var p = ctx.G3(gx + (float)(rnd.NextDouble() - .5) * 1.2f, gz + (float)(rnd.NextDouble() - .5) * 1.2f);
                    int kind = rnd.Next(4);
                    var g = ctx.Prop("grave" + kind + "_" + (int)gx % 3, () => Props.Gravestone(kind, (int)gx), p, 180 + (float)(rnd.NextDouble() - .5) * 14, 1, Kit.ColliderKind.Box);
                    g.transform.rotation *= Quaternion.Euler((float)(rnd.NextDouble() - .5) * 8, 0, (float)(rnd.NextDouble() - .5) * 8);
                    if (rnd.NextDouble() < 0.45) ctx.Prop("gravemound", Props.GraveMound, p + new Vector3(0, 0, 1.1f), 0);
                    if (ep < epitaphs.Length && rnd.NextDouble() < 0.25)
                    {
                        ctx.Note("epitaph" + ep, "Gravestone", g, "Gravestone", epitaphs[ep]);
                        ep++;
                    }
                }
            // the burgomaster's grave, waiting
            var gp = ctx.G3(GravePos.x, GravePos.z);
            if (!Game.I.Flag("kolyan_buried"))
            {
                ctx.Prop("open_grave", OpenGrave, gp, 0, 1, Kit.ColliderKind.Box);
                ctx.Marker("grave", gp);
            }
            else
            {
                ctx.Prop("gravemound", Props.GraveMound, gp, 0);
                var st = ctx.Prop("grave2_x", () => Props.Gravestone(2, 99), gp + new Vector3(0, 0, -1.2f), 180, 1.1f, Kit.ColliderKind.Box);
                ctx.Note("kolyan_stone", "Burgomaster's Grave", st, "Kolyan Indirovich", "KOLYAN INDIROVICH\nBURGOMASTER OF BAROVIA\n\nHe kept the doors barred and the lamps lit, and he did not let them have her.");
            }
            ctx.Prop("deadtree", () => Nature.DeadTree(77), ctx.G3(70, 58), 10, 1.1f, Kit.ColliderKind.Capsule);
            ctx.Prop("deadtree", () => Nature.DeadTree(78), ctx.G3(34, 58), 120, 0.9f, Kit.ColliderKind.Capsule);
        }

        static void KnockDoor(AreaContext ctx, BuildingInfo info, int seed)
        {
            var go = new GameObject("Door"); go.transform.SetParent(ctx.root, false);
            go.transform.position = info.doorWorld - info.doorFacing * 1.1f + Vector3.up * 1.1f;
            Interactable.AddBoxCollider(go, Vector3.zero, new Vector3(1.3f, 2.2f, 0.4f));
            string[] replies =
            {
                "Silence. Then, very close to the other side of the door, someone holds their breath.",
                "\"Go away! We have nothing! We've given everything already!\"",
                "A child starts to speak and is hushed. A bolt is quietly drawn — locking, not opening.",
                "\"Is it day? Is it still day?\" an old man asks through the keyhole. He does not wait for an answer.",
                "Nobody answers. Through a gap in the boards you see a table set for four, and dust on every plate.",
                "\"Try the tavern,\" says a tired woman's voice. \"The Vistani girls will talk to anyone. We won't.\"",
            };
            string r = replies[Mathf.Abs(seed) % replies.Length];
            ctx.Use("knock" + seed, "Barred Door", go, "Knock", (u, s) => { Audio.Sfx.Play("door", 0.5f, 1.3f); ReadWindow.Show("A Barred Door", r); }, true);
        }

        static void MadMaryDoor(AreaContext ctx, BuildingInfo info)
        {
            var go = new GameObject("MaryDoor"); go.transform.SetParent(ctx.root, false);
            go.transform.position = info.doorWorld - info.doorFacing * 1.1f + Vector3.up * 1.1f;
            Interactable.AddBoxCollider(go, Vector3.zero, new Vector3(1.3f, 2.2f, 0.4f));
            ctx.Use("mary_door", "Weeping House", go, "Knock", (u, s) => Dialogue.DialogueRunner.I.Begin("mad_mary", null, u));
            ctx.Trigger("mary_wail", info.doorWorld, 9f, () =>
            {
                Audio.Sfx.Play("whisper", 0.6f, 0.8f);
                UIRoot.I.Bark(Game.I.Selected, "Someone is sobbing in that house. Howling, almost.");
            });
        }

        static void TryLeaveWest()
        {
            var g = Game.I;
            if (g.Flag("ireena_escort") && g.Flag("kolyan_buried")) { g.GoToArea("tser_pool", "east"); return; }
            if (g.Flag("ireena_escort")) UIRoot.I.Bark(g.Selected, "Not before the burgomaster is in the ground. Ireena won't leave him like that — and I don't blame her.");
            else UIRoot.I.Bark(g.Selected, "The road west runs on toward Vallaki. We came here for the burgomaster's daughter; we're not leaving without seeing this through.");
            PartyController.I.MoveParty(g.area.G3(-100, 71.5f));
        }

        void BuildPeople(AreaContext ctx)
        {
            var g = Game.I;
            // Rose and Thorn, at the gate of the Durst house
            if (!g.Flag("dh_done"))
            {
                var rose = ctx.NPC("rose", "Rose", new Appearance { race = RaceId.Human, bodyType = 1, child = true, skin = new Color(.86f, .74f, .66f), hair = new Color(.5f, .33f, .18f), hairStyle = 7, eyes = new Color(.35f, .5f, .6f), cloth1 = new Color(.48f, .5f, .56f), cloth2 = new Color(.3f, .3f, .34f) }, new GearLook(), ctx.G3(38.8f, -48.6f), -90, "rose_thorn");
                var thorn = ctx.NPC("thorn", "Thorn", new Appearance { race = RaceId.Human, bodyType = 0, child = true, heightAdj = -0.8f, skin = new Color(.86f, .74f, .66f), hair = new Color(.45f, .3f, .16f), hairStyle = 1, eyes = new Color(.35f, .5f, .6f), cloth1 = new Color(.32f, .36f, .44f), cloth2 = new Color(.25f, .24f, .26f) }, new GearLook(), ctx.G3(38.6f, -51.3f), -80, "rose_thorn");
                if (thorn) thorn.transform.localScale = Vector3.one * 0.86f;
            }
            // villagers
            var rnd = new System.Random(5150);
            (Vector2 p, float yaw, string bark)[] folk =
            {
                (new Vector2(-4.5f, -3.5f), 30, "\"Don't stand in the open like that. He sees everything from up there.\""),
                (new Vector2(6.5f, 5f), 220, "\"Bildrath's got what you need, if you've the coin. He's got what everyone needs. That's the trouble.\""),
                (new Vector2(-6, -40f), 80, "\"Another lot come through the gates? The mists let you in. They won't let you out. You'll see.\""),
                (new Vector2(3f, 32f), 190, "\"The burgomaster's dead three days and nobody will dig the grave. Father Donavich won't come out of the church.\""),
            };
            if (!g.nightfall)
                for (int i = 0; i < folk.Length; i++)
                {
                    var f = folk[i];
                    var a = ctx.NPC("villager" + i, i % 2 == 0 ? "Barovian Villager" : "Gaunt Villager", Monsters.Villager(rnd, i % 2 == 1), new GearLook(), ctx.G3(f.p.x, f.p.y), f.yaw, null, MotionStyle.Normal);
                    if (a) a.barkOnClick = f.bark;
                }
            // the burial party waits at the open grave
            if (g.Flag("burial_ready") && !g.Flag("kolyan_buried"))
            {
                var gp = GravePos;
                var don = ctx.NPC("donavich_g", "Father Donavich", Campaign.DonavichLook, new GearLook { armor = ArmorVisual.Robe }, ctx.G3(gp.x, gp.z + 1.9f), 180, "burial");
                ctx.NPC("ireena_g", "Ireena Kolyanovna", Campaign.IreenaLook, Campaign.IreenaGear, ctx.G3(gp.x - 1.6f, gp.z + 0.3f), 90, null);
                ctx.NPC("ismark_g", "Ismark Kolyanovich", Campaign.IsmarkLook, Campaign.IsmarkGear, ctx.G3(gp.x - 1.6f, gp.z - 1f), 70, null);
                ctx.Prop("coffin_closed", () => Props.Coffin(false), ctx.G3(gp.x + 1.9f, gp.z), 0, 1, Kit.ColliderKind.Box);
                ctx.Trigger("burial_start", ctx.G3(gp.x, gp.z - 3f), 5f, () =>
                {
                    var d = Game.I.FindActor("donavich_g");
                    if (d) Dialogue.DialogueRunner.I.Begin("burial", d, Game.I.Selected);
                });
                ctx.Trigger("burial_hint", ctx.G3(47f, 22f), 6f, () => UIRoot.I.Bark(Game.I.Selected, "There — by the open grave. They're waiting for us."));
            }
            // after the burial, Ireena travels with the party
            if (g.Flag("ireena_escort") && !g.Flag("chapter1_done"))
            {
                var ire = ctx.NPC("ireena_f", "Ireena Kolyanovna", Campaign.IreenaLook, Campaign.IreenaGear, ctx.G3(GravePos.x - 2f, GravePos.z - 3f), 0, "ireena_road");
                if (ire) ire.gameObject.AddComponent<Follower>();
            }
            // at night the dead walk the high street
            if (g.nightfall && !g.Flag("night_street_cleared"))
            {
                ctx.Monster("zombie_n1", "zombie", ctx.G3(-2, -30), 0, "night_street", true, 10f);
                ctx.Monster("zombie_n2", "zombie", ctx.G3(3, -33), 20, "night_street", true, 10f);
                ctx.Monster("zombie_n3", "zombie", ctx.G3(-1, -36), 340, "night_street", true, 10f);
            }
        }

        // ---------------------------------------------------------------------------------- small meshes

        static (Mesh, Material[]) GatePost()
        {
            var mb = new MeshBuilder(2);
            mb.AddRoughBox(0, new Vector3(0, 1.2f, 0), new Vector3(0.6f, 2.4f, 0.6f), 0.03f, 5);
            mb.AddBox(0, new Vector3(0, 2.5f, 0), new Vector3(0.75f, 0.2f, 0.75f));
            mb.AddEllipsoid(1, new Vector3(0, 2.8f, 0), new Vector3(0.22f, 0.22f, 0.22f), 6, 8);
            return (mb.Build("gatepost"), new[] { Pal.Stone, Pal.Iron });
        }

        static (Mesh, Material[]) GarlicStrings()
        {
            var mb = new MeshBuilder(1);
            for (int i = 0; i < 5; i++)
            {
                float x = -0.8f + i * 0.4f;
                for (int k = 0; k < 4; k++) mb.AddEllipsoid(0, new Vector3(0.05f, -k * 0.13f - (i % 2) * 0.05f, x), new Vector3(0.06f, 0.07f, 0.06f), 4, 6);
            }
            return (mb.Build("garlic"), new[] { MatLib.Lit(new Color(.62f, .58f, .48f), null, .2f) });
        }

        static (Mesh, Material[]) Scratches()
        {
            var mb = new MeshBuilder(1);
            var rnd = new System.Random(8);
            for (int i = 0; i < 26; i++)
            {
                float y = (float)rnd.NextDouble() * 1.4f - 0.9f, z = (float)(rnd.NextDouble() - .5) * 1.4f;
                mb.Push(); mb.Translate(new Vector3(0.02f, y, z)); mb.Rotate(Quaternion.Euler((float)(rnd.NextDouble() - .5) * 40, 0, 0));
                mb.AddBox(0, Vector3.zero, new Vector3(0.01f, 0.35f + (float)rnd.NextDouble() * 0.3f, 0.02f));
                mb.Pop();
            }
            return (mb.Build("scratches"), new[] { MatLib.Lit(new Color(.55f, .45f, .35f), null, .1f) });
        }

        static (Mesh, Material[]) OpenGrave()
        {
            var mb = new MeshBuilder(2) { uvScale = 0.8f };
            // a dark pit framed by spoil heaps, with a spade stuck in the dirt
            mb.AddBox(1, new Vector3(0, 0.01f, 0), new Vector3(1.1f, 0.02f, 2.2f));
            mb.AddEllipsoid(0, new Vector3(1.2f, 0.05f, 0), new Vector3(0.6f, 0.4f, 1.3f), 6, 9);
            mb.AddEllipsoid(0, new Vector3(-1.1f, 0.0f, 0.3f), new Vector3(0.4f, 0.25f, 0.8f), 6, 9);
            mb.Push(); mb.Translate(new Vector3(1.3f, 0.3f, 0.6f)); mb.Rotate(Quaternion.Euler(12, 0, -10));
            mb.AddCylinder(0, Vector3.zero, 0.025f, 0.025f, 1.1f, 6); mb.AddBox(0, new Vector3(0, -0.1f, 0), new Vector3(0.22f, 0.3f, 0.03f));
            mb.Pop();
            return (mb.Build("opengrave"), new[] { Pal.Dirt, Pal.Black });
        }

        // ---------------------------------------------------------------------------------- arrival

        public override void OnEnter(AreaContext ctx, bool first)
        {
            if (first)
            {
                Game.I.journal.Update("mists", "Into the Mists", "We reached the Village of Barovia: shuttered houses, empty stalls, and people who will not meet our eyes. The burgomaster's letter spoke of his manor on the west side of the village.", QuestState.Done);
                Game.I.journal.Update("burgomaster", "The Burgomaster's Letter", "Find Kolyan Indirovich, burgomaster of Barovia, who wrote asking for help. Someone in the tavern on the square may know where to find him.");
                Game.I.GiveXP(50, "reaching the village");
                Game.I.StartCoroutine(ArrivalBark());
            }
        }

        IEnumerator ArrivalBark()
        {
            yield return new WaitForSeconds(2.5f);
            var s = Game.I.Selected;
            if (s) UIRoot.I.Bark(s, "Not a light in a single window. They know we're here — they're just not opening up.");
        }
    }
}
