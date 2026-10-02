# Agent A: audit v2, and choose the engine

Status: done 2026-09-30. Reports: `coord\research\v2_audit.md` and `coord\research\engine_decision.md` (recommendation: stay on Unity 6; waiting for Peter).

## Brief (the prompt as given)

You're helping Peter plan Dungine.v3, a Curse of Strahd (D&D 5e) party RPG he'll play with friends.

**Context**
- `E:\Unity\Projects\dungine.v2` is a working Unity demo: BG3-style, three customisable characters, the Curse of Strahd
  module, and every asset generated in code. Peter wants v3 to "pretty much" reuse that demo, re-assetted with his new
  voxel "3D pixel art" library, and tested further. He hasn't settled the engine: "We'll determine if unity is best for
  this pixel3d art game".
- The asset library is `E:\Assets\DND5E`. It's independent of any game; games take copies. Its `pixel3d\` side builds
  voxel models by Python script:
  - 1 voxel = 1.5 cm;
  - figures are faceless;
  - Hytale is the style reference;
  - output is `.npz` and `.vox`, and Blender renders GLB.
  Read `E:\Assets\DND5E\README.md` and `pixel3d\README.md`, and skim `pixel3d\pipeline\3dpixelartlessons.md` for the
  style and workflow. Don't modify anything there.
- v3 lives at `E:\Unity\Projects\dungine.v3`. Its one coordination folder is `coord\` (plans, agent comms, research).
  Read `coord\PLAN.md`. You write only your two report files.

**Tasks**
1. **Audit dungine.v2, read-only.** Never modify, build or run it, and don't open it in the Unity editor. Cover:
   - the Unity version, render pipeline and packages;
   - the project structure;
   - the systems that make the demo work: party, combat, dialogue, camera, navigation, UI, save;
   - how its visuals are produced (procedural meshes and materials);
   - how characters are built and animated;
   - every place where art is generated that the library would replace.

   Say what re-assetting would take: an import path for voxel GLB or VOX, animation, and shaders and lighting for a
   pixel-art look. Keep it spoiler-free: Peter will play the campaign. Describe systems and content in neutral terms,
   and name places and people only as the party first meets them. No secret identities, twists or hidden forms. Write
   `coord\research\v2_audit.md`.
2. **Engine decision.** Compare Unity (6.x, the v2 base), Godot 4.x, and any other realistic option (Unreal 5, a custom
   engine, anything else worth naming) for this project:
   - voxel pixel-art characters and props in 3D: thousands of small cubes merged into meshes;
   - a BG3-style isometric party camera and turn-based combat;
   - many NPCs, animated through skinned humanoid rigs;
   - glTF import from a Python pipeline;
   - lighting that suits a gothic Barovia;
   - performance, tooling, and licensing or cost for a private game among friends;
   - the cost of porting v2's systems.

   Web research is fine; no downloads. Give a clear recommendation, the reasons and risks, and what Peter must decide.
   Write `coord\research\engine_decision.md`.

**Rules**
- Don't touch `E:\Unity\Projects\Dungine` at all; it's another repo.
- Never modify dungine.v2.
- Don't run Blender or Unity, and don't download anything.
- Don't read `E:\Assets\DND5E\pixel3d\sealed`: it holds spoilers.
- Write in plain, direct English, with short sections and tables where they help.

**Final reply:** 5 to 10 lines: your recommendation, the key facts behind it, and the two file paths.
