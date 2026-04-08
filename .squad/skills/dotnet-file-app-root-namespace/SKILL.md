---
name: "dotnet-file-app-root-namespace"
description: "How to keep file-based .NET apps resolving shared repo types without changing behavior"
domain: "backend"
confidence: "high"
source: "earned"
---

## Context
Use this when a repo mixes normal C# projects with file-based `.cs` apps launched via `dotnet run path\to\script.cs`.

## Patterns
- Treat file-based apps as compiling outside any project namespace wrapper from older `Program.cs` files.
- If the script references types declared in the repo root namespace, import that namespace explicitly.
- Prefer a single `using` fix over copying shared infrastructure code into the script.

## Examples
- `src\VideoSizeBackfill.cs` needed `using Neptuo.Recollections;` so `SchemaOptions<T>` from `src\Recollections.Data.Ef\SchemaOptions.cs` would resolve.

## Anti-Patterns
- Duplicating shared EF/schema types inside the script just to satisfy compilation.
- Refactoring working runtime behavior when the only defect is namespace resolution in the file-based app.
