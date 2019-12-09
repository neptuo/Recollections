const baseURL = '/';
const indexURL = '/index.html';
const networkFetchEvent = 'fetch';
const swInstallEvent = 'install';
const swInstalledEvent = 'installed';
const swActivateEvent = 'activate';
const staticCachePrefix = 'blazor-cache-v';
const staticCacheName = 'blazor-cache-v0.0.637115268534426753';
const requiredFiles = [
"/_framework/blazor.boot.json",
"/_framework/blazor.webassembly.js",
"/_framework/wasm/mono.js",
"/_framework/wasm/mono.wasm",
"/_framework/_bin/CommonMark.dll",
"/_framework/_bin/Microsoft.AspNetCore.Authorization.dll",
"/_framework/_bin/Microsoft.AspNetCore.Blazor.dll",
"/_framework/_bin/Microsoft.AspNetCore.Blazor.HttpClient.dll",
"/_framework/_bin/Microsoft.AspNetCore.Components.dll",
"/_framework/_bin/Microsoft.AspNetCore.Components.Forms.dll",
"/_framework/_bin/Microsoft.AspNetCore.Components.Web.dll",
"/_framework/_bin/Microsoft.AspNetCore.Metadata.dll",
"/_framework/_bin/Microsoft.Bcl.AsyncInterfaces.dll",
"/_framework/_bin/Microsoft.Extensions.DependencyInjection.Abstractions.dll",
"/_framework/_bin/Microsoft.Extensions.DependencyInjection.dll",
"/_framework/_bin/Microsoft.Extensions.Logging.Abstractions.dll",
"/_framework/_bin/Microsoft.Extensions.Options.dll",
"/_framework/_bin/Microsoft.Extensions.Primitives.dll",
"/_framework/_bin/Microsoft.JSInterop.dll",
"/_framework/_bin/Mono.WebAssembly.Interop.dll",
"/_framework/_bin/mscorlib.dll",
"/_framework/_bin/Neptuo.dll",
"/_framework/_bin/Neptuo.Events.dll",
"/_framework/_bin/Neptuo.Exceptions.dll",
"/_framework/_bin/Neptuo.pdb",
"/_framework/_bin/Recollections.Api.Shared.dll",
"/_framework/_bin/Recollections.Api.Shared.pdb",
"/_framework/_bin/Recollections.Blazor.Components.dll",
"/_framework/_bin/Recollections.Blazor.Components.pdb",
"/_framework/_bin/Recollections.Blazor.UI.dll",
"/_framework/_bin/Recollections.Blazor.UI.pdb",
"/_framework/_bin/System.ComponentModel.DataAnnotations.dll",
"/_framework/_bin/System.Core.dll",
"/_framework/_bin/System.dll",
"/_framework/_bin/System.Net.Http.dll",
"/_framework/_bin/System.Runtime.CompilerServices.Unsafe.dll",
"/_framework/_bin/System.Text.Encodings.Web.dll",
"/_framework/_bin/System.Text.Json.dll",
"/_framework/_bin/WebAssembly.Bindings.dll",
"/_framework/_bin/WebAssembly.Net.Http.dll",
"/css/bootstrap-datepicker/bootstrap-datepicker.min.css",
"/css/bootstrap/bootstrap.min.css",
"/css/bootstrap/bootstrap.min.css.map",
"/css/easymde/easymde.min.css",
"/css/open-iconic/css/open-iconic-bootstrap.css",
"/css/open-iconic/css/open-iconic-bootstrap.min.css",
"/css/open-iconic/fonts/open-iconic.eot",
"/css/open-iconic/fonts/open-iconic.otf",
"/css/open-iconic/fonts/open-iconic.svg",
"/css/open-iconic/fonts/open-iconic.ttf",
"/css/open-iconic/fonts/open-iconic.woff",
"/css/simplemde/simplemde.min.css",
"/css/site.css",
"/css/site.min.css",
"/img/icon-192x192.png",
"/img/icon-512x512.png",
"/img/logo.png",
"/img/thumbnail-placeholder.png",
"/index.html",
"/js/bootstrap-datepicker/bootstrap-datepicker.en-GB.min.js",
"/js/bootstrap-datepicker/bootstrap-datepicker.min.js",
"/js/bootstrap/bootstrap.min.js",
"/js/easymde/easymde.min.js",
"/js/jquery/jquery.min.js",
"/js/popper/popper.min.js",
"/js/simplemde/simplemde.min.js",
"/js/site.js",
"/service-worker-update.js",
"/service-worker-register.js",
"/manifest.json"
];
// * listen for the install event and pre-cache anything in filesToCache * //
self.addEventListener(swInstallEvent, event => {
    self.skipWaiting();
    event.waitUntil(
        caches.open(staticCacheName)
            .then(cache => {
                return cache.addAll(requiredFiles);
            })
    );
});
self.addEventListener(swActivateEvent, function (event) {
    event.waitUntil(
        caches.keys().then(function (cacheNames) {
            return Promise.all(
                cacheNames.map(function (cacheName) {
                    if (staticCacheName !== cacheName && cacheName.startsWith(staticCachePrefix)) {
                        return caches.delete(cacheName);
                    }
                })
            );
        })
    );
});
self.addEventListener(networkFetchEvent, event => {
    const requestUrl = new URL(event.request.url);
    if (requestUrl.origin === location.origin) {
        if (requestUrl.pathname === baseURL) {
            event.respondWith(caches.match(indexURL));
            return;
        }
    }
    event.respondWith(
        caches.match(event.request)
            .then(response => {
                if (response) {
                    return response;
                }
                return fetch(event.request)
                    .then(response => {
                        if (response.ok) {
                            if (requestUrl.origin === location.origin) {
                                caches.open(staticCacheName).then(cache => {
                                    cache.put(event.request.url, response);
                                });
                            }
                        }
                        return response.clone();
                    });
            }).catch(error => {
                console.error(error);
            })
    );
});
