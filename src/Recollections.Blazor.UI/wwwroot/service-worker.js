import * as FileUpload from './_content/Recollections.Blazor.Components/FileUpload.js';

// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
self.addEventListener('fetch', async event => {
    if (event.request.method === "POST") {
        // TODO: Trigger it only for share target URL (= /)
        return FileUpload.handleShareTarget(event);
    }
});

self.addEventListener('message', onMessage);

function onMessage(event) {
    if (event.data.action === 'skipWaiting') {
        self.skipWaiting();
    }
}
