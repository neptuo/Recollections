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

## Database Migrations

EF Core migrations run against both **SQLite** (local dev) and **Azure SQL Server** (prod). Migrations are typically scaffolded via the SQLite provider, so you MUST manually review and adjust them so they work correctly on SQL Server as well. See issues #494 and #498 for past breakage.

When adding or reviewing a migration, verify ALL of the following:

- **Inherit from `MigrationWithSchema<DataContext>`**, never from `Migration` directly. Every `CreateTable`, `DropTable`, `CreateIndex`, `AddColumn`, `AddForeignKey`, etc. must pass `schema: Schema.Name`. Check existing migrations under `src/Recollections.*.Data/Migrations/` for reference.
- **Integer identity (`Id`) columns** must carry `.Annotation("SqlServer:Identity", "1, 1")` in addition to the scaffolded `Sqlite:Autoincrement` annotation. Without it, SQL Server creates the column as plain `int NOT NULL` and inserts fail with "Cannot insert the value NULL into column 'Id'".
- **String foreign key columns** must declare a `maxLength` that matches the referenced column (e.g. `maxLength: 36` for FKs to `AspNetUsers.Id`). A mismatched or missing length causes SQL Server to reject the FK due to incompatible types (`nvarchar(max)` vs `nvarchar(36)`).
- **Indexed string columns** must declare a `maxLength`. SQL Server cannot build an index key over `nvarchar(max)`.
- **No hardcoded SQLite-specific column types** (e.g. `type: "TEXT"`, `type: "INTEGER"`). Let EF map the CLR type so each provider picks the correct native type.
- **Provider-specific fix-up migrations** (e.g. repairing past mistakes on SQL Server only) must guard with `migrationBuilder.ActiveProvider` and clearly document in an XML comment why the migration exists and why `Down` is a no-op (if applicable).

Before committing a migration, re-read the generated `*.cs` file top-to-bottom with this checklist in mind — do not rely on the scaffolder's output alone.
