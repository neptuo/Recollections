# Media Object URL Lifetime

## When to use
When browser interop assigns a `blob:` URL or `MediaSource` URL to an `<img>` or `<video>` element.

## Rule
Treat the object URL as part of the element's active source state. Revoke it only when the source is being replaced or reset (`emptied`, explicit release, element teardown) or on terminal failure — not when initial media data arrives. When wiring `emptied` cleanup for a freshly created URL, register that cleanup after `element.src = url` so the source swap itself cannot revoke the new URL.

## Why
`loadeddata` only means the browser has enough data to render the first frame. Video playback, replay, and seek can still re-read from the same `blob:` URL afterward. `MediaSource.sourceended` only means appending is complete; the element may still be actively playing the attached source.

## Recollections example
- `src/Recollections.Blazor.UI/wwwroot/js/site.js`
- `src/Recollections.Blazor.Components/wwwroot/Gallery.js`

## Checklist
1. Release any previous source before assigning a new object URL.
2. Assign the new `src` before arming `emptied` cleanup for that same URL.
3. Keep blob-backed video URLs alive through playback.
4. Keep MediaSource URLs alive after `endOfStream()` until the element detaches or errors.
5. Mirror the same rule in fallback paths so unsupported streaming browsers do not regress.
