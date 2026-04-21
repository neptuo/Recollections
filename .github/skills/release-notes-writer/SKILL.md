---
name: release-notes-writer
description: >
  Prepare Recollections release notes by finding the matching GitHub milestone,
  extracting the delivered user-facing changes, and updating
  `src/Recollections.Blazor.UI/wwwroot/release-notes.json`. USE FOR: "prepare
  release notes", "update release-notes.json", "write release notes for v0.18.0",
  summarizing a milestone, release summary from GitHub issues or PRs. DO NOT
  USE FOR: exhaustive changelogs, CI/test summaries, deployment notes, or API
  release notes that do not update the Blazor UI release notes page. INVOKES:
  gh CLI and git history.
---

# Release Notes Writer

Use this skill to update the Blazor UI release notes page for a specific
Recollections release.

## Target File

Update only:

- `src/Recollections.Blazor.UI/wwwroot/release-notes.json`

The file is a JSON array ordered newest-first. Each element has:

- `version` — the release version string (e.g. `"0.19.0"`)
- `milestone` — the GitHub milestone number (integer)
- `breakingChanges` — array of plain-text strings; use `[]` if none
- `newFeatures` — array of plain-text strings; use `[]` if none
- `bugFixes` — array of plain-text strings; use `[]` if none

The component renders the HTML `<h3>` headings, `<ul>` lists, and the GitHub
milestone button automatically from these plain-text strings.

Example shape:

```json
[
    {
        "version": "0.19.0",
        "milestone": 50,
        "breakingChanges": [],
        "newFeatures": [
            "New Highest altitude view, with dedicated versions on beings and user profiles",
            "Swipe between months on the calendar view"
        ],
        "bugFixes": []
    },
    {
        "version": "0.18.0",
        "milestone": 49,
        "breakingChanges": [],
        "newFeatures": [
            "..."
        ],
        "bugFixes": []
    }
]
```

**Always prepend** the new release object at the top of the array so entries
remain ordered newest-first. Do **not** remove or modify existing entries.

Because items are plain text, **do not include HTML tags** in the string values.
The component handles all markup rendering.

## Process

### 1. Find the Release Milestone

- Read the user-requested version, for example `v0.18.0`.
- Find the matching GitHub milestone with:

```bash
gh api repos/neptuo/Recollections/milestones --paginate --jq '.[] | [.number, .title, .state] | @tsv'
```

- If the version does not map cleanly to a milestone title, ask the user before
  editing.

### 2. Learn the Repo's Release-Note Style

- Read the current `release-notes.json`.
- Inspect its recent history with:

```bash
git --no-pager log --follow --oneline -- src/Recollections.Blazor.UI/wwwroot/release-notes.json
git --no-pager show <commit>:src/Recollections.Blazor.UI/wwwroot/release-notes.json
```

- Match the existing style:
  - concise user-facing bullet fragments
  - no trailing periods
  - no issue numbers or PR numbers in the items

### 3. Collect Delivered Work

- List all delivered milestone items with:

```bash
gh api --paginate 'repos/neptuo/Recollections/issues?milestone={number}&state=closed&per_page=100'
```

- Read issue or PR bodies for items whose titles are ambiguous.
- Use labels and titles to separate visible product changes from internal work.

### 4. Filter for User-Facing Changes

Keep:

- visible features
- navigation, sharing, search, loading, and media improvements
- map and timeline improvements
- bug fixes users would notice

Exclude:

- CI, tests, deployment, publishing, screenshots, and sample data
- refactors and internal performance work unless the improvement is visible to users
- code-only cleanup like library swaps

### 5. Write Release Bullets

- Prefer 6-12 bullets across all sections, depending on scope.
- Lead with the biggest visible features in `newFeatures`.
- Group related small fixes into one bullet instead of mirroring each issue title.
- Translate internal issue names into product language.
- Put genuine regressions or behaviour changes in `bugFixes`.
- Leave `breakingChanges` empty (`[]`) unless the release contains a true breaking change.

Good:

- `Search in Entry picker`
- `Better loading states, faster story lists, and small UI fixes`

Avoid:

- `Optimize StoryListMapper to avoid N+1 query pattern`
- `Remove jQuery`
- `Fix publish`

### 6. Update the File

- Build the new entry object with `version`, `milestone`, and the three item arrays.
- Prepend the new JSON object at the start of the array in `release-notes.json`.
- Keep indentation and JSON structure consistent with the file.

### 7. Final Check

- Make sure every bullet is user-facing and free of HTML tags.
- Make sure the `milestone` number is correct.
- Make sure the new entry is at index 0 (newest-first).
- Make sure the JSON is valid.

## Example

### Example 1: Preparing a Release Page

**User Request**: `Prepare release notes for v0.18.0 and update src/Recollections.Blazor.UI/wwwroot/release-notes.json`

**Expected behavior**:

1. Find the `v0.18.0` milestone number on GitHub.
2. Review the history of `release-notes.json` to match prior wording.
3. Summarize the milestone into short user-facing bullets.
4. Prepend the new entry at the top of the JSON array.

## References

- [Release note checklist](references/release-note-checklist.md)
