# Agent E: build the village kit (E01) at 1.5 cm

Status: started 2026-10-02.

## Peter's answers (2026-10-02)

- **The scale:** "I like A 1.5 cm everywhere". One grain throughout, like the people and props. Your study's notes on A
  apply: modules of 3 m or less, bounded painting, and LODs later at export.
- **The layout:** "I liked v2 but lets keep everything as strict to the book as possible but keeping room for you to add
  flair. Think Vanilla plus, I wanna keep the lore intended experience but enhance it in ways that make sense."
- **Not answered, so the brief's recommendations stand:** signs are pictures only (grapes over a cup, a pair of
  scales); wooden shingles on the houses, slate on the church, the manor and the second-skin houses; the grid and roof
  rules (1.5 m squares, 0.3 m steps, 3.0 m storeys, 45° roofs). Interiors: build the inside faces the brief lists;
  which buildings open in the game is decided when the village is laid out.

## What to build

The brief: `E:\Assets\DND5E\pixel3d\plan\briefs\E01_village_kit.md` (yours). Every kit piece, with its second look, at
1.5 cm, in modules of 3 m or less that snap on the brief's grid.

- Split the work into reviews of a sensible size, numbered from 33, and report after each one:
  - review 33: the houses (cottage bays, upper storeys, roofs, chimneys, doors and shutters) and the tavern front;
  - review 34: the church, its undercroft and the bell tower;
  - review 35: the manor, walls, fences, gateposts, the cobbled square, the well, the market stall, the notice board
    and the statue.
  Adjust the split if another works better, and say why.
- Each page shows:
  - the pieces one by one;
  - a few assembled examples (a whole cottage, the tavern front, the church), with the review man beside them for
    scale;
  - the second looks, and the sprites where they make sense.
- Keep it generic and spoiler-free. The kit is what a traveller sees in any Barovian village; no story places by name,
  and nothing from the house at the edge of the fog (that location is being built sealed, unseen by Peter).
- The book first, with a little flair ("vanilla plus"): Barovian gothic, poor and weathered, as the module describes the
  village. Flair is welcome where it serves that mood.

## How

- Read first: the newest entries of `pipeline\3dpixelartlessons.md`. There are many since your study, including review
  12 round 2 on clean, un-blocky shapes, which Peter asked for ("a little more blocky / pixelated than we'd like"), and
  the face lessons (windows, vents and knots in pairs read as eyes; shutters and doors on a facade must not make a face).
- **Bounded painting:** at 1.5 cm you need it. Build it inside your own new module (e.g. `pipeline\kit\env.py`, a canvas
  that paints each shape only inside its own box). Don't change the shared kit core (`kit\core.py`, `vox.py`); if a
  shared change is truly needed, stop and say so.
- New files only, your own names: the kit module, `env_review.py`, a page script, `render_review33.sh` and so on.
  New colours via `LIB.update` with a prefix nobody uses yet.
- Library paths: the manifest's E01 paths. Write each review's manifest script but don't run it
  (`plan\research\update_2026-10-02_review33_village.py`, guarded by `2026-10-02-review33`, and so on). Dry-run each on a
  copy.
- Renders: never run Blender. Give the main session the exact render commands, and tell it about any piece too large
  for `render_vox.py`'s item view (it handles props of 3 m and more since review 11).
- Hero regression when done: 0 differences.
- Rules: no downloads; don't touch `E:\Unity\Projects\Dungine`; never modify dungine.v2 (reading it for the village's
  look is fine); don't read `pixel3d\sealed`; never mark anything approved; plain English in anything Peter reads.

**Final reply per review:** counts, the render commands, doubts, lessons (for the main session to add).
