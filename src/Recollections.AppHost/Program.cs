var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder
    .AddProject<Projects.Recollections_Api>("api");

var uiService = builder
    .AddProject<Projects.Recollections_Blazor_UI>("ui")
    .WithReference(apiService);

builder.Build().Run();
