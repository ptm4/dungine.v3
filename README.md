# Dungine.v3

A Curse of Strahd party RPG for Peter and friends. It's built on the working **dungine.v2** demo, re-assetted with
Peter's voxel "3D pixel art" library and tested further.

## Layout

- **`coord\`** is the one folder for plans, agent comms, research and progress:
  - `WORKBOARD.md`: the live board (running, waiting for Peter, done, next) and the update log. The main session keeps it.
  - `PLAN.md`: the goals and phases.
  - `decisions.md`: Peter's decisions, dated and in his words.
  - `agents\`: each agent task's brief (its prompt, reusable) and its report.
  - `research\` and `design\`: what the agents and the main session write for Peter.
- **Everything else** in this folder will be the game itself. It arrives once the engine is chosen (see `coord\PLAN.md`).

## Neighbours

| What | Where | How v3 uses it |
|---|---|---|
| The v2 demo | `E:\Unity\Projects\dungine.v2` | The working model v3 starts from. Read-only: never modified. |
| The asset library | `E:\Assets\DND5E` | Independent of any game: v3 takes copies of its approved assets. Pixel art is in `pixel3d\`, shared data in `data\`. |
| The first Dungine repo | `E:\Unity\Projects\Dungine` | Not used by v3. |
