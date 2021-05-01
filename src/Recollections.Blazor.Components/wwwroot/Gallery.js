import PhotoSwipeLightbox from './photoswipe/photoswipe-lightbox.esm.js';

const options = {
    showHideAnimationType: 'none',
    pswpModule: './photoswipe.esm.js',
    pswpCSS: '/_content/Recollections.Blazor.Components/photoswipe/photoswipe.css'
};
const lightbox = new PhotoSwipeLightbox(options);

export function initialize(interop, images) {
    lightbox.on("numItems", (e) => {
        e.numItems = images.length
    });

    lightbox.on("itemData", (e) => {
        e.itemData = {
            provider: new Promise((resolve) => {
                interop.invokeMethodAsync("GetImageDataAsync", e.index).then(function (data) {
                    resolve(data);
                });
            }),
            w: images[e.index].width,
            h: images[e.index].height,
            alt: images[e.index].title,
        }
    });
    lightbox.init();
}

export function open(index) {
    lightbox.loadAndOpen(index);
}
