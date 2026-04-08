# Project Context

- **Project:** Recollections
- **Requested by:** Marek Fišera
- **Stack:** .NET 10, ASP.NET Core API, Blazor UI, EF Core, Aspire AppHost
- **Created:** 2026-04-04

## Core Context

Switch protects the release path by focusing on regressions, edge cases, and readiness.

## Recent Updates

- 📌 Team hired on 2026-04-04
- 🔍 **2026-04-07:** Completed PR #425 review; identified critical MediaSource URL revocation bug blocking video streaming
- ✅ **2026-04-07:** Fixed MediaSource URL lifetime preservation (Trinity) and Gallery.js cleanup ordering; identified Gallery image provider cleanup gap (separate architectural issue)

## Learnings

- Recollections is split across many projects, so regressions may cross UI, API, and data boundaries.
- End-to-end checks should account for both the Blazor UI and the API surface.
- The Aspire AppHost is a likely anchor for broader validation when multiple services are involved.
- Gallery video replay reuse is implemented in `src/Recollections.Blazor.Components/wwwroot/Gallery.js` by caching the created `HTMLVideoElement` on the gallery model and reattaching it on reopen.
- Replay verification for this fix lives in `artifacts/playwright-video-repro.js`; it measures first-frame warm start and asserts that replay adds no new `/videos/{id}/original` requests in the same gallery session.

## PR #425 Review — Video Streaming (2026-04-04)

### Key Finding
PR #425 attempts to implement MediaSource-based video streaming but has a critical bug: **object URLs are revoked too early** (lines 352–353 in `site.js`). The MediaSource URL is immediately invalidated before the browser can attach it, making streaming impossible. This is not a timing race — it's synchronous destruction of the URL reference.

### Architecture Pattern
- `site.js` owns the streaming logic via `setStreamingVideoSource()` (uses MediaSource API)
- `setObjectUrlSource()` is the fallback for unsupported browsers or MIME types
- `Gallery.js` orchestrates video setup via `window.ImageSource.Set()` 
- C# (`ImageInterop.cs`, `GalleryInterop.cs`) marshals streams as `DotNetStreamReference`

### What Streaming Requires
1. Object URL must remain valid from `element.src = url` through `mediaSource.endOfStream()`
2. `sourceopen` listener must be attached before assignment (correct here)
3. Fallback path must still work when MediaSource init fails
4. URL cleanup should happen only after playback ends or on error

### Regression Vectors to Guard
- MIME type falls back to `"video/mp4"` if not provided; other formats silently fail
- Browser compatibility: MediaSource not available in all environments; fallback must work
- Gallery image playback path must remain unaffected
- URL leak if SourceBuffer event handlers throw unexpectedly

## Fix Verification (2026-04-07)

✅ Trinity updated `site.js` with proper URL lifetime management  
✅ Tank enabled response streaming in `Entries/Api.cs`  
✅ Both changes preserve fallback behavior  
✅ Node syntax and Blazor UI project builds passed

## Blob URL Failure Investigation (2026-04-07)

### Error Report
```
GET blob:http://localhost:33881/d4e69e1c-2891-4ed7-bd62-e9cc9774804c net::ERR_FILE_NOT_FOUND
```

### Root Cause
**Image gallery lazy-load path orphans blob URLs.** Gallery.js (lines 239–250) creates object URLs for image providers but never registers cleanup. When Gallery component re-renders (state change, params update), PhotoSwipe attempts to reuse cached blob URL that was never explicitly revoked, resulting in `ERR_FILE_NOT_FOUND`.

### Call Chain
1. `Gallery.js:239` – Image provider promise lazy-loads blob URL
2. `Gallery.js:245` – `URL.createObjectURL(blob)` stores on model
3. `Gallery.js:246` – URL cached on `model.src`, no cleanup registration
4. **Gallery re-renders** – MediaModel reloaded or component state changes
5. **PhotoSwipe reuses old cached URL** – Browser 404s (URL was never revoked or already garbage-collected)

### Critical Finding
**Trinity's site.js implementation is sound:**
- ✅ MediaSource URL lifetime properly managed (lines 425–446)
- ✅ Fallback blob path safe (lines 316–339 with cleanup via `registerMediaElementSource`)
- ✅ No premature revocation affecting video path

**Issue is NOT Trinity's responsibility** – it's Gallery.js image provider path:
- 🔴 No cleanup registration for image blob URLs
- 🔴 URLs cached on model object with no lifecycle tie to element
- 🔴 Re-renders trigger PhotoSwipe to reuse invalidated URLs

### Why This Breaks
1. Blob URLs created in JS are only valid while the browser tab is open
2. If not explicitly revoked, they can leak or become invalid if MediaSource/element that created them is garbage-collected
3. Gallery.js stores URLs on model without binding to element lifetime
4. When component re-renders, new streams are fetched but old URLs remain cached and stale

### Regression Vector
- User views gallery → image lazy-loads (blob URL cached)
- Gallery component state changes or re-renders
- PhotoSwipe attempts to load same index again
- Old blob URL is reused but no longer valid → `ERR_FILE_NOT_FOUND`

### No Product Code Changes Made
This is a testing/investigation task per Charter. Identified the failure, preserved Trinity's work (it is not at fault), and documented cleanup lifecycle mismatch for team decision.
