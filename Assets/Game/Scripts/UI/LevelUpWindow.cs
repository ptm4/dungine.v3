using System;
using System.Collections.Generic;
using System.Linq;
using Dungine.Rules;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dungine.UI
{
    /// <summary>Level-up: subclass, fighting style, spells, expertise, ability improvements.</summary>
    public class LevelUpWindow : Window
    {
        public override string Id => "levelup";
        public override string Title => "Level Up";
        public override float Width => 1200;
        public override float Height => 820;
        Creature c;
        string subclass, style, asiMode = "+2";
        Ability asiA = Ability.STR, asiB = Ability.DEX;
        readonly List<string> newCantrips = new List<string>(), newSpells = new List<string>();
        readonly List<Skill> newExpertise = new List<Skill>();

        public void OpenFor(Creature cr) { c = cr; subclass = null; style = null; asiMode = "+2"; newCantrips.Clear(); newSpells.Clear(); newExpertise.Clear(); asiA = cr.sheet.Class.primary; asiB = Ability.CON; Open(); }

        public override void Open() { if (c == null) c = Game.I.SelectedC; if (c == null || !Game.I.CanLevelUp(c)) { Toast.Show("Not enough experience to level up yet.", Theme.TextDim); return; } base.Open(); }

        int NewLevel => c.sheet.level + 1;

        public override void Refresh()
        {
            body.Clear();
            var s = c.sheet; var cd = s.Class; int L = NewLevel;
            SetTitle($"{c.name}: Level {L} {cd.name}");
            var sv = UIB.Scroll(body);
            int con = c.Mod(Ability.CON);
            int hpGain = cd.hitDie / 2 + 1 + con + (s.Subrace != null && s.Subrace.extraHP ? 1 : 0) + (s.subclass == "draconic" ? 1 : 0);
            UIB.Lbl($"Hit points +{hpGain}  ·  Proficiency bonus +{RulesData.ProfBonus(L)}", "h3", sv);
            // features gained
            var feats = cd.features.Where(f => f.level == L).ToList();
            var sub = s.Sub;
            if (sub != null) feats.AddRange(sub.features.Where(f => f.level == L));
            foreach (var f in feats) { UIB.Lbl("✦ " + f.name, "gold", sv); UIB.Lbl(f.desc, "small", sv); }
            // subclass
            if (string.IsNullOrEmpty(s.subclass) && L >= cd.subclassLevel)
            {
                UIB.Lbl($"Choose your {cd.subclassLabel}", "h3", sv);
                var row = UIB.Row(sv, "wrap");
                foreach (var sc in cd.subclasses)
                {
                    var card = UIB.Col(row, "card" + (subclass == sc.id ? " card-selected" : "")); card.style.width = 350;
                    UIB.Lbl(sc.name, "gold", card); UIB.Lbl(sc.desc, "small", card);
                    foreach (var f in sc.features.Where(f => f.level <= L)) UIB.Lbl($"• {f.name}: {f.desc}", "tiny", card);
                    var id = sc.id;
                    card.RegisterCallback<ClickEvent>(_ => { subclass = id; Refresh(); });
                }
            }
            // fighting style
            if (string.IsNullOrEmpty(s.fightingStyle) && L == 2 && (s.cls == ClassId.Paladin || s.cls == ClassId.Ranger))
            {
                UIB.Lbl("Choose a Fighting Style", "h3", sv);
                var row = UIB.Row(sv, "wrap");
                foreach (var fs in RulesData.FightingStyles.Where(f => s.cls == ClassId.Ranger || f != "archery"))
                {
                    var card = UIB.Col(row, "card" + (style == fs ? " card-selected" : "")); card.style.width = 260;
                    UIB.Lbl(RulesData.StyleName(fs), "gold", card); UIB.Lbl(RulesData.StyleDesc(fs), "tiny", card);
                    var f2 = fs; card.RegisterCallback<ClickEvent>(_ => { style = f2; Refresh(); });
                }
            }
            // expertise (bard 3)
            if (s.cls == ClassId.Bard && L == 3)
            {
                UIB.Lbl($"Expertise: choose two proficient skills ({newExpertise.Count}/2)", "h3", sv);
                var row = UIB.Row(sv, "wrap");
                foreach (var sk in c.skillProf.Where(k => !c.expertise.Contains(k)))
                {
                    var k = sk; var b = UIB.Btn(sk.Nice(), () => { if (newExpertise.Contains(k)) newExpertise.Remove(k); else if (newExpertise.Count < 2) newExpertise.Add(k); Refresh(); }, "btn btn-small" + (newExpertise.Contains(sk) ? " btn-selected" : ""), row);
                }
            }
            // spells
            int maxLvl = RulesData.MaxSpellLevel(cd, L);
            if (cd.caster != CasterType.None && maxLvl > 0 || cd.cantripsKnown[Mathf.Min(L, 5)] > cd.cantripsKnown[Mathf.Min(L - 1, 5)])
            {
                int cantripGain = cd.cantripsKnown[Mathf.Min(L, 5)] - cd.cantripsKnown[Mathf.Min(L - 1, 5)];
                int spellGain = SpellGain(L);
                var list = ActionLibrary.SpellsFor(cd.id, maxLvl);
                if (cantripGain > 0)
                {
                    UIB.Lbl($"Learn {cantripGain} new cantrip{(cantripGain > 1 ? "s" : "")} ({newCantrips.Count}/{cantripGain})", "h3", sv);
                    SpellPicker(sv, list.Where(id => ActionLibrary.Get(id).spellLevel == 0 && !s.cantrips.Contains(id)), newCantrips, cantripGain);
                }
                if (spellGain > 0)
                {
                    UIB.Lbl($"Learn {spellGain} new spell{(spellGain > 1 ? "s" : "")} ({newSpells.Count}/{spellGain}) — up to level {maxLvl}", "h3", sv);
                    SpellPicker(sv, list.Where(id => ActionLibrary.Get(id).spellLevel > 0 && !s.spells.Contains(id) && !c.knownSpells.Contains(id)), newSpells, spellGain);
                }
            }
            // ASI
            if (L == 4)
            {
                UIB.Lbl("Ability Score Improvement", "h3", sv);
                var row = UIB.Row(sv, "wrap");
                foreach (var m in new[] { "+2", "+1/+1", "alert", "tough" })
                {
                    string lbl = m == "+2" ? "+2 to one ability" : m == "+1/+1" ? "+1 to two abilities" : m == "alert" ? "Feat: Alert (+5 initiative)" : "Feat: Tough (+2 HP per level)";
                    var mm = m; UIB.Btn(lbl, () => { asiMode = mm; Refresh(); }, "btn" + (asiMode == m ? " btn-selected" : ""), row);
                }
                if (asiMode == "+2" || asiMode == "+1/+1")
                {
                    var r2 = UIB.Row(sv, "wrap");
                    UIB.Lbl(asiMode == "+2" ? "Ability: " : "First: ", "small", r2);
                    foreach (Ability a in Enum.GetValues(typeof(Ability))) { var aa = a; UIB.Btn($"{a} {c.Score(a)}", () => { asiA = aa; Refresh(); }, "btn btn-small" + (asiA == a ? " btn-selected" : ""), r2).SetEnabled(c.Score(a) < (asiMode == "+2" ? 19 : 20)); }
                    if (asiMode == "+1/+1")
                    {
                        var r3 = UIB.Row(sv, "wrap"); UIB.Lbl("Second: ", "small", r3);
                        foreach (Ability a in Enum.GetValues(typeof(Ability))) { var aa = a; UIB.Btn($"{a} {c.Score(a)}", () => { asiB = aa; Refresh(); }, "btn btn-small" + (asiB == a ? " btn-selected" : ""), r3).SetEnabled(a != asiA && c.Score(a) < 20); }
                    }
                }
            }
            var foot = UIB.Row(body, "space-between"); foot.style.marginTop = 10;
            string missing = Missing();
            UIB.Lbl(missing ?? "<color=#70e090>Ready.</color>", "small" + (missing != null ? " bad" : ""), foot);
            var confirm = UIB.Btn("Confirm Level Up", Apply, "btn btn-primary", foot);
            confirm.SetEnabled(missing == null);
        }

        int SpellGain(int L)
        {
            var cd = c.sheet.Class;
            if (cd.caster == CasterType.None) return 0;
            if (cd.prepared)
            {
                if (cd.caster == CasterType.Half && L < 2) return 0;
                int prep = Mathf.Max(1, c.Mod(cd.spellAbility) + (cd.caster == CasterType.Half ? L / 2 : L));
                return Mathf.Max(0, prep - c.sheet.spells.Count) + (cd.id == ClassId.Wizard ? 1 : 0);
            }
            return Mathf.Max(0, cd.spellsKnown[Mathf.Min(L, 5)] - cd.spellsKnown[Mathf.Min(L - 1, 5)]);
        }

        void SpellPicker(VisualElement p, IEnumerable<string> ids, List<string> picked, int max)
        {
            var row = UIB.Row(p, "wrap");
            foreach (var id in ids)
            {
                var a = ActionLibrary.Get(id); if (a == null) continue;
                var card = UIB.Row(row, "card" + (picked.Contains(id) ? " card-selected" : "")); card.style.width = 272;
                UIB.IconCircle(a.icon, a.color, 40, card).style.marginRight = 6;
                var col = UIB.Col(card); col.style.flexShrink = 1;
                UIB.Lbl(a.name, "small gold", col); UIB.Lbl(a.LevelLabel(), "tiny dim", col);
                var sid = id;
                card.RegisterCallback<ClickEvent>(_ => { if (picked.Contains(sid)) picked.Remove(sid); else if (picked.Count < max) picked.Add(sid); Refresh(); });
                card.tooltip = a.desc;
            }
        }

        string Missing()
        {
            var s = c.sheet; var cd = s.Class; int L = NewLevel;
            if (string.IsNullOrEmpty(s.subclass) && L >= cd.subclassLevel && subclass == null) return $"Choose a {cd.subclassLabel}.";
            if (string.IsNullOrEmpty(s.fightingStyle) && L == 2 && (s.cls == ClassId.Paladin || s.cls == ClassId.Ranger) && style == null) return "Choose a fighting style.";
            if (s.cls == ClassId.Bard && L == 3 && newExpertise.Count < 2) return "Choose two expertise skills.";
            int cg = cd.cantripsKnown[Mathf.Min(L, 5)] - cd.cantripsKnown[Mathf.Min(L - 1, 5)];
            if (newCantrips.Count < cg && ActionLibrary.SpellsFor(cd.id, 0).Count(id => !s.cantrips.Contains(id)) > newCantrips.Count) return "Choose your new cantrips.";
            int sg = SpellGain(L);
            int avail = ActionLibrary.SpellsFor(cd.id, RulesData.MaxSpellLevel(cd, L)).Count(id => ActionLibrary.Get(id).spellLevel > 0 && !s.spells.Contains(id) && !c.knownSpells.Contains(id));
            if (newSpells.Count < Mathf.Min(sg, avail)) return "Choose your new spells.";
            return null;
        }

        void Apply()
        {
            var s = c.sheet; int L = NewLevel;
            s.level = L;
            if (subclass != null) s.subclass = subclass;
            if (style != null) s.fightingStyle = style;
            s.cantrips.AddRange(newCantrips);
            s.spells.AddRange(newSpells);
            s.expertise.AddRange(newExpertise);
            if (L == 4)
            {
                if (asiMode == "+2") { s.asi.Add(asiA); s.asi.Add(asiA); }
                else if (asiMode == "+1/+1") { s.asi.Add(asiA); s.asi.Add(asiB); }
                else s.feats.Add(asiMode);
            }
            int oldMax = c.MaxHPTotal;
            c.Rebuild(false);
            c.hp = Mathf.Min(c.MaxHPTotal, c.hp + (c.MaxHPTotal - oldMax));
            Audio.Sfx.Play("levelup");
            if (c.actor) Visual.FX.Burst(c.actor.Chest, FxKind.Holy, 1.8f);
            Toast.Show($"{c.name} reached level {L}!", Theme.Gold, 3.5f);
            Game.I.NotifyPartyChanged();
            Close();
            if (Game.I.CanLevelUp(c)) OpenFor(c);
        }
    }
}
