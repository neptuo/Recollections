---
last_updated: 2026-04-04T07:48:20.505Z
---

# Team Wisdom

Reusable patterns and heuristics learned through work. NOT transcripts — each entry is a distilled, actionable insight.

## Patterns

<!-- Append entries below. Format: **Pattern:** description. **Context:** when it applies. -->

- **Pattern:** Treat `src/Recollections.Api` and `src/Recollections.Blazor.UI` as the two primary execution surfaces, with the remaining projects supporting shared domain and infrastructure concerns. **Context:** planning or splitting feature work across the solution.
- **Pattern:** Prefer running the full product through the Aspire AppHost when validating end-to-end behavior. **Context:** local development, multi-service debugging, and release readiness checks.
