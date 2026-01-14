#:sdk Aspire.AppHost.Sdk@13.0.2
#:project .\Recollections.Api\Recollections.Api.csproj
#:project .\Recollections.Blazor.UI\Recollections.Blazor.UI.csproj

var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions 
{
    DashboardApplicationName = "Recollections AppHost",
    Args = args,
});

var apiService = builder
    .AddProject<Projects.Recollections_Api>("api");

// var isWatch = builder.Configuration.GetValue<string>("DOTNET_WATCH") == "1";
var isWatch = true;
if (isWatch)
{
    var uiProjectDirectory = Path.GetDirectoryName(new Projects.Recollections_Blazor_UI().ProjectPath)!;

    builder
        .AddExecutable("ui", "dotnet", uiProjectDirectory, ["watch", "--non-interactive", "--verbose"])
        .WithEnvironment(context =>
        {
            context.EnvironmentVariables["DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER"] = "1";
            context.EnvironmentVariables["DOTNET_WATCH_RESTART_ON_RUDE_EDIT"] = "1";
        })
        .WithHttpEndpoint(targetPort: 33881, isProxied: false)
        .WithReference(apiService);

    builder
        .AddExecutable("watch-scss", "dotnet", ".", ["watch", "--no-restore", "--non-interactive", "--verbose", "build", ".\\WatchScss.proj", $"--property:RootPath={uiProjectDirectory}"]);
}
else
{
    builder
        .AddProject<Projects.Recollections_Blazor_UI>("ui")
        .WithReference(apiService);
}

builder.Build().Run();
