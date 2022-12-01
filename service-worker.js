// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
//TODO inform developer they need to check update on refresh in dev tools
self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/];
const offlineAssetsExclude = [/^service-worker\.js$/];

async function onInstall(event) {
    console.info('Service worker: Install');

    // Fetch and cache all matching items from the assets manifest
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, {integrity: asset.hash, cache: 'no-cache'}));
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
    // Except for file sharing target posts


    let cachedResponse = null;
    const url = new URL(event.request.url);
    if (event.request.method === 'GET') {

        if (url.pathname.startsWith("/files")) {
            // Get file name from query
            const cache = await caches.open("files");
            cachedResponse = await cache.match(event.request);
        } else {
            // For all navigation requests, try to serve index.html from cache
            // If you need some URLs to be server-rendered, edit the following check to exclude those URLs
            const shouldServeIndexHtml = event.request.mode === 'navigate';

            const request = shouldServeIndexHtml ? 'index.html' : event.request;
            const cache = await caches.open(cacheName);
            cachedResponse = await cache.match(request);
        }

    }

    if (event.request.method !== "POST" || url.pathname !== "/open") {
        return cachedResponse || fetch(event.request);
    }

    // Save shared file in cache and redirect to app to allow pick up of file in cache
    const formData = await event.request.formData();
    // Title is in example code used as the file name but we get the file with the name from the form data 
    const title = formData.get("title");
    const file = formData.get("digital pass");
    console.info("Service Worker: Received file share from share target", {file, title});
    const cache = await caches.open("files");
    // Can use the same as because the HTTP Verb is different (GET not POST)
    const path = `/files/${file.name}`;
    const request = new Request(path, {method: "GET"});
    // const response = new Response(file);
    const response = new Response(file, {status: 200, statusText: "OK"});
    await cache.put(request, response);

    return Response.redirect(path, 303);
}/* Manifest version: obHQ0yEM */
