using System.Collections;
using System.Linq;
using Dungine.Rules;
using Dungine.UI;
using Dungine.Visual;
using Dungine.World;
using UnityEngine;

namespace Dungine
{
    /// <summary>
    /// The burgomaster's manor. A cold entrance hall; the parlour where Kolyan lies in his coffin and Ireena keeps
    /// watch; his study; the dining room, its shutters nailed from the inside.
    /// </summary>
    public class AreaMansion : AreaDef
    {
        public override string Id => "mansion";
        public override string Title => "The Burgomaster's Manor";
        public override string Subtitle => "Every shutter nailed, every lamp lit";
        public override bool Interior => true;
        public override AtmosphereProfile Atmos => Atmosphere.Interior;
        public override string Music => "interior";

        public override void Build(AreaContext ctx)
        {
            var b = new Indoor(ctx) { height = 3.4f, wallIn = Pal.Wallpaper, floor = Pal.FloorBoards };
            b.Void();
            // hall (centre), parlour (west), study (east), dining (north)
            b.Room(0, 0, 8, 9, new Opening(2, 0, 1.8f), new Opening(3, 1f, 1.4f), new Opening(1, 1f, 1.2f), new Opening(0, 0, 1.6f));
            b.RoomMats(-8, 0.5f, 8, 10, Pal.Wallpaper, Pal.FloorBoards, new Opening(1, -0.5f, 1.4f), new Opening(3, 1.5f, 1.4f, true), new Opening(2, 0, 1.6f, true));
            b.RoomMats(7, 0.5f, 6, 8, Pal.PlasterDark, Pal.FloorBoards, new Opening(3, -0.5f, 1.2f), new Opening(1, 0.5f, 1.3f, true));
            b.RoomMats(0, 8.5f, 12, 8, Pal.Wallpaper, Pal.FloorBoards, new Opening(2, 0, 1.6f), new Opening(0, -3f, 1.4f, true), new Opening(0, 3f, 1.4f, true));
            b.DoorLeaf(0.9f, -4.52f, 0, 1.8f, false);
            b.Exit("exit", "Out to the Village", 0, -4.3f, 0, "village", "mansion", 1.8f);
            ctx.Spawn("door", new Vector3(0, 0, -3.2f), 0);

            // --- hall
            b.Prop("rug", () => Props.Rug(2.4f, 5f, new Color(.3f, .08f, .08f)), 0, 0, 0, 1, Kit.ColliderKind.None, 0.01f);
            b.Prop("painting", () => Props.Painting(1.4f, 1.1f, 4), -3.9f, -1.5f, 90, 1, Kit.ColliderKind.None, 1.6f);
            b.Prop("statue", () => Props.Statue(false), 3.3f, -3.3f, -40, 0.8f, Kit.ColliderKind.Capsule);
            b.Candelabra(-3.2f, -3.4f);
            b.Sconce(3.85f, -1.5f, -90); b.Sconce(-3.85f, 3f, 90);
            var portrait = b.Prop("painting2", () => Props.Painting(1.1f, 1.4f, 9), 3.9f, 3f, -90, 1, Kit.ColliderKind.None, 1.5f);
            ctx.Note("kolyan_portrait", "Portrait", portrait, "A Family Portrait",
                "A broad, red-cheeked man in a burgomaster's chain stands behind two children: a solemn boy of perhaps ten, and a girl with auburn hair who is not smiling, though the painter has tried very hard to make her look as if she might.\n\nSomeone has recently straightened the frame.");

            // --- parlour: the coffin, the hearth, Ireena
            b.Fireplace(-11.75f, 0.5f, 90);
            b.Prop("rug", () => Props.Rug(3f, 4f, new Color(.25f, .2f, .12f)), -8.5f, 0.5f, 0, 1, Kit.ColliderKind.None, 0.01f);
            var coffinPos = new Vector3(-7.4f, 0, 3.4f);
            if (!Game.I.Flag("burial_ready") && !Game.I.Flag("kolyan_buried"))
            {
                b.Prop("trestle", Trestles, coffinPos.x, coffinPos.z, 0);
                var cof = b.Prop("coffin_open", () => Props.Coffin(true), coffinPos.x, coffinPos.z, 0, 1, Kit.ColliderKind.Box, 0.72f);
                ctx.Use("kolyan", "Kolyan's Coffin", cof, "Pay respects", (u, s) =>
                {
                    ReadWindow.Show("Kolyan Indirovich", "The burgomaster lies in his best coat with his chain of office on his chest. He was a big man, and death has made him smaller in the particular way it does. There are no marks on him. His hands are folded around a sprig of dried garlic.\n\nWhoever laid him out did it carefully, and more than once — the coat has been buttoned and re-buttoned.");
                    if (!Game.I.Flag("paid_respects")) { Game.I.SetFlag("paid_respects"); Game.I.GiveXP(10); }
                }, false);
                foreach (var cx in new[] { -1f, 1f }) b.Candle(coffinPos.x + cx * 0.4f, 1.3f, coffinPos.z - 1.2f, 1.2f, 4f);
                b.Prop("candelabra_f", () => Props.Candelabra(true), coffinPos.x - 1.2f, coffinPos.z + 1.2f);
            }
            b.Prop("chair_f", () => Props.Chair(true), -9.5f, -2.2f, 30);
            b.Prop("chair_f", () => Props.Chair(true), -6.6f, -2.4f, -30);
            b.Prop("table1.1", () => Props.Table(1.1f, 0.7f), -8f, -2.8f, 0);
            b.Sconce(-4.15f, 3.5f, -90); b.Sconce(-11.85f, -3f, 90);
            var restGo = b.Prop("chair_f", () => Props.Chair(true), -10.4f, 1.8f, 100);
            ctx.Add<RestSpot>("parlour_rest", "Beside the Hearth", restGo, 2f);

            // --- study
            b.Prop("table1.6", () => Props.Table(1.6f, 0.9f), 7.5f, 1.2f, 90);
            b.Prop("chair_f", () => Props.Chair(true), 8.4f, 1.2f, -90);
            for (int i = 0; i < 3; i++) b.Prop("shelf" + i, () => Props.Bookshelf(40 + i), 6.2f + i * 1.3f - 1f, 4.25f, 180);
            b.Candle(7.2f, 0.9f, 0.8f);
            b.Sconce(9.85f, -2f, -90);
            var desk = b.Prop("book", AreaTavern.BookProp, 7.4f, 1.5f, 70, 1, Kit.ColliderKind.None, 0.86f);
            ctx.Note("kolyan_drafts", "Unfinished Letters", desk, "Unfinished Letters",
                "A stack of drafts of the same letter, each one abandoned a little further along.\n\n<i>To whatever brave souls this finds —</i>\n<i>To whatever brave souls this finds, I write from a village the world has forgotten —</i>\n<i>To whatever brave souls this finds, I write from a village the world has forgotten, beneath a castle that has not. My daughter —</i>\n\nThe last is not a draft at all. It says only: <i>Ismark, forgive me. Keep her away from the windows.</i>");
            var chest = ctx.Chest("study_chest", "Burgomaster's Strongbox", new Vector3(8.8f, 0, -2.6f), -90, Game.I.Flag("ismark_bonus") ? new[] { "ring_protection", "potion_healing" } : new[] { "potion_healing" }, Game.I.Flag("ismark_bonus") ? 80 : 40, true, 15, "key_mansion");

            // --- dining room: nailed shutters, the long table
            b.Prop("table3", () => Props.Table(3f, 1.1f), 0, 8.5f, 0);
            for (int i = 0; i < 3; i++) foreach (var sz in new[] { -1, 1 }) b.Prop("chair_f", () => Props.Chair(true), -1f + i, 8.5f + sz * 0.85f, sz > 0 ? 180 : 0);
            b.Candelabra(0, 8.5f);
            b.Sconce(-5.85f, 8f, 90); b.Sconce(5.85f, 8f, -90);
            foreach (var wx in new[] { -3f, 3f }) b.Prop("boards", NailedBoards, wx, 12.4f, 0, 1, Kit.ColliderKind.None, 1.6f);

            b.Fill(0, 0, new Color(1f, .75f, .55f), 0.7f, 12f);
            b.Fill(-8, 0.5f, new Color(1f, .7f, .5f), 0.6f, 10f);
            b.Fill(0, 8.5f, new Color(1f, .75f, .55f), 0.6f, 10f);

            // --- people
            if (!Game.I.Flag("burial_ready") && !Game.I.Flag("kolyan_buried"))
            {
                ctx.NPC("ireena", "Ireena Kolyanovna", Campaign.IreenaLook, Campaign.IreenaGear, new Vector3(-8.6f, 0, 2.2f), 60, "ireena");
                if (Game.I.Flag("ismark_agreed")) ctx.NPC("ismark_home", "Ismark Kolyanovich", Campaign.IsmarkLook, Campaign.IsmarkGear, new Vector3(-1.8f, 0, 1.8f), 160, "ismark_home");
            }
            ctx.Marker("win_w", new Vector3(-12.2f, 0, 2f));
            ctx.Marker("win_s", new Vector3(-8f, 0, -4.6f));
            ctx.Marker("win_n1", new Vector3(-3f, 0, 12.6f));
            ctx.Marker("win_n2", new Vector3(3f, 0, 12.6f));
            ctx.Marker("win_e", new Vector3(10.2f, 0, 1f));
        }

