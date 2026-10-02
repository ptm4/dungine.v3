using System.Linq;
using Dungine.Dialogue;
using Dungine.Rules;
using Dungine.UI;
using Dungine.Visual;
using UnityEngine;
using static Dungine.Dialogue.Dialogues;

namespace Dungine
{
    /// <summary>Everyone you can talk to in the village, the tavern and the shop. All dialogue is original writing.</summary>
    public static partial class Campaign
    {
        static void Journal(string id, string title, string entry, QuestState st = QuestState.Active) => Game.I.journal.Update(id, title, entry, st);

        static void VillageDialogues()
        {
            // ------------------------------------------------------------------ Ismark
            new DB("ismark")
                .Start(() => F("met_ismark") ? "again" : "start")
                .Node("start", null, "*The big man at the corner table doesn't look up from his cup until your shadow falls across it.* Strangers. Real ones — boots still wet from the pass. Sit, if you like. Nobody else will.")
                    .Opt("Who are you?", "who").Do(() => G.SetFlag("met_ismark"))
                    .Opt("We're looking for the burgomaster. He sent for help.", "letter").If(() => G.stash.Has("letter_kolyan")).Do(() => G.SetFlag("met_ismark"))
                    .Opt("We'll leave you to your drink.")
                .Node("who", "Ismark Kolyanovich", "Ismark, son of Kolyan, the burgomaster. The village calls me Ismark the Lesser — because I have never once managed to be enough. *He drinks.* They're not wrong.")
                    .Opt("Your father sent a letter. It reached us.", "letter")
                    .Opt("Lesser than what?", "lesser")
                .Node("lesser", "Ismark Kolyanovich", "Than whatever this valley needs. Than my father. Than the thing that comes to our door at night. Take your pick.")
                    .Then("letter")
                .Node("letter", "Ismark Kolyanovich", "*He takes the letter, reads the first line, and folds it very carefully in half.* My father died three days ago. His heart. It gave out after the hundredth night of scratching at the shutters.")
                    .Opt("We're sorry.", "sorry")
                    .Opt("What was scratching at the shutters?", "what")
                    .Opt("[INSIGHT] He blames himself.", null).Check(Skill.Insight, 10, "insight_ok", "insight_no")
                .Node("insight_ok", "Ismark Kolyanovich", "*He meets your eyes for the first time.* ...Yes. I stood in the hall with a sword I barely know how to use, and I listened, and I did not open the door. Every night. And every night he came back.")
                    .Then("what")
                .Node("insight_no", "Ismark Kolyanovich", "*His face gives you nothing but tiredness.*")
                    .Then("what")
                .Node("sorry", "Ismark Kolyanovich", "So am I. Though you'd be the first in Barovia to say it. The rest are only sorry he went before he could bar their doors for them, too.")
                    .Then("what")
                .Node("what", "Ismark Kolyanovich", "The wolves, at first. Then worse. Things that used to be our neighbours. They come for my sister, Ireena. The master of the castle has chosen her — he has bitten her twice already, and each time he leaves, she forgets a little more of the night.")
                    .Opt("The master of the castle?", "strahd")
                    .Opt("What do you need from us?", "need")
                .Node("strahd", "Ismark Kolyanovich", "Strahd von Zarovich. Lord of this valley, and the reason the sun hides. *He lowers his voice as he says the name, and so does the fire.* Don't say it loudly. He hears more than he should.")
                    .Then("need")
                .Node("need", "Ismark Kolyanovich", "Two things. My father lies in a coffin in our parlour, and no one will help me bury him — the priest has locked himself in his church and won't come out. And then Ireena must leave this village. Vallaki, west of here, has walls and a burgomaster who still believes in them.")
                    .Opt("We'll help. Where do we start?", "agree")
                    .Opt("What's in it for us?", "pay")
                    .Opt("[PERSUASION] Surely a burgomaster's son can offer more than thanks.", null).Check(Skill.Persuasion, 13, "pay_more", "pay")
                    .Opt("We need time to think.", "later")
                .Node("pay", "Ismark Kolyanovich", "Everything my father left, which is less than you'd hope — the tax-coffers were emptied long ago. Forty gold, and my sword arm, such as it is. And my gratitude, which I suspect is worth more here than in most places.")
                    .Opt("That will do. Where do we start?", "agree")
                    .Opt("We'll think about it.", "later")
                .Node("pay_more", "Ismark Kolyanovich", "*A tired half-smile.* You bargain like a Vistani. Fine. Eighty gold, and there's a ring in my father's study that was my grandfather's — it once turned a wolf's teeth. Take it when you come.")
                    .Do(() => G.SetFlag("ismark_bonus"))
                    .Opt("Deal. Where do we start?", "agree")
                .Node("agree", "Ismark Kolyanovich", "Come to the manor, west side of the village — the one with the iron fence and the claw marks on the door. I'll go ahead and tell Ireena to expect you. *He stands, taller than you expected, and leaves coins on the table for the wine he drank.* Thank you. I mean that.")
                    .Enter(() =>
                    {
                        G.SetFlag("ismark_agreed");
                        Journal("burgomaster", "The Burgomaster's Daughter", "The burgomaster, Kolyan Indirovich, is dead. His son Ismark asked us to help bury him and then escort his sister Ireena — whom the lord of Castle Ravenloft has bitten twice — to the walled town of Vallaki. We are to meet them at the manor on the west side of the village.");
                        G.GiveXP(25);
                    })
                    .Then("leave")
                .Node("leave", "Ismark Kolyanovich", "*He pauses at the door.* And stay off the streets after dark. Whatever you hear.")
                    .Enter(() => IsmarkWalksOut())
                    .End()
                .Node("later", "Ismark Kolyanovich", "Take the time. It's the one thing in Barovia there's plenty of.")
                    .End()
                .Node("again", "Ismark Kolyanovich", "Back again. Have you thought on it?")
                    .Opt("We'll help. Where do we start?", "agree").If(() => !F("ismark_agreed"))
                    .Opt("Tell me about your sister again.", "what")
                    .Opt("Not yet.", "later");

            // ------------------------------------------------------------------ Arik
            new DB("arik")
                .Node("start", "Arik the Barkeep", "*He polishes a cup that is already clean.* Wine.")
                    .Opt("What kind of wine?", "wine")
                    .Opt("We'd like to hear the local gossip.", "gossip")
                    .Opt("Who are the women by the door?", "vistani")
                    .Opt("Buy a round for the room. (2 gold)", "round").If(() => G.gold >= 2).Once()
                    .Opt("Nothing, thanks.")
                .Node("wine", "Arik the Barkeep", "Purple Grapemash Number Three. There was a One and a Two once. *He looks at the cup.* Don't ask what happened to them.")
                    .Opt("We'll take a bottle. (2 gold)", "sold").If(() => G.gold >= 2).Do(() => { G.gold -= 2; G.stash.Add("wine"); Audio.Sfx.Play("gold"); })
                    .Opt("Anything else?", "start")
                .Node("sold", "Arik the Barkeep", "*A bottle appears on the bar. So does his hand, palm up, until the coins are in it.*")
                    .Then("start")
                .Node("round", "Arik the Barkeep", "*For the first time something like expression crosses his face.* ...Round for the room. *The three women at the door raise their cups to you. Ismark does not.*")
                    .Enter(() => { G.gold -= 2; G.partyInspiration = Mathf.Min(4, G.partyInspiration + 1); Toast.Show("Inspiration gained.", Theme.Gold); })
                    .Then("start")
                .Node("gossip", "Arik the Barkeep", "Gossip. *He thinks.* The house down Mad Mary's lane, the one with the grand porch. Nobody lives in it. Nobody's lived in it for as long as I've poured wine. Children stand at its gate some days, all the same. Don't go talking to children in Barovia.")
                    .Opt("What else?", "gossip2")
                    .Opt("Back to the wine.", "start")
                .Node("gossip2", "Arik the Barkeep", "Priest's boy went missing. Doru. Now the priest won't unlock his church, and at night you can hear someone in there screaming for food. *He sets down the cup.* That's enough gossip.")
                    .Enter(() => G.journal.AddLore("Arik: Father Donavich's son Doru disappeared; now the priest keeps his church locked, and something inside screams for food at night."))
                    .Then("start")
                .Node("vistani", "Arik the Barkeep", "Vistani. Travelling folk. The only ones the mists let come and go. They pay in stories and I let them. *A beat.* You'd do well to do the same.")
                    .Then("start");

            // ------------------------------------------------------------------ Vistani
            new DB("vistani")
                .Start(() => F("met_vistani") ? "again" : "start")
                .Node("start", "Alenka", "*The eldest of the three pushes out a stool with her boot.* Look, sisters. Outsiders who walked in through the front door of Barovia. Sit. Drink. Tell us how the mists felt on your skin.")
                    .Opt("Cold. And they took the road behind us.", "cold").Do(() => G.SetFlag("met_vistani"))
                    .Opt("Who are you?", "who").Do(() => G.SetFlag("met_vistani"))
                    .Opt("We're not staying.", null)
                .Node("cold", "Mirabel", "They do that. The mists belong to him. They let us pass because we are useful, and because we are fun. *She smiles with a lot of teeth.* You will find out which you are.")
                    .Then("who")
                .Node("who", "Alenka", "Alenka. My sisters, Mirabel and Sorvia. Our people are camped west of here at Tser Pool, beside the river. If you ever leave this village — and you should — follow the road west and you will find our fires.")
                    .Opt("Can you get us out of the valley?", "out")
                    .Opt("What do you know about the castle?", "castle")
                    .Opt("[INSIGHT] What aren't you telling us?", null).Check(Skill.Insight, 14, "insight_ok", "insight_no")
                    .Opt("Do you have anything to trade?", "trade")
                    .Opt("Enjoy your wine.")
                .Node("out", "Sorvia", "*The youngest laughs, then stops when her sisters don't.* No one leaves. Not you, not us, not really. But Madam Eva, at the camp — she reads cards. Some people she reads for, they find doors in places doors shouldn't be.")
                    .Enter(() => G.journal.AddLore("The Vistani say their people camp at Tser Pool, west of the village, and that a fortune-teller named Madam Eva might help travellers who want to leave Barovia."))
                    .Then("who")
                .Node("castle", "Alenka", "It watches. It remembers. Its master was a conqueror once, before he was what he is. *She turns her cup a slow half circle.* He loves the burgomaster's daughter. Or he loves someone he thinks she is. That kind of love has buried a great many people in this valley.")
                    .Then("who")
                .Node("insight_ok", "Mirabel", "*Her smile goes thin.* Clever. Very well: we tell him things. Who comes through the gates, what they carry, what they fear. He lets our wagons roll. *She shrugs.* We will tell him about you too. We will say you were polite.")
                    .Enter(() => G.journal.AddLore("The Vistani sisters admitted they report travellers to the lord of Castle Ravenloft in exchange for free passage."))
                    .Then("who")
                .Node("insight_no", "Mirabel", "*She pours you more wine. You find you've forgotten the question.*")
                    .Then("who")
                .Node("trade", "Alenka", "For you, we have this. *She lifts a short bow from under the bench, painted all over in red and gold and tiny staring eyes.* It hunted deer across three worlds. One hundred gold.")
                    .Opt("We'll take it. (100 gold)", "bought").If(() => G.gold >= 100 && !F("bought_vistani_bow")).Do(() => { G.gold -= 100; G.stash.Add("bow_vistani"); G.SetFlag("bought_vistani_bow"); Audio.Sfx.Play("gold"); })
                    .Opt("Too rich for us.", "who")
                .Node("bought", "Alenka", "It likes you. You can tell by how little it bites.")
                    .Then("who")
                .Node("again", "Alenka", "Our outsiders return. Sit. The wine is terrible and the company is not.")
                    .Opt("Tell us again about the camp.", "out")
                    .Opt("The castle.", "castle")
                    .Opt("Show me that bow again.", "trade").If(() => !F("bought_vistani_bow"))
                    .Opt("Goodbye.");

            // ------------------------------------------------------------------ Bildrath & Parriwimple
            new DB("bildrath")
                .Start(() => F("met_bildrath") ? "again" : "start")
                .Node("start", "Bildrath Cantemir", "*A heavy old man watches you over the counter with the patience of a spider.* Welcome to the only shop in the Village of Barovia. Prices are fixed, goods are sound, and credit died with my brother. What do you need?")
                    .Enter(() => G.SetFlag("met_bildrath"))
                    .Opt("Show us what you have.", null).Do(() => OpenBildrath())
                    .Opt("Your prices are outrageous.", "prices")
                    .Opt("We found a wagon on the road. Your goods, I think.", "wagon").If(() => G.stash.Has("courier_note") && !F("told_bildrath_wagon"))
                    .Opt("Goodbye.")
                .Node("prices", "Bildrath Cantemir", "Outrageous is having no shop at all. Every nail in here crossed the pass on a wagon, and half the wagons don't. *He taps the counter.* Pay or don't.")
                    .Opt("[INTIMIDATION] Lower them. Now.", null).Check(Skill.Intimidation, 16, "cow", "nocow")
                    .Opt("Fine. Show us.", null).Do(() => OpenBildrath())
                .Node("cow", "Bildrath Cantemir", "*His nephew straightens behind you. Bildrath waves him down.* ...A small discount. For heroes. Don't tell anyone.")
                    .Enter(() => G.SetFlag("bildrath_discount"))
                    .Opt("Show us.", null).Do(() => OpenBildrath())
                .Node("nocow", "Bildrath Cantemir", "*He does not so much as blink.* Parriwimple. The door.")
                    .End()
                .Node("wagon", "Bildrath Cantemir", "*He reads the waybill and his face goes grey.* Mikhail. He's driven that road twenty years. *He is quiet a long moment, then counts out coins.* For bringing word. The wolves can keep the lamp oil.")
                    .Enter(() => { G.SetFlag("told_bildrath_wagon"); G.gold += 15; G.GiveXP(25); Audio.Sfx.Play("gold"); Toast.Show("+15 gold", Theme.Gold); })
                    .Then("again")
                .Node("again", "Bildrath Cantemir", "Back. Buying?")
                    .Opt("Show us what you have.", null).Do(() => OpenBildrath())
                    .Opt("We found a wagon on the road. Your goods, I think.", "wagon").If(() => G.stash.Has("courier_note") && !F("told_bildrath_wagon"))
                    .Opt("Goodbye.");

            new DB("parriwimple")
                .Start(() => F("parri_contest") ? "after" : "start")
                .Node("start", "Parriwimple", "*An enormous young man in an apron stops sweeping.* Hello! Uncle says I'm not to talk to customers because I talk too much. Are you customers? You look like adventurers. Adventurers are better.")
                    .Opt("We're adventurers.", "adv")
                    .Opt("Just browsing.")
                .Node("adv", "Parriwimple", "I knew it! Do you arm-wrestle? Nobody in the village will arm-wrestle me anymore. *He sets his elbow on a barrel hopefully.*")
                    .Opt("[ATHLETICS] You're on.", null).Check(Skill.Athletics, 15, "won", "lost")
                    .Opt("Maybe later.")
                .Node("won", "Parriwimple", "*The barrel creaks. Then his hand goes down, and he stares at it in delight.* You beat me! Nobody beats me! Here — Uncle doesn't know about my jar. *He presses a handful of coins on you.*")
                    .Enter(() => { G.SetFlag("parri_contest"); G.gold += 12; Audio.Sfx.Play("gold"); Toast.Show("+12 gold", Theme.Gold); })
                    .End()
                .Node("lost", "Parriwimple", "*Your knuckles hit the barrel with a sound like a door slamming.* Oh! Sorry. Sorry! Are you alright? Uncle says I don't know my strength. I think I do know it, it's just a lot.")
                    .Enter(() => G.SetFlag("parri_contest"))
                    .End()
                .Node("after", "Parriwimple", "Hello again! I've been practising on the barrels.")
                    .End();

            // ------------------------------------------------------------------ Rose & Thorn
            new DB("rose_thorn")
                .Start(() => F("dh_open") ? "again" : "start")
                .Node("start", "Rose", "*The girl steps in front of her little brother as you approach. Both of them are pale and very clean, and neither is dressed for the cold.* Please — are you grown-ups? Real ones? There's a monster in our house!")
                    .Opt("A monster? Where?", "monster")
                    .Opt("Where are your parents?", "parents")
                    .Opt("[INSIGHT] Something about these children is wrong.", null).Check(Skill.Insight, 13, "wrong", "monster")
                .Node("monster", "Thorn", "*The boy speaks from behind her.* In the basement. It makes noises. And Walter is up in the nursery all alone and Mother and Father won't come out of wherever they went.")
                    .Actor("thorn")
                    .Then("ask")
                .Node("parents", "Rose", "We don't know. They went down to the basement to be with their friends, and they didn't come back up. *She twists her hands.* We're not allowed in the basement.")
                    .Then("monster")
                .Node("wrong", "Rose", "*You notice that neither child casts a shadow in the lamplight, and their breath does not mist in the cold.* ...Please. We're so frightened. Please go in.")
                    .Enter(() => G.journal.AddLore("The children outside the house on Mad Mary's lane cast no shadows, and their breath does not mist in the cold."))
                    .Then("ask")
                .Node("ask", "Rose", "Will you go inside and make the monster go away? And bring Walter out? The door will open for you. It never opens for us.")
                    .Opt("We'll go in.", "yes")
                    .Opt("Not today.", "no")
                .Node("yes", "Rose", "Thank you! Thank you! We'll wait right here. *She takes her brother's hand.* We always wait right here.")
                    .Enter(() =>
                    {
                        G.SetFlag("dh_open");
                        Journal("deathhouse", "The House on Mad Mary's Lane", "Two children, Rose and Thorn, begged us to enter their family's house and deal with a 'monster in the basement', and to rescue their baby brother Walter from the nursery. The front door, they say, will open for us.");
                        Audio.Sfx.Play("unlock", 0.8f, 0.7f);
                    })
                    .End()
                .Node("no", "Thorn", "*The boy begins to cry without making any sound at all.*")
                    .Actor("thorn")
                    .End()
                .Node("again", "Rose", "The door's open now. Please hurry. Walter doesn't like to be alone.")
                    .End();

            // ------------------------------------------------------------------ Mad Mary (through her door)
            new DB("mad_mary")
                .Start(() => F("met_mary") ? "again" : "start")
                .Node("start", "A Woman's Voice", "*The sobbing stops. A bolt scrapes. Through a gap the width of an eye, a wild-haired woman peers out at you.* Gertruda? Is that you? *The eye takes you in.* ...No. No, you're not her. You're never her.")
                    .Enter(() => G.SetFlag("met_mary"))
                    .Opt("Who is Gertruda?", "who")
                    .Opt("We're sorry to disturb you.")
                .Node("who", "Mary", "My daughter. My good girl. I kept her inside every day of her life, safe from all of it — and three nights ago she climbed out of the window in her best dress. *Her voice cracks.* She was humming. She had never heard that song. Somebody taught it to her.")
                    .Opt("We'll look for her.", "promise")
                    .Opt("[INSIGHT] Does she know where Gertruda went?", null).Check(Skill.Insight, 12, "knows", "promise")
                .Node("knows", "Mary", "*She glances past you — not at the street, but up, toward the mountain where the castle stands against the clouds. Then she shuts her eyes tight.* Don't make me say it.")
                    .Then("promise")
                .Node("promise", "Mary", "If you find her... tell her the door isn't barred. Tell her Mama isn't angry. *The bolt scrapes back into place.*")
                    .Enter(() => Journal("gertruda", "Mad Mary's Daughter", "A woman called Mary begged us to find her daughter Gertruda, who slipped out of their house three nights ago humming a song she had never been taught. Mary looked toward Castle Ravenloft when she spoke of it."))
                    .End()
                .Node("again", "Mary", "*Through the door, very quietly:* Is she with you? ...No. Of course not.")
                    .End();

            // ------------------------------------------------------------------ Ismark, outside the manor before the burial
            new DB("ismark_burial")
                .Node("start", "Ismark Kolyanovich", "Father Donavich has agreed? Then we do it now, before he changes his mind — or before the dark comes down. Ireena is ready. Meet us at the graveyard behind the church.")
                    .Opt("We'll be there.")
                    .End();
        }

