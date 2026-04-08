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
const imageIcon = '<i class="fas fa-image"></i>';
const videoIcon = '<i class="fas fa-film"></i>';

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

function pauseCachedVideo(model) {
    if (model?.videoElement instanceof HTMLVideoElement) {
        model.videoElement.pause();
    }
}

function attachCachedVideo(currentSlide, model, titleEl, originalTitle) {
    const cachedVideoEl = model?.videoElement;
    if (!(cachedVideoEl instanceof HTMLVideoElement)) {
        return null;
    }

    const imageEl = currentSlide.image;
    cachedVideoEl.autoplay = true;
    if (cachedVideoEl.ended) {
        cachedVideoEl.currentTime = 0;
    }

    imageEl.parentNode.replaceChild(cachedVideoEl, imageEl);
    currentSlide.image = cachedVideoEl;

    if (cachedVideoEl.readyState >= HTMLMediaElement.HAVE_CURRENT_DATA) {
        titleEl.style.display = 'none';
    } else {
        titleEl.textContent = `${originalTitle} (loading video...)`;
        titleEl.style.display = '';
    }

    return cachedVideoEl;
}

async function setVideoSource(element, stream, contentType) {
    if (window.ImageSource && typeof window.ImageSource.Set === 'function') {
        return window.ImageSource.Set(element, stream, contentType || 'video/mp4');
    }

    const arrayBuffer = await stream.arrayBuffer();
    const blob = new Blob([arrayBuffer], {
        type: contentType || 'video/mp4'
    });
    const url = URL.createObjectURL(blob);
    const release = element.__recollectionsReleaseSource;
    if (typeof release === 'function') {
        release();
    }

    const cleanup = () => {
        if (element.__recollectionsReleaseSource !== cleanup) {
            return;
        }

        element.__recollectionsReleaseSource = null;
        URL.revokeObjectURL(url);
        element.removeEventListener('emptied', cleanup);
        element.removeEventListener('error', cleanup);
        element.removeEventListener('abort', cleanup);
    };

    element.src = url;
    element.__recollectionsReleaseSource = cleanup;
    element.addEventListener('emptied', cleanup, { once: true });
    element.addEventListener('error', cleanup, { once: true });
    element.addEventListener('abort', cleanup, { once: true });
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
                        items.forEach(pauseCachedVideo);
                        stop(el);
                    });
                     
                    lightbox.pswp.on('change', () => {
                        const index = lightbox.pswp.currIndex;
                        const model = items[index];
                        items.forEach((item, itemIndex) => {
                            if (itemIndex !== index) {
                                pauseCachedVideo(item);
                            }
                        });

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
                        el.textContent = title;
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

            lightbox.pswp.ui.registerElement({
                name: 'media-type-badge',
                order: 9,
                isButton: false,
                appendTo: 'root',
                html: '',
                onInit: (el, pswp) => {
                    el.className = 'pswp__media-type-badge';
                    lightbox.pswp.on('change', () => {
                        const index = lightbox.pswp.currIndex;
                        const model = items[index];
                        el.innerHTML = isVideo(model) ? videoIcon : imageIcon;
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

                let videoEl = attachCachedVideo(lightbox.pswp.currSlide, model, titleEl, originalTitle);
                if (videoEl == null) {
                    titleEl.textContent = `${originalTitle} (loading video...)`;
                    const imageEl = lightbox.pswp.currSlide.image;
                    videoEl = document.createElement('video');
                    videoEl.controls = true;
                    videoEl.playsInline = true;
                    videoEl.autoplay = true;
                    videoEl.preload = 'auto';
                    videoEl.className = imageEl.className;
                    videoEl.style.width = imageEl.style.width;
                    videoEl.style.height = imageEl.style.height;
                    videoEl.poster = imageEl.currentSrc || imageEl.src || '';

                    videoEl.addEventListener('loadeddata', () => {
                        titleEl.style.display = 'none';
                    }, { once: true });
                    videoEl.addEventListener('error', () => {
                        titleEl.textContent = `${originalTitle} (unable to load video)`;
                        titleEl.style.display = '';
                    }, { once: true });

                    imageEl.parentNode.replaceChild(videoEl, imageEl);
                    lightbox.pswp.currSlide.image = videoEl;
                    model.videoElement = videoEl;
                    if (model.originalUrl) {
                        videoEl.src = model.originalUrl;
                    } else {
                        const stream = await interop.invokeMethodAsync("GetImageDataAsync", index, "original");
                        await setVideoSource(videoEl, stream, model.contentType);
                    }
                }

                if (videoEl.paused) {
                    videoEl.play().catch(() => { });
                }
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
                        type: "image/png"
                    });
                    const url = URL.createObjectURL(blob);
                    model.src = url;

                    console.log(`Loading image at index '${e.index}'`);
                    resolve(url);
                });
            }
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