        static (Mesh, Material[]) Trestles()
        {
            var mb = new MeshBuilder(1);
            foreach (var z in new[] { -0.8f, 0.8f })
            {
                mb.AddBox(0, new Vector3(0, 0.68f, z), new Vector3(0.8f, 0.06f, 0.1f));
                foreach (var x in new[] { -0.32f, 0.32f }) { mb.Push(); mb.Translate(new Vector3(x, 0.34f, z)); mb.Rotate(Quaternion.Euler(0, 0, x > 0 ? -8 : 8)); mb.AddBox(0, Vector3.zero, new Vector3(0.06f, 0.7f, 0.06f)); mb.Pop(); }
            }
            return (mb.Build("trestles"), new[] { Pal.TimberDark });
        }

        public static (Mesh, Material[]) NailedBoards()
        {
            var mb = new MeshBuilder(2);
            for (int i = 0; i < 4; i++) { mb.Push(); mb.Translate(new Vector3(0, -0.5f + i * 0.33f, 0)); mb.Rotate(Quaternion.Euler(0, 0, (i % 2 == 0 ? 6 : -5))); mb.AddBox(0, Vector3.zero, new Vector3(1.6f, 0.2f, 0.04f)); mb.Pop(); }
            for (int i = 0; i < 8; i++) mb.AddBox(1, new Vector3(i % 2 == 0 ? -0.7f : 0.7f, -0.5f + (i / 2) * 0.33f, 0.03f), new Vector3(0.03f, 0.03f, 0.02f));
            return (mb.Build("boards"), new[] { Pal.Planks, Pal.Iron });
        }

