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

## Video Streaming Fix (2026-04-07)

### Switch Finding — PR #425 URL Revocation Bug
MediaSource object URL was revoked synchronously before browser could attach it, blocking streaming. URL must remain valid from assignment through `endOfStream()`. Fallback path for unsupported browsers must remain functional.

### Tank Decision — Progressive Streaming Path
Enabled browser response streaming in `Entries/Api.cs` for authenticated media fetches. MediaSource path delays object-URL revocation until metadata loads. Falls back to blob URL when MIME type is unsupported. Response stream lifecycle properly bounded.

### Trinity Implementation — URL Lifetime Preservation
Updated `site.js` to centralize source cleanup logic and preserve MediaSource object URL lifetime throughout stream attachment. Maintained fallback for unsupported browsers/MIME types.

**Result:** Video streaming now works end-to-end with safe cleanup and browser fallback.

## Blob URL Lifetime Management (2026-04-07)

### Trinity Decision — Playback Cleanup Timing
Keep video object URLs alive for the full lifetime of the active media element source. Do not revoke blob URLs on `loadeddata`, and do not revoke MediaSource URLs on `sourceended`. The browser continues resolving blob URLs during playback, replay, and seek even after initial data loads. Early revocation causes `GET blob:... net::ERR_FILE_NOT_FOUND` while the element is still using the source.

**Implementation:** Release blob URLs only on `emptied`/`error`/`abort`. Do not revoke MediaSource URLs on `sourceended`.

### Trinity Decision — Tighten Blob Cleanup Ordering
When assigning a new `blob:` or `MediaSource` URL to an element: (1) release the previous source first, (2) set `element.src = url`, (3) only after that register `emptied`/failure cleanup for the new URL. The source swap itself emits `emptied`; if cleanup is already armed, that event revokes the just-assigned URL and the browser fails with `ERR_FILE_NOT_FOUND`.

**Affected files:** `src/Recollections.Blazor.UI/wwwroot/js/site.js` and `src/Recollections.Blazor.Components/wwwroot/Gallery.js`

### Switch Finding — Gallery Image Provider Cleanup Gap
Image gallery lazy-load path (Gallery.js lines 239–250) creates blob URLs for images but does not register cleanup. When Gallery component re-renders, PhotoSwipe reuses cached blob URLs that are no longer valid, causing `ERR_FILE_NOT_FOUND`. Video path (site.js) correctly uses `registerMediaElementSource()` for cleanup. Gallery image path must bind blob URLs to element lifecycle or implement equivalent cleanup strategy. This is a separate architectural issue from the video streaming fix and requires team decision on cleanup binding approach.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
