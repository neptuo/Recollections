// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).

self.importScripts('./share-target.js');

self.addEventListener('fetch', onFetch);
self.addEventListener('message', onMessage);

function onFetch(event) {
    const response = shareTargetHandler(event);
    if (response) {
        return response;
    }
}

function onMessage(event) {
    if (event.data.action === 'skipWaiting') {
        self.skipWaiting();
    }
}