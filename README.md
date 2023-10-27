# Recollections
Manage your recollections

## Docker
 - [API - https://hub.docker.com/r/neptuo/recollections-api](https://hub.docker.com/r/neptuo/recollections-api)

## Developer notes
Here are some tips and tricks.

### Run in dev mode
We typically run the app(s) from command line and so `dev.ps1` opens Windows Terminal with
- API started
- Blazor with `dotnet watch build`
- Blazor with `dotnet run --no-build`

### Add migration
Execute from command line in root git repository folder:
```
dotnet ef migrations add {name} --startup-project src\Recollections.Api --project {data_project} --context {context}
```

Example:
```
dotnet ef migrations add NewMigration --startup-project src\Recollections.Api --project src\Recollections.Entries.Data --context Neptuo.Recollections.Entries.DataContext
```

### Build docker images
From repository root:
```
docker build . -f .\docker\Dockerfile.api-bullseye-slim -t neptuo/recollections-api:{version}-bullseye-slim
docker build . -f .\docker\Dockerfile.api-bullseye-slim-arm32v7 -t neptuo/recollections-api:{version}-bullseye-slim-arm32v7

docker push neptuo/recollections-api:{version}-bullseye-slim
docker push neptuo/recollections-api:{version}-bullseye-slim-arm32v7
```
