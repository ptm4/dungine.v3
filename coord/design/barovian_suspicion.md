# Barovian suspicion: how the valley treats non-humans

**DRAFT for Peter's approval.** Agent C, 2026-09-30. Nothing is built. This is the design. The machine-readable version is
`barovian_suspicion.json`, in this folder, and its numbers match this page. It has no spoilers: only Barovian attitudes in
general, with no module secrets, hidden identities or twists. Your choices are in section 4.

Your call (2026-09-30): "lets keep it true to the book, lets actually put a good amount of distrust, xenophobia and suspicion
upon party members who are not "normal" in barovia, scaling for their race (dwarves and elves are weird sure but hobgoblin,
bugbear obviously are worse)".

## In one screen

| Tier | Name | In one line | Kept races that start here |
|---|---|---|---|
| T0 | **Outlander** | Human, but not from here. | humans |
| T1 | **Odd** | Strange, but plainly people. | high and wood elves, most half-elves, dwarves, halflings, gnomes |
| T2 | **Uncanny** | People, but wrong. | drow, shadar-kai, grey-skinned half-elves, duergar, deep gnomes, goliaths, firbolgs, githyanki, githzerai, dhampirs |
| T3 | **Feared** | Marked by evil. | tieflings, dragonborn, orcs and half-orcs, hexbloods, reborn |
| T4 | **Monstrous** | A monster at the door. | goblins, hobgoblins, bugbears |

**The rules in five lines:**
1. Each member has a tier from their race. Only what villagers can see counts: a hood, full muffling or a disguise can lower it.
2. The worst visible member sets the tier for everything the party does together. A member who is out of sight doesn't count.
3. A human companion's word (a vouch), a superstitious test and good deeds bring it down: two steps at most, and never below
   Odd while a non-human is visible.
4. Being caught in a disguise, changing shape in public, drawing steel or casting spells in the street make it worse.
5. Word travels. Rumours reach the next town before the party does, and bad news travels twice as well as good.

It never blocks the story: key people always talk, story doors always open, and anything the campaign needs can always be
bought or found. Suspicion changes prices, doors and moods, never whether you can finish.

## 1. The tiers

**Why Barovians act like this.** This is the book's spirit, spoiler-free. The mists cut the valley off from the world. Its
people are human, the Barovians and the Vistani, and the dusk elves are the only non-human people they know. Most Barovians
have never met anyone else. They are poor, superstitious and afraid: they hang garlic over the door, shutter the windows and
bar the door after dark. A stranger is a threat until proved otherwise, and the stranger the shape, the worse the threat.
Even a human outlander gets a door held half-shut.

**Faceless.** Our figures are faceless, so nobody is judged by their eyes or face. What gives a race away is on the body:
height, horns, tails, wings, skin, ears, tusks, fur. That is also what a hood or a cloak can hide.

**The tiers, in the villagers' terms:**
- **T0 Outlander: "Human, but not from here."** Barovians are human, and they still don't trust you. You came out of the
  mists, and in their experience nothing good comes out of the mists. They answer, but they keep a hand on the door.
- **T1 Odd: "Strange, but plainly people."** Short folk and fair folk out of grandmothers' tales. The valley knows the dusk
  elves, so elves are strange but real; dwarves, halflings and gnomes look like a story walking. People stare, whisper and
  charge a little more, but they deal with you.
- **T2 Uncanny: "People, but wrong."** Too tall, too grey, too pale, the wrong colour altogether. Nothing in Barovia's
  stories explains them, and what the valley can't explain it keeps at arm's length: served at the end of the counter,
  talked about, never asked in.
- **T3 Feared: "Marked by evil."** Horns and tails, scales and snouts, tusks, a witch's crown, a dead man's stitches: the
  shapes the priests' sermons and the old stories give to devils, dragons, witches and the walking dead. People make the sign
  against evil, bar their doors and send for the watch.
- **T4 Monstrous: "A monster at the door."** Goblin faces, fur, fangs, beaks, beasts that walk like men: the things mothers
  warn their children about, and in Barovia every child knows the wolves are real. People scream, run, pray or pick up a
  pitchfork. Nobody sells to it and nobody opens a door.

### The races the library keeps (73 profiles: R01 to R07, RV1 to RV4)

*Hooded*, *Muffled* and *Disguised* show what the member reads as with that cover (section 2.1). An asterisk marks an alias:
it takes its parent's look and tier.

