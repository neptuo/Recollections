# Project Context

- **Project:** Recollections
- **Requested by:** Marek Fišera
- **Stack:** .NET 10, Blazor WebAssembly/UI, ASP.NET Core API, EF Core, Aspire AppHost
- **Created:** 2026-04-04

## Core Context

Trinity owns the Blazor UI experience for the recollections product.

## Recent Updates

- 📌 Team hired on 2026-04-04
- ✅ **2026-04-07:** Fixed video streaming URL lifetime issue in `site.js` — preserved MediaSource object URL through full playback lifecycle and centralized cleanup logic
- ✅ **2026-04-07:** Follow-up fix: moved cleanup registration to after `element.src = url` in both `site.js` and `Gallery.js`; hardened `emptied` event handling to prevent fresh URLs from being revoked during attachment

## Learnings

- The main UI lives in `src/Recollections.Blazor.UI`.
- Shared Blazor components also exist in `src/Recollections.Blazor.Components`.
- UI work should stay coordinated with the API and shared models rather than duplicating server logic.
- MediaSource-backed video playback requires precise URL lifecycle management: URL must remain valid from assignment through `mediaSource.endOfStream()`, and cleanup must be deferred until playback completion or error.
- Blob-backed video URLs cannot be revoked on `loadeddata`; browsers still resolve the `blob:` source during playback and seek.
- MediaSource URLs must also outlive `sourceended`; release them only when the element is reset, replaced, or fails.
- The runtime fix for this regression lives in `src/Recollections.Blazor.UI/wwwroot/js/site.js`, with fallback parity in `src/Recollections.Blazor.Components/wwwroot/Gallery.js`.
- When a media element swaps to a new object URL, `emptied` can fire during `element.src = url`; arm cleanup listeners only after assignment so the fresh `blob:` URL is not revoked mid-attach.
- Gallery video replay state lives best in `src/Recollections.Blazor.Components/wwwroot/Gallery.js`: cache the created `HTMLVideoElement` on the gallery item model and reattach it on reopen so the browser can reuse the buffered media instead of starting a fresh fetch.
- When reusing a cached gallery video element, pause non-active cached videos on slide change/close; that keeps audio from leaking while preserving the already-buffered source for replay.

## 2026-04-07 Video Streaming Fix

### Implementation
Updated `src/Recollections.Blazor.UI/wwwroot/js/site.js` to fix MediaSource object URL lifetime:
- Preserved URL validity from assignment through stream completion
- Centralized source cleanup logic to prevent premature revocation
- Maintained fallback behavior for unsupported browsers/MIME types
- Properly wrapped response stream lifecycle

### Result
✅ Node syntax check passed  
✅ Browser fallback path preserved  
✅ Coordinated with Tank on API streaming enablement
