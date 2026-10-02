# Agent P: the sealed loot items (the three flagged G04 items)

Status: done 2026-09-30 (sealed; counts only here).

## Brief (the prompt as given)

Peter plays the Curse of Strahd campaign, so what only play reveals is built without his review and sealed, with no
pictures. He asked for this on 2026-09-30: "Do it without my oversight so it remains spoiler free for me". Three of the
loot items dungine.v2 invented (batch G04) are flagged `"spoiler": true` in the library's manifest. Build those three,
sealed.

**Read first**
- `E:\Assets\DND5E\pixel3d\sealed\README.md`, the sealed folder's rules. This task may read and write inside
  `E:\Assets\DND5E\pixel3d\sealed\chapter1\items\` only. Don't open anything else under `sealed\`.
- `E:\Assets\DND5E\pixel3d\pipeline\3dpixelartlessons.md`, the style bible (glow values, faces, speckle, small things).
- `E:\Assets\DND5E\pixel3d\pipeline\kit\v2_loot.py` and `loot_review.py`: agent L's builders for the other 25 G04
  items. Match their style; import from them, never edit them.
- `E:\Assets\DND5E\pixel3d\plan\scope_manifest.json`: the G04 entries with `"spoiler": true` are your three.
- `E:\Unity\Projects\dungine.v2\Assets\Game\Scripts\Rules\Items.cs`, read-only, for what each item is.

**Task**
- Build each of the three in the library's style: a standalone model, a second look and a floor version, as the other
  G04 items have. Put them in `pixel3d\sealed\chapter1\items\i01\`, `i02\` and `i03\` (npz, vox and a neutral json:
  name "sealed item iNN", chapter 1, sealed true).
- **No pictures of any kind**, no GLB and no page. Check them only in flat projections in your own temp folder
  (`%TEMP%\dnd5e_agentP\`), then delete those pictures.
- The builder script goes inside the sealed folder: `pixel3d\sealed\sealed_items.py`. Add the mapping (which id is
  which item) to `pixel3d\sealed\chapter1\CONTENTS.md` under a new heading, "Items".
- Write, but don't run, a manifest script, `pixel3d\plan\research\update_2026-09-30r_sealed_items.py`:
  - guarded by the decision id `2026-09-30-sealed-items`;
  - it sets the three entries' `path` to `sealed/chapter1/items/iNN`, their status to `sealed`, and `sealed: true`;
  - its docstring and any printed text must be neutral: counts only, with no item names or descriptions.
- Hero regression at the end: `python -m kit.build ... --out <tmp>` for the three heroes, and `kit_diff.py`: 0
  differences.

**Rules**
- Your final reply may be shown to Peter, so it must be neutral: counts, file paths and the regression result only,
  never what the items are.
- Never run Blender. No downloads. Never mark anything approved.
- Don't touch `E:\Unity\Projects\Dungine`, and never modify dungine.v2.
- Don't edit other agents' or shared files.
