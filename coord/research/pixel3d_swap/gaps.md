# Pixel3d swap: the gaps (agent A, 2026-10-04)

These block a wholly pixel3d capture of the village and the default screen. They are listed most visible first. Who
fills each one is the main session's call; the likely owner is in brackets. The details are in `inventory.md`.

1. **The ground and the roads** (agent E's terrain E02, then agent N's export). The valley floor, the verges, the mud
   high street and lanes cover most of every frame, in the village and on the title screen.
   - The library has the tiles (review 43: dead grass, dirt, mud road, rock, forest floor) and village example layouts,
     but no game files yet.
   - v3 needs the tiles exported (atlas, LODs) and a ground layout for the village, as for the kit.
   - The square is already the kit's cobbles: v3 lowers v2's ground under them.
2. **Trees, undergrowth, grass** (agent E's vegetation, review 44, not started). In the village that's 520 pines and
   73 dead trees, 520 bushes, 1,040 ferns and 1,600 grass tufts; on the title screen, 70 pines, 40 dead trees and grass.
   **Rocks, stumps and fallen logs** (104, 65, 52) have no library version either.
3. **The party.**
   - Arkus and Chai'rn need rigged game exports from their locked library models (agent N, as for the 19 rigs).
   - Dulandir has no library design; his brief is Peter's.
   - Until then the party on screen is v2's, which alone rules out any capture.
   - Arkus's and Chai'rn's HUD portraits are the library's already; Dulandir's portrait waits on his model.
4. **The villagers** (4 in the square and street): rigged exports of the Barovian commoner looks,
   `characters\looks\barovian_commoner_m`, `_f` and their second looks (agent N). The library has them, but only as
   static figures.
5. **The castle on the skyline**, seen from the village and on the title screen, and **the village gates** on the title
   screen. Neither is in the library (a library agent).
6. **Graveyard pieces** (agent N, all exports of what the library has):
   - the four gravestones as separate models: the library's `gravestone_4_shapes` holds all four in one;
   - the open grave needs the library terrain (a hole in the ground);
   - the garlic strings without their door;
   - claw marks as a door overlay (both go on the kit's manor door).
7. **The HUD's top-right buttons**: bag, character, journal, camp, settings. There are no UI icons for these in the
   library yet (the icon agent; the same style as reviews 27 to 29).
8. **Small things**:
   - a broken cart look (one, by the manor drive);
   - the night enemies on the high street (night only, so they stay out of day captures).

## Requests to agent N for things already swapped (for cost and look, not for the capture)

- **Game exports (atlas, AO, LODs, no review base) of the props now swapped from the look test's bake.** These are
  bench, cart, haybale, woodpile, grave mound, coffin and glaive. The bakes are heavy: the cart is 140 k triangles,
  the coffin 106 k, the woodpile 71 k, and 18 grave mounds come to 0.9 M.
- **The cobbles.** The kit's square costs about 4.5 M triangles a frame at play zoom, even with 1.5 cm stones only
  within 16 m of the camera. A `--no-ao` export of `cobbled_square` (about half), or the occlusion baked into the
  texture, would let them show at full detail everywhere. At 3 cm the stones lose their shape and read as speckle.
