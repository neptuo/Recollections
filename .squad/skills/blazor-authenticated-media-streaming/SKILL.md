# Blazor authenticated media streaming

## When to use

Use this pattern when a Blazor WebAssembly client must stream authenticated media into browser APIs such as `MediaSource` without waiting for the whole response body.

## Pattern

1. In the Blazor UI API wrapper, create an `HttpRequestMessage` for the media URL.
2. Call `SetBrowserResponseStreamingEnabled(true)` before `SendAsync(..., HttpCompletionOption.ResponseHeadersRead)`.
3. Return a `Stream` wrapper that disposes the `HttpRequestMessage` and `HttpResponseMessage` when the consumer disposes the stream.
4. In JS, choose the MediaSource path only if `stream.stream()` exists and a supported MIME can be selected before reading bytes.
5. Revoke MediaSource object URLs only after the media element has attached to the source; otherwise use the blob/object-URL fallback.

## Repo example

- `src/Recollections.Blazor.UI\Entries\Api.cs`
- `src/Recollections.Blazor.UI\wwwroot\js\site.js`
- `src/Recollections.Blazor.Components\wwwroot\Gallery.js`
