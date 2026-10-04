# Agent P: split the later chapters into surface and sealed

Status: started 2026-10-04.

## Peter's answer (2026-10-04)

Asked how to build the rest of the campaign's places, creatures and story items, Peter chose: show the surface, seal
the secrets. "you should be getting a taste of my design choices and what im going for overall". He reviews what a
traveller would see; the rest is built sealed, to his taste, without his review, as you did for Chapter One.

- **Surface (reviewed by Peter):** town and building exteriors; the roads, gates and landscapes; common creatures as the
  Monster Manual shows them; people as first met (their public look); generic kits and gear.
- **Sealed (built unseen):** interiors or rooms that hold a secret; anyone's or anything's hidden form; bosses and
  unique creatures whose look is itself a reveal; story items and plot props; anything whose name alone gives the story
  away.
- When unsure, seal it. A sealed piece costs Peter nothing; a spoiled one can't be unspoiled.

## What to do

1. **The split.** Go through every asset of the batches C03 to C06, L02 to L12, M01, P05, N02 to N04 and E05 in
   `plan\scope_manifest.json`, and decide surface or sealed for each. Use your knowledge of the module.
2. **The spoiler map:** `pixel3d\sealed\later\CONTENTS.md`: every sealed id, what it is, why it's sealed, and its neutral
   folder (`sealed\later\<chapter>\...`, neutral names as in `chapter1\`). Update `sealed\README.md` in neutral words
   only (no counts, no names).
3. **The manifest script:** `plan\research\update_2026-10-04c_later_split.py`, guarded by `2026-10-04-later-split`.
   Handle the sealed entries the way your Chapter One scripts did (`update_2026-10-03a_sealed_creatures.py`,
   `update_2026-10-03c_sealed_location.py`): status `sealed`, `sealed: true`, neutral paths, matched without naming
   them, counts-only output. Neutralise any surface entry whose id, name or note gives something away. Dry-run on a copy;
   the main session runs it.
4. **The surface plan:** `plan\briefs\later_surface.md`, spoiler-free, for the builders and for Peter: the surface
   assets grouped into reviews of a sensible size, in the order the party meets them, each with a line of what it is
   and what it needs first (for example the castle kit, review 46, or the water kit, review 45, both being built now).
5. **The sealed plan:** in `sealed\later\`, which sealed pieces you can build now and which wait for a kit or a surface
   piece. Then start building the ones that can go now, with your Chapter One builders as the pattern
   (`sealed_creatures.py`, `sealed_location.py`, `sealed_items.py`), to Peter's taste: dark, worn gothic; magic gear
   unique and detailed, wider blades and heads; faceless (beasts may bare teeth, never eyes).

## Rules

- **Your reports to the main session are counts only:** no names, ids, descriptions or hints of anything sealed. Say
  only how many are sealed, how many are surface, and which surface reviews you propose.
- No PNGs anywhere under `sealed\`. Check sealed renders only in your own scratch folder, and don't run Blender (the
  main session renders; for sealed pieces, give it a command that writes to a scratch folder outside the library).
- Keep spoilers out of every file Peter can read: the manifest's visible fields, the plan page, `later_surface.md`, any
  review page, any file name outside `sealed\`.
- No downloads; don't touch `E:\Unity\Projects\Dungine`; never modify dungine.v2; never mark anything approved
  (`sealed` is approved by delegation, not by Peter).

**Final reply:** counts, the surface reviews you propose, the manifest script's dry-run output, and what you've
started building (counts only).
