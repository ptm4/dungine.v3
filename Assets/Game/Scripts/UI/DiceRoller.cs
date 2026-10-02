using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dungine.Rules;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dungine.UI
{
    /// <summary>The ability-check dice roll screen, in the style of Baldur's Gate 3.</summary>
    public static class DiceRoller
    {
        public static bool Active { get; private set; }

        public static void Roll(Creature c, Skill skill, int dc, string title, Action<bool> done, bool adv = false, bool dis = false)
            => UIRoot.I.Run(Routine(c, skill, null, dc, title, done, adv, dis));

        public static void RollAbility(Creature c, Ability ab, int dc, string title, Action<bool> done, bool adv = false)
            => UIRoot.I.Run(Routine(c, null, ab, dc, title, done, adv, false));

        static IEnumerator Routine(Creature c, Skill? skill, Ability? ab, int dc, string title, Action<bool> done, bool adv, bool dis)
        {
            Active = true;
            var layer = UIRoot.I.modalLayer;
            var modal = UIB.El("dice-modal", layer);
            var p = UIB.El("dice-panel", modal);
            var t = UIB.Lbl(title, "h3 center", p); Theme.ApplyFont(t, true);
            string what = skill.HasValue ? skill.Value.Nice() : ab.Value.Long();
            UIB.Lbl($"{c.name} — {what} check", "small dim center", p);
            var dcL = UIB.Lbl($"DIFFICULTY CLASS  {dc}", "dice-dc", p); Theme.ApplyFont(dcL, true);
            var face = UIB.El("dice-face", p);
            var die = UIB.Img(Icons.Get("dice"), null, face, new Color(.85f, .72f, .45f)); die.style.position = Position.Absolute; die.style.left = die.style.top = die.style.right = die.style.bottom = 0;
            var num = UIB.Lbl("20", "dice-number", face); Theme.ApplyFont(num, true);
            int bonus = skill.HasValue ? c.SkillBonus(skill.Value) : c.Mod(ab.Value);
            var parts = new List<string> { $"{what} {(bonus >= 0 ? "+" : "")}{bonus}" };
            int extraDie = 0; string extraLbl = null;
            var guider = Game.I.party.FirstOrDefault(pc => pc.Active && pc.knownSpells.Contains("guidance"));
            if (guider != null) { extraLbl = $"Guidance ({guider.name}) +1d4"; }
            if (c.Has(Cond.Inspired)) extraLbl = (extraLbl != null ? extraLbl + ", " : "") + "Bardic Inspiration";
            if (c.Has(Cond.Poisoned) || c.Has(Cond.Frightened)) dis = true;
            var mode = D20.Combine(adv ? 1 : 0, dis ? 1 : 0);
            UIB.Lbl(string.Join("   ", parts) + (extraLbl != null ? $"   <color=#9fd0ff>{extraLbl}</color>" : "") + (mode == RollMode.Advantage ? "   <color=#8fd88f>Advantage</color>" : mode == RollMode.Disadvantage ? "   <color=#e89a8a>Disadvantage</color>" : ""), "small center", p);
            float chance = D20.Chance(bonus + (guider != null ? 2 : 0), dc, mode, false);
            UIB.Lbl($"<color=#8a8278>{Mathf.RoundToInt(chance * 100)}% chance</color>", "tiny center", p);
            var result = UIB.Lbl("", "dice-result center", p); Theme.ApplyFont(result, true);
            var btnRow = UIB.Row(p); btnRow.style.marginTop = 12;
            bool clicked = false;
            var rollBtn = UIB.Btn("Roll", () => clicked = true, "btn btn-primary", btnRow); rollBtn.style.width = 180; rollBtn.style.height = 48;
            while (!clicked && !Keyboard(out _) && !Dialogue.DialogueRunner.DevFast) yield return null;
            rollBtn.RemoveFromHierarchy();
            bool success = false;
            int attempt = 0;
            while (true)
            {
                Audio.Sfx.Play("dice_roll");
                float tt = 0; var rnd = new System.Random();
                while (tt < 0.9f)
                {
                    tt += Time.unscaledDeltaTime;
                    num.text = rnd.Next(1, 21).ToString();
                    face.style.rotate = new Rotate(Angle.Degrees(Mathf.Sin(tt * 30) * 25 * (1 - tt / 0.9f)));
                    face.style.scale = new Scale(Vector3.one * (1 + 0.1f * Mathf.Sin(tt * 20) * (1 - tt)));
                    yield return null;
                }
                face.style.rotate = new Rotate(Angle.Degrees(0)); face.style.scale = new Scale(Vector3.one);
                var r = D20.Roll(bonus, mode, c.Race != null && c.Race.lucky);
                if (guider != null) { extraDie += Rng.D(4); }
                if (c.Has(Cond.Inspired)) { extraDie += Rng.D(c.level >= 5 ? 8 : 6); c.RemoveCond(Cond.Inspired); }
                int total = r.total + extraDie;
                num.text = r.natural.ToString();
                Audio.Sfx.Play("dice_land");
                success = r.natural == 20 || (r.natural != 1 && total >= dc);
                yield return new WaitForSecondsRealtime(0.35f);
                string nat = r.natural == 20 ? " <color=#ffd060>Natural 20!</color>" : r.natural == 1 ? " <color=#ff6050>Natural 1</color>" : "";
                result.text = $"<size=60%>{r.natural}{(r.mode != RollMode.Normal ? $" ({r.other})" : "")} {(bonus >= 0 ? "+" : "")}{bonus}{(extraDie > 0 ? $" +{extraDie}" : "")} = {total}</size>{nat}\n" + (success ? "SUCCESS" : "FAILURE");
                result.style.color = success ? Theme.Success : Theme.Failure;
                die.style.unityBackgroundImageTintColor = success ? new Color(.5f, .9f, .55f) : new Color(.95f, .45f, .4f);
                Audio.Sfx.Play(success ? "success" : "failure");
                btnRow.Clear();
                bool cont = false, reroll = false;
                if (!success && Game.I.partyInspiration > 0 && attempt == 0) UIB.Btn($"Use Inspiration to reroll ({Game.I.partyInspiration})", () => reroll = true, "btn", btnRow);
                UIB.Btn("Continue", () => cont = true, "btn btn-primary", btnRow).style.width = 180;
                while (!cont && !reroll) { if ((Keyboard(out bool esc) || Dialogue.DialogueRunner.DevFast) && !reroll) cont = true; yield return null; }
                if (reroll) { Game.I.partyInspiration--; attempt++; extraDie = 0; result.text = ""; btnRow.Clear(); die.style.unityBackgroundImageTintColor = new Color(.85f, .72f, .45f); continue; }
                break;
            }
            modal.RemoveFromHierarchy();
            Active = false;
            done?.Invoke(success);
        }

        static bool Keyboard(out bool esc)
        {
            var k = UnityEngine.InputSystem.Keyboard.current;
            esc = k != null && k.escapeKey.wasPressedThisFrame;
            return k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame);
        }
    }
}
