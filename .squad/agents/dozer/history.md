# Project Context

- **Project:** Recollections
- **Requested by:** Marek Fišera
- **Stack:** .NET 10, Aspire AppHost, ASP.NET Core API, Blazor UI, Docker
- **Created:** 2026-04-04

## Core Context

Dozer owns the platform path that keeps the recollections stack runnable and shippable.

## Recent Updates

- 📌 Team hired on 2026-04-04
- 📌 AppHost MessagePack vulnerability fixed on 2026-07-15

## Learnings

- Local development is centered on `src/Recollections.AppHost`.
- Docker publishing is part of the repo workflow, especially around the API project.
- Platform work should support both the API and the Blazor UI without introducing fragile local-only steps.
- File-based AppHost dependencies in this repo are rooted in `src/AppHost.cs`; there is no checked-in AppHost `.csproj`, so hosting dependency changes are managed through `#:sdk` / `#:package` directives in `src/AppHost.cs`.
- The MessagePack vulnerability was addressed by upgrading the AppHost SDK version in `src/AppHost.cs`, removing the need for temporary central MessagePack pins or `#:package` override directives.
- Current local hosting vulnerability analysis should start from `src/AppHost.cs` and re-verify the transitive chain from the pinned `Aspire.AppHost.Sdk` version instead of assuming historical package overrides are still present.
