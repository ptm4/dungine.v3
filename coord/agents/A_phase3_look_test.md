# Agent A: the look test (phase 3, step 1)

Status: queued 2026-10-03, after phase 2.

## Peter's go-ahead (2026-10-03)

Asked whether glTFast (Unity's free, official glTF importer) may be downloaded into v3 for the look test, he said: "lol you
can do what you gotta i was just curious & wondering why it was seeming testing old material". He saw the fork running in
Play with v2's art and wondered why; the look test is the first time the library's voxel art appears in the game.

## What to do

As your `research\engine_decision.md` lays out (next steps, item 2):
1. **Add glTFast** to v3 from Unity's registry, pinned to an exact version in `Packages\manifest.json`. That is the one
   approved download. Nothing else, unless it's a dependency the package manager pulls with it (list them).
2. **A voxel material:** a URP shader or material for the library's GLBs, with vertex or atlas colours, lit like the
   scene, crisp (point-sampled), and with any glow the GLB carries. Keep it simple; it's a test.
3. **Put library models into the village as static models:**
   - two or three figures, using the static GLBs (`model.glb`), not the rigs yet, e.g. the fighter hero and two Chapter
     One villagers already approved (review 9);
   - a few approved props and items: a cart or wagon, a barrel, a lantern, from reviews 10, 12 and 25.
   - Place them near the party's start, where Play opens.
   - Keep everything spoiler-safe: nothing from the sealed folder, no hidden forms.
4. **Compare two looks** at v2's camera distances:
   - (a) crisp voxels at native resolution;
   - (b) a pixelated render (a low-resolution target scaled up).

   Capture both from the same view.
5. **Measure a crowd:** for example 30 library figures standing in the square. Note the frame time against v2's own
   figures.
6. **Captures for Peter** in `coord\research\look_test\`: what Play opens, each look, close-ups, the crowd. Write a short
   page or markdown with the pictures and plain notes, and ask him only what he needs to choose (the look, outlines or
   none).

## Rules

- Work only in v3. Never modify dungine.v2 or touch `E:\Unity\Projects\Dungine`.
- Read the library's files; never write into `E:\Assets\DND5E`.
- Don't read `pixel3d\sealed`.
- Close the editor instances you open when you're done. Never close one you didn't open.
- Commit the look test in v3's git with a clear message.
- Plain, direct English in anything Peter reads.

**Final reply:** what was added (versions), the captures' paths, the frame times, what Peter needs to choose.
