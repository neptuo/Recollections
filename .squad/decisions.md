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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
