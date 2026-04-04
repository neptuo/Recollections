# Project Context

- **Project:** Recollections
- **Requested by:** Marek Fišera
- **Stack:** .NET 10, ASP.NET Core API, Blazor UI, EF Core, Aspire AppHost
- **Created:** 2026-04-04

## Core Context

Switch protects the release path by focusing on regressions, edge cases, and readiness.

## Recent Updates

- 📌 Team hired on 2026-04-04

## Learnings

- Recollections is split across many projects, so regressions may cross UI, API, and data boundaries.
- End-to-end checks should account for both the Blazor UI and the API surface.
- The Aspire AppHost is a likely anchor for broader validation when multiple services are involved.