        public override void OnEnter(AreaContext ctx, bool first)
        {
            if (first) Game.I.StartCoroutine(Arrive());
        }

        IEnumerator Arrive()
        {
            yield return new WaitForSeconds(1.2f);
            var s = Game.I.Selected;
            if (s) UIRoot.I.Bark(s, "Garlic on every lintel. Salt on every sill. They've been holding this house like a fortress.");
        }

        /// <summary>The night attack: the dead come through the windows while the party sleeps.</summary>
        public static IEnumerator NightAttack()
        {
            var g = Game.I;
            g.SetFlag("mansion_attack");
            Campaign.restInterrupted = true;
            g.busy = true;
            yield return UIRoot.I.FadeOut(0.5f, "Deep in the Night", "Something is scratching at the shutters.");
            Audio.Sfx.Play("howl", 1f, 0.8f);
            yield return new WaitForSecondsRealtime(1.6f);
            var ctx = g.area;
            // the attack scales with the party: a warning at level 1, a real siege by level 3
            int lvl = g.party.Where(c => c.sheet != null).Select(c => c.sheet.level).DefaultIfEmpty(1).Max();
            string[] spots = lvl <= 1 ? new[] { "win_w", "win_s", "win_e" } : lvl == 2 ? new[] { "win_w", "win_s", "win_n1", "win_e" } : new[] { "win_w", "win_s", "win_n1", "win_n2", "win_e" };
            string[] kinds = lvl <= 1 ? new[] { "zombie", "wolf", "wolf" } : lvl == 2 ? new[] { "zombie", "wolf", "zombie", "wolf" } : new[] { "zombie", "zombie", "wolf", "zombie", "wolf" };
            var spawned = new System.Collections.Generic.List<Actor>();
            for (int i = 0; i < spots.Length; i++)
            {
                if (!ctx.markers.TryGetValue(spots[i], out var m)) continue;
                var p = m.position;
                var inside = new Vector3(Mathf.Clamp(p.x, -11.2f, 9.3f), 0, Mathf.Clamp(p.z, -3.8f, 11.8f));
                var a = ctx.Monster("night" + i, kinds[i], inside, 0, "mansion_night", true, 30f);
                if (a) spawned.Add(a);
                FX.Burst(inside + Vector3.up, FxKind.Necrotic, 1.4f);
            }
            // Ireena bars herself in upstairs; Ismark stands with you
            var ire = g.FindActor("ireena"); if (ire) ire.gameObject.SetActive(false);
            var ism = g.FindActor("ismark_home");
            if (ism)
            {
                ism.c.faction = Faction.Ally; ism.c.hp = ism.c.maxHP = 24; ism.c.baseAC = 14;
                ism.talkable = false;
                if (ism.anim != null) ism.anim.SetCombat(true);
            }
            Atmosphere.I.Apply(Atmosphere.DeathHouse);
            yield return UIRoot.I.FadeIn(0.8f);
            g.busy = false;
            UIRoot.I.Bark(g.Selected, "They're through the shutters! On your feet!");
            yield return new WaitForSeconds(0.6f);
            var first = spawned.FirstOrDefault(a => a);
            if (first) Combat.CombatManager.I.StartCombat(first, null);
            yield return Campaign.WaitForCombat();
            if (!g.party.Any(c => c.Active)) yield break;
            Atmosphere.I.Apply(Atmosphere.Interior);
            if (ire) ire.gameObject.SetActive(true);
            if (ism && ism.c.hp > 0) { ism.c.faction = Faction.Neutral; ism.talkable = true; ism.anim?.SetCombat(false); }
            g.SetFlag("mansion_attack_won");
            g.GiveXP(100, "holding the manor");
            yield return new WaitForSeconds(1f);
            if (ire) Dialogue.DialogueRunner.I.Begin("ireena_after_attack", ire, g.Selected);
        }
    }

