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
- `html` — the full HTML markup for that release (same structure as the old
  `release-notes.html` file)

Example shape:

```json
[
    {
        "version": "0.19.0",
        "html": "<h3>New features</h3>\n<ul>\n    <li>...</li>\n</ul>\n\n<div class=\"row\">...</div>"
    },
    {
        "version": "0.18.0",
        "html": "..."
    }
]
```

**Always prepend** the new release object at the top of the array so entries
remain ordered newest-first. Do **not** remove or modify existing entries.

Keep the `html` value consistent with the previous HTML structure:

```html
<h3>New features</h3>
<ul>
    <li>...</li>
</ul>

<div class="row">
    <div class="col-12 col-md-auto">
        <a target="_blank" href="https://github.com/neptuo/Recollections/milestone/{number}?closed=1" class="btn bg-light-subtle w-100">
            <span class="fab fa-github"></span>
            See details on GitHub
        </a>
    </div>
</div>
```

Escape all double-quotes in the HTML value as `\"` and represent newlines as
`\n` (standard JSON string escaping).

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
  - one `New features` section
  - concise user-facing bullet fragments
  - no trailing periods
  - no issue numbers or PR numbers in the HTML
  - one GitHub milestone button at the end

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

- Prefer 6-12 bullets, depending on scope.
- Lead with the biggest visible features.
- Group related small fixes into one bullet instead of mirroring each issue title.
- Translate internal issue names into product language.

Good:

- `Search in Entry picker`
- `Better loading states, faster story lists, and small UI fixes`

Avoid:

- `Optimize StoryListMapper to avoid N+1 query pattern`
- `Remove jQuery`
- `Fix publish`

### 6. Update the File

- Build the HTML string for the new entry following the template above.
- Prepend a new JSON object `{ "version": "X.Y.Z", "html": "..." }` at the
  start of the array in `release-notes.json`.
- Update the milestone link to the matching milestone number.
- Keep indentation and JSON structure consistent with the file.

### 7. Final Check

- Make sure every bullet is user-facing.
- Make sure the milestone link ends with `?closed=1`.
- Make sure the page does not mention CI or tests.
- Make sure the new entry is at index 0 (newest-first).
- Make sure the JSON is valid (no unescaped quotes or bare newlines in strings).

## Example

### Example 1: Preparing a Release Page

**User Request**: `Prepare release notes for v0.18.0 and update src/Recollections.Blazor.UI/wwwroot/release-notes.json`

**Expected behavior**:

1. Find the `v0.18.0` milestone number on GitHub.
2. Review the history of `release-notes.json` to match prior wording.
3. Summarize the milestone into short user-facing bullets.
4. Prepend the new entry at the top of the JSON array.
5. Update the milestone link in the HTML value.

## References

- [Release note checklist](references/release-note-checklist.md)
