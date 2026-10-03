using System.Collections;
using System.Linq;
using Dungine.Dialogue;
using Dungine.Rules;
using Dungine.UI;
using Dungine.Visual;
using Dungine.World;
using UnityEngine;
using static Dungine.Dialogue.Dialogues;

namespace Dungine
{
    /// <summary>Shared pieces for the Durst house.</summary>
    static class DH
    {
        public static Indoor Setup(AreaContext ctx, float height = 3.2f, Material wall = null, Material floor = null)
        {
            var b = new Indoor(ctx) { height = height, wallIn = wall ?? Pal.Wallpaper, floor = floor ?? Pal.FloorBoards, wallOut = Pal.PlasterDark };
            b.Void();
            return b;
        }

        public static (Mesh, Material[]) ArmorStand()
        {
            var mb = new MeshBuilder(2);
            mb.AddBox(1, new Vector3(0, 0.05f, 0), new Vector3(0.6f, 0.1f, 0.5f));
            mb.AddCylinder(0, new Vector3(0, 0.1f, 0), 0.03f, 0.03f, 1.0f, 6);
            foreach (var sx in new[] { -1, 1 }) mb.AddCylinder(0, new Vector3(sx * 0.1f, 0.1f, 0), 0.07f, 0.06f, 0.85f, 8);
            mb.AddEllipsoid(0, new Vector3(0, 1.3f, 0), new Vector3(0.24f, 0.33f, 0.16f), 7, 9);
            foreach (var sx in new[] { -1, 1 }) { mb.AddEllipsoid(0, new Vector3(sx * 0.27f, 1.52f, 0), new Vector3(0.12f, 0.08f, 0.12f), 5, 7); mb.AddCylinder(0, new Vector3(sx * 0.3f, 0.95f, 0), 0.05f, 0.06f, 0.55f, 7); }
            mb.AddCylinder(0, new Vector3(0, 1.62f, 0), 0.12f, 0.13f, 0.26f, 9);
            mb.AddCone(0, new Vector3(0, 1.88f, 0), 0.13f, 0.12f, 9);
            mb.AddBox(0, new Vector3(0.36f, 0.9f, 0.05f), new Vector3(0.05f, 1.6f, 0.04f));
            return (mb.Build("armorstand"), new[] { Pal.Iron, Pal.TimberDark });
        }

        public static (Mesh, Material[]) SheetedFurniture(int seed)
        {
            var mb = new MeshBuilder(1);
            var rnd = new System.Random(seed);
            float w = 0.8f + (float)rnd.NextDouble(), h = 0.6f + (float)rnd.NextDouble() * 0.9f, d = 0.6f + (float)rnd.NextDouble() * 0.5f;
            mb.AddEllipsoid(0, new Vector3(0, h * 0.5f, 0), new Vector3(w * 0.55f, h * 0.55f, d * 0.55f), 6, 10, p => new Vector3(p.x, Mathf.Max(p.y, -h * 0.5f), p.z) * (1 + Mathf.Sin(p.x * 13 + p.z * 7) * 0.03f));
            return (mb.Build("sheet" + seed), new[] { MatLib.Lit(new Color(.7f, .68f, .62f), TexId.Fabric, .05f) });
        }

        public static void Cobwebs(Indoor b, params (float x, float z, float yaw)[] at)
        {
            foreach (var c in at) b.Prop("cobweb", Props.Cobweb, c.x, c.z, c.yaw, 1.2f, Kit.ColliderKind.None, b.height - 1.3f);
        }
    }

    // ================================================================================== ground floor
    public class AreaDeathHouse : AreaDef
    {
        public override string Id => "deathhouse";
        public override string Title => "The Durst House";
        public override string Subtitle => "Ground floor";
        public override bool Interior => true;
        public override bool RestUnsafe => true;
        public override AtmosphereProfile Atmos => Atmosphere.DeathHouse;
        public override string Music => "interior";
        public override float CameraMaxZoom => 15f;

