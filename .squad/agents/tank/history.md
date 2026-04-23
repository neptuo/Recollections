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
- 🔴 2026-04-23: Traced production float insert failure to ImagePropertyReader.ToDoubleCoordinates() — missing non-finite validation on GPS coordinates. Decision merged to decisions.md; fix strategy documented.

## Learnings

- The API project is `src/Recollections.Api`.
- Data and domain work is spread across `Recollections.*.Data`, `Recollections.Entries`, and related shared projects.
- Backend work should preserve clear separation between transport contracts and persistence details.
- `src/VideoSizeBackfill.cs` runs as a file-based .NET app but still consumes EF/schema types from `src/Recollections.Data.Ef`.
- File-based .NET apps in this repo need an explicit `using Neptuo.Recollections;` when they reference root-namespace types such as `SchemaOptions<T>`.
- The current dev SQLite DB at `artifacts/Entries.db` lets the video backfill script start, but it currently fails at query time because the `Videos` table does not have an `OriginalSize` column yet.
- **SQL Server vs SQLite float handling**: SQLite accepts NaN/Infinity in float columns; SQL Server rejects them at parameter binding. Image location coordinates (latitude/longitude) from EXIF can produce invalid floats when parsed from corrupted EXIF data, causing INSERT failures on SQL Server but not locally. Altitude already has defensive validation via `AltitudeBounds.IsValid()`. Coordinates need the same pattern.
- **Image EXIF coordinate parsing** happens in `ImagePropertyReader.ToDoubleCoordinates()` without validation. Invalid coordinates propagate to `ImageService.SetProperties()` lines 90-91, where they bypass validation (unlike altitude at line 93) and reach EF/SQL Server as invalid parameter values (@p10/@p11).
