using System.Collections;
using System.Linq;
using Dungine.Dialogue;
using Dungine.Rules;
using Dungine.UI;
using Dungine.Visual;
using UnityEngine;
using static Dungine.Dialogue.Dialogues;

namespace Dungine
{
    public static partial class Campaign
    {
        static partial void MoreDialoguesImpl()
        {
            ManorDialogues();
            ChurchDialogues();
            DeathHouseDialogues();
            TserDialogues();
        }

        static void ManorDialogues()
        {
            // ------------------------------------------------------------------ Ireena
            new DB("ireena")
                .Start(() => F("met_ireena") ? (F("mansion_attack_won") ? "after" : "again") : "start")
                .Node("start", "Ireena Kolyanovna", "*A young woman in a dark red dress turns from the coffin, one hand already on the hilt of a rapier she keeps within reach.* ...You're the ones Ismark found. He said you had kind faces. *She studies you.* He's a poor judge of faces.")
                    .Enter(() => G.SetFlag("met_ireena"))
                    .Opt("We're sorry about your father.", "sorry")
                    .Opt("Your brother asked us to take you to Vallaki.", "vallaki")
                    .Opt("[MEDICINE] Those marks on your neck — may I look?", null).Check(Skill.Medicine, 12, "neck_ok", "neck_no")
                .Node("sorry", "Ireena Kolyanovna", "He was a good man who was never quite brave enough, in a place that asked nothing but bravery of him. *She touches the edge of the coffin.* He kept the house standing. Every night. That was his whole war, and he won it until he didn't.")
                    .Then("vallaki")
                .Node("neck_ok", "Ireena Kolyanovna", "*She lets you draw aside her collar. Two small wounds, weeks old and not healing — the skin around them is cold to the touch and oddly smooth, as if it has already decided to belong to someone else.* He came in through a window I had nailed shut. I remember opening it. I don't remember deciding to.")
                    .Enter(() => G.journal.AddLore("Ireena bears two bite wounds on her neck that do not heal. She remembers opening a nailed window, but not deciding to."))
                    .Then("vallaki")
                .Node("neck_no", "Ireena Kolyanovna", "*She steps back and pulls her collar closed.* No. Not yet. I don't know you.")
                    .Then("vallaki")
                .Node("vallaki", "Ireena Kolyanovna", "I'll go. Ismark is right; I'm only bait here. But I won't leave my father lying in the parlour for the rats. He goes into the ground first, properly, with the rites. And the only priest in the village has barricaded himself in his church.")
                    .Opt("Then we'll go and talk to the priest.", "priest")
                    .Opt("Is there anything in the house we could use?", "house")
                .Node("house", "Ireena Kolyanovna", "Father's strongbox is in the study. *She presses a heavy iron key into your hand.* Whatever Ismark promised you is in there. Take it — it's no use to the dead.")
                    .Enter(() => { if (!G.stash.Has("key_mansion")) { G.stash.Add("key_mansion"); Audio.Sfx.Play("pickup"); Toast.Show("Received: Burgomaster's Key", Theme.Gold); } })
                    .Then("priest")
                .Node("priest", "Ireena Kolyanovna", "Father Donavich. The church is on the rise at the east end of the village, past the square. People say he hasn't come out since his son vanished. *She hesitates.* And they say that at night, the church screams.")
                    .Enter(() =>
                    {
                        Journal("burgomaster", "The Burgomaster's Daughter", "Ireena will leave for Vallaki — but only after her father is buried with proper rites. Father Donavich, the village priest, has locked himself inside his church on the east side of the village. We should persuade him to perform the burial.");
                        if (!G.stash.Has("key_mansion")) G.stash.Add("key_mansion");
                    })
                    .Opt("We'll bring him.", "rest")
                .Node("rest", "Ireena Kolyanovna", "Stay here tonight if you need to — the hearth in the parlour is warm, and this house is the one place in Barovia he can't simply walk into. *A small, bleak smile.* Mostly.")
                    .End()
                .Node("again", "Ireena Kolyanovna", "Any word from the church?")
                    .Opt("Not yet.", "priest")
                    .Opt("Tell us about the one who bit you.", "strahd")
                    .Opt("Goodbye.")
                .Node("strahd", "Ireena Kolyanovna", "He's courteous. That's the worst of it. He asks after my health. He apologises for the cold. And when he looks at me he isn't seeing me — he's looking at someone behind my face, someone he's been waiting for, for a very long time.")
                    .Then("again")
                .Node("after", "Ireena Kolyanovna", "*She's shaking, but her rapier is steady.* You held the house. Father did that for a hundred nights by himself. I think... I think he'd have liked you.")
                    .Opt("Any word from the church?", "priest")
                    .Opt("Goodbye.");

            new DB("ireena_after_attack")
                .Node("start", "Ireena Kolyanovna", "*She comes down the stairs with her rapier drawn, and looks at the broken shutters, the bodies, you.* They've never come so many at once. He sent them because you're here. *She wipes her blade on a curtain.* Good. Let him worry.")
                    .End();

            new DB("ismark_home")
                .Node("start", "Ismark Kolyanovich", "You came. Good. *He glances at his sister and lowers his voice.* She won't leave until Father is buried. I've tried. You'll have better luck with the priest than I did — he threw a candlestick at me.")
                    .Opt("We'll talk to him.")
                    .End();

            // ------------------------------------------------------------------ the burial at the graveyard
            new DB("burial")
                .Node("start", "Father Donavich", "*The priest stands at the head of the grave with his book open, though he isn't reading from it. His voice is hoarse.* Morninglord, who rises when all hope has set — receive Kolyan Indirovich, who kept his lamp lit. He did not see your dawn. Let him see it now.")
                    .Then("ireena")
                .Node("ireena", "Ireena Kolyanovna", "*She drops a handful of earth onto the coffin, and then a sprig of dried garlic.* Goodbye, Papa. I'm going. Like you wanted. *She doesn't cry. Ismark does, silently, and doesn't try to hide it.*")
                    .Actor("ireena_g")
                    .Then("cold")
                .Node("cold", "", "*The candles by the grave gutter all at once. Mist pours over the churchyard wall like milk, and in it, where there was no one, a tall figure in a black cloak is standing among the headstones with his hands folded behind his back.*")
                    .Enter(() => Game.I.StartCoroutine(StrahdAppears()))
                    .Actor("strahd")
                    .Then("strahd1")
                .Node("strahd1", "The Stranger", "Forgive the intrusion. I would not miss the burial of a man who served me for so many years — however reluctantly. *His voice is warm, cultured, and entirely without hurry.* My condolences, Ireena. You know you need only ask, and your grief would end tonight.")
                    .Actor("strahd")
                    .Then("ire2")
                .Node("ire2", "Ireena Kolyanovna", "*Her hand is white on her rapier.* Leave. You have no right to be here.")
                    .Actor("ireena_g")
                    .Then("strahd2")
                .Node("strahd2", "The Stranger", "I have every right to be everywhere in this valley. *He turns his attention to you, and the air grows colder by degrees.* And you. Guests. I have been expecting guests. I trust my village has been hospitable?")
                    .Actor("strahd")
                    .Opt("Who are you?", "who")
                    .Opt("Stay away from her.", "threat")
                    .Opt("[INSIGHT] What does he actually want?", null).Check(Skill.Insight, 15, "insight_ok", "insight_no")
                    .Opt("*Say nothing, and step in front of Ireena.*", "stand")
                .Node("who", "Strahd von Zarovich", "Strahd von Zarovich. Count, lord, landowner. Your host. *A slight, precise bow.* Everything you have eaten here grew on my land; every road you have walked was laid by my ancestors. I mention it only so that we understand one another.")
                    .Actor("strahd")
                    .Then("strahd3")
                .Node("threat", "Strahd von Zarovich", "*He smiles, and you see that his canines are too long.* Such conviction. I have been told to stay away from things by kings. I outlived them, and their kingdoms, and the names of their kingdoms.")
                    .Actor("strahd")
                    .Then("strahd3")
                .Node("insight_ok", "Strahd von Zarovich", "*Watching his eyes, you realise he has barely looked at you. He looks at Ireena the way a man looks at a letter he has been waiting centuries to open — not with hunger, but with a terrible, patient longing.*")
                    .Actor("strahd")
                    .Enter(() => G.journal.AddLore("At the burial, Strahd looked at Ireena not with hunger but with something like longing — as if he saw someone else in her face."))
                    .Then("strahd3")
                .Node("insight_no", "Strahd von Zarovich", "*His face is a courteous mask. You find you cannot hold his gaze for long.*")
                    .Actor("strahd")
                    .Then("strahd3")
                .Node("stand", "Strahd von Zarovich", "*He regards you for a long, cold moment, and inclines his head, as if you had said something rather witty.* Loyal. How refreshing.")
                    .Actor("strahd")
                    .Then("strahd3")
                .Node("strahd3", "Strahd von Zarovich", "Take her to Vallaki, then. Take her wherever you like. The mists will be kind to you on the road — I shall see to it. *He steps back into the fog, and it folds around him like a cloak.* We will speak again. I am told I am a very good host.")
                    .Actor("strahd")
                    .Enter(() => Game.I.StartCoroutine(StrahdVanishes()))
                    .Then("after")
                .Node("after", "Ismark Kolyanovich", "*Nobody speaks for a long time. Finally Ismark picks up the spade.* ...Help me fill it in. And then we go. Tonight.")
                    .Actor("ismark_g")
                    .Enter(FinishBurial)
                    .End();
        }