        public override void Build(AreaContext ctx)
        {
            var b = DH.Setup(ctx);
            // foyer (south), hall (centre, stairs), den (west), dining (east), kitchen (north-east)
            b.Room(0, -5, 6, 5, new Opening(2, 0, 1.4f), new Opening(0, 0, 1.6f));
            b.Room(0, 1.5f, 6, 8, new Opening(2, 0, 1.6f), new Opening(3, 0, 1.2f), new Opening(1, 0, 1.2f));
            b.RoomMats(-5.5f, 1.5f, 5, 8, Pal.TimberDark, Pal.FloorBoards, new Opening(1, 0, 1.2f), new Opening(3, 1.5f, 1.2f, true));
            b.RoomMats(5.5f, 0, 5, 5, Pal.Wallpaper, Pal.FloorBoards, new Opening(3, 1.5f, 1.2f), new Opening(0, 0, 1.1f), new Opening(1, 0, 1.2f, true));
            b.RoomMats(5.5f, 4.5f, 5, 4, Pal.Plaster, Pal.Stone, new Opening(2, 0, 1.1f), new Opening(0, 1.2f, 0.9f));
            b.DoorLeaf(0.7f, -7.52f, 0, 1.4f);
            b.Exit("exit", "Front Door", 0, -7.3f, 0, "village", "deathhouse", 1.4f);
            ctx.Spawn("door", new Vector3(0, 0, -6.2f), 0);
            ctx.Spawn("stairs", new Vector3(1.2f, 0, 3.4f), 180);

            // foyer
            b.Prop("armorstand", DH.ArmorStand, -2.3f, -5.8f, 90);
            b.Prop("armorstand", DH.ArmorStand, 2.3f, -5.8f, -90);
            var portrait = b.Prop("painting", () => Props.Painting(1.6f, 1.2f, 13), 0, -2.6f, 180, 1, Kit.ColliderKind.None, 1.7f);
            ctx.Note("durst_portrait", "Family Portrait", portrait, "The Durst Family",
                "A painted family, arranged with care: a thin, proud man; a woman with a small, satisfied smile; between them a girl of ten and a boy of six, both in their Sunday best. The woman holds a swaddled infant.\n\nThe paint over the children's faces is faintly scuffed, as though someone has touched them again and again. The paint over the infant has been scratched away entirely.");
            b.Sconce(-2.85f, -4.5f, 90); b.Sconce(2.85f, -4.5f, -90);
            DH.Cobwebs(b, (-2.6f, -7.2f, 45), (2.6f, -2.8f, -135));

            // hall: stairs up, hearth
            b.Prop("stairs", () => Props.Stairs(1.4f, 3.2f, 4.2f, 12), 1.9f, 3.6f, 0, 1, Kit.ColliderKind.None);
            var up = ctx.Door("upstairs", "Stairs Up", new Vector3(1.9f, 0, 1.2f), 180, "deathhouse_upper", "stairs", new Vector3(1.4f, 2.4f, 1f));
            b.Fireplace(-2.75f, 3.8f, 90);
            b.Prop("rug", () => Props.Rug(2.4f, 3.5f, new Color(.18f, .1f, .12f)), -0.8f, 0.8f, 0, 1, Kit.ColliderKind.None, 0.01f);
            b.Prop("chair_f", () => Props.Chair(true), -1.6f, 2.2f, 120);

            // den
            for (int i = 0; i < 3; i++) b.Prop("wolfhead" + i, WolfTrophy, -7.85f, -0.6f + i * 1.6f, 90, 1, Kit.ColliderKind.None, 1.9f);
            b.Prop("chair_f", () => Props.Chair(true), -5.5f, 3.4f, 200);
            b.Prop("chair_f", () => Props.Chair(true), -4.3f, 3.1f, 160);
            b.Prop("table1.1", () => Props.Table(1.1f, 0.7f), -5f, 4.4f, 0);
            b.Candle(-5f, 0.88f, 4.4f, 1.4f);
            var cab = ctx.Chest("den_cabinet", "Hunting Cabinet", new Vector3(-7.3f, 0, 4.8f), 90, new[] { "light_crossbow", "potion_healing", "wine" }, 18);
            var hunt = b.Prop("book", AreaTavern.BookProp, -4.8f, 4.3f, 30, 1, Kit.ColliderKind.None, 0.86f);
            ctx.Note("hunt_log", "Hunting Log", hunt, "Gustav Durst's Hunting Log",
                "<i>Wolf, grey, large. Svalich woods.</i>\n<i>Wolf, grey. Svalich woods.</i>\n<i>Wolf. Would not die cleanly. Elisabeth says the one below prefers them alive, next time.</i>\n\nThe entries stop there.");

            // dining
            b.Prop("table2.4", () => Props.Table(2.4f, 1f), 5.5f, -0.3f, 90);
            for (int i = 0; i < 3; i++) foreach (var sx in new[] { -1, 1 }) b.Prop("chair_f", () => Props.Chair(true), 5.5f + sx * 0.85f, -1.2f + i * 0.9f, sx > 0 ? -90 : 90);
            b.Candelabra(7.5f, 1.8f);
            var silver = b.Prop("candlesticks", Candlesticks, 5.5f, -0.3f, 0, 1, Kit.ColliderKind.None, 0.8f);
            ctx.Stash("dining_silver", "Tarnished Place Settings", silver, new[] { "candlesticks" }, 6);
            ctx.Trigger("dining_feel", new Vector3(5.5f, 0, 0), 2f, () => UIRoot.I.Bark(Game.I.Selected, "Places for six. Dinner went cold on these plates a long, long time ago."));

            // kitchen: the broom closet
            b.Prop("barrel", Props.Barrel, 7.4f, 5.6f, 0, 1, Kit.ColliderKind.Capsule);
            b.Prop("table1.1", () => Props.Table(1.1f, 0.7f), 4.6f, 5.4f, 90);
            b.Prop("woodpile", Props.Woodpile, 3.8f, 3.2f, 0);
            b.Candle(4.6f, 0.9f, 5.4f, 1.2f);
            if (!Game.I.Flag("dead:" + ctx.Key("broom")))
            {
                var broom = ctx.Monster("broom", "broom", new Vector3(7.2f, 0, 3.4f), 225, "broom", true, 2.6f);
            }
            var pantry = b.Prop("crate0.8", () => Props.Crate(0.8f), 6.6f, 6.1f, 10);
            ctx.Stash("pantry", "Pantry Shelf", pantry, new[] { "bread", "garlic", "alchemist_fire" });

            b.Fill(0, 0, new Color(.75f, .72f, .85f), 0.5f, 14f);
            b.Fill(5.5f, 2f, new Color(1f, .75f, .5f), 0.4f, 8f);
        }

        static (Mesh, Material[]) WolfTrophy()
        {
            var mb = new MeshBuilder(2);
            mb.AddBox(1, new Vector3(0, 0, -0.03f), new Vector3(0.45f, 0.5f, 0.05f));
            mb.AddEllipsoid(0, new Vector3(0, 0, 0.14f), new Vector3(0.16f, 0.15f, 0.18f), 6, 8);
            mb.AddEllipsoid(0, new Vector3(0, -0.04f, 0.33f), new Vector3(0.07f, 0.07f, 0.12f), 5, 7);
            foreach (var sx in new[] { -1, 1 }) mb.AddCone(0, new Vector3(sx * 0.09f, 0.12f, 0.1f), 0.05f, 0.12f, 5);
            return (mb.Build("wolfhead"), new[] { MatLib.Lit(new Color(.42f, .4f, .38f), TexId.Hair, .1f), Pal.TimberDark });
        }

