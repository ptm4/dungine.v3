# Agent H: build review 13, beasts and mounts (library batch C01)

Status: review 13 published 2026-09-30; round 2 running (the wolves heavier and lower, the cat, the horses), resumed 2026-09-30.

## Brief (the prompt as given)

You're building a batch of Peter's voxel "3D pixel art" asset library: the beasts of Barovia and the mounts (batch C01).
It becomes library review 13. Several agents work in parallel with the main session, so follow the rules exactly.

**Read first, fully**
- `E:\Assets\DND5E\pixel3d\pipeline\3dpixelartlessons.md`: the style bible. Obey it. Faceless throughout, beasts
  included: no eyes on any creature. Only one character in the whole library has a face.
- `E:\Assets\DND5E\pixel3d\README.md`, and `E:\Assets\DND5E\pixel3d\plan\SCOPE_PLAN.md` sections 1 to 3 and 8.
- Learn the code:
  - `pipeline\vox.py` and `pipeline\kit\materials.py`.
  - `pipeline\kit\beasts.py`: the wolf and bat builders made for a key character's animal forms in review 6. You may
    import and call them with other colours, sizes and settings. Never edit `beasts.py`: those approved models must not
    change.
  - `pipeline\item_review.py`: lineups beside a person, and sprites.
  - `pipeline\archetype_review.py`: how review 6 saved and rendered its creature forms (the `item` view).
  - `pipeline\render_review8.sh` and `pipeline\outfit_review_page.py`.

**What to build.** Every C01 asset in `E:\Assets\DND5E\pixel3d\plan\scope_manifest.json` (18), each at its manifest
`path` (`pixel3d\<path>\<id>\<id>.npz` and `.vox`):
- horse_draft, horse_riding, mastiff, pony, warhorse, chicken, goat, wolf, dire_wolf, bat, swarm_of_bats, raven,
  swarm_of_ravens, rat, giant_rat, swarm_of_rats, cat, bear.
- **Second looks:** every creature gets one, `<id>_2`: another coat or colouring, or, for the mounts, tacked up. The
  riding horse and warhorse get saddle and bridle; the pony a pack saddle; the draft horse a collar and harness; the
  mastiff a small saddle for small riders. Peter's rule: "dont make them too irrecognizable".
- **Mounts:** the manifest says "rideable body: saddle and barding points". Record attachment points (saddle seat,
  bridle, and barding anchors at the chest, flanks and rump) in each mount's saved meta, the `.json` beside the `.npz`,
  as world-voxel coordinates.
- **Pose:** a neutral standing pose, all feet on the ground, head level. The creatures will be rigged and animated later,
  so avoid lifted feet and odd angles. Swarms are loose clusters of their animal, about 1.5 m across.
- **Scale:** 1 voxel = 1.5 cm, at real-world sizes. A draft horse is about 1.75 m at the withers, a riding horse about
  1.55 m, a pony about 1.3 m, a warhorse about 1.65 m; a mastiff about 0.75 m at the shoulder; a wolf about 0.8 m; a dire
  wolf about 1.2 m; a brown bear about 1.2 m on all fours; a cat about 25 cm; a bat's wingspan about 30 cm; a raven's
  about 1 m; a rat about 20 cm plus its tail; a giant rat about 60 cm long. A person is 1.8 m, about 120 voxels.
- **Distinct from the key character's animal forms** (review 6, built with `beasts.py`: a black wolf the size of a pony,
  and a bat with a 2 m span). Your wolf is an ordinary grey wolf, your bat a small ordinary bat.
- **The look:** readable silhouettes, natural coats in three or four value groups and one accent; Barovian gloom, with
  nothing cartoonish.

**Where your code goes**
- New files only:
  - `pipeline\kit\creatures.py`: builders; import from `beasts.py`, never edit it;
  - `pipeline\creature_review.py`: build, a true-scale lineup beside a person, `--only`, `--out-root`, `--sprites`;
  - `pipeline\creature_review_page.py`: the page, `pixel3d\reviews\13_beasts_mounts\index.html`;
  - `pipeline\render_review13.sh`: each creature with `--views item --glb` (use `hero` if it frames better, and say
    why), the lineup with `lineup`.
- New colours go through `LIB.update({...})` in your module, with names starting `beast_`.
- Don't edit any existing file. If a shared change is truly needed, stop and say so.
- Test in `%TEMP%\dnd5e_agentH\` until the build is right, then build into the library.

**Checks**
- Flat projections of everything (front, side and top PNGs made with numpy and PIL from the npz) before calling it done.
  Fix floating parts, wrong proportions, speckle, and anything that reads as having eyes.
- Confirm the review 6 animal forms are untouched: they're saved under `pixel3d\characters\npcs\` and made by
  `beasts.py`. Rebuild them into your temp folder with `archetype_review.py` if it has an option, or skip this and say so.
- Then the hero regression: rebuild the three heroes to your temp folder with `python -m kit.build ... --out <tmp>`, and
  compare with `kit_diff.py`: 0 differences.

**The manifest.** Write, but don't run, `pixel3d\plan\research\update_2026-09-30j_review13.py`:
- guarded by the decision id `2026-09-30-review13`;
- the 18 set to `in_review` with `review='reviews/13_beasts_mounts'`;
- the second looks added as new assets;
- recounts.

**Rules**
- Never run Blender; only the main session does.
- No downloads.
- Don't touch `E:\Unity\Projects\Dungine`, never modify dungine.v2, and don't read `E:\Assets\DND5E\pixel3d\sealed`.
- Never mark anything approved.
- Don't edit other agents' or the main session's files: `kit\props.py`, `props_review*.py`, `kit\props_religion.py`,
  `religion_review*.py`, `kit\adventuring_gear.py`, `gear_review*.py`, `village_review*.py`, `kit\core.py`, `kit\pose.py`,
  `vox.py`, `kit\beasts.py`.
- Write in plain, direct English in anything Peter reads.

**Final reply:** counts built, doubts, the exact commands for the main session to run next, and the regression result.
