import * as FileUpload from './_content/Recollections.Blazor.Components/FileUpload.js';

// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
self.addEventListener('fetch', onFetch);
self.addEventListener('message', onMessage);

function onFetch(event) {
    const isRootPost = event.request.method === 'POST' && event.request.mode === 'navigate';
    if (isRootPost) {
        event.respondWith(FileUpload.handleShareTarget(event));
    }
}

function onMessage(event) {
    if (event.data.action === 'skipWaiting') {
        self.skipWaiting();
    }
}
