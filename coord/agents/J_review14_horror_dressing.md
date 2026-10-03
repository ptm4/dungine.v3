# Agent J: build review 14, horror dressing (library batch P04)

Status: built and published 2026-09-30 (28 models, 4 lineups, sprites); review 14 approved by Peter 2026-09-30.

## Brief (the prompt as given)

You're building a batch of Peter's voxel "3D pixel art" asset library: the horror dressing props (batch P04). It becomes
library review 14. Several agents work in parallel with the main session, so follow the rules exactly.

**Read first, fully**
- `E:\Assets\DND5E\pixel3d\pipeline\3dpixelartlessons.md`: the style bible and every lesson (including the newest ones on
  bounded painting and speckle). Obey it.
- `E:\Assets\DND5E\pixel3d\README.md`, and `E:\Assets\DND5E\pixel3d\plan\SCOPE_PLAN.md` sections 1 to 3 and 8.
- The code: `pipeline\vox.py`, `pipeline\kit\materials.py`, `pipeline\kit\weapons.py`, `pipeline\item_review.py`
  (lineups and sprites), `pipeline\kit\parts\village.py` (props in real centimetres), `pipeline\render_review8.sh` and
  `pipeline\outfit_review_page.py`.
- Other agents are building furniture and lighting (`kit\props.py`) and graveyard props (`kit\props_religion.py`). Read
  them if they exist, to match materials; never edit them.

**What to build.** Every P04 asset in `E:\Assets\DND5E\pixel3d\plan\scope_manifest.json` (14), each at its manifest
`path` (`pixel3d\<path>\<id>\<id>.npz` and `.vox`):
- hanging_corpse, wooden_cage, hanging_cage, shackles_and_chains, torture_rack, bone_pile, skull, pile_of_teeth,
  giant_web_and_cocoon, cobweb, wall_mirror, mannequin (a tailor's dummy), claw_marks (on a plank wall section),
  garlic_strings_on_a_door.
- **Second looks:** every asset gets one, `<id>_2`, but "dont make them too irrecognizable".
- **Scale:** 1 voxel = 1.5 cm, at real-world sizes; a person is 1.8 m, about 120 voxels. Use bounded painting (the
  lesson) for anything big.
- **The look:** Barovian gothic horror, grim but not gory. The hanging corpse is a faceless, shrouded or ragged figure
  (the kit's faceless rule holds for the dead too); bones are aged ivory, not white; iron is black and rusted; webs are
  thin and pale. Three or four value groups and one accent.
- **Keep it spoiler-free:** Peter will play the campaign. Every prop is generic. No notes on where anything appears in
  the module, no clues, nothing specific to a place.

**Where your code goes**
- New files only:
  - `pipeline\kit\props_horror.py`: builders;
  - `pipeline\horror_review.py`: build, a true-scale lineup, `--only`, `--out-root`, `--sprites`;
  - `pipeline\horror_review_page.py`: the page, `pixel3d\reviews\14_horror_dressing\index.html`;
  - `pipeline\render_review14.sh`: each asset with `--views item --glb`, and the lineup with `lineup`.
- New colours go through `LIB.update({...})` in your module, with names starting `hor_`.
- Don't edit any existing file; if a shared change is truly needed, stop and say so.
- Test in `%TEMP%\dnd5e_agentJ\` until the build is right, then build into the library.

**Checks**
- Flat projections of everything before calling it done. Fix floating parts, wrong sizes, speckle, and anything that
  reads as a face or eyes.
- Then the hero regression: rebuild the three heroes to your temp folder with `python -m kit.build ... --out <tmp>`, and
  compare with `kit_diff.py`: 0 differences.

**The manifest.** Write, but don't run, `pixel3d\plan\research\update_2026-09-30m_review14.py`:
- guarded by the decision id `2026-09-30-review14`;
- the 14 set to `in_review` with `review='reviews/14_horror_dressing'`;
- the second looks added as new assets;
- recounts.

**Rules**
- Never run Blender. No downloads.
- Don't touch `E:\Unity\Projects\Dungine`, never modify dungine.v2, and don't read `E:\Assets\DND5E\pixel3d\sealed`.
- Never mark anything approved.
- Don't edit other agents' or the main session's files: the `kit\props*.py`, `kit\adventuring_gear.py`,
  `kit\creatures.py`, `kit\rig.py`, `rig_export.py` and their review scripts; `village_review*.py`, `kit\core.py`,
  `kit\pose.py`, `vox.py` and `kit\beasts.py`.
- Write in plain, direct English in anything Peter reads.

**Final reply:** counts built, doubts, the exact commands for the main session to run next, and the regression result.
