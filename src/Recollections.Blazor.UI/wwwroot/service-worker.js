// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
self.addEventListener('fetch', event => {
    if (event.request.method === "POST") {
        return handleShareTarget(event);
    }
});

self.addEventListener('message', onMessage);

function onMessage(event) {
    if (event.data.action === 'skipWaiting') {
        self.skipWaiting();
    }
}

async function handleShareTarget(e) {
    const formData = await e.request.formData();
    const files = formData.getAll("allfiles");
    const url = e.request.url;

    if (files && files.length > 0) {
        let fileObjects = [];
        for (let i = 0; i < files.length; i++) {
            fileObjects.push({
                entryId: null,
                name: files[i].name,
                buffer: await files[i].arrayBuffer()
            });
        }

        // TODO: Pass to UI thread or store in DB
        for (var i = 0; i < fileObjects.length; i++) {
            console.log(fileObjects[i].name);
        }
    }

    return Response.redirect(url, 302);
}