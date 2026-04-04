# Project Context

- **Project:** Recollections
- **Requested by:** Marek Fišera
- **Stack:** .NET 10, Aspire AppHost, ASP.NET Core API, Blazor UI, Docker
- **Created:** 2026-04-04

## Core Context

Dozer owns the platform path that keeps the recollections stack runnable and shippable.

## Recent Updates

- 📌 Team hired on 2026-04-04

## Learnings

- Local development is centered on `src/Recollections.AppHost`.
- Docker publishing is part of the repo workflow, especially around the API project.
- Platform work should support both the API and the Blazor UI without introducing fragile local-only steps.
