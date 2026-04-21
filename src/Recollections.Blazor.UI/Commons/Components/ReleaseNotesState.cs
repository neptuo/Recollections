using Microsoft.AspNetCore.Components;
using Neptuo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
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
    private List<ReleaseNotesEntry> cachedEntries;

    public ReleaseNotesState(HttpClient http, NavigationManager navigation)
    {
        Ensure.NotNull(http, "http");
        Ensure.NotNull(navigation, "navigation");
        this.http = http;
        this.navigation = navigation;
    }

    private async Task<List<ReleaseNotesEntry>> EnsureFetchedAsync()
    {
        if (cachedEntries != null)
            return cachedEntries;

        var url = new Uri(new Uri(navigation.BaseUri), "release-notes.json");
        cachedEntries = await http.GetFromJsonAsync<List<ReleaseNotesEntry>>(url);
        return cachedEntries;
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
}
