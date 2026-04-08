# Project Context

- **Project:** Recollections
- **Requested by:** Marek Fišera
- **Stack:** .NET 10, ASP.NET Core API, EF Core, Blazor UI, Aspire AppHost
- **Created:** 2026-04-04

## Core Context

Tank owns the backend delivery path for recollections, accounts, and entry/media workflows.

## Recent Updates

- 📌 Team hired on 2026-04-04
- ✅ 2026-04-08: Resolved build issue in `src\VideoSizeBackfill.cs`; video backfill migration now runs and updates 46 videos from local database

## Learnings

- The API project is `src/Recollections.Api`.
- Data and domain work is spread across `Recollections.*.Data`, `Recollections.Entries`, and related shared projects.
- Backend work should preserve clear separation between transport contracts and persistence details.
- `src/VideoSizeBackfill.cs` runs as a file-based .NET app but still consumes EF/schema types from `src/Recollections.Data.Ef`.
- File-based .NET apps in this repo need an explicit `using Neptuo.Recollections;` when they reference root-namespace types such as `SchemaOptions<T>`.
- The current dev SQLite DB at `artifacts/Entries.db` lets the video backfill script start, but it currently fails at query time because the `Videos` table does not have an `OriginalSize` column yet.
