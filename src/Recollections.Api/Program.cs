using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Neptuo;
using Neptuo.Events;
using Neptuo.Recollections;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;

var builder = WebApplication.CreateBuilder(args);

// Path resolver function
string ResolvePath(string relativePath)
{
    Ensure.NotNull(relativePath, "relativePath");
    return relativePath.Replace("{BasePath}", builder.Environment.ContentRootPath);
}

// Initialize startup classes
var accountsStartup = new AccountsStartup(builder.Configuration.GetSection("Accounts"), ResolvePath);
var entriesStartup = new EntriesStartup(builder.Configuration.GetSection("Entries"), ResolvePath);
var sharingStartup = new SharingStartup();

// Configure services
builder.Services.AddSingleton<PathResolver>(ResolvePath);

builder.Services
    .AddRouting(options => options.LowercaseUrls = true)
    .AddControllers()
    .AddNewtonsoftJson();

var yarp = builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

DefaultEventManager eventManager = new DefaultEventManager();
builder.Services
    .AddSingleton<IEventDispatcher>(eventManager)
    .AddSingleton<IEventHandlerCollection>(eventManager);

builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection("Cors"));

accountsStartup.ConfigureServices(builder.Services, builder.Environment);
entriesStartup.ConfigureServices(builder.Services, builder.Environment, yarp);
sharingStartup.ConfigureServices(builder.Services);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseStatusCodePages();

app.UseRouting();

// CORS configuration
var corsOptions = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value;
app.UseCors(p =>
{
    p.WithOrigins(corsOptions.Origins);
    p.AllowAnyMethod();
    p.AllowCredentials();
    p.AllowAnyHeader();
    p.SetPreflightMaxAge(TimeSpan.FromMinutes(10));
});

accountsStartup.ConfigureAuthentication(app, app.Environment);
entriesStartup.Configure(app.Services);

app.MapHealthChecks("/health");
app.MapReverseProxy().RequireAuthorization();
app.MapControllers();

app.Run();
