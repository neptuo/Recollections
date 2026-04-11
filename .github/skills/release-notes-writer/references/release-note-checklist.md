# Release Note Checklist

Use this checklist after collecting milestone items and before editing
`src/Recollections.Blazor.UI/wwwroot/release-notes.html`.

## Keep the Existing Format

- One `<h3>New features</h3>` heading.
- One `<ul>` with short bullet fragments.
- No trailing periods.
- No issue numbers, PR numbers, or commit SHAs in the HTML.
- One GitHub button pointing to the matching milestone with `?closed=1`.

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
git --no-pager log --follow --oneline -- src/Recollections.Blazor.UI/wwwroot/release-notes.html
git --no-pager show <commit>:src/Recollections.Blazor.UI/wwwroot/release-notes.html
```
