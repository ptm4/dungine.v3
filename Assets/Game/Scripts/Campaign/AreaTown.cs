using System.Collections.Generic;
using System.Linq;
using Dungine.Rules;
using Dungine.UI;
using Dungine.Visual;
using Dungine.World;
using UnityEngine;

namespace Dungine
{
    /// <summary>The Blood of the Vine: a low, smoky common room with a bar along one wall and a hearth on the other.</summary>
    public class AreaTavern : AreaDef
    {
        public override string Id => "tavern";
        public override string Title => "Blood of the Vine";
        public override string Subtitle => "The only warm room in Barovia";
        public override bool Interior => true;
        public override AtmosphereProfile Atmos => Atmosphere.Interior;
        public override string Music => "tavern";
        public override string Ambience => "tavern";

        public override void Build(AreaContext ctx)
        {
            var b = new Indoor(ctx) { height = 3.1f, wallIn = Pal.Plaster, floor = Pal.FloorBoards };
            b.Void();
            // common room 14 x 10, back room (kitchen/stores) behind the bar to the north
            b.Room(0, 0, 14, 10, new Opening(2, 0, 1.6f), new Opening(0, 4.5f, 1.2f), new Opening(3, -2.5f, 1.4f, true), new Opening(3, 2.5f, 1.4f, true), new Opening(2, -4.5f, 1.6f, true), new Opening(2, 4.5f, 1.6f, true));
            b.Room(4.5f, 7.5f, 5, 5, new Opening(2, 0, 1.2f));

            b.DoorLeaf(0.8f, -5.02f, 0, 1.4f, true);
            b.Exit("exit", "Out to the Square", 0, -5.3f, 0, "village", "tavern", 1.8f);
            ctx.Spawn("door", new Vector3(0, 0, -3.8f), 0);

            // the bar
            b.Prop("bar6", () => Props.BarCounter(6f), 2.2f, 3.4f, 0);
            b.Prop("keg", Props.Keg, -0.5f, 4.55f, 90, 1, Kit.ColliderKind.Box, 0.0f);
            b.Prop("keg", Props.Keg, 0.6f, 4.55f, 90, 1, Kit.ColliderKind.Box, 0.0f);
            b.Prop("keg", Props.Keg, 0.05f, 4.55f, 90, 1, Kit.ColliderKind.None, 0.72f);
            b.Prop("shelf", () => Props.Bookshelf(3), 3.6f, 4.8f, 180);
            b.Prop("barrel", Props.Barrel, 5.9f, 4.4f, 0, 1, Kit.ColliderKind.Capsule);
            b.Candle(1.2f, 1.18f, 3.4f); b.Candle(3.8f, 1.18f, 3.4f, 1f);

            // hearth on the west wall
            b.Fireplace(-6.75f, 0.5f, 90);
            b.Prop("rug", () => Props.Rug(3f, 2.2f, new Color(.4f, .12f, .1f)), -4.8f, 0.5f, 90, 1, Kit.ColliderKind.None, 0.01f);
            b.Prop("chair_f", () => Props.Chair(true), -5f, -0.7f, 60);

            // tables
            void Table(float x, float z, float yaw, int chairs)
            {
                b.Prop("table1.4", () => Props.Table(1.4f, 0.9f), x, z, yaw);
                b.Candle(x, 0.86f, z, 0.9f, 3.5f);
                for (int i = 0; i < chairs; i++)
                {
                    float a = yaw + i * 360f / chairs;
                    var p = Quaternion.Euler(0, a, 0) * new Vector3(0, 0, 0.95f);
                    b.Prop("chair", () => Props.Chair(false), x + p.x, z + p.z, a + 180);
                }
            }
            Table(-3.2f, -2.4f, 0, 0);   // the Vistani table (benches)
            b.Prop("bench1.8", () => Props.Bench(1.8f), -3.2f, -3.25f, 0);
            b.Prop("bench1.8", () => Props.Bench(1.8f), -3.2f, -1.55f, 0);
            Table(3.3f, -2.2f, 0, 3);
            Table(-2.2f, 2.3f, 90, 0);   // Ismark's table
            b.Prop("chair", () => Props.Chair(false), -2.2f, 3.2f, 180);
            b.Prop("chair", () => Props.Chair(false), -3.15f, 2.3f, 90);
            b.Prop("wine", WineBottles, -2.0f, 2.3f, 30, 1, Kit.ColliderKind.None, 0.8f);

            b.Sconce(-6.85f, -3.5f, 90); b.Sconce(6.85f, -3f, -90); b.Sconce(6.85f, 1.5f, -90); b.Sconce(-2f, 4.85f, 180);
            // warm fill
            b.Fill(0, 0, new Color(1f, .7f, .45f), 0.9f, 14f);
            b.Fill(4.5f, 7.5f, new Color(1f, .7f, .45f), 0.5f, 6f);
            b.Prop("crate0.8", () => Props.Crate(0.8f), 3.2f, 8.8f, 10);
            b.Prop("sack", Props.Sack, 5.8f, 9.1f, 70);
            var stores = b.Prop("barrel", Props.Barrel, 6.2f, 6.2f, 0, 1, Kit.ColliderKind.Capsule);
            ctx.Stash("tavern_stores", "Wine Barrel", stores, new[] { "wine", "wine", "bread" });
            var cellarNote = b.Prop("book", BookProp, 5.9f, 8.4f, 20, 1, Kit.ColliderKind.None, 0.0f);
            ctx.Note("arik_ledger", "Tally Book", cellarNote, "Arik's Tally Book",
                "Columns of tiny, careful numbers — bottles in, bottles out.\n\n<i>Wizard of Wines — delivery late 3rd week running. 11 bottles left. Tell no one.</i>\n\n<i>Vistani girls owe nothing. Do not ask them for coin. Do not.</i>");

            // ---------------------------------------------------------------- people
            var arik = ctx.NPC("arik", "Arik the Barkeep", Campaign.ArikLook, new GearLook(), new Vector3(2.4f, 0, 4.3f), 180, "arik");
            if (!Game.I.Flag("ismark_left_tavern"))
            {
                var ism = ctx.NPC("ismark", "Ismark Kolyanovich", Campaign.IsmarkLook, Campaign.IsmarkGear, new Vector3(-2.2f, 0, 3.2f), 180, "ismark");
                ism?.Sit(0.46f);
            }
            string[] vn = { "Alenka", "Mirabel", "Sorvia" };
            for (int i = 0; i < 3; i++)
            {
                float x = -3.9f + i * 0.7f;
                var v = ctx.NPC("vistani" + i, vn[i], Campaign.VistaniLook(i), new GearLook(), new Vector3(x, 0, i == 1 ? -1.6f : -3.2f), i == 1 ? 180 : 0, "vistani");
                v?.Sit(0.45f);
            }
        }

