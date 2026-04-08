# Gallery video reuse

## When to use

Use this when a JS-driven gallery or lightbox replaces preview content with a playable `<video>` and users expect reopening the same item to replay without a fresh download.

## Pattern

1. Create the playable `HTMLVideoElement` only on the first activation.
2. Store that element on the gallery item/view-model in JS.
3. On reopen, reattach the cached video element instead of creating a new `<video>` and reassigning `src`.
4. Pause cached videos when switching items or closing the gallery so hidden playback does not continue.
5. Keep the original native URL source path unchanged for the initial play; the cache is in the element instance, not a rewritten transport.

## Why

Many gallery libraries rebuild slide DOM when the overlay closes or the active slide changes. If the app recreates the `<video>` each time, the browser treats it like a new playback session and may hit the network again even for the same authenticated URL. Reusing the same element preserves buffered data and keeps replay feeling instant.

## Repo example

- `src/Recollections.Blazor.Components/wwwroot/Gallery.js`

## Verification

1. Measure first play from poster click to `readyState >= HAVE_CURRENT_DATA` and `currentTime > 0.2`.
2. Let the video finish, close the gallery, reopen the same item, and click play again.
3. Assert replay stays near the beginning (`currentTime < 1`) and does not add new original-video network requests.
4. Ignore benign `net::ERR_ABORTED` cancellations caused by gallery teardown, but fail on blob URL errors or new `/original` fetches.
5. In this repo, use `artifacts/playwright-video-repro.js` as the regression harness.
