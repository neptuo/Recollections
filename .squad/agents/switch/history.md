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
- ✅ 2026-04-23: Validated Tank's EXIF coordinate fix with multi-layer regression coverage. Reviewed real JPEG fixture and end-to-end API import path. Approved for release. All 227 tests pass.

## Learnings

- Recollections is split across many projects, so regressions may cross UI, API, and data boundaries.
- End-to-end checks should account for both the Blazor UI and the API surface.
- The Aspire AppHost is a likely anchor for broader validation when multiple services are involved.
- `src/Recollections.Blazor.UI\Entries\Pages\VideoDetail.razor` currently renders duration and original size inside the same `d-flex flex-wrap gap-3` metadata container, so QA should explicitly verify any size-row change stays stacked on desktop and mobile.
- User-visible layout regression checks require both static visual inspection and testing across device widths.
- **EXIF coordinate parsing (ImagePropertyReader.FindCoordinate) lacks non-finite checks.** Must validate that `Math.Round()` output is finite before storing to database. Altitude has bounds checks (AltitudeBounds), but latitude/longitude do not.
- **Multi-layer input validation is essential for external data sources.** EXIF parsing, service-layer bounds checks, and optionally model validation on API edits should all guard against NaN/Infinity reaching the database.
- **Corrupted EXIF GPS array scenarios:** zero-length arrays, malformed rationals, extreme values (>999999) all produce NaN or Infinity when converted. Silent nullification is better than crash.
- **Video location uses the same pattern as Image location** — the bug likely affects both. Any fix must include Video.cs as well.
- **Regression asset path:** `src/Recollections.Api.Tests\TestData\Images\synthetic-exif-gps.jpg` is the synthetic image fixture for EXIF import coverage (generated at test-fixture initialization time, not committed).
- **Upload integration tests need explicit storage/free-limit config in `src\Recollections.Api.Tests\Infrastructure\ApiFactory.cs`.** Without those test settings, premium media validators can throw before the import path is exercised.
- **Shared normalization now lives in `src\Recollections.Entries.Data\MediaLocationSanitizer.cs`.** Image and video services both call it after metadata import and model mapping, so review media-location bugs there first.
