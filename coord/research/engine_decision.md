# Engine decision for Dungine.v3

Agent A, 2026-09-30. Based on the v2 audit (`v2_audit.md`, next to this file), the library's own files, and web research
(sources at the end). Nothing was downloaded, built or run.

## Recommendation

**Stay on Unity 6.** Fork v2 as it is (Unity 6000.6.0f1, URP), move to the 6.7 LTS when it ships late this year, and
spend the effort on the bridge from the library to the game and on the voxel look, not on a port.

Godot 4.7 is the one credible alternative. It's free and open, glTF is its native format, and its built-in volumetric
fog suits Barovia. But moving to it means rewriting most of a working game to get back to where v2 already is, and it
doesn't make any of the new voxel work easier. Unreal and the rest don't fit this project.

## Why

1. **v2 works today, and it's Unity through and through.** It's 23,836 lines of C# that build everything at runtime
   through Unity's APIs: meshes, URP materials and post-processing, UI Toolkit, the runtime NavMesh, physics, particles,
   the Input System and custom URP shaders. In Unity it runs on day one of phase 2, and Peter can play something at
   every step of the re-asset. In any other engine, nothing plays until the port is done.
2. **The hard new work is the same in every engine.** Merging voxels into efficient meshes, the skeleton and skin, LODs,
   the voxel shader and the look are Python export work plus one shader and some lighting. No engine does them for us.
3. **Unity handles our scale.** v2 already skins every character on the GPU. The library's figures are 40,000 to 75,000
   triangles as exported today, and 10,000 to 18,000 once merged properly; a crowd of 60 is well within reach. The GPU
   Resident Drawer batches static props.
4. **The agents' dev loop already exists, and it's Unity's official route.** v2's tools drive the editor through the
   Unity CLI: run C# in the running game, enter Play, capture the screen, read the console, build. Unity has deprecated
   its MCP server in favour of this CLI.
5. **It costs nothing.** Unity Personal is free under $200,000 a year in revenue, the splash screen is optional in
   Unity 6, and the runtime fee was cancelled in 2024.
6. **v2's animation is procedural code, not clips**, so it needs no animation tooling from the engine, just a skeleton.
   It will drive library figures once they carry a skeleton that maps onto v2's bones.

## The risks, and what to do about them

| Risk | What to do |
|---|---|
| **Unity's move to CoreCLR.** 6.6 turned off the domain reload by default for new projects; the 6.8 alpha (Unity 7) removes it for good. v2 keeps its singletons and caches in static fields and relies on that reload. | Keep v2's setting in the fork. Before moving past 6.7, mark the statics with Unity's new `[AutoStaticsCleanup]` attribute or reset them by hand. It's a contained job. |
| **The dev loop runs on an experimental package** (`com.unity.pipeline` 0.7.0-exp.1). | Pin the Unity and package versions in the fork, and upgrade on purpose, never automatically. |
| **URP has no built-in volumetric fog.** | Keep v2's approach (exponential fog plus particle fog banks), or add a URP volumetric package (open-source ones exist) when the look calls for it. |
| **No native glTF, and URP's Lit shader ignores vertex colours.** | Use glTFast, Unity's official glTF package (editor import with vertex colours and skins), plus one small voxel shader. |
| **Unity as a company:** the 2023 fee episode. | For a private game among friends there's no money at stake, and v2's code would port to Godot C# later if it ever had to. |

## How the options compare

| | Unity 6 (6.6 now, 6.7 LTS late 2026) | Godot 4.7 | Unreal Engine 5.8 |
|---|---|---|---|
| **Voxel meshes from Python** | glTFast imports GLB with vertex colours and skins; needs one custom voxel shader | glTF is native and vertex colours import (with some reported colour quirks); a custom voxel shader is simplest here too | Interchange imports glTF; needs a vertex-colour material; its temporal AA smears one-pixel detail |
| **BG3 camera and turn-based combat** | Done: v2's camera, combat, targeting and AI | To be rebuilt | To be rebuilt, in C++ or Blueprints |
| **Many NPCs on skinned rigs** | GPU skinning; v2's procedural animator; proven | GPU skinning; users report a CPU cost per skeleton in big crowds; fine at 50 to 100 | The strongest animation tools; the heaviest to use |
| **glTF import** | Official package | Native, the best of the three | Good |
| **Gothic lighting** | Forward+ (many small lights), soft shadows, SSAO, bloom, grading. No built-in volumetric fog. Real-time GI (Surface Cache GI) is arriving in 6.7 as a preview. | Built-in volumetric fog with fog volumes, real-time GI (SDFGI), SSIL, glow, AgX tonemapping, area lights (new in 4.7). The best fit out of the box. | Lumen, MegaLights (production-ready in 5.8), volumetric fog: the best quality, on the strongest PCs only |
| **Tooling for agents** | The Unity CLI (official, and proven by v2) | Command line plus community MCP plugins; scenes are text files | Python and Remote Control; slow C++ builds; binary Blueprints |
| **Licence and cost for a private game** | Free (Personal, under $200k a year) | Free (MIT) | Free (royalties only past $1M in revenue) |
| **Porting v2** | Nothing | About 17,000 of the 23,836 lines to rewrite or adapt | Everything, in C++ or Blueprints |
| **Getting it to the friends** | A Windows build; a web build is possible (WebGPU is fully supported in 6.6) | A Windows build; C# projects can't export to the web yet | A Windows build |

