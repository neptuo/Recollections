window['updateAvailable']
    .then(isAvailable => {
        if (isAvailable) {
            const blazorAssembly = 'Recollections.Blazor.UI';
            const blazorInstallMethod = 'Pwa.Updateable';
            DotNet.invokeMethodAsync(blazorAssembly, blazorInstallMethod);
        }
    });