        static (Mesh, Material[]) WineBottles()
        {
            var mb = new MeshBuilder(2);
            for (int i = 0; i < 3; i++)
            {
                var c = new Vector3((i - 1) * 0.12f, 0, (i % 2) * 0.08f);
                mb.AddCylinder(0, c, 0.04f, 0.04f, 0.2f, 8);
                mb.AddCylinder(0, c + Vector3.up * 0.2f, 0.04f, 0.012f, 0.07f, 8);
                mb.AddCylinder(1, c + Vector3.up * 0.27f, 0.013f, 0.013f, 0.04f, 6);
            }
            mb.AddCylinder(0, new Vector3(0.25f, 0, -0.1f), 0.035f, 0.03f, 0.1f, 8);
            return (mb.Build("wine"), new[] { MatLib.Lit(new Color(.14f, .05f, .08f), null, .9f), Pal.Planks });
        }

        public static (Mesh, Material[]) BookProp()
        {
            var mb = new MeshBuilder(2);
            mb.AddBox(0, new Vector3(0, 0.03f, 0), new Vector3(0.24f, 0.05f, 0.32f));
            mb.AddBox(1, new Vector3(0.005f, 0.03f, 0), new Vector3(0.22f, 0.04f, 0.3f));
            return (mb.Build("book"), new[] { Pal.Fabric(new Color(.35f, .12f, .1f)), MatLib.Lit(new Color(.85f, .8f, .68f), null, .1f) });
        }