        static void ChurchDialogues()
        {
            // ------------------------------------------------------------------ Father Donavich
            new DB("donavich")
                .Start(() => G.party.Any(c => c.dead) && F("met_donavich") ? "raise" : F("doru_dead") ? (F("burial_ready") || F("kolyan_buried") ? "done" : "grief") : F("met_donavich") ? "again" : "start")
                .Node("raise", "Father Donavich", "*He looks at the body you have carried in, and something in his face hardens into purpose.* Lay them before the altar. The Morninglord is silent in this valley — but not deaf. Not yet. It will cost the church a hundred gold in candles and oils, and me a night's prayer.")
                    .Opt("Please. Here is the gold. (100 gold)", "raised").If(() => G.gold >= 100)
                    .Opt("We can't pay that.", "cantpay")
                    .Opt("Not now.")
                .Node("raised", "Father Donavich", "*He prays until his voice gives out. Near dawn — or what passes for dawn here — the fallen draws a ragged breath, and then another.*")
                    .Enter(() =>
                    {
                        G.gold -= 100;
                        foreach (var c in G.party.Where(c => c.dead)) { Combat.RulesEngine.Revive(c, 1); c.hp = Mathf.Max(1, c.MaxHPTotal / 2); }
                        Audio.Sfx.Play("revive"); Toast.Show("Your fallen companion lives again.", Theme.Gold);
                        G.NotifyPartyChanged();
                    })
                    .End()
                .Node("cantpay", "Father Donavich", "*He hesitates, then shakes his head wearily.* ...Then bring me what you can when you can. I'll do it now. The dead in this valley have waited long enough.")
                    .Opt("Thank you, Father.", "raised_free")
                .Node("raised_free", "Father Donavich", "*He prays until his voice gives out, and the fallen draws breath again.*")
                    .Enter(() =>
                    {
                        G.gold = Mathf.Max(0, G.gold - 100);
                        foreach (var c in G.party.Where(c => c.dead)) { Combat.RulesEngine.Revive(c, 1); c.hp = Mathf.Max(1, c.MaxHPTotal / 2); }
                        Audio.Sfx.Play("revive"); G.NotifyPartyChanged();
                    })
                    .End()
                .Node("start", "Father Donavich", "*The priest is kneeling at the altar with his forehead against the stone. He doesn't turn.* The church is closed. The church is closed. Go home, bar your doors, and do not come here at night.")
                    .Enter(() => G.SetFlag("met_donavich"))
                    .Opt("We need a priest. The burgomaster is dead.", "burgo")
                    .Opt("What's under the floor, Father?", "floor")
                .Node("burgo", "Father Donavich", "*He finally looks at you — an old man, grey-stubbled, eyes red-rimmed from nights without sleep.* Kolyan. Yes. I heard. I would bury him, I would, but I cannot leave this church. If I leave it, I will not be here to keep the door shut.")
                    .Then("floor")
                .Node("floor", "Father Donavich", "*From beneath your feet, muffled by stone and wood, someone screams: FATHER! FATHER, I'M SO HUNGRY! The priest shuts his eyes.*  My son. Doru. He went with the other young fools to storm the castle, a month ago. He came back three nights ago. He came back... changed.")
                    .Enter(() => Audio.Sfx.Play("whisper", 1f, 0.5f))
                    .Opt("He's a vampire.", "vamp")
                    .Opt("[RELIGION] There are rites for this. Let us help you.", null).Check(Skill.Religion, 12, "rites", "vamp")
                .Node("vamp", "Father Donavich", "He is my son. *His voice breaks.* He is my son and he begs me for blood every night and every night I do not give it and every night he hates me more. The dawn does not come to Barovia. It will never burn him clean. What am I supposed to do?")
                    .Opt("We can end his suffering. Give us the key.", "key")
                    .Opt("[PERSUASION] Bury Kolyan first. Doru will still be there tomorrow.", null).Check(Skill.Persuasion, 16, "persuaded", "refused")
                    .Opt("We'll think about it.", "later")
                .Node("rites", "Father Donavich", "*Something like hope crosses his face, and dies there.* I have read them all. The rites for the restless dead ask the dead to rest. He does not want to rest. He wants to eat. *He looks at the trapdoor.* There is only one rite left.")
                    .Then("key")
                .Node("key", "Father Donavich", "*For a long time he doesn't move. Then he takes a key from around his neck — iron, warm from his skin — and holds it out without looking at you.* Quickly. Please. Don't let him speak to you; he will use my voice. And when it's done... tell me he didn't suffer. Even if it isn't true.")
                    .Enter(() =>
                    {
                        if (!G.stash.Has("key_undercroft")) { G.stash.Add("key_undercroft"); Toast.Show("Received: Iron Undercroft Key", Theme.Gold); Audio.Sfx.Play("pickup"); }
                        Journal("doru", "The Priest's Son", "Father Donavich's son Doru came back from Castle Ravenloft as a vampire spawn. The priest has locked him in the undercroft beneath the church and cannot bring himself to act. He gave us the key.");
                    })
                    .End()
                .Node("persuaded", "Father Donavich", "*He stares at you, then at the trapdoor, then at his own shaking hands.* ...Yes. Yes. A burial. A clean thing, with earth and words. I can do that. I can still do that. Tell Ismark I will come to the graveyard. *He locks the trapdoor twice over before he goes.*")
                    .Enter(() => { G.SetFlag("burial_ready"); G.SetFlag("donavich_persuaded"); Journal("burgomaster", "The Burgomaster's Daughter", "Father Donavich has agreed to bury Kolyan. Ismark, Ireena and the priest will meet us at the graveyard behind the church."); G.GiveXP(75, "a priest's promise"); })
                    .End()
                .Node("refused", "Father Donavich", "*He turns back to the altar.* No. No, I can't. Not while he's down there. Go away. Please go away.")
                    .End()
                .Node("later", "Father Donavich", "Think quickly. He is louder every night.")
                    .End()
                .Node("again", "Father Donavich", "*He hasn't moved from the altar.* Is it done? ...No. I can see that it isn't.")
                    .Opt("Give us the key, Father.", "key").If(() => !G.stash.Has("key_undercroft"))
                    .Opt("[PERSUASION] Bury Kolyan first. Doru will keep.", null).If(() => !F("burial_ready")).Check(Skill.Persuasion, 16, "persuaded", "refused")
                    .Opt("Not yet.", "later")
                .Node("grief", "Father Donavich", "*He knows before you say it. He has heard the silence under the floor.* ...Did he suffer?")
                    .Opt("No. It was quick.", "grief2")
                    .Opt("He asked for you at the end.", "grief2")
                    .Opt("Yes.", "grief_truth")
                .Node("grief_truth", "Father Donavich", "*He nods slowly, as if you had confirmed a diagnosis.* Thank you for not lying to me. Everyone lies to priests.")
                    .Then("grief3")
                .Node("grief2", "Father Donavich", "*He closes his eyes, and something in his shoulders lets go for the first time in a month.* Thank you.")
                    .Then("grief3")
                .Node("grief3", "Father Donavich", "Kolyan. Yes. I'll bury him — and my son beside him, in the morning, if there is one. Take this. It was blessed by my order when there still was an order. It should be carried by someone who can still use it. *He presses a heavy mace into your hands, its head worked into a rayed sun.* Meet us in the churchyard.")
                    .Enter(() =>
                    {
                        G.SetFlag("burial_ready");
                        G.stash.Add("mace_morninglord"); Audio.Sfx.Play("quest_done");
                        Toast.Show("Received: Mace of the Morninglord", Theme.Gold);
                        Journal("doru", "The Priest's Son", "We put Doru to rest in the undercroft. Father Donavich gave us his order's blessed mace in thanks.", QuestState.Done);
                        Journal("burgomaster", "The Burgomaster's Daughter", "Father Donavich has agreed to bury Kolyan. Ismark, Ireena and the priest will meet us at the open grave in the churchyard.");
                        G.GiveXP(100, "the priest's son");
                    })
                    .End()
                .Node("done", "Father Donavich", "Go with the Morninglord. What little of Him is left here.")
                    .End();

            // ------------------------------------------------------------------ Doru
            new DB("doru")
                .Node("start", "Doru", "*A young man is crouched in the corner of the cellar with his back to you, very still. His voice, when it comes, is his father's voice exactly.* Father? Father, is that you? Did you bring me something? *He sniffs the air, and turns, and his eyes catch the candlelight red.* ...Oh. You're not Father. You smell much better than Father.")
                    .Opt("Doru. Your father sent us.", "sent")
                    .Opt("[PERSUASION] You don't have to do this. Let us help you.", null).Check(Skill.Persuasion, 18, "calm", "fight")
                    .Opt("*Draw your weapon.*", "fight")
                .Node("sent", "Doru", "Did he. *He laughs, and the laugh goes wrong halfway through.* To bring me dinner? He always said I'd come to a bad end. I climbed the castle wall, you know. I was going to kill the devil. I got so close. And then he asked me to stay for supper.")
                    .Then("fight")
                .Node("calm", "Doru", "*He hesitates — and for a moment he's just a frightened boy in a cellar.* ...It hurts. All the time. Tell him I'm sorry. Tell him— *then his face twists, and the hunger closes over him like water.* NO. No, I'm so HUNGRY—")
                    .Enter(() => G.journal.AddLore("For a moment, Doru remembered himself. He asked us to tell his father he was sorry."))
                    .Then("fight")
                .Node("fight", "Doru", "*He comes at you across the ceiling.*")
                    .Enter(() => Game.I.StartCoroutine(DoruFight()))
                    .End();
        }

