# Agent N: the library's game export (engine-neutral)

Status: started 2026-10-03.

Agent A's look test put approved library GLBs into v3 (`E:\Unity\Projects\dungine.v3\coord\research\look_test\`). It
found three gaps in what the library exports for games:
- the GLB export drops the metal value, so steel and gold render like cloth;
- every figure's GLB still carries its review ground disc (about 9,000 triangles), with grass and pebbles;
- there is no voxel ambient occlusion and no LODs.

It also saw thin dark streaks along the voxel step sides in Unity, which aren't shadow artefacts. Smoothed lighting
normals in the export may help; say what you think.

## What to do

A game export in the library's pipeline, beside `rig_export.py` and engine-neutral (glTF, no Unity code):
- carry each voxel's material properties: colour, metal, glow (as the review renders do);
- leave out the review base (the ground disc and its dressing), for figures and props alike;
- bake voxel ambient occlusion into the vertex colours (or a second attribute; your call, and say which);
- write LODs: the full model, then about 3 cm and 6 cm downsamples (agent E's study showed the downsampled versions
  read well at distance);
- keep merged faces and the texture atlas from your rig export;
- for rigged figures, the same, with the skin and spring chains as now.

Then re-export the 19 rigs and the approved static models the look test used, into a game folder in the library
(e.g. `pixel3d\export\game\`, one folder per asset, mirroring the library). Write a short note in that folder on the
format, for agent A.

## Rules

- Library models must not change. Regression as before.
- Never run Blender; give me commands for a Blender check of a few exported files.
- Don't read `pixel3d\sealed`.
- Lessons go in your final reply.
