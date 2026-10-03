using System;
using System.Collections.Generic;
using System.Linq;
using Dungine.Rules;
using Dungine.Visual;
using Dungine.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Dungine.UI
{
    /// <summary>Party creation: three fully customisable characters.</summary>
    public static class CreationScreen
    {
        static VisualElement root, left, right, tabsRow, slotsRow;
        static readonly List<CharacterSheet> sheets = new List<CharacterSheet>();
        static int cur;
        static string tab = "race";
        static readonly string[] Tabs = { "race", "class", "background", "abilities", "skills", "spells", "appearance", "name" };
        static readonly string[] TabNames = { "Origin", "Class", "Background", "Abilities", "Skills", "Features", "Appearance", "Name" };

        static CharacterSheet S => sheets[cur];
        static string contentKey;

        /// <summary>Picking a card rebuilds the tab; put the list back where the player was reading.</summary>
        static void RestoreScroll(ScrollView sv, Vector2 offset)
        {
            sv.scrollOffset = offset;
            EventCallback<GeometryChangedEvent> once = null;
            once = _ => { sv.scrollOffset = offset; sv.contentContainer.UnregisterCallback(once); };
            sv.contentContainer.RegisterCallback(once);
            sv.schedule.Execute(() => sv.scrollOffset = offset);
        }

        public static void Show(VisualElement parent)
        {
            Hide();
            if (sheets.Count != 3) { sheets.Clear(); sheets.AddRange(Premade()); }
            cur = 0; tab = "race";
            root = UIB.El("layer", parent);
            root.pickingMode = PickingMode.Ignore;   // clicks on empty space reach the 3D preview (drag to rotate, scroll to zoom)
            // top: party slots
            var top = UIB.Row(root); UIB.Pos(top, 0, 12, 0); top.style.justifyContent = Justify.Center; top.pickingMode = PickingMode.Ignore;
            var title = UIB.Lbl("CREATE YOUR PARTY", "h2", top); Theme.ApplyFont(title, true); title.style.marginRight = 30;
            slotsRow = UIB.Row(top);
            UIB.Btn("Randomise", () => { var r = new System.Random(); sheets[cur] = RandomSheet(r, cur); Changed(true); }, "btn btn-small", top).style.marginLeft = 20;
            UIB.Btn("Premade Party", () => { sheets.Clear(); sheets.AddRange(Premade()); cur = 0; Changed(true); }, "btn btn-small", top);
            left = UIB.Col(root, "panel cc-left"); UIB.Pos(left, 20, 70, null, 90);
            tabsRow = UIB.Row(left, "wrap");
            right = UIB.Col(root, "panel cc-right"); UIB.Pos(right, null, 70, 20, 90);
            var bottom = UIB.Row(root); UIB.Pos(bottom, 20, null, 20, 18); bottom.style.justifyContent = Justify.SpaceBetween; bottom.pickingMode = PickingMode.Ignore;
            UIB.Btn("◂  Main Menu", () => { Hide(); CreationScene.Clear(); Game.I.GoToMainMenu(); }, "btn", bottom);
            var hint = UIB.Lbl("Drag the character to turn them (or Q / E). Scroll to zoom.", "small dim", bottom); hint.pickingMode = PickingMode.Ignore;
            var go = UIB.Btn("Begin the Adventure  ▸", Begin, "btn btn-primary", bottom); go.name = "begin"; go.style.width = 320; go.style.height = 54;
            Changed(true);
        }

        public static void Hide() { root?.RemoveFromHierarchy(); root = null; }

        static void Changed(bool rebuildModel)
        {
            if (root == null) return;
            // slots
            slotsRow.Clear();
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                var s = sheets[i];
                string ok = Validate(s) == null ? "✓" : "…";
                UIB.Btn($"{i + 1}. {s.name}  <size=75%><color=#9a9080>{(s.Subrace != null ? s.Subrace.name : s.Race.name)} {s.Class.name}</color></size> {ok}", () => { cur = k; Changed(true); }, "tab" + (i == cur ? " tab-active" : ""), slotsRow);
            }
            // tabs
            tabsRow.Clear();
            for (int i = 0; i < Tabs.Length; i++)
            {
                var t = Tabs[i];
                UIB.Btn(TabNames[i], () => { tab = t; Changed(false); }, "tab" + (tab == t ? " tab-active" : ""), tabsRow);
            }
            var content = left.Q<ScrollView>("content");
            string key = tab + ":" + cur;
            Vector2 keep = content != null && key == contentKey ? content.scrollOffset : Vector2.zero;
            contentKey = key;
            content?.RemoveFromHierarchy();
            var sv = UIB.Scroll(left); sv.name = "content"; sv.style.marginTop = 8;
            if (keep.y > 0) RestoreScroll(sv, keep);
            switch (tab)
            {
                case "race": RaceTab(sv); break;
                case "class": ClassTab(sv); break;
                case "background": BackgroundTab(sv); break;
                case "abilities": AbilityTab(sv); break;
                case "skills": SkillTab(sv); break;
                case "spells": SpellTab(sv); break;
                case "appearance": AppearanceTab(sv); break;
                case "name": NameTab(sv); break;
            }
            Summary();
            var b = root.Q<Button>("begin");
            b?.SetEnabled(sheets.All(s => Validate(s) == null));
            if (rebuildModel) CreationScene.Show(S);
        }

        // ================================================================== tabs
        static void RaceTab(VisualElement p)
        {
            UIB.Lbl("Race", "h3", p);
            var grid = UIB.Row(p, "wrap");
            foreach (RaceId r in Enum.GetValues(typeof(RaceId)))
            {
                var rd = RulesData.Races[r];
                var b = UIB.Btn(rd.name, () =>
                {
                    if (S.race == r) return;
                    S.race = r; S.subrace = rd.subraces.Count > 0 ? rd.subraces[0] : SubraceId.None;
                    var rnd = new System.Random();
                    S.look = RaceLooks.Randomize(r, S.subrace, S.look.bodyType, rnd);
                    S.look.cloth1 = ClassColor(S.cls); S.look.cloth2 = new Color(.2f, .18f, .16f);
                    S.name = Names.Random(r, S.look.bodyType, rnd);
                    Changed(true);
                }, "btn" + (S.race == r ? " btn-selected" : ""), grid);
                b.style.width = 138;
            }
            var race = S.Race;
            var d = UIB.Lbl(race.desc, "small", p); d.style.marginTop = 8;
            if (race.subraces.Count > 0)
            {
                UIB.Lbl(race.id == RaceId.Dragonborn ? "Draconic Ancestry" : "Subrace", "h3", p);
                var g2 = UIB.Row(p, "wrap");
                foreach (var sid in race.subraces)
                {
                    var sd = RulesData.Subraces[sid];
                    var b = UIB.Btn(sd.name.Replace(" Dragonborn", ""), () =>
                    {
                        S.subrace = sid;
                        if (S.race == RaceId.Dragonborn || sid == SubraceId.Duergar || sid == SubraceId.DeepGnome || sid == SubraceId.HalfElfDrow)
                        { var pal = RaceLooks.SkinPalette(S.race, sid); S.look.skin = pal[0]; S.look.subrace = sid; }
                        S.look.subrace = sid;
                        Changed(true);
                    }, "btn btn-small" + (S.subrace == sid ? " btn-selected" : ""), g2);
                    if (race.id == RaceId.Dragonborn) { b.style.borderLeftColor = b.style.borderRightColor = b.style.borderTopColor = b.style.borderBottomColor = RaceLooks.DragonColor(sid); b.style.borderLeftWidth = 3; }
                }
                if (S.Subrace != null) UIB.Lbl(S.Subrace.desc, "small", p).style.marginTop = 6;
            }
            UIB.Lbl("Traits", "h3", p);
            foreach (var t in race.traits) UIB.Lbl("• " + t, "small", p);
            if (S.Subrace != null) foreach (var t in S.Subrace.traits) UIB.Lbl("• " + t, "small gold", p);
            UIB.Lbl($"Speed {race.speedM + (S.Subrace?.speedBonus ?? 0):0.#}m · Size {race.size}", "small dim", p);
            if (race.id == RaceId.Human || race.id == RaceId.HalfElf)
            {
                UIB.Lbl("Bonus skill (Human Versatility)", "h3", p);
                var row = UIB.Row(p, "wrap");
                foreach (Skill sk in Enum.GetValues(typeof(Skill)))
                {
                    var k = sk;
                    UIB.Btn(sk.Nice(), () => { S.extraSkills.Clear(); S.extraSkills.Add(k); Changed(false); }, "btn btn-small" + (S.extraSkills.Contains(sk) ? " btn-selected" : ""), row);
                }
            }
            else S.extraSkills.Clear();
        }

        static void ClassTab(VisualElement p)
        {
            UIB.Lbl("Class", "h3", p);
            var grid = UIB.Row(p, "wrap");
            foreach (ClassId c in Enum.GetValues(typeof(ClassId)))
            {
                var cd = RulesData.Classes[c];
                var card = UIB.Row(grid, "card" + (S.cls == c ? " card-selected" : "")); card.style.width = new Length(47, LengthUnit.Percent);
                UIB.IconCircle(ClassIcon(c), ClassColor(c) * 1.6f, 40, card).style.marginRight = 6;
                var col = UIB.Col(card); UIB.Lbl(cd.name, "gold", col); UIB.Lbl(cd.tagline, "tiny dim", col);
                card.RegisterCallback<ClickEvent>(_ => SetClass(c));
            }
            var cls = S.Class;
            UIB.Lbl(cls.desc, "small", p).style.marginTop = 8;
            UIB.Lbl($"Hit die d{cls.hitDie} · Primary {cls.primary.Long()} · Saves {string.Join(", ", cls.saves.Select(a => a.Long()))}", "small dim", p);
            string armor = string.Join(", ", new[] { cls.lightArmor ? "light" : null, cls.mediumArmor ? "medium" : null, cls.heavyArmor ? "heavy" : null, cls.shields ? "shields" : null }.Where(x => x != null));
            UIB.Lbl($"Armour: {(armor.Length > 0 ? armor : "none")} · Weapons: {(cls.martialWeapons ? "simple & martial" : cls.simpleWeapons ? "simple" : "")}{(cls.weaponFamilies.Count > 0 ? (cls.simpleWeapons ? ", " : "") + string.Join(", ", cls.weaponFamilies) : "")}", "small dim", p);
            UIB.Lbl("Level 1 features", "h3", p);
            foreach (var f in cls.features.Where(f => f.level == 1)) UIB.Lbl($"• <color=#e6c67a>{f.name}</color> — {f.desc}", "small", p);
            if (cls.subclassLevel == 1)
            {
                UIB.Lbl(cls.subclassLabel, "h3", p);
                foreach (var sc in cls.subclasses)
                {
                    var card = UIB.Col(p, "card" + (S.subclass == sc.id ? " card-selected" : ""));
                    UIB.Lbl(sc.name, "gold", card); UIB.Lbl(sc.desc, "small", card);
                    foreach (var f in sc.features.Where(f => f.level == 1)) UIB.Lbl($"• {f.name}: {f.desc}", "tiny", card);
                    var id = sc.id;
                    card.RegisterCallback<ClickEvent>(_ => { S.subclass = id; Changed(false); });
                }
            }
            else UIB.Lbl($"You choose your {cls.subclassLabel} at level {cls.subclassLevel}.", "small dim", p);
        }

        static void SetClass(ClassId c)
        {
            if (S.cls == c) return;
            var cd = RulesData.Classes[c];
            S.cls = c; S.subclass = cd.subclassLevel == 1 ? cd.subclasses[0].id : null;
            S.kit.Clear();
            S.baseAbilities = (int[])cd.recommended.Clone();
            S.plus2 = cd.primary; S.plus1 = cd.primary == Ability.CON ? Ability.DEX : Ability.CON;
            S.classSkills.Clear(); S.expertise.Clear(); S.cantrips.Clear(); S.spells.Clear();
            S.fightingStyle = c == ClassId.Fighter ? "defence" : null;
            AutoSkills(S); AutoSpells(S);
            S.look.cloth1 = ClassColor(c);
            Changed(true);
        }

        static void BackgroundTab(VisualElement p)
        {
            UIB.Lbl("Background", "h3", p);
            foreach (BackgroundId b in Enum.GetValues(typeof(BackgroundId)))
            {
                var bd = RulesData.Backgrounds[b];
                var card = UIB.Col(p, "card" + (S.background == b ? " card-selected" : ""));
                UIB.Lbl(bd.name, "gold", card);
                UIB.Lbl(bd.desc, "small", card);
                UIB.Lbl("Skills: " + string.Join(", ", bd.skills.Select(s => s.Nice())), "tiny dim", card);
                card.RegisterCallback<ClickEvent>(_ => { S.background = b; S.classSkills.RemoveAll(k => bd.skills.Contains(k)); AutoSkills(S); Changed(false); });
            }
            UIB.Lbl("Acting in keeping with your background during conversations can earn the party Inspiration, which lets you reroll a failed check.", "tiny dim", p);
        }

        static void AbilityTab(VisualElement p)
        {
            int spent = Spent(S);
            UIB.Lbl($"Point Buy — {RulesData.PointBudget - spent} points remaining", "h3", p);
            foreach (Ability a in Enum.GetValues(typeof(Ability)))
            {
                var row = UIB.El("ab-row", p);
                UIB.Lbl(a.Long(), "ab-name", row);
                var aa = a;
                var minus = UIB.Btn("−", () => { if (S.baseAbilities[(int)aa] > 8) { S.baseAbilities[(int)aa]--; Changed(false); } }, "btn ab-btn", row);
                UIB.Lbl(S.baseAbilities[(int)a].ToString(), "ab-val", row);
                var plus = UIB.Btn("+", () => { int v = S.baseAbilities[(int)aa]; if (v < 15 && Spent(S) - RulesData.PointCost[v] + RulesData.PointCost[v + 1] <= RulesData.PointBudget) { S.baseAbilities[(int)aa]++; Changed(false); } }, "btn ab-btn", row);
                int total = S.Score(a);
                UIB.Lbl($"= {total}", "ab-total", row);
                int m = Creature.ModOf(total);
                UIB.Lbl((m >= 0 ? "+" : "") + m, "ab-mod", row);
                var b2 = UIB.Btn("+2", () => { if (S.plus1 == aa) S.plus1 = S.plus2; S.plus2 = aa; Changed(false); }, "btn ab-bonus" + (S.plus2 == a ? " btn-selected" : ""), row);
                var b1 = UIB.Btn("+1", () => { if (S.plus2 == aa) S.plus2 = S.plus1; S.plus1 = aa; Changed(false); }, "btn ab-bonus" + (S.plus1 == a ? " btn-selected" : ""), row);
                if (a == S.Class.primary) UIB.Lbl("★", "gold", row).tooltip = "Primary ability";
            }
            UIB.Lbl("Scores start at 8 and cost more as they rise (max 15 before bonuses). Assign a +2 and a +1 bonus to any abilities.", "tiny dim", p).style.marginTop = 8;
            UIB.Btn("Use Recommended", () => { S.baseAbilities = (int[])S.Class.recommended.Clone(); S.plus2 = S.Class.primary; Changed(false); }, "btn btn-small", p);
        }

        static void SkillTab(VisualElement p)
        {
            var cls = S.Class;
            var bgSkills = RulesData.Backgrounds[S.background].skills;
            var fixedSkills = new HashSet<Skill>(bgSkills.Concat(S.Race.skills).Concat(S.Subrace?.skills ?? new List<Skill>()).Concat(S.extraSkills));
            UIB.Lbl($"Class skills — choose {cls.skillCount} ({S.classSkills.Count}/{cls.skillCount})", "h3", p);
            var row = UIB.Row(p, "wrap");
            foreach (var sk in cls.skillList)
            {
                var k = sk;
                bool fixedK = fixedSkills.Contains(sk);
                var b = UIB.Btn(sk.Nice() + (fixedK ? " ✓" : ""), () =>
                {
                    if (S.classSkills.Contains(k)) S.classSkills.Remove(k);
                    else if (S.classSkills.Count < cls.skillCount) S.classSkills.Add(k);
                    S.expertise.RemoveAll(e => !S.classSkills.Contains(e) && !fixedSkills.Contains(e));
                    Changed(false);
                }, "btn btn-small" + (S.classSkills.Contains(sk) ? " btn-selected" : ""), row);
                b.SetEnabled(!fixedK);
            }
            UIB.Lbl($"Already proficient from race and background: {string.Join(", ", fixedSkills.Select(s => s.Nice()))}", "tiny dim", p);
            if (S.cls == ClassId.Rogue)
            {
                UIB.Lbl($"Expertise — choose 2 ({S.expertise.Count}/2)", "h3", p);
                var r2 = UIB.Row(p, "wrap");
                foreach (var sk in S.classSkills.Concat(fixedSkills).Distinct())
                {
                    var k = sk;
                    UIB.Btn(sk.Nice(), () => { if (S.expertise.Contains(k)) S.expertise.Remove(k); else if (S.expertise.Count < 2) S.expertise.Add(k); Changed(false); }, "btn btn-small" + (S.expertise.Contains(sk) ? " btn-selected" : ""), r2);
                }
            }
        }

        static void SpellTab(VisualElement p)
        {
            var cls = S.Class;
            if (S.cls == ClassId.Fighter)
            {
                UIB.Lbl("Fighting Style", "h3", p);
                foreach (var fs in RulesData.FightingStyles)
                {
                    var card = UIB.Col(p, "card" + (S.fightingStyle == fs ? " card-selected" : ""));
                    UIB.Lbl(RulesData.StyleName(fs), "gold", card); UIB.Lbl(RulesData.StyleDesc(fs), "small", card);
                    var f = fs; card.RegisterCallback<ClickEvent>(_ => { S.fightingStyle = f; Changed(false); });
                }
            }
            int nc = cls.cantripsKnown[1];
            if (nc > 0)
            {
                UIB.Lbl($"Cantrips — choose {nc} ({S.cantrips.Count}/{nc})", "h3", p);
                SpellList(p, ActionLibrary.SpellsFor(cls.id, 0).Where(id => ActionLibrary.Get(id).spellLevel == 0), S.cantrips, nc);
            }
            int ns = SpellsAtOne(S);
            if (ns > 0)
            {
                UIB.Lbl($"1st-level spells — choose {ns} ({S.spells.Count}/{ns})", "h3", p);
                var bonus = S.Sub?.bonusSpells.TryGetValue(1, out var bs) == true ? bs : new string[0];
                if (bonus.Length > 0) UIB.Lbl("Always prepared from your subclass: " + string.Join(", ", bonus.Select(b => ActionLibrary.Get(b)?.name)), "tiny gold", p);
                SpellList(p, ActionLibrary.SpellsFor(cls.id, 1).Where(id => ActionLibrary.Get(id).spellLevel == 1 && !bonus.Contains(id)), S.spells, ns);
            }
            if (nc == 0 && ns == 0 && S.cls != ClassId.Fighter)
            {
                UIB.Lbl("Class features", "h3", p);
                foreach (var f in cls.features.Where(f => f.level == 1)) UIB.Lbl($"• <color=#e6c67a>{f.name}</color> — {f.desc}", "small", p);
                if (cls.caster == CasterType.Half) UIB.Lbl("You gain spellcasting at level 2.", "small dim", p);
            }
        }

        static void SpellList(VisualElement p, IEnumerable<string> ids, List<string> picked, int max)
        {
            var row = UIB.Row(p, "wrap");
            foreach (var id in ids)
            {
                var a = ActionLibrary.Get(id);
                var card = UIB.Row(row, "card" + (picked.Contains(id) ? " card-selected" : "")); card.style.width = new Length(47, LengthUnit.Percent);
                UIB.IconCircle(a.icon, a.color, 36, card).style.marginRight = 6;
                var col = UIB.Col(card); col.style.flexShrink = 1; UIB.Lbl(a.name, "small gold", col); UIB.Lbl(a.school.ToString(), "tiny dim", col);
                var sid = id;
                card.RegisterCallback<ClickEvent>(_ => { if (picked.Contains(sid)) picked.Remove(sid); else if (picked.Count < max) picked.Add(sid); Changed(false); });
                card.RegisterCallback<MouseEnterEvent>(_ => Tip(a.name, a.desc + (string.IsNullOrEmpty(a.dmg) ? "" : $"\n<color=#e6c67a>{a.dmg} {a.dtype}</color>"), card));
                card.RegisterCallback<MouseLeaveEvent>(_ => HideTip());
            }
        }

        static VisualElement tip;
        static void Tip(string title, string body, VisualElement anchor)
        {
            if (tip == null || tip.parent == null) { tip = UIB.El("tooltip", UIRoot.I.tipLayer); tip.pickingMode = PickingMode.Ignore; }
            tip.Clear(); tip.style.display = DisplayStyle.Flex;
            UIB.Lbl(title, "tt-title", tip); UIB.Lbl(body, "tt-body", tip);
            var wb = anchor.worldBound; tip.style.left = wb.xMax + 10; tip.style.top = wb.yMin;
        }
        static void HideTip() { if (tip != null) tip.style.display = DisplayStyle.None; }

        static void AppearanceTab(VisualElement p)
        {
            var a = S.look; var look = RaceLooks.Looks[S.race];
            UIB.Lbl("Body", "h3", p);
            var row = UIB.Row(p, "wrap");
            UIB.Btn("Body Type 1", () => { a.bodyType = 0; Changed(true); }, "btn btn-small" + (a.bodyType == 0 && !a.strong ? " btn-selected" : ""), row);
            UIB.Btn("Body Type 2", () => { a.bodyType = 1; a.strong = false; Changed(true); }, "btn btn-small" + (a.bodyType == 1 && !a.strong ? " btn-selected" : ""), row);
            UIB.Btn("Body Type 3", () => { a.bodyType = 0; a.strong = true; Changed(true); }, "btn btn-small" + (a.bodyType == 0 && a.strong ? " btn-selected" : ""), row);
            UIB.Btn("Body Type 4", () => { a.bodyType = 1; a.strong = true; Changed(true); }, "btn btn-small" + (a.bodyType == 1 && a.strong ? " btn-selected" : ""), row);
            var h = new Slider("Height", -1, 1) { value = a.heightAdj }; h.RegisterCallback<ChangeEvent<float>>(e => { a.heightAdj = e.newValue; CreationScene.Show(S); }); p.Add(h);
            var age = new Slider("Age", 0, 1) { value = a.age }; age.RegisterCallback<ChangeEvent<float>>(e => { a.age = e.newValue; CreationScene.Show(S); }); p.Add(age);
            UIB.Lbl(look.dragon ? "Scales" : "Skin", "h3", p);
            Swatches(p, RaceLooks.SkinPalette(S.race, S.subrace), a.skin, c => { a.skin = c; Changed(true); });
            if (!look.dragon)
            {
                UIB.Lbl("Hair", "h3", p);
                Swatches(p, look.hairs.Concat(new[] { new Color(.1f, .1f, .12f), new Color(.6f, .15f, .1f), new Color(.25f, .3f, .5f) }).ToArray(), a.hair, c => { a.hair = c; Changed(true); });
                Choice(p, RaceLooks.HairStyleNames, a.hairStyle, v => { a.hairStyle = v; Changed(true); });
                UIB.Lbl("Facial hair", "h3", p);
                Choice(p, RaceLooks.BeardNames, a.beardStyle, v => { a.beardStyle = v; Changed(true); });
            }
            UIB.Lbl("Face", "h3", p);
            Choice(p, RaceLooks.FaceNames, a.faceShape, v => { a.faceShape = v; Changed(true); });
            UIB.Lbl("Eyes", "h3", p);
            Swatches(p, look.eyeColors.Concat(new[] { new Color(.2f, .6f, .9f), new Color(.3f, .7f, .3f), new Color(.5f, .3f, .15f), new Color(.8f, .2f, .2f) }).ToArray(), a.eyes, c => { a.eyes = c; Changed(true); });
            if (look.horns || look.dragon)
            {
                UIB.Lbl("Horns", "h3", p);
                Choice(p, RaceLooks.HornNames, a.hornStyle, v => { a.hornStyle = v; Changed(true); });
                Swatches(p, new[] { new Color(.12f, .1f, .1f), new Color(.3f, .22f, .18f), new Color(.55f, .45f, .35f), new Color(.82f, .76f, .62f), new Color(.35f, .1f, .12f) }, a.horn, c => { a.horn = c; Changed(true); });
            }
            if (S.race == RaceId.Tiefling) { var t = new Toggle("Tail") { value = a.tail }; t.RegisterValueChangedCallback(e => { a.tail = e.newValue; CreationScene.Show(S); }); p.Add(t); }
            UIB.Lbl("Clothing colours", "h3", p);
            var cols = new[] { new Color(.45f, .1f, .1f), new Color(.15f, .25f, .45f), new Color(.2f, .35f, .2f), new Color(.35f, .25f, .45f), new Color(.5f, .4f, .2f), new Color(.2f, .2f, .22f), new Color(.6f, .58f, .52f), new Color(.1f, .08f, .08f), new Color(.5f, .25f, .1f), new Color(.2f, .4f, .45f) };
            Swatches(p, cols, a.cloth1, c => { a.cloth1 = c; Changed(true); });
            Swatches(p, cols.Select(c => c * 0.55f).Select(c => new Color(c.r, c.g, c.b, 1)).ToArray(), a.cloth2, c => { a.cloth2 = c; Changed(true); });
            UIB.Btn("Randomise appearance", () => { var r = new System.Random(); var nl = RaceLooks.Randomize(S.race, S.subrace, a.bodyType, r); nl.cloth1 = a.cloth1; nl.cloth2 = a.cloth2; S.look = nl; Changed(true); }, "btn btn-small", p).style.marginTop = 10;
        }

        static void Swatches(VisualElement p, Color[] cols, Color cur, Action<Color> set)
        {
            var row = UIB.Row(p, "wrap");
            foreach (var c in cols)
            {
                var col = c;
                var s = UIB.El("swatch" + (Approx(c, cur) ? " swatch-selected" : ""), row);
                s.style.backgroundColor = c;
                s.RegisterCallback<ClickEvent>(_ => set(col));
            }
        }

        static bool Approx(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.02f;

        static void Choice(VisualElement p, string[] names, int cur, Action<int> set)
        {
            var row = UIB.Row(p, "wrap");
            for (int i = 0; i < names.Length; i++) { int k = i; UIB.Btn(names[i], () => set(k), "btn btn-small" + (cur == i ? " btn-selected" : ""), row); }
        }

        static void NameTab(VisualElement p)
        {
            UIB.Lbl("Name", "h3", p);
            var tf = new TextField { value = S.name, maxLength = 24 };
            tf.RegisterValueChangedCallback(e => { S.name = e.newValue; Summary(); });
            tf.RegisterCallback<FocusOutEvent>(_ => Changed(false));
            p.Add(tf);
            UIB.Btn("Random name", () => { S.name = Names.Random(S.race, S.look.bodyType, new System.Random()); Changed(false); }, "btn btn-small", p);
            UIB.Lbl("Voice", "h3", p);
            Choice(p, new[] { "Low", "Warm", "Bright", "Rasp" }, S.voice, v => { S.voice = v; Changed(false); });
        }

        // ================================================================== summary & validation
        static void Summary()
        {
            right.Clear();
            var s = S; var c = Creature.FromSheet(s);
            foreach (var id in s.StartingItems) { var st = new ItemStack(id); Game.I.AutoEquip(c, st); }
            var n = UIB.Lbl(s.name, "h2", right); Theme.ApplyFont(n, true);
            UIB.Lbl($"Level 1 {(s.Subrace != null ? s.Subrace.name : s.Race.name)} {s.Class.name}" + (s.Sub != null ? $"\n{s.Sub.name}" : ""), "gold", right);
            UIB.Lbl(RulesData.Backgrounds[s.background].name, "small dim", right);
            var stats = UIB.Row(right, "wrap"); stats.style.marginTop = 6;
            foreach (Ability a in Enum.GetValues(typeof(Ability)))
            {
                var box = UIB.El("stat-box", stats); box.style.width = 50; box.style.height = 78; box.style.marginLeft = 2; box.style.marginRight = 2;
                UIB.Lbl(a.ToString(), "stat-name", box); var v = UIB.Lbl(s.Score(a).ToString(), "stat-val", box); v.style.fontSize = 22;
                int m = Creature.ModOf(s.Score(a)); UIB.Lbl((m >= 0 ? "+" : "") + m, "stat-mod", box);
            }
            UIB.Lbl($"HP {c.MaxHPTotal}   AC {c.AC}   Speed {c.Speed:0.#}m   Initiative {(c.InitiativeBonus >= 0 ? "+" : "")}{c.InitiativeBonus}", "tt-stat", right);
            UIB.Lbl("Proficient skills: " + string.Join(", ", c.skillProf.OrderBy(k => k.ToString()).Select(k => k.Nice() + (c.expertise.Contains(k) ? "*" : ""))), "small", right).style.marginTop = 6;
            if (c.knownSpells.Count > 0) UIB.Lbl("Spells: " + string.Join(", ", c.knownSpells.Select(i => ActionLibrary.Get(i)?.name)), "small", right).style.marginTop = 4;
            UIB.Lbl("Equipment: " + string.Join(", ", s.StartingItems.Select(i => Items.Get(i)?.name)), "small dim", right).style.marginTop = 4;
            var v2 = Validate(s);
            var st2 = UIB.Lbl(v2 == null ? "✓ Ready" : "Still needed: " + v2, "small " + (v2 == null ? "good" : "bad"), right); st2.style.marginTop = 10;
            var nav = UIB.Row(right); nav.style.marginTop = 10;
            if (cur < 2) UIB.Btn("Next Character ▸", () => { cur++; tab = "race"; Changed(true); }, "btn", nav);
            if (cur > 0) UIB.Btn("◂ Previous", () => { cur--; Changed(true); }, "btn", nav);
        }

        static string Validate(CharacterSheet s)
        {
            if (string.IsNullOrWhiteSpace(s.name)) return "a name";
            if (s.Race.subraces.Count > 0 && (s.subrace == SubraceId.None || !s.Race.subraces.Contains(s.subrace))) return "a subrace";
            if (s.Class.subclassLevel == 1 && string.IsNullOrEmpty(s.subclass)) return s.Class.subclassLabel;
            if (Spent(s) > RulesData.PointBudget) return "fewer ability points";
            if (s.classSkills.Count < s.Class.skillCount) return $"{s.Class.skillCount - s.classSkills.Count} more skill(s)";
            if (s.cls == ClassId.Rogue && s.expertise.Count < 2) return "expertise";
            if (s.cls == ClassId.Fighter && string.IsNullOrEmpty(s.fightingStyle)) return "a fighting style";
            if (s.cantrips.Count < s.Class.cantripsKnown[1]) return "cantrips";
            if (s.spells.Count < SpellsAtOne(s)) return "spells";
            if ((s.race == RaceId.Human || s.race == RaceId.HalfElf) && s.extraSkills.Count == 0) return "a bonus skill (Origin tab)";
            return null;
        }

        static int Spent(CharacterSheet s) => s.baseAbilities.Sum(v => RulesData.PointCost[Mathf.Clamp(v, 0, 15)]);

        static int SpellsAtOne(CharacterSheet s)
        {
            var cd = s.Class;
            if (cd.caster == CasterType.None || cd.caster == CasterType.Half) return 0;
            if (cd.prepared) return Mathf.Max(1, Creature.ModOf(s.Score(cd.spellAbility)) + 1) + (cd.id == ClassId.Wizard ? 1 : 0);
            return cd.spellsKnown[1];
        }

        static void Begin()
        {
            if (!sheets.All(s => Validate(s) == null)) return;
            Hide();
            var list = sheets.ToList();
            sheets.Clear();
            Game.I.NewGame(list);
        }

        // ================================================================== presets
        static void AutoSkills(CharacterSheet s)
        {
            var cd = s.Class;
            var bg = RulesData.Backgrounds[s.background].skills;
            foreach (var k in cd.skillList) { if (s.classSkills.Count >= cd.skillCount) break; if (!bg.Contains(k) && !s.classSkills.Contains(k) && !s.Race.skills.Contains(k)) s.classSkills.Add(k); }
            if (s.cls == ClassId.Rogue && s.expertise.Count < 2) s.expertise.AddRange(s.classSkills.Take(2 - s.expertise.Count));
        }

        static void AutoSpells(CharacterSheet s)
        {
            var cd = s.Class;
            var cantrips = ActionLibrary.SpellsFor(cd.id, 0).Where(i => ActionLibrary.Get(i).spellLevel == 0).ToList();
            string[] prefC = { "fire_bolt", "eldritch_blast", "sacred_flame", "vicious_mockery", "produce_flame", "ray_of_frost", "toll_the_dead", "guidance", "shocking_grasp", "thorn_whip", "chill_touch" };
            foreach (var p in prefC.Where(cantrips.Contains)) if (s.cantrips.Count < cd.cantripsKnown[1] && !s.cantrips.Contains(p)) s.cantrips.Add(p);
            foreach (var p in cantrips) if (s.cantrips.Count < cd.cantripsKnown[1] && !s.cantrips.Contains(p)) s.cantrips.Add(p);
            var spells = ActionLibrary.SpellsFor(cd.id, 1).Where(i => ActionLibrary.Get(i).spellLevel == 1).ToList();
            string[] prefS = { "magic_missile", "healing_word", "cure_wounds", "shield", "guiding_bolt", "bless", "hex", "armor_of_agathys", "dissonant_whispers", "sleep", "burning_hands", "thunderwave", "entangle", "faerie_fire", "chromatic_orb", "mage_armor" };
            int n = SpellsAtOne(s);
            foreach (var p in prefS.Where(spells.Contains)) if (s.spells.Count < n && !s.spells.Contains(p)) s.spells.Add(p);
            foreach (var p in spells) if (s.spells.Count < n && !s.spells.Contains(p)) s.spells.Add(p);
        }

        static CharacterSheet Make(string name, RaceId r, SubraceId sr, ClassId c, string sub, BackgroundId bg, int body, int seed, Action<Appearance> tweak = null, params string[] kit)
        {
            var cd = RulesData.Classes[c];
            var s = new CharacterSheet { name = name, race = r, subrace = sr, cls = c, subclass = sub, background = bg, baseAbilities = (int[])cd.recommended.Clone(), plus2 = cd.primary, plus1 = cd.primary == Ability.CON ? Ability.DEX : Ability.CON };
            s.look = RaceLooks.Randomize(r, sr, body, new System.Random(seed));
            s.look.cloth1 = ClassColor(c); s.look.cloth2 = new Color(.18f, .16f, .14f);
            tweak?.Invoke(s.look);
            if (c == ClassId.Fighter) s.fightingStyle = "defence";
            if (kit != null && kit.Length > 0) s.kit.AddRange(kit);
            if (r == RaceId.Human || r == RaceId.HalfElf) s.extraSkills.Add(Skill.Perception);
            AutoSkills(s); AutoSpells(s);
            return s;
        }

        public static List<CharacterSheet> Premade() => new List<CharacterSheet>
        {
            Make("Arkus", RaceId.Elf, SubraceId.HighElf, ClassId.Fighter, null, BackgroundId.Soldier, 0, 11, a =>
            {
                a.hairStyle = 10; a.hair = new Color(.06f, .06f, .07f); a.beardStyle = 0;
                a.eyes = new Color(.35f, .95f, .45f); a.glowEyes = new Color(.35f, 1f, .45f, .35f);
                a.skin = new Color(.8f, .66f, .56f);
                a.cloth1 = new Color(.2f, .24f, .22f); a.cloth2 = new Color(.24f, .17f, .12f);
                a.cape = true; a.capeColor = new Color(.11f, .1f, .1f); a.capeLining = new Color(.2f, .24f, .21f);
                a.armorLook = (int)ArmorVisual.Plate; a.metalTint = new Color(.4f, .5f, .45f);
            }, "glaive", "chain_mail", "potion_healing"),
            Make("Chairn", RaceId.Dragonborn, SubraceId.Blue, ClassId.Sorcerer, "draconic", BackgroundId.HauntedOne, 0, 22, a => { a.skin = RaceLooks.DragonColor(SubraceId.Blue); a.cloth1 = new Color(.3f, .22f, .4f); a.hornStyle = 1; }),
            Make("Dulandir", RaceId.Firbolg, SubraceId.None, ClassId.Wizard, null, BackgroundId.Sage, 0, 44, a => { a.skin = new Color(.56f, .62f, .7f); a.hairStyle = 2; a.hair = new Color(.93f, .93f, .96f); a.beardStyle = 4; a.eyes = new Color(.42f, .63f, .78f); a.cloth1 = new Color(.16f, .2f, .38f); a.cloth2 = new Color(.3f, .26f, .2f); a.age = 0.45f; }),
        };

        static CharacterSheet RandomSheet(System.Random r, int idx)
        {
            var races = (RaceId[])Enum.GetValues(typeof(RaceId));
            var classes = (ClassId[])Enum.GetValues(typeof(ClassId));
            var race = races[r.Next(races.Length)];
            var rd = RulesData.Races[race];
            var sr = rd.subraces.Count > 0 ? rd.subraces[r.Next(rd.subraces.Count)] : SubraceId.None;
            var cls = classes[r.Next(classes.Length)];
            var cd = RulesData.Classes[cls];
            var bgs = (BackgroundId[])Enum.GetValues(typeof(BackgroundId));
            int body = r.Next(2);
            return Make(Names.Random(race, body, r), race, sr, cls, cd.subclassLevel == 1 ? cd.subclasses[r.Next(cd.subclasses.Count)].id : null, bgs[r.Next(bgs.Length)], body, r.Next());
        }

        public static Color ClassColor(ClassId c)
        {
            switch (c)
            {
                case ClassId.Barbarian: return new Color(.45f, .22f, .12f);
                case ClassId.Bard: return new Color(.5f, .15f, .3f);
                case ClassId.Cleric: return new Color(.6f, .55f, .42f);
                case ClassId.Druid: return new Color(.22f, .35f, .18f);
                case ClassId.Fighter: return new Color(.4f, .12f, .1f);
                case ClassId.Monk: return new Color(.55f, .38f, .15f);
                case ClassId.Paladin: return new Color(.2f, .28f, .5f);
                case ClassId.Ranger: return new Color(.2f, .3f, .2f);
                case ClassId.Rogue: return new Color(.15f, .14f, .16f);
                case ClassId.Sorcerer: return new Color(.45f, .12f, .2f);
                case ClassId.Warlock: return new Color(.25f, .12f, .35f);
                default: return new Color(.15f, .2f, .45f);
            }
        }

        static string ClassIcon(ClassId c)
        {
            switch (c)
            {
                case ClassId.Barbarian: return "rage";
                case ClassId.Bard: return "music";
                case ClassId.Cleric: return "holy";
                case ClassId.Druid: return "leaf";
                case ClassId.Fighter: return "sword";
                case ClassId.Monk: return "fist";
                case ClassId.Paladin: return "smite";
                case ClassId.Ranger: return "bow";
                case ClassId.Rogue: return "dagger";
                case ClassId.Sorcerer: return "fire";
                case ClassId.Warlock: return "eye";
                default: return "book";
            }
        }
    }

    /// <summary>Syllable-based name generator per race.</summary>
    public static class Names
    {
        static readonly Dictionary<RaceId, (string[] m, string[] f)> sets = new Dictionary<RaceId, (string[], string[])>
        {
            [RaceId.Human] = (new[] { "Aldric", "Bram", "Corvin", "Darius", "Edric", "Falk", "Garret", "Holm", "Ivo", "Jurek", "Kasimir", "Leoric", "Marek", "Oswin", "Radomir", "Stefan", "Tobiah", "Valen" }, new[] { "Anika", "Brisa", "Celia", "Dagna", "Elsbeth", "Freya", "Greta", "Helka", "Irina", "Jolan", "Katya", "Lenka", "Mirela", "Nadia", "Oriana", "Petra", "Sabine", "Vesna" }),
            [RaceId.Elf] = (new[] { "Aelar", "Arkus", "Beiro", "Carric", "Erevan", "Galinndan", "Ivellios", "Laucian", "Peren", "Quarion", "Riardon", "Thamior", "Varis" }, new[] { "Adrie", "Birel", "Caelynn", "Enna", "Ielenia", "Keyleth", "Lia", "Meriele", "Naivara", "Quelenna", "Sariel", "Thia", "Valanthe" }),
            [RaceId.Drow] = (new[] { "Ilphas", "Jarlaxin", "Kelnozz", "Nalfein", "Ryld", "Tsabrak", "Vorn", "Zaknafein" }, new[] { "Akordia", "Chalithra", "Ilvara", "Minolin", "Nedylene", "Shyntlara", "Viconia", "Zesstra" }),
            [RaceId.HalfElf] = (new[] { "Aeren", "Dalton", "Evander", "Gideon", "Lorian", "Roland", "Tanis", "Veylan" }, new[] { "Arwen", "Cerys", "Elowen", "Isolde", "Maelis", "Sylvie", "Talia", "Wren" }),
            [RaceId.HalfOrc] = (new[] { "Dench", "Feng", "Gell", "Henk", "Holg", "Krusk", "Mhurren", "Ront", "Shump", "Thokk" }, new[] { "Baggi", "Emen", "Engong", "Kansif", "Myev", "Neega", "Ovak", "Sutha", "Vola", "Yevelda" }),
            [RaceId.Halfling] = (new[] { "Alton", "Cade", "Eldon", "Finnan", "Garret", "Lindal", "Milo", "Osborn", "Perrin", "Roscoe", "Wellby" }, new[] { "Andry", "Bree", "Callie", "Cora", "Euphemia", "Kithri", "Lavinia", "Merla", "Nedda", "Seraphina", "Verna" }),
            [RaceId.Dwarf] = (new[] { "Adrik", "Baern", "Brottor", "Dain", "Eberk", "Harbek", "Kildrak", "Morgran", "Orsik", "Rurik", "Thorin", "Vondal" }, new[] { "Amber", "Bardryn", "Dagnal", "Eldeth", "Gunnloda", "Hlin", "Kathra", "Mardred", "Riswynn", "Torbera", "Vistra" }),
            [RaceId.Gnome] = (new[] { "Alston", "Boddynock", "Brocc", "Dimble", "Fonkin", "Gimble", "Glim", "Orryn", "Warryn", "Zook" }, new[] { "Bimpnottin", "Carlin", "Ellyjobell", "Lilli", "Loopmottin", "Nissa", "Orla", "Roywyn", "Tana", "Zanna" }),
            [RaceId.Tiefling] = (new[] { "Akmenos", "Barakas", "Damakos", "Ekemon", "Kairon", "Leucis", "Mordai", "Skamos", "Zevrin" }, new[] { "Akta", "Bryseis", "Criella", "Kallista", "Lerissa", "Makaria", "Nemeia", "Orianna", "Rieta" }),
            [RaceId.Githyanki] = (new[] { "Dak'thar", "Gith'var", "Ka'rath", "Vlaakith'ar", "Zerth'kel", "Mor'akh" }, new[] { "Lae'zel", "Kith'rak", "Sirr'ith", "Zhal'ka", "Vel'shar", "Aa'ryn" }),
            [RaceId.Dragonborn] = (new[] { "Arjhan", "Balasar", "Chairn", "Donaar", "Ghesh", "Kriv", "Medrash", "Nadarr", "Rhogar", "Torinn" }, new[] { "Akra", "Biri", "Daar", "Harann", "Kava", "Korinn", "Mishann", "Sora", "Thava", "Uadjit" }),
            [RaceId.Firbolg] = (new[] { "Dulandir", "Orren", "Taevas", "Hullan", "Merrowin", "Brannoch", "Eskil", "Tobrin" }, new[] { "Aelwen", "Brisa", "Ennet", "Loravel", "Maedra", "Quilla", "Tamsin", "Wenna" }),
        };

        public static string Random(RaceId r, int body, System.Random rnd)
        {
            var s = sets[r];
            var arr = body == 1 ? s.f : s.m;
            return arr[rnd.Next(arr.Length)];
        }
    }

    /// <summary>The candlelit pedestal where characters are previewed during creation.</summary>
    public static class CreationScene
    {
        static GameObject root; static HumanoidRig model; static float yaw = 18f, zoom = 1.12f;
        static bool dragging;
        static CharacterSheet pending; static float pendingT;
        static readonly Vector3 Center = new Vector3(0, 500, 0);

        public static void Clear() { if (root) UnityEngine.Object.Destroy(root); root = null; model = null; }

        public static void Build()
        {
            Clear();
            root = new GameObject("CreationScene");
            root.AddComponent<CreationSceneDriver>();
            // neutral light: creation is where colours are chosen, so they must read true
            var atm = Atmosphere.Interior; atm.exposure = 0.55f; atm.fogDensity = 0.01f; atm.sunIntensity = 0f;
            atm.ambientSky = new Color(.42f, .42f, .46f); atm.ambientEquator = new Color(.3f, .3f, .32f); atm.ambientGround = new Color(.12f, .12f, .12f);
            atm.temperature = 0f; atm.colorFilter = Color.white; atm.saturation = 0f; atm.contrast = 8f;
            Atmosphere.Ensure().Apply(atm);
            var t = root.transform;
            // stone dais & floor
            var mb = new MeshBuilder(2) { uvScale = 0.5f };
            mb.AddCylinder(0, Center + new Vector3(0, -0.4f, 0), 2.2f, 2.0f, 0.4f, 32);
            mb.AddCylinder(0, Center + new Vector3(0, -0.8f, 0), 2.8f, 2.8f, 0.4f, 32);
            mb.AddCylinder(1, Center + new Vector3(0, -1.2f, 0), 30f, 30f, 0.4f, 32);
            Kit.FromBuilder(mb, new[] { Pal.Ashlar, Pal.Cobble }, t, "Dais", Kit.ColliderKind.Mesh);
            // pillars and candelabras in a ring
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2;
                Vector3 p = Center + new Vector3(Mathf.Cos(a) * 9.5f, -0.8f, Mathf.Sin(a) * 9.5f);
                var pm = new MeshBuilder(1) { uvScale = 0.5f };
                pm.AddCylinder(0, Vector3.zero, 0.5f, 0.45f, 7f, 12);
                var pg = Kit.FromBuilder(pm, new[] { Pal.Stone }, t, "Pillar"); pg.transform.position = p;
                if (i % 2 == 0)
                {
                    Vector3 cp = Center + new Vector3(Mathf.Cos(a + 0.35f) * 3.4f, -0.8f, Mathf.Sin(a + 0.35f) * 3.4f);
                    var cg = Kit.Place("candelabra_floor", () => Props.Candelabra(true), t, Vector3.zero); cg.transform.position = cp;
                    for (int k = -1; k <= 1; k++) Kit.Flame(cg.transform, new Vector3(k * 0.18f, 1.64f, 0), 0.05f);
                    Kit.PointLight(t, cp + Vector3.up * 1.9f, new Color(1f, .78f, .55f), 1.6f, 7f, false);
                }
            }
            Kit.SpotLight(t, Center + new Vector3(1.5f, 6f, 3.5f), new Vector3(58, 200, 0), new Color(1f, .98f, .95f), 14f, 14f, 40f, true);
            Kit.PointLight(t, Center + new Vector3(-2.2f, 2.2f, 2.6f), new Color(.85f, .9f, 1f), 2.2f, 7f, false, false);
            Kit.PointLight(t, Center + new Vector3(-2.5f, 2.5f, -2f), new Color(.5f, .6f, 1f), 3f, 8f, false, false);
            Kit.Motes(t, Center + new Vector3(0, 2, 0), new Vector3(8, 4, 8), new Color(1f, .8f, .5f, .6f), 60, 0.04f, 0.05f);
            Kit.FogBank(t, Center + new Vector3(0, -0.6f, 0), new Vector3(16, 0.5f, 16), new Color(.4f, .38f, .45f, .25f), 40, 4f, 0.08f);
        }

        public static void Show(CharacterSheet s)
        {
            if (root == null) Build();
            pending = s; pendingT = 0.08f;
        }

        public static void Tick()
        {
            if (root == null) return;
            if (pending != null)
            {
                pendingT -= Time.deltaTime;
                if (pendingT <= 0)
                {
                    var s = pending; pending = null;
                    if (model) UnityEngine.Object.Destroy(model.gameObject);
                    var c = Creature.FromSheet(s);
                    foreach (var id in s.StartingItems) Game.I.AutoEquip(c, new ItemStack(id));
                    model = HumanoidBuilder.Build(s.look, ActorFactory.GearFor(c), "Preview", 512);
                    model.transform.SetParent(root.transform, false);
                    model.transform.position = Center;
                    var an = model.gameObject.AddComponent<HumanoidAnimator>(); an.Init(model, MotionStyle.Normal);
                }
            }
            var m = Mouse.current;
            if (m != null)
            {
                if ((m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame) && !UIRoot.PointerOverUI) dragging = true;
                if (!m.leftButton.isPressed && !m.rightButton.isPressed) dragging = false;
                if (dragging) yaw -= m.delta.ReadValue().x * 0.4f;
                float sc = m.scroll.ReadValue().y;
                if (Mathf.Abs(sc) > 0.01f && !UIRoot.PointerOverUI) zoom = Mathf.Clamp(zoom * (sc > 0 ? 0.9f : 1.1f), 0.35f, 1.4f);
            }
            var kb = Keyboard.current;
            if (kb != null && !(UIRoot.I != null && UIRoot.I.TextFieldFocused))
            {
                if (kb.qKey.isPressed) yaw += 120f * Time.unscaledDeltaTime;
                if (kb.eKey.isPressed) yaw -= 120f * Time.unscaledDeltaTime;
            }
            if (model) model.transform.rotation = Quaternion.Euler(0, yaw, 0);
            float h = model ? model.height : 1.8f;
            float focusY = Mathf.Lerp(h * 0.93f, h * 0.55f, Mathf.InverseLerp(0.35f, 1.2f, zoom));
            float dist = Mathf.Lerp(0.9f, 4.6f, Mathf.InverseLerp(0.35f, 1.4f, zoom)) * Mathf.Lerp(0.75f, 1f, h / 1.8f) * Mathf.Max(1f, h / 1.95f);
            Vector3 target = Center + new Vector3(0, focusY, 0);
            Vector3 pos = target + new Vector3(0.35f, 0.15f, 1f).normalized * dist;
            CameraRig.I.SetCinematic(pos, target + Vector3.right * 0.25f * dist * 0.2f, 34f);
        }
    }

    public class CreationSceneDriver : MonoBehaviour { void Update() => CreationScene.Tick(); }
}
