# Squad Decisions

## Active Decisions

- **2026-04-04:** Staff the project with a five-person core squad covering lead, frontend, backend, platform, and testing responsibilities.
- **2026-04-04:** Treat Recollections as a full-stack .NET 10 product with `src/Recollections.Api` and `src/Recollections.Blazor.UI` as the primary delivery surfaces.
- **2026-04-04:** Keep Squad local-only for this repo; do not add GitHub label workflows, auto-triage, or bundled Copilot skill packs unless explicitly requested later.

## Scope Finalization (2026-04-04)

### User Directive (via Copilot, 2026-04-04T08:07:40Z)
Opt out of Squad-related GitHub workloads and triage automation. Do not create GitHub labels, auto-assignment workflows, or load bundled skills unless explicitly needed. Keep the PR lean and focused on the product.

### Dozer Decision (2026-04-04)
Remove repo-level GitHub label/triage automation and bundled `.copilot/skills/*` scaffolding. Keep local `.squad/` team context intact for coordination, but treat Squad as a local-only aid, not a source of GitHub automation.

### Morpheus Review (2026-04-04)
Reviewed PR #424 scope. Confirmed: Recollections project does not need multi-team Squad governance yet. Focus is shipping the .NET 10 product. Minimal footprint is correct:
- Keep: `.squad/decisions.md`, `.squad/identity/`, `.squad/agents/*/charter.md` (reference/record-keeping)
- Remove: `.copilot/skills/*`, all squad-related GitHub workflows, `.github/agents/squad.agent.md`, ceremony docs and templates
- Result: Cleaner repo, faster CLI startup, no GitHub automation burden

## Tactical Decisions

### Tank: Video Backfill Build Fix (2026-04-08)

Keep `src\VideoSizeBackfill.cs` as a file-based .NET app and resolve shared EF schema types by importing the repo root namespace explicitly.

**Why:** The script is compiled outside the project namespace wrapper that exists in the older tool programs, so unqualified references to `SchemaOptions<T>` do not bind automatically. Adding `using Neptuo.Recollections;` preserves the current behavior without duplicating schema helpers or changing the migration flow.

**Implementation:**
- Added `using Neptuo.Recollections;` to `src\VideoSizeBackfill.cs`.
- Verified the script now builds and runs; initial pass updated 46 videos and identified 2 missing-original edge cases.

**Learning:** File-based .NET apps in this repo need an explicit `using Neptuo.Recollections;` when they reference root-namespace types such as `SchemaOptions<T>`.

### Trinity & Switch: Video Detail Metadata Stacking (2026-04-08)

Render video original size on its own row underneath duration by stacking the metadata container vertically.

**Why:** Duration is the primary scan target in the metadata block. Treating original size as a second row preserves the existing inline-link styling while removing the cramped inline read.

**Implementation:**
- Modified `src\Recollections.Blazor.UI\Entries\Pages\VideoDetail.razor` metadata wrapper from `d-flex flex-wrap` (horizontal with wrap) to vertical stack layout.
- Size now renders below duration.

**Acceptance Criteria (verified by Switch):**
- Duration remains on its own metadata row.
- Original size appears on a separate row beneath duration when both values exist.
- Size does not sit beside duration at desktop or mobile widths.
- Entry title, parent link, date, owner, location, description, and media preview spacing remain unchanged.
- Conditional rendering works cleanly when only duration or only size exists.

**Review:** Build verified; no regressions detected.

### Tank & Switch: Production Float Insert Failure (2026-04-23)

**Issue:** Image and Video location uploads fail on SQL Server with "Parameter @p10: supplied value is not a valid instance of data type float" when EXIF GPS metadata is corrupted or malformed.

**Root Cause:** ImagePropertyReader.ToDoubleCoordinates() produces NaN or Infinity when parsing corrupted GPS arrays. These non-finite floats propagate to ImageService.SetProperties() (lines 90–91) without validation, unlike altitude (line 93, which uses AltitudeBounds.IsValid()). SQL Server rejects non-finite floats at parameter binding; SQLite accepts them, masking the bug during development.

**Why It Matters:** Corrupted EXIF GPS (zero-length arrays, malformed rationals, extreme values >999999) can produce NaN/Infinity. A single bad image upload crashes the entire operation in production.

**Implementation (Completed 2026-04-23):**
1. Added `CoordinateBounds` class with finite/range validation for latitude [-90,90] and longitude [-180,180].
2. Added `MediaLocationSanitizer` to normalize media locations in one place before persistence.
3. Updated `ImagePropertyReader` to use safe EXIF tag reads and null out malformed GPS arrays/non-finite values.
4. Updated `ImageService` and `VideoService` to sanitize media locations after EXIF/video metadata import and manual model mapping.
5. Added regression coverage with real JPEG fixture at `src\Recollections.Api.Tests\TestData\Images\20260423_073316.jpg`.

**Files Created:**
- `src/Recollections.Entries.Data/CoordinateBounds.cs`
- `src/Recollections.Entries.Data/MediaLocationSanitizer.cs`
- `src\Recollections.Api.Tests\TestData\Images\20260423_073316.jpg`
- `src\Recollections.Api.Tests\Entries\ImagePropertyReaderRegressionTests.cs`
- `src\Recollections.Api.Tests\Entries\ImageImportRegressionTests.cs`

**Files Modified:**
- `src/Recollections.Entries/ImagePropertyReader.cs`
- `src/Recollections.Entries/ImageService.cs`
- `src/Recollections.Entries/VideoService.cs`

**Validation:** All 227 tests pass. Multi-layer validation: parser → service → EF/SQL Server chain holds.

**Follow-Up:** Monitor Video.Location handling post-deployment. Consider extracting CoordinateBounds as a reusable utility if other services also process GPS data.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