        public override void OnEnter(AreaContext ctx, bool first)
        {
            if (first) Game.I.StartCoroutine(Arrive());
        }

        System.Collections.IEnumerator Arrive()
        {
            yield return new WaitForSeconds(1.2f);
            var s = Game.I.Selected;
            if (s) UIRoot.I.Bark(s, "Warm. Actual firelight. And people who'll look at us.");
        }
    }

    /// <summary>Bildrath's Mercantile: one crowded room of shelves, sacks and a counter with a ledger chained to it.</summary>
    public class AreaShop : AreaDef
    {
        public override string Id => "shop";
        public override string Title => "Bildrath's Mercantile";
        public override string Subtitle => "Prices are not negotiable";
        public override bool Interior => true;
        public override AtmosphereProfile Atmos => Atmosphere.Interior;

        public override void Build(AreaContext ctx)
        {
            var b = new Indoor(ctx) { height = 3f, wallIn = Pal.PlasterDark, floor = Pal.Planks };
            b.Void();
            b.Room(0, 0, 10, 8, new Opening(2, 0, 1.4f), new Opening(1, 0, 1.3f, true), new Opening(3, 1.5f, 1.3f, true));

            b.DoorLeaf(0.7f, -4.02f, 0, 1.2f, true);
            b.Exit("exit", "Out to the Square", 0, -4.3f, 0, "village", "shop", 1.6f);
            ctx.Spawn("door", new Vector3(0, 0, -2.8f), 0);
            b.Prop("bar4", () => Props.BarCounter(4f), 0, 1.4f, 0);
            for (int i = 0; i < 4; i++) b.Prop("shelf" + i, () => Props.Bookshelf(10 + i), -4.6f, -2.4f + i * 1.6f, 90);
            for (int i = 0; i < 3; i++) b.Prop("shelf" + (i + 4), () => Props.Bookshelf(20 + i), -2.2f + i * 2.2f, 3.8f, 180);
            b.Prop("sack", Props.Sack, 3.6f, -2.8f, 30); b.Prop("sack", Props.Sack, 4.1f, -2.2f, 80); b.Prop("barrel", Props.Barrel, 4.2f, 2.9f, 0, 1, Kit.ColliderKind.Capsule);
            b.Prop("crate0.8", () => Props.Crate(0.8f), 3.8f, 0.2f, 12); b.Prop("crate0.6", () => Props.Crate(0.6f), 3.8f, 0.2f, 40, 1, Kit.ColliderKind.None, 0.8f);
            b.Prop("woodpile", Props.Woodpile, 2.5f, -3.2f, 0);
            b.Candle(-1.2f, 1.18f, 1.4f, 1.3f); b.Candle(1.3f, 1.18f, 1.4f, 1f);
            b.Sconce(-4.85f, 0.5f, 90); b.Sconce(4.85f, -2f, -90); b.Sconce(1.5f, 3.85f, 180);
            b.Fill(0, 0, new Color(1f, .72f, .5f), 1f, 12f);
            ctx.NPC("bildrath", "Bildrath Cantemir", Campaign.BildrathLook, new GearLook(), new Vector3(0.4f, 0, 2.3f), 180, "bildrath");
            ctx.NPC("parriwimple", "Parriwimple", Campaign.ParriwimpleLook, new GearLook(), new Vector3(-3.4f, 0, 2.6f), 150, "parriwimple");
        }
    }
}
