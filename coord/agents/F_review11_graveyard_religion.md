# Agent F: build review 11, graveyard and religion (library batch P03)

Status: review 11 published 2026-09-30; round 2 running (the stocks become a standing pillory), resumed 2026-09-30.

## Brief (the prompt as given)

You're building a batch of Peter's voxel "3D pixel art" asset library: Curse of Strahd's graveyard and religion props
(batch P03). They become library review 11. Several agents work in parallel with the main session, so follow the rules
exactly.

**Read first, fully**
- `E:\Assets\DND5E\pixel3d\pipeline\3dpixelartlessons.md`: the style bible. Obey it.
- `E:\Assets\DND5E\pixel3d\README.md`, and `E:\Assets\DND5E\pixel3d\plan\SCOPE_PLAN.md` sections 1 to 3 and 8.
- Learn the code:
  - `pipeline\vox.py`: the Grid and its primitives, `paint`, `recolor`, `save`, `save_vox`.
  - `pipeline\kit\materials.py`: `LIB` and `shade`.
  - `pipeline\kit\weapons.py`: builders drawing items into a grid.
  - `pipeline\item_review.py`: saving items, true-scale lineups beside a person, sprites.
  - `pipeline\kit\parts\village.py`: `pie_cart`, a prop sized in real centimetres.
  - `pipeline\render_review8.sh` and `pipeline\outfit_review_page.py`: how reviews are rendered and paged.
- `pipeline\village_review.py`, `lie_in_coffin`: the coffin Kolyan lies in (plain dark wood, cream lining). Your coffin
  should match it.
- `pipeline\kit\parts\npc_gear.py`, the `pendant` sun: the Morninglord's sun symbol. Your wall sun and shrine should
  match it.

**What to build.** Every P03 asset in `E:\Assets\DND5E\pixel3d\plan\scope_manifest.json` (12), each at its manifest
`path`: `pixel3d\<path>\<id>\<id>.npz` and `.vox`. The assets are gravestone_4_shapes (four stones in one asset),
grave_mound, open_grave (with its spoil heap), coffin (closed), sarcophagus, mausoleum, altar, blood_altar, shrine (a
small roadside shrine), holy_symbol_wall_sun, gallows and stocks.
- **Second looks:** every asset gets one, `<id>_2`. Peter's rule: another stone, wood or weathering, or a small design
  change, but "dont make them too irrecognizable". The coffin's second look is open and empty, showing its lining.
- **Scale:** 1 voxel = 1.5 cm, at real-world sizes. A mausoleum is about 3 m tall, gallows about 3.5 m; a person is
  1.8 m, about 120 voxels. Big grids are heavy; build them efficiently and in parts if needed.
- **The look:** Barovian gothic. Weathered grey stone with moss and lichen, dark wood, black iron, faded cloth, old
  candle wax; three or four strong value groups and one accent. Candle flames are emissive voxels, few and deliberate.
- **Keep it spoiler-free:** Peter will play the campaign. Every prop is generic: no clues, and no notes
  on where anything appears in the module.

**Where your code goes**
- New files only:
  - `pipeline\kit\props_religion.py`: the builders;
  - `pipeline\religion_review.py`: builds everything and a true-scale lineup, with `--only`, `--out-root` and `--sprites`;
  - `pipeline\religion_review_page.py`: the page, `pixel3d\reviews\11_graveyard_religion\index.html`;
  - `pipeline\render_review11.sh`: renders each asset with `--views item --glb`, and the lineup with `lineup`. Model it
    on `render_review8.sh`.
- New colours go through `LIB.update({...})` in your module, with names starting `rel_`.
- Don't edit any existing file. If a shared change is truly needed, stop and say so in your final reply.
- Test in your own temp folder, such as `%TEMP%\dnd5e_agentF\`, until the build is right, then build into the library.

**Checks**
- Flat projections of every asset (front, side and top PNGs made with numpy and PIL from the npz) before calling it
  done. Fix floating parts, wrong sizes, speckle and unreadable silhouettes.
- Then the regression: rebuild the three heroes into your temp folder
  (`cd E:\Assets\DND5E\pixel3d\pipeline`, then `python -m kit.build recipes\characters\heroes\fighter.json --out <tmp>`,
  and the same for ranger and wizard). Compare with `pixel3d\characters\heroes\<id>\<id>.npz` using `kit_diff.py`:
  0 differences.

**The manifest.** Write, but don't run, `pixel3d\plan\research\update_2026-09-30h_review11.py`, modelled on
`update_2026-09-29l_review8.py`:
- guarded by the decision id `2026-09-30-review11`;
- it sets the 12 assets to `in_review`, with `review='reviews/11_graveyard_religion'`;
- it adds the 12 second looks as new assets in the batch;
- it recounts the batch and the summary.
The main session runs it after the renders.

**Rules**
- Never run Blender; only the main session does, one at a time.
- No downloads.
- Don't touch `E:\Unity\Projects\Dungine`, never modify `E:\Unity\Projects\dungine.v2`, and don't read
  `E:\Assets\DND5E\pixel3d\sealed`.
- Never mark anything approved.
- Don't edit other agents' files (`kit\props.py`, `props_review*.py`, `render_review10.sh`) or the main session's
  (`village_review*.py`, `kit\core.py`, `kit\pose.py`, `vox.py`, `kit\beasts.py`).
- Write in plain, direct English in anything Peter reads.

**Final reply:** counts built, doubts, the exact commands for the main session to run next (renders, then sprites, the
page and the manifest script), and the regression result.
