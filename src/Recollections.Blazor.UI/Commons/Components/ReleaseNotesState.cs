using Microsoft.AspNetCore.Components;
using Neptuo;
using Neptuo.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Commons.Components;

public record ReleaseNotesEntry(
    string Version,
    int Milestone,
    List<string> BreakingChanges,
    List<string> NewFeatures,
    List<string> BugFixes
);

public class ReleaseNotesState
{
    private readonly HttpClient http;
    private readonly NavigationManager navigation;
    private readonly ILog<ReleaseNotesState> log;
    private List<ReleaseNotesEntry> cachedEntries;

    public ReleaseNotesState(HttpClient http, NavigationManager navigation, ILog<ReleaseNotesState> log)
    {
        Ensure.NotNull(http, "http");
        Ensure.NotNull(navigation, "navigation");
        Ensure.NotNull(log, "log");
        this.http = http;
        this.navigation = navigation;
        this.log = log;
    }

    private async Task<List<ReleaseNotesEntry>> EnsureFetchedAsync()
    {
        if (cachedEntries != null)
            return cachedEntries;

        try
        {
            var url = new Uri(new Uri(navigation.BaseUri), "release-notes.json");
            cachedEntries = await http.GetFromJsonAsync<List<ReleaseNotesEntry>>(url);
            return cachedEntries;
        }
        catch (HttpRequestException e)
        {
            log.Debug($"Failed to fetch release-notes.json: {e.Message}");
            return [];
        }
        catch (JsonException e)
        {
            log.Debug($"Failed to parse release-notes.json: {e.Message}");
            return [];
        }
    }

    public async Task<List<ReleaseNotesEntry>> GetAllAsync()
        => await EnsureFetchedAsync();

    public async Task<List<ReleaseNotesEntry>> GetSinceAsync(string sinceVersion)
    {
        var all = await EnsureFetchedAsync();

        if (String.IsNullOrWhiteSpace(sinceVersion))
            return all;

        if (!Version.TryParse(sinceVersion, out Version since))
            return all;

        return all
            .Where(e => Version.TryParse(e.Version, out Version v) && v > since)
            .ToList();
    }

    public async Task<string> GetLatestVersionAsync()
    {
        var all = await EnsureFetchedAsync();
        return all
            ?.Select(e => Version.TryParse(e.Version, out var v) ? v : null)
            .Where(v => v != null)
            .OrderByDescending(v => v)
            .FirstOrDefault()
            ?.ToString();
    }
}
