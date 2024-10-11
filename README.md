# Recollections

Manage your recollections

## Docker

Currently published to private repository.

## Developer notes

Here are some tips and tricks.

### Run dev mode

We have Aspire app host.

- `dotnet watch build src/Recollections.Blazor.UI` to make blazor recompile on change,
- `dotnet run --project src/Recollections.AppHost` to run apire app host

### VS code tasks

Ctrl+Shift+B

- Compile SCSS

### Add migration

Execute from command line in root git repository folder:

```sh
dotnet ef migrations add {name} --startup-project src\Recollections.Api --project {data_project} --context {context}
```

Example:

```sh
dotnet ef migrations add NewMigration --startup-project src\Recollections.Api --project src\Recollections.Entries.Data --context Neptuo.Recollections.Entries.DataContext
```

### Build docker images

From the API project folder:

```sh
dotnet publish --os linux --arch x64 /t:PublishContainer
```