        public static (Mesh, Material[]) Candlesticks()
        {
            var mb = new MeshBuilder(1);
            foreach (var x in new[] { -0.4f, 0.4f }) { mb.AddCylinder(0, new Vector3(x, 0, 0), 0.06f, 0.03f, 0.05f, 8); mb.AddCylinder(0, new Vector3(x, 0.05f, 0), 0.02f, 0.02f, 0.25f, 6); }
            for (int i = 0; i < 6; i++) mb.AddCylinder(0, new Vector3(-0.7f + i * 0.28f, 0, (i % 2) * 0.3f - 0.15f), 0.11f, 0.11f, 0.01f, 12);
            return (mb.Build("candlesticks"), new[] { MatLib.Lit(new Color(.6f, .6f, .62f), TexId.Metal, .6f, .9f) });
        }

        public override void OnEnter(AreaContext ctx, bool first)
        {
            if (first) Game.I.StartCoroutine(Enter());
        }

        IEnumerator Enter()
        {
            Game.I.journal.Update("deathhouse", "The House on Mad Mary's Lane", "We have entered the Durst house. The front door swung shut behind us — and the children outside did not follow.");
            yield return new WaitForSeconds(1.5f);
            Audio.Sfx.Play("door", 0.8f, 0.6f);
            yield return new WaitForSeconds(0.4f);
            var s = Game.I.Selected;
            if (s) UIRoot.I.Bark(s, "The door shut on its own. ...Of course it did.");
        }
    }

    // ================================================================================== upper floor
    public class AreaDeathHouseUpper : AreaDef
    {
        public override string Id => "deathhouse_upper";
        public override string Title => "The Durst House";
        public override string Subtitle => "Upper floor";
        public override bool Interior => true;
        public override bool RestUnsafe => true;
        public override AtmosphereProfile Atmos => Atmosphere.DeathHouse;
        public override string Music => "interior";
        public override float CameraMaxZoom => 15f;

