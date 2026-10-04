# Workboard

The live board for Dungine.v3 and the asset library work that feeds it. The main session keeps it; agents write only
their own reports. Newest log entries at the bottom.

**Working mode (Peter, 2026-09-30):** "keep working continuously / prompt me for reviews as you go but unless I stop you on
a part keep going".

## Running now

| Who | What | Output |
|---|---|---|
| Main session | Coordinating: renders (one Blender at a time, queued through a lock), publishing, this board. Render queue: empty. | this board |
| Agent A | Phase 4 round 3: the trees and flora (village_flora.json, instanced), then the remaining gaps as they land; capture only when wholly pixel3d | the v3 game files, `research\pixel3d_swap\`, `research\pixel3d_village\` |
| Agent E | The title screen's ground and planting, an open-grave tile; review 44 rendering | library `pixel3d\reviews\33_...` to `36_...`, `43_...`, `44_...` |
| Agent O | Round 2 of the magic gear: 31 (boots of elvenkind, the giant's belt), 37 (+1/+2/+3 grades each distinct, elven chain, armour of resistance, giant slayer), 38 (boots of speed, wings of flying), 40 (demon armour, dwarven plate), 41 (armour of invulnerability, plate of etherealness), and every rare-or-better weapon of 37, 40 and 41 wider and more unique | library `pixel3d\reviews\31_...`, `37_...`, `38_...`, `40_...`, `41_...` |
| Agent P | A spoiler audit of what Peter can read (counts only), then sealed builds rounds 2 to 5 (`agents\P_later_chapters_split.md`) | `pixel3d\sealed\later\`, `plan\briefs\later_surface.md` |
| Agent G | Done: review 45 published | library `pixel3d\reviews\45_water_crossings\` |
| Agent M | Done: darker castle pass (about 53); re-rendering | library `pixel3d\reviews\46_...` |
| Agent S | The village gates for the title screen (v3 gap) | library `pixel3d\reviews\47_...` |

## Waiting for Peter

- **The cast and strike rework in v3** (agent A, GIFs sent 2026-10-03): cast A the throw, cast B the push, the guard
  stance with its left-right-stab combo.
- **The look test** (`research\look_test\index.html`, by agent A): (a) crisp at native resolution or (b) pixelated at
  half or a third; outlines or none. Agent A suggests (a) with outlines.
- **The suspicion design, a draft** (`design\barovian_suspicion.md`, by agent C): five tiers; section 4 has 11
  questions. Its warding sign is decided: clutch a charm and spit.
- **The engine plan's other questions** (`research\engine_decision.md`): how the friends play, the look, the character
  creator and race list, what replaces the premade pilot party, fog.

## Next up

- Agent O's, E's and A's rounds as they finish: render the changed models through the queue, publish, prompt Peter.
- After E02 and E03: E04 (water and crossings) and E05 (atmosphere and weather).
- The kit's bounded painting (agent E's finding: 27 times faster at 1.5 cm, with the same voxels), adopted with a full
  regression once the build agents are idle.
- On hold: T01's gothic trinkets (they need the book's table); the big ships (review 32).

## Done

- 2026-10-03: **phase 2 done** (agent A): v2 backed up (`E:\Backups\dungine.v2_2026-10-02.zip`, 502 MB, 2,032 files, CRC-checked; v2 never written to) and forked into v3 (275 files, byte-identical; captures left out; tools repointed at v3; product name "Dungine v3 - The Curse of Strahd" so saves don't collide). Compiles clean; v2's own tools pass (tour of 12 areas, dialogue, an AI-played fight). Capture: `research\phase2\`. **The look test done** too: glTFast 6.14.1 (Peter's OK), a voxel shader, 15 library models in the village; 30 library figures cost about what 30 of v2's do (8.0 against 8.3 ms).
- 2026-10-03: Peter approved review 24 ("good") and review 26 but the pig ("make the pig actually pink & give their swirly tails"); the dire wolf's body is right, its head goes to round 4 ("more wolf shaped").
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
- 2026-10-02: agent T finished review 24: 72 items (18 gems, 24 art objects, 17 trade goods, 8 foods and drinks, 5 coins), 72 second looks, 140 floor versions, 10 demos, 7 lineups; coin emblems struck flat (raised ones read as faces); regression clean. Rendering now (301 runs). Shared change: kit.treasure loads with the kit (5 parts, 249 tg_ colours, nothing replaced; heroes identical). T's lessons added.
- 2026-10-02: **the dire wolf's round 3 and review 26 published**: 12 renders, checked by eye (the side sprites show round 2's deep belly gone, a tucked waist); review 26's manifest (4 in review, 4 second looks, 2,657 assets), home link and thumbnail; page clean.
- 2026-10-03: the previous session ended mid-work (the render job died at a process fork with 0xC000026B, a logoff code, after 21 of review 24's 301 renders; its lock was left behind and cleared). State found: agent A had backed up v2 (`E:\Backups\dungine.v2_2026-10-02.zip`, 502 MB), copied it into v3 and made two commits, without the capture yet; O had begun `kit\magic_uncommon.py`; N had begun the soft bends; P had begun; E and H had written nothing yet. All six resumed; review 24's renders restarted. Manifest: review 24's own script, then `update_2026-10-03_peter_verdicts.py` (144 + 6 approved; the pig and the dire wolf held).
- 2026-10-03: Peter saw agent A's Play test of the fork (v2's art) and asked why; explained. He OK'd the glTFast download ("you can do what you gotta"); agent A goes on to the look test after phase 2.
- 2026-10-03: agent N finished the soft bends: all 19 rigs re-exported with soft elbows and knees (wrists and ankles stay cut), knee cops bend with them, the skirt's knee handover re-tuned to 3 voxels; every GLB passes --check; regression clean (21 figures). Cost: leaning limb faces 2,457 to 67,265 across all poses; clipping about the same; triangles up 3-4%. Review 17's page keeps Peter's approval, with a soft-bends section added. Blender check queued after review 24's renders.
- 2026-10-03: agent H did Peter's two notes: the dire wolf's head redrawn (a domed skull, a stop under the brow, a long tapering muzzle, a heavy jaw line, tall wide-set ears; body unchanged), and the pig pink with a curly corkscrew tail (pig_2 stays ginger, also curly). Sprites checked by eye: both read. Regression clean (review 13's other 38 models and the other livestock unchanged by hash). Renders queued behind review 24. H goes on to review 32.
- 2026-10-03: Peter on the dire wolf's head: "wolf faces are wider" (two more wolf pictures) and "no eyes but teeth/fangs are okay". Round 5 to agent H: a wider face, a snarl with fangs allowed, and the same wider head offered for the approved wolf as a proposal. The queued round 4 renders were cancelled; the pig renders run on their own. The fangs rule is in the lessons file's style decisions.
- 2026-10-03: render note: background tasks are cut at about 30 minutes, but the killed task's render loop keeps running on its own (review 24 at 89 of 301 at 01:58, about 20 s a render with seven agents busy). A watcher reports when it ends; the soft-bend Blender check and the pig renders wait behind it on the lock. Long render runs should be split into chunks of about 80.
- 2026-10-03: agent P built Chapter One's creatures sealed (no pictures, no page; counts and contents kept in the sealed folder); manifest script run, the sealed README updated in neutral words, the plan page lists none of them; regression clean. The location (L01) waits for the village kit's first review.
- 2026-10-03: agent O built review 30 (uncommon magic arms, armour, potions, rings, rods, staffs, wands): 53 items, 53 second looks (38 on review 5's mannequin), 106 floor versions, 10 demos, 8 lineups; glows within limits; regression clean. Renders queued (230). Its questions for Peter: the +1/+2/+3 marks, the sentinel shield's watchtower, review 4's sickle (its blade stands clear of the handle), the poison potion's tell, the mithral and adamantine colours. O goes on to review 31. kit.magic_uncommon joins the kit's imports only when O is done (a half-edited module would break every build).
- 2026-10-03: agent E built review 33 (the village houses, shop and tavern fronts, at 1.5 cm): 63 modules with second looks (126), 6 assembled examples with the review man, 16 sign sprites; bounded painting in its own kit/env.py (under 2 s a module); regression clean. Renders queued (126 modules, 6 big examples needing 5 to 6 GB each in Blender). E goes on to review 34 (church, undercroft, tower), then 35, then the village layout as review 36. Agent P started the sealed location (L01) from E's modules. E's lessons added.
- 2026-10-03: Peter on review 33: "make the houses more gothic looking, theyre not bad right now". Round 1's queued renders cancelled; agent E does round 2 (steeper roofs, pointed openings, darker timber and soot, stone ground floors, iron) before review 34, and carries the feel into 34 and 35. Agent P holds the sealed location's architecture until the gothic kit lands.
- 2026-10-03: agent A's phase 2 and look test reported; Peter sent the pictures. A goes on to G2 (rigged figures through v2's HumanoidRig); N to the library's game export (metal, no disc, AO, LODs).
- 2026-10-03: **review 24 published and approved** (Peter had approved it from its build): renders done, page marked, home link and thumbnail.
- 2026-10-03: overnight the queue finished: review 24 (published, approved), the soft-bend Blender check (19 rigs: weights sum to 1, at most 4 influences, chains and blended knees present; bends, sitting and arms up deform cleanly), the pig, the dire wolf's round 5 and the wolf proposal, review 30 (230 renders, none failed). Published: the dire wolf's round 5, the pig's round 2, review 30 (53 in review, 53 second looks, 2,782 assets). After the usage limit reset, Peter said "review 24 is good continue on other work"; all six agents resumed.
- 2026-10-03: agent P has the sealed location planned and its builder written (it takes the architecture from agent E's modules by name), tested in memory only; it waits for E's gothic round 2 before building into the sealed folder.
- 2026-10-03: **library figures move in v3** (agent A, G2, commit d713792): Bildrath, Arik, Ireena and the fighter imported with their skins; an adapter fills v2's HumanoidRig so v2's own animator drives them (walk, run, idle, cast, strike, fall); spring chains run hair, dresses, aprons and capes; Bildrath strolls the square (v2's villagers only stood). Captures in research/rigged_figures/, sent to Peter. Rig findings (aprons splitting between the legs, flicking hems, the cape riding the arm) passed to agent N. A goes on: more Chapter One people, props and weapons on the sockets, crowd cost.
- 2026-10-03: Peter on the motions: "fall & walk are good"; cast and strike need work. Cast: elbows in; A, a throw from under the armpit to full extension with the other arm held out for balance; B, a push from the chest like a ball. Strike: a fighting stance, the left arm out level at chest height as a guard, the right hand striking: swing left, swing right, stab forward. Sent to agent A, ahead of its other steps.
- 2026-10-03: agent O built review 31 (the 67 uncommon wondrous items): 67 and 67 second looks (44 on the mannequin), 134 floor versions, 9 demos, 9 lineups; lens pairs kept in cases, figurines eyeless, the dog with a wide skull; regression clean. Renders queued (286). M20 is complete (reviews 30 and 31). Shared change: kit.magic_uncommon and kit.magic_uncommon_wondrous now load with the kit (15 parts, 232 unc_ colours, nothing replaced; heroes identical). O's lessons added.
- 2026-10-03: agent O started M30 (reviews 37 and 38) and agent G the equipment packs (review 39). The gothic trinkets (T01) stay on hold: their designs need the book's table entries, which aren't copied.
- 2026-10-03: agent E built review 33's gothic round 2: 2:1 roofs (about 63 degrees), carved barge boards with finials, pointed doors and lancets, soot-black oak and darker plaster, stone ground storeys, chimney pots in threes, dormers, sagging ridges, iron cresting; 87 modules with second looks, 8 examples (a narrow gabled house added); previews on the page, sent to Peter. E now splits the roof slopes into pieces of 3 m or less before the renders; agent P builds the sealed location from the gothic kit.
- 2026-10-03: agent N built the library's game export (pipeline/game_export.py, into pixel3d/export/game/): metal, roughness and glow in a point-sampled atlas; review bases left out; voxel AO in vertex colours; LODs at 1.5, 3 and 6 cm in one file, bound to one skin; skirts, robes and aprons on centre-front and centre-back chains from Hips (game export only; the approved library rigs keep their weights). The streaks: step sides; shader fix suggested. 19 rigs and 15 statics exported; library unchanged (21/21). Blender check queued; agent A told to switch v3 to the export after the cast and strike rework.
- 2026-10-03: agent E split review 33's long pieces (roof slopes, the 6 m and 6.6 m gables, the dormer) into pieces of 3 m or less, cut from the whole builds so they rejoin voxel for voxel (all 8 examples identical); 115 modules, 230 models, every one with a .vox. Renders queued (230 modules, then the 8 examples at 6 to 8 GB each). E goes on to review 34.
- 2026-10-03: agent G built review 39 (the seven equipment packs): each closed with telling items strapped on, and opened on the floor with every item laid out in its approved loot state (voxel-identical to the library copies); second looks; 28 models, a lineup, sprites; regression clean. Renders queued (29). Its questions: the rope coil read as a ring, the floor versions' size (up to 1.1 x 2 m), the folded clothes.
- 2026-10-03: agent O built review 37 (rare magic arms, armour, potions, rings, rods, staffs, wands): 52 and 52 second looks, 104 floor versions, 8 demos, 5 lineups; regression clean; the +1/+2/+3 marks kept in one place for Peter's answer. Renders queued (221). O goes on to review 38. Lessons from O (37) and G (39) added.
- 2026-10-03: the usage limit cut agents A, E, O and P mid-task; all resumed when it reset. The render queue had finished everything meanwhile: reviews 31, 33 round 2, 37 and 39 published (manifest: 2,910 assets), and the game export's Blender check passed (three LODs, AO, no base). Agent H built review 32 (28 assets with second looks: donkey or mule, camel, elephant, 12 bardings on the warhorse, tack, six vehicles; the four big ships held, the keelboat asked); rendering now.
- 2026-10-03: agent P built Chapter One's location sealed, from agent E's gothic kit (no pictures, no page; contents kept in the sealed folder); manifest script run, the sealed README and the plan page note neutral; regression clean. Agent P is done.
- 2026-10-03: **review 32 published** (mounts, tack, barding and vehicles, by agent H): 105 renders, none failed; 28 in review with 28 second looks, the five ships to needs_decision; home link and thumbnail; page clean (its Barovia mentions are geography). Its questions: the mule inside one asset, dromedary or Bactrian, barding under the saddle, the exotic saddle, the keelboat.
- 2026-10-03: agent O built review 38 (the 51 rare wondrous items): 102 models, 102 floor versions, 6 demos, 8 lineups; robe of eyes with silver triangles, not eyes; regression clean. Renders queued (218). M30 is complete. kit.magic_rare and kit.magic_rare_wondrous now load with the kit (10 parts, 185 rare_ colours; heroes identical; 252 parts). O goes on to M31 (very rare, review 40). O's lessons added.
- 2026-10-03: agent E built review 34 (the gothic church, undercroft, bell tower and spire): 100 modules with second looks (200 models, all under .vox's limit), 7 examples (the whole church and tower at 3 cm, close-ups at 1.5 cm); a 2:1 slate nave roof (ridge 15.6 m), a 7-stage tower with a louvred belfry above the nave ridge and gargoyles (teeth, no eyes), a 4:1 spire to 30 m with an iron sun; regression clean. Renders queued. E goes on to review 35, then the village layout (36).
- 2026-10-03: agent A: cast and strike reworked to Peter's notes (cast A the throw for bolt spells, cast B the push for area spells, a guard stance with a left-right-stab combo; v2's heroes get them too; staves pass to the steadying hand), v3 switched to the game export (LODGroup, a voxel atlas shader with wrap diffuse softening the streaks; hem flicks gone; centre cloth hangs between the knees when seated), and Chapter One's people placed as library figures with library weapons. Crowd: 30 library figures cost 9.2 to 10.4 ms against v2's 8.1, mostly cloth and hair springs. One README line named what the house's figures turn out to be; reworded neutrally (left for A to commit). GIFs sent to Peter. Still open: the cape over a raised arm (agent N), the look test's static figures.
- 2026-10-03: **review 38 published** (the rare wondrous items): renders done, page clean; 51 in review with 51 second looks; home link and thumbnail.
- 2026-10-03: agent N gave caped rigs their own arm chains in the game export (cloth over an arm hangs from the shoulder, the upper arm as collider; the fighter, Ireena, Ismark, the ranger re-exported; library rigs byte-identical; regression clean). Agent A re-tests in v3 with the real springs.
- 2026-10-03: agent E built review 35 (the manor, walls, fences, gateposts, cobbled square, well, market stall, notice board, headless statue), in the gothic of 33 and 34: 83 modules with second looks (166 models), 7 examples; the manor's roof cut off flat with a lead deck so it stays below the church; the statue from the character kit turned to stone; regression clean. Renders queued. The village kit (E01) is complete; E goes on to the layout (review 36), following v2's positions with a neutral reserved plot for the sealed house.
- 2026-10-03: agent O built review 40 (the 53 very rare items, one review): 106 models, 106 floor versions, 8 demos, 8 lineups; regression clean. O also found eight props in reviews 31, 37 and 38 clipped by their grids, and widened them; one queued job renders review 40, then rebuilds and re-renders those eight and their lineups. kit.magic_very_rare loads with the kit (heroes identical). Review 18's approved rope of mending is clipped by one voxel; left as approved.
- 2026-10-03: **review 34 published** (the gothic church, undercroft and tower): renders done, checked by eye (church, tower and belfry right; the undercroft example renders hidden under the ground plane, sent back to agent E); 2 kits in review with second looks; home link and thumbnail.
- 2026-10-03: agent E built review 36 (the village laid out from the kit, following v2's AreaVillage): 21 buildings within 18 cm of v2's centres, 3,031 placements in landmarks/village_of_barovia/village_set/ (a day and a night dressing), the square, fences, the graveyard and the manor yard; a neutral reserved plot; a 15 cm overview. The undercroft example lifted for its picture. Both renders queued. E01 is complete (reviews 33 to 36). E goes on to E02 terrain (review 43) and E03 vegetation (review 44).
- 2026-10-03: agent A round 2 (commit d72bbc7): the cape fix confirmed in the game with real springs (no red sleeve; the tuck clean); cloth and hair cost for 30 figures cut from about 2.3 to 0.4 ms (a rewritten pass, Burst jobs, updates throttled by detail level); the road's static figures straight from the export. Next: agent N exports the village kit for the game; agent A then puts the gothic village into v3 from review 36's layout, before and after captures.
- 2026-10-03: **review 35 published** (the manor, walls, fences, gateposts, the square, well, stall, notice board, statue): renders checked by eye; 10 kits in review with second looks (3,001 assets); home link and thumbnail. Agent H is idle (review 32 done); agents G and M too.
- 2026-10-03: **review 36 published** (the village laid out): page clean, the village set in review (21 buildings, 3,031 placements), home link (thumbnail from the preview: the render frames the village small in warm light; agent E to put the preview first). The undercroft's lifted render is right.
- 2026-10-03: agent O built review 41 (the 36 legendary items and 12 of the 13 artifacts): 96 models, 96 floor versions, 6 demos, 7 lineups; the doubled-grid clipping check from the start; regression clean. One artifact tied to the Curse of Strahd story left out, unbuilt and unnamed in anything Peter reads. Renders queued (205). kit.magic_legendary loads with the kit. All the generic magic items are now built (M10, M20, M30, M31, M32, M40). Agent O is free.
- 2026-10-03: agent N exported the village kit for games (596 modules in both looks, four LODs at 1.5/3/6/15 cm, day and night layouts with glTF placements, 42 far meshes; python game_export.py --village redoes it; LOD0 31.4 M triangles with AO, 14.4 M without). Usage limit reached: agents A (the village in v3, waiting on this export) and E (terrain and vegetation) paused; on resume, tell A the export is ready (README village section).
- 2026-10-04: Peter reviewed 23 to 39 and the wolves: approved 23, 30, 32, 34, 39 in full, and 26, 31, 33, 35, 37, 38 but named items (the black pig; boots of elvenkind and the giant's belt; dark worn second looks for 33 and 35, his choice when asked; the +1/+2/+3 grades each distinct, elven chain, armour of resistance, giant slayer; Flash-style boots of speed and a wing-cloak). The manor on a hill (36). The wolf takes the wide head; the dire wolf gets round 1's flared neck back. Manifest: `update_2026-10-04_peter_verdicts.py` (529 approved). Sent: agent O (31, 37, 38), H (wolves, pig), E (second looks, the manor's hill, then terrain and woods), A (the village into v3). New standing rule: dark, worn gothic, second looks included.
- 2026-10-04: Peter reviewed 40 and 41 (from the previews): approved but demon armour (spikes, horns, symbols), dwarven plate (dwarven design, four reference pictures), armour of invulnerability (a light blue aura), plate armour of etherealness (godly or ethereal), and every rare-or-better weapon, 37's included (wider, more detailed, each more unique). Manifest: update_2026-10-04b_peter_40_41.py. Reviews 40 and 41 published with links and thumbnails. All sent to agent O.
- 2026-10-04: agent H finished the wolves and the pig: the wolf and its second look take the wide head Peter chose; the dire wolf (round 6) gets round 1's flared ruff on the approved body with round 5's head and snarl (0.78 m across the mane); the second pig is black (round 1's slate-black, darker bristles). Regression: heroes 0 differences; review 13 rebuilds all 41 (only the four wolves and the lineup changed), 26 all 8 (only pig_2), 32's horse kin unchanged. Rounds archived in each review's round5\ and round2\. Renders queued.
- 2026-10-04: **reviews 13 (round 6) and 26 (round 3) published**: the wolf's wide head, the dire wolf's flared ruff (round 1's) on the approved body, and the black pig (round 1's slate). Renders checked by eye, pages spoiler-clean, home labels set.
- 2026-10-04: Peter on the later chapters: show the surface, seal the secrets ("you should be getting a taste of my design choices and what im going for overall"). Recorded in decisions.md, SCOPE_PLAN section 8 and memory. Started: agent P (the split, then sealed builds; counts-only reports), G (E04 water and crossings, review 45), M (E06 castle set, review 46), S (E06 wild set, review 47).
- 2026-10-04: Peter: "review 13 - design is good lets just skip the teeth"; "review 26 - approve". Review 26 approved in full (update_2026-10-04d_peter_26.py run; home label approved). Agent H closes the dire wolf's mouth and marks both pages approved; review 13's manifest script (update_2026-10-04e_peter_13.py) waits for the render. Lesson: beasts default to a closed mouth.
- 2026-10-04: **review 13 approved in full**: the dire wolf's closed mouth rendered and checked by eye (round 6's snarl archived in round6\); update_2026-10-04e_peter_13.py run; pages for 13 and 26 read approved in full; home labels set. Agent H done.
- 2026-10-04: Peter rejected agent A's village captures: "All the data captured must be with the new models & textures in pixel3d style otherwise nothing matters" (only the buildings were the library's). New standing rule: nothing from the game goes to him until everything in frame is pixel3d. Agent A to phase 4 (inventory, swaps, gaps; capture only when wholly pixel3d). Agent G finished review 45 (water and crossings: 62 models, 10 examples); renders queued. A usage limit stopped O, P, E, M and S; all resumed. O cleared to write its round 2 into the library.
- 2026-10-04: review 45 rendered (72 pictures, 0 failures) but held back from Peter: the stone bridges, causeway and drawbridge read pale and clean against his dark, worn gothic rule. Agent G is doing a darker, worn pass. Agent E told its terrain reads pale and dry under the studio light (Barovia's ground: dark, damp, muted).
- 2026-10-04: agent P finished the later-chapters split: the manifest script update_2026-10-04c_later_split.py run (215 surface entries planned, the rest sealed; 26 surface entries made neutral). The spoiler-free surface plan is plan\briefs\later_surface.md (proposed reviews 48 to 63 plus people brief sets). Sealed round 1 built; P is doing a counts-only spoiler audit of everything Peter can read, then sealed rounds 2 to 5. P's lessons added.
- 2026-10-04: Peter: magic gear second looks are aged relics (tarnished silver, dulled gilt, blackened steel, worn grips; glow stays). Magic round 2 rendered but held: weapons still flat, wings and etherealness off; agent O doing 5 prototypes first, then the rollout with relic second looks. Renders queued: 45 (darker), 33/35 second looks, 36, 43, 47, 46. Gaps from agent A's phase 4 handed out: N (ground, village re-export, Arkus/Chai'rn and villager rigs, prop exports, cheap cobbles), F (separate gravestones), J (garlic and claw marks), H (broken cart, then review 54 creatures), M (HUD buttons), E (rocks, stumps, logs in 44). Agent P: split applied, spoiler audit done (plan_page phase label fixed by main), sealed rounds 1 to 5 built. Open: Dulandir (Peter), skyline castle and village gates (after 46, review 49).
- 2026-10-04: **published**: review 45 (water and crossings, the darker round; home link), 33 and 35 (dark, worn second looks), 36 round 2 (the manor on a hill). Manifest: update_2026-10-04_second_looks_dark.py, update_2026-10-04_review45_water.py (3,111 assets). Review 47 rendered but 38 below-ground pictures failed (their lifted copies lacked the json render_vox reads); sidecars written, the missing pictures re-queued. Review 46 rendering; 43 queued. All agents stopped by the usage limit resumed (O, N, F, J, H, M, E).
- 2026-10-04: agent M made the five HUD button icons (inventory, character, journal, camp, settings) in the review 27 to 29 style; added to review 29's page, waiting for Peter; manifest update_2026-10-04_hud_icons.py run (batch U09). Agent F split the approved four gravestones into single models (review 11 addition), queued to render. Agent H built the broken cart (review 32 addition), queued; H now on review 54.
- 2026-10-04: Peter: "I approve of review 29 icons": the five HUD buttons approved (update_2026-10-04g_peter_hud_icons.py). Agent M to mark the page; agent A can swap them in.
- 2026-10-04: broken cart (review 32 addition) rendered and published; manifest run (3,118 assets). Agent N exported for the game: terrain (144 modules, texture AO), the village ground (22,264 tiles, far meshes), the village re-run (manor on its hill, 3,109 placements), Arkus, Chai'rn and four villager rigs, seven props, cheaper cobbles; README in export/game. N was blocked by the permission check on three steps (a prop re-export overwrite, deleting four stray partial castle export folders, editing the --village default); raised with Peter, not done by main. Review 46 held: stone too pale and clean; agent M doing a darker, worn pass. Agent A on phase 4 round 2 (swapping N's exports and the HUD buttons).
- 2026-10-04: published: review 43 (terrain; tiles measure about 57 brightness, the studio light reads them tan), review 47 (the wild set, ruins and temple darkened in round 2), the review 11 addition (the four gravestones as single models, in review). Agent O's prototypes rendered and passed by main with three notes (flame tongue's wave, the wings' feathers, the etherealness fade); full rollout under way. Agent E built review 44 (vegetation: 72 modules plus second looks, far tree versions, rocks, stumps, logs, and village_flora.json with 4,005 placements); rendering. Agent N exporting vegetation, the flora list, the single gravestones and the broken cart.
- 2026-10-04: Peter's hero notes: Arkus gets a royal greatsword (agent O, first); Dulandir briefed (half-elf bladesinger, lanky, wild grey hair, scholar-arcanist robe open, hood down; elven rapier with a magical-science look), agent H builds him as review 42 'The party'; Chai'rn no changes; Arkus and Chai'rn approved into v3's party. Agent M's darker castle pass (about 53 brightness) re-rendering. Agent N exported the vegetation (144 modules; trees 513M triangles at LOD0, 35.6M at LOD2), village_flora.json with instancing, the single gravestones and the broken cart.
- 2026-10-04: rows
- 2026-10-04: agent A phase 4 round 2 (commits cb8dc98, 09a06b9): the library ground and roads, the village relaid on its hills, Arkus and Chai'rn rigs (Arkus empty-handed for now), the villager rigs, props, full-detail cobbles and the HUD buttons are in v3; all 22 doors reachable; 9.5 to 23 M triangles per view, GPU 2.7 to 5.2 ms. Still v2's: trees (A on them now), Dulandir, the title screen's ground, the castle and village gates, the open grave. Sent: E (title-screen ground and planting, an open-grave tile), S (village gates), N (Chai'rn's staff, the clawed doors, the garlic).
