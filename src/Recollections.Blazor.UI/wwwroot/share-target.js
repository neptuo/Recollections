function shareTargetHandler(event) {
    const isRootPost = event.request.method === 'POST' && event.request.mode === 'navigate';
    if (isRootPost) {
        return (async () => {
            const formData = await event.request.formData();
            const files = formData.getAll("allfiles");
            await storeFiles(files, null, null, null, null);

            const existingClient = await findExistingClient(event);
            if (existingClient) {
                return Response.redirect(existingClient.url, 303);
            } else {
                return Response.redirect('/', 303);
            }
        })();
    }

    return null;
}

// Picks the window the user is currently looking at so the share target
// handler can redirect back to it instead of always landing on the timeline.
// The client that is being navigated for this POST is excluded because its
// URL already points at the share target action ("/").
async function findExistingClient(event) {
    const excludeId = event.resultingClientId;
    const allClients = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
    const candidates = allClients.filter(c => c.id && c.id !== excludeId);
    return candidates.find(c => c.focused)
        || candidates.find(c => c.visibilityState === 'visible')
        || candidates[0]
        || null;
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