# Agent O: build review 18, common magic items (library batch M10)

Status: built and published 2026-09-30 (215 models, 4 lineups, sprites); review 18 approved by Peter 2026-09-30.

## Brief (the prompt as given)

You're building a batch of Peter's voxel "3D pixel art" asset library: the 50 common magic items of D&D 5e (batch M10).
It becomes library review 18. (Review 17 is the rig's stress sheet, another agent's.) Several agents work in parallel
with the main session, so follow the rules exactly.

**Read first, fully**
- `E:\Assets\DND5E\pixel3d\pipeline\3dpixelartlessons.md`: the style bible and every lesson (the glow values, speckle,
  faces, thin parts, flames on glass). Obey it.
- `E:\Assets\DND5E\pixel3d\README.md`, and `E:\Assets\DND5E\pixel3d\plan\SCOPE_PLAN.md` sections 1 to 3 and 8.
- The code, and the finished batches it's modelled on:
  - `pipeline\vox.py` and `pipeline\kit\materials.py`;
  - `pipeline\kit\weapons.py` (weapons) and `pipeline\wear_review.py` (armour and clothing on mannequins, and floor
    versions);
  - `pipeline\item_review.py` (items, held demos, lineups, sprites);
  - `pipeline\kit\adventuring_gear.py` and `pipeline\gear_review.py` (agent G's review 12: small items, floors, demos).
    Read these; never edit them.
  - `pipeline\render_review12.sh` and `pipeline\gear_review_page.py`.
- The manifest, `E:\Assets\DND5E\pixel3d\plan\scope_manifest.json`, batch M10: 50 items with their manifest paths.

**What to build.** Every M10 asset, at its manifest `path` (`pixel3d\<path>\<id>\<id>.npz` and `.vox`).
- Each item as the rulebooks describe it, in the library's style. A magic item should look magic without going garish:
  one accent, often a soft glow. Warm glows stay at 0.16 or below, cool ones at 0.5 or below.
- Clothing and armour among them go on a display mannequin, like reviews 5 and 8.
- **Second looks:** every item gets one, `<id>_2`, but "dont make them too irrecognizable".
- **Floor (loot) versions** for everything portable (`<folder>\<id>\floor\<id>_floor.npz`).
- **Held or worn demos** where they matter (weapons, cloaks, helms, boots, amulets), as reviews 4 and 12 did.
- **Scale:** 1 voxel = 1.5 cm, at real sizes. Tiny items (beads, dice, rings) may be drawn a little larger to read in the
  sprite; say which on the page.
- Keep it generic: no module spoilers.

**Where your code goes**
- New files only:
  - `pipeline\kit\magic_common.py`: builders;
  - `pipeline\magic_review.py`: build, lineups, `--only`, `--out-root`, `--sprites`;
  - `pipeline\magic_review_page.py`: the page, `pixel3d\reviews\18_common_magic_items\index.html`;
  - `pipeline\render_review18.sh`: items and floors with `item`, demos and mannequins with `hero,back`, lineups with
    `lineup`, all with `--glb`.
- New colours go through `LIB.update({...})`, with names starting `magic_`.
- Don't edit any existing file; if a shared change is truly needed, stop and say so.
- Test in `%TEMP%\dnd5e_agentO\` until the build is right, then build into the library.

**Checks**
- Flat projections of everything before calling it done. Fix floating parts, wrong sizes, speckle, and anything that
  reads as a face or eyes.
- Then the hero regression: rebuild the three heroes to your temp folder with `python -m kit.build ... --out <tmp>`, and
  compare with `kit_diff.py`: 0 differences.

**The manifest.** Write, but don't run, `pixel3d\plan\research\update_2026-09-30p_review18.py`:
- guarded by the decision id `2026-09-30-review18`;
- the 50 set to `in_review` with `review='reviews/18_common_magic_items'`;
- the second looks added as new assets;
- recounts.

**Rules**
- Never run Blender. No downloads.
- Don't touch `E:\Unity\Projects\Dungine`, never modify dungine.v2, and don't read `E:\Assets\DND5E\pixel3d\sealed`.
- Never mark anything approved.
- Don't edit other agents' or the main session's files: the `kit\props*.py`, `kit\adventuring_gear.py`,
  `kit\creatures.py`, `kit\v2_loot.py`, `kit\rig.py`, `rig_export.py` and their review scripts; `village_review*.py`,
  `kit\core.py`, `kit\pose.py`, `vox.py`, `render_vox.py` and `kit\beasts.py`.
- Write in plain, direct English in anything Peter reads.

**Final reply:** counts built (items, second looks, floors, demos), doubts, the exact commands for the main session to run
next, and the regression result.
