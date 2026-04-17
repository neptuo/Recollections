window.Pwa = {
    Install: function () {
        if (window.PwaInstallPrompt) {
            window.PwaInstallPrompt.prompt();
            window.PwaInstallPrompt.userChoice.then(function () {
                window.PwaInstallPrompt = null;
            });
        }
    },
    Update: function () {
        navigator.serviceWorker.ready.then(function (registration) {
            var newWorker = null;
            if (registration.waiting != null) {
                newWorker = registration.waiting;
            }

            if (newWorker != null) {
                newWorker.postMessage({ action: 'skipWaiting' });
            }
        });
    },
    Version: async function () {
        try {
            const response = await fetch('service-worker-assets.js', { cache: 'no-store' });
            if (!response.ok) {
                return 'development';
            }

            const content = await response.text();
            const match = content.match(/"version":"([^"]+)"/);
            return match && match[1] ? match[1] : 'development';
        } catch {
            return 'development';
        }
    },
    installable: async () => {
        await Recollections.WaitForDotNet();
        DotNet.invokeMethodAsync('Recollections.Blazor.UI', 'Pwa.Installable');
    },
    updateable: async () => {
        await Recollections.WaitForDotNet();
        DotNet.invokeMethodAsync('Recollections.Blazor.UI', 'Pwa.Updateable');
    }
};

function activateWaitingServiceWorker(registration) {
    var waiting = registration.waiting;
    if (waiting != null) {
        waiting.postMessage({ action: 'skipWaiting' });
    }
}

window.addEventListener('beforeinstallprompt', function (e) {
    window.PwaInstallPrompt = e;
    Pwa.installable();
});

if ("serviceWorker" in navigator) {
    navigator.serviceWorker.register('service-worker.js').then(function (registration) {
        activateWaitingServiceWorker(registration);

        if (registration.waiting !== null) {
            if (navigator.serviceWorker.controller) {
                Pwa.updateable();
            }
        } else {
            registration.addEventListener("updatefound", function () {
                var installing = registration.installing;
                if (installing !== null) {
                    installing.addEventListener("statechange", function () {
                        switch (installing.state) {
                            case 'installed':
                                activateWaitingServiceWorker(registration);
                                if (navigator.serviceWorker.controller) {
                                    Pwa.updateable();
                                }

                                break;
                        }
                    });
                } else if (registration.waiting !== null) {
                    if (navigator.serviceWorker.controller) {
                        Pwa.updateable();
                    }
                }
            });
        }
    });

    var isRefreshing = false;
    navigator.serviceWorker.addEventListener('controllerchange', function () {
        if (isRefreshing) {
            return;
        }

        window.location.reload();
        isRefreshing = true;
    });
}
