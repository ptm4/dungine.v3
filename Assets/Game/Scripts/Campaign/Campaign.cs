using System.Linq;
using Dungine.Rules;
using Dungine.UI;
using Dungine.World;
using UnityEngine;

namespace Dungine
{
    /// <summary>Campaign glue: registers areas and dialogues, rests, texts.</summary>
    public static partial class Campaign
    {
        public static string IntroTitle => "THE MISTS";
        public static string[] IntroText => new[]
        {
            "The letter found you three nights ago, pinned beneath a tankard in a roadside inn by a hand no one saw.",
            "It spoke of a village beneath a castle, of a burgomaster's daughter bitten in the night, of wealth for anyone brave enough to come.",
            "You took the eastern road into the forest. By the second evening the birds had stopped singing.\n\nOn the third, the mist came.",
            "It rose from the ground like breath on a cold morning, thick and grey and patient. When it thinned again, the road behind you was gone.\n\nAhead, through black pines, the path wound on toward a valley called Barovia."
        };

        public static string ControlsText =>
            "EXPLORATION\n" +
            "Left-click ground — move the selected character (the party follows)\n" +
            "Left-click a person — talk · an object — use or search · an enemy — attack\n" +
            "Right-click a creature — examine\n" +
            "W A S D — pan the camera · Q / E or middle mouse — rotate · Scroll — zoom · Home / F — recentre\n" +
            "F1–F3 or click a portrait — select a character\n" +
            "Tab (hold) — highlight things you can interact with\n" +
            "1–0 — hotbar · I — inventory · C — character sheet · J — journal · Esc — menu\n" +
            "F5 — quicksave · F9 — quickload\n\n" +
            "COMBAT\n" +
            "Each character has an Action (green), a Bonus Action (orange), a Reaction (purple) and movement.\n" +
            "Left-click ground to move; hover an enemy to see your chance to hit, click to attack.\n" +
            "Choose spells and abilities from the hotbar, then pick targets. Right-click cancels.\n" +
            "Space — end turn\n\n" +
            "Moving away from an adjacent enemy provokes an opportunity attack unless you Disengage.\n" +
            "Downed allies make death saves each turn; use Help or a healing spell to get them up.";

        public static string CreditsText =>
            "DUNGINE II — THE CURSE OF STRAHD\n\n" +
            "A fan-made, turn-based adventure in the style of Baldur's Gate 3, set in the Curse of Strahd campaign, made for private play among friends.\n\n" +
            "Every mesh, texture, character, sound and piece of music in this game is generated procedurally by its own code at runtime.\n\n" +
            "Dungeons & Dragons, Curse of Strahd, Barovia and Strahd von Zarovich are property of Wizards of the Coast. Baldur's Gate 3 is property of Larian Studios. This project is not affiliated with or endorsed by either, and is not for sale or public distribution.\n\n" +
            "Built with Unity " + Application.unityVersion + ".";

        static bool registered;
        public static void RegisterAll()
        {
            if (registered) return;
            registered = true;
            RegisterAreas();
            RegisterDialogues();
        }

        static partial void RegisterAreasImpl();
        static partial void RegisterDialoguesImpl();
        static void RegisterAreas() => RegisterAreasImpl();
        static void RegisterDialogues() => RegisterDialoguesImpl();

        public static void OnItemTaken(string id) => ItemTaken(id);
        static partial void ItemTakenImpl(string id);
        static void ItemTaken(string id) => ItemTakenImpl(id);

        public static bool CanLongRest(out string where)
        {
            where = null;
            var g = Game.I;
            if (g.area == null) return false;
            var spot = g.area.interactables.OfType<RestSpot>().FirstOrDefault();
            if (spot != null) { where = spot.label; return true; }
            if (g.area.def.RestUnsafe || g.mode == GameMode.Combat) return false;
            where = g.area.def.Interior ? "a quiet corner" : "a makeshift camp";
            return true;
        }

        public static void DoLongRest() => Game.I.StartCoroutine(LongRestRoutine());

        static System.Collections.IEnumerator LongRestRoutine()
        {
            var g = Game.I;
            yield return UIRoot.I.FadeOut(1f, "The Night Passes", "Your wounds close. Your minds clear. The mists do not lift.");
            g.LongRest();
            OnLongRested();
            if (restInterrupted) { restInterrupted = false; yield break; }   // a story event took over the night
            Audio.Sfx.Play("rest");
            yield return new WaitForSecondsRealtime(1.5f);
            yield return UIRoot.I.FadeIn(1f);
            Toast.Show("Long rest complete: hit points, spell slots and abilities restored.", Theme.Success, 3f);
            SaveSystem.Autosave();
        }

        /// <summary>Set by a rest-time story event (the night attack) so the normal "morning" doesn't play over it.</summary>
        public static bool restInterrupted;

        /// <summary>Waits for a combat that has just been started to begin and then to finish.</summary>
        public static System.Collections.IEnumerator WaitForCombat(float startTimeout = 3f)
        {
            float t = 0;
            while (!Combat.CombatManager.I.Active && t < startTimeout) { t += Time.deltaTime; yield return null; }
            while (Combat.CombatManager.I.Active) yield return null;
        }

        static partial void OnLongRestedImpl();
        static void OnLongRested() => OnLongRestedImpl();
    }
}
