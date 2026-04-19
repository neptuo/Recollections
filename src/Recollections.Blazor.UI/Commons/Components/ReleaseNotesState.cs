using Neptuo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Commons.Components;

public record ReleaseNotesEntry(string Version, string Html);

public class ReleaseNotesState
{
    private readonly HttpClient http;
    private Task<List<ReleaseNotesEntry>> fetchTask;

    public ReleaseNotesState(HttpClient http)
    {
        Ensure.NotNull(http, "http");
        this.http = http;
    }

    private Task<List<ReleaseNotesEntry>> EnsureFetchedAsync()
    {
        if (fetchTask == null)
            fetchTask = http.GetFromJsonAsync<List<ReleaseNotesEntry>>("/release-notes.json");

        return fetchTask;
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