        public override void Build(AreaContext ctx)
        {
            var b = DH.Setup(ctx);
            // landing (centre), library (west), master bedroom (east), conservatory (north-west), nursery (north-east), attic door (north)
            b.Room(0, 0, 5, 7, new Opening(3, -1, 1.2f), new Opening(1, -1, 1.2f), new Opening(0, 0, 1.2f));
            b.RoomMats(-5.5f, -1, 6, 5, Pal.TimberDark, Pal.FloorBoards, new Opening(1, 0, 1.2f), new Opening(0, -1f, 1.1f));
            b.RoomMats(5.5f, -1, 6, 5, Pal.Wallpaper, Pal.FloorBoards, new Opening(3, 0, 1.2f), new Opening(0, 1f, 1.1f), new Opening(1, 0, 1.2f, true));
            b.RoomMats(-5.5f, 4.5f, 6, 6, Pal.Wallpaper, Pal.FloorBoards, new Opening(2, -1f, 1.1f), new Opening(3, 0, 1.4f, true));
            b.RoomMats(5.5f, 4.5f, 6, 6, Pal.Wallpaper, Pal.FloorBoards, new Opening(2, 1f, 1.1f));
            b.Room(0, 5.25f, 5, 3.5f, new Opening(2, 0, 1.2f), new Opening(0, 0, 1f));
            b.Prop("stairs", () => Props.Stairs(1.4f, 3.2f, 3f, 10), 1.3f, -2.6f, 180, 1, Kit.ColliderKind.None);
            ctx.Door("downstairs", "Stairs Down", new Vector3(1.3f, 0, -1.4f), 0, "deathhouse", "stairs", new Vector3(1.4f, 2.4f, 1f));
            ctx.Spawn("stairs", new Vector3(0.2f, 0, -0.6f), 0);
            ctx.Spawn("attic", new Vector3(0, 0, 5.6f), 180);

            // library
            for (int i = 0; i < 3; i++) b.Prop("shelf" + i, () => Props.Bookshelf(60 + i), -7.2f + i * 1.3f, 1.25f, 180);
            for (int i = 0; i < 2; i++) b.Prop("shelf" + (i + 3), () => Props.Bookshelf(70 + i), -8.25f, -2.6f + i * 1.4f, 90);
            b.Prop("table1.6", () => Props.Table(1.6f, 0.8f), -5f, -1.8f, 0);
            b.Prop("chair_f", () => Props.Chair(true), -5f, -2.7f, 0);
            b.Candle(-4.5f, 0.86f, -1.8f, 1.5f);
            var notes = b.Prop("book", AreaTavern.BookProp, -5.3f, -1.7f, 20, 1, Kit.ColliderKind.None, 0.86f);
            ctx.Note("walters_notes", "Loose Notes", notes, "Notes in Gustav's Hand", "Our patrons in the dark ask much and give much. Elisabeth says the one below grows hungry. We have sent the servants away. We shall not send the children away. Not yet.\n\nThe stair behind the nursemaid's wall goes down farther than the house should allow. Every night I hear it breathing.", "dh_journal");
            var drawer = b.Prop("crate0.6", () => Props.Crate(0.5f), -7.8f, -3f, 0, 1, Kit.ColliderKind.Box);
            var st = ctx.Stash("hidden_drawer", "Hidden Drawer", drawer, new[] { "dh_deed", "scroll_magic_missile" }, 30);
            st.hidden = true; st.perceptionDC = 13;

            // master bedroom: the attic key in the wardrobe
            b.Prop("bed4", () => Props.Bed(true, new Color(.35f, .1f, .12f)), 6.4f, -1.2f, -90);
            b.Prop("wardrobe", Props.Wardrobe, 7.9f, 0.9f, -90);
            var ward = new GameObject("WardrobeUse"); ward.transform.SetParent(ctx.root); ward.transform.position = new Vector3(7.6f, 1f, 0.9f);
            var wst = ctx.Stash("wardrobe", "Wardrobe", ward, new[] { "key_deathhouse_cellar", "cloak_mists" }, 0);
            b.Prop("rug", () => Props.Rug(2f, 3f, new Color(.22f, .08f, .1f)), 5f, -1f, 0, 1, Kit.ColliderKind.None, 0.01f);
            b.Candelabra(3.4f, -3f);
            if (!Game.I.Flag("dead:" + ctx.Key("armor")))
                ctx.Monster("armor", "animated_armor", new Vector3(0, 0, 3.9f), 180, "attic_guard", true, 3.2f);

            // conservatory: an instrument no one plays
            b.Prop("chair_f", () => Props.Chair(true), -6.2f, 6.4f, -90);
            for (int i = 0; i < 3; i++) b.Prop("sheet" + i, () => DH.SheetedFurniture(i + 3), -4f + i * 0.2f, 3.2f + i * 1.2f, i * 40);
            ctx.Use("organ", "Harpsichord", ctx.Prop("organ_use", () => Props.Organ(), new Vector3(-7.4f, 0, 6.6f), 90), "Play", (u, s) =>
            {
                Audio.Sfx.Play("whisper", 0.8f, 1.4f);
                UIRoot.I.Bark(u, "A few notes of a lullaby. From somewhere above, faintly, a child's voice hums the next bar.");
            }, true);

            // nursery: an empty crib and a dollhouse that is not quite a toy
            b.Prop("crib", Props.Crib, 6.8f, 6.2f, 0);
            var crib = new GameObject("CribUse"); crib.transform.SetParent(ctx.root); crib.transform.position = new Vector3(6.8f, 0.8f, 6.2f);
            ctx.Use("crib", "Crib", crib, "Look in", (u, s) => ReadWindow.Show("The Crib", "A swaddling blanket, carefully tucked, wrapped around nothing at all. It is as cold as a stone from a riverbed.\n\nEmbroidered in one corner: <i>Walter</i>."), true);
            var dh = b.Prop("dollhouse", Props.Dollhouse, 3.6f, 6.8f, 180, 1, Kit.ColliderKind.Box);
            ctx.Use("dollhouse", "Dollhouse", dh, "Examine", (u, s) =>
            {
                ReadWindow.Show("The Dollhouse", "A perfect miniature of this house, down to the wolf heads in the den. Every room is furnished; tiny dolls sit at the dining table.\n\nIn the attic, a little wooden panel behind the nursemaid's bed slides aside to reveal a painted spiral stair, winding down through the whole house and below it, into a room painted entirely black.");
                Game.I.SetFlag("dh_secret_known");
                Game.I.journal.AddLore("The dollhouse in the Durst nursery shows a secret stair behind the nursemaid's bed in the attic, spiralling down below the house.");
            }, true);
            b.Sconce(8.35f, 5f, -90);

            b.Fill(0, 1, new Color(.72f, .7f, .85f), 0.5f, 14f);
            b.Fill(-5.5f, 4.5f, new Color(.6f, .65f, .85f), 0.35f, 8f);
            ctx.Door("attic_door", "Attic Door", new Vector3(0, 0, 7.2f), 180, "deathhouse_attic", "stairs", new Vector3(1.1f, 2.3f, 0.6f), true, "key_deathhouse_cellar").lockedText = "Locked. The keyhole is small and rusted; the key must be somewhere in the house.";
            DH.Cobwebs(b, (-8.2f, 1.2f, 45), (8.2f, 7.2f, -135), (-2.2f, 6.8f, 45));
        }
    }

    // ================================================================================== attic
    public class AreaDeathHouseAttic : AreaDef
    {
        public override string Id => "deathhouse_attic";
        public override string Title => "The Durst House";
        public override string Subtitle => "Attic";
        public override bool Interior => true;
        public override bool RestUnsafe => true;
        public override AtmosphereProfile Atmos => Atmosphere.DeathHouse;
        public override string Music => "strahd";
        public override float CameraMaxZoom => 14f;

