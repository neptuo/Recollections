import PhotoSwipeLightbox from './photoswipe/photoswipe-lightbox.esm.js';

const options = {
    showHideAnimationType: 'none',
    pswpModule: './photoswipe.esm.js',
    pswpCSS: '/_content/Recollections.Blazor.Components/photoswipe/photoswipe.css'
};
const lightbox = new PhotoSwipeLightbox(options);
let isInitiazed = false;
let autoPlayTimer = null;
let stopCallback = () => { };
let interop = null;
let items = [];

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

function isVideo(model) {
    const type = (model.type || 'image').toLowerCase();
    return type === 'video';
}

export function initialize(intr, i) {
    interop = intr;
    items = i;

    if (!isInitiazed) {
        lightbox.on('uiRegister', function () {
            lightbox.pswp.ui.registerElement({
                name: 'autoplay',
                order: 9,
                isButton: true,
                html: playIcon,
                onInit: (el, pswp) => {
                    lightbox.pswp.on('close', () => {
                        stop(el);
                    });
                    
                    lightbox.pswp.on('change', () => {
                        const index = lightbox.pswp.currIndex;
                        const model = items[index];
                        if (isVideo(model)) {
                            stop(el);
                        }
                    });
                },
                onClick: (event, el) => {

                    if (autoPlayTimer == null) {
                        play(el);
                    } else {
                        stop(el);
                    }
                }
            });

            lightbox.pswp.ui.registerElement({
                name: 'info',
                order: 9,
                isButton: true,
                html: '<i class="fas fa-info-circle"></i>',
                onClick: () => {
                    interop.invokeMethodAsync("OpenInfoAsync", lightbox.pswp.currIndex);
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
                        const index = lightbox.pswp.currIndex;
                        const model = items[index];

                        let title = lightbox.pswp.currSlide.data.alt || '';
                        if (isVideo(model)) {
                            title += ' (click to play video)';
                        }
                        el.innerHTML = title;
                        el.style.display = '';
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

            lightbox.pswp.on('change', () => {
                const index = lightbox.pswp.currIndex;
                const model = items[index];
                if (isVideo(model)) {
                    lightbox.pswp.currSlide.image.classList.add('pswp__video');
                } else {
                    lightbox.pswp.currSlide.image.classList.remove('pswp__video');
                }
            });

            lightbox.pswp.on('pointerUp', async e => {
                const index = lightbox.pswp.currIndex;
                const model = items[index];
                if (!isVideo(model) || e.originalEvent.target != lightbox.pswp.currSlide.image) {
                    return;
                }

                e.preventDefault();
                if (lightbox.pswp.currSlide.image.tagName.toLowerCase() === 'video') {
                    return;
                }

                const titleEl = lightbox.pswp.scrollWrap.parentElement.querySelector(".pswp__title");
                const originalTitle = lightbox.pswp.currSlide.data.alt || '';

                titleEl.innerHTML = `${originalTitle} (loading video...)`;
                
                const stream = await interop.invokeMethodAsync("GetImageDataAsync", index, "original");
                const arrayBuffer = await stream.arrayBuffer();
                const blob = new Blob([arrayBuffer], {
                    type: model.contentType || "video/mp4"
                });
                const url = URL.createObjectURL(blob);

                const imageEl = lightbox.pswp.currSlide.image;
                const videoEl = document.createElement('video');
                videoEl.src = url;
                videoEl.controls = true;
                videoEl.playsInline = true;
                videoEl.autoplay = true;
                videoEl.className = imageEl.className;
                videoEl.style.width = imageEl.style.width;
                videoEl.style.height = imageEl.style.height;

                imageEl.parentNode.replaceChild(videoEl, imageEl);
                lightbox.pswp.currSlide.image = videoEl;
                titleEl.style.display = 'none';
            });
        });

        lightbox.on("numItems", (e) => {
            // Just for auto function.
            lightbox.pswp.numItems = items.length;

            e.numItems = items.length
        });

        lightbox.on("itemData", (e) => {
            const model = items[e.index];
            const type = (model.type || 'image').toLowerCase();

            e.itemData = {
                w: model.width,
                h: model.height,
                alt: model.title,
            };

            // Src is only used when swiping images in gallery.
            // On every gallery open, all images are refetched (fortunately disk cache is used).
            // It is caused by the reseting of images array on every gallery component render.
            if (model.src) {
                e.itemData.src = model.src;
            } else if (model.provider) {
                e.itemData.provider = model.provider;
            } else {
                e.itemData.provider = model.provider = new Promise(async resolve => {
                    const stream = await interop.invokeMethodAsync("GetImageDataAsync", e.index, "");
                    const arrayBuffer = await stream.arrayBuffer();
                    const blob = new Blob([arrayBuffer], {
                        // type: model.contentType || (type === 'video' ? "video/mp4" : "image/png")
                        type: "image/png"
                    });
                    const url = URL.createObjectURL(blob);
                    model.src = url;

                    console.log(`Loading image at index '${e.index}'`);
                    resolve(url);
                });
            }

            // if (type === 'video') {
            //     // PhotoSwipe will render this HTML instead of treating it as an image.
            //     // Src is injected asynchronously via ensureVideoSrc (from .NET stream interop).
            //     const actualSize = (model.width && model.height) ? `width:${model.width}px;height:${model.height}px;` : '';
            //     e.itemData.html = `<video data-pswp-index="${e.index}" controls playsinline preload style="min-width:200px;min-height:150px;max-width:100%;max-height:calc(100% - 100px);display:block;margin:40px auto;"></video>`;

            //     // Ensure src gets set once the slide is in DOM.
            //     setTimeout(() => ensureVideoSrc(e.index), 0);
            //     return;
            // }
        });

        lightbox.init();
        isInitiazed = true;
    }
}

export function open(index) {
    lightbox.loadAndOpen(index);
}

export function isOpen() {
    if (lightbox.pswp) {
        return lightbox.pswp.isOpen && !lightbox.pswp.isDestroying;
    }

    return false;
}

export function close() {
    if (lightbox.pswp) {
        lightbox.pswp.close();
    }
}
