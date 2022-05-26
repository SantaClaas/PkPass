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

async function onInstall(event) {
    console.info('Service worker: Install');}

async function onActivate(event) {
    console.info('Service worker: Activate');
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
    // Can use the same as this handler because the HTTP Verb is different (GET not POST)
    const path = `/files/${file.name}`;
    const request = new Request(path, {method: "GET"});
    const response = new Response(file, {status: 200, statusText: "OK"});
    await cache.put(request, response);

    return Response.redirect(`/open/${file.name}`, 303);
}