| Group | Profiles | Tier | Hooded | Muffled | Disguised | What gives them away | Villagers call them |
|---|---|---|---|---|---|---|---|
| Humans | `human`, `human_variant`* | T0 | T0 | T2 (don't) | n/a | nothing | outlander |
| High and wood elves | `elf_high`, `elf_wood`, `elf_pallid`*, `elf_astral`* | T1 | **T0** | T2 | T0 | pointed ears | elf, fair one |
| Half-elves | `half_elf`, `half_elf_high`, `half_elf_wood`, `khoravar`* | T1 | **T0** | T2 | T0 | pointed ears | elf-touched |
| Dwarves | `dwarf_mountain`, `dwarf_hill` | T1 | T1 | T2 | T0 (mountain), T1 (hill: too short) | short and broad, big braided beards on the men | little grandfather, little grandmother |
| Halflings | `halfling_lightfoot`, `halfling_stout`, `halfling_ghostwise`*, `halfling_lotusden`* | T1 | T1 | T2 | T1 (too short) | child-sized, big hairy bare feet | half-child |
| Gnomes | `gnome_rock`, `gnome_forest` | T1 | T1 | T2 | T1 (too short) | child-sized, big head and nose, wild hair | mannikin |
| Dark and ash-grey elves | `elf_drow`, `elf_shadar_kai` | T2 | T2 | T2 | T0 | night-dark or ash-grey skin, bone-white hair (drow) | mist-born |
| Half-elves of drow or sea blood | `half_elf_drow`, `half_elf_aquatic` | T2 | T2 | T2 | T0 | grey-violet or blue-grey skin, gills | mist-born |
| Grey dwarves and deep gnomes | `dwarf_duergar`, `gnome_deep` | T2 | T2 | T2 | T1 (too short) | stone-grey skin, bald or craggy heads | mist-born |
| Goliaths | `goliath_cloud`, `goliath_fire`, `goliath_frost`, `goliath_hill`, `goliath_stone`, `goliath_storm` | T2 | T2 | T2 | T1 (too tall) | 2.25 m tall, stone-marked skin | giant |
| Firbolg | `firbolg` | T2 | T2 | T2 | T1 (too tall) | 2.25 m tall, blue-grey skin, broad nose | giant |
| Githyanki and githzerai | `githyanki`, `githzerai` | T2 | T2 | T2 | T0 | sallow skin, gaunt frame, swept-back ears | the gaunt one |
| Dhampir | `dhampir` | T2 | **T1** | T2 | T0 | corpse pallor, too still | the pale one |
| Tieflings (big horns) | `tiefling_asmodeus`, `tiefling_mephistopheles`, `tiefling_abyssal`, `tiefling_chthonic`, `tiefling_dispater`, `tiefling_fierna`, `tiefling_glasya`, `tiefling_levistus`, `tiefling_mammon`, `tiefling_zariel`, `tiefling_feral`* | T3 | T3 | T3 | T0 | horns, a tail, red, purple or blue skin | horned one, devil's get |
| Tiefling (small horns) | `tiefling_baalzebul` | T3 | T3 | **T2** | T0 | horn nubs, a tail, skin colour | horned one, devil's get |
| Tiefling (winged) | `tiefling_winged` | T3 | T3 | T3 | T0 | horns, a tail, bat wings | horned one, devil's get |
| Dragonborn | `dragonborn_red`, `dragonborn_blue`, `dragonborn_black`, `dragonborn_green`, `dragonborn_white`, `dragonborn_brass`, `dragonborn_bronze`, `dragonborn_copper`, `dragonborn_gold`, `dragonborn_silver`, `dragonborn_amethyst`, `dragonborn_crystal`, `dragonborn_emerald`, `dragonborn_sapphire`, `dragonborn_topaz`, `dragonborn_draconblood`, `dragonborn_ravenite` | T3 | T3 | T3 | T0 | a dragon's head and snout, scales, a crest | lizard, dragon-man |
| Orcs and half-orcs | `half_orc`, `orc` | T3 | T3 | **T2** | T0 | tusks, grey-green skin, a big frame | tusker |
| Hexblood | `hexblood` | T3 | **T2** | T2 | T0 | a crown of thorns grown into the scalp, green-grey skin | witch |
| Reborn | `reborn` | T3 | **T2** | T2 | T0 | grey skin, stitch scars | dead-man |
| Goblins | `goblin`, `goblin_dankwood`*, `boggart`* | T4 | T4 | **T2** | T1 (too short) | goblin face and ears, green skin, child-sized | goblin |
| Hobgoblin | `hobgoblin` | T4 | T4 | **T2** | T0 | goblin face and ears, red-orange skin, a soldier's bearing | goblin |
| Bugbear | `bugbear` | T4 | T4 | **T3** | T1 (too tall) | 2.1 m tall, fur, a bear's head, hunched | bogey |

Notes on the placements:
- **Horns.** Every tiefling horn the kit builds today (ram curl, tall spiral, swept back, long sharp) is too big for a hood,
  so a tiefling stays Feared unless disguised. When the RV2 briefs set each line's horns, any line small enough to hood moves
  to the small-horns row.
- **Drow** are not mistaken for dusk elves: the library's dusk elves are deep brown with black hair.
- **Dhampir.** In the faceless style no fangs show, so the tell is the pallor. A dhampir seen using their bite counts as
  unmasked (Feared for the rest of the visit).
- **Muffling a human or an Odd member makes things worse** (Uncanny): a stranger who hides every inch of skin is suspicious in
  himself. It only pays for Feared and Monstrous members.

**Your party.** A tiefling like Arkus, with one long horn and one broken, is **Feared**. No hood or muffling hides the long
horn; only a disguise does. (This is only an example. Arkus stays yours to bring into the game.) Chai'rn, a wood elf, is
**Odd**, and **Outlander** with the hood up; the hood also covers their glowing tendrils (question 6). Together, the party
reads Feared wherever the tiefling can be seen.

### The races the library cut (67 profiles): ready if they come back

- **T0 Outlander:** `kalashtar`, and the human dragonmarks `human_mark_of_finding`, `human_mark_of_handling`,
  `human_mark_of_making`, `human_mark_of_passage` and `human_mark_of_sentinel`.
- **T1 Odd:** `yuan_ti_pureblood` (the library body shows no scales), `aasimar_protector`, `aasimar_scourge`, and the four
  shifters (`shifter_beasthide`, `shifter_longtooth`, `shifter_swiftstride`, `shifter_wildhunt`): a shifter seen shifting
  counts as changing shape (Monstrous). Aliases take their parent's row: `kender`, `kithkin`, `dwarf_mark_of_warding`,
  `elf_mark_of_shadow`, `half_elf_mark_of_detection`, `half_elf_mark_of_storm`, `halfling_mark_of_healing`,
  `halfling_mark_of_hospitality` and `gnome_mark_of_scribing`.
- **T2 Uncanny:** `elf_sea`, the eladrin (`elf_eladrin_spring`, `elf_eladrin_summer`, `elf_eladrin_autumn` and
  `elf_eladrin_winter`, which a hood takes to Odd), `duskling`, `triton`, `fairy` and its alias `faerie` (wings: nothing hides
  them), `genasi_air`, `genasi_earth`, `genasi_water`, `vedalken`, and `changeling`, which can wear a human face at will;
  seen changing, it is Monstrous.
- **T3 Feared:** `aasimar_fallen` (bone wings), `genasi_fire`, `flamekin`, `rimekin`, `lorwyn_shadowmoor_elf` (antlers),
  `warforged`, `autognome`, `simic_hybrid`, `verdan`, and the alias `half_orc_mark_of_finding`.
- **T4 Monstrous:** `kobold`, `lizardfolk`, `locathah`, `tabaxi`, `leonin`, `lupin`, `harengon`, `minotaur`, `satyr`,
  `hadozee`, `aarakocra`, `kenku`, `owlin`, `tortle`, `giff`, `loxodon`, `centaur`, `thri_kreen`, `grung`, `plasmoid` and
  `lorwyn_changeling`. Beasts that walk like men carry the valley's werewolf fear. The centaur and the thri-kreen can't even
  disguise themselves: a human disguise can't change the body's shape.

Every one of the 140 profiles is in the JSON, with its height, cover results, tells and note.

## 2. What each tier changes

### 2.1 How the tier is worked out

**Cover.** What a member wears decides what villagers can see.

| Cover | What it is | What it hides | Effect |
|---|---|---|---|
| None | | nothing | the race tier |
| Hood | a hood, a wide hat, a headscarf or a cap pulled low | ears, hair, small horns, a crown or mark on the head | race tier minus the race's hood steps (0 or 1). It costs nothing: Barovians wear hoods and headscarves in this weather. |
| Muffled | a hood plus a face scarf or veil, gloves, and a long cloak or coat | also skin colour, tusks, tails and hands | race tier minus the race's muffle steps (0 to 2), but never below T2 |
| Disguise | a disguise kit, Disguise Self, Alter Self, a Hat of Disguise, or a race's own magic (firbolgs, hexbloods) | everything, until someone looks closely | reads T0; T1 if shorter than 1.4 m or 2.1 m and taller, since nobody passes as a grown human at a halfling's or a giant's height |

**Unmasking.** On close contact (talking, trading, a guard's inspection, a search), each NPC checks once a day: their Wisdom
(Insight) against the disguise DC, which is 10 + the wearer's Deception bonus with a kit, or the spell save DC for magic. If
they see through it, the member reads one tier worse than their race for the rest of the visit (Monstrous at most), standing
drops 15, the guards are called, and a rumour starts ("It wore a man's face. Wore it like a coat."). Honesty is safer.

**The party.** The worst visible member counts.
- A member counts if the NPC can see them: within 12 m and in line of sight, or in the same room.
- Shops, inns, door knocks and gates use the **party tier**: the highest tier among the members present.
- In the street, each villager reacts to the highest-tier member inside their notice radius.
- In a conversation, the tier is the highest among the speaker and the members within 6 m.
- **Proxy shopping:** a member who is served can shop for everyone. If the keeper saw the whole party together that day, the
  proxy pays at the party tier minus one. So leaving the tiefling outside works best before the keeper has seen them.
- **Many strangers:** when two or more members sit at the party tier, rumours about the party count one and a half times.
- **Familiar sight:** from the second day in a place, crowds react one tier lower to members they've already seen (never below
  Odd for a non-human). Shops, doors and guards don't soften.
- **Companions:** animal companions, mounts, familiars and summons count at the tier of what they look like. A horse, dog,
  cat or bird is T0; a wolf is T4.

**What brings it down.** These are the relief steps. Each lowers the tier, and every table in this section then applies at
the new tier.
- **Vouch.** A member who reads T0 takes the [VOUCH] option: a Persuasion check, DC 10 + 2 x the tier vouched for. On a pass,
  that NPC treats the party one step lower from then on. On a fail, that NPC won't hear another vouch that day. A Barovian
  travelling with the party vouches automatically for one step, or two with a passed check. If a vouched member causes
  trouble in that place, every vouch there lapses.
- **The test.** A wary NPC may offer a superstitious test: touch the holy symbol, hold the garlic, take a splash of holy
  water, step over the salted threshold. Every playable race passes it, since the test is theatre: what the villager needs is
  to see you submit. Passing takes that NPC one step lower from then on and adds 3 standing. Refusing when asked takes that
  NPC one step higher.
- **[HOOD DOWN].** A member who shows their face before being asked can't be unmasked by that NPC, and adds 2 standing
  (once per member per place).
- **Standing:** each settlement's opinion of the party, from -100 to +100, starting at 0. It doesn't fade: Barovians
  remember. **Trusted** (+60 and up) is two steps down; **Tolerated** (+25 to +59) one step down; **Unknown** (-24 to +24)
  no change; **Distrusted** (-25 to -59) one step up; **Hated** (-60 and below) two steps up, and the gate is shut to the
  party.
- **Cap and floor:** vouches, tests and standing together bring the tier down two steps at most, and never below Odd while a
  non-human is visible. They'll accept you; they won't forget what you are.

**What makes it worse.**
- Seen changing shape (wild shape, polymorph, a changeling's change, a shifter's shift): Monstrous for the rest of the visit,
  standing -20. In Barovia, a person who turns into a beast is the oldest fear there is.
- A weapon drawn in a settlement, or a spell cast in sight of locals (healing excepted, for an hour after): one step worse
  for guards and crowds.
- Night, from dusk to dawn: one step worse for doors, gates and crowds, humans included.

**Who is looking.** Barovians count in full. The Vistani have travelled beyond the valley and go by manners and trade, not
looks, so they halve the tier, rounding down. This draft does the same for the dusk elves, who are outsiders to Barovians
themselves (question 7).

**The formula.** The JSON's `formula` holds the same eight steps.
1. Race tier: from the race table. An alias uses its parent's tier.
2. Cover: hooded = race tier - hood steps; muffled = the larger of 2 and race tier - muffle steps; disguised = 0 (1 if too
   short or too tall) until unmasked.
3. Events: unmasked here = race tier + 1 (at most 4) for the visit; seen changing shape here = 4 for the visit.
4. Party tier: the highest seen tier among the members this NPC can see.
5. Viewer: the Vistani and the dusk elves halve it, rounding down.
6. Relief: a vouch (-1, or -2 from a local), a passed test (-1), standing (Tolerated -1, Trusted -2). At most 2 steps
   together, and never below 1 while a non-human is visible.
7. Penalties: Distrusted +1, Hated +2. For guards and crowds only: a drawn weapon or open magic +1. At night, for doors,
   gates and crowds: +1.
8. Clamp to 0 to 4. The tables below use this effective tier.

### 2.2 Shops

| | T0 Outlander | T1 Odd | T2 Uncanny | T3 Feared | T4 Monstrous |
|---|---|---|---|---|---|
| Buying | x1 | x1.25 | x1.5 | x2 | refused |
| What they pay for your goods | x1 | x0.9 | x0.75 | x0.5 | refused ("I'll not touch its coin") |
| Where you're served | the counter | the counter | the end of the counter; coin goes in a dish, never hand to hand | at the door or through a hatch, never inside | nowhere: the door is barred and the sign turned |
| Protective goods: holy water, holy symbols, silvered weapons, garlic, wolfsbane | sold | sold | sold | refused ("I'll not sell a devil what keeps devils out") | refused |
| Inn room | x1 | x1.25 | x1.5: the worst room, paid in advance | x2, and no room in the house: the attic or the stable loft | no room at all |
| Tavern | served | served, and stared at | served last, at the far table | served last, in a corner; half the room leaves | not let in: the keeper blocks the door |

- A vouch, a passed test or good standing lowers the tier, and the table then applies at the new tier: a vouch at a Feared
  shop gets Uncanny service.
- The multipliers sit on top of each shop's own markup. dungine.v2's village shop already charges everyone 2.6 times the
  item's value, so a 10 gp item costs 26 gp for a human, 32 gp with an Odd member present, and 52 gp with a Feared one.
- Discounts won by a check (like v2's Intimidation at the village shop) use the dialogue DC shifts below.
- Refused goods are never story items: anything the campaign needs can always be had.

### 2.3 Dialogue

**Openers.** Samples for generic villagers. Named characters get their own lines in the same spirit. `{human}` is the member
the NPC chooses to talk to; `{name}` is the member being judged.

| Tier | Villager | Shopkeeper | Guard |
|---|---|---|---|
| T0 | *He looks at your boots, not your face.* Outlanders. Mind where you walk, and mind who you talk to. | Coin first. Outlander coin, I weigh twice. | Name and business. Weapons stay sheathed in town. |
| T1 | *She looks down at you, then at your friends, as if one of them might explain you.* ...And what are you meant to be? | Same prices for your kind. *He writes a new price over the old one.* Plus the trouble. | Business? *He looks {name} up and down, slowly.* Your friend's a long way from home. |
| T2 | *He answers {human}, not you.* Your friend there. Does it understand us? Keep it away from the well. | I'll sell. You stand there, at the end. Coin in the dish. | Halt. Hood down. *A long look.* What in the name of all that's holy are you? |
| T3 | *She makes the sign against evil and keeps her hand up between you.* I've nothing for you. Go back to whatever made you. | Not in here. If {human} wants something, {human} can come in alone. | *His hand is on his sword before you've stopped walking.* That one stays outside. |
| T4 | *He backs into a doorway with a spade up between you.* Stay back! Stay back, or I call the watch! | *The bolt goes home. Through the door:* Closed. Closed to you. | *He shouts over his shoulder:* To the gate! There's a monster on the road! |

**Who they talk to.** From T2 up, the NPC answers the lowest-tier member present and talks past the speaker; the camera
follows who is spoken to. If nobody lower is present, a generic NPC gives one line and ends.

**Which options show.**
- T0 and T1: all options.
- T2: personal options (family, gossip, favours, local history) are hidden.
- T3: business only: trade, directions, the task at hand.
- T4: generic NPCs don't talk, only a bark and a reaction; key NPCs keep only their story options.
- A vouch, a passed test or good standing lowers the tier mid-conversation, and the hidden options come back with it.

**Checks.**

| | T0 | T1 | T2 | T3 | T4 |
|---|---|---|---|---|---|
| Persuasion DC | +0 | +2 | +4 | +6 | +8 |
| Deception DC | +0 | +1 | +2 | +3 | +4 |
| Intimidation DC | +0 | -1 | -2 | -3 | -4 |

Fear works: the stranger the party, the easier it is to frighten people. But each successful Intimidation costs 3 standing
and feeds the rumours.

**New options.**
- **[VOUCH]** "They're with me. I'll answer for them." Pass: "*She looks from you to {name} and back. Her hand comes down,
  slowly.* ...On your head, then." Fail: "*She doesn't even look at you.* Then you're a fool, and I don't deal with fools
  either."
- **[TEST]** "*He holds a braid of garlic out at arm's length.* Touch it. Go on." Pass: "*Nothing happens. He almost looks
  disappointed.* ...Fine. Fine." Refuse: "*He steps back.* That's what I thought."
- **[HOOD DOWN]** Lower your hood before they ask: that NPC can't unmask you, and it earns 2 standing.

### 2.4 Guards and crowds

| | T0 | T1 | T2 | T3 | T4 |
|---|---|---|---|---|---|
| Villagers notice from | 4 m | 8 m | 12 m | 16 m | 24 m |
| What the crowd does | half glance, half ignore you | 60% stare, 30% whisper in pairs, 10% step aside | 40% stare, 30% whisper, 25% step back or cross the street, 5% go inside | 40% go inside, 30% make the warding sign, 20% pull children in, 10% stare | 70% flee inside, 15% grab a tool, 10% cower or pray, 5% raise the alarm |
| Market stalls | open | open | open | stalls within 12 m close for an hour | stalls within 24 m close for the day |
| At the gate | name and business, once | asks your business and watches you in | halts you: questions, "hood down", then lets you in | by day, in with weapons peace-bonded; at night, refused | refused |
| In town | normal patrols | guards' heads follow the member within 10 m | one guard walks your route about 10 m behind, for an hour | one guard shadows the party at 6 to 8 m for the whole visit | challenged on sight: weapons out, ordered out of town |

- At T4, at least one villager raises the alarm if guards are within earshot.
- **Guards never attack on sight.** They bar the way, warn twice, and fight only if the party forces past or draws on them.

### 2.5 Doors and shutters

| | T0 | T1 | T2 | T3 | T4 |
|---|---|---|---|---|---|
| Knocking at a house | *The door opens a hand's width.* "Who's asking?" Lets you in by day on Persuasion DC 12. | Talks through the closed door and doesn't open it: "We've nothing. Try the tavern." | *A slot opens at head height and snaps shut.* "Go away." | *The bar drops.* "Go back to the dark you came from!" | *The light behind the shutters goes out. Somebody inside is praying.* |
| Shutters (on first sight, by day) | none | none | 25% of the windows within 8 m close | 60% within 16 m | all within 24 m |
| Shutters reopen | | | an hour after you leave | the next morning | the next morning, if nothing else happened |

- Night makes every door one tier worse, humans included: after dark, a human knocking gets the Odd answer.
- A Monstrous member seen within 24 m: nearby shops turn the sign and bar the door for the day (hatch trade if Tolerated).
- Holy ground: at T3 and up, the priest asks for the test at the door. Refusing means staying outside.
- Story doors are never locked by suspicion.

### 2.6 Rumours

**Standing changes.** Each is a rumour too: it starts where it happened and travels.

| What happened | Standing |
|---|---|
| A member first seen here: Uncanny / Feared / Monstrous | 0 / -3 / -5 |
| A crowd panics | -5 |
| Intimidation succeeds on a local | -3 |
| Theft seen | -15 |
| Unmasked in a disguise | -15 |
| Seen changing shape | -20 |
| A weapon drawn on a local, or a brawl | -20 |
| A local killed | -50 |
| [HOOD DOWN] before being asked | +2 |
| Passed the test | +3 |
| A gift to the church or the poor (10 gp or more, once a day) | +3 |
| Helped a villager (a small task, healing) | +5 |
| Saved a local's life | +10 |
| Cleared a threat to the settlement | +25 |

**How rumours spread.** A day per road link, two links at most. Bad news carries at half strength and good news at a
quarter: Barovians believe the worst. So clearing a threat in one village (+25) is worth about +6 in the next.

**Forewarned.** Once any rumour about a member reaches a place, that member's first sighting there doesn't set off a panic.
People gather at a distance to stare, and the guards meet the party at the gate. The party is expected.

**What people say.** When clicked, villagers use the freshest rumours (up to 7 days old) instead of their usual line:
- "Did you see the little one with the beard? Like somebody's grandfather, cut off at the knees." (Odd)
- "A giant stooped under the tavern door this morning. Had to." (Uncanny)
- "Horns. I saw horns on it, under the hood." / "They say it's one of his. Sent down to count us." (Feared)
- "A goblin-thing, walking the road like a man, and the outlanders let it." (Monstrous)
- "It wore a man's face. Wore it like a coat." (unmasked) / "It went down on all fours in the square. I saw it change."
  (shapechange)
- "The horned one carried the widow's firewood. I saw it myself. It didn't burn her." / "They cleared the wolves off the
  north road. Whatever they are, they did that." (good deeds)
- "It's them. The ones from the village. The one with the tail." (forewarned)

Every rumour the party overhears goes into a **What they say** page in the journal, with the place and the day, beside each
settlement's standing band.

### 2.7 Three parties, worked through

The JSON's `examples` holds these as test cases.
1. **A tiefling like Arkus, Chai'rn hooded, and a human, in a village square at noon (standing 0).** The tiefling reads
   Feared (the long horn shows), Chai'rn Outlander, the human Outlander: the party is Feared. Villagers notice from 16 m;
   40% go inside, 30% make the sign, 20% pull their children in. Six windows in ten within 16 m shutter. A guard shadows the
   party at 6 to 8 m.
   - **In the shop:** served at the door, x2, and no holy water or other protective goods. The human vouches (Persuasion
     DC 16) and passes: Uncanny. Now they're served inside at the end of the counter, x1.5, coin in the dish, and the
     protective goods are for sale again.
   - **Or:** the tiefling waits outside. If the keeper hasn't seen the tiefling: Outlander, x1. If the keeper saw them all
     together that morning: Uncanny, x1.5.
   - **A week later,** after they clear a threat (+25, Tolerated) and with the vouch: Odd. Two steps is the cap, and Odd is
     the floor.
2. **A bugbear, a hobgoblin and a human cleric at a town gate by day.** Monstrous: refused. The cleric can vouch
   (Persuasion DC 18); on a pass, the guard treats them as Feared and lets them in by day, weapons peace-bonded. Or the two
   muffle up: the bugbear reads Feared and the hobgoblin Uncanny, so the party is Feared and gets in by day without a vouch.
   Either way a guard shadows them all visit. A vouch only persuades the one it was made to, so an unmuffled bugbear still
   sends the street running.
3. **A hooded half-elf, a dwarf and a halfling.** The half-elf reads Outlander, the other two Odd: the party is Odd. Prices
   x1.25; stares and whispers within 8 m; doors talk to them through the wood. A light touch.

### 2.8 Knobs for Peter

Every number above is in the JSON and can be changed there. The ones most worth turning:

| Knob | Default | What it does |
|---|---|---|
| `enabled` | on | the whole system |
| `intensity` | 1.0 (0.5 to 1.5) | scales every price, DC and radius change up or down, from mild to harsh |
| `tier_shift` | 0 (-1 to +1) | moves every race one tier milder or harsher |
| `race_tier_overrides` | none | moves any single race to another tier |
| `combine.rule` | worst | how the party combines: worst, average rounded up, or worst softened by humans |
| `effects.shops.buy_mult` / `sell_mult` | x1 / 1.25 / 1.5 / 2 / refused; x1 / 0.9 / 0.75 / 0.5 / refused | shop prices per tier |
| `effects.shops.refuse_trade_from_tier` | 4 | 3 = Feared refused too; 5 = never refused |
| `effects.dialogue.persuasion_dc_shift` | +0 / 2 / 4 / 6 / 8 | how hard it is to talk people round |
| `effects.crowds.notice_radius_m` | 4 / 8 / 12 / 16 / 24 | how far away villagers react |
| `effects.doors.shutters_close_share` | 0 / 0 / 25% / 60% / 100% | how many windows shutter |
| `relief.cap_steps` | 2 | how far vouches, tests and deeds can bring a tier down |
| `relief.accepted_floor_tier` | 1 | 0 lets a trusted non-human read as an Outlander |
| `cover.muffled.floor_tier` | 2 | what a fully muffled figure reads as, at best |
| `modifiers.night.steps` | 1 | how much worse doors, gates and crowds get at night |
| `viewers` | Barovians 1, Vistani 0.5, dusk elves 0.5 | how wary each people is |
| `effects.rumours.spread` | a day a link, bad 50%, good 25%, 2 links | how word travels |
| `settlement_tier_shift` | 0 everywhere | per-town wariness. Left equal in this draft so nothing hints at what each town is like; it can be set later. |
| `quest_safe` | on | keep it on |

### 2.9 Where it plugs in

This is for whoever builds it. The names are dungine.v2's, for reference; the engine is still being chosen.
- **Shops:** `TradeWindow.OpenShop(name, wares, markup, sellRate, gold)` takes the markup and sell rate; multiply them by the
  tier's values, and check refusal in the dialogue option that opens the shop.
- **Dialogue:** `DB.Start` picks a tier opener node; `DOption.cond` gates options by tier; `DB.Check` adds the DC shift;
  `Fill()` gains `{human}` and `{called}`; the runner reframes on the member the NPC talks to.
- **Street:** villagers' `barkOnClick` lines come from the rumour pools; a small reactor component picks the crowd reaction by
  tier and distance and plays the animations below.
- **Doors:** the existing knock interactions answer by tier.
- **State:** standing per settlement, vouches and tests per NPC, and unmasking per member per settlement go in the save,
  along with the rumour list.
- **Hoods:** v2's `Appearance.hooded` already draws a hood; the hood becomes a toggle with its own animation.

**What it needs from the library** (for the main session to plan, not built here):
- player versions of the hood and face scarf (review 8's hoods and scarves are NPC variants, and NPC gear stays unique), with
  mannequin and floor versions as usual;
- the reaction props: a pitchfork, spade, cudgel and torch to grab, a coin dish, a door bar, hinged shutters, a turning shop
  sign, a door slot (the garlic braid and the holy symbols are already planned);
- a small icon set for the portrait (open shutter to barred door), with the U01 icon style.

## 3. Villager reaction animations

For the animation list (agent B's `coord\research\animation_pipeline.md`). Our figures are faceless, so every reaction is
carried by the head, shoulders, hands and feet, and by props: doors, shutters, lamps. None of it needs a face. The head turn,
bow, stoop and lean going into the kit now do much of the work, and the ones marked procedural come almost free from a
look-at. **46 villager clips, 29 of them for the first playable slice; 5 for the party; 8 prop animations.**

**Attention (T0 and up)**

| Name | What the body does | When | Kind | Slice |
|---|---|---|---|---|
| `react_glance` | the head turns to the stranger for a beat, then back to work | T0+ | one-shot, head (procedural) | first |
| `react_stare` | stops work; head and shoulders turn and follow the stranger; hands go still | T1+ | loop layer (look-at on a still pose) | first |
| `react_whisper_pair` | two villagers lean their heads together, one hand raised beside the head; both glance at the party | T1+, and while a rumour is fresh | pair loop | first |
| `react_nudge_point` | an elbow to a neighbour, then a low point at hip height | T1+ | one-shot | later |
| `react_head_shake` | a slow shake with the chin down | T1+ in dialogue and refusals | one-shot, head (procedural) | first |

**Wariness (T2)**

| Name | What the body does | When | Kind | Slice |
|---|---|---|---|---|
| `react_step_back` | one step back, weight on the back foot, hands drawn up to the chest | T2+ | one-shot, root motion | first |
| `react_arms_fold` | folds the arms high, squares the shoulders, lifts the chin: a closed stance | T2+ (shopkeepers, gatekeepers) | enter + idle loop | first |
| `react_turn_away` | turns a shoulder, then the back, head down | T2+ (a refused talk) | one-shot, root turn | first |
| `walk_wary` | a walk with hunched shoulders and the head turned toward the stranger, drifting to the far side of the street | T2+ | locomotion loop (walk + look-at) | first |
| `react_clutch_symbol` | one hand closes on a pendant or holy symbol at the chest; the other covers it | T2+ | enter + loop | first |
| `react_pull_child` | steps between a child and the stranger and sweeps the child behind the coat or skirts | T2+ | pair one-shot (adult) | later |
| `child_hide_behind` | the child ducks behind the adult and peeks round | T2+ | pair one-shot (child) | later |
| `child_curious` | the child edges toward the stranger, head tilted, one hand out, until pulled away | T1+ (question 11) | loop | later |

**Fear (T3)**

| Name | What the body does | When | Kind | Slice |
|---|---|---|---|---|
| `react_warding_sign` | the valley's sign against evil: fingertips to the brow, then to the heart, then the hand thrust out flat toward the stranger and held. Everyone makes the same sign. | T3+ (T2 for the pious) | one-shot + hold loop | first |
| `react_spit_aside` | a sharp turn of the head and a spit to the side | T3+ | one-shot | later |
| `react_drop_carry` | startles; what they carry falls; the hands fly up | T3+ on first sight | one-shot (the dropped prop falls with physics) | later |
| `react_back_away` | backs away facing the stranger, palms out, small steps | T3+ | locomotion loop, backward | first |
| `walk_hurry` | a fast hunched walk, arms tight, head low, one glance back | T3+ | locomotion loop | first |
| `door_slam_bar` | pulls the door shut from inside and drops the bar | T3+ | one-shot, synced to the door | first |
| `window_shutters_close` | leans out of a window, pulls both shutters in and latches them | T2+ | one-shot, upper body in the window | first |
| `window_lamp_out` | cups the lamp and blows it out; the window goes dark | T3+ (T2+ at night) | one-shot | later |

**Terror (T4)**

| Name | What the body does | When | Kind | Slice |
|---|---|---|---|---|
| `run_scared` | a hunched run, arms pulled in, a look back every few strides | T4 | locomotion loop | first |
| `react_cower` | drops to a crouch with the arms over the head, trembling | T4, when cornered | enter + loop | first |
| `react_pray_kneel` | drops to the knees, hands clasped at the chest, rocking | T4 (the old and the pious) | enter + loop | later |
| `react_raise_alarm` | the head thrown back and both hands raised beside it, then an arm pointing at the stranger | T4 | one-shot | first |
| `react_grab_tool` | snatches up a pitchfork, spade, cudgel or torch and brings it across the body in both hands (your across-the-chest grip) | T4 (the brave), mobs | one-shot | first |
| `react_brandish` | holds the tool out with short jabs, feet planted, giving half a step between jabs | T4 | loop | first |

**Trade**

| Name | What the body does | When | Kind | Slice |
|---|---|---|---|---|
| `shop_refuse` | palm out and a firm head shake; the other hand flat on the counter or ledger | refusals (T3, T4) | one-shot | first |
| `shop_coin_dish` | slides a dish across for the coin, never hand to hand, then tips the coin out | T2, T3 | one-shot | later |
| `shop_goods_arm_length` | pushes the goods across at arm's length, leaning back | T2, T3 | one-shot | later |
| `shop_close_up` | turns the sign and pulls the hatch or counter flap shut | T4 | one-shot | later |

**Guards**

| Name | What the body does | When | Kind | Slice |
|---|---|---|---|---|
| `guard_halt` | steps into the path, one hand raised flat, the other on the hilt | T2+ at gates | one-shot + hold | first |
| `guard_hand_on_hilt` | a resting hand on the hilt while the head tracks the stranger | T2+ | layer loop (look-at) | first |
| `guard_bar_path` | plants the feet and holds the spear or halberd across the chest to block the way (the across-the-chest grip) | T3+ | one-shot + hold | first |
| `guard_draw_ready` | draws and holds the weapon low and ready | T3, T4, or when a party weapon is drawn | one-shot (reuses the combat draw) | first |
| `guard_move_along` | a sweeping wave of the hand: move along | T2, T3 | one-shot | later |
| `guard_signal` | fingers to the front of the head for a whistle, or an arm raised, calling another guard | T3, T4 | one-shot | later |
| `guard_escort_walk` | walks alongside at a set distance, head locked on the stranger | T2+ | locomotion + look-at (procedural) | first |

**Doors, warmth and the test**

| Name | What the body does | When | Kind | Slice |
|---|---|---|---|---|
| `door_peek` | opens the door a hand's width and talks through the gap, one hand on the door edge | knocks at T0, T1 | enter + loop | first |
| `hatch_speak` | a slot in the door or shutter opens; a hand and the shadow of a head show; it snaps shut | knocks at T2 | prop + audio | later |
| `react_nod` | a small nod of recognition | standing Tolerated or better | one-shot, head (procedural) | first |
| `react_bow_thanks` | a hand on the heart and a short bow | Tolerated or better, after a deed | one-shot | first |
| `react_offer_gift` | holds out a small gift in both hands: bread, a garlic braid, a candle | Trusted | one-shot | later |
| `react_ease` | the shoulders drop and the hands open: the way back from any wary pose | after a vouch or a passed test | one-shot | first |
| `test_hold_out` | holds a holy symbol or a garlic braid out at arm's length, leaning back, waiting | [TEST] | enter + loop | later |
| `test_splash` | flicks a few drops of holy water at the stranger, then peers | [TEST] | one-shot | later |

**The party's side**

| Name | What the body does | When | Slice |
|---|---|---|---|
| `pc_hood_up` | both hands lift the hood over the head | cover on | first |
| `pc_hood_down` | both hands push the hood back | cover off, [HOOD DOWN] | first |
| `pc_open_hands` | hands open and out, palms up: I mean no harm | [VOUCH], [TEST] | first |
| `pc_vouch_shoulder` | the voucher sets a hand on the companion's shoulder | [VOUCH] | later |
| `pc_submit_test` | stands still, chin up, arms a little out, while the test is done | [TEST] | later |

**Props:** `prop_door_crack` (opens a hand's width and holds), `prop_door_slam`, `prop_bar_drop`, `prop_shutters_close`,
`prop_shutters_open`, `prop_window_light_off`, `prop_sign_turn`, `prop_hatch_slide`.

## 4. Open questions for Peter

Each has a recommendation; answer by number and letter (for example "1B, 2A, 5B: orc to Monstrous").
1. **How harsh by default?** A: mild (intensity 0.5, every race one tier milder). **B: the book, as drafted (recommended).**
   C: harsh (intensity 1.5, and shops refuse Feared members too).
2. **Does the worst member count most?** **A: yes, the worst visible member sets the tier for everything the party does
   together (recommended).** B: no, use the party's average, rounded up. C: mostly: the worst member, minus one step if at
   least half of those present read as human.
3. **How far can good deeds take a non-human?** **A: down to Odd at best: accepted, never forgotten (recommended).** B: all the
   way: at Trusted they read as Outlanders. C: one step at most, ever.
4. **Disguises: how risky?** **A: risky: unmasked is worse than honest, one tier worse for the visit and standing -15
   (recommended).** B: safe: unmasked just means back to the normal tier. C: none: players can't disguise their race.
5. **The borderline races.** As drafted: orc Feared; drow and the grey-skinned half-elves Uncanny; dhampir Uncanny; goliath
   and firbolg Uncanny; dragonborn Feared. **A: keep them (recommended).** B: move some (say which, and where).
6. **Magic in a character's look, like Chai'rn's glowing tendrils: does it count?** **A: race only, but casting a spell in
   public counts one step for an hour (recommended).** B: a permanent glow counts one step while it shows (a hood covers
   Chai'rn's). C: magic never counts.
7. **The Vistani and the dusk elves as onlookers.** **A: half as wary as Barovians (recommended).** B: as wary as Barovians.
   C: not wary at all.
8. **What villagers call non-humans.** **A: mild in-world names (horned one, devil's get, tusker, bogey), never about skin
   colour (recommended).** B: none: they only describe (the tall one, the one with the tail).
9. **Show it in the interface?** **A: yes: an icon on each portrait for how Barovia sees that member right now, and the What
   they say journal page (recommended).** B: no: players learn it by playing.
10. **The party's own reactions:** a short bark from a member when a door slams on them? **A: yes, short and occasional
    (recommended).** B: no.
11. **Children.** **A: curious, not afraid, until a parent pulls them away (recommended).** B: afraid, like the adults.
