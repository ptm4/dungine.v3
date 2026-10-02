# Agents T and U: build reviews 24 (treasure and trade goods) and 25 (tools, games and instruments)

Status: started 2026-09-30.

## Brief (the prompt as given; the same for both, with the batch filled in)

You're building a batch of Peter's voxel "3D pixel art" asset library, as its own library review.
- Agent T builds batches I09 and I08 as review 24: gems and art objects (42), and trade goods, food and drink, and coins
  (30). I08's four livestock wait for review 13's reworked animals.
- Agent U builds batch I11 as review 25: artisan tools, gaming sets and instruments (31). I10's packs wait for review
  12's reworked backpack.

Several agents work in parallel with the main session, so follow the rules exactly.

**Read first, fully**
- `E:\Assets\DND5E\pixel3d\pipeline\3dpixelartlessons.md`: the style bible and every lesson (glow values, speckle,
  faces, thin parts, small things, laying things flat, the newest ones from reviews 19 and 20). Obey it.
- `E:\Assets\DND5E\pixel3d\README.md`, and `E:\Assets\DND5E\pixel3d\plan\SCOPE_PLAN.md` sections 1 to 3 and 8
  (section 8's newest entry is Peter's latest review, 2026-09-30).
- The models to follow:
  - agent R's review 20: `pipeline\kit\gear3.py`, `pipeline\gear3_review.py`, `pipeline\gear3_review_page.py`,
    `pipeline\render_review20.sh`, and the page `pixel3d\reviews\20_gear_consumables_tools\index.html`;
  - agent Q's review 19: `pipeline\kit\gear2.py` and `pipeline\gear2_review.py`.
  Read them; never edit them. Reuse their helpers by importing (`gear3.hbox`, `gear2.box`, `gear2.lathe`...).
- Also: `pipeline\kit\props.py`, `pipeline\item_review.py`, `pipeline\wear_review.py` (floor versions) and
  `pipeline\sprite_from_vox.py`.
- The manifest, `E:\Assets\DND5E\pixel3d\plan\scope_manifest.json`: your batches' items and their paths.

**Peter's newest note on style (2026-09-30), which applies to you.** Of review 12's gear he wrote, "Some of these are a
little more blocky / pixelated than we'd like such as backpack, bucket & waterskin, flask/tankard". What made them so:
- ragged outlines (uneven rims, staves of uneven height, stair-stepped edges on round things);
- one-voxel bumps and notches on surfaces that should be flat or evenly round;
- per-voxel colour noise.
Build clean, deliberate shapes instead:
- straight sides and even rims;
- round things symmetric about their axis;
- regular repeats (staves, strings, keys, links);
- two or three tones in broad areas, not speckle.
Small things may be drawn larger than life to read; say which, and by how much, on their cards.

**What to build.** Every asset of your batch at its manifest `path`.
- Each item as the Player's Handbook or the Dungeon Master's Guide describes it, in the library's style.
- A second look for each (`<id>_2`), but "dont make them too irrecognizable".
- A floor (loot) version for everything portable.
- Held or worn demos where they matter, as reviews 19 and 20 did.
- Sizes are real (1 voxel = 1.5 cm). Tiny things (gems, coins, dice, cards) are drawn larger, and the page says so.
- **Agent T:**
  - Gems: cut and polished, with glints but no glow (glow is for magic).
  - Art objects: gothic and old.
  - Coins: one coin of each metal, larger than life, plus a pile or stack of each for loot.
  - Trade goods: as a merchant would sell them, e.g. an ingot or bar, a bolt of cloth, a sack, a spice jar or pouch.
  - Food and drink: clean, appetising shapes.
  - Don't copy review 12's tankard for the ale mug: Peter found it too blocky.
- **Agent U:**
  - Each artisan's tools as a set a character would carry: a roll, a case or a box, with the tools visible.
  - The gaming sets, each with its box or pouch.
  - The instruments; demos of a few being played by the review man.
- **Faces.** The library is faceless except Strahd. Art objects that show a face (a portrait, a statuette, a mask, a
  painted sarcophagus) are drawn without eyes: a veiled or turned-away figure, a silhouette, a landscape instead of a
  face, a closed locket, a mask lying so its holes don't stare. List each case on the page. Also watch for accidental
  faces in small things (the lessons have many examples).
- Keep it generic and spoiler-free.

**Where your code goes**
- New files only:
  - agent T: `pipeline\kit\treasure.py`, `pipeline\treasure_review.py`, `pipeline\treasure_review_page.py`,
    `pipeline\render_review24.sh`, and the page `pixel3d\reviews\24_treasure_trade_goods\index.html`;
  - agent U: `pipeline\kit\tools.py`, `pipeline\tools_review.py`, `pipeline\tools_review_page.py`,
    `pipeline\render_review25.sh`, and the page `pixel3d\reviews\25_tools_games_instruments\index.html`.
- New colours go through `LIB.update({...})`, with names starting `tg_` (T) or `tl_` (U).
- Don't edit any existing file. If a shared change is truly needed, stop and say so.
- Test in `%TEMP%\dnd5e_agentT\` or `%TEMP%\dnd5e_agentU\` until the build is right, then build into the library.

**Checks**
- Flat projections and sprites of everything before calling it done. Fix:
  - floating parts (settle loose bits in floor versions);
  - wrong sizes;
  - speckle and ragged outlines;
  - anything that reads as a face or eyes.
- Then the hero regression: rebuild the three heroes to your temp folder with `python -m kit.build ... --out <tmp>`, and
  compare them with `kit_diff.py`. There must be 0 differences.

**The manifest.** Write the script, but don't run it:
- agent T: `pixel3d\plan\research\update_2026-09-30y_review24.py`, guarded by `2026-09-30-review24`;
- agent U: `pixel3d\plan\research\update_2026-09-30z_review25.py`, guarded by `2026-09-30-review25`.
Each script:
- sets the batch's built items to `in_review` with the review path (T leaves I08's livestock planned);
- adds the second looks as new assets;
- recounts.
Find assets by batch and id, since ids can repeat across batches. Dry-run it on a temp copy of the manifest.

**Rules**
- Never run Blender. No downloads.
- Don't touch `E:\Unity\Projects\Dungine`, never modify dungine.v2, and don't read `E:\Assets\DND5E\pixel3d\sealed`.
- Never mark anything approved.
- Don't edit other agents' or the main session's files: every existing file in `pipeline\` and `pipeline\kit\`.
- Write in plain, direct English in anything Peter reads.

**Final reply:**
- counts built (items, second looks, floors, demos, lineups);
- doubts;
- the exact commands for the main session to run next;
- the regression result;
- proposed lessons (don't edit the lessons file).
