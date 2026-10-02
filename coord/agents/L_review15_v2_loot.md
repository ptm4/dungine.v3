# Agent L: build review 15, the loot items dungine.v2 invented (library batch G04)

Status: review 15 published 2026-09-30; round 2 running (wider blades for Dawnwarden and Gravefang), resumed 2026-09-30.

## Brief (the prompt as given)

You're building a batch of Peter's voxel "3D pixel art" asset library: the 28 items the dungine.v2 demo invented as Chapter
One loot (batch G04). They become library review 15. Several agents work in parallel with the main session, so follow the
rules exactly.

**Read first, fully**
- `E:\Assets\DND5E\pixel3d\pipeline\3dpixelartlessons.md`: the style bible and every lesson (glow values, speckle,
  bounded painting, faces). Obey it.
- `E:\Assets\DND5E\pixel3d\README.md`, and `E:\Assets\DND5E\pixel3d\plan\SCOPE_PLAN.md` sections 1 to 3 and 8.
- The code:
  - `pipeline\vox.py` and `pipeline\kit\materials.py`;
  - `pipeline\kit\weapons.py`: the review 4 weapon builders; reuse them for the weapons, as variants;
  - `pipeline\item_review.py`: items as a prop, a sprite, a held demo on a person, and true-scale lineups;
  - `pipeline\wear_review.py`: worn pieces on mannequins, and floor versions with `lay_flat`;
  - `pipeline\outfit_review.py`: `set_down`;
  - `pipeline\render_review8.sh` and `pipeline\outfit_review_page.py`.
- **What the items are:** `E:\Unity\Projects\dungine.v2\Assets\Game\Scripts\Rules\Items.cs`, read-only. Read that file
  only in dungine.v2. The area scripts hold spoilers about where things are found, so don't open them.
- The manifest: `E:\Assets\DND5E\pixel3d\plan\scope_manifest.json`, batch G04.

**What to build.** Every G04 asset whose manifest entry is not flagged `"spoiler": true`: 25 of the 28. Skip the flagged
ones entirely; the main session handles them. Build each at its manifest `path` (`pixel3d\<path>\<id>\<id>.npz` and
`.vox`).
- Follow v2's description of each item (its type, material and look) in the library's style.
- Weapons are variants of review 4's builders with their own finish. Rings and amulets are tiny, so make them read at
  sprite size. Potions are glass with a coloured fill.
- **Second looks:** every item gets one, `<id>_2`, but "dont make them too irrecognizable".
- **Floor (loot) versions** for everything, lying on the ground (`<folder>\<id>\floor\<id>_floor.npz`), as reviews 5 and
  8 did.
- **Held or worn demos,** as review 4 did, for the weapons, shields and anything worn: helms and cloaks on a mannequin;
  gloves, boots and rings on a figure.
- **Glow:** keep warm glows at 0.16 or below and cool ones at 0.5 or below. The lessons have the numbers.
- **Keep it spoiler-free:** Peter will play the campaign. Page notes describe what each item looks like and is, from
  Items.cs, but never where it's found or any plot it touches.

**Where your code goes**
- New files only:
  - `pipeline\kit\v2_loot.py`: builders;
  - `pipeline\loot_review.py`: build, lineups, `--only`, `--out-root`, `--sprites`;
  - `pipeline\loot_review_page.py`: the page, `pixel3d\reviews\15_v2_loot\index.html`;
  - `pipeline\render_review15.sh`: items and floors with `item`, demos with `hero,back`, lineups with `lineup`, all with
    `--glb`.
- New colours go through `LIB.update({...})`, with names starting `loot_`.
- Don't edit any existing file; if a shared change is truly needed, stop and say so.
- Test in `%TEMP%\dnd5e_agentL\` until the build is right, then build into the library.

**Checks**
- Flat projections of everything before calling it done. Fix floating parts, wrong sizes, speckle, and anything that
  reads as a face or eyes.
- Then the hero regression: rebuild the three heroes to your temp folder with `python -m kit.build ... --out <tmp>`, and
  compare with `kit_diff.py`: 0 differences.

**The manifest.** Write, but don't run, `pixel3d\plan\research\update_2026-09-30o_review15.py`:
- guarded by the decision id `2026-09-30-review15`;
- the 25 set to `in_review` with `review='reviews/15_v2_loot'`;
- the second looks added as new assets, and flagged `spoiler` if their parent is;
- recounts.

**Rules**
- Never run Blender. No downloads.
- Don't touch `E:\Unity\Projects\Dungine`, never modify dungine.v2, and don't read `E:\Assets\DND5E\pixel3d\sealed`.
- Never mark anything approved.
- Don't edit other agents' or the main session's files: the `kit\props*.py`, `kit\adventuring_gear.py`,
  `kit\creatures.py`, `kit\rig.py`, `rig_export.py` and their review scripts; `village_review*.py`, `kit\core.py`,
  `kit\pose.py`, `vox.py`, `render_vox.py` and `kit\beasts.py`.
- Your final reply may be shown to Peter: counts and file paths, no plot.
- Write in plain, direct English in anything Peter reads.

**Final reply:** counts built (items, second looks, floors, demos), doubts, the exact commands for the main session to run
next, and the regression result.
