# Release Note Checklist

Use this checklist after collecting milestone items and before editing
`src/Recollections.Blazor.UI/wwwroot/release-notes.json`.

## Keep the Existing Format

- Items are **plain-text strings** — no HTML tags in the values.
- The component renders `<h3>` headings, `<ul>` lists, and the GitHub button automatically.
- No trailing periods.
- No issue numbers, PR numbers, or commit SHAs in the items.
- The new entry must be **prepended** at the top of the JSON array (newest-first).
- Leave `breakingChanges` and `bugFixes` as `[]` if there are none.
- The `milestone` field is the integer GitHub milestone number.
- The JSON must remain valid.

## Prefer Product Language

Write what users notice, not how the code changed.

Good:

- `Map can highlight visited countries`
- `Direct links to Story and Beings from shared entries`
- `Better loading states, faster story lists, and small UI fixes`

Avoid:

- `Optimize StoryListMapper to avoid N+1 query pattern`
- `Remove jQuery`
- `Fix publish`

## What to Exclude

Do not mention:

- CI or tests
- deployment and publishing changes
- screenshots and sample data work
- internal refactors unless users directly feel the effect

## Group Small Changes

Do not mirror GitHub item titles one by one when several items describe the same
surface area.

Examples:

- Several loading and polish items -> `Better loading states, faster story lists, and small UI fixes`
- Multiple media presentation tweaks -> `Video size warning and better fit for tall media in detail`

## Milestone Review Commands

```bash
gh api repos/neptuo/Recollections/milestones --paginate --jq '.[] | [.number, .title, .state] | @tsv'
gh api --paginate 'repos/neptuo/Recollections/issues?milestone={number}&state=closed&per_page=100'
git --no-pager log --follow --oneline -- src/Recollections.Blazor.UI/wwwroot/release-notes.json
git --no-pager show <commit>:src/Recollections.Blazor.UI/wwwroot/release-notes.json
```