        public override void Build(AreaContext ctx)
        {
            var b = DH.Setup(ctx, 2.6f, Pal.Planks, Pal.Planks);
            // storage (south), children's room (west), nursemaid's room (east)
            b.Room(0, -2, 10, 5, new Opening(2, 0, 1f), new Opening(0, -3f, 1f), new Opening(0, 3f, 1f));
            b.Room(-3, 3, 4, 5, new Opening(2, 0, 1f), new Opening(3, 0, 1f, true));
            b.Room(3, 3, 4, 5, new Opening(2, 0, 1f));
            b.Exit("down", "Attic Stairs Down", 0, -4.3f, 0, "deathhouse_upper", "attic", 1f);
            ctx.Spawn("stairs", new Vector3(0, 0, -3.3f), 0);
            ctx.Spawn("secret", new Vector3(3.8f, 0, 4.2f), 180);
            for (int i = 0; i < 6; i++) b.Prop("sheet" + (i + 10), () => DH.SheetedFurniture(i + 10), -4f + i * 1.6f, -1f + (i % 2) * 1.4f, i * 50);
            b.Prop("crate0.8", () => Props.Crate(0.8f), 4.2f, -3.6f, 10); b.Prop("crate0.6", () => Props.Crate(0.6f), 3.3f, -3.8f, 30);
            var trunk = ctx.Chest("attic_trunk", "Old Trunk", new Vector3(-4.2f, 0, -3.8f), 0, new[] { "potion_healing", "potion_healing", "scroll_bless" }, 12);
            b.Candle(0.5f, 0.1f, -2.8f, 1.2f, 5f);

            // the children's room: two small beds, and what lies on them
            b.Prop("bed_small", () => Props.Bed(false, new Color(.4f, .42f, .5f)), -4.2f, 4.3f, 90, 0.7f);
            b.Prop("bed_small", () => Props.Bed(false, new Color(.3f, .34f, .44f)), -1.8f, 4.3f, -90, 0.7f);
            b.Prop("toychest", () => Props.Chest(true), -3f, 5.1f, 180, 0.6f);
            if (!Game.I.stash.Has("children_bones") && !Game.I.Flag("bones_laid"))
            {
                var bones = b.Prop("bones", ChildBones, -3f, 4.3f, 0, 1, Kit.ColliderKind.None, 0.45f);
                var pk = ctx.Add<Pickup>("children_bones", "Small Bones", bones, 1.6f);
                pk.itemId = "children_bones";
            }
            ctx.Trigger("ghosts", new Vector3(-3, 0, 2.5f), 2.2f, () => Game.I.StartCoroutine(Ghosts(ctx)));
            b.Fill(-3, 3, new Color(.55f, .62f, .85f), 0.4f, 6f);

            // the nursemaid's room: her spirit, and the wall behind her bed
            b.Prop("bed", () => Props.Bed(false, new Color(.3f, .28f, .26f)), 3.9f, 4.4f, -90);
            b.Prop("chair", () => Props.Chair(false), 2f, 2.2f, 30);
            if (!Game.I.Flag("dead:" + ctx.Key("nursemaid")))
            {
                var sp = ctx.Monster("nursemaid", "specter", new Vector3(2.8f, 0, 3.2f), 200, "nursemaid", true, 3f);
            }
            var panelGo = new GameObject("SecretPanel"); panelGo.transform.SetParent(ctx.root); panelGo.transform.position = new Vector3(4.9f, 0, 4.4f);
            var secret = ctx.Door("secret_stair", "Sliding Panel", new Vector3(4.9f, 0, 4.4f), -90, "deathhouse_dungeon", "stairs", new Vector3(0.4f, 2f, 1.2f));
            secret.hidden = !Game.I.Flag("dh_secret_known"); secret.perceptionDC = 14;
            b.Fill(3, 3, new Color(.55f, .6f, .8f), 0.35f, 6f);
            DH.Cobwebs(b, (-4.8f, -4.2f, 45), (4.8f, -0.1f, -135), (-4.8f, 5.3f, 45), (4.8f, 5.3f, -135));
        }

        static (Mesh, Material[]) ChildBones()
        {
            var mb = new MeshBuilder(1);
            mb.AddEllipsoid(0, new Vector3(0, 0.06f, 0.35f), new Vector3(0.08f, 0.08f, 0.09f), 6, 8);
            for (int i = 0; i < 6; i++) mb.AddCylinder(0, new Vector3(-0.07f + (i % 2) * 0.14f, 0.02f, 0.15f - i * 0.07f), 0.012f, 0.012f, 0.02f, 5);
            mb.Push(); mb.Rotate(Quaternion.Euler(90, 0, 0)); mb.AddCylinder(0, new Vector3(0, -0.3f, -0.02f), 0.015f, 0.015f, 0.55f, 5); mb.Pop();
            mb.Push(); mb.Rotate(Quaternion.Euler(90, 12, 0)); mb.AddCylinder(0, new Vector3(0.05f, -0.45f, -0.02f), 0.013f, 0.013f, 0.35f, 5); mb.Pop();
            mb.Push(); mb.Rotate(Quaternion.Euler(90, -12, 0)); mb.AddCylinder(0, new Vector3(-0.05f, -0.45f, -0.02f), 0.013f, 0.013f, 0.35f, 5); mb.Pop();
            return (mb.Build("childbones"), new[] { Pal.Bone });
        }

        static IEnumerator Ghosts(AreaContext ctx)
        {
            if (Game.I.Flag("met_rt_ghosts")) yield break;
            Audio.Sfx.Play("child_laugh", 0.7f, 0.9f);
            var rose = ctx.NPC("rose_ghost", "Rose", new Appearance { race = RaceId.Human, bodyType = 1, child = true, ghostly = true, skin = new Color(.8f, .85f, .95f), hair = new Color(.6f, .65f, .75f), hairStyle = 7, cloth1 = new Color(.6f, .65f, .8f), cloth2 = new Color(.5f, .55f, .7f) }, new GearLook(), new Vector3(-3.6f, 0, 3.3f), 180, "rt_ghosts", MotionStyle.Ghost);
            var thorn = ctx.NPC("thorn_ghost", "Thorn", new Appearance { race = RaceId.Human, bodyType = 0, child = true, ghostly = true, heightAdj = -0.8f, skin = new Color(.8f, .85f, .95f), hair = new Color(.6f, .65f, .75f), hairStyle = 1, cloth1 = new Color(.55f, .6f, .75f), cloth2 = new Color(.5f, .55f, .7f) }, new GearLook(), new Vector3(-2.4f, 0, 3.5f), 180, "rt_ghosts", MotionStyle.Ghost);
            if (thorn) thorn.transform.localScale = Vector3.one * 0.86f;
            foreach (var g in new[] { rose, thorn }) if (g) { g.agent.enabled = false; FX.Burst(g.Chest, FxKind.Holy, 0.8f); }
            yield return new WaitForSeconds(1f);
            if (rose) DialogueRunner.I.Begin("rt_ghosts", rose, Game.I.Selected);
        }
    }

