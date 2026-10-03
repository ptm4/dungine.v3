# Agent G: build review 12, adventuring gear (library batch G01)

Status: review 12 published 2026-09-30; round 2 running (the backpack, bucket, waterskin and flask or tankard less blocky), resumed 2026-09-30.

## Brief (the prompt as given)

You're building a batch of Peter's voxel "3D pixel art" asset library: the PHB adventuring gear (batch G01): containers,
light, camping and food. It becomes library review 12. Several agents work in parallel with the main session, so follow
the rules exactly.

**Read first, fully**
- `E:\Assets\DND5E\pixel3d\pipeline\3dpixelartlessons.md`: the style bible. Obey it.
- `E:\Assets\DND5E\pixel3d\README.md`, and `E:\Assets\DND5E\pixel3d\plan\SCOPE_PLAN.md` sections 1 to 3 and 8.
- Learn the code:
  - `pipeline\vox.py`, `pipeline\kit\materials.py`, `pipeline\kit\weapons.py`.
  - `pipeline\item_review.py`, which review 4 used: each item as a prop, a sprite and a held demo on a person
    (`held_demo`), plus true-scale lineups.
  - `pipeline\wear_review.py`: `lay_flat` and `to_grid`, how floor (loot) versions are made.
  - `pipeline\outfit_review.py`: `set_down` for things set down upright.
  - `pipeline\kit\parts\village.py`: `held_cup` and `held_jug`, held things sized in centimetres.
  - `pipeline\kit\parts\npc_gear.py`: worn gear such as `belt_book` and `belt_sack`.
  - `pipeline\render_review8.sh` and `pipeline\outfit_review_page.py`.
- Agent D is building the fixed lighting (candlesticks, lanterns, torches in brackets) in `pipeline\kit\props.py`. Once
  that file exists, read it (read-only) and keep your candles, torch and lanterns consistent with it.

**What to build.** Every G01 asset in `E:\Assets\DND5E\pixel3d\plan\scope_manifest.json` (25), each at its manifest
`path` (`pixel3d\<path>\<id>\<id>.npz` and `.vox`):
- backpack, barrel, basket, bedroll, blanket, bottle_glass, bucket, candle, case_map_or_scroll, chest, flask_or_tankard,
  jug_or_pitcher, lamp, lantern_bullseye, lantern_hooded, mess_kit, pot_iron, pouch, rations_1_day, sack,
  tent_two_person, tinderbox, torch, vial, waterskin.
- **Second looks:** every item gets one, `<id>_2`. Another material, colour or wear, or a small design change, but "dont
  make them too irrecognizable".
- **Floor (loot) versions:** every portable item also gets a floor version, lying or dropped on the ground, in
  `<folder>\<id>\floor\<id>_floor.npz`, as review 5 and review 8 did. Things that already stand on the floor (barrel,
  chest, tent, bucket, pot, basket) don't need one; say so on the page.
- **Held or worn demos,** as review 4 did, where they matter: the torch and both lanterns held and lit, the backpack worn
  with the bedroll strapped on, and the waterskin and pouch at the belt. Use your judgement for others.
- **Scale:** 1 voxel = 1.5 cm, at real-world sizes. A backpack is about 50 cm tall, a barrel about 90 cm, a two-person
  tent about 2 m long; a person is 1.8 m, about 120 voxels.
- **The look:** worn and practical, Barovian gothic. Leather, canvas, dark wood, black iron, tin, green glass, wax;
  three or four value groups and one accent. Flames are emissive voxels, few and deliberate. Tiny items (vial,
  tinderbox) must still read in the sprite.

**Where your code goes**
- New files only:
  - `pipeline\kit\adventuring_gear.py`: builders;
  - `pipeline\gear_review.py`: build, lineups, `--only`, `--out-root`, `--sprites`;
  - `pipeline\gear_review_page.py`: the page, `pixel3d\reviews\12_adventuring_gear\index.html`;
  - `pipeline\render_review12.sh`: items and floors with `item`, demos with `hero,back`, lineups with `lineup`, all with
    `--glb`.
- New colours go through `LIB.update({...})` in your module, with names starting `gear_`.
- Don't edit any existing file. If a shared change is truly needed, stop and say so.
- Test in `%TEMP%\dnd5e_agentG\` until the build is right, then build into the library.

**Checks**
- Flat projections of everything before calling it done. Fix floating parts, wrong sizes, speckle and unreadable
  silhouettes.
- Then the hero regression: rebuild the three heroes to your temp folder with `python -m kit.build ... --out <tmp>`, and
  compare with `kit_diff.py`: 0 differences.

**The manifest.** Write, but don't run, `pixel3d\plan\research\update_2026-09-30i_review12.py`:
- guarded by the decision id `2026-09-30-review12`;
- the 25 set to `in_review` with `review='reviews/12_adventuring_gear'`;
- the second looks added as new assets;
- recounts.

**Rules**
- Never run Blender; only the main session does.
- No downloads.
- Don't touch `E:\Unity\Projects\Dungine`, never modify dungine.v2, and don't read `E:\Assets\DND5E\pixel3d\sealed`.
- Never mark anything approved.
- Don't edit other agents' or the main session's files: `kit\props.py`, `props_review*.py`, `kit\props_religion.py`,
  `religion_review*.py`, `kit\creatures.py`, `creature_review*.py`, `village_review*.py`, `kit\core.py`, `kit\pose.py`,
  `vox.py`, `kit\beasts.py`.
- Write in plain, direct English in anything Peter reads.

**Final reply:** counts built (items, second looks, floors, demos), doubts, the exact commands for the main session to run
next, and the regression result.
