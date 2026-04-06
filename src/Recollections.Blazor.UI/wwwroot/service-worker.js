// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).

self.importScripts('./share-target.js');

self.addEventListener('fetch', onFetch);
self.addEventListener('message', onMessage);
self.addEventListener('push', event => event.waitUntil(onPush(event)));
self.addEventListener('notificationclick', event => event.waitUntil(onNotificationClick(event)));

function onFetch(event) {
    const response = shareTargetHandler(event);
    if (response) {
        event.respondWith(response);
        return;
    }
}

function onMessage(event) {
    if (event.data.action === 'skipWaiting') {
        self.skipWaiting();
    }
}

function onPush(event) {
    let data = {};
    try {
        data = event.data?.json() || {};
    } catch {
        data = {
            body: event.data?.text()
        };
    }

    return self.registration.showNotification(data.title || 'Recollections', {
        body: data.body || 'You have an update waiting in Recollections.',
        icon: '/img/icon-192x192.png',
        badge: '/img/icon-maskable-192x192.png',
        tag: data.tag || 'recollections-notification',
        data: {
            url: data.url || '/'
        }
    });
}

async function onNotificationClick(event) {
    event.notification.close();

    const targetUrl = event.notification.data?.url || '/';
    const windowClients = await clients.matchAll({
        type: 'window',
        includeUncontrolled: true
    });

    for (const client of windowClients) {
        if ('focus' in client) {
            await client.focus();
            if ('navigate' in client) {
                await client.navigate(targetUrl);
            }
            return;
        }
    }

    if (clients.openWindow) {
        await clients.openWindow(targetUrl);
    }
}
