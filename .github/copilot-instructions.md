# Copilot Instructions

## Code Quality

- Always deduplicate logic by extracting to a service or shared method/class.

## Running the Application

- Restore .NET tools with `dotnet tool restore` before building. This installs the `excubo.webcompiler` tool needed to compile SCSS files.
- Run the application with `dotnet run ./src/AppHost.cs`.
- If the default ports are already in use, invoke the `run-custom-ports` skill to run the application on a different set of ports.

## Release Notes

- When asked to prepare release notes, invoke the `release-notes-writer` skill and update `src/Recollections.Blazor.UI/wwwroot/release-notes.html` from the matching GitHub milestone.

## Pull Request Reviews

- Always reply to review comments when applying feedback on a PR.
