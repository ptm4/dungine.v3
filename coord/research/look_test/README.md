# The look test (phase 3, step 1)

Agent A, 2026-10-03. Open `index.html` for the pictures and the two questions for Peter: the look, and outlines or none.

## What was added to v3

- **glTFast 6.14.1** (`com.unity.cloud.gltfast`), pinned in `Packages\manifest.json`. It was the only package
  downloaded (2.16 MB). Its dependencies all came from the editor's built-in packages: Burst 2.0.0, Collections 6.6.0
  and Mathematics 1.4.0 (the last newly listed in the lock file).
- **`Assets\LookTest\`**, kept apart from v2's code:
  - `Models\`: copies of 15 approved library GLBs and their JSON (stored with Git LFS). Figures: the fighter hero, three
    review 9 villagers, two Barovian commoners, a Barovian noble and a guard. Others: barrel, crate, keg, lamp post,
    hanging lantern, campfire and draft horse.
  - `Resources\LookTest\DungineVoxel.shader`: URP lit, with vertex colour as albedo, glow from the material, shadows,
    SSAO, fog and Forward+ lights.
  - `Resources\LookTest\DungineOutline.shader` and `Scripts\LookTestOutline.cs`: optional outlines (a depth-edge pass
    added from code, so no renderer asset changes).
  - `Editor\LookTestBake.cs` (menu Dungine > Look Test, or `BakeOne(id)` from `tools\ev.sh`): turns each imported GLB
    into a prefab in `Resources\LookTest\Baked`. It swaps in the voxel material and removes the round ground disc
    under each figure, along with grass and pebbles left floating.
  - `Scripts\LookTest.cs`: places the models when the village or the start of the road is built, with no v2 code
    changed. Dev hooks: `Pixelate(scale)`, `Native()`, `Crowd(n)`, `V2Crowd(n)`, `ClearCrowd()`, `Measure(frames)`,
    `View(yaw, zoom)`. `LookTest.Enabled = false` brings back v2's view alone.
- `tools\lookshot.sh` (a capture at the game view's own size) and `tools\lookmeasure.sh` (frame times).

## Numbers

Frame time, median, in the village square: in the editor on an RTX 4070 Ti, vsync off, game view 1868 x 983.

| Scene | Median | 95th percentile | Triangles drawn (all passes) | Draw calls |
|---|---|---|---|---|
| Nothing added | 6.3 to 6.4 ms | 9.8 to 10.2 ms | 4.3 M | 1,380 |
| + 30 library figures (static) | 8.0 to 8.3 ms | 11.6 to 11.9 ms | 10.5 M | 1,419 |
| + 30 v2 figures (animated) | 8.3 ms | 13.3 ms | 8.7 M | 3,343 |

Ground discs removed per figure: about 9,000 triangles each, so figures are 28,000 to 57,000 triangles.

## Findings for the next steps

- glTFast imports the library GLBs as they are: vertex colours, glow materials, readable meshes, correct scale, and
  figures facing +Z.
- Dark streaks run along the voxel steps (the step sides face away from the light). They aren't shadow artefacts:
  turning the sun's shadows off doesn't change them. Fix them in the lighting pass with smoothed lighting normals or
  more fill.
- The library's GLB export drops the metal value, so steel and gold render like cloth. The planned Python game export
  should carry metal, glow and voxel AO.
- Every figure GLB still has its review ground disc; the game export should leave it out.
- The first Play after a fresh import shows cyan placeholders for a few seconds while shaders compile.
