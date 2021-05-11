import PhotoSwipeLightbox from './photoswipe/photoswipe-lightbox.esm.js';

const options = {
    showHideAnimationType: 'none',
    pswpModule: './photoswipe.esm.js',
    pswpCSS: '/_content/Recollections.Blazor.Components/photoswipe/photoswipe.css'
};
const lightbox = new PhotoSwipeLightbox(options);
let autoPlayTimer = null;
let stopCallback = () => { };

const playDurationSeconds = 4;
const playIcon = '<i class="fas fa-play"></i>';
const pauseIcon = '<i class="fas fa-pause"></i>';

function next(el) {
    if (lightbox.pswp.currIndex < lightbox.pswp.numItems - 1) {
        lightbox.pswp.next();
    } else {
        stop(el);
    }
}

function play(el) {
    autoPlayTimer = setInterval(() => next(el), playDurationSeconds * 1000);
    el.innerHTML = pauseIcon;
    lightbox.pswp.next();
}

function stop(el) {
    clearInterval(autoPlayTimer);
    autoPlayTimer = null;
    el.innerHTML = playIcon;

    stopCallback();
}

export function initialize(interop, images) {
    lightbox.on('uiRegister', function () {
        lightbox.pswp.ui.registerElement({
            name: 'autoplay',
            order: 9,
            isButton: true,
            html: playIcon,
            onClick: (event, el) => {
                lightbox.pswp.on('close', () => {
                    stop(el);
                });

                if (autoPlayTimer == null) {
                    play(el);
                } else {
                    stop(el);
                }
            }
        });

        lightbox.pswp.ui.registerElement({
            name: 'title',
            order: 9,
            isButton: false,
            appendTo: 'root',
            html: 'Caption text',
            onInit: (el, pswp) => {
                lightbox.pswp.on('change', () => {
                    el.innerHTML = lightbox.pswp.currSlide.data.alt || '';
                });
            }
        });

        lightbox.pswp.ui.registerElement({
            name: 'autoplay-progress',
            order: 9,
            isButton: false,
            appendTo: 'root',
            html: '',
            onInit: (el, pswp) => {
                stopCallback = () => {
                    el.style.display = "none";
                }

                lightbox.pswp.on('change', () => {
                    if (autoPlayTimer != null) {
                        el.style.display = "block";
                        el.style.transition = "";
                        el.style.width = "0%";
                        el.offsetHeight;
                        el.style.transition = "width " + playDurationSeconds + "s linear";
                        el.style.width = "100%";
                    } else {
                        el.style.display = "none";
                    }
                });
            }
        });
    });

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
