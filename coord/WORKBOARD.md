# Workboard

The live board for Dungine.v3 and the asset library work that feeds it. The main session keeps it; agents write only
their own reports. Newest log entries at the bottom.

**Working mode (Peter, 2026-09-30):** "keep working continuously / prompt me for reviews as you go but unless I stop you on
a part keep going".

## Running now

| Who | What | Output |
|---|---|---|
| Main session | Coordinating: renders (one Blender at a time, queued through a lock), publishing, this board. Render queue: the dire wolf's round 3 and review 26 (livestock). | this board |
| Agent A | Phase 2: back up v2 (a copy, v2 untouched), fork it into v3, get it running unchanged in Unity 6000.6 (`agents\A_phase2_fork_v2.md`) | the v3 game files, `research\phase2\` |
| Agent E | The village kit (E01) at 1.5 cm: reviews 33 (houses, tavern front), 34 (church, undercroft, tower), 35 (manor, walls, fences, square, well, stall, notice board, statue) (`agents\E_village_kit_build.md`) | library `pixel3d\reviews\33_...` to `35_...` |
| Agent H | Review 32 (I07): camel, mule, elephant, the 12 bardings, tack, land vehicles, the rowboat (big ships held) | library `pixel3d\reviews\32_...` |
| Agent N | Soft bends at the elbows and knees (Peter's choice), all 19 rigs re-exported, review 17's page noted | the rigs, `pixel3d\reviews\17_rig_stress` |
| Agent O | M20, the uncommon magic items: review 30, then review 31 | library `pixel3d\reviews\30_...`, `31_...` |
| Agent P | Chapter One's creatures (C02), sealed; then its location (L01), sealed, once the village kit exists. Counts-only reports. | library `pixel3d\sealed\chapter1\` |
| Agent T | Review 24: gems and art objects (I09), trade goods, food and drink, and coins | library `pixel3d\reviews\24_treasure_trade_goods` |

## Waiting for Peter

- **Review 23, the Tarokka high deck** (library `pixel3d\reviews\23_tarokka_high_deck\index.html`, by agent S): the
  fourteen cards, woodcut. No questions.
- **The suspicion design, a draft** (`design\barovian_suspicion.md`, by agent C): five tiers, from Outlander (humans) to
  Monstrous (goblins, hobgoblins, bugbears); the worst visible member sets the tier; suspicion never blocks the story.
  Section 4 has 11 questions. Its warding sign is decided: clutch a charm and spit.
- **The engine plan's other questions** (`research\engine_decision.md`): how the friends play (each on their own, turns
  on one PC, or online), the look, the character creator and race list, what replaces the premade pilot party, fog.
  Not needed for phase 2.
- Coming next: the dire wolf's round 3 and review 26 (rendering), then reviews 24, 30, 31, 32, the village kit (33 to
  35), and phase 2's first capture of v3.

## Next up

- Review 19: after its renders, the page, the manifest, the home link, Q's lessons, and `gear2` loaded with the kit (then
  the hero regression).
- The round-2 pages (reviews 11, 12, 13, 15, 16, 17) as their agents finish: render the changed models, publish, prompt
  Peter.
- After the engine decision: back up v2, fork it into v3, and get it running unchanged (phase 2).
- G1 and G2, the rig on the game side, once v3 exists.
- The kit's bounded painting (agent E's finding: 27 times faster at 1.5 cm, with the same voxels), adopted with a full
  regression once the build agents are idle.

## Done

- 2026-10-02: Peter approved reviews 12, 17 (soft bends), 22, 25, 27, 28 and 29, chose 1.5 cm everywhere for the environment, approved the engine plan, answered the animation plan, kept Chapter One's location and creatures a surprise (sealed), and left review 5's floor armour. His words are in `decisions.md`.
- 2026-09-30: Peter approved reviews 11 ("11 perfect"), 15 ("15 is good"), 19 and 20 ("19 & 20 approved"), and 13 but the dire wolf ("13 is good except for the dire wolf they just look fat now lol"). The icon style is decided: "16 lets do Voxel over stained glass im good & decided".
- 2026-09-30: Peter reviewed 10 to 18 and 21. Approved as built ("the ones I ignored are approved"): review 10 (76 assets), review 14 (28) and review 18 (100), their open questions settled as the pages recommended. Round 2 for 11, 12, 13, 15, 16 and 17. Look B, the woodcut, for the Tarokka deck.
- 2026-09-30: review 9 built, re-posed (round 2) and approved: 16 people, their outfits, the lineup and sprites. The
  sealed side is built.
- 2026-09-30: Dungine.v3 set up: this folder, `README.md`, `PLAN.md`, `decisions.md` and the agent briefs.
- 2026-09-30: posing in the library kit (`kit\pose.py`). Regression clean.
- 2026-09-30: rig export v0 (`pixel3d\pipeline\rig_export.py`): a 19-bone skinned GLB, checked in Blender. Agent I is
  taking it to the plan's 24-bone spec.
- 2026-09-30: agent A (v2 audit, engine), B (animation plan), C (suspicion draft), E (scale study, E01 brief) and K
  (spoiler audit) reports, all checked.
- 2026-09-30: review 10 built by agent D (76 props, 8 lineups) and rendered. The flame colours were tuned after the
  first renders.
- 2026-09-30: the spoiler audit applied: 163 assets hidden on the plan page, 7 batch titles or notes neutralised, 91
  manifest fields and 25 text lines rewritten.

## Log

- 2026-09-30: v3 created. Agents A, B and C started, with their briefs in `agents\`.
- 2026-09-30: posing landed in the library kit. Eight Chapter One people were re-posed from their briefs. Regression: the
  57 hero and review 1 to 3 models, and review 9's eight unposed people, are all unchanged.
- 2026-09-30: Peter asked whether UniMate can make the animations. Agent B evaluated it: a short trial once the rig
  exists.
- 2026-09-30: agents D (review 10) and E (scale, E01 brief) started. Review 9 round 2 went up, and Peter approved it.
- 2026-09-30: agents F (review 11), G (review 12) and H (review 13) started, at Peter's ask.
- 2026-09-30: agents A, B and C finished. This machine has an RTX 4070 Ti (12 GB) and 32 GB of RAM.
- 2026-09-30: agent I started the animation plan's library side (L1 to L3), on the plan's recommended defaults. Arkus
  and Chai'rn are skipped (locked; Peter hasn't been asked).
- 2026-09-30: agent E finished. The main session rendered the brief's page, set E01 to brief-ready (15 assets) and
  added E's lessons. E's slip: one `find` walked file names under the sealed folder, filtered out unread; those names
  are neutral anyway.
- 2026-09-30: the plan page's spoiler guard hid only named NPCs' notes, but some asset names alone gave the game away. A
  first pass hid 32; agent K audited the rest, and the main session applied it (163 hidden in all). Agents J (review 14)
  and K started and K finished.
- 2026-09-30: agent D finished review 10's build. The first renders showed flames burning out to pale peach (the
  renderer multiplies glow by 6). The main session tuned the palette, rebuilt the 20 lit props and the lineups, and told
  agents F and G, whose candles and torches share it.
- 2026-09-30: agent F finished review 11's build: 24 models (12 props, 12 second looks) and 2 lineups, hero regression
  clean. Its candles already use the new flame palette.
  - It found a renderer bug: for props of 3 m and more, the item view's rim light ended up behind the backdrop wall.
    The main session fixed `render_vox.py` (the wall now stays behind every light; small items render as before) and
    added F's lessons.
  - Review 11 is queued to render after review 10.
  - F's questions for Peter are on its page: seated stocks or a standing pillory? And the open grave is a block of ground
    that the game sinks into its terrain.
- 2026-09-30: agent H finished review 13's build:
  - 36 creatures: the 18 beasts and mounts, and their second looks.
  - The mounts' second looks are tacked up (saddle and bridle, war saddle, pack saddle, collar and harness); the others
    wear another coat.
  - The mounts record saddle, bridle and barding points for the rig.
  - Every figure is one connected piece with its feet on the ground. It has 5 lineups and its sprites.
  - Regression clean, and review 6's wolf and bat forms are unchanged. Its mist form predates Strahd's final face, so a
    rebuild differs slightly; it's left as approved.
  - Queued to render after review 11. H's lessons were added.
- 2026-09-30: agents L (review 15, v2's loot, minus the 3 spoiler-flagged items, which will be sealed) and M (review 16,
  icon styles) started.
- 2026-09-30: Arik and Bildrath re-rendered with their aprons (7 renders). The review 9 page carries a note on the fix.
- 2026-09-30: agent I's rigs checked in Blender (the fighter, Mad Mary and Arik):
  - each imports with 23 bones, 23 vertex groups and 8 socket empties;
  - the texture atlas maps the voxel colours correctly;
  - posing an arm, an elbow, the head and a step deforms correctly.
  - As expected, Mad Mary's skirt splits along the stepping leg and her shawl bands the arms. Agent N fixes these (L7,
    then L4).
- 2026-09-30: agents N (the rig, L7 and L4, plus the stress sheet as review 17) and O (review 18, the common magic items)
  started.
- 2026-09-30: agent J finished review 14's build:
  - 28 models (14 horror props and their second looks), 4 lineups, sprites. Nothing floats, and the regression is clean.
  - Questions on its page: should skulls show their eye sockets (a face?), be face down, or have dust-filled sockets?
    Teeth drawn twice life size? Wall pieces at real height, or on the floor as review 10 does?
  - Rendering now. J's lessons were added. J's slip: it printed one N01 manifest entry, with plot notes, into its own
    context only; nothing went into Peter's pages.
- 2026-09-30: **review 12 published** (adventuring gear): 99 renders, 25 in review and 25 second looks added, 2,459
  assets. The page is clean for spoilers, and the renders were checked by eye.
- 2026-09-30: shared fix: the held and worn parts registered by review batches' own modules (adventuring gear, v2's loot, the magic items, gear 3) now load with the kit (`kit\parts\__init__.py`), so the catalogue lists them (216 parts) and `kit.build` knows them. Regression: the heroes and a skirted commoner are identical. Known gap: a saved demo recipe names a race skin colour (`skin_human`) that only `race_review` registers, so demos rebuild through their review scripts, not plain `kit.build`. Gear 2 (agent Q) will be added when Q finishes.
- 2026-09-30: agent R finished review 20's build (consumables, tools and kits): 23 items and 23 second looks, 46 floor versions, 10 demos and 3 lineups, sprites. Faces fixed along the way (a cord's holes, an ink cork, a round compass, a row of corks); regression clean. Rendering now.
- 2026-09-30: **review 21 published** (the Tarokka mock card): drawn without Blender, page scanned (no reading meanings), manifest note on U02, home link.
- 2026-09-30: **review 18 published** (common magic items): 219 renders, 50 in review and 50 second looks added, 2,548 assets. Page clean; glows checked by eye.
- 2026-09-30: agent N's new rigs checked in Blender (the fighter 31 bones, Mad Mary 48, Ireena 51): they import with every chain bone and 8 socket empties, the texture maps, and posing works. The skirts drape over a stepping leg instead of splitting, and Mad Mary's shawl stays on her chest as the arm moves.
- 2026-09-30: agent N finished the rig, L7 and L4:
  - soft chains for skirts, robes, coats, capes, long hair and tails, with springs described in VRM terms for the
    game;
  - ball joints with hidden cores, and soft bands on the trunk and neck;
  - all six rest-stance part breaks fixed, in rig builds only.
  - 19 GLBs re-exported, and every check passes. Regression clean: the heroes, all 16 review 9 figures, the skirted
    commoners and 20 review 6 armour files are identical.
  - Cost: triangles roughly doubled (8k to 63k per figure), since soft cloth is drawn a voxel face at a time.
  - **Review 17** (the stress sheet) is published. Blender re-check of the new GLBs (chain bones, four weights per
    vertex) queued after review 18's renders.
- 2026-09-30: the rest of Chapter One waits on Peter's decisions (the tall house's rooms and creatures, the environment scale, the icon style), so agents Q (review 19, gear 2), R (review 20, gear 3) and S (review 21, the Tarokka mock card) started on P2's unblocked batches.
- 2026-09-30: agent O finished review 18's build:
  - 50 common magic items and 50 second looks, 100 floor versions, 10 on mannequins, 15 demos, 4 lineups and sprites:
    215 models.
  - Glows within limits; regression clean.
  - Questions for Peter on its page: the shield's gilt mask and the tankard's brass face (faces the rules give
    them); the ersatz eye, which is an eye; tiny things drawn larger; the jeweller's stand and cradles.
  - Side finding: review 5's approved floor half plate and splint have their greaves 2 voxels up. Left as approved;
    a question for Peter.
  - Rendering now. O's lessons were added.
- 2026-09-30: shared fix: `sprite_from_vox.crop` returned an empty image when a sprite touched its frame (agents G and J hit it). It now pads only in that case; every other sprite crops as before.
- 2026-09-30: agent P finished: the three spoiler-flagged loot items are built and sealed (12 models, no pictures), the manifest points them to the sealed folder (status sealed), and the regression is clean. The sealed README names the new builder.
- 2026-09-30: **review 15 published** (the loot v2 invented): 119 renders, 25 in review and 25 second looks added, 2,498 assets. Page clean; glows checked by eye.
- 2026-09-30: agent P started: the three spoiler-flagged loot items, built sealed like the hidden forms (Peter's rule for what only play reveals).
- 2026-09-30: agent L finished review 15 (v2's loot): 25 items and 25 second looks, 50 floor versions, 14 demos, 5 lineups and sprites. Faces and glows checked, regression clean. Rendering now (119 runs). L's lessons were added.
- 2026-09-30: agents N (rig), L (review 15) and O (review 18) were cut off by an API usage limit mid-task. When it reset,
  Peter said "Try again", and all three were resumed from where they stopped. N had added a flag to `kit\core.py` (a
  recipe carrying `rig` sets `c.data['rest_build']`). The main session checked it: library recipes never carry that flag,
  so it's safe; N still owes the full regression.
- 2026-09-30: **review 16 published** (icon styles, by agent M): no renders needed; manifest (U01 in review), home link and lessons added.
- 2026-09-30: **review 14 published** (horror dressing): 32 renders, 14 in review and 14 second looks added, 2,473 assets. Page clean; renders checked by eye (the hanged man is sack-hooded, so faceless).
- 2026-09-30: **review 13 published** (beasts and mounts): 18 in review, 18 second looks added, 2,434 assets. Page
  checked: its only hits were references to review 6's forms and the word "shaggy". Renders checked by eye: no eyes,
  clean framing.
- 2026-09-30: **review 11 published** (graveyard and religion): 12 props in review, 12 second looks added, lineups, home
  link. Page checked for spoilers: clean.
- 2026-09-30: agent G finished review 12's build:
  - 96 models: 25 items, 25 second looks, 38 floor versions and 8 held or worn demos, plus 3 lineups and sprites.
  - Its lit items use the new flame palette. Regression clean. Queued to render after review 13.
  - Two shared changes are waiting for a quiet moment: the kit catalogue and `kit.build` need to import
    `kit.adventuring_gear` for its three parts, and `sprite_from_vox.crop` returns an empty image when an outline touches
    the frame edge.
- 2026-09-30: agent I finished the rig export, L1 to L3:
  - 19 rigged GLBs in the library: the 16 Chapter One people and the heroes fighter, ranger and wizard. Each has a
    23-joint humanoid skeleton, 8 sockets and a point-sampled atlas, and carries the figure's approved pose as bone
    rotations.
  - Faces are merged to 39% of the triangles. Every structural and round-trip check passes, and the regression is
    clean.
  - Parts that break in the rest stance: the shawl wraps the upper arms; the waistcoat and laced bodice paint over the
    inner upper arms of broad bodies; skirts and robes notch round the hanging hands; long skirts and robes can't walk
    with rigid skinning. The plan's L4 (joints) and L7 (blended skirts, cape chains) fix these; L7 comes first.
- 2026-09-30: a defect in approved review 9, found by agent I. The apron part only painted over a long skirt, so Arik's
  and Bildrath's aprons (in their briefs) were missing.
  - Fixed in `valley.py`: with no skirt, the apron hangs straight from the waist. The skirted commoner looks are
    unchanged, voxel for voxel.
  - Arik and Bildrath are rebuilt, with their outfits, the lineup, sprites and rigs. Their renders are queued.
- 2026-09-30: **review 10 published**, with the flames re-rendered: the page, the manifest (38 in review and 38 second
  looks added, 2,404 assets), the library home, the catalogue and the plan. Its page text was checked for spoilers, and
  one hinting line was trimmed.
- 2026-09-30: agent Q finished review 19's build (gear 2): 51 items and 51 second looks, 102 floor versions, 19 demos, 5 lineups and sprites. 7 would-be faces and 5 misreads fixed; hero regression clean. Rendering now (228 runs).
- 2026-09-30: review 20's holy water looked like a face at sprite size: the cord round its neck filled only the neck's two front corners, two "eyes" under the dark cap. The cord is now twine and fills the whole layer, one band across the front. Rebuilt and re-rendered (6 renders).
- 2026-09-30: **review 20 published** (consumables, tools and kits): 105 renders plus the holy water's 6, 23 in review and 23 second looks added, 2,571 assets; page clean for spoilers; home link, thumbnail and catalogue.
- 2026-09-30: Peter's review of 10 to 18 and 21 (his notes, in his words):
  - 11: "I pictured a standing stocks/pillory"; 12: "Some of these are a little more blocky / pixelated than we'd like such as backpack, bucket & waterskin, flask/tankard".
  - 13: "DIre wolf & wolf are a bit too thin & bodily are too high off ground/legs are too long, kinda awkward looking", "Cat are too pixelated", "Horse dimensions have a weird lump near the throat & near the butt too, the whole horse body is too circular".
  - 15: "Make dawnwarden & gravefang wider blade"; 16: "Can we do the voxel style with the stained glass background? I like how defined the voxel is(A) but also enjoy the theme of (D)".
  - 17: "I attached screenshots of clipping in poses that needs to be fixed(shoulder with cape, on wizard legs, on models legs in bending in general" (six screenshots, passed to agent N); 21: "B".
  - "the ones I ignored are approved": 10, 14 and 18. He hasn't looked at the engine, the animation plan, the suspicion tiers or the smaller questions yet.
  - The manifest records it (`update_2026-09-30v_reviews10_14_18_approved.py`: 204 assets approved), the pages of 10, 14 and 18 say approved, and the home page's labels follow. Agents F, G, H, L, M and N were resumed for round 2, and S for the deck.
- 2026-09-30: shared change: `gear2` (review 19) now loads with the kit, like the other gear modules. It adds 5 parts and 68 `g2_` colours and replaces nothing; the heroes are identical, and the catalogue lists 221 parts.
- 2026-09-30: agents T (review 24: treasure, trade goods, food and drink, coins) and U (review 25: artisan tools, gaming sets, instruments) started, from `agents\T_U_reviews24_25_treasure_tools.md`, with Peter's review 12 note on blocky shapes in their brief. Waiting on round 2: I08's livestock (on review 13's animals) and I10's packs (on review 12's backpack).
- 2026-09-30: round 2s in: agent F (review 11: the standing pillory, both looks; manifest names and notes updated; 3 renders queued), agent L (review 15: Dawnwarden 7 voxels wide, Gravefang 5, like a cinquedea; 11 renders queued) and agent M (review 16: A's voxel figures on leaded glass in three variants, the recommendation variant 1 with plain glass at 24 px; no renders, **published**). Lessons from F, L and M added. Render jobs now queue through a lock, one Blender at a time, after review 19's run.
- 2026-09-30: **review 19 published** (utility gear, writing things, foci and holy symbols, by agent Q): 228 renders with no failures, checked by eye (no faces; the shield emblem sits right on the back); 51 in review and 51 second looks added, 2,622 assets; page clean for spoilers; home link, thumbnail, catalogue. Its questions: which gods the party follows (the Morninglord and Kelemvor are stand-ins), the reliquary as a casket or a pendant, glowing foci.
- 2026-09-30: **review 15 round 2 published** (wider blades): 11 renders, checked by eye against round 1; page clean for spoilers, every image rendered. Dawnwarden 7 voxels wide stepping to its point, Gravefang a cinquedea 5 voxels wide. The page asks: blades broad enough?
- 2026-09-30: **review 11 round 2 published** (the standing pillory): 3 renders, checked by eye against round 1; page clean for spoilers. Two posts, a split board at neck height, a neck hole and two wrist holes; the second look holds two people. The page asks: one post or two, and the second look as it is?
- 2026-09-30: agent H finished review 13's round 2: horses rebuilt from lofts (level back, flat sides, no lumps at the throat or rump), and the warhorse and pony too, since they share the body; the wolf and dire wolf on their own heavier, lower build (Strahd's wolf form untouched); the cat shaped by hand at 1.25 times life size (the page asks). Regression clean. 19 renders queued. H's lessons added.
- 2026-09-30: **review 13 round 2 published**: 19 renders (14 creatures, 5 lineups), checked by eye against round 1; page clean (its two mentions of Strahd are his approved review 6 forms).
- 2026-09-30: agent H resumed for review 26, the four livestock that waited on review 13's reworked bodies.
- 2026-09-30: Peter's verdicts recorded: manifest (`update_2026-09-30_approved_11_13_15_16_19_20.py`: 257 assets approved; the dire wolf's two looks held for round 3), the pages of 11, 15, 19 and 20 marked approved, the home labels, the scope plan. His two wolf pictures (a side-on wolf in snow, a snarling head) went to agent H as design references; the beasts stay without eyes. Agent M resumed for the 186 icons: A's figures over glass as present as D's (his screenshot of D's row), in A's frame for the kind of icon, plain glass at 24 px.
- 2026-09-30: all seven running agents (G, H, M, N, S, T, U) were cut off by the API usage limit mid-task; when it reset, Peter said to continue, and each was resumed from where it stopped, asked to check any file it was writing first.
- 2026-09-30: **review 22 published** (the Tarokka common deck, by agent S): 40 cards and the back, woodcut, drawn without Blender; each card checked against the library's named people; page clean for spoilers (its one "Strahd" is the book's title); 41 assets in review; home link and thumbnail. S's lessons added. S goes on to the high deck (review 23).
- 2026-09-30: agent G finished review 12's round 2: the four named items and, with the same eye, the barrel, basket, sack, pouch (its pale toggle read as a face; now a brass button), glass bottle, jug and pitcher (one run of glaze, not two), iron pot, lamp, the lanterns' vents (the bullseye's two front vents read as a face) and the mess kit; six floor versions laid square. Regression: heroes 0, review 19's 228 and review 20's 105 identical. Known drift: review 18's approved spice pouch is built on the gear pouch, so a rebuild would differ (its library copy is untouched); the Tarokka cards' shared props changed too (agent S told). Manifest notes updated, lessons added, renders queued.
- 2026-09-30: **review 27 published** (the class, subclass and condition icons, by agent M): 56 icons over glass as strong as D's in A's frames, plain glass at 24 px; checked by eye (no faces; M caught a "smiling sun" behind the life domain's heart); page clean for spoilers; manifest (U04 12, U05 29, U06 15 in review); home link and thumbnail; M's lessons added. Review 16's page now says the style is decided. M goes on to 28 and 29.
- 2026-09-30: review 22 touch-up: the Philanthropist's barefoot boy (card 22) had a mop of brown hair like a village child of review 9; agent S gave him short black hair and rebuilt that card, the Coins sheets and the deck picture; thumbnail refreshed. S also checked the four cards that use review 12's redrawn props (Trader, Merchant, Tax Collector, Miser): already built with them, whole, unchanged.
- 2026-09-30: **review 12 round 2 published**: 73 renders, checked by eye against round 1 (clearly cleaner: straight sides, even rims, flat tones; the pouch's toggle and the bullseye's vents no longer read as faces); every image current on the page.
- 2026-09-30: **review 17 round 2 published** (agent N): the clipping Peter found is fixed across all 19 rigs in nine poses (a kneel added); sunk cloth points fell from 154,817 to 34,916, mostly hidden folds. Checked by eye against round 1 and in Blender: Ireena 51 bones, Mad Mary 54, the wizard 53, the fighter 31; all import with their skins, sockets and armature, and a bent knee carries every lower skirt chain. Regression clean (heroes, review 9's sixteen, the village women). N's lessons added. Known: capes now lift with the arm like a wing, and a kneeling back leg carries the hem like a sleeve.
- 2026-09-30: **review 28 published** (the spell icons, by agent M): 66 icons, each following what v2's version of the spell does; checked by eye (no faces); page clean for spoilers; manifest (U07 66 in review); home link and thumbnail; M's lessons added. M goes on to review 29.
- 2026-09-30: agent U finished review 25 (artisan tools, gaming sets, instruments): 31 items and 31 second looks, 62 floor versions, 6 demos (lute, lyre, drum, flute, horn, the carpenter's tote), 4 lineups, sprites; regression clean. Shared change: kit.tools now loads with the kit (1 part, 144 tl_ colours, nothing replaced; heroes identical; 222 parts). Rendering now (about 134 runs). U's lessons added.
- 2026-09-30: **review 29 published** (the action and class feature icons, by agent M): 64 icons; all 186 icons are now drawn (reviews 27 to 29, 558 pictures, rebuild pixel-identical). Checked by eye (no faces, no skulls); page clean; manifest (U08 64 in review); home link and thumbnail; M's lessons added. Agent M is done.
- 2026-09-30: agent O resumed for M20, the uncommon magic items (reviews 30 and 31), and agent H given review 32 (I07: mounts, tack, barding, land vehicles) after its current work. Review numbers so far: 23 (high deck, S), 24 (T), 25 (U), 26 (livestock, H), 30 and 31 (O), 32 (H).
- 2026-10-02: after the weekly usage limit reset, Peter asked for a status report first, then reviewed. Recorded: the manifest (`update_2026-10-02_peter_verdicts.py`: 339 assets approved, after review 25's own script), the pages and home labels, `decisions.md`, SCOPE_PLAN. Asked him two clarifications: joints (soft bends, confirmed over the plan's 2a) and the warding sign (c: clutch a charm and spit); and the village layout ("strict to the book ... Vanilla plus").
- 2026-10-02: **review 23 published** (the Tarokka high deck, by agent S): 14 cards, woodcut, drawn without Blender; page clean (its one "Strahd" is the book's title); 14 in review; home link. **Review 25 published** and approved at once (Peter approved it from its build): 31 in review then approved, with 31 second looks.
- 2026-10-02: agent H handed back the dire wolf's round 3 and review 26 (sheep, pig, cow, ox, each with a second look; regression clean); rendering now. Agents started or resumed: A (phase 2), E (the village kit), N (soft bends), P (sealed Chapter One), H (review 32); T and O resumed after the limit. Agent S is done.