    // ================================================================================== the dungeon beneath
    public class AreaDeathHouseDungeon : AreaDef
    {
        public override string Id => "deathhouse_dungeon";
        public override string Title => "Beneath the Durst House";
        public override string Subtitle => "One must die";
        public override bool Interior => true;
        public override bool RestUnsafe => true;
        public override AtmosphereProfile Atmos => Atmosphere.Dungeon;
        public override string Music => "strahd";
        public override float CameraMaxZoom => 16f;

        public override void Build(AreaContext ctx)
        {
            var b = DH.Setup(ctx, 3f, Pal.Stone, Pal.Cobble);
            // stair landing (north), long corridor, crypts (east), cult quarters (west), ritual chamber (south)
            b.Room(0, 14, 4, 4, new Opening(2, 0, 1.6f));
            b.Room(0, 6, 3, 12, new Opening(0, 0, 1.6f), new Opening(1, 2f, 1.4f), new Opening(3, 2f, 1.4f), new Opening(2, 0, 2f));
            b.Room(6, 8, 9, 8, new Opening(3, 0, 1.4f));
            b.Room(-5.5f, 8, 8, 7, new Opening(1, 0, 1.4f));
            b.Room(0, -6, 14, 12, new Opening(0, 0, 2f));
            b.Prop("stairs", () => Props.Stairs(1.6f, 3f, 3f, 10), 0, 15.2f, 0, 1, Kit.ColliderKind.None);
            b.Exit("up", "The Long Stair Up", 0, 15.8f, 180, "deathhouse_attic", "secret", 1.6f);
            ctx.Spawn("stairs", new Vector3(0, 0, 13.2f), 180);

            // corridor: niches with bones
            for (int i = 0; i < 4; i++) foreach (var sx in new[] { -1, 1 }) b.Prop("bonepile", () => Props.BonePile(i * 2 + (sx > 0 ? 1 : 0)), sx * 1.2f, 1.8f + i * 2.6f, sx * 90, 0.6f, Kit.ColliderKind.None);
            b.Sconce(-1.45f, 9.5f, 90); b.Sconce(1.45f, 3.5f, -90);
            ctx.Trigger("chant1", new Vector3(0, 0, 9f), 2f, () => Game.I.StartCoroutine(Chant("One must die... one must die...")));

            // crypts: the Dursts' dead, and the ones who eat them
            for (int i = 0; i < 4; i++) b.Prop("sarcophagus", Props.Sarcophagus, 3.5f + i * 2f, 10.6f, 180);
            var laySpot = new GameObject("CryptNiche"); laySpot.transform.SetParent(ctx.root); laySpot.transform.position = new Vector3(9.8f, 0.6f, 5f);
            b.Prop("coffin_c", () => Props.Coffin(false), 9.8f, 5f, 90, 0.7f);
            ctx.Use("lay_bones", "Empty Child's Coffin", laySpot, "Lay to rest", (u, s) =>
            {
                if (!Game.I.stash.Has("children_bones")) { ReadWindow.Show("A Small Coffin", "A child's coffin, never used. The lid is carved with two names: ROSE and THORN."); return; }
                Game.I.stash.Remove("children_bones");
                Game.I.SetFlag("bones_laid");
                s.used = true;
                Audio.Sfx.Play("revive");
                FX.Burst(laySpot.transform.position + Vector3.up * 0.5f, FxKind.Holy, 2f);
                foreach (var c in Game.I.party.Where(c => c.Active)) c.AddCond(Cond.Blessed, 10, null);
                Game.I.partyInspiration = Mathf.Min(4, Game.I.partyInspiration + 1);
                ReadWindow.Show("Laid to Rest", "You place the small bones in the coffin that was carved for them and never used, and close the lid.\n\nFor a moment the crypt is warm. Somewhere very far away, two children laugh — not the thin laugh of the house, but a real one — and then it is quiet, and it stays quiet.\n\n<color=#e6c67a>The party is Blessed and gains Inspiration.</color>");
                Game.I.GiveXP(150, "Rose and Thorn, at rest");
                Game.I.journal.Update("rosethorn", "Rose and Thorn", "We laid the bones of Rose and Thorn to rest in the crypt their parents carved for them.", QuestState.Done);
            });
            if (!Game.I.Flag("dead:" + ctx.Key("ghoul1")))
            {
                ctx.Monster("ghoul1", "ghoul", new Vector3(5.2f, 0, 9.2f), 250, "crypt", true, 5f);
                ctx.Monster("ghoul2", "ghoul", new Vector3(8.4f, 0, 8.6f), 230, "crypt", true, 5f);
            }
            var cryptChest = ctx.Chest("crypt_chest", "Grave Goods", new Vector3(10f, 0, 10.8f), 180, new[] { "gravefang", "amulet_morninglord" }, 40);

            // cult quarters
            for (int i = 0; i < 3; i++) b.Prop("cot" + i, () => Props.Bed(false, new Color(.25f, .22f, .2f)), -8.2f, 5.6f + i * 1.9f, 90, 0.8f);
            b.Prop("table1.1", () => Props.Table(1.1f, 0.7f), -4.2f, 9.8f, 0);
            b.Candle(-4.2f, 0.88f, 9.8f, 1.2f);
            var creed = b.Prop("book", AreaTavern.BookProp, -4f, 9.7f, 60, 1, Kit.ColliderKind.None, 0.86f);
            ctx.Note("cult_creed", "Black Book", creed, "The Priests of Osybus",
                "<i>We are the ones who kept the old bargain when the rest of the valley forgot it.</i>\n\n<i>Keep the braziers low. It hates the fire; it drinks the storm.</i>\n\n<i>What lives below does not want gold. It does not want prayers. It wants one thing, and it wants it again and again, and it gives in return a long, rich, careful life.</i>\n\n<i>The children must never come down. When the time comes, the children must never come down.</i>\n\nThe last page has been torn out, and the tear is stained dark.");
            var locker = ctx.Chest("cult_locker", "Cultist's Locker", new Vector3(-8.6f, 0, 10.8f), 90, new[] { "staff_ashes", "potion_healing" }, 20);
            if (!Game.I.Flag("dead:" + ctx.Key("shadow1")))
            {
                ctx.Monster("shadow1", "shadow", new Vector3(-7.2f, 0, 9.4f), 90, "quarters", true, 4.5f);
            }

            // ritual chamber
            b.Prop("bloodaltar", Props.BloodAltar, 0, -7.5f, 0);
            b.Prop("bonepile", () => Props.BonePile(20), -4f, -9f, 20, 1, Kit.ColliderKind.None);
            b.Prop("bonepile", () => Props.BonePile(21), 4.5f, -3.5f, 70, 1, Kit.ColliderKind.None);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2;
                var p = new Vector3(Mathf.Cos(a) * 5f, 0, -6f + Mathf.Sin(a) * 4.2f);
                b.Prop("brazier", Props.Brazier, p.x, p.z, 0, 1, Kit.ColliderKind.Capsule);
                Kit.Flame(ctx.root, p + Vector3.up * 1.05f, 0.14f);
                Kit.PointLight(ctx.root, p + Vector3.up * 1.4f, new Color(1f, .45f, .25f), 1.6f, 6f, false);
            }
            b.Fill(0, -6, new Color(.8f, .3f, .25f), 0.35f, 14f);
            b.Fill(0, 6, new Color(.45f, .5f, .65f), 0.3f, 12f);
            if (!Game.I.Flag("dead:" + ctx.Key("lorghoth")))
            {
                var lor = ctx.Monster("lorghoth", "lorghoth", new Vector3(0, 0, -9.2f), 0, "lorghoth", false, 1f);
                if (lor) { lor.gameObject.SetActive(false); }
                ctx.Trigger("ritual", new Vector3(0, 0, -2f), 3.5f, () => Game.I.StartCoroutine(Ritual(ctx)));
            }

        }