## Engine by engine

### Unity 6

- **For:** v2 runs as is; its dev loop, dev hooks and fixes for build traps all carry over. Forward+ and the GPU
  Resident Drawer suit many small lights and many voxel props. Web builds are possible. URP is now Unity's one standard
  renderer (the built-in pipeline was deprecated in 6.5), and 6.7 adds Surface Cache GI and screen-space reflections
  to it.
- **Against:** the CoreCLR change will need a clean-up of static state; there's no built-in volumetric fog; glTF and
  vertex colours need a package and a shader. Unity has paused new animation features to focus on CoreCLR, which
  doesn't affect v2's procedural animation.
- **Versions:** 6.6 is a "Supported" release until the 6.7 LTS arrives (late 2026); the 6.3 LTS is supported to
  December 2027.

### Godot 4.7

- **For:** MIT licence and no company risk; glTF is its native format; volumetric fog, fog volumes and real-time GI
  work out of the box, which suits Barovia's mists; a light, fast editor; text scene files; C# (.NET 8 and later), so
  v2's rules and campaign code would carry over with edits. It can bake a navigation mesh at runtime from colliders,
  as v2 does. 4.6 brought back a modular IK system (two-bone, FABRIK and more).
- **Against:** everything that touches the engine must be rewritten: the UI (3,200 lines of UI Toolkit), actors and
  navigation, the camera, picking, occlusion, the four HLSL shaders, particles, terrain, input, and 209 lines of
  coroutine code (Godot has no coroutines of that kind). Godot is right-handed and looks down −Z, where Unity is
  left-handed and looks down +Z, so every hand-placed position and angle in the campaign needs care. There's no
  official match for the Unity CLI's "run this C# in the live game", so the dev loop would be rebuilt on community
  plugins. The crowd cost per skeleton is a known issue: fine at our scale, but worth watching.
- **When it would be right:** starting from nothing, or if Unity's licence or direction changed badly. Neither is the
  case today.

### Unreal Engine 5.8

- **For:** the best lighting (Lumen, MegaLights, volumetric fog) and the best animation tools.
- **Against:** a full rewrite in C++ or Blueprints; an editor that wants 32 GB of RAM and idles at 8 to 12 GB; its
  defaults (Lumen, temporal upscaling) push toward realism and blur one-pixel voxel detail; slow compile loops and
  binary Blueprints make agent work harder; the friends would need strong GPUs. Too heavy for this project.

### Others

