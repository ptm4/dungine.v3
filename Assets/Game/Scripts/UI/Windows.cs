using System;
using System.Collections.Generic;
using System.Linq;
using Dungine.Combat;
using Dungine.Rules;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dungine.UI
{
    /// <summary>Base class for centred in-game windows.</summary>
    public abstract class Window
    {
        public abstract string Id { get; }
        public virtual string Title => Id;
        public virtual float Width => 900;
        public virtual float Height => 700;
        public bool blocksWorld = true;
        protected VisualElement win, body;
        Label titleLbl;
        public bool IsOpen => win != null && win.style.display == DisplayStyle.Flex;

        public void Build(VisualElement parent)
        {
            win = UIB.El("window", parent);
            win.style.width = Width; win.style.height = Height;
            win.style.left = new Length(50, LengthUnit.Percent); win.style.top = new Length(50, LengthUnit.Percent);
            win.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
            var head = UIB.El("window-header", win);
            titleLbl = UIB.Lbl(Title, "window-title", head); Theme.ApplyFont(titleLbl, true);
            UIB.Btn("✕", Close, "btn close-btn", head);
            body = UIB.El("grow", win);
            body.style.flexGrow = 1;
            win.style.display = DisplayStyle.None;
        }

        protected void SetTitle(string t) => titleLbl.text = t;

        public virtual void Open()
        {
            win.style.display = DisplayStyle.Flex;
            win.BringToFront();
            Refresh();
            Audio.Sfx.Play("open");
        }

        public virtual void Close() { if (!IsOpen) return; win.style.display = DisplayStyle.None; Audio.Sfx.Play("close"); UIRoot.I?.hud?.HideTip(); }
        public abstract void Refresh();
        public virtual void Tick() { }

        protected static VisualElement ItemCell(ItemStack st, VisualElement parent, Action onClick, Action onRight = null, int count = 1)
        {
            var cell = UIB.El("inv-cell", parent);
            var d = st.Def;
            if (d == null) return cell;
            if (d.rarity == Rarity.Uncommon) cell.AddToClassList("rarity-uncommon");
            if (d.rarity == Rarity.Rare) cell.AddToClassList("rarity-rare");
            if (d.rarity == Rarity.VeryRare || d.rarity == Rarity.Legendary) cell.AddToClassList("rarity-veryrare");
            if (d.rarity == Rarity.Story) cell.AddToClassList("rarity-story");
            var ic = UIB.Img(Icons.Get(d.icon), null, cell, d.tint == Color.white ? RarityColor(d.rarity) : d.tint);
            ic.style.position = Position.Absolute; ic.style.left = ic.style.top = ic.style.right = ic.style.bottom = 8;
            if (count > 1) { var c = UIB.Lbl(count.ToString(), "slot-count", cell); }
            cell.RegisterCallback<MouseUpEvent>(e => { if (e.button == 0) onClick?.Invoke(); else if (e.button == 1) onRight?.Invoke(); });
            cell.RegisterCallback<MouseEnterEvent>(_ => ItemTip.Show(st, cell));
            cell.RegisterCallback<MouseLeaveEvent>(_ => ItemTip.Hide());
            return cell;
        }

        public static Color RarityColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Uncommon: return new Color(.5f, .9f, .55f);
                case Rarity.Rare: return new Color(.5f, .7f, 1f);
                case Rarity.VeryRare: case Rarity.Legendary: return new Color(.8f, .55f, 1f);
                case Rarity.Story: return new Color(1f, .8f, .4f);
                default: return new Color(.9f, .86f, .78f);
            }
        }
    }

    /// <summary>Item tooltip shared by inventory, loot and trade.</summary>
    public static class ItemTip
    {
        static VisualElement tip;
        public static void Show(ItemStack st, VisualElement anchor, int price = -1)
        {
            var d = st.Def; if (d == null) return;
            if (tip == null) { tip = UIB.El("tooltip", UIRoot.I.tipLayer); tip.pickingMode = PickingMode.Ignore; }
            tip.Clear(); tip.style.display = DisplayStyle.Flex;
            var t = UIB.Lbl(d.name, "tt-title", tip); t.style.color = Window.RarityColor(d.rarity); Theme.ApplyFont(t, true);
            UIB.Lbl($"{d.rarity} {d.kind}" + (d.weight > 0 ? $" · {d.weight:0.#} kg" : "") + $" · {d.value} gp", "tt-sub", tip);
            if (d.IsWeapon && d.id != "unarmed")
            {
                UIB.Lbl($"{d.dmg}{(d.magic > 0 ? "+" + d.magic : "")} {d.dtype}" + (string.IsNullOrEmpty(d.versatile) ? "" : $" (two-handed {d.versatile})") + (string.IsNullOrEmpty(d.extraDmg) ? "" : $" + {d.extraDmg} {d.extraType}"), "tt-stat", tip);
                var props = new List<string>();
                foreach (WeaponProps p in Enum.GetValues(typeof(WeaponProps))) if (p != WeaponProps.None && p != WeaponProps.Monk && d.Has(p)) props.Add(p.ToString());
                UIB.Lbl($"{d.wcat} weapon · {string.Join(", ", props)}", "tt-body dim", tip);
                if (d.weaponActions.Length > 0) UIB.Lbl("Weapon actions: " + string.Join(", ", d.weaponActions.Select(w => ActionLibrary.Get(w)?.name ?? w)), "tt-body gold", tip);
            }
            if (d.kind == ItemKind.Armor) UIB.Lbl($"Armour Class {d.baseAC}{(d.acat == ArmorCat.Light ? " + Dex" : d.acat == ArmorCat.Medium ? " + Dex (max 2)" : "")} · {d.acat} armour" + (d.stealthDis ? " · Stealth disadvantage" : ""), "tt-stat", tip);
            if (d.kind == ItemKind.Shield) UIB.Lbl($"+{d.baseAC} Armour Class", "tt-stat", tip);
            if (d.acBonus > 0) UIB.Lbl($"+{d.acBonus} Armour Class", "tt-stat good", tip);
            if (d.saveBonus > 0) UIB.Lbl($"+{d.saveBonus} to saving throws", "tt-stat good", tip);
            if (!string.IsNullOrEmpty(d.desc)) UIB.Lbl(d.desc, "tt-body", tip).style.marginTop = 6;
            if (price >= 0) UIB.Lbl($"Price: {price} gold", "tt-stat gold", tip).style.marginTop = 6;
            var wb = anchor.worldBound;
            float rw = UIRoot.I.root.layout.width;
            tip.style.left = Mathf.Min(wb.xMax + 8, rw - 400); tip.style.top = Mathf.Max(8, wb.yMin - 20);
            tip.BringToFront();
        }
        public static void Hide() { if (tip != null) tip.style.display = DisplayStyle.None; }
    }

    // =====================================================================================
    public class InventoryWindow : Window
    {
        public override string Id => "inventory";
        public override string Title => "Inventory";
        public override float Width => 1180;
        public override float Height => 780;
        int who;

        public override void Refresh()
        {
            body.Clear();
            var g = Game.I;
            if (g.party.Count == 0) return;
            who = Mathf.Clamp(g.selectedIndex, 0, g.party.Count - 1);
            var tabs = UIB.Row(body);
            for (int i = 0; i < g.party.Count; i++)
            {
                int k = i;
                var b = UIB.Btn(g.party[i].name, () => { g.Select(k); Refresh(); }, "tab" + (i == who ? " tab-active" : ""), tabs);
            }
            var main = UIB.Row(body); main.style.flexGrow = 1; main.style.alignItems = Align.Stretch; main.style.marginTop = 10;
            var c = g.party[who];
            // equipment paper doll
            var left = UIB.Col(main, "panel-inset"); left.style.width = 420; left.style.marginRight = 12;
            UIB.Lbl($"{c.name} — Level {c.level} {c.sheet.Title}", "h3", left);
            UIB.Lbl($"AC {c.AC}   HP {Mathf.Max(0, c.hp)}/{c.MaxHPTotal}   Speed {c.Speed:F1}m", "tt-stat", left);
            var slotsGrid = UIB.Row(left, "wrap"); slotsGrid.style.marginTop = 8;
            foreach (var s in Equipment.AllSlots)
            {
                var col = UIB.Col(slotsGrid); col.style.alignItems = Align.Center; col.style.width = 92;
                var cell = UIB.El("equip-slot", col);
                var st = c.eq.Get(s);
                if (st != null && st.Def != null)
                {
                    var ic = UIB.Img(Icons.Get(st.Def.icon), null, cell, RarityColor(st.Def.rarity));
                    ic.style.position = Position.Absolute; ic.style.left = ic.style.top = ic.style.right = ic.style.bottom = 8;
                    var slot = s;
                    cell.RegisterCallback<ClickEvent>(_ => { g.Unequip(c, slot); Refresh(); });
                    cell.RegisterCallback<MouseEnterEvent>(_ => ItemTip.Show(st, cell));
                    cell.RegisterCallback<MouseLeaveEvent>(_ => ItemTip.Hide());
                }
                UIB.Lbl(SlotName(s), "equip-label", col);
            }
            UIB.Lbl("Click an equipped item to unequip it. Click an item in the stash to equip or use it. Shift-click a light weapon to put it in the off hand. Right-click to drop or give.", "tiny dim", left).style.marginTop = 10;
            // weapon summary
            var w = c.MainWeapon;
            if (w != null) UIB.Lbl($"Main hand: {w.name} — +{c.WeaponAttackBonus(w)} to hit, {HUD.DamageText(c, ActionLibrary.Get("attack"))?.Replace("Damage: ", "")}", "tt-body", left).style.marginTop = 8;
            if (c.RangedWeapon != null) UIB.Lbl($"Ranged: {c.RangedWeapon.name} — +{c.WeaponAttackBonus(c.RangedWeapon)} to hit", "tt-body", left);
            // stash
            var right = UIB.Col(main, "panel-inset"); right.style.flexGrow = 1;
            var hdr = UIB.Row(right, "space-between");
            UIB.Lbl("Party Stash", "h3", hdr);
            UIB.Lbl($"<color=#e6c67a>{g.gold} gold</color>   ·   {g.stash.Weight:0.#} kg", "small", hdr);
            var sv = UIB.Scroll(right);
            var grid = UIB.El("inv-grid", sv);
            foreach (var st in g.stash.items.OrderBy(i => (int)i.Def.kind).ThenBy(i => i.Def.name).ToList())
            {
                var stc = st;
                ItemCell(st, grid, () => OnItem(c, stc), () => OnRight(c, stc), st.count);
            }
        }

        static string SlotName(Slot s)
        {
            switch (s)
            {
                case Slot.MainHand: return "Main Hand";
                case Slot.OffHand: return "Off Hand";
                case Slot.Ring1: return "Ring";
                case Slot.Ring2: return "Ring";
                default: return s.ToString();
            }
        }

        void OnItem(Creature c, ItemStack st)
        {
            var d = st.Def;
            ItemTip.Hide();
            if (d.Equippable) { Game.I.Equip(c, st); Refresh(); return; }
            if (!string.IsNullOrEmpty(d.useAction))
            {
                var a = ActionLibrary.Get(d.useAction);
                Close();
                if (CombatManager.I.Active) CombatManager.I.UseAction(a, st);
                else
                {
                    if (!RulesEngine.CanUse(c, a, false, out var why)) { Toast.Show(why, Theme.Failure); return; }
                    Targeting.I.Begin(c, a, false, ctx => CombatManager.I.StartCoroutine(RulesEngine.Execute(ctx, false)), null, st);
                }
                return;
            }
            if (!string.IsNullOrEmpty(d.lore)) { ReadWindow.Show(d.name, d.lore); return; }
            Toast.Show(d.name, Theme.Parchment);
        }

        void OnRight(Creature c, ItemStack st)
        {
            if (st.Def.kind == ItemKind.Quest || st.Def.kind == ItemKind.Key) { Toast.Show("You'd better hold on to that.", Theme.TextDim); return; }
            Game.I.stash.Remove(st);
            Toast.Show($"Dropped {st.Def.name}.", Theme.TextDim);
            Refresh();
        }
    }

    // =====================================================================================
    public class CharacterWindow : Window
    {
        public override string Id => "character";
        public override string Title => "Character";
        public override float Width => 1240;
        public override float Height => 800;

        public override void Refresh()
        {
            body.Clear();
            var g = Game.I;
            if (g.party.Count == 0) return;
            int who = Mathf.Clamp(g.selectedIndex, 0, g.party.Count - 1);
            var tabs = UIB.Row(body);
            for (int i = 0; i < g.party.Count; i++) { int k = i; UIB.Btn(g.party[i].name, () => { g.Select(k); Refresh(); }, "tab" + (i == who ? " tab-active" : ""), tabs); }
            var c = g.party[who]; var s = c.sheet;
            var main = UIB.Row(body); main.style.flexGrow = 1; main.style.alignItems = Align.Stretch; main.style.marginTop = 10;
            // left: identity & abilities
            var left = UIB.Col(main, "panel-inset"); left.style.width = 400; left.style.marginRight = 10;
            var portrait = UIB.Img(PortraitRenderer.I?.Get(c), null, left); UIB.Size(portrait, 200, 234); portrait.style.alignSelf = Align.Center;
            var nm = UIB.Lbl(c.name, "h2 center", left); Theme.ApplyFont(nm, true);
            UIB.Lbl($"Level {c.level} {s.Title}" + (s.Sub != null ? $"\n{s.Sub.name}" : ""), "center gold", left);
            UIB.Lbl($"{RulesData.Backgrounds[s.background].name} · XP {s.xp}" + (c.level < RulesData.MaxLevel ? $" / {RulesData.XpForLevel[c.level + 1]}" : ""), "center small dim", left);
            if (g.CanLevelUp(c)) UIB.Btn("Level Up!", () => { Close(); UIRoot.I.Win<LevelUpWindow>("levelup").OpenFor(c); }, "btn btn-primary", left).style.marginTop = 8;
            var stats = UIB.Row(left, "wrap"); stats.style.justifyContent = Justify.Center; stats.style.marginTop = 8;
            foreach (Ability ab in Enum.GetValues(typeof(Ability)))
            {
                var box = UIB.El("stat-box", stats);
                UIB.Lbl(ab.ToString(), "stat-name", box);
                UIB.Lbl(c.Score(ab).ToString(), "stat-val", box);
                int m = c.Mod(ab);
                UIB.Lbl((m >= 0 ? "+" : "") + m, "stat-mod", box);
                if (c.saveProf.Contains(ab)) box.style.borderTopColor = box.style.borderBottomColor = box.style.borderLeftColor = box.style.borderRightColor = Theme.Gold;
            }
            var line = UIB.Row(left, "wrap"); line.style.justifyContent = Justify.Center; line.style.marginTop = 6;
            foreach (var t in new[] { $"AC {c.AC}", $"HP {Mathf.Max(0, c.hp)}/{c.MaxHPTotal}", $"Initiative {(c.InitiativeBonus >= 0 ? "+" : "")}{c.InitiativeBonus}", $"Speed {c.Speed:F1}m", $"Proficiency +{c.Prof}" }) UIB.Lbl(t, "chip", line);
            if (s.Class.caster != CasterType.None) UIB.Lbl($"Spell save DC {c.SpellDC} · Spell attack +{c.SpellAttackBonus} ({c.SpellAbility.Long()})", "small center gold", left).style.marginTop = 6;
            // middle: skills & saves
            var mid = UIB.Col(main, "panel-inset"); mid.style.width = 300; mid.style.marginRight = 10;
            UIB.Lbl("Saving Throws", "h3", mid);
            foreach (Ability ab in Enum.GetValues(typeof(Ability))) UIB.Lbl($"{(c.saveProf.Contains(ab) ? "◆" : "◇")} {ab.Long()}  <color=#e6c67a>{Sign(c.SaveBonus(ab))}</color>", "small", mid);
            UIB.Lbl("Skills", "h3", mid);
            var sks = UIB.Scroll(mid);
            foreach (Skill sk in Enum.GetValues(typeof(Skill)))
            {
                string mark = c.expertise.Contains(sk) ? "◆◆" : c.skillProf.Contains(sk) ? "◆" : "◇";
                UIB.Lbl($"{mark} {sk.Nice()} <size=80%><color=#8a8278>({sk.Ab()})</color></size>  <color=#e6c67a>{Sign(c.SkillBonus(sk))}</color>", "small", sks);
            }
            UIB.Lbl($"Passive Perception {c.PassivePerception}", "small dim", mid);
            // right: features, spells, conditions
            var right = UIB.Col(main, "panel-inset"); right.style.flexGrow = 1;
            var sv = UIB.Scroll(right);
            UIB.Lbl("Features & Traits", "h3", sv);
            foreach (var f in s.Class.features.Where(f => f.level <= c.level)) Feature(sv, f.name, f.desc);
            if (s.Sub != null) foreach (var f in s.Sub.features.Where(f => f.level <= c.level)) Feature(sv, f.name, f.desc);
            if (!string.IsNullOrEmpty(s.fightingStyle)) Feature(sv, "Fighting Style: " + RulesData.StyleName(s.fightingStyle), RulesData.StyleDesc(s.fightingStyle));
            foreach (var t in s.Race.traits) Feature(sv, t.Split(':')[0], t.Contains(":") ? t.Substring(t.IndexOf(':') + 1).Trim() : "");
            if (s.Subrace != null) foreach (var t in s.Subrace.traits) Feature(sv, t.Split(':')[0], t.Contains(":") ? t.Substring(t.IndexOf(':') + 1).Trim() : "");
            if (c.knownSpells.Count > 0)
            {
                UIB.Lbl("Spells", "h3", sv);
                foreach (var grp in c.knownSpells.Select(ActionLibrary.Get).Where(a => a != null).GroupBy(a => a.spellLevel).OrderBy(gp => gp.Key))
                    UIB.Lbl($"<color=#e6c67a>{(grp.Key == 0 ? "Cantrips" : "Level " + grp.Key)}:</color> " + string.Join(", ", grp.Select(a => a.name)), "small", sv);
            }
            if (c.conds.Count > 0)
            {
                UIB.Lbl("Conditions", "h3", sv);
                foreach (var cd in c.conds) UIB.Lbl($"<color={Theme.Hex(Conditions.Get(cd.id).color)}>{cd.id.Nice()}</color> — {Conditions.Get(cd.id).desc}", "small", sv);
            }
        }

        static void Feature(VisualElement p, string n, string d)
        {
            var e = UIB.Col(p); e.style.marginBottom = 6;
            UIB.Lbl(n, "gold", e);
            if (!string.IsNullOrEmpty(d)) UIB.Lbl(d, "small", e);
        }

        static string Sign(int v) => (v >= 0 ? "+" : "") + v;
    }

    // =====================================================================================
    public class JournalWindow : Window
    {
        public override string Id => "journal";
        public override string Title => "Journal";
        public override float Width => 1100;
        public override float Height => 760;
        string sel;

        public override void Refresh()
        {
            body.Clear();
            var j = Game.I.journal;
            var main = UIB.Row(body); main.style.flexGrow = 1; main.style.alignItems = Align.Stretch;
            var list = UIB.Col(main, "panel-inset"); list.style.width = 360; list.style.marginRight = 10;
            var sv = UIB.Scroll(list);
            UIB.Lbl("Active", "h3", sv);
            foreach (var q in j.quests.Where(q => q.state == QuestState.Active).OrderByDescending(q => q.order)) QuestBtn(sv, q);
            if (j.quests.Any(q => q.state != QuestState.Active))
            {
                UIB.Lbl("Completed", "h3", sv);
                foreach (var q in j.quests.Where(q => q.state != QuestState.Active).OrderByDescending(q => q.order)) QuestBtn(sv, q);
            }
            if (j.lore.Count > 0) { UIB.Lbl("Lore", "h3", sv); foreach (var l in j.lore) UIB.Lbl("· " + l, "small dim", sv); }
            var detail = UIB.Col(main, "panel-inset"); detail.style.flexGrow = 1;
            var qsel = j.quests.FirstOrDefault(q => q.id == sel) ?? j.quests.Where(q => q.state == QuestState.Active).OrderByDescending(q => q.order).FirstOrDefault();
            if (qsel != null)
            {
                var t = UIB.Lbl(qsel.title, "h2", detail); Theme.ApplyFont(t, true);
                UIB.Lbl(qsel.state == QuestState.Done ? "<color=#70e090>Completed</color>" : qsel.state == QuestState.Failed ? "<color=#e87a6a>Failed</color>" : "<color=#e6c67a>In progress</color>", "small", detail);
                var dv = UIB.Scroll(detail);
                for (int i = qsel.log.Count - 1; i >= 0; i--) { var l = UIB.Lbl(qsel.log[i], i == qsel.log.Count - 1 ? "" : "dim small", dv); l.style.marginTop = 8; }
            }
            else UIB.Lbl("No entries yet.", "dim", detail);
        }

        void QuestBtn(VisualElement p, QuestEntry q)
        {
            var b = UIB.Btn(q.title, () => { sel = q.id; Refresh(); }, "btn" + (q.id == sel ? " btn-selected" : ""), p);
            b.style.unityTextAlign = TextAnchor.MiddleLeft;
        }
    }

    // =====================================================================================
    public class LootWindow : Window
    {
        public override string Id => "loot";
        public override string Title => "Loot";
        public override float Width => 640;
        public override float Height => 520;
        Container container; Actor corpse; Actor user;

        public static void Open(Container c, Actor u) { var w = UIRoot.I.Win<LootWindow>("loot"); w.container = c; w.corpse = null; w.user = u; w.Open(); }
        public static void OpenCorpse(Actor a, Actor u)
        {
            var w = UIRoot.I.Win<LootWindow>("loot"); w.container = null; w.corpse = a; w.user = u;
            if (!a.looted && a.c.mdef != null && a.gold == 0 && a.c.type == CreatureType.Humanoid) a.gold = Rng.Range(0, 6);
            w.Open();
        }

        List<ItemStack> Items => container != null ? container.items : corpse?.loot;

        public override void Refresh()
        {
            body.Clear();
            SetTitle(container != null ? container.label : corpse != null ? corpse.Name : "Loot");
            var items = Items; if (items == null) return;
            int gold = container != null ? container.gold : corpse.gold;
            var grid = UIB.El("inv-grid", body);
            if (gold > 0)
            {
                var gcell = UIB.El("inv-cell", grid);
                var ic = UIB.Img(Icons.Get("coins"), null, gcell, Theme.Gold); ic.style.position = Position.Absolute; ic.style.left = ic.style.top = ic.style.right = ic.style.bottom = 8;
                UIB.Lbl(gold.ToString(), "slot-count", gcell);
                gcell.RegisterCallback<ClickEvent>(_ => { TakeGold(); Refresh(); });
            }
            foreach (var st in items.ToList())
            {
                var s = st;
                ItemCell(st, grid, () => { Take(s); Refresh(); }, null, st.count);
            }
            if (items.Count == 0 && gold == 0) UIB.Lbl("Empty.", "dim", body);
            var row = UIB.Row(body); row.style.marginTop = 16;
            UIB.Btn("Take All", () => { TakeGold(); foreach (var s in items.ToList()) Take(s); Close(); }, "btn btn-primary", row);
            UIB.Btn("Close", Close, "btn", row);
        }

        void TakeGold()
        {
            int g = container != null ? container.gold : corpse.gold;
            if (g <= 0) return;
            Game.I.gold += g;
            if (container != null) container.gold = 0; else corpse.gold = 0;
            Audio.Sfx.Play("gold");
            Toast.Show($"+{g} gold", Theme.Gold);
            MarkEmpty();
        }

        void Take(ItemStack s)
        {
            Items.Remove(s);
            Game.I.stash.Add(s);
            Toast.Show($"Took {s.Def.name}" + (s.count > 1 ? $" x{s.count}" : ""), Theme.Parchment);
            Audio.Sfx.Play("pickup");
            MarkEmpty();
            Campaign.OnItemTaken(s.id);
        }

        void MarkEmpty()
        {
            var items = Items;
            int g = container != null ? container.gold : corpse.gold;
            if (items.Count == 0 && g == 0)
            {
                if (container != null) Game.I.SetFlag("emptied:" + container.id, 1);
                if (corpse != null) { corpse.looted = true; Game.I.SetFlag("looted:" + corpse.npcId, 1); }
            }
        }
    }

    // =====================================================================================
    public class ReadWindow : Window
    {
        public override string Id => "read";
        public override string Title => "";
        public override float Width => 760;
        public override float Height => 720;
        string title, text;
        public static void Show(string t, string x) { var w = UIRoot.I.Win<ReadWindow>("read"); w.title = t; w.text = x; w.Open(); }
        public override void Refresh()
        {
            body.Clear();
            SetTitle(title);
            var paper = UIB.El("panel-inset", body); paper.style.flexGrow = 1; paper.style.backgroundColor = new Color(.18f, .15f, .11f, .95f);
            var sv = UIB.Scroll(paper);
            var l = UIB.Lbl(text, "", sv); l.style.fontSize = 20; l.style.color = new Color(.92f, .87f, .76f); l.style.whiteSpace = WhiteSpace.Normal; l.style.paddingLeft = 12; l.style.paddingRight = 12;
        }
    }

    // =====================================================================================
    public static class RestMenu { public static void Open() => UIRoot.I.Open("rest"); }

    public class RestWindow : Window
    {
        public override string Id => "rest";
        public override string Title => "Rest";
        public override float Width => 620;
        public override float Height => 420;
        public override void Refresh()
        {
            body.Clear();
            var g = Game.I;
            if (CombatManager.I.Active) { UIB.Lbl("You can't rest while enemies are near.", "bad", body); return; }
            UIB.Lbl("Short Rest", "h3", body);
            UIB.Lbl($"Catch your breath: regain half your hit points and recover short-rest abilities. ({2 - g.shortRestsUsed} left before you need a long rest)", "small", body);
            UIB.Btn("Take a Short Rest", () => { if (g.ShortRest()) Close(); }, "btn", body).SetEnabled(g.shortRestsUsed < 2);
            UIB.El("divider", body);
            UIB.Lbl("Long Rest", "h3", body);
            bool safe = Campaign.CanLongRest(out string where);
            UIB.Lbl(safe ? $"Make camp for the night{(where != null ? " — " + where : "")}. Fully restores hit points, spell slots and abilities." : "This place is not safe enough to sleep. Get out of it first.", "small" + (safe ? "" : " dim"), body);
            UIB.Btn("Long Rest", () => { Close(); Campaign.DoLongRest(); }, "btn btn-primary", body).SetEnabled(safe);
        }
    }

    // =====================================================================================
    public class TradeWindow : Window
    {
        public override string Id => "trade";
        public override string Title => "Trade";
        public override float Width => 1100;
        public override float Height => 720;
        public List<ItemStack> wares = new List<ItemStack>();
        public string merchantName; public float markup = 1f, sellRate = 0.5f;
        public int merchantGold = 200;

        public static void OpenShop(string name, List<ItemStack> wares, float markup, float sellRate, int gold)
        {
            var w = UIRoot.I.Win<TradeWindow>("trade");
            w.merchantName = name; w.wares = wares; w.markup = markup; w.sellRate = sellRate; w.merchantGold = gold;
            w.Open();
        }

        int Price(ItemDef d, bool buying) => Mathf.Max(1, Mathf.RoundToInt(d.value * (buying ? markup : sellRate)));

        public override void Refresh()
        {
            body.Clear();
            SetTitle("Trade with " + merchantName);
            var g = Game.I;
            var main = UIB.Row(body); main.style.flexGrow = 1; main.style.alignItems = Align.Stretch;
            var left = UIB.Col(main, "panel-inset"); left.style.flexGrow = 1; left.style.marginRight = 10;
            UIB.Lbl($"{merchantName}'s wares  ·  <color=#e6c67a>{merchantGold} gold</color>", "h3", left);
            var grid = UIB.El("inv-grid", UIB.Scroll(left));
            foreach (var st in wares.ToList())
            {
                var s = st; int price = Price(s.Def, true);
                var cell = ItemCell(s, grid, () => Buy(s, price), null, s.count);
                cell.RegisterCallback<MouseEnterEvent>(_ => ItemTip.Show(s, cell, price));
            }
            var right = UIB.Col(main, "panel-inset"); right.style.flexGrow = 1;
            UIB.Lbl($"Your stash  ·  <color=#e6c67a>{g.gold} gold</color>", "h3", right);
            var grid2 = UIB.El("inv-grid", UIB.Scroll(right));
            foreach (var st in g.stash.items.Where(i => i.Def.kind != ItemKind.Quest && i.Def.kind != ItemKind.Key && i.Def.value > 0).ToList())
            {
                var s = st; int price = Price(s.Def, false);
                var cell = ItemCell(s, grid2, () => Sell(s, price), null, s.count);
                cell.RegisterCallback<MouseEnterEvent>(_ => ItemTip.Show(s, cell, price));
            }
            UIB.Lbl("Click an item to buy or sell one of it.", "tiny dim", body);
        }

        void Buy(ItemStack s, int price)
        {
            var g = Game.I;
            if (g.gold < price) { Toast.Show("Not enough gold.", Theme.Failure); Audio.Sfx.Play("error"); return; }
            g.gold -= price; merchantGold += price;
            if (s.count > 1) { s.count--; g.stash.Add(s.id, 1); } else { wares.Remove(s); g.stash.Add(s); }
            Audio.Sfx.Play("gold"); ItemTip.Hide(); Refresh();
        }

        void Sell(ItemStack s, int price)
        {
            var g = Game.I;
            if (merchantGold < price) { Toast.Show($"{merchantName} can't afford that.", Theme.Failure); return; }
            g.gold += price; merchantGold -= price;
            g.stash.Remove(s.id, 1);
            var ex = wares.FirstOrDefault(w => w.id == s.id && s.Def.stackable);
            if (ex != null) ex.count++; else wares.Add(new ItemStack(s.id));
            Audio.Sfx.Play("gold"); ItemTip.Hide(); Refresh();
        }
    }

    // =====================================================================================
    public class PauseWindow : Window
    {
        public override string Id => "pause";
        public override string Title => "Paused";
        public override float Width => 440;
        public override float Height => 560;
        public override void Refresh()
        {
            body.Clear();
            var col = UIB.Col(body); col.style.alignItems = Align.Stretch;
            UIB.Btn("Resume", Close, "btn btn-primary", col);
            UIB.Btn("Save Game", () => { Close(); UIRoot.I.Win<SaveLoadWindow>("saveload").OpenMode(true); }, "btn", col).SetEnabled(Game.I.mode == GameMode.Explore);
            UIB.Btn("Load Game", () => { Close(); UIRoot.I.Win<SaveLoadWindow>("saveload").OpenMode(false); }, "btn", col);
            UIB.Btn("Options", () => { Close(); UIRoot.I.Open("options"); }, "btn", col);
            UIB.Btn("Controls", () => ReadWindow.Show("Controls", Campaign.ControlsText), "btn", col);
            UIB.Btn("Main Menu", () => { Close(); Game.I.GoToMainMenu(); }, "btn", col);
            UIB.Btn("Quit to Desktop", () => { Application.Quit(); }, "btn", col);
            foreach (var b in col.Children()) { b.style.height = 46; b.style.fontSize = 19; }
        }
    }

    // =====================================================================================
    public class OptionsWindow : Window
    {
        public override string Id => "options";
        public override string Title => "Options";
        public override float Width => 700;
        public override float Height => 640;
        public override void Refresh()
        {
            body.Clear();
            var sv = UIB.Scroll(body);
            UIB.Lbl("Audio", "h3", sv);
            AddSlider(sv, "Master volume", Settings.Master, v => { Settings.Master = v; Audio.AudioSys.I.ApplyVolumes(); });
            AddSlider(sv, "Music", Settings.Music, v => { Settings.Music = v; Audio.AudioSys.I.ApplyVolumes(); });
            AddSlider(sv, "Effects", Settings.Effects, v => { Settings.Effects = v; Audio.AudioSys.I.ApplyVolumes(); });
            AddSlider(sv, "Ambience", Settings.Ambience, v => { Settings.Ambience = v; Audio.AudioSys.I.ApplyVolumes(); });
            UIB.Lbl("Gameplay", "h3", sv);
            AddToggle(sv, "Use reactions automatically (Shield, Hellish Rebuke, Uncanny Dodge)", Settings.AutoReactions, v => Settings.AutoReactions = v);
            AddToggle(sv, "Always show the event log", Settings.ShowLog, v => Settings.ShowLog = v);
            AddSlider(sv, "Text speed", Settings.TextSpeed / 2f, v => Settings.TextSpeed = Mathf.Max(0.2f, v * 2f));
            AddSlider(sv, "Camera pan speed", CameraRig.I.panSpeed / 2f, v => { CameraRig.I.panSpeed = Mathf.Max(0.2f, v * 2f); Settings.PanSpeed = CameraRig.I.panSpeed; });
            UIB.Lbl("Graphics", "h3", sv);
            AddToggle(sv, "Fullscreen", Screen.fullScreen, v => Screen.fullScreen = v);
            AddToggle(sv, "High quality shadows", Settings.HighShadows, v => { Settings.HighShadows = v; Settings.ApplyGraphics(); });
            AddToggle(sv, "Film grain", Settings.Grain, v => { Settings.Grain = v; Settings.ApplyGraphics(); });
            UIB.Btn("Done", () => { Settings.Save(); Close(); }, "btn btn-primary", body);
        }

        static void AddSlider(VisualElement p, string label, float v, Action<float> set)
        {
            var s = new Slider(label, 0, 1) { value = v };
            s.RegisterValueChangedCallback(e => set(e.newValue));
            p.Add(s);
        }

        static void AddToggle(VisualElement p, string label, bool v, Action<bool> set)
        {
            var t = new Toggle(label) { value = v };
            t.RegisterValueChangedCallback(e => set(e.newValue));
            p.Add(t);
        }

        public override void Close() { Settings.Save(); base.Close(); }
    }

    // =====================================================================================
    public class SaveLoadWindow : Window
    {
        public override string Id => "saveload";
        public override string Title => "Save / Load";
        public override float Width => 900;
        public override float Height => 680;
        bool saving;

        public void OpenMode(bool save) { saving = save; SetTitle(save ? "Save Game" : "Load Game"); Open(); }

        public override void Refresh()
        {
            body.Clear();
            var sv = UIB.Scroll(body);
            if (saving)
            {
                var row = UIB.Row(sv);
                var tf = new TextField { value = "Save " + DateTime.Now.ToString("MMM d, HH:mm") }; tf.style.flexGrow = 1; row.Add(tf);
                UIB.Btn("Save New", () => { SaveSystem.Save(Sanitize(tf.value)); Refresh(); Toast.Show("Game saved.", Theme.Success); }, "btn btn-primary", row);
            }
            foreach (var info in SaveSystem.List())
            {
                var card = UIB.Row(sv, "card"); card.style.justifyContent = Justify.SpaceBetween;
                var txt = UIB.Col(card);
                UIB.Lbl(info.name, "gold", txt);
                UIB.Lbl($"{info.area} · {info.party} · {info.time:g} · played {TimeSpan.FromSeconds(info.playTime):hh\\:mm}", "small dim", txt);
                var btns = UIB.Row(card);
                var inf = info;
                if (saving) UIB.Btn("Overwrite", () => { SaveSystem.Save(inf.name); Refresh(); Toast.Show("Game saved.", Theme.Success); }, "btn btn-small", btns);
                else UIB.Btn("Load", () => { Close(); SaveSystem.Load(inf.name); }, "btn btn-small btn-primary", btns);
                UIB.Btn("Delete", () => { SaveSystem.Delete(inf.name); Refresh(); }, "btn btn-small", btns);
            }
            if (!saving && !SaveSystem.List().Any()) UIB.Lbl("No saved games yet.", "dim", sv);
        }

        static string Sanitize(string s) { foreach (var c in System.IO.Path.GetInvalidFileNameChars()) s = s.Replace(c, '-'); return s.Trim(); }
    }
}
