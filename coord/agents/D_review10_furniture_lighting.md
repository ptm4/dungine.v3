# Agent D: build review 10, furniture and lighting (library batches P01 and P02)

Status: built and published 2026-09-30 (76 props, 8 lineups); review 10 approved by Peter 2026-09-30.

## Brief (the prompt as given)

You're building the next batch of Peter's voxel "3D pixel art" asset library: the furniture (P01) and the lighting (P02)
of Curse of Strahd's Chapter One. They become library review 10. You're one of several agents working in parallel with the
main session, so follow the rules exactly.

**Read first, fully**
- `E:\Assets\DND5E\pixel3d\pipeline\3dpixelartlessons.md`: the style decisions and every technical lesson. It's the style
  bible. Obey it.
- `E:\Assets\DND5E\pixel3d\README.md`, and `E:\Assets\DND5E\pixel3d\plan\SCOPE_PLAN.md` sections 1 to 3 and 8.
- Learn the code:
  - `pipeline\vox.py`: the `Grid` and its signed-distance primitives, `paint`, `recolor`, `save`, `save_vox`.
  - `pipeline\kit\materials.py`: `LIB` and `shade`.
  - `pipeline\kit\weapons.py`: builders drawing items into a grid.
  - `pipeline\item_review.py`: building and saving items, the true-scale lineups beside a person, and sprites.
  - `pipeline\kit\parts\village.py`: `pie_cart` and `held_tray`, props sized in real centimetres.
  - `pipeline\render_review8.sh`, `pipeline\outfit_review_page.py` and `pipeline\archetype_review_page.py`: how reviews
    are rendered and paged.

**What to build.** Every asset of batches P01 and P02 in `E:\Assets\DND5E\pixel3d\plan\scope_manifest.json` (38 in all),
each at its manifest `path`: `pixel3d\<path>\<id>\<id>.npz` and `.vox`.
- **P01, furniture:** bed, crib_and_bassinet, cot, table, long_banquet_table, chair, bench, pew, wardrobe, bookshelf,
  desk, rug, painting, fireplace, bar_counter, shelves, keg, crate, haybale, woodpile, dollhouse, harpsichord, pipe_organ,
  throne, sheeted_furniture, clawed_tub, trophy_head, workbench, lectern.
- **P02, lighting:** candlesticks, candelabra, chandelier, hanging_lantern, wall_sconce, torch_in_bracket, brazier,
  lamppost, campfire.
- **Second looks:** every asset also gets a second look, `<id>_2`, beside it. Peter's standing rule: another wood,
  finish or colour, or a small design change, but "dont make them too irrecognizable".
- **Scale:** 1 voxel = 1.5 cm, at real-world sizes in centimetres. A chair seat is about 45 cm high; a four-poster bed
  about 2 m long. Check every size against a person: 1.8 m is about 120 voxels.
- **The look:** Barovian gothic. Dark, worn woods, black iron, tarnished brass, old candle wax, faded cloth; three or
  four strong value groups and one accent per asset.
  - Flames, embers and glowing wax are emissive voxels (`emit`), few and deliberate.
  - Silhouettes must read at sprite size.
- **Keep it spoiler-free:** Peter will play the campaign. Every prop is generic: no clues, and no notes on
  where a prop appears in the module.


**Where your code goes**
- New files only:
  - `pipeline\kit\props.py`: the builders;
  - `pipeline\props_review.py`: builds everything and the true-scale lineups, with `--only <ids>` and
    `--out-root <dir>` for tests, plus `--sprites`;
  - `pipeline\props_review_page.py`: the review page, `pixel3d\reviews\10_furniture_lighting\index.html`, in the style
    of the review 8 page;
  - `pipeline\render_review10.sh`: renders every prop with the `item` view and `--glb`, and the lineups with `lineup`.
    Model it on `render_review8.sh`.
- New colours go in your own module through `LIB.update({...})`, with the key `'name': (hex, var, dark, light, metal,
  emit)`.
- Don't edit any existing file. If a shared change is truly needed, stop and describe it in your final reply.
- Test builds go in your own temp folder, such as `%TEMP%\dnd5e_agentD\`, never in the library, until the build is
  right.

**Checks**
- Look at every asset in flat projections before calling it done. Front, side and top views are PNGs you make with numpy
  and PIL from the npz (`ijk` + `origin`, `rgb`), in your temp folder.
- Fix what looks wrong: floating parts, wrong sizes, noise speckle (the lessons explain why), unreadable silhouettes.
- Then build into the library.
- Last, the regression check. Rebuild the three heroes into your temp folder
  (`cd E:\Assets\DND5E\pixel3d\pipeline`, then `python -m kit.build recipes\characters\heroes\fighter.json --out <tmp>`,
  and the same for ranger and wizard). Compare each with `pixel3d\characters\heroes\<id>\<id>.npz` using `kit_diff.py`:
  0 differences, or report it.

**The manifest.** Write, but don't run, `pixel3d\plan\research\update_2026-09-30e_review10.py`, modelled on
`update_2026-09-29l_review8.py`:
- it's guarded by the decision id `2026-09-30-review10`;
- it sets the 38 assets to `in_review` with `review='reviews/10_furniture_lighting'`;
- it adds the 38 second looks as new assets in the same batches, with a `decision` note;
- it recounts the batches and the summary.
The main session runs it after the renders.

**Rules**
- Never run Blender. Only the main session does, one at a time.
- No downloads.
- Don't touch `E:\Unity\Projects\Dungine`, never modify `E:\Unity\Projects\dungine.v2`, and don't read
  `E:\Assets\DND5E\pixel3d\sealed`.
- Never mark anything approved.
- Don't edit the files the main session is working on: `village_review*.py`, `render_review9.sh`, `kit\core.py`,
  `kit\pose.py`, `vox.py`, and the review 6, 8 and 9 files.
- Write in plain, direct English in anything Peter reads.

**Final reply:** what you built (counts), anything you couldn't do or doubt, the exact commands for the main session to
run next (renders, then sprites, the page and the manifest script), and the regression result.
