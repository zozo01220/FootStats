// Service worker minimal pour rendre FootStats installable (PWA).
// L'app est du Blazor Server : elle a besoin d'une connexion SignalR live,
// donc pas de vrai mode hors-ligne. On se contente de mettre en cache les
// assets statiques pour accélérer les chargements, et de laisser passer
// le reste (pages, SignalR) directement au réseau.

const CACHE_NAME = 'footstats-static-v1';
const STATIC_ASSETS = [
    'icons/icon-192.png',
    'icons/icon-512.png',
    'icons/icon-maskable-192.png',
    'icons/icon-maskable-512.png',
    'favicon.png'
];

self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME).then((cache) => cache.addAll(STATIC_ASSETS))
    );
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches.keys().then((keys) =>
            Promise.all(keys.filter((key) => key !== CACHE_NAME).map((key) => caches.delete(key)))
        )
    );
    self.clients.claim();
});

self.addEventListener('fetch', (event) => {
    const url = new URL(event.request.url);
    const isStaticAsset = STATIC_ASSETS.some((asset) => url.pathname.endsWith(asset));

    if (event.request.method !== 'GET' || !isStaticAsset) {
        // Pages, blazor.web.js, SignalR (_blazor) : toujours réseau direct.
        return;
    }

    event.respondWith(
        caches.match(event.request).then((cached) => cached || fetch(event.request))
    );
});
