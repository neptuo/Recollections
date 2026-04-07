# Project Context

- **Project:** Recollections
- **Requested by:** Marek Fišera
- **Stack:** .NET 10, ASP.NET Core API, EF Core, Blazor UI, Aspire AppHost
- **Created:** 2026-04-04

## Core Context

Tank owns the backend delivery path for recollections, accounts, and entry/media workflows.

## Recent Updates

- 📌 Team hired on 2026-04-04
- ✅ **2026-04-07:** Enabled browser response streaming in `Entries/Api.cs` for authenticated media fetches; wrapped stream lifecycle and hardened MIME fallback behavior
- ✅ **2026-04-07:** Coordinated with Trinity on cleanup ordering; streaming fix ready for merge

## Learnings

- The API project is `src/Recollections.Api`.
- Data and domain work is spread across `Recollections.*.Data`, `Recollections.Entries`, and related shared projects.
- Backend work should preserve clear separation between transport contracts and persistence details.
- Progressive media playback in the Blazor UI depends on `src/Recollections.Blazor.UI\Entries\Api.cs` enabling browser response streaming before handing streams to JS interop.
- The shared browser-side progressive video path lives in `src/Recollections.Blazor.UI\wwwroot\js\site.js` via `window.ImageSource.Set`, and gallery lightbox playback reuses it from `src/Recollections.Blazor.Components\wwwroot\Gallery.js`.
- For MediaSource-backed video playback, keep the object URL alive until the element has attached metadata and fall back to blob URLs only before stream consumption starts.
- When Marek asks for a focused PR fix, preserve unrelated local edits already present in the branch unless they are required for the targeted behavior.

## 2026-04-07 Progressive Streaming Fix

### Implementation
Updated `src/Recollections.Blazor.UI/Entries/Api.cs` to enable browser response streaming:
- Enabled streaming for authenticated media fetches via HttpResponseMessage
- Wrapped response stream lifetime to maintain resource safety
- Hardened MIME selection and fallback behavior (defaults to `"video/mp4"`)
- Preserved unrelated local edits per product focus directive

### Result
✅ Blazor UI project build passed  
✅ Stream lifecycle properly bounded  
✅ Coordinated with Trinity on client-side URL lifetime fix