        static IEnumerator Chant(string text)
        {
            Audio.Sfx.Play("whisper", 1f, 0.7f);
            var s = Game.I.Selected;
            Toast.Show($"<i>Voices, from somewhere ahead:</i> \"{text}\"", Theme.Hostile, 4f);
            yield return new WaitForSeconds(2f);
            if (s) UIRoot.I.Bark(s, "Tell me everyone else heard that.");
        }

        static IEnumerator Ritual(AreaContext ctx)
        {
            var g = Game.I;
            g.busy = true;
            Audio.Sfx.Play("heartbeat", 1f, 0.8f);
            Toast.Show("<i>Many voices, all around you:</i> \"ONE MUST DIE! ONE MUST DIE! ONE MUST DIE!\"", Theme.Hostile, 5f);
            yield return new WaitForSeconds(2.2f);
            g.busy = false;
            DialogueRunner.I.Begin("ritual", null, g.Selected);
        }

        public static IEnumerator Awaken()
        {
            var g = Game.I;
            var lor = g.area.byId.TryGetValue("lorghoth", out var l) ? l : null;
            if (!lor) yield break;
            lor.gameObject.SetActive(true);
            lor.c.faction = Faction.Hostile;
            if (lor.agent && UnityEngine.AI.NavMesh.SamplePosition(lor.transform.position, out var hit, 3f, UnityEngine.AI.NavMesh.AllAreas)) { lor.agent.enabled = true; lor.agent.Warp(hit.position); }
            FX.Burst(lor.Chest, FxKind.Necrotic, 3f);
            CameraRig.I.Shake(0.5f);
            Audio.Sfx.Play("combat_start", 1f, 0.6f);
            yield return new WaitForSeconds(0.8f);
            UIRoot.I.Bark(g.Selected, "The pit — something's coming out of the pit!");
            yield return new WaitForSeconds(0.6f);
            Combat.CombatManager.I.StartCombat(lor, null);
            yield return Campaign.WaitForCombat();
            if (!g.party.Any(c => c.Active)) yield break;
            if (lor && lor.c.dead) g.StartCoroutine(Collapse());
        }

