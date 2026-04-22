# Recollections

Manage your recollections

## Docker

API images are published to [ghcr.io/neptuo/recollections-api](https://ghcr.io/neptuo/recollections-api)

## Developer notes

Here are some tips and tricks.

### Run dev mode

We have an Aspire app host.

- `dotnet tool restore`
- `dotnet watch build src/Recollections.Blazor.UI` to make Blazor recompile on change
- `dotnet run ./src/AppHost.cs` to run the app host

### Sample data and screenshots

To seed the local development databases and media with the sample users/stories used for screenshots:

```sh
dotnet run ./src/SampleDataSeeder.cs
```

Sample credentials:

- `jondoe / demo1234` (premium)
- `janedoe / demo1234`
- `billdoe / demo1234`

Drop optimized photos into `assets/sample-data/media/` and rerun the seeder to refresh the media used in the demo dataset. If the folder is empty, the seeder falls back to bundled repo images so the structure still comes up populated.

The PWA manifest screenshot assets live in `src/Recollections.Blazor.UI/wwwroot/img/screenshots/`; replace those files in place when you have final curated captures.

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

### Publish blazor project

- Update release notes
- Remove aspnetcore hotreload script

From the blazor UI project folder:

```sh
dotnet publish -c Release -p:RunAOTCompilation=true
```
