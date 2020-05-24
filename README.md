# Recollections
Manage your recollections

## Docker
 - [API - https://hub.docker.com/r/neptuo/recollections-api](https://hub.docker.com/r/neptuo/recollections-api)

## Developer notes
Here are some tips.

### Add migration
Execute from command line in root git repository folder:
```
dotnet ef migrations add {name} --startup-project src\Recollections.Api --project {data_project} --context {context}
```

Example:
```
dotnet ef migrations add NewMigration --startup-project src\Recollections.Api --project src\Recollections.Entries.Data --context Neptuo.Recollections.Entries.DataContext
```