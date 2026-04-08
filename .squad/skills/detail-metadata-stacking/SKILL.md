---
name: "detail-metadata-stacking"
description: "How to separate secondary detail metadata onto its own row without changing link styling"
domain: "frontend"
confidence: "medium"
source: "earned"
---

## Context
Use this when a Blazor detail screen shows multiple lightweight metadata facts and one of them should read as secondary information.

## Patterns
- Keep the existing metadata component markup intact when the content and behavior stay the same.
- Change the wrapper layout to `d-flex flex-column ... align-items-start` so items stack vertically but preserve their natural width.
- Reuse the same child classes and inline link components to avoid style drift.

## Examples
- `src\Recollections.Blazor.UI\Entries\Pages\VideoDetail.razor` stacks duration above `Original size: ...` by changing only the metadata container classes.

## Anti-Patterns
- Rebuilding the metadata block with new custom styles when layout classes already solve the problem.
- Mixing primary and secondary metadata on one crowded row when scan order matters.
