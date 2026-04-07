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
Image gallery lazy-load path (Gallery.js lines 270–281) creates blob URLs for images but does not register cleanup. When Gallery component re-renders, PhotoSwipe reuses cached blob URLs that are no longer valid, causing `ERR_FILE_NOT_FOUND`. Video path (site.js) correctly uses `registerMediaElementSource()` for cleanup. Gallery image path must bind blob URLs to element lifecycle or implement equivalent cleanup strategy. This is a separate architectural issue from the video streaming fix and requires team decision on cleanup binding approach.

## Regression Test Suite & Debug Constraints (2026-04-07)

### Switch Decision — Blob URL Regression Constraints
Document exact failure modes and what Trinity's implementation must preserve to avoid regressions:

**Premature revocation (Failure Mode #1, FIXED by Trinity):**
- Error: `GET blob:... net::ERR_FILE_NOT_FOUND` within 100ms of play
- Cause: URL revoked before browser can attach or playback starts
- Trinity's fix: Cleanup fires only on `emptied`/`error`/`abort`, not `loadeddata`/`sourceended`

**Late/deferred revocation (Failure Mode #2, Image Gallery Issue):**
- Error: `GET blob:... net::ERR_FILE_NOT_FOUND` on re-open after navigation
- Cause: Cached blob URL stale or garbage-collected
- Scope: Gallery.js image provider, not Trinity's video path

**Regression test checklist:**
- Play video, verify DevTools Network: no 404s on blob: URLs
- Test fallback path (disable MediaSource support)
- Test with unsupported codec
- Test gallery images on re-open
- Play, pause, seek, replay — verify cleanup doesn't fire prematurely
- Break network mid-playback, verify error cleanup works
- Replace video element (Gallery.js does this), verify old cleanup works
- Play same video twice without reload

**Key implementation details (IMMUTABLE):**
| Component | File | Lines | Behavior |
|-----------|------|-------|----------|
| Streaming setup | site.js | 391–422 | MediaSource URL: until `sourceclose` |
| Blob fallback | site.js | 298–318 | Blob URL: until `emptied`/`error`/`abort` |
| Cleanup registry | site.js | 320–340 | Prevents duplicate cleanup via marker |
| Release hook | site.js | 342–347 | Calls registered cleanup function |
| Gallery video | Gallery.js | 48–83 | Delegates to `window.ImageSource.Set()` |
| Image provider | Gallery.js | 270–281 | Creates blob URLs; no cleanup (debt) |

### Switch Finding — Root Cause & Verdict Analysis
"It's still happening" report explained by stale local runtime assets (import-map hash mismatch) after JS changes without AppHost restart. Trinity's blob URL lifetime fix is correct and in place. Code path verification:

**Video playback (Trinity's domain):** ✅ Correct
- URL created, element assigned, cleanup registered AFTER (not before)
- Cleanup fires on `emptied`/`error`/`abort` only
- Fallback to blob URL when MediaSource unsupported
- Guards against double-firing via element marker

**Image gallery lazy-load (Architectural gap):** ⚠️ Cleanup missing
- Creates blob URLs but no lifecycle binding
- Re-render or navigation causes stale URL 404s
- Not a regression in Trinity's fix
- Requires separate team decision on cleanup strategy

**API response streaming:** ✅ Enabled (Api.cs line 103)
- Browser streaming enabled for authenticated media fetches
- MediaSource path delays object-URL revocation until metadata loads
- Falls back to blob URL when MIME type unsupported

**Required deployment verification:**
1. Verify code has lines 320–340 cleanup logic in deployed site.js
2. Test video playback on entry `4f50f8e1-2083-4486-9773-90a6dd2d9426` (jondoe/1234)
3. Monitor DevTools Network: blob: URL status 206/200 (not failed)
4. Console: No "blob: ... ERR_FILE_NOT_FOUND" errors
5. If failures persist: Isolate to video vs. image path

### Dozer Finding — Stale Import Map
Local AppHost serving `src/Recollections.Blazor.Components/wwwroot/Gallery.js` with content hash `sha256-2kn3hK6KtPm4FwMljirDASAJByet4ivKpBXzymGVHhk=` while page import map advertised `sha256-ExheHOd+idA9CFSS6aMkh2tuPeuX5eugKHgAQnwFdWw=`. Browser blocked /_content/Recollections.Blazor.Components/Gallery.js on integrity mismatch. GalleryInterop initialization failed, gallery overlay never opened, video playback never started. **Operational implication:** In Aspire + `dotnet watch` happy path, editing JS static web assets can leave running UI with stale import-map entry. AppHost restart required before validating JS-module changes.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
