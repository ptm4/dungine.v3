using System.Collections.Generic;
using System.Linq;
using Dungine.Combat;
using Dungine.Rules;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dungine.UI
{
    /// <summary>In-game HUD: portraits, hotbar with resources, turn order, combat log, menu buttons, tooltips.</summary>
    public class HUD
    {
        readonly VisualElement root;
        public MiniMap miniMap;
        VisualElement portraits, hotbarWrap, hotbar, slotsRow, resRow, turnOrder, logBox, buttons, tooltip, topInfo;
        ScrollView logScroll;
        Button endTurn;
        Label moveLabel, goldLabel, areaLabel;
        VisualElement moveFill;
        readonly List<(VisualElement el, ActionDef a, ItemStack item)> slots = new List<(VisualElement, ActionDef, ItemStack)>();
        Creature shownFor; int shownHash;
        bool dirty = true;
        float tickT;

        public HUD(VisualElement parent)
        {
            root = UIB.El("layer", parent); root.pickingMode = PickingMode.Ignore;
            portraits = UIB.El("portrait-col", root); portraits.pickingMode = PickingMode.Ignore;
            topInfo = UIB.Col(root); UIB.Pos(topInfo, 16, 14); topInfo.pickingMode = PickingMode.Ignore;
            areaLabel = UIB.Lbl("", "gold", topInfo); areaLabel.style.fontSize = 20; Theme.ApplyFont(areaLabel, true);
            goldLabel = UIB.Lbl("", "small dim", topInfo);
            hotbarWrap = UIB.El("hotbar-wrap", root); hotbarWrap.pickingMode = PickingMode.Ignore;
            var bar = UIB.El("hotbar", hotbarWrap);
            hotbar = UIB.Col(bar);
            resRow = UIB.El("resource-bar", hotbar);
            slotsRow = UIB.Row(hotbar, "wrap"); slotsRow.style.maxWidth = 1330;
            endTurn = UIB.Btn("End Turn\n<size=70%>[Space]</size>", () => CombatManager.I.EndTurn(), "btn btn-primary endturn", bar);
            turnOrder = UIB.El("turn-order", root); turnOrder.pickingMode = PickingMode.Ignore;
            logBox = UIB.El("combat-log panel-soft", root);
            logScroll = UIB.Scroll(logBox);
            buttons = UIB.El("hud-buttons", root);
            HudButton("bag", "Inventory [I]", () => UIRoot.I.Toggle("inventory"));
            HudButton("person", "Character [C]", () => UIRoot.I.Toggle("character"));
            HudButton("book", "Journal [J]", () => UIRoot.I.Toggle("journal"));
            HudButton("tent", "Rest", () => RestMenu.Open());
            HudButton("gear", "Menu [Esc]", () => UIRoot.I.Open("pause"));
            miniMap = new MiniMap(root);
            tooltip = UIB.El("tooltip", root); tooltip.pickingMode = PickingMode.Ignore; tooltip.style.display = DisplayStyle.None;
            CombatLog.OnLine += AddLog;
            Game.I.OnPartyChanged += () => dirty = true;
            Game.I.OnSelectionChanged += () => dirty = true;
            CombatManager.I.OnTurnChanged += () => { dirty = true; RebuildTurnOrder(); };
        }

        void HudButton(string icon, string tip, System.Action a)
        {
            var b = UIB.Btn("", a, "btn icon-btn", buttons);
            var ic = UIB.Img(Icons.Get(icon), null, b, Theme.Gold); ic.style.flexGrow = 1;
            b.tooltip = tip;
            b.RegisterCallback<MouseEnterEvent>(_ => ShowSimpleTip(tip, b));
            b.RegisterCallback<MouseLeaveEvent>(_ => HideTip());
        }

        public void Show() { root.style.display = DisplayStyle.Flex; dirty = true; }
        public void Hide() { root.style.display = DisplayStyle.None; }

        void AddLog(string s)
        {
            var l = UIB.Lbl(s, "log-line", logScroll);
            if (logScroll.contentContainer.childCount > 80) logScroll.contentContainer.RemoveAt(0);
            logScroll.schedule.Execute(() => logScroll.scrollOffset = new Vector2(0, float.MaxValue)).ExecuteLater(10);
        }

        public void OnTurn(Creature c) { dirty = true; }

        public void Tick()
        {
            miniMap?.Tick();
            if (root.style.display == DisplayStyle.None) return;
            var g = Game.I;
            tickT -= Time.unscaledDeltaTime;
            bool combat = CombatManager.I.Active;
            endTurn.style.display = combat ? DisplayStyle.Flex : DisplayStyle.None;
            endTurn.SetEnabled(combat && CombatManager.I.waitingForPlayer && !CombatManager.I.busy);
            logBox.style.display = combat || Settings.ShowLog ? DisplayStyle.Flex : DisplayStyle.None;
            turnOrder.style.display = combat ? DisplayStyle.Flex : DisplayStyle.None;
            var who = combat && CombatManager.I.Current != null && CombatManager.I.Current.isPC ? CombatManager.I.Current : g.SelectedC;
            int hash = Hash(who);
            if (dirty || who != shownFor || hash != shownHash || tickT <= 0)
            {
                tickT = 0.5f;
                RebuildPortraits();
                if (dirty || who != shownFor || hash != shownHash) RebuildHotbar(who);
                shownFor = who; shownHash = hash;
                dirty = false;
                areaLabel.text = g.area?.def.Title ?? "";
                goldLabel.text = $"{g.gold} gold" + (g.partyInspiration > 0 ? $"   ·   Inspiration {g.partyInspiration}" : "");
            }
            if (moveFill != null && who != null)
            {
                float max = Mathf.Max(0.1f, who.Speed);
                float left = combat ? who.moveLeft : max;
                moveFill.style.width = Length.Percent(Mathf.Clamp01(left / max) * 100f);
                if (moveLabel != null) moveLabel.text = combat ? $"{left:F1}m" : $"{max:F1}m";
            }
        }

        static int Hash(Creature c)
        {
            if (c == null) return 0;
            unchecked
            {
                int h = c.hp * 31 + (c.hasAction ? 1 : 0) + (c.hasBonus ? 2 : 0) + (c.hasReaction ? 4 : 0) + c.attacksLeft * 8;
                for (int i = 1; i <= 3; i++) h = h * 17 + c.slotsUsed[i] + c.slotsMax[i] * 5;
                h = h * 13 + c.pactUsed + c.conds.Count * 7 + c.uses.Values.Sum() + c.resUsed.Values.Sum() * 3 + Game.I.stash.items.Count * 101 + (c.pendingMeta?.Length ?? 0);
                h = h * 7 + c.extraActions.Count + c.level * 1000;
                return h;
            }
        }

        // ---------------------------------------------------------------- portraits
        /// <summary>v3: the library's portrait (a face render, cropped to fill) where it has one, else v2's live render.</summary>
        static VisualElement PortraitImg(Creature c, string cls, VisualElement parent)
        {
            var lib = Library.LibraryIcons.Portrait(c);
            if (!lib) return UIB.Img(PortraitRenderer.I != null ? PortraitRenderer.I.Get(c) : null, cls, parent);
            var img = UIB.Img(lib, cls, parent);
            img.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            return img;
        }

        void RebuildPortraits()
        {
            var g = Game.I;
            portraits.Clear();
            for (int i = 0; i < g.party.Count; i++)
            {
                var c = g.party[i];
                int idx = i;
                var p = UIB.El("portrait", portraits);
                bool isTurn = CombatManager.I.Active && CombatManager.I.Current == c;
                if (c == g.SelectedC) p.AddToClassList("portrait-selected");
                if (isTurn) p.AddToClassList("portrait-turn");
                var img = PortraitImg(c, "portrait-img", p);   // v3: the library's portrait where it has one
                img.pickingMode = PickingMode.Ignore;
                if (c.hp <= 0) img.style.unityBackgroundImageTintColor = c.dead ? new Color(.3f, .3f, .3f) : new Color(1f, .45f, .45f);
                var plate = UIB.El("portrait-plate", p); plate.pickingMode = PickingMode.Ignore;
                var nm = UIB.Lbl(c.name + (c.dead ? " †" : c.hp <= 0 ? " (down)" : ""), "portrait-name", plate);
                var bar = UIB.El("hpbar", plate);
                var fill = UIB.El("hpfill", bar);
                fill.style.width = Length.Percent(Mathf.Clamp01((float)Mathf.Max(0, c.hp) / c.MaxHPTotal) * 100f);
                if (c.tempHP > 0) fill.style.backgroundColor = new Color(.35f, .5f, .9f);
                UIB.Lbl($"{Mathf.Max(0, c.hp)}/{c.MaxHPTotal}" + (c.tempHP > 0 ? $" +{c.tempHP}" : ""), "hptext", bar);
                var cr = UIB.El("cond-row", p);
                foreach (var cd in c.conds.Take(10)) { var pip = UIB.El("cond-pip", cr); pip.style.backgroundColor = Conditions.Get(cd.id).color; pip.tooltip = cd.id.Nice(); }
                if (g.CanLevelUp(c)) { var b = UIB.Lbl("+", "levelup-badge", p); b.tooltip = "Level up available"; b.RegisterCallback<ClickEvent>(e => { g.Select(idx); UIRoot.I.Win<LevelUpWindow>("levelup").OpenFor(c); e.StopPropagation(); }); }
                p.RegisterCallback<ClickEvent>(e =>
                {
                    if (e.clickCount >= 2 && c.actor) { CameraRig.I.Focus(c.actor.transform.position); CameraRig.I.follow = true; }
                    if (!CombatManager.I.Active) g.Select(idx);
                    else if (c.actor) CameraRig.I.Focus(c.actor.transform.position);
                });
                p.RegisterCallback<MouseEnterEvent>(_ => ShowCreatureTip(c, p));
                p.RegisterCallback<MouseLeaveEvent>(_ => HideTip());
            }
        }

        // ---------------------------------------------------------------- hotbar
        void RebuildHotbar(Creature c)
        {
            slotsRow.Clear(); resRow.Clear(); slots.Clear(); moveFill = null; moveLabel = null;
            if (c == null) return;
            bool combat = CombatManager.I.Active;
            // resources
            Pip(Theme.ActionGreen, !combat || c.hasAction || c.attacksLeft > 0, "Action" + (c.attacksLeft > 0 && !c.hasAction ? $" (extra attack ×{c.attacksLeft})" : ""));
            Pip(Theme.BonusOrange, !combat || c.hasBonus, "Bonus Action");
            Pip(new Color(.7f, .45f, .95f), !combat || c.hasReaction, "Reaction");
            var mb = UIB.El("move-bar", resRow); moveFill = UIB.El("move-fill", mb);
            moveLabel = UIB.Lbl("", "small", resRow); moveLabel.style.marginLeft = 6; moveLabel.style.color = Theme.MoveYellow;
            var sp = UIB.El("slot-pips", resRow);
            for (int l = 1; l <= 3; l++)
            {
                if (c.slotsMax[l] <= 0) continue;
                var lab = UIB.Lbl(new[] { "", "I", "II", "III" }[l], "tiny dim", sp); lab.style.marginLeft = 6; lab.style.marginRight = 2;
                for (int k = 0; k < c.slotsMax[l]; k++) { var pip = UIB.El("spell-pip", sp); pip.style.backgroundColor = k < c.slotsMax[l] - c.slotsUsed[l] ? new Color(.45f, .55f, 1f) : new Color(.1f, .1f, .15f); }
            }
            if (c.pactMax > 0)
            {
                var lab = UIB.Lbl("Pact " + new[] { "", "I", "II", "III" }[c.pactLevel], "tiny dim", sp); lab.style.marginLeft = 6; lab.style.marginRight = 2;
                for (int k = 0; k < c.pactMax; k++) { var pip = UIB.El("spell-pip", sp); pip.style.backgroundColor = k < c.pactMax - c.pactUsed ? new Color(.7f, .4f, 1f) : new Color(.1f, .08f, .15f); }
            }
            foreach (var kv in c.resMax)
            {
                var chip = UIB.Lbl($"{RulesEngine.ResourceName(kv.Key)} {c.ResourceLeft(kv.Key)}/{kv.Value}", "chip", resRow);
                chip.style.marginLeft = 8;
            }
            // actions
            var acts = c.AvailableActions();
            var ordered = acts.Where(a => !a.IsSpell && a.cost != ActionCost.Free && !IsGeneral(a)).Concat(acts.Where(a => !a.IsSpell && a.cost == ActionCost.Free))
                .Concat(acts.Where(a => a.IsSpell).OrderBy(a => a.spellLevel).ThenBy(a => a.name)).Concat(acts.Where(IsGeneral)).Distinct().ToList();
            int key = 0;
            foreach (var a in ordered) AddSlot(c, a, null, key++);
            // consumables from the stash
            foreach (var st in Game.I.stash.items.Where(s => s.Def != null && !string.IsNullOrEmpty(s.Def.useAction)).GroupBy(s => s.id).Select(gr => gr.First()))
            {
                var a = ActionLibrary.Get(st.Def.useAction);
                if (a != null) AddSlot(c, a, st, key++);
            }
        }

        static bool IsGeneral(ActionDef a) => a.id == "dash" || a.id == "disengage" || a.id == "hide" || a.id == "shove" || a.id == "help" || a.id == "dodge";

        void Pip(Color c, bool on, string tip)
        {
            var p = UIB.El("res-pip", resRow);
            p.style.backgroundColor = on ? c : new Color(c.r * .2f, c.g * .2f, c.b * .2f);
            p.tooltip = tip;
            p.RegisterCallback<MouseEnterEvent>(_ => ShowSimpleTip(tip, p));
            p.RegisterCallback<MouseLeaveEvent>(_ => HideTip());
        }

        void AddSlot(Creature c, ActionDef a, ItemStack item, int index)
        {
            bool combat = CombatManager.I.Active;
            bool can = RulesEngine.CanUse(c, a, combat, out var why);
            if (combat && (!CombatManager.I.waitingForPlayer || CombatManager.I.Current != c)) { can = false; why = "Not your turn"; }
            var s = UIB.El("slot", slotsRow);
            Color bg = a.IsSpell ? (a.spellLevel == 0 ? new Color(.35f, .4f, .75f) : a.color) : a.cost == ActionCost.BonusAction ? Theme.BonusOrange : a.color;
            // v3: the library's icon tile (it carries its own frame and colours), else v2's painted icon
            var lib = item != null ? Library.LibraryIcons.Item(item.id) : Library.LibraryIcons.Action(a.id);
            VisualElement ic;
            if (lib)
            {
                ic = UIB.Img(lib, null, s);
                UIB.Size(ic, 56, 56);
                ic.style.borderTopLeftRadius = ic.style.borderTopRightRadius = ic.style.borderBottomLeftRadius = ic.style.borderBottomRightRadius = 56 * 0.12f;
                if (item != null) ic.style.backgroundColor = new Color(bg.r * 0.25f, bg.g * 0.25f, bg.b * 0.25f, 1f);
            }
            else ic = UIB.IconCircle(item != null ? item.Def.icon : a.icon, item != null ? item.Def.tint : bg, 56, s);
            ic.pickingMode = PickingMode.Ignore; ic.style.position = Position.Absolute; ic.style.left = 0; ic.style.top = 0;
            if (!can) s.AddToClassList("slot-disabled");
            var cost = UIB.El("slot-cost", s);
            cost.style.backgroundColor = a.cost == ActionCost.Action ? Theme.ActionGreen : a.cost == ActionCost.BonusAction ? Theme.BonusOrange : a.cost == ActionCost.Reaction ? new Color(.7f, .45f, .95f) : Color.gray;
            if (index < 10) UIB.Lbl(((index + 1) % 10).ToString(), "slot-key", s);
            if (item != null) UIB.Lbl(Game.I.stash.Count(item.id).ToString(), "slot-count", s);
            else if (a.maxUses > 0 || a.weaponAction) UIB.Lbl(c.UsesLeft(a).ToString(), "slot-count", s);
            else if (a.IsSpell && a.spellLevel > 0) UIB.Lbl(new[] { "", "I", "II", "III" }[Mathf.Clamp(a.spellLevel, 0, 3)], "slot-count", s);
            s.RegisterCallback<MouseEnterEvent>(_ => ShowActionTip(c, a, item, s, can ? null : why));
            s.RegisterCallback<MouseLeaveEvent>(_ => HideTip());
            s.RegisterCallback<ClickEvent>(_ => Activate(c, a, item));
            slots.Add((s, a, item));
        }

        public void HotbarKey(int k)
        {
            if (k < 0 || k >= slots.Count) return;
            var (el, a, item) = slots[k];
            Activate(shownFor, a, item);
        }

        void Activate(Creature c, ActionDef a, ItemStack item)
        {
            if (c == null) return;
            HideTip();
            if (CombatManager.I.Active) { CombatManager.I.UseAction(a, item); return; }
            // exploration use
            if (!RulesEngine.CanUse(c, a, false, out var why)) { Toast.Show(why, Theme.Failure); Audio.Sfx.Play("error"); return; }
            if (c.actor == null || c.hp <= 0) return;
            Targeting.I.Begin(c, a, false, ctx =>
            {
                var hostileTarget = ctx.targets.FirstOrDefault(t => t != null && t != c && (t.faction == Faction.Hostile || (a.who == Who.Enemy && t.faction == Faction.Neutral)));
                if (hostileTarget != null && hostileTarget.actor != null)
                {
                    // attacking out of combat starts combat; the opening strike lands first
                    CombatManager.I.StartCoroutine(OpeningStrike(ctx, hostileTarget));
                    return;
                }
                CombatManager.I.StartCoroutine(ExploreExecute(ctx));
            }, null, item);
        }

        System.Collections.IEnumerator ExploreExecute(ActionContext ctx)
        {
            var u = ctx.user;
            if (ctx.targets.Count > 0 && ctx.targets[0] != u && ctx.targets[0].actor)
            {
                float range = RulesEngine.Range(u, ctx.a);
                if (RulesEngine.Dist(u, ctx.targets[0]) > range + 0.3f)
                {
                    bool arrived = false;
                    PartyController.I.Approach(u.actor, ctx.targets[0].actor.transform.position, range * 0.8f, () => arrived = true);
                    float t = 0; while (!arrived && t < 8f) { t += Time.deltaTime; yield return null; }
                }
            }
            yield return RulesEngine.Execute(ctx, false);
            Game.I.NotifyPartyChanged();
        }

        System.Collections.IEnumerator OpeningStrike(ActionContext ctx, Creature target)
        {
            if (target.faction == Faction.Neutral) target.faction = Faction.Hostile;
            yield return ExploreExecute(ctx);
            if (target.actor) CombatManager.I.StartCombat(target.actor, ctx.user.actor);
        }

        // ---------------------------------------------------------------- turn order
        void RebuildTurnOrder()
        {
            turnOrder.Clear();
            var cm = CombatManager.I;
            if (!cm.Active) return;
            int start = cm.turnIdx;
            var list = new List<Creature>();
            for (int i = 0; i < cm.order.Count; i++) { var c = cm.order[(start + i) % cm.order.Count]; if (!c.dead) list.Add(c); }
            foreach (var c in list.Take(12))
            {
                var card = UIB.El("turn-card", turnOrder);
                if (c == cm.Current) card.AddToClassList("turn-card-current");
                Color col = c.isPC ? Theme.Gold : c.faction == Faction.Hostile ? Theme.Hostile : Theme.Friendly;
                UIB.Border(card, col * (c == cm.Current ? 1f : 0.6f), c == cm.Current ? 3 : 2);
                var img = PortraitImg(c, "portrait-img", card);
                if (c.hp <= 0) img.style.unityBackgroundImageTintColor = new Color(1, .4f, .4f);
                var hpbar = UIB.El("turn-card-hp", card); hpbar.style.width = Length.Percent(Mathf.Clamp01((float)Mathf.Max(0, c.hp) / c.MaxHPTotal) * 100);
                var n = UIB.Lbl(c.name, "turn-card-name", card); n.style.color = col;
                card.RegisterCallback<MouseEnterEvent>(_ => { ShowCreatureTip(c, card); if (c.actor) c.actor.SetHighlight(col * 0.4f, 1); });
                card.RegisterCallback<MouseLeaveEvent>(_ => { HideTip(); if (c.actor) c.actor.SetHighlight(Color.black, 0); });
                card.RegisterCallback<ClickEvent>(_ => { if (c.actor) CameraRig.I.Focus(c.actor.transform.position); });
            }
        }

        // ---------------------------------------------------------------- tooltips
        void Place(VisualElement anchor, bool above = true)
        {
            tooltip.style.display = DisplayStyle.Flex;
            var wb = anchor.worldBound;
            float w = 380, rootW = root.layout.width, rootH = root.layout.height;
            float left = Mathf.Clamp(wb.center.x - w / 2, 8, rootW - w - 8);
            tooltip.style.left = left;
            tooltip.style.top = StyleKeyword.Auto; tooltip.style.bottom = StyleKeyword.Auto;
            if (above) tooltip.style.bottom = rootH - wb.yMin + 8;
            else { tooltip.style.top = wb.yMin; tooltip.style.left = wb.xMax + 10; }
        }

        void ShowSimpleTip(string text, VisualElement anchor)
        {
            tooltip.Clear();
            UIB.Lbl(text, "tt-body", tooltip);
            tooltip.style.width = StyleKeyword.Auto;
            Place(anchor, anchor.worldBound.y > 300);
        }

        public void HideTip() { tooltip.style.display = DisplayStyle.None; tooltip.style.width = 380; }

        void ShowActionTip(Creature c, ActionDef a, ItemStack item, VisualElement anchor, string why)
        {
            tooltip.Clear();
            tooltip.style.width = 380;
            var t = UIB.Lbl(item != null ? item.Def.name : a.name, "tt-title", tooltip); Theme.ApplyFont(t, true);
            string sub = a.CostLabel();
            if (a.IsSpell) sub += " · " + a.LevelLabel();
            if (a.concentration) sub += " · Concentration";
            if (a.weaponAction) sub += " · Weapon Action (short rest)";
            UIB.Lbl(sub, "tt-sub", tooltip);
            var stats = new List<string>();
            float range = RulesEngine.Range(c, a);
            if (a.target != TargetKind.Self) stats.Add(a.target == TargetKind.Cone ? $"Cone: {a.radius:F1}m" : a.target == TargetKind.Line ? $"Line: {a.radius:F0}m" : range <= 1.6f ? "Range: Melee" : $"Range: {range:F0}m");
            if (a.radius > 0 && a.target == TargetKind.Point) stats.Add($"Radius: {a.radius:F1}m");
            if (a.IsAttack && !a.autoHit) stats.Add($"Attack: +{RulesEngine.AttackBonus(c, a)}");
            if (a.hasSave) stats.Add($"{a.saveAb} save DC {RulesEngine.SaveDC(c, a)}");
            string dmg = DamageText(c, a);
            if (!string.IsNullOrEmpty(dmg)) stats.Add(dmg);
            if (!string.IsNullOrEmpty(a.heal)) stats.Add($"Heals {a.heal}{(a.healAddMod ? $"+{c.SpellMod}" : "")}");
            if (a.maxUses > 0) stats.Add($"Uses: {c.UsesLeft(a)}/{a.maxUses} per {(a.recharge == Recharge.ShortRest ? "short" : "long")} rest");
            if (!string.IsNullOrEmpty(a.resource)) stats.Add($"Costs {a.resourceCost} {RulesEngine.ResourceName(a.resource)}");
            foreach (var s in stats) UIB.Lbl(s, "tt-stat", tooltip);
            var d = UIB.Lbl(item != null ? item.Def.desc : a.desc, "tt-body", tooltip); d.style.marginTop = 6;
            if (!string.IsNullOrEmpty(why)) { var w = UIB.Lbl(why, "tt-body bad", tooltip); w.style.marginTop = 6; }
            Place(anchor);
        }

        public static string DamageText(Creature c, ActionDef a)
        {
            if (a.usesWeapon)
            {
                var w = RulesEngine.WeaponFor(c, a);
                if (w == null) return null;
                int mod = a.addMod ? c.Mod(c.WeaponAbility(w)) : 0;
                if (c.Has(Cond.Raging) && !w.IsRanged) mod += 2;
                string dice = c.eq.off == null && !string.IsNullOrEmpty(w.versatile) ? w.versatile : w.dmg;
                var de = DiceExpr.Parse(dice).WithBonus(mod + w.magic);
                string s = $"Damage: {de} {w.dtype}";
                if (!string.IsNullOrEmpty(w.extraDmg)) s += $" + {w.extraDmg} {w.extraType}";
                if (!string.IsNullOrEmpty(a.dmg2)) s += $" + {a.dmg2}";
                if (c.SneakDice > 0 && (w.Has(WeaponProps.Finesse) || w.IsRanged)) s += $"\n<color=#b0a090>Sneak Attack +{c.SneakDice}d6 when eligible</color>";
                return s;
            }
            if (string.IsNullOrEmpty(a.dmg)) return null;
            var d = DiceExpr.Parse(a.dmg);
            if (a.cantripScales && c.level >= 5) d.count *= 2;
            int flat = a.addMod ? c.SpellMod : 0;
            if (a.special == "eldritch_blast" && c.IsClass(ClassId.Warlock) && c.level >= 2) flat += c.Mod(Ability.CHA);
            if (a.id == "unarmed_strike" || a.special == "martial_arts") { d = DiceExpr.Parse(c.IsClass(ClassId.Monk) ? (c.level >= 5 ? "1d6" : "1d4") : "1"); flat = c.Mod(c.IsClass(ClassId.Monk) && c.Mod(Ability.DEX) > c.Mod(Ability.STR) ? Ability.DEX : Ability.STR); }
            d = d.WithBonus(flat);
            string res = $"Damage: {d} {a.dtype}";
            if (a.targets > 1 && !a.distinctTargets) res += $" ×{a.targets}";
            if (!string.IsNullOrEmpty(a.upcast)) res += $" (+{a.upcast} per higher slot)";
            return res;
        }

        void ShowCreatureTip(Creature c, VisualElement anchor)
        {
            tooltip.Clear(); tooltip.style.width = 330;
            var t = UIB.Lbl(c.name, "tt-title", tooltip); Theme.ApplyFont(t, true);
            UIB.Lbl(c.isPC ? $"Level {c.level} {c.sheet.Title}" : $"{c.type} · {c.size}", "tt-sub", tooltip);
            UIB.Lbl($"HP {Mathf.Max(0, c.hp)}/{c.MaxHPTotal}   AC {c.AC}   Speed {c.Speed:F1}m", "tt-stat", tooltip);
            if (c.hp <= 0 && c.isPC && !c.dead) UIB.Lbl($"Death saves: {c.deathSucc} ✓  {c.deathFail} ✗", "tt-stat bad", tooltip);
            foreach (var cd in c.conds) UIB.Lbl($"<color={Theme.Hex(Conditions.Get(cd.id).color)}>{cd.id.Nice()}</color>{(cd.turns > 0 ? $" ({cd.turns})" : "")}: <size=85%>{Conditions.Get(cd.id).desc}</size>", "tt-body", tooltip);
            if (!c.isPC && c.mdef != null && !string.IsNullOrEmpty(c.mdef.desc)) { var d = UIB.Lbl(c.mdef.desc, "tt-body dim", tooltip); d.style.marginTop = 4; }
            Place(anchor, false);
        }
    }
}
