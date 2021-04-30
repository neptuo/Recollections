import PhotoSwipeLightbox from './photoswipe/photoswipe-lightbox.esm.js';

const options = {
    showHideAnimationType: 'none',
    pswpModule: './photoswipe.esm.js',
    pswpCSS: '/_content/Recollections.Blazor.Components/photoswipe/photoswipe.css'
};
const lightbox = new PhotoSwipeLightbox(options);

export function initialize(interop, count) {
    lightbox.on("numItems", (e) => {
        e.numItems = count
    });

    lightbox.on("itemData", (e) => {
        e.itemData = {
            provider: new Promise((resolve) => {
                interop.invokeMethodAsync("GetImageDataAsync", e.index).then(function (data) {
                    resolve(data);
                });
            }),
            //w: 200,
            //h: 160,
            alt: "Image...",
        }
    });
    lightbox.init();
}

export function open() {
    lightbox.loadAndOpen(0);
}
