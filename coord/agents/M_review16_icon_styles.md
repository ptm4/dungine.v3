# Agent M: review 16, the icon style exploration (library batch U01)

Status: review 16 published 2026-09-30; round 2 running (A's voxel icons on D's chapel glass, in variants), resumed 2026-09-30.

## Brief (the prompt as given)

Peter's voxel "3D pixel art" asset library, `E:\Assets\DND5E\pixel3d`, needs game icons for a Curse of Strahd party RPG:
- 12 classes (U04);
- 29 subclasses (U05);
- 15 conditions (U06);
- the game's 66 spells (U07);
- 64 actions and class features (U08).

The plan says to pick a style first (U01). Your job is that exploration: a few clear style options, each shown on the same
sample icons, so Peter can pick one at a glance. It becomes library review 16.

**Read first**
- `E:\Assets\DND5E\pixel3d\pipeline\3dpixelartlessons.md`: the library's style. Faceless figures, Hytale as the
  reference, Barovian gothic palettes.
- `E:\Assets\DND5E\pixel3d\plan\scope_manifest.json`: batches U01 and U04 to U08 list the icons.
- `pipeline\sprite_from_vox.py` and `pipeline\item_review.py` (`sprite`): how the library turns voxel models into
  pixel-art sprites with numpy, no Blender.
- `E:\Unity\Projects\dungine.v3\coord\research\v2_audit.md`, for how the game's UI uses icons now, and its size: v2 makes
  its UI icons in code.

**Task**
1. **Design 3 or 4 distinct styles** that suit the library. Suggestions, and you may replace any:
   - (a) small voxel models turned into pixel-art icons by the library's own sprite code, on a dark gothic frame;
   - (b) flat pixel-art glyphs at 32 or 48 px, limited palette, crisp silhouettes;
   - (c) engraved or embossed metal plaques, symbol only, dark iron and tarnished gold;
   - (d) something else you judge fits Barovia better.
2. **Make the same 10 sample icons in every style:**
   - 2 classes: fighter, wizard;
   - 2 conditions: frightened, poisoned;
   - 3 spells: fire bolt, cure wounds, darkness;
   - 3 actions: attack, dash, hide.
   Show each at game size (for example 48 and 64 px) and enlarged 4 times, on a dark UI background and on a light one.
   Make them with Python (numpy and PIL, and the library's voxel code for style (a)).
3. **Put them on a page:** `pixel3d\reviews\16_icon_styles\index.html`, in the library's review page style (see
   `pipeline\outfit_review_page.py`).
   - A sheet per style, then all styles side by side on the same icons.
   - For each style: its pros and cons, how it scales to the full set of about 186 icons (effort, consistency), and how
     it reads at small sizes.
   - End with a recommendation and the question for Peter.
   - Write the page with a script, `pipeline\icon_styles_page.py`; the pictures go beside the page.

**Rules**
- New files only: your scripts in `pipeline\` (named `icon_*.py`) and the review folder's files. Don't edit any existing
  file.
- Never run Blender. No downloads: no icon packs, no fonts beyond what's installed.
- Don't touch `E:\Unity\Projects\Dungine`, never modify dungine.v2, and don't read `E:\Assets\DND5E\pixel3d\sealed`.
- Keep it spoiler-free: generic D&D icons only.
- Never mark anything approved.
- Write in plain, direct English on the page.

**Final reply:** the styles in one line each, your recommendation, and the page path.
