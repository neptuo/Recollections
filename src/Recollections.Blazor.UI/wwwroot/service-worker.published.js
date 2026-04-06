// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');

const shareTargetAsset = self.assetsManifest.assets.find(a => /^share-target(\.[a-z0-9]+)?\.js$/.test(a.url));
if (shareTargetAsset) {
    self.importScripts('./' + shareTargetAsset.url);
} else {
    function shareTargetHandler() { return null; }
}

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/];
const offlineAssetsExclude = [/^service-worker\.js$/, /^release-notes\.html$/];

self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));
self.addEventListener('push', event => event.waitUntil(onPush(event)));
self.addEventListener('notificationclick', event => event.waitUntil(onNotificationClick(event)));
self.addEventListener('message', onMessage);

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

async function onInstall(event) {
    console.info('Service worker: Install');

    // Fetch and cache all matching items from the assets manifest
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Delete unused caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    const response = shareTargetHandler(event);
    if (response) {
        return response;
    }

    let cachedResponse = null;
    if (event.request.method === 'GET') {
        // For all navigation requests, try to serve index.html from cache
        // If you need some URLs to be server-rendered, edit the following check to exclude those URLs
        const shouldServeIndexHtml = event.request.mode === 'navigate';

        const request = shouldServeIndexHtml ? 'index.html' : event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }

    return cachedResponse || fetch(event.request);
}
