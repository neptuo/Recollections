# Project Context

- **Project:** Recollections
- **Requested by:** Marek Fišera
- **Stack:** .NET 10, Blazor WebAssembly/UI, ASP.NET Core API, EF Core, Aspire AppHost
- **Created:** 2026-04-04

## Core Context

Trinity owns the Blazor UI experience for the recollections product.

## Recent Updates

- 📌 Team hired on 2026-04-04
- 2026-04-08: Stacked video metadata vertically in VideoDetail.razor — size now renders below duration. Approved by Switch; build passes.

## Learnings

- The main UI lives in `src/Recollections.Blazor.UI`.
- Shared Blazor components also exist in `src/Recollections.Blazor.Components`.
- UI work should stay coordinated with the API and shared models rather than duplicating server logic.
- src\Recollections.Blazor.UI\Entries\Pages\VideoDetail.razor keeps video metadata in a small stacked block, so secondary facts like original size can move below duration without changing the inline link treatment.
- Metadata layout changes must preserve conditional rendering for duration-only and size-only cases.
