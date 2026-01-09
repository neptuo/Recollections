function shareTargetHandler(event) {
    const isRootPost = event.request.method === 'POST' && event.request.mode === 'navigate';
    if (isRootPost) {
        return event.respondWith((async () => {
            const formData = await event.request.formData();
            const files = formData.getAll("allfiles");
            await storeFiles(files, null, null, null, null);

            const allClients = await clients.matchAll();
            if (allClients && allClients.length > 0) {
                return Response.redirect(allClients[0].url, 303);
            } else {
                return Response.redirect('/', 303);
            }
        })());
    }

    return null;
}

// Sync with FileUpload.js (1:1)
async function storeFiles(files, actionUrl, entityType, entityId, userId) {
    const mediaCache = await caches.open('media');
    
    const promises = Array.from(files).map(file => {
        return new Promise(async resolve => {
            let id = self.crypto.randomUUID();
            const request = new Request(id);
            await mediaCache.put(request, new Response(file, { 
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
                id: request.url,
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