    /// <summary>The village church: pews, a cold altar, a priest who has not slept, and screaming under the floor.</summary>
    public class AreaChurch : AreaDef
    {
        public override string Id => "church";
        public override string Title => "The Church";
        public override string Subtitle => "Where the Morninglord is not answering";
        public override bool Interior => true;
        public override AtmosphereProfile Atmos => Atmosphere.Interior;
        public override string Music => "interior";

        public override void Build(AreaContext ctx)
        {
            var b = new Indoor(ctx) { height = 5f, wallIn = Pal.Ashlar, floor = Pal.Stone };
            b.Void();
            b.Room(0, 0, 9, 16, new Opening(2, 0, 2f), new Opening(3, -3, 1f, true) { sill = 2f, top = 4.2f }, new Opening(3, 2, 1f, true) { sill = 2f, top = 4.2f }, new Opening(1, -3, 1f, true) { sill = 2f, top = 4.2f }, new Opening(1, 2, 1f, true) { sill = 2f, top = 4.2f });
            b.DoorLeaf(1f, -8.02f, 0, 2f, true);
            b.Exit("exit", "Out to the Churchyard", 0, -7.8f, 0, "village", "church", 2f);
            ctx.Spawn("door", new Vector3(0, 0, -6.6f), 0);
            for (int i = 0; i < 5; i++)
                foreach (var sx in new[] { -1, 1 })
                    b.Prop("pew2.8", () => Props.Pew(2.8f), sx * 2.3f, -4.8f + i * 1.7f, 180);
            b.Prop("altar", Props.Altar, 0, 6.3f, 180);
            b.Prop("rug", () => Props.Rug(1.6f, 9f, new Color(.4f, .3f, .12f)), 0, 0, 0, 1, Kit.ColliderKind.None, 0.01f);
            b.Candelabra(-1.6f, 6.6f); b.Candelabra(1.6f, 6.6f);
            b.Sconce(-4.35f, 0.5f, 90); b.Sconce(4.35f, 0.5f, -90); b.Sconce(-4.35f, -5f, 90); b.Sconce(4.35f, -5f, -90);
            b.Prop("statue", () => Props.Statue(false), 0, 7.4f, 180, 1.1f, Kit.ColliderKind.Capsule);
            Kit.SpotLight(ctx.root, new Vector3(0, 4.8f, 3.6f), new Vector3(60, 0, 0), new Color(1f, .85f, .6f), 3f, 9f, 50f);
            b.Fill(0, 0, new Color(.9f, .8f, .65f), 0.9f, 16f);
            // the trapdoor to the undercroft
            var hatch = b.Prop("trapdoor", Trapdoor, 3.2f, 5.3f, 0, 1, Kit.ColliderKind.None, 0.02f);
            var t = ctx.Door("undercroft", "Undercroft Trapdoor", new Vector3(3.2f, 0, 5.3f), 180, "undercroft", "stairs", new Vector3(1.2f, 0.6f, 1.2f), true, "key_undercroft");
            t.lockedText = "Barred from above with an iron bar and a padlock the size of your fist. Something beneath it goes very quiet when you touch it.";
            t.lockDC = 18;
            t.label = Game.I.Flag("doru_dead") ? "Undercroft" : "Undercroft Trapdoor";
            if (!Game.I.Flag("doru_dead")) ctx.Trigger("doru_scream", new Vector3(2.5f, 0, 3.5f), 3f, () => { Audio.Sfx.Play("whisper", 1f, 0.5f); UIRoot.I.Bark(Game.I.Selected, "...That came from under the floor."); });
            if (!Game.I.Flag("kolyan_buried") || Game.I.Flag("burial_done_here"))
                ctx.NPC("donavich", "Father Donavich", Campaign.DonavichLook, new GearLook { armor = ArmorVisual.Robe }, new Vector3(0, 0, 5.2f), 0, "donavich");
        }

