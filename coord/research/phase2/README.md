# Phase 2: v2 forked into v3

Agent A, 2026-10-03. v3 now holds an unchanged copy of the v2 demo, and it runs as v2 does.

## The pictures

- `v3_default_screen.png`: what Play opens in v3 (the main menu).
- `v2_v3_default_screen.png`: v2's own capture of the same screen (left) beside v3's (right). The menu, the scene and
  the buttons are the same, including Continue, because v2's two saves were copied over. v3 shows the ground and gate
  textures a little more clearly; v2's own captures varied from run to run in the same way.
- `v3_first_area.png`: the first area after New Game, with the premade party.

## What was done

1. **Backup:** `E:\Backups\dungine.v2_2026-10-02.zip` (502 MB, 2,032 files, checked by reopening it). It leaves out only
   the folders Unity rebuilds (`Library`, `Logs`) and the build.
2. **Copy:** 275 files, byte for byte, into this folder beside `coord\`. Left out: `Library`, `Logs`, the build, the
   screenshot folders (about 485 MB) and the generated `.csproj` and `.slnx` files. The screenshot folders exist again,
   empty, because v2's tools write into them; git ignores their contents.
3. **Git:** a new repo here, with `coord\` in it. Commits: the unchanged fork, then the fixes below, then a change so
   files are stored byte for byte as v2 has them.
4. **Running it:** Unity 6000.6.0f1 imported and compiled the copy with no errors. v2's own tools then ran against it:
   compile, Play and capture, the run-C# hook, a tour of all 12 areas, a dialogue driven by the tools, an AI-played test
   fight, and a recorded walk cycle turned into a GIF. None raised an error.

## What differs from v2, and why

- **The dev tools point at v3.** Every tool had `cd` into v2's folder; left alone, they would have driven v2.
- **v3 has its own product name**, "Dungine v3 - The Curse of Strahd", so its saves and settings live apart from v2's.
  With v2's name, v3's autosaves would have overwritten v2's.
- **The very first Play** in a fresh copy shows some models in flat cyan for a few seconds while Unity compiles their
  shaders. It happens once; later Plays look right.
- Not done: a Windows build of v3. v2's build test (`tools\runbuild.ps1`) is ready for when one is needed.
