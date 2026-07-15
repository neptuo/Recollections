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
- File-based AppHost dependencies in this repo are rooted in `src/AppHost.cs`; platform package overrides need `#:package` there because there is no checked-in AppHost `.csproj`.
- `Directory.Packages.props` central package management applies to the generated `AppHost.cs.csproj`, so a central `<PackageVersion Include="MessagePack" Version="2.5.301" />` can safely override the vulnerable transitive `MessagePack` from Aspire's `StreamJsonRpc` dependency.
- Current MessagePack vulnerability chain for local hosting is `src/AppHost.cs` -> `Aspire.Hosting.AppHost` -> `StreamJsonRpc` -> `MessagePack`.
- File-based AppHost `#:package` directives are the minimum fix for transitive hosting-scope vulnerabilities when central package management alone is insufficient.