        /// <summary>With its master dead, the house turns on the intruders: a scramble for the front door.</summary>
        static IEnumerator Collapse()
        {
            var g = Game.I;
            g.SetFlag("dh_done");
            g.GiveXP(200, "Lorghoth the Decayer");
            yield return new WaitForSeconds(1.2f);
            CameraRig.I.Shake(0.8f);
            Audio.Sfx.Play("thunder_hit", 1f, 0.5f);
            UIRoot.I.Bark(g.Selected, "The walls are moving. The HOUSE is moving. Run!");
            yield return new WaitForSeconds(1.6f);
            yield return UIRoot.I.FadeOut(0.8f, "The House Awakens", "Every door slams. Every stair tilts. Run.");
            yield return new WaitForSecondsRealtime(1.2f);
            // a dash through the collapsing house: everyone makes a Dexterity save
            foreach (var c in g.party.Where(c => c.Active))
            {
                int roll = UnityEngine.Random.Range(1, 21) + c.SaveBonus(Ability.DEX);
                if (roll < 12)
                {
                    int dmg = UnityEngine.Random.Range(1, 7) + UnityEngine.Random.Range(1, 7);
                    c.hp = Mathf.Max(1, c.hp - dmg);
                    Toast.Show($"{c.name} is struck by falling timbers: {dmg} damage (DEX save {roll} vs 12).", Theme.Failure, 4f);
                }
                else Toast.Show($"{c.name} dodges clear (DEX save {roll} vs 12).", Theme.Success, 4f);
            }
            yield return new WaitForSecondsRealtime(2.2f);
            g.journal.Update("deathhouse", "The House on Mad Mary's Lane", "Beneath the Durst house we found the cult's ritual chamber and destroyed the thing it fed, Lorghoth the Decayer. The house itself tried to kill us as we fled — but we made it out.", QuestState.Done);
            g.GoToArea("village", "deathhouse");
        }
    }

    public static partial class Campaign
    {
        static partial void RegisterDeathHouseImpl()
        {
            AreaLoader.Register(new AreaDeathHouse());
            AreaLoader.Register(new AreaDeathHouseUpper());
            AreaLoader.Register(new AreaDeathHouseAttic());
            AreaLoader.Register(new AreaDeathHouseDungeon());
        }

        static void DeathHouseDialogues()
        {
            new DB("rt_ghosts")
                .Node("start", "Rose", "*The girl is sitting on the edge of the little bed, looking down at what's lying on it. She's translucent now — you can see the wallpaper through her.* ...That's us, isn't it. That's what they left.")
                    .Enter(() => G.SetFlag("met_rt_ghosts"))
                    .Opt("I'm sorry, Rose.", "sorry")
                    .Opt("What happened to you?", "what")
                .Node("sorry", "Thorn", "*The boy has his face pressed into his sister's side.* They locked the door. Mother said it was for our own good. Then they forgot us. Then they stopped coming at all.")
                    .Actor("thorn_ghost")
                    .Then("what")
                .Node("what", "Rose", "Mother and Father had friends who came at night, in robes. They went down below the house. We weren't allowed. When the baby came, they took him down with them. He didn't come back up. *She looks at you.* And then they locked us in here, and nobody ever came back up again.")
                    .Opt("Is there anything we can do for you?", "rest")
                    .Opt("How do we get below the house?", "stair")
                .Node("stair", "Rose", "The nursemaid's room. There's a panel behind her bed — I saw Father go through it once. The nursemaid's still in there. She's angry all the time now. *A small, terrible smile.* She was never nice anyway.")
                    .Enter(() => { G.SetFlag("dh_secret_known"); var s = G.area.interactables.FirstOrDefault(i => i && i.id != null && i.id.EndsWith("secret_stair")); if (s) s.Reveal(); })
                    .Then("rest")
                .Node("rest", "Rose", "There's a crypt down there, where the Dursts go when they die. Our names are on a coffin. Mother had it made before we were born. *She touches the bones without being able to feel them.* Would you take us down? We don't want to be up here in the dark anymore.")
                    .Opt("We'll take you down.", "yes")
                    .Opt("We'll see.", "maybe")
                .Node("yes", "Thorn", "*The boy looks up for the first time.* Promise?")
                    .Actor("thorn_ghost")
                    .Enter(() =>
                    {
                        Journal("rosethorn", "Rose and Thorn", "The ghosts of Rose and Thorn asked us to carry their bones down into the crypt beneath the house, to the coffin carved with their names.");
                        Game.I.StartCoroutine(FadeGhosts());
                    })
                    .Opt("Promise.")
                .Node("maybe", "Rose", "...That's what grown-ups always say.")
                    .Enter(() => Game.I.StartCoroutine(FadeGhosts()))
                    .End();

            new DB("ritual")
                .Node("start", "", "*The braziers flare. Shapes crowd the edges of the chamber — robed figures made of smoke, their faces blank, their voices one voice.* ONE MUST DIE. ONE MUST DIE. GIVE ONE TO THE HUNGRY DARK, AND THE REST WILL WALK AWAY. *In the black pit behind the altar, something vast begins to stir.*")
                    .Opt("No one is dying for you.", "refuse")
                    .Opt("[RELIGION] These are echoes — command them to be still.", null).Check(Skill.Religion, 15, "still", "refuse")
                    .Opt("[DECEPTION] We brought an offering. It's already in the pit.", null).Check(Skill.Deception, 17, "lie", "refuse")
                .Node("still", "", "*Your voice cuts through the chant, and half the smoke-figures unravel like breath on glass. The remaining ones fall silent — but the thing in the pit doesn't need them anymore. It has smelled you.*")
                    .Enter(() => { foreach (var c in G.party.Where(c => c.Active)) c.AddCond(Cond.Blessed, 10, null); Toast.Show("The party is Blessed.", Theme.Gold); })
                    .Then("rise")
                .Node("lie", "", "*The chant falters. The smoke-figures turn toward the pit, waiting — and in the silence you have just enough time to set your feet before the pit realises it has been cheated.*")
                    .Enter(() => G.SetFlag("lorghoth_surprised"))
                    .Then("rise")
                .Node("refuse", "", "*The chant becomes a howl. The smoke-figures lunge and pass through you like cold water, and the thing in the pit heaves itself up over the lip — a hill of rot and roots and bones, bigger than a cart, with a mouth.*")
                    .Then("rise")
                .Node("rise", "", "*Lorghoth the Decayer rises.*")
                    .Enter(() => Game.I.StartCoroutine(RiseAfterDialogue()))
                    .End();
        }

        static IEnumerator RiseAfterDialogue()
        {
            while (DialogueRunner.I.Active) yield return null;
            yield return AreaDeathHouseDungeon.Awaken();
        }

        static IEnumerator FadeGhosts()
        {
            while (DialogueRunner.I.Active) yield return null;
            foreach (var id in new[] { "rose_ghost", "thorn_ghost" })
            {
                var a = Game.I.FindActor(id);
                if (a) { FX.Burst(a.Chest, FxKind.Holy, 1f); Object.Destroy(a.gameObject); }
            }
        }
    }
}