| Option | Verdict |
|---|---|
| **Stride** (C#, MIT, 4.3 with .NET 10) | Closest to Unity in spirit, but a smaller community and a full port. No gain over Godot. |
| **Flax** (C#/C++; royalty only past $250k a quarter) | Unity-like, small community, full port. |
| **Bevy** (Rust, 0.19) | No visual editor yet, breaking changes each release, and a rewrite in Rust. No. |
| **A web stack** (three.js, Babylon.js) | Friends could play from a link, but everything (navigation, UI, audio, tools) is rebuilt from scratch. Unity's web build covers this need if it arises. |
| **A custom engine** | Months of engine work before any game. No. |
| **A Hytale mod** | The style reference itself: in early access since January 2026, with server-side Java mods, so friends join a modded server without installing mods. But it's a real-time sandbox, not a turn-based party RPG, and v2 would be thrown away. A different game. |

## What porting v2 would cost

v2's lines by what happens to them:

| Kind of code | Lines | Unity | Godot | Unreal |
|---|---|---|---|---|
| Art generators the library replaces (bodies, heads, armour, weapons, beasts, textures, buildings, props, nature, icons, the sound synth) | about 6,400 | retired bit by bit | retired | retired |
| Engine-neutral logic: rules, combat maths, AI, dialogue model, campaign scripts, save data, dice | about 7,200 | kept | adapted: Unity types, coroutines, coordinates | rewritten |
| Engine-facing code: game loop, actors and navigation, camera, input, occlusion, UI, animators, atmosphere, FX, audio playback, dev tools, shaders | about 10,200 | kept | rewritten | rewritten |

In agent time, a Godot port is probably days to a few weeks: every v2 script was last changed on one day, 23 September,
so agents clearly write code at this scale fast. The real cost is re-testing every system to v2's standard, which only
Peter's play can confirm, and meeting a new engine's traps again. v2's list of solved traps is Unity-specific.

## Performance: what the voxels cost (the same in any engine)

Measured from library models (see `v2_audit.md`, 9.1):

| On screen | Triangles as exported today | Merged with a colour atlas | Half-resolution LOD |
|---|---|---|---|
| The party plus 20 villagers | about 1.3 million | about 0.3 million | about 0.25 million |
| A crowd of 60 | about 3.3 million | about 0.8 million | about 0.6 million |

- Characters are affordable in every engine. The GPU skins about twice as many vertices as triangles, so merging and
  LODs matter most on the friends' older machines.
- **Environments are the real risk.** At 1.5 cm voxels, one face of a 10 m by 3 m wall is 266,000 triangles before
  merging. Buildings must be merged into textured boxes (the way Hytale builds with boxes and pixel textures) or built
  at a coarser voxel (agent E's study). That's the same problem in Unity, Godot and Unreal.
- At v2's default camera distance, one voxel covers about 1.7 pixels at 1080p. The library's detail sits right at pixel
  scale, so native-resolution rendering with MSAA is the natural look. A low-resolution "pixelate" pass would throw
  detail away except in close-ups.

## What Peter must decide

1. **The engine.** Unity (recommended), or a port to Godot. If there's doubt, a short Godot test (one library figure,
   fog, a crowd) could run beside the Unity fork, but it won't change the porting cost.
2. **How the friends play.** Each on their own copy (as v2 works now: one player runs the whole party), taking turns on
   one PC, or online together, each with a character. Online play is a big feature in any engine, and v2 has no
   networking. It changes how v3's code should be organised, so it's best decided early. It doesn't change the engine
   choice: Unity and Godot both have the networking pieces a turn-based game needs.
3. **The look.** Crisp voxels at native resolution (recommended to try first), or a pixelated render; outlines or
   none; v2's free orbit camera, or fixed angles.
4. **The character creator.** A choice of library bodies and parts, with armour fitted per body, in place of v2's
   sliders. And which races: v2 has 12; the library covers 8 today, with more planned and some cut.
5. **The premade party.** What replaces v2's pilots. The library's Arkus stays out of the game until Peter implements
   him himself.
6. **Unity versions.** Fork on 6000.6.0f1, move to the 6.7 LTS when it ships, and stay off 6.8 and Unity 7 until the
   statics are cleaned up.
7. **Volumetric fog**, when the look pass comes: v2's fog banks, an open-source URP package, a paid asset, or a custom
   pass.

## If Peter picks Unity: next steps

1. **Phase 2:** copy v2 into v3 without `Library`, the captures and the build (about 2.1 GB), commit it to git first
   (v2's own repo has no commits), and confirm that it runs unchanged.
2. **A look test** in the fork: add glTFast and a voxel shader, then put two or three library items and figures into
   the village as static models. Compare native resolution with a pixelated render at v2's camera distances, and
   measure a crowd.
3. **The bridge:** a Python game export from the `.npz` (merged mesh, voxel AO, metal and glow, a skin on agent B's
   skeleton mapped to v2's bones, sockets, LODs, colliders; no Blender), following agent B's rig proposal.
4. **Phases 3 and 4** as the plan lays out: figures first, then areas as the library batches land.

## Sources

- Unity 6.6 release notes: [Unity 6.6 is now available](https://discussions.unity.com/t/unity-6-6-is-now-available/1735357)
- Unity release support: [Unity 6 releases and support](https://unity.com/releases/unity-6/support)
- Unity's 2026 roadmap: [Digital Production](https://digitalproduction.com/2025/11/26/unitys-2026-roadmap-coreclr-verified-packages-fewer-surprises/),
  [Unity's roadmap from 6.4 to 6.8](https://daily.dev/posts/unity-s-roadmap-from-6-4-to-6-8-render-pipelines-ai-tools-coreclr-and-web-improvements-feibgy4zs)
- CoreCLR and domain reload: [CoreCLR update, June 2026](https://discussions.unity.com/t/coreclr-scripting-and-serialization-update-june-2026/1723299),
  [Domain reloading issues (Unity 6.7 manual)](https://docs.unity3d.com/6000.7/Documentation/Manual/project-auditor/domain-reloading-issues.html)
- Unity pricing: [Unity is canceling the Runtime Fee](https://unity.com/blog/unity-is-canceling-the-runtime-fee),
  [Unity pricing updates](https://unity.com/products/pricing-updates)
- Unity CLI and MCP: [Unity MCP (deprecated in favour of the CLI)](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.20/manual/integration/unity-mcp-overview.html),
  [Unity Pipeline package](https://docs.unity.com/en-us/unity-production-pipeline/local-tools-cli/unity-pipeline-package)
- glTFast features: [glTFast 6.10 features](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@6.10/manual/features.html)
- URP volumetric fog: [URP 6.3 still has no native volumetric fog](https://discussions.unity.com/t/urp-6-3-still-has-no-native-volumetric-fog/1735239),
  [CristianQiu/Unity-URP-Volumetric-Light](https://github.com/CristianQiu/Unity-URP-Volumetric-Light)
- Surface Cache GI: [Unity getting real-time GI](https://gamefromscratch.com/unity-getting-real-time-global-illumination/),
  [Surface Cache GI preview](https://discussions.unity.com/t/surface-cache-gi-preview/1720494)
- Unity WebGPU: [WebGPU out of experimental in Unity 6.6](https://discussions.unity.com/t/webgpu-out-of-experimental-in-unity-6-6/1734694)
- Godot releases: [Godot 4.6](https://godotengine.org/releases/4.6/), [Godot 4.7](https://godotengine.org/releases/4.7/),
  [Godot 4.7.2](https://www.opensourceforu.com/2026/08/godot-4-7-2-released/)
- Godot features: [Volumetric fog and fog volumes](https://docs.godotengine.org/en/latest/tutorials/3d/volumetric_fog.html),
  [NavigationServer3D](https://docs.godotengine.org/en/stable/classes/class_navigationserver3d.html),
  [C#/.NET in 4.7](https://docs.godotengine.org/en/4.7/tutorials/scripting/c_sharp/index.html),
  [glTF vertex colours used by default](https://github.com/godotengine/godot/pull/41007),
  [a reported vertex-colour quirk](https://github.com/godotengine/godot/issues/107621),
  [C# web export still in progress](https://github.com/godotengine/godot/pull/118976)
- Godot crowd cost: [Skeleton heavy CPU bottleneck](https://github.com/godotengine/godot/issues/93568),
  [crowds of animated characters](https://forum.godotengine.org/t/performance-of-crowds-of-animated-rigged-characters-in-3d/130409)
- Godot MCP plugins: [Godot-MCP](https://godotengine.org/asset-library/asset/5245)
- Unreal: [Unreal Engine 5.8](https://www.unrealengine.com/news/unreal-engine-5-8-is-now-available),
  [hardware specifications](https://dev.epicgames.com/documentation/en-us/unreal-engine/hardware-and-software-specifications-for-unreal-engine),
  [Unreal EULA](https://www.unrealengine.com/eula/unreal)
- Others: [Stride](https://www.stride3d.net/), [Flax FAQ](https://flaxengine.com/faq/), [Bevy 0.19](https://bevy.org/news/bevy-0-19/)
- Hytale: [An introduction to making models for Hytale](https://hytale.com/news/2025/12/an-introduction-to-making-models-for-hytale),
  [Hytale early access](https://hytale.com/news/2025/11/hytale-early-access-january-13-2026),
  [Hytale modding strategy](https://hytale.com/news/2025/11/hytale-modding-strategy-and-status)
- 3D pixel-art rendering: [3D Pixel Art Rendering (David Holland)](https://www.davidhol.land/articles/3d-pixel-art-rendering/)
- Rigid versus skinned voxel animation: [Working with voxels in gamedev (80.lv)](https://80.lv/articles/working-with-voxels-in-gamedev)
