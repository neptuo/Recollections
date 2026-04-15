# Project Context

- **Project:** Recollections
- **Requested by:** Marek Fišera
- **Stack:** .NET 10, ASP.NET Core API, Blazor UI, EF Core, Aspire AppHost
- **Created:** 2026-04-04

## Core Context

Switch protects the release path by focusing on regressions, edge cases, and readiness.

## Recent Updates

- 📌 Team hired on 2026-04-04
- 2026-04-08: Reviewed VideoDetail.razor metadata stacking — verified no regressions at desktop/mobile widths. Conditional rendering (duration-only, size-only) works cleanly. Approved for release.

## Learnings

- Recollections is split across many projects, so regressions may cross UI, API, and data boundaries.
- End-to-end checks should account for both the Blazor UI and the API surface.
- The Aspire AppHost is a likely anchor for broader validation when multiple services are involved.
- `src/Recollections.Blazor.UI\Entries\Pages\VideoDetail.razor` currently renders duration and original size inside the same `d-flex flex-wrap gap-3` metadata container, so QA should explicitly verify any size-row change stays stacked on desktop and mobile.
- User-visible layout regression checks require both static visual inspection and testing across device widths.
