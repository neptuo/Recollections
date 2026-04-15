// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).

self.importScripts('./share-target.js');

self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', onFetch);
self.addEventListener('message', onMessage);
self.addEventListener('push', event => event.waitUntil(onPush(event)));
self.addEventListener('notificationclick', event => event.waitUntil(onNotificationClick(event)));

function onInstall(event) {
    console.info('Service worker: Install');
    return self.skipWaiting();
}

function onActivate(event) {
    console.info('Service worker: Activate');
    return self.clients.claim();
}

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

async function onPush(event) {
    let data = {};
    try {
        data = event.data?.json() || {};
    } catch {
        data = {
            body: event.data?.text()
        };
    }

    console.info('Service worker: Push received', data);
    const title = data.title || 'Recollections';
    const options = {
        body: data.body || 'You have a new entry waiting in your timeline.',
        icon: '/img/icon-192x192.png',
        badge: '/img/icon-maskable-192x192.png',
        tag: data.tag || 'recollections-notification',
        renotify: true,
        data: {
            url: data.url || '/'
        }
    };

    try {
        await self.registration.showNotification(title, options);
    } catch (error) {
        console.error('Service worker: Failed to show notification.', error, {
            title: title,
            permission: typeof Notification === 'undefined' ? 'unsupported' : Notification.permission,
            options: options
        });

        await self.registration.showNotification(title, {
            body: options.body,
            tag: options.tag,
            renotify: true,
            data: options.data
        });
    }
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
