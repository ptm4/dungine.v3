using System.Collections;
using System.Collections.Generic;
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
    /// <summary>
    /// Tser Pool: where the Luna river widens into a still pool below the western road, and the Vistani have drawn
    /// their painted wagons into a ring around a great fire. Madam Eva keeps her tent at the water's edge.
    /// </summary>
    public class AreaTserPool : AreaDef
    {
        public override string Id => "tser_pool";
        public override string Title => "Tser Pool Encampment";
        public override string Subtitle => "Fire, music, and a fortune waiting";
        public override AtmosphereProfile Atmos => Evening;
        public override string Music => "tavern";

        static AtmosphereProfile Evening
        {
            get
            {
                var p = Atmosphere.Mists;
                p.name = "Tser evening";
                p.sunIntensity = 2.0f; p.sunColor = new Color(.98f, .78f, .62f); p.sunEuler = new Vector3(34, -70, 0);
                p.ambientSky = new Color(.62f, .58f, .72f); p.ambientEquator = new Color(.46f, .4f, .46f); p.ambientGround = new Color(.2f, .17f, .16f);
                p.skyHorizon = new Color(.7f, .48f, .44f); p.cloudLight = new Color(.85f, .55f, .45f); p.stars = .15f;
                p.exposure = .5f; p.temperature = 6f;
                return p;
            }
        }

        static readonly List<Vector2> River = new List<Vector2> { new Vector2(-120, 40), new Vector2(-70, 32), new Vector2(-40, 34), new Vector2(-18, 30), new Vector2(10, 38), new Vector2(50, 46), new Vector2(120, 52) };
        static readonly Vector2 Pool = new Vector2(-30, 26);
        static readonly Vector2 Camp = new Vector2(0, 4);

        static float RiverDepth(float x, float z)
        {
            float d = Nature.DistToPolyline(new Vector2(x, z), River, out _);
            float river = MathX.Smoothstep(9f, 4f, d);
            float pool = MathX.Smoothstep(15f, 7f, Vector2.Distance(new Vector2(x, z), Pool));
            return -1.6f * Mathf.Max(river, pool);
        }

        public override void Build(AreaContext ctx)
        {
            var o = new Outdoor(ctx) { seed = 21, hills = 3.2f, wallStart = 45f, wallSteep = 0.5f, roadWidth = 3.0f, forestFloorStart = 26f };
            o.road = new List<Vector2> { new Vector2(118, -30), new Vector2(80, -24), new Vector2(40, -14), new Vector2(18, -6), new Vector2(-10, -12), new Vector2(-60, -20), new Vector2(-118, -26) };
            o.Lane(2.4f, new Vector2(8, -6), Camp);
            o.clearings.Add((Camp, 16f));
            o.flats.Add((Camp, 14f));
            o.flats.Add((new Vector2(-19, 14), 5f));
            o.extraHeight = RiverDepth;
            o.blockers.Add(new Rect(Camp.x - 16, Camp.y - 16, 32, 32));
            o.BuildTerrain();

            // the river and the pool
            var water = MatLib.LitTransparent(new Color(.08f, .12f, .14f, .82f), .95f);
            var wmb = new MeshBuilder(1);
            wmb.AddBox(0, new Vector3(0, -0.75f, 40f), new Vector3(240, 0.02f, 40), 4);
            wmb.AddBox(0, new Vector3(Pool.x, -0.75f, Pool.y), new Vector3(30, 0.02f, 30), 4);
            var wgo = Kit.FromBuilder(wmb, new[] { water }, ctx.root, "Water", Kit.ColliderKind.None, false);
            // reeds along the pool
            var rnd = new System.Random(3);
            for (int i = 0; i < 70; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2, r = 10f + (float)rnd.NextDouble() * 5f;
                var p = new Vector2(Pool.x + Mathf.Cos(a) * r, Pool.y + Mathf.Sin(a) * r);
                if (p.y < Pool.y - 11f) continue;
                ctx.Prop("reeds" + (i % 3), () => Reeds(i % 3), ctx.G3(p.x, p.y), (float)rnd.NextDouble() * 360, 0.8f + (float)rnd.NextDouble() * 0.5f);
            }

            // ---------------------------------------------------------------- the camp
            var fireP = ctx.G3(Camp.x, Camp.y);
            ctx.Prop("campfire", AreaSvalichRoad.CampfireMesh, fireP, 0, 1.6f, Kit.ColliderKind.Capsule);
            var fire = FX.MakePS("bonfire", fireP + Vector3.up * 0.3f, ProcTex.FogPuff, true, 0, 1.0f, 1.2f, 0.7f, new Color(1f, .55f, .2f, .95f), new Color(.5f, .1f, 0, 0), -1.0f, 0.45f, ParticleSystemShapeType.Circle, 5f, true, 40f);
            fire.transform.SetParent(ctx.root, true);
            Kit.PointLight(ctx.root, fireP + Vector3.up * 1.4f, new Color(1f, .58f, .28f), 9f, 18f, true);
            Kit.Motes(ctx.root, fireP + Vector3.up * 2f, new Vector3(2, 3, 2), new Color(1f, .6f, .2f, .9f), 40, 0.05f, 0.9f);
            Color[,] paint =
            {
                { new Color(.62f, .12f, .12f), new Color(.85f, .65f, .2f) }, { new Color(.14f, .32f, .5f), new Color(.8f, .55f, .2f) }, { new Color(.2f, .42f, .22f), new Color(.8f, .2f, .2f) },
                { new Color(.55f, .38f, .1f), new Color(.2f, .3f, .55f) }, { new Color(.42f, .14f, .38f), new Color(.85f, .75f, .3f) }, { new Color(.62f, .3f, .1f), new Color(.18f, .4f, .38f) },
            };
            for (int i = 0; i < 6; i++)
            {
                float a = (i / 6f) * Mathf.PI * 2 + 0.3f;
                var wp = new Vector2(Camp.x + Mathf.Cos(a) * 10.5f, Camp.y + Mathf.Sin(a) * 10.5f);
                int k = i;
                var vardo = ctx.Obstacle("vardo" + i, () => Vardo(paint[k, 0], paint[k, 1], k), ctx.G3(wp.x, wp.y), -a * Mathf.Rad2Deg);
                Occluders.RegisterGroup(vardo.transform);
                Kit.PointLight(ctx.root, ctx.G3(wp.x, wp.y) + new Vector3(Mathf.Cos(a) * -1.6f, 2.3f, Mathf.Sin(a) * -1.6f), new Color(1f, .7f, .4f), 1.6f, 6f, false);
            }
            // benches and bits around the fire
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI / 2 + 0.8f;
                ctx.Prop("log", () => Nature.FallenLog(3), ctx.G3(Camp.x + Mathf.Cos(a) * 4f, Camp.y + Mathf.Sin(a) * 4f), -a * Mathf.Rad2Deg, 0.55f, Kit.ColliderKind.Box);
            }
            ctx.Prop("barrel", Props.Barrel, ctx.G3(6.5f, -2f), 0, 1, Kit.ColliderKind.Capsule);
            ctx.Prop("barrel", Props.Barrel, ctx.G3(7.1f, -1.3f), 30, 1, Kit.ColliderKind.Capsule);
            ctx.Prop("crate0.8", () => Props.Crate(0.8f), ctx.G3(-7f, -3.5f), 20, 1, Kit.ColliderKind.Box);
            var wine = ctx.Prop("crate0.6", () => Props.Crate(0.6f), ctx.G3(6.2f, -3.2f), 10, 1, Kit.ColliderKind.Box);
            ctx.Stash("vistani_wine", "Vistani Wine Crate", wine, new[] { "wine", "wine", "wine" });

            // Madam Eva's tent at the water's edge
            var tentP = ctx.G3(-19, 14);
            Occluders.RegisterGroup(ctx.Obstacle("tent", EvaTent, tentP, 200).transform);
            Kit.PointLight(ctx.root, tentP + new Vector3(0, 1.6f, 0), new Color(.8f, .5f, 1f), 2.4f, 7f, false);
            Kit.Motes(ctx.root, tentP + new Vector3(0, 1.5f, 0), new Vector3(3, 2, 3), new Color(.7f, .5f, 1f, .7f), 20, 0.04f, 0.2f);

            o.Forest(520, 60, 10f);
            o.Grass(1600);
            o.RoadFog(34f, new Color(.72f, .7f, .76f, .08f));
            Kit.FogBank(ctx.root, new Vector3(Pool.x, -0.3f, Pool.y), new Vector3(24, 1, 24), new Color(.8f, .78f, .85f, .22f), 40, 7f, 0.1f);
            foreach (var (t, side) in new[] { (0.15f, 1), (0.42f, -1), (0.56f, 1), (0.85f, -1) }) o.RoadLamp(t, side, true);

            // ---------------------------------------------------------------- people
            ctx.Spawn("east", ctx.G3(108, -28.5f), -95);
            ctx.Spawn("camp", ctx.G3(10, -4), -60);
            var rnd2 = new System.Random(77);
            (Vector2 p, float yaw, int look, string bark)[] folk =
            {
                (new Vector2(3.5f, 1.5f), 230, 0, "\"Dance, outsiders! In Barovia you dance while you can.\""),
                (new Vector2(-3.8f, 2.5f), 120, 1, "\"Madam Eva has been expecting you for three days. She has been expecting everyone for three hundred years.\""),
                (new Vector2(1.5f, 7.5f), 190, 2, "\"The pool is older than the castle. Don't drink from it. Don't look into it too long, either.\""),
                (new Vector2(-2f, -3.5f), 20, 0, "\"You came from the village? With her? *He looks at Ireena, then quickly away.* Bold. Or foolish. Here, those are the same word.\""),
            };
            for (int i = 0; i < folk.Length; i++)
            {
                var f = folk[i];
                var a = ctx.NPC("vistani_camp" + i, i % 2 == 0 ? "Vistani Dancer" : "Vistani", i == 3 ? VistaniMan(rnd2) : Campaign.VistaniLook(f.look), new GearLook(), ctx.G3(f.p.x, f.p.y), f.yaw);
                if (a) a.barkOnClick = f.bark;
            }
            ctx.NPC("arrigal", "Arrigal", VistaniMan(new System.Random(9)), new GearLook { armor = ArmorVisual.Leather, main = WeaponVisual.Scimitar }, ctx.G3(14, -5), -100, "arrigal");
            // she sits at the mouth of her tent, facing the camp
            var evaP = tentP + Quaternion.Euler(0, 200, 0) * new Vector3(0, 0, 3.4f);
            evaP.y = ctx.GroundY(evaP.x, evaP.z);
            ctx.Prop("chair_f", () => Props.Chair(true), evaP, 200, 1, Kit.ColliderKind.Box);
            var eva = ctx.NPC("eva", "Madam Eva", EvaLook, new GearLook { armor = ArmorVisual.Robe }, evaP, 200, "madam_eva", MotionStyle.Noble);
            eva?.Sit(0.46f);
            Kit.PointLight(ctx.root, evaP + Quaternion.Euler(0, 200, 0) * new Vector3(0.6f, 1.2f, 0.8f), new Color(1f, .7f, .45f), 1.8f, 4f, false);
            if (Game.I.Flag("ireena_escort"))
            {
                var ire = ctx.NPC("ireena_f", "Ireena Kolyanovna", Campaign.IreenaLook, Campaign.IreenaGear, ctx.G3(106, -27), -95, "ireena_road");
                if (ire) ire.gameObject.AddComponent<Follower>();
            }
            ctx.Trigger("camp_music", ctx.G3(22, -6), 10f, () => UIRoot.I.Bark(Game.I.Selected, "Fiddles. Actual music. And the smell of roasting meat — whatever it was."));
            ctx.Trigger("exit_east", ctx.G3(116, -29), 5f, () => Game.I.GoToArea("village", "west")).repeat = true;
            var west = ctx.Trigger("exit_west", ctx.G3(-114, -26), 6f, () =>
            {
                UIRoot.I.Bark(Game.I.Selected, Game.I.Flag("eva_read") ? "Vallaki is that way. Let's rest by the fire tonight and set out at first light." : "We should see the fortune-teller first. The Vistani say she's been waiting for us.");
                PartyController.I.MoveParty(ctx.G3(-100, -24));
            });
            if (west) west.repeat = true;
            Kit.FogBank(ctx.root, new Vector3(118, ctx.GroundY(118, -30) + 1f, -30), new Vector3(6, 3, 26), new Color(.8f, .8f, .84f, .5f), 40, 8f, 0.1f);
            Kit.FogBank(ctx.root, new Vector3(-118, ctx.GroundY(-118, -26) + 1f, -26), new Vector3(6, 3, 26), new Color(.8f, .8f, .84f, .5f), 40, 8f, 0.1f);
            Buildings.CastleRavenloft(ctx.root, new Vector3(200, 22, 170), 235, 1.25f);
        }

        static Appearance VistaniMan(System.Random r) => new Appearance
        {
            race = RaceId.Human, bodyType = 0, strong = r.NextDouble() < .5, skin = new[] { new Color(.68f, .48f, .34f), new Color(.57f, .37f, .25f) }[r.Next(2)], hair = new Color(.1f, .08f, .07f),
            hairStyle = 8, beardStyle = 5, eyes = new Color(.42f, .29f, .18f), cloth1 = new Color(.55f, .12f, .14f), cloth2 = new Color(.8f, .62f, .25f)
        };

        static Appearance EvaLook => new Appearance { race = RaceId.Human, bodyType = 1, hunched = true, gaunt = true, age = 1f, skin = new Color(.66f, .5f, .4f), hair = new Color(.86f, .85f, .82f), hairStyle = 9, eyes = new Color(.55f, .45f, .7f), cloth1 = new Color(.3f, .12f, .36f), cloth2 = new Color(.85f, .68f, .3f) };

        // ---------------------------------------------------------------------------------- props

        /// <summary>A Vistani vardo: a painted wagon with a barrel roof, carved trim and big spoked wheels.</summary>
        static (Mesh, Material[]) Vardo(Color body, Color trim, int seed)
        {
            var mb = new MeshBuilder(4) { uvScale = 0.8f };
            // body
            mb.AddBox(0, new Vector3(0, 1.5f, 0), new Vector3(1.9f, 1.5f, 3.4f));
            // barrel roof
            for (int i = 0; i < 12; i++)
            {
                float a0 = Mathf.PI * i / 12f, a1 = Mathf.PI * (i + 1) / 12f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * 1.05f, 2.25f + Mathf.Sin(a0) * 0.75f, 0), p1 = new Vector3(Mathf.Cos(a1) * 1.05f, 2.25f + Mathf.Sin(a1) * 0.75f, 0);
                mb.AddQuad(1, p1 + Vector3.back * 1.85f, p1 + Vector3.forward * 1.85f, p0 + Vector3.forward * 1.85f, p0 + Vector3.back * 1.85f);
            }
            foreach (var z in new[] { -1.72f, 1.72f })
            {
                mb.Push(); mb.Translate(new Vector3(0, 0, z));
                for (int i = 0; i < 12; i++)
                {
                    float a0 = Mathf.PI * i / 12f, a1 = Mathf.PI * (i + 1) / 12f;
                    int c = mb.AddVertex(new Vector3(0, 2.25f, 0), Vector3.forward * Mathf.Sign(z), new Vector2(.5f, .5f));
                    int v0 = mb.AddVertex(new Vector3(Mathf.Cos(a0) * 0.98f, 2.25f + Mathf.Sin(a0) * 0.7f, 0), Vector3.forward * Mathf.Sign(z), new Vector2(0, 0));
                    int v1 = mb.AddVertex(new Vector3(Mathf.Cos(a1) * 0.98f, 2.25f + Mathf.Sin(a1) * 0.7f, 0), Vector3.forward * Mathf.Sign(z), new Vector2(1, 0));
                    if (z > 0) mb.Tri(0, c, v1, v0); else mb.Tri(0, c, v0, v1);
                }
                mb.Pop();
            }
            // trim: base rail, corner posts, a painted band, the door and steps at the back
            mb.AddBox(2, new Vector3(0, 0.76f, 0), new Vector3(2.0f, 0.1f, 3.5f));
            mb.AddBox(2, new Vector3(0, 2.26f, 0), new Vector3(2.0f, 0.08f, 3.5f));
            foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 }) mb.AddBox(2, new Vector3(sx * 0.97f, 1.5f, sz * 1.72f), new Vector3(0.1f, 1.5f, 0.1f));
            foreach (var sx in new[] { -1, 1 }) mb.AddBox(2, new Vector3(sx * 0.96f, 1.75f, 0), new Vector3(0.04f, 0.16f, 3.3f));
            foreach (var sx in new[] { -1, 1 }) { mb.AddBox(3, new Vector3(sx * 0.97f, 1.7f, 0.6f), new Vector3(0.03f, 0.5f, 0.6f)); }
            mb.AddBox(2, new Vector3(0, 1.55f, -1.76f), new Vector3(0.8f, 1.3f, 0.05f));
            for (int st = 0; st < 3; st++) mb.AddBox(2, new Vector3(0, 0.2f + st * 0.2f, -2.0f - (2 - st) * 0.22f), new Vector3(0.7f, 0.05f, 0.24f));
            // wheels
            foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1.1f, 1.2f })
                {
                    float r = sz > 0 ? 0.62f : 0.72f;
                    mb.Push(); mb.Translate(new Vector3(sx * 1.08f, r, sz)); mb.Rotate(Quaternion.Euler(0, 0, 90));
                    mb.AddCylinder(2, new Vector3(0, -0.05f, 0), r, r, 0.1f, 18, false);
                    mb.AddCylinder(2, new Vector3(0, -0.07f, 0), 0.13f, 0.13f, 0.14f, 8);
                    for (int k = 0; k < 8; k++) { mb.Push(); mb.Rotate(Quaternion.Euler(0, k * 22.5f, 0)); mb.AddBox(2, Vector3.zero, new Vector3(0.04f, 0.04f, r * 1.9f)); mb.Pop(); }
                    mb.Pop();
                }
            // shafts
            foreach (var sx in new[] { -0.45f, 0.45f }) mb.AddBox(2, new Vector3(sx, 0.7f, 2.9f), new Vector3(0.07f, 0.07f, 2.2f));
            return (mb.Build("vardo" + seed), new[] { MatLib.Lit(body, TexId.WoodPlank, .25f, 0, 1f, .6f), MatLib.Lit(Color.Lerp(body, Color.black, .35f), TexId.Shingle, .25f, 0, 1f, .8f), MatLib.Lit(trim, TexId.WoodPlank, .35f, 0, 1f, .5f), Pal.WindowLit });
        }

        static (Mesh, Material[]) EvaTent()
        {
            var mb = new MeshBuilder(3) { uvScale = 0.6f };
            // a tall many-sided tent with a scalloped valance and a pole through the peak
            int sides = 10; float r = 2.8f, wallH = 1.7f, peak = 4.4f;
            for (int i = 0; i < sides; i++)
            {
                if (i == 0) continue; // the open door flap faces forward
                float a0 = i * Mathf.PI * 2 / sides, a1 = (i + 1) * Mathf.PI * 2 / sides;
                Vector3 b0 = new Vector3(Mathf.Sin(a0) * r, 0, Mathf.Cos(a0) * r), b1 = new Vector3(Mathf.Sin(a1) * r, 0, Mathf.Cos(a1) * r);
                mb.AddQuad(i % 2, b0, b1, b1 + Vector3.up * wallH, b0 + Vector3.up * wallH);
                mb.AddQuad(i % 2, b1, b0, b0 + Vector3.up * wallH, b1 + Vector3.up * wallH);
            }
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2 / sides, a1 = (i + 1) * Mathf.PI * 2 / sides;
                Vector3 b0 = new Vector3(Mathf.Sin(a0) * r * 1.08f, wallH, Mathf.Cos(a0) * r * 1.08f), b1 = new Vector3(Mathf.Sin(a1) * r * 1.08f, wallH, Mathf.Cos(a1) * r * 1.08f);
                int t = mb.AddVertex(new Vector3(0, peak, 0), Vector3.up, new Vector2(.5f, 1));
                int v0 = mb.AddVertex(b0, (b0.normalized + Vector3.up).normalized, new Vector2(0, 0));
                int v1 = mb.AddVertex(b1, (b1.normalized + Vector3.up).normalized, new Vector2(1, 0));
                mb.Tri(i % 2, t, v0, v1); mb.Tri(i % 2, t, v1, v0);
                // valance scallop
                mb.AddBox(2, (b0 + b1) * 0.5f + Vector3.down * 0.12f, new Vector3(0.9f, 0.22f, 0.04f));
            }
            mb.AddCylinder(2, Vector3.zero, 0.06f, 0.05f, peak + 0.5f, 6);
            mb.AddEllipsoid(2, new Vector3(0, peak + 0.55f, 0), Vector3.one * 0.12f, 5, 7);
            // the table, cloth and the crystal inside
            mb.AddCylinder(2, new Vector3(0.4f, 0, 0.4f), 0.5f, 0.5f, 0.75f, 12);
            return (mb.Build("evatent"), new[] { Pal.Fabric(new Color(.34f, .12f, .38f)), Pal.Fabric(new Color(.62f, .45f, .15f)), MatLib.Lit(new Color(.75f, .6f, .3f), TexId.Metal, .5f, .6f) });
        }

        static (Mesh, Material[]) Reeds(int seed)
        {
            var mb = new MeshBuilder(1);
            var rnd = new System.Random(seed);
            for (int i = 0; i < 9; i++)
            {
                var p = new Vector3(((float)rnd.NextDouble() - .5f) * 0.6f, 0, ((float)rnd.NextDouble() - .5f) * 0.6f);
                mb.Push(); mb.Translate(p); mb.Rotate(Quaternion.Euler(((float)rnd.NextDouble() - .5f) * 16, 0, ((float)rnd.NextDouble() - .5f) * 16));
                mb.AddCylinder(0, Vector3.zero, 0.015f, 0.004f, 1.1f + (float)rnd.NextDouble() * 0.8f, 4, false);
                if (i % 3 == 0) mb.AddCylinder(0, new Vector3(0, 1.0f, 0), 0.03f, 0.03f, 0.18f, 5);
                mb.Pop();
            }
            return (mb.Build("reeds" + seed), new[] { MatLib.Lit(new Color(.42f, .4f, .26f), null, .1f) });
        }

        public override void OnEnter(AreaContext ctx, bool first)
        {
            if (first)
            {
                Game.I.journal.Update("burgomaster", "The Burgomaster's Daughter", "We followed the western road with Ireena to the Vistani camp at Tser Pool. The Vistani say the fortune-teller, Madam Eva, has been expecting us.");
                Game.I.StartCoroutine(Arrive());
            }
        }

        IEnumerator Arrive()
        {
            yield return new WaitForSeconds(2f);
            var ire = Game.I.FindActor("ireena_f");
            if (ire) UIRoot.I.Bark(ire, "Firelight. I'd forgotten how it looks when nobody is afraid of it.");
        }
    }

    public static partial class Campaign
    {
        /// <summary>An original fortune-telling deck: each card is drawn for one question of the reading.</summary>
        static readonly (string name, string vision, string place)[] BladeCards =
        {
            ("The Lantern-Bearer", "A thin light moving through a house of the dead. It does not flicker.", "the crypts beneath the castle, among the count's forebears"),
            ("The Miller's Stone", "A great wheel turning where no wind blows, grinding something that screams.", "the old windmill on the hill above the Vallaki road"),
            ("The Drowned Bell", "A bell ringing under still water, calling a congregation that never comes.", "the bell tower of a town that forgot its saint"),
            ("The Ninth Chair", "A long table, and at its head a chair nobody has dared to sit in for four hundred years.", "the great dining hall of Castle Ravenloft"),
        };
        static readonly (string name, string vision, string place)[] BookCards =
        {
            ("The Quiet Scholar", "Ink that writes itself when the candle goes out.", "a study high in the castle, behind a door that pretends to be a wall"),
            ("The Raven Queen", "A black feather laid across an open page, keeping the place.", "a nest of ravens at the edge of the Svalich woods"),
            ("The Beggar's Cup", "Coins that fall and never land.", "the storerooms of a wealthy family in Vallaki"),
            ("The Hollow Oak", "A tree with a door carved into its trunk, and a light behind the door.", "the grove where the druids keep their fire"),
        };
        static readonly (string name, string vision, string place)[] SymbolCards =
        {
            ("The Sunless Saint", "Hands folded around something that glows through the fingers.", "the tomb of a saint whose bones were stolen"),
            ("The Iron Maiden", "A shield hung on a wall above a cold hearth, and rust weeping from it like tears.", "the armory of a ruined abbey on the mountainside"),
            ("The Weeping Bride", "A veil, a ring, and a mirror that shows the wrong face.", "a chapel where no wedding was ever finished"),
            ("The Winter Road", "Footprints in snow that lead up a mountain and never come back.", "a lake of ice at the top of the valley"),
        };
        static readonly (string name, string vision, string ally)[] AllyCards =
        {
            ("The Unhorsed Knight", "A warrior sitting by a dead fire, polishing armour nobody will see.", "a knight without a horse, who still keeps his oath"),
            ("The Hunter's Mask", "A face painted with the stripes of a hunted thing.", "a hunter who hunts the things that hunt"),
            ("The Mad Magician", "A tower whose windows flash with coloured light all night long.", "a wizard everyone agrees is mad, and nobody agrees is wrong"),
            ("The Grieving Priest", "A candle carried into a crypt by a man who is not afraid of the dark.", "a priest who has already lost everything, and so has nothing to fear"),
        };
        static readonly (string name, string vision, string place)[] LairCards =
        {
            ("The Tower Heart", "A spiral stair climbing to a room with no windows and one throne.", "the highest room of the castle's tallest tower"),
            ("The Organ Hall", "Music played by hands that are not there.", "the castle chapel, beneath the great organ"),
            ("The Resting Place", "A coffin in a room of earth, and flowers laid by someone who loved her.", "the crypt where he keeps the only grave he visits"),
            ("The Throne of Wings", "A chair made of bats that have learned to be still.", "his audience hall, before his own throne"),
        };

        static int Draw(string key, int n)
        {
            var g = Game.I;
            if (g.GetFlag(key) > 0) return g.GetFlag(key) - 1;
            int v = UnityEngine.Random.Range(0, n);
            g.SetFlag(key, v + 1);
            return v;
        }

        static void TserDialogues()
        {
            new DB("arrigal")
                .Node("start", "Arrigal", "*A lean Vistani man with a curved sword across his knees looks up from sharpening it. He does not smile.* You're the ones from the village. Madam Eva's tent is by the water. Go in, sit down, and do not touch anything on her table. *His eyes go to Ireena, and stay a moment too long.*")
                    .Opt("You seem to know a lot about us.", "know")
                    .Opt("[INSIGHT] Why is he watching Ireena?", null).Check(Skill.Insight, 15, "watch_ok", "watch_no")
                    .Opt("Thank you.")
                .Node("know", "Arrigal", "The whole valley knows about you. The mists told it. *He goes back to his whetstone.* Whether they told it true — that's Madam Eva's business.")
                    .End()
                .Node("watch_ok", "Arrigal", "*It isn't desire in his face. It's appraisal — the look of a man counting what something might be worth to someone else.*")
                    .Enter(() => G.journal.AddLore("Arrigal, a Vistani of Tser Pool, looked at Ireena the way a trader looks at goods."))
                    .End()
                .Node("watch_no", "Arrigal", "*He tests the edge of his blade with his thumb, and you lose the moment.*")
                    .End();

            new DB("madam_eva")
                .Start(() => F("eva_read") ? "after" : "start")
                .Node("start", "Madam Eva", "*At the mouth of the purple tent, an ancient woman sits with a deck of painted cards in her lap. The air around her smells of beeswax and old silk. She does not look up.* Sit. You are late, and also exactly on time. The mists are very bad at clocks.")
                    .Opt("You were expecting us?", "expect")
                    .Opt("Who are you?", "who")
                    .Opt("We were told you could help us leave Barovia.", "leave")
                .Node("who", "Madam Eva", "An old woman with a deck of cards. That is all anyone needs me to be. *Her eyes, when she finally raises them, are the colour of a bruise healing.* I have been telling fortunes in this valley for a very long time, and the lord of that castle has never once asked for his.")
                    .Then("expect")
                .Node("leave", "Madam Eva", "*She laughs, not unkindly.* Everyone asks that. Leaving is easy — you only have to take away the reason the doors are locked. And the reason sits in his castle, drinking his wine, waiting for her. *She glances at Ireena.*")
                    .Then("expect")
                .Node("expect", "Madam Eva", "The cards told me three strangers would come up the western road with the burgomaster's girl. They told me you would bury a good man and meet a bad one. *She shuffles, and the cards make a sound like wings.* Shall I tell you what else they say?")
                    .Opt("Read the cards.", "read1")
                    .Opt("Not yet.", "later")
                .Node("later", "Madam Eva", "The cards will wait. They are more patient than I am.")
                    .End()
                .Node("read1", "Madam Eva", "*She lays the first card face down.* This card tells of a weapon of light — a blade of sunlight, lost when the sun left this land. *She turns it.*")
                    .Then("card1")
                .Node("card1", "Madam Eva", "")
                    .Dyn(() => { var c = BladeCards[Draw("eva_blade", BladeCards.Length)]; return $"<b>{c.name}.</b> {c.vision}\n\nThe blade waits in {c.place}."; })
                    .Then("read2")
                .Node("read2", "Madam Eva", "*The second card.* This one tells of knowledge — his own history, written in his own hand. What a man writes about himself is always his greatest weakness.")
                    .Then("card2")
                .Node("card2", "Madam Eva", "")
                    .Dyn(() => { var c = BookCards[Draw("eva_book", BookCards.Length)]; return $"<b>{c.name}.</b> {c.vision}\n\nHis history lies in {c.place}."; })
                    .Then("read3")
                .Node("read3", "Madam Eva", "*The third card.* This one tells of faith — a holy symbol shaped like a raven, that his kind cannot bear to look upon.")
                    .Then("card3")
                .Node("card3", "Madam Eva", "")
                    .Dyn(() => { var c = SymbolCards[Draw("eva_symbol", SymbolCards.Length)]; return $"<b>{c.name}.</b> {c.vision}\n\nThe symbol rests in {c.place}."; })
                    .Then("read4")
                .Node("read4", "Madam Eva", "*The fourth card, laid gently, like something that might bruise.* This card tells of a friend. You will not win this alone.")
                    .Then("card4")
                .Node("card4", "Madam Eva", "")
                    .Dyn(() => { var c = AllyCards[Draw("eva_ally", AllyCards.Length)]; return $"<b>{c.name}.</b> {c.vision}\n\nSeek {c.ally}."; })
                    .Then("read5")
                .Node("read5", "Madam Eva", "*She hesitates over the last card for a long time. The candle flames lean away from it.* And this one tells where the darkness will face you, when you finally go to him.")
                    .Then("card5")
                .Node("card5", "Madam Eva", "")
                    .Dyn(() => { var c = LairCards[Draw("eva_lair", LairCards.Length)]; return $"<b>{c.name}.</b> {c.vision}\n\nHe will meet you in {c.place}."; })
                    .Then("done")
                .Node("done", "Madam Eva", "*She gathers the cards, and her hands are shaking only a little.* There. Now you know more than anyone who has walked into that castle in three hundred years. It will not be enough — but it will be a beginning. Sleep by our fire tonight. In the morning, the road to Vallaki.")
                    .Enter(RecordReading)
                    .Opt("Thank you, Madam Eva.", "end")
                    .Opt("What happens if we fail?", "fail")
                .Node("fail", "Madam Eva", "Then I will lay out these same cards for the next ones, and they will ask me the same question. *She smiles, and every one of her many years is in it.* Try not to fail. I am very tired of shuffling.")
                    .Then("end")
                .Node("end", "Madam Eva", "Go on. The fire is warm and the wine is terrible. Both will do you good.")
                    .Enter(() => Game.I.StartCoroutine(ChapterEndAfterReading()))
                    .End()
                .Node("after", "Madam Eva", "The cards have spoken. Asking them twice only makes them rude.")
                    .End();
        }

        static void RecordReading()
        {
            var g = Game.I;
            g.SetFlag("eva_read");
            var b = BladeCards[Draw("eva_blade", BladeCards.Length)]; var k = BookCards[Draw("eva_book", BookCards.Length)];
            var s = SymbolCards[Draw("eva_symbol", SymbolCards.Length)]; var a = AllyCards[Draw("eva_ally", AllyCards.Length)]; var l = LairCards[Draw("eva_lair", LairCards.Length)];
            g.journal.Update("reading", "Madam Eva's Reading",
                $"At Tser Pool, the seer Madam Eva laid out five cards for us.\n\n<b>{b.name}</b> — the blade of sunlight lies in {b.place}.\n<b>{k.name}</b> — the count's history lies in {k.place}.\n<b>{s.name}</b> — the raven symbol rests in {s.place}.\n<b>{a.name}</b> — we should seek {a.ally}.\n<b>{l.name}</b> — he will face us in {l.place}.");
            g.GiveXP(150, "the reading");
        }

        static IEnumerator ChapterEndAfterReading()
        {
            while (DialogueRunner.I.Active) yield return null;
            yield return new WaitForSeconds(0.8f);
            EndChapter();
        }
    }
}
