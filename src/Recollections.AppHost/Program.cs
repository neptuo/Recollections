using Microsoft.Extensions.Configuration;
using System.Diagnostics;

var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder
    .AddProject<Projects.Recollections_Api>("api");

if (builder.Configuration.GetValue<string>("DOTNET_WATCH") == "1")
{
    var ui = builder
        .AddExecutable("ui", "dotnet", Path.GetDirectoryName(new Projects.Recollections_Blazor_UI().ProjectPath)!, ["watch", "--non-interactive"])
        .WithHttpEndpoint(targetPort: 33881, isProxied: false)
        .WithReference(apiService);
}
else
{
    var ui = builder
        .AddProject<Projects.Recollections_Blazor_UI>("ui")
        .WithReference(apiService);
}

builder.Build().Run();
