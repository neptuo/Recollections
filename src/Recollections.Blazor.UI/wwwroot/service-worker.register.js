window.Pwa = {
    install: function () {
        if (window.PwaInstallPrompt) {
            window.PwaInstallPrompt.prompt();
            window.PwaInstallPrompt.userChoice.then(function () {
                window.PwaInstallPrompt = null;
            });
        }
    },
    installable: function () {
        DotNet.invokeMethodAsync('Recollections.Blazor.UI', 'Pwa.Installable').then(function () { }, function () { setTimeout(Pwa.installable, 1000); });
    },
    updateable: function () {
        DotNet.invokeMethodAsync('Recollections.Blazor.UI', 'Pwa.Updateable').then(function () { }, function () { setTimeout(Pwa.updateable, 1000); });
    }
};

window.addEventListener('beforeinstallprompt', function (e) {
    window.PwaInstallPrompt = e;
    Pwa.installable();
});

if ("serviceWorker" in navigator) {
    navigator.serviceWorker.register('service-worker.js').then(function (registration) {
        registration.addEventListener("updatefound", function () {
            var installing = registration.installing;
            if (installing !== null) {
                installing.addEventListener("statechange", function () {
                    switch (installing.state) {
                        case 'installed':
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
    });
}