        static (Mesh, Material[]) Trapdoor()
        {
            var mb = new MeshBuilder(2);
            mb.AddBox(0, new Vector3(0, 0.02f, 0), new Vector3(1.1f, 0.05f, 1.1f));
            mb.AddBox(1, new Vector3(0, 0.07f, 0), new Vector3(1.3f, 0.05f, 0.08f));
            mb.AddBox(1, new Vector3(0.45f, 0.1f, 0), new Vector3(0.15f, 0.12f, 0.1f));
            return (mb.Build("trapdoor"), new[] { Pal.Planks, Pal.Iron });
        }

        public override void OnEnter(AreaContext ctx, bool first)
        {
            if (first) Game.I.StartCoroutine(Arrive());
        }

        IEnumerator Arrive()
        {
            yield return new WaitForSeconds(0.8f);
            Audio.Sfx.Play("bell_toll", 0.4f, 0.7f);
            yield return new WaitForSeconds(1.2f);
            var d = Game.I.FindActor("donavich");
            if (d && !Game.I.Flag("doru_dead")) UIRoot.I.Bark(d, "...and I will not open it, I will not, I will not, give me strength to not open it...");
        }
    }

    /// <summary>The undercroft beneath the church, where Doru waits.</summary>
    public class AreaUndercroft : AreaDef
    {
        public override string Id => "undercroft";
        public override string Title => "The Undercroft";
        public override string Subtitle => "";
        public override bool Interior => true;
        public override bool RestUnsafe => true;
        public override AtmosphereProfile Atmos => Atmosphere.Dungeon;
        public override string Music => Game.I.Flag("doru_dead") ? "interior" : "strahd";
        public override float CameraMaxZoom => 13f;

