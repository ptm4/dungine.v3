# Agents Q and R: build reviews 19 (gear 2, G02) and 20 (gear 3, G03)

Status: both published 2026-09-30 (review 19 by Q, review 20 by R); waiting for Peter.

## Brief (the prompt as given; the same for both, with the batch filled in)

You're building a batch of Peter's voxel "3D pixel art" asset library, as its own library review. Agent Q builds batch
G02 (utility gear, writing things, arcane foci and holy symbols, 51 items) as review 19. Agent R builds batch G03
(consumables, tools and kits, 23 items) as review 20. Several agents work in parallel with the main session, so follow
the rules exactly.

**Read first, fully**
- `E:\Assets\DND5E\pixel3d\pipeline\3dpixelartlessons.md`: the style bible and every lesson (glow values, speckle,
  faces, thin parts, small things, laying things flat). Obey it.
- `E:\Assets\DND5E\pixel3d\README.md`, and `E:\Assets\DND5E\pixel3d\plan\SCOPE_PLAN.md` sections 1 to 3 and 8.
- The model to follow, agent G's review 12 (adventuring gear, G01):
  - `pipeline\kit\adventuring_gear.py`, `pipeline\gear_review.py`, `pipeline\gear_review_page.py` and
    `pipeline\render_review12.sh`;
  - the page, `pixel3d\reviews\12_adventuring_gear\index.html`.
  Read them; never edit them. Reuse its helpers by importing.
- Also: `pipeline\kit\props.py` (the flame palette, the candle), `pipeline\kit\weapons.py`, `pipeline\item_review.py`,
  `pipeline\wear_review.py` (floor versions) and `pipeline\sprite_from_vox.py` (its `crop` is fixed now).
- The manifest, `E:\Assets\DND5E\pixel3d\plan\scope_manifest.json`: your batch's items and their paths.

**What to build.** Every asset of your batch at its manifest `path`.
- Each item as the Player's Handbook describes it, in the library's style.
- A second look for each (`<id>_2`), but "dont make them too irrecognizable".
- A floor (loot) version for everything portable.
- Held or worn demos where they matter, as review 12 did: holy symbols worn or held up, foci held, tools in use.
- Sizes are real (1 voxel = 1.5 cm). Tiny things may be drawn a little larger to read in a sprite; say which on the page.
- Holy symbols: the gods' symbols as the D&D books show them. The Morninglord's sun must match the library's existing
  sun (`pipeline\kit\parts\npc_gear.py`, the pendant, and review 11's wall sun in `kit\props_religion.py`).
- Keep it generic and spoiler-free.

**Where your code goes**
- New files only:
  - agent Q: `pipeline\kit\gear2.py`, `pipeline\gear2_review.py`, `pipeline\gear2_review_page.py`,
    `pipeline\render_review19.sh`, and the page `pixel3d\reviews\19_gear_utility_foci\index.html`;
  - agent R: `pipeline\kit\gear3.py`, `pipeline\gear3_review.py`, `pipeline\gear3_review_page.py`,
    `pipeline\render_review20.sh`, and the page `pixel3d\reviews\20_gear_consumables_tools\index.html`.
- New colours go through `LIB.update({...})`, with names starting `g2_` (Q) or `g3_` (R).
- Don't edit any existing file; if a shared change is truly needed, stop and say so.
- Test in `%TEMP%\dnd5e_agentQ\` or `%TEMP%\dnd5e_agentR\` until the build is right, then build into the library.

**Checks**
- Flat projections of everything before calling it done. Fix floating parts (settle loose bits in floor versions),
  wrong sizes, speckle, and anything that reads as a face or eyes.
- Then the hero regression: rebuild the three heroes to your temp folder with `python -m kit.build ... --out <tmp>`, and
  compare with `kit_diff.py`: 0 differences.

**The manifest.** Write, but don't run:
- agent Q: `pixel3d\plan\research\update_2026-09-30s_review19.py`, guarded by `2026-09-30-review19`;
- agent R: `pixel3d\plan\research\update_2026-09-30t_review20.py`, guarded by `2026-09-30-review20`.
Each sets the batch to `in_review` with its review path, adds the second looks as new assets, and recounts.

**Rules**
- Never run Blender. No downloads.
- Don't touch `E:\Unity\Projects\Dungine`, never modify dungine.v2, and don't read `E:\Assets\DND5E\pixel3d\sealed`.
- Never mark anything approved.
- Don't edit other agents' or the main session's files: every existing file in `pipeline\` and `pipeline\kit\`.
- Write in plain, direct English in anything Peter reads.

**Final reply:** counts built (items, second looks, floors, demos), doubts, the exact commands for the main session to run
next, and the regression result.
