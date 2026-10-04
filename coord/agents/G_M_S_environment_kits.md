# Agents G, M and S: the next environment kits (E04, E06) at 1.5 cm

Status: started 2026-10-04.

Agent E built the village kit (reviews 33 to 36) and is now on terrain (E02, review 43) and vegetation (E03, review 44).
These three kits run beside it, on E's foundations.

| Agent | Batch | Review | Pieces (the manifest's ids) |
|---|---|---|---|
| G | E04 water and crossings | 45 | river, lake, pond, waterfall, flooded_floor, stone_arch_bridge, wooden_bridge, drawbridge, causeway |
| M | E06, the castle set | 46 | castle_curtain_wall_kit, castle_tower_and_spire_kit, staircase_kit, interior_room_kit, dungeon_kit |
| S | E06, the wild set | 47 | log_cottage_kit, palisade_kit, ruin_kit, cave_kit, rock_cut_temple_kit |

## Peter's standing direction

- **1.5 cm everywhere** ("I like A 1.5 cm everywhere"); modules of 3 m or less that snap on the village kit's grid (1.5 m
  squares, 0.3 m steps, 3.0 m storeys, 45° roofs).
- **Vanilla plus:** "as strict to the book as possible but keeping room for you to add flair". Barovian gothic: poor,
  weathered, oppressive.
- **Dark, worn gothic, second looks included** (2026-10-04): "i actually like the darker look(i consider dark gothic
  too)", "I like the first design which was darker and more worn looking". Every piece gets a second look (`<id>_2`)
  that varies stone, wood, weathering or damage, never brighter or cleaner than the first.
- **Faceless:** windows, vents, arrow slits, knots or doors in pairs must not read as a face.
- **Spoiler-free:** keep it generic, what a traveller sees anywhere in the valley. No story places by name, and nothing
  of any particular location's secrets. Peter plays the campaign; anything he reads must not give the story away.

## How

- Read first: the top of `pipeline\3dpixelartlessons.md` (style decisions) and its newest entries, especially agent
  E's village kit lessons (bounded painting, the face rules, the undercroft's hidden-under-the-ground render, the
  washed-out layout render).
- Study E's kit before building: `pipeline\kit\env.py` (`EnvCanvas`, `Region`, `pointed_arch`, `place`, `assemble`,
  `drop_crumbs`), `env_houses.py`, `env_church.py`, `env_manor.py`, `env_terrain.py`, and the review scripts
  `env_review.py`, `env_review_page.py`, `render_review33.sh`. Match their palette (the `env_` colours) so your pieces sit
  with the village. Look at reviews 33 to 36's pages for what Peter approved.
- **New files only, your own names:**
  - G: `kit\env_water.py`, `water_review.py`, `water_review_page.py`, `render_review45.sh`, colours prefixed `wt_`;
  - M: `kit\env_castle.py`, `castle_review.py`, `castle_review_page.py`, `render_review46.sh`, colours prefixed `cas_`;
  - S: `kit\env_wild.py`, `wild_review.py`, `wild_review_page.py`, `render_review47.sh`, colours prefixed `wld_`.
  Don't edit `env.py` or any of E's files (E is editing them now). If you truly need a change there, stop and message
  the main session.
- Water (G): voxel water is a surface, not a block. Keep it a thin, dark, still sheet with a few lighter ripple voxels,
  readable from above at game distance; banks and shallows meet E's terrain. Coordinate the ground height with
  `env_terrain.py` (read it; don't edit it).
- Each review page shows the pieces one by one, a few assembled examples with the review man beside them for scale, the
  second looks, and the sprites where they make sense. Model it on `env_review_page.py`.
- Library paths: the manifest's paths for your ids. Write the review's manifest script but don't run it
  (`plan\research\update_2026-10-04_review45_water.py`, guarded by `2026-10-04-review45`; 46 and 47 likewise). Dry-run
  it on a copy.
- Renders: never run Blender. Give the main session the exact render commands, and flag any piece too large for
  `render_vox.py`'s item view, or one that sits under the ground plane (lift its example for the picture).
- Hero regression when done: 0 differences. Check that E's review 33 to 36 models still rebuild identically.
- Rules: no downloads; don't touch `E:\Unity\Projects\Dungine`; never modify dungine.v2 (reading it is fine); don't read
  `pixel3d\sealed`; never mark anything approved; plain English in anything Peter reads.

**Final reply:** counts, the render commands, doubts, and lessons (for the main session to add).
