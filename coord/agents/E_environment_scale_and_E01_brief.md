# Agent E: the environment's voxel scale, and the village kit brief (E01)

Status: done 2026-09-30. Reports: `coord\research\env_scale.md` (with `env_scale\`) and the library's
`plan\briefs\E01_village_kit.md`. Both wait for Peter.

## Brief (the prompt as given)

**Context**
- Peter's voxel "3D pixel art" asset library, `E:\Assets\DND5E\pixel3d`, builds characters and props at 1 voxel = 1.5 cm.
  A 1.8 m person is about 120 voxels.
- Buildings at that scale would be enormous: a 7 m house is 470 voxels tall, and the kit's dense numpy grids would run to
  tens of millions of cells.
- Chapter One's next environment batch is E01, the Barovian village kit. It covers:
  - half-timbered houses;
  - shop and tavern fronts and signs;
  - a stone church and a bell tower;
  - a noble manor;
  - stone walls, iron and wood fences, gateposts;
  - a cobbled square, the village well, market stalls, a notice board, a headless statue.
  See batch E01 in `pixel3d\plan\scope_manifest.json`.
- The game is Dungine.v3 (`E:\Unity\Projects\dungine.v3`, read `coord\PLAN.md`), a BG3-style party RPG with an
  isometric camera. The engine isn't chosen yet.
- Read `pixel3d\pipeline\3dpixelartlessons.md`, the style bible (faceless figures, Hytale as the reference), and the
  approved brief style in `pixel3d\plan\briefs\N01_village_and_death_house.md` and
  `pixel3d\plan\briefs\00_sample*.md`.

**Tasks**
1. **A scale study,** for Peter's decision on the environment's voxel size. Compare three options:
   - 1.5 cm, like the characters;
   - a coarser environment scale: 3 cm, 6 cm, or whatever you judge best;
   - a mixed approach: modular architecture at a coarser scale, with 1.5 cm detail props.

   Build small test pieces with the library's own tools (`pipeline\vox.py`), in your own temp folder, never the library.
   For example, a 2 m wall section with a timber frame and a shuttered window, at each scale. Put a 1.8 m figure from
   the kit beside each for scale; `pipeline\race_review.py` and `kit\core.build` show how figures are built. Make
   flat-projection PNGs with numpy and PIL. No Blender.

   Weigh the look (a coarser grid next to fine characters), the voxel counts, meshing and draw cost in a game engine
   (greedy meshing, merging, LOD), the build times of the Python pipeline, and how modular kits snap together.

   Write `E:\Unity\Projects\dungine.v3\coord\research\env_scale.md`, with the PNGs in
   `E:\Unity\Projects\dungine.v3\coord\research\env_scale\`. End with a recommendation and the question for Peter, with
   options.
2. **The E01 brief:** `E:\Assets\DND5E\pixel3d\plan\briefs\E01_village_kit.md`, in the approved brief style.
   - Each kit piece: in one breath; at a glance; its parts and modules; colours and materials; its second look.
   - Build notes: scale per your study, marked as depending on Peter's scale decision; sizes in metres; how the pieces
     snap together.
   - Questions for Peter at the end.

   Keep it spoiler-free: Peter will play the campaign. Describe the village as the party first sees it, as
   architecture only.

**Rules**
- Write only the brief, the research file and its PNG folder. Read-only everywhere else, and no edits to existing code.
- Never run Blender. Only the main session does.
- No downloads.
- Don't touch `E:\Unity\Projects\Dungine`, never modify dungine.v2, and don't read `E:\Assets\DND5E\pixel3d\sealed`.
- Write in plain, direct English.

**Final reply:** your scale recommendation in 3 lines, and the file paths.
