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

## Learnings

- Recollections is split across many projects, so regressions may cross UI, API, and data boundaries.
- End-to-end checks should account for both the Blazor UI and the API surface.
- The Aspire AppHost is a likely anchor for broader validation when multiple services are involved.

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