        public override void Build(AreaContext ctx)
        {
            var b = new Indoor(ctx) { height = 2.8f, wallIn = Pal.Stone, floor = Pal.Stone };
            b.Void();
            b.Room(0, 0, 8, 10, new Opening(2, 2.4f, 1.2f));
            b.Prop("stairs", () => Props.Stairs(1.2f, 2.6f, 3f, 10), 2.4f, -3.8f, 180, 1, Kit.ColliderKind.None);
            b.Exit("up", "Back up to the Church", 2.4f, -5.2f, 0, "church", "door", 1.4f);
            ctx.Spawn("stairs", new Vector3(2.4f, 0, -2.8f), 0);
            for (int i = 0; i < 3; i++) b.Prop("barrel", Props.Barrel, -3.3f, -3.8f + i * 0.9f, i * 40, 1, Kit.ColliderKind.Capsule);
            b.Prop("crate0.8", () => Props.Crate(0.8f), 3.2f, 3.6f, 20);
            b.Prop("bonepile", () => Props.BonePile(3), -2.2f, 3.8f, 0, 1, Kit.ColliderKind.None);
            b.Prop("chains", () => Props.HangingCage(), 0.5f, 3.8f, 0, 1, Kit.ColliderKind.None);
            b.Candle(3.4f, 0.85f, 3.4f, 1.2f, 5f);
            b.Fill(0, 0, new Color(.6f, .55f, .7f), 0.4f, 10f);
            var chest = ctx.Chest("doru_chest", "Doru's Chest", new Vector3(-3.3f, 0, 3.8f), 90, new[] { "scroll_revivify", "holy_water", "silver_locket" }, 25);
            if (!Game.I.Flag("doru_dead"))
            {
                var doru = ctx.Monster("doru", "vampire_spawn", new Vector3(0, 0, 2.6f), 180, "doru", false, 4f);
                if (doru)
                {
                    doru.talkable = true; doru.dialogue = "doru"; doru.c.faction = Faction.Neutral;
                    // a starving spawn: tough enough to matter whenever the party comes down here
                    int lvl = Game.I.party.Where(c => c.sheet != null).Select(c => c.sheet.level).DefaultIfEmpty(1).Max();
                    doru.c.maxHP = doru.c.hp = 22 + 6 * lvl;
                }
            }
        }

        public override void OnEnter(AreaContext ctx, bool first)
        {
            if (!Game.I.Flag("doru_dead")) Game.I.StartCoroutine(Meet());
        }

        IEnumerator Meet()
        {
            yield return new WaitForSeconds(1f);
            var d = Game.I.FindActor("doru");
            if (d) Dialogue.DialogueRunner.I.Begin("doru", d, Game.I.Selected);
        }
    }

    public static partial class Campaign
    {
        static partial void RegisterMoreAreasImpl()
        {
            AreaLoader.Register(new AreaMansion());
            AreaLoader.Register(new AreaChurch());
            AreaLoader.Register(new AreaUndercroft());
            AreaLoader.Register(new AreaTserPool());
            RegisterDeathHouse();
        }

        static partial void RegisterDeathHouseImpl();
        static void RegisterDeathHouse() => RegisterDeathHouseImpl();

        static partial void OnRestStoryImpl()
        {
            var g = Game.I;
            if (g.areaId == "mansion" && g.Flag("met_ireena") && !g.Flag("mansion_attack"))
                g.StartCoroutine(AreaMansion.NightAttack());
        }
    }
}
