// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
self.addEventListener('fetch', onFetch);
self.addEventListener('message', onMessage);

function onFetch(event) {
    const isRootPost = event.request.method === 'POST' && event.request.mode === 'navigate';
    if (isRootPost) {
        return event.respondWith((async () => {
            const formData = await event.request.formData();
            const files = formData.getAll("allfiles");
            await storeFiles(files, null, null, null, null);
            return Response.redirect('/', 303);
        })());
    }
}

function onMessage(event) {
    if (event.data.action === 'skipWaiting') {
        self.skipWaiting();
    }
}

// Sync with FileUpload.js
async function storeFiles(files, actionUrl, entityType, entityId, userId) {
    const mediaCache = await caches.open('media');
    
    const promises = Array.from(files).map(file => {
        return new Promise(async resolve => {
            let id = self.crypto.randomUUID();
            await mediaCache.put(id, new Response(file, { 
                headers: { 
                    'X-Entity-Type': entityType || '',
                    'X-Entity-Id': entityId || '',
                    'X-User-Id': userId || '',
                    'X-Action-Url': actionUrl || '',

                    'X-File-Name': file.name,
                    'X-Last-Modified': file.lastModified,
                    'Content-Size': file.size,
                    'Content-Type': file.type,
                }
            }));
            resolve({
                id: id,
                file: file,
                actionUrl: actionUrl,
                userId: userId,
                entityType: entityType,
                entityId: entityId,
            });
        });
    });

    return Promise.all(promises);
}