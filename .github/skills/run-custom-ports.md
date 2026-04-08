# Run on Custom Ports

Use this skill when the default ports (33880 for API, 33881 for Blazor UI, 33882/33883 for Aspire dashboard) are already in use and you need to run the application on a different set of ports.

## Default Ports

| Component        | Default Port |
|------------------|-------------|
| API              | 33880       |
| Blazor UI        | 33881       |
| Aspire Dashboard | 33882 (http), 33883 (https) |
| OTLP Endpoint    | 19035 (http), 21149 (https) |
| Resource Service | 20053 (http), 22221 (https) |

## Port Selection

Pick a new base port (e.g., 34880) and derive the main ports from it. The OTLP and Resource Service ports don't follow a formula — just pick any free ports.

> **Note:** These formulas are only for choosing a new alternate port set. They intentionally don't reproduce the default ports (e.g., the default OTLP port 19035 is unrelated to 33880).

| Component        | Formula              | Example (base=34880) |
|------------------|---------------------|----------------------|
| API              | base                 | 34880                |
| Blazor UI        | base + 1             | 34881                |
| Aspire Dashboard | base + 2 / base + 3  | 34882 / 34883        |
| OTLP Endpoint    | any free port pair   | 35035 / 35149        |
| Resource Service | any free port pair   | 35053 / 35221        |

## Files to Update

All paths are relative to the repository root.

### 1. API launch settings — `src/Recollections.Api/Properties/launchSettings.json`

Change `applicationUrl` to use the new API port:

```json
"applicationUrl": "http://localhost:{API_PORT}/"
```

### 2. Blazor UI launch settings — `src/Recollections.Blazor.UI/Properties/launchSettings.json`

Change `applicationUrl` to use the new Blazor UI port:

```json
"applicationUrl": "http://localhost:{UI_PORT}/"
```

### 3. AppHost — `src/AppHost.cs`

Update the `targetPort` in `.WithHttpEndpoint()` to match the new Blazor UI port:

```csharp
.WithHttpEndpoint(targetPort: {UI_PORT}, isProxied: false)
```

### 4. CORS configuration — `src/Recollections.Api/appsettings.json`

Update the localhost origin in `Cors.Origins` to match the new Blazor UI port:

```json
"Origins": [ "http://localhost:{UI_PORT}", "https://app.recollections.neptuo.com", "https://recollections.app", "https://www.recollections.app" ]
```

### 5. API base address — `src/Recollections.Blazor.UI/Recollections.Blazor.UI.csproj`

Update the debug `ApiBaseAddress` to match the new API port:

```xml
<ApiBaseAddress Condition="'$(Configuration)' == 'Debug'">http://localhost:{API_PORT}/</ApiBaseAddress>
```

### 6. Aspire dashboard — `src/AppHost.run.json`

Update all port numbers in both `http` and `https` profiles:

- `applicationUrl` — use `{DASHBOARD_HTTP_PORT}` and `{DASHBOARD_HTTPS_PORT}`
- `DOTNET_DASHBOARD_OTLP_ENDPOINT_URL` — use `{OTLP_HTTP_PORT}` and `{OTLP_HTTPS_PORT}`
- `DOTNET_RESOURCE_SERVICE_ENDPOINT_URL` — use `{RESOURCE_HTTP_PORT}` and `{RESOURCE_HTTPS_PORT}`

## Verification

After updating all files, run the application:

```bash
dotnet tool restore
dotnet run ./src/AppHost.cs
```

Verify that:
1. The Aspire dashboard opens on the new dashboard port.
2. The Blazor UI is accessible on the new UI port.
3. The Blazor UI can communicate with the API (login, load entries) — this confirms CORS and API base address are correct.