        static void OpenBildrath()
        {
            var wares = new System.Collections.Generic.List<ItemStack>
            {
                new ItemStack("potion_healing") { count = 4 }, new ItemStack("holy_water") { count = 3 }, new ItemStack("alchemist_fire") { count = 2 },
                new ItemStack("garlic") { count = 6 }, new ItemStack("torch") { count = 5 }, new ItemStack("candle") { count = 8 }, new ItemStack("bread") { count = 6 },
                new ItemStack("scroll_bless"), new ItemStack("elixir_vigilance"), new ItemStack("helm_barovian"),
                new ItemStack("dagger"), new ItemStack("shortbow"), new ItemStack("light_crossbow"), new ItemStack("leather"), new ItemStack("chain_shirt"), new ItemStack("shield"),
            };
            wares.RemoveAll(w => w.Def == null);
            float markup = G.Flag("bildrath_discount") ? 2.0f : 2.6f;
            TradeWindow.OpenShop("Bildrath's Mercantile", wares, markup, 0.3f, 400);
        }

        static void IsmarkWalksOut()
        {
            var a = Game.I.FindActor("ismark");
            G.SetFlag("ismark_left_tavern");
            if (!a) return;
            a.StartCoroutine(WalkOut(a));
        }

        static System.Collections.IEnumerator WalkOut(Actor a)
        {
            yield return new WaitForSeconds(0.4f);
            if (!a) yield break;
            if (a.anim is HumanoidAnimator h) h.seated = false;
            a.sitting = false;
            if (a.agent && UnityEngine.AI.NavMesh.SamplePosition(a.transform.position + Vector3.back * 0.8f, out var hit, 2f, UnityEngine.AI.NavMesh.AllAreas)) { a.agent.enabled = true; a.agent.Warp(hit.position); }
            a.SetAgentSpeed(2.2f);
            bool arrived = false;
            a.MoveTo(new Vector3(0, 0, -4.6f), () => arrived = true);
            float t = 0;
            while (!arrived && t < 12f && a) { t += Time.deltaTime; yield return null; }
            if (a) Object.Destroy(a.gameObject);
        }
    }
}