        static IEnumerator DoruFight()
        {
            yield return null;
            var d = Game.I.FindActor("doru");
            if (!d) yield break;
            d.c.faction = Faction.Hostile; d.talkable = false;
            while (Dialogue.DialogueRunner.I.Active) yield return null;
            Combat.CombatManager.I.StartCombat(d, null);
            yield return WaitForCombat();
            if (d == null || d.c.dead)
            {
                Game.I.SetFlag("doru_dead");
                Game.I.journal.Update("doru", "The Priest's Son", "Doru is dead — truly dead, this time. We should tell his father.");
            }
        }

        // ------------------------------------------------------------------ burial staging

        static IEnumerator StrahdAppears()
        {
            var g = Game.I;
            var ctx = g.area;
            var gp = ctx.markers.TryGetValue("grave", out var m) ? m.position : AreaVillage.GravePos;
            var sp = ctx.G3(gp.x - 9f, gp.z + 7f);
            Audio.Sfx.Play("bell_toll", 0.8f, 0.5f);
            Audio.AudioSys.I.PlayMusic("strahd");
            Atmosphere.I.Apply(Atmosphere.Dusk);
            var fog = World.Kit.FogBank(ctx.root, sp + Vector3.up * 0.5f, new Vector3(14, 2, 14), new Color(.8f, .8f, .85f, .45f), 60, 6f, 0.2f);
            FX.Burst(sp + Vector3.up, FxKind.Necrotic, 2f);
            var s = ctx.NPC("strahd", "The Stranger", Monsters.Get("strahd").look, Monsters.Get("strahd").gear, sp, 0, null, MotionStyle.Noble);
            if (s)
            {
                s.Face(gp, true);
                s.agent.enabled = false;
            }
            yield break;
        }

        static IEnumerator StrahdVanishes()
        {
            yield return new WaitForSeconds(2.5f);
            var s = Game.I.FindActor("strahd");
            if (s)
            {
                FX.Burst(s.Chest, FxKind.Necrotic, 2.5f);
                Audio.Sfx.Play("teleport", 1f, 0.6f);
                Object.Destroy(s.gameObject);
            }
        }

        static void FinishBurial()
        {
            var g = Game.I;
            g.SetFlag("kolyan_buried"); g.SetFlag("ireena_escort");
            Atmosphere.I.Apply(Atmosphere.BaroviaDay);
            Audio.AudioSys.I.PlayMusic("explore");
            g.journal.Update("burgomaster", "The Burgomaster's Daughter", "Kolyan Indirovich is buried. Strahd von Zarovich himself came to the graveside, and made it clear that he considers Ireena his. She will travel with us west, toward the Vistani at Tser Pool and the walls of Vallaki. The road west leaves the village past the manor.");
            g.GiveXP(150, "the burgomaster's burial");
            Toast.Show("Ireena will now travel with you. Leave the village by the western road when you are ready.", Theme.Gold, 6f);
        }
    }
}
