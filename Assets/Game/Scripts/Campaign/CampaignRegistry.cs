using System.Collections;
using System.Linq;
using Dungine.Rules;
using Dungine.UI;
using Dungine.World;
using UnityEngine;

namespace Dungine
{
    public static partial class Campaign
    {
        static partial void RegisterAreasImpl()
        {
            AreaLoader.Register(new AreaSvalichRoad());
            AreaLoader.Register(new AreaVillage());
            AreaLoader.Register(new AreaTavern());
            AreaLoader.Register(new AreaShop());
            RegisterMoreAreas();
        }

        static partial void RegisterDialoguesImpl()
        {
            VillageDialogues();
            MoreDialogues();
        }

        static partial void RegisterMoreAreasImpl();
        static void RegisterMoreAreas() => RegisterMoreAreasImpl();
        static partial void MoreDialoguesImpl();
        static void MoreDialogues() => MoreDialoguesImpl();

        static partial void ItemTakenImpl(string id)
        {
            var g = Game.I;
            switch (id)
            {
                case "courier_note":
                    if (!g.Flag("read_courier")) { g.SetFlag("read_courier"); g.journal.AddLore("A dead wagon driver on the Svalich road was carrying goods for Bildrath's Mercantile in the Village of Barovia."); }
                    break;
                case "children_bones":
                    g.journal.Update("deathhouse", "The House on Mad Mary's Lane", "We found the remains of two children in the attic of the Durst house — Rose and Thorn, starved and forgotten. Their parents' cult sealed them up there. The monster they spoke of waits below.");
                    break;
                case "dh_deed":
                case "dh_journal":
                    g.journal.AddLore("The Dursts belonged to a cult that worshipped something beneath their house. The children were never allowed in the basement.");
                    break;
            }
        }

        static partial void OnLongRestedImpl()
        {
            var g = Game.I;
            g.AddFlag("rests");
            OnRestStory();
        }

        static partial void OnRestStoryImpl();
        static void OnRestStory() => OnRestStoryImpl();

        // ------------------------------------------------------------------ the end of the chapter

        public static void EndChapter() { if (!Game.I.busy) Game.I.StartCoroutine(EndChapterRoutine()); }

        static IEnumerator EndChapterRoutine()
        {
            var g = Game.I;
            g.busy = true;
            g.mode = GameMode.Cutscene;
            g.SetFlag("chapter1_done");
            g.journal.Update("burgomaster", "The Burgomaster's Daughter", "With Kolyan Indirovich buried, we brought Ireena west to the Vistani camp at Tser Pool, where Madam Eva read our fortune. In the morning we set out for the walled town of Vallaki. Behind us, a light burned in the highest window of Castle Ravenloft.", QuestState.Done);
            yield return UIRoot.I.FadeOut(2f, null, null);
            Audio.AudioSys.I.PlayMusic("strahd");
            yield return UIRoot.I.Narration(new[]
            {
                "That night you sleep beside the Vistani fire, with Ireena wrapped in her father's cloak and the fiddles playing long after the stars come out — as if music could keep the dark at the edge of the firelight. For one night, it does.",
                "Madam Eva does not sleep. She sits at the door of her tent and watches the castle on its crag, and turns one card over and over in her fingers without ever looking at its face.",
                "High on its pillar of stone, in a castle that has never known a sunrise, a figure stands at a window and watches a small fire burn beside a black pool.\n\nHe is in no hurry. He never has been.",
                "In the morning the road runs west toward Vallaki, a town that still believes walls can keep things out.\n\nThe valley is patient. So, you are learning, must you be."
            }, "END OF CHAPTER ONE");
            yield return UIRoot.I.ChapterSummary();
            g.busy = false;
            SaveSystem.Autosave();
            g.GoToMainMenu();
        }
    }
}
