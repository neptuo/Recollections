---
name: release-notes-writer
description: >
  Prepare Recollections release notes by finding the matching GitHub milestone,
  extracting the delivered user-facing changes, and updating
  `src/Recollections.Blazor.UI/wwwroot/release-notes.html`. USE FOR: "prepare
  release notes", "update release-notes.html", "write release notes for v0.18.0",
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

- `src/Recollections.Blazor.UI/wwwroot/release-notes.html`

Keep the existing structure:

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

- Read the current `release-notes.html`.
- Inspect its recent history with:

```bash
git --no-pager log --follow --oneline -- src/Recollections.Blazor.UI/wwwroot/release-notes.html
git --no-pager show <commit>:src/Recollections.Blazor.UI/wwwroot/release-notes.html
```

- Match the existing style:
  - one `New features` section
  - concise user-facing bullet fragments
  - no trailing periods
  - no issue numbers or PR numbers in the HTML
  - one GitHub milestone button at the end

### 3. Collect Delivered Work

- List all milestone items with:

```bash
gh api 'repos/neptuo/Recollections/issues?milestone={number}&state=all&per_page=100'
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

- Replace the existing bullet list with the new release content.
- Update the milestone link to the matching milestone number.
- Keep indentation and HTML structure consistent with the file history.

### 7. Final Check

- Make sure every bullet is user-facing.
- Make sure the milestone link ends with `?closed=1`.
- Make sure the page does not mention CI or tests.

## Example

### Example 1: Preparing a Release Page

**User Request**: `Prepare release notes for v0.18.0 and update src/Recollections.Blazor.UI/wwwroot/release-notes.html`

**Expected behavior**:

1. Find the `v0.18.0` milestone number on GitHub.
2. Review the history of `release-notes.html` to match prior wording.
3. Summarize the milestone into short user-facing bullets.
4. Update the milestone link in the HTML.

## References

- [Release note checklist](references/release-note-checklist.md)
