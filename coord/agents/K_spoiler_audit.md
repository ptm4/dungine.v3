# Agent K: a spoiler audit of everything Peter reads in the library

Status: done 2026-09-30. Its results (library plan/research/spoiler_audit.json) were applied by the main session.

## Brief (the prompt as given)

Peter hopes to play the Curse of Strahd campaign, so nothing he reads may spoil it. The library shows him its plan and
reviews. The plan page (`E:\Assets\DND5E\pixel3d\plan\index.html`, made by `pipeline\plan_page.py` from
`scope_manifest.json` and `SCOPE_PLAN.md`) lists every asset by name, path and notes. Until today it hid only named NPCs'
notes. Asset names alone can give the game away, such as a location's secret rooms or the monsters in a place. A first
pass flagged two batches and three assets with `"spoiler": true`; flagged assets show as "hidden (spoiler)", with no
name or path. Finish the job.

**Task**
1. **Audit every asset in `scope_manifest.json`:** its `name`, `path`, `notes` and `source`, and every batch's `title`
   and `note`. Decide what Peter shouldn't see before play. Things he can see:
   - generic D&D content: standard monsters, items and spells by their book names;
   - places and people as the party first meets them.

   Things he can't: secret rooms, hidden identities or true natures, what a place holds, who is really behind
   something, plot items tied to twists, later-chapter reveals. When in doubt, flag it. It's cheap: the asset stays in
   the plan, just hidden.
2. **Audit the text pages Peter reads:**
   - `pixel3d\plan\SCOPE_PLAN.md`;
   - `pixel3d\plan\briefs\*.md`;
   - `pixel3d\index.html` (the kit catalogue);
   - `E:\Assets\DND5E\index.html`;
   - the review pages `pixel3d\reviews\*\index.html`;
   - `pixel3d\pipeline\3dpixelartlessons.md`;
   - `E:\Unity\Projects\dungine.v3\coord\*.md` and its `research\` and `design\` files.
   List any line that spoils, with a spoiler-free rewrite for each.
3. **Write your results** to `E:\Assets\DND5E\pixel3d\plan\research\spoiler_audit.json` (the research folder is already
   marked "full module spoilers" for Peter):
   - `flag`: asset ids to hide, each with a short reason code, for example `secret_place`, `true_nature`,
     `plot_item`, `later_reveal`;
   - `batch_titles`: any batch titles or notes to neutralise, with the new text;
   - `text_fixes`: file, the line text, and the rewrite.

**Rules**
- Write only `spoiler_audit.json`. Don't edit the manifest, the pages or any other file; the main session applies your
  results.
- Your final reply may be shown to Peter, so it must itself be spoiler-free: counts and reason codes only, never what an
  asset or a line reveals.
- Don't read `E:\Assets\DND5E\pixel3d\sealed`.
- Don't touch `E:\Unity\Projects\Dungine` or modify dungine.v2.
- No downloads.

**Final reply:** counts only (assets flagged per reason code, batch titles to change, text lines to fix) and the file path.
