const _modalData = new WeakMap();

window.Bootstrap = {
    Modal: {
        Show: function (container) {
            let data = _modalData.get(container);
            if (!data) {
                var modal = new bootstrap.Modal(container, {});
                data = { modal: modal };
                _modalData.set(container, data);
                
                container.addEventListener('shown.bs.modal', function () {
                    if (container.dataset.autofocus === undefined) {
                        return;
                    }

                    var selectEl = container.querySelector("[data-select]");
                    if (selectEl) {
                        selectEl.setSelectionRange(0, selectEl.value.length);
                    }

                    const autofocusEl = container.querySelector('[data-autofocus]');
                    let targetFocusElement = autofocusEl || container.querySelector("input");

                    if (targetFocusElement) {
                        targetFocusElement.scrollIntoView(true);
                        targetFocusElement.focus();
                    }
                });
            }

            data.modal.show();
        },
        Hide: function (container) {
            _modalData.get(container)?.modal?.hide();
        },
        IsOpen: function (container) {
            return container.classList.contains("show");
        },
        Dispose: function (container) {
            _modalData.get(container)?.modal?.dispose();
            _modalData.delete(container);
        }
    },
    Offcanvas: {
        Initialize: function (interop, container) {
            let offcanvas = bootstrap.Offcanvas.getInstance(container);
            if (!offcanvas) {
                offcanvas = new bootstrap.Offcanvas(container);
                container.addEventListener("show.bs.offcanvas", () => {
                    interop.invokeMethodAsync("Offcanvas.VisibilityChanged", true);
                });
                container.addEventListener("hide.bs.offcanvas", () => {
                    interop.invokeMethodAsync("Offcanvas.VisibilityChanged", false);
                });
            }
        },
        Show: function (container) {
            bootstrap.Offcanvas.getInstance(container).show()
        },
        Hide: function (container) {
            bootstrap.Offcanvas.getInstance(container).hide();
        },
        Dispose: function (container) {
            bootstrap.Offcanvas.getInstance(container).dispose();

            // If the offcanvas was shown, the body styles are not reset on dispose.
            document.body.style.paddingRight = null;
            document.body.style.overflow = null;
        }
    },
    Tooltip: {
        Init: function (container) {
            var tooltip = bootstrap.Tooltip.getInstance(container);
            if (tooltip == null) {
                tooltip = new bootstrap.Tooltip(container);
            }
        },
        Show: function (container) {
            var tooltip = bootstrap.Tooltip.getInstance(container);
            if (tooltip != null) {
                tooltip.show();
            }
        },
        Hide: function (container) {
            var tooltip = bootstrap.Tooltip.getInstance(container);
            if (tooltip != null) {
                tooltip.hide();
            }
        },
        Dispose: function (container) {
            var tooltip = bootstrap.Tooltip.getInstance(container);
            if (tooltip != null) {
                tooltip.dispose();
            }
        }
    },
    Dropdown: {
        Init: function (container) {
            var dropDown = bootstrap.Dropdown.getInstance(container);
            if (dropDown == null) {
                dropDown = new bootstrap.Dropdown(container);
            }
        },
        Show: function (container) {
            var dropDown = bootstrap.Dropdown.getInstance(container);
            if (dropDown != null) {
                dropDown.show();
            }
        },
        Hide: function (container) {
            var dropDown = bootstrap.Dropdown.getInstance(container);
            if (dropDown != null) {
                dropDown.hide();
            }
        },
        Dispose: function (container) {
            var dropDown = bootstrap.Dropdown.getInstance(container);
            if (dropDown != null) {
                dropDown.dispose();
            }
        }
    },
    Popover: {
        Show: function (container) {
            var popover = bootstrap.Popover.getInstance(container);
            if (popover == null) {
                popover = new bootstrap.Popover(container, {
                    placement: "bottom"
                });
            }
        },
        Dispose: function (container) {
            var popover = bootstrap.Popover.getInstance(container);
            if (popover != null) {
                popover.dispose();
            }
        }
    },
    Theme: {
        Apply: function (theme) {
            document.documentElement.setAttribute("data-bs-theme", theme);
        },
        GetBrowserPreference: function () {
            return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
        }
    }
};

window.ElementReference = {
    ScrollIntoView: function (element) {
        element.scrollIntoView();
    },
    Blur: function (element) {
        element.blur();
    },
    GetValue: function (input) {
        return input.value;
    }
};

window.Recollections = {
    NavigateTo: function (href) {
        window.location.href = href;
        return true;
    },
    SetTitle: function (title) {
        document.title = title;
    },
    WaitForDotNet: () => window.Recollections._DotNetPromise,
    DotNetReady: () => window.Recollections._DotNetPromiseResolve()
};
window.Recollections._DotNetPromise = new Promise(resolve => window.Recollections._DotNetPromiseResolve = resolve);

const _easyMdeData = new WeakMap();

window.InlineMarkdownEdit = {
    Initialize: function (interop, textArea, value) {
        if (_easyMdeData.has(textArea)) {
            return;
        }

        var editor = new EasyMDE({
            autoDownloadFontAwesome: false,
            element: textArea,
            autofocus: true,
            forceSync: true,
            spellChecker: false,
            toolbar: [
                "heading-2",
                "heading-3",
                "|",
                "bold",
                "italic",
                "strikethrough",
                "|",
                "unordered-list",
                "ordered-list",
                "|",
                "link",
                "quote",
                "horizontal-rule",
                {
                    name: "save",
                    className: "fa fa-check ms-auto",
                    title: "Save",
                    action: function (editor) {
                        var value = editor.value();
                        interop.invokeMethodAsync("Markdown.OnSave", value);
                    }
                },
                {
                    name: "cancel",
                    className: "fa fa-times",
                    title: "Close Editor",
                    action: function (editor) {
                        interop.invokeMethodAsync("Markdown.OnCancel");
                    }
                }
            ],
            shortcuts: {
                "save": "Ctrl-Enter",
                "cancel": "Escape"
            }
        });

        _easyMdeData.set(textArea, editor);

        if (value !== null) {
            InlineMarkdownEdit.SetValue(textArea, value);
        }
    },
    Destroy: function (textArea) {
        var editor = _easyMdeData.get(textArea);
        if (editor != null) {
            editor.toTextArea();
            _easyMdeData.delete(textArea);
        }
    },
    SetValue: function (textArea, value) {
        if (value === null) {
            value = "";
        }

        var editor = _easyMdeData.get(textArea);
        if (editor != null) {
            editor.value(value);
        }
    },
    GetValue: function (textArea) {
        var editor = _easyMdeData.get(textArea);
        if (editor != null) {
            return editor.value();
        }
    }
};

window.InlineTextEdit = {
    Initialize: function (interop, input) {
        input.focus();
        input.addEventListener('keyup', function (e) {
            if (e.keyCode == 27) {
                input.blur();
                setTimeout(function () {
                    interop.invokeMethodAsync("TextEdit.OnCancel");
                }, 1);
            }
        });
    }
};

window.Downloader = {
    FromUrl: function (name, url) {
        var link = document.createElement("a");
        link.target = "_blank";
        link.download = name;
        link.href = url;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    },
    FromStreamAsync: async function (name, stream, mimeType) {
        const arrayBuffer = await stream.arrayBuffer();
        const blob = new Blob([arrayBuffer], {
            type: mimeType
        });
        const url = URL.createObjectURL(blob);
        try {
            return Downloader.FromUrl(name, url);
        } finally {
            setTimeout(function () {
                URL.revokeObjectURL(url);
            }, 0);
        }
    }
};

async function setObjectUrlSource(element, stream, mimeType) {
    releaseMediaElementSource(element);

    const arrayBuffer = await stream.arrayBuffer();
    const blob = new Blob([arrayBuffer], {
        type: mimeType
    });
    const url = URL.createObjectURL(blob);
    const isVideo = element.tagName && element.tagName.toLowerCase() === "video";
    const releaseEvents = isVideo
        ? ["emptied", "error", "abort"]
        : ["load", "error", "abort"];
    element.src = url;
    registerMediaElementSource(element, () => {
        URL.revokeObjectURL(url);
    }, releaseEvents);

    if (typeof element.load === "function") {
        element.load();
    }
}

function registerMediaElementSource(element, cleanup, eventNames = []) {
    const wrappedCleanup = () => {
        if (element.__recollectionsReleaseSource !== wrappedCleanup) {
            return;
        }

        element.__recollectionsReleaseSource = null;
        for (const eventName of eventNames) {
            element.removeEventListener(eventName, wrappedCleanup);
        }

        cleanup();
    };

    element.__recollectionsReleaseSource = wrappedCleanup;
    for (const eventName of eventNames) {
        element.addEventListener(eventName, wrappedCleanup, { once: true });
    }

    return wrappedCleanup;
}

function releaseMediaElementSource(element) {
    const cleanup = element.__recollectionsReleaseSource;
    if (typeof cleanup === "function") {
        cleanup();
    }
}

function getStreamingMimeTypeCandidates(mimeType) {
    const normalizedMimeType = (mimeType || "video/mp4").toLowerCase();
    const [containerType] = normalizedMimeType.split(";");
    const candidates = [normalizedMimeType];

    if (containerType === "video/mp4") {
        candidates.push(
            'video/mp4; codecs="avc1.42E01E"',
            'video/mp4; codecs="avc1.42E01E, mp4a.40.2"',
            'video/mp4; codecs="avc1.4D401E, mp4a.40.2"',
            'video/mp4; codecs="avc1.64001F, mp4a.40.2"',
            'video/mp4; codecs="hev1.1.6.L93.B0"',
            'video/mp4; codecs="hvc1.1.6.L93.B0"',
            'video/mp4; codecs="hev1.1.6.L93.B0, mp4a.40.2"',
            'video/mp4; codecs="hvc1.1.6.L93.B0, mp4a.40.2"'
        );
    } else if (containerType === "video/webm") {
        candidates.push(
            'video/webm; codecs="vp8"',
            'video/webm; codecs="vp8, vorbis"',
            'video/webm; codecs="vp9"',
            'video/webm; codecs="vp9, opus"'
        );
    }

    return [...new Set(candidates)];
}

function findStreamingMimeType(mimeType) {
    if (typeof MediaSource === "undefined" || typeof MediaSource.isTypeSupported !== "function") {
        return mimeType || "video/mp4";
    }

    for (const candidate of getStreamingMimeTypeCandidates(mimeType)) {
        if (MediaSource.isTypeSupported(candidate)) {
            return candidate;
        }
    }

    return null;
}

function setStreamingVideoSource(element, stream, mimeType) {
    const normalizedMimeType = mimeType || "video/mp4";
    const streamingMimeType = findStreamingMimeType(normalizedMimeType);
    const isSupported = typeof MediaSource !== "undefined"
        && stream != null
        && typeof stream.stream === "function"
        && streamingMimeType != null;
    if (!isSupported) {
        return setObjectUrlSource(element, stream, normalizedMimeType);
    }

    releaseMediaElementSource(element);

    const mediaSource = new MediaSource();
    const url = URL.createObjectURL(mediaSource);
    let reader = null;
    let isDisposed = false;
    let resetPendingChunks = () => { };
    element.src = url;
    const cleanup = registerMediaElementSource(element, () => {
        if (isDisposed) {
            return;
        }

        isDisposed = true;
        resetPendingChunks();
        if (reader != null) {
            reader.cancel().catch(() => { });
        }

        URL.revokeObjectURL(url);
    }, ["emptied", "error", "abort"]);

    mediaSource.addEventListener("sourceclose", cleanup, { once: true });

    mediaSource.addEventListener("sourceopen", () => {
        if (isDisposed) {
            return;
        }

        let sourceBuffer;
        try {
            sourceBuffer = mediaSource.addSourceBuffer(streamingMimeType);
        } catch (error) {
            console.error("Unable to initialize streaming video playback.", error);
            void setObjectUrlSource(element, stream, normalizedMimeType);
            return;
        }

        reader = stream.stream().getReader();
        const pendingChunks = [];
        const maxPendingChunks = 8;
        let pendingChunkIndex = 0;
        let isReadingCompleted = false;
        let isAppending = false;
        let queueDrainPromise = null;
        let queueDrainResolver = null;

        const pendingChunkCount = () => pendingChunks.length - pendingChunkIndex;

        const resolveQueueDrain = () => {
            if (pendingChunkCount() < maxPendingChunks && queueDrainResolver != null) {
                queueDrainResolver();
                queueDrainPromise = null;
                queueDrainResolver = null;
            }
        };

        const waitForQueueDrain = () => {
            if (pendingChunkCount() < maxPendingChunks) {
                return Promise.resolve();
            }

            if (queueDrainPromise == null) {
                queueDrainPromise = new Promise(resolve => {
                    queueDrainResolver = resolve;
                });
            }

            return queueDrainPromise;
        };

        resetPendingChunks = () => {
            pendingChunks.length = 0;
            pendingChunkIndex = 0;
            resolveQueueDrain();
        };

        const dequeueNextChunk = () => {
            if (pendingChunkCount() === 0) {
                return null;
            }

            const chunk = pendingChunks[pendingChunkIndex++];
            if (pendingChunkIndex >= pendingChunks.length) {
                pendingChunks.length = 0;
                pendingChunkIndex = 0;
            } else if (pendingChunkIndex > 32 && pendingChunkIndex * 2 >= pendingChunks.length) {
                pendingChunks.splice(0, pendingChunkIndex);
                pendingChunkIndex = 0;
            }

            resolveQueueDrain();
            return chunk;
        };

        const closeStreamIfReady = () => {
            if (isDisposed || !isReadingCompleted || isAppending || sourceBuffer.updating || pendingChunkCount() > 0 || mediaSource.readyState !== "open") {
                return;
            }

            try {
                mediaSource.endOfStream();
            } catch (error) {
                console.error("Unable to finalize streaming video playback.", error);
            }
        };

        const appendNextChunk = () => {
            if (isDisposed || isAppending || sourceBuffer.updating || pendingChunkCount() === 0) {
                closeStreamIfReady();
                return;
            }

            const nextChunk = dequeueNextChunk();
            if (nextChunk == null) {
                closeStreamIfReady();
                return;
            }

            isAppending = true;
            try {
                sourceBuffer.appendBuffer(nextChunk);
            } catch (error) {
                isAppending = false;
                isReadingCompleted = true;
                resetPendingChunks();
                console.error("Unable to append streamed video chunk.", error);
                reader.cancel(error).catch(() => { });
                closeStreamIfReady();
            }
        };

        sourceBuffer.addEventListener("updateend", () => {
            isAppending = false;
            appendNextChunk();
        });

        sourceBuffer.addEventListener("error", error => {
            isReadingCompleted = true;
            resetPendingChunks();
            console.error("Streaming video source buffer error.", error);
            reader.cancel(error).catch(() => { });
            closeStreamIfReady();
        });

        void (async () => {
            try {
                while (true) {
                    if (isDisposed) {
                        break;
                    }

                    if (pendingChunkCount() >= maxPendingChunks) {
                        await waitForQueueDrain();
                        continue;
                    }

                    const { value, done } = await reader.read();
                    if (done) {
                        isReadingCompleted = true;
                        closeStreamIfReady();
                        break;
                    }

                    if (value != null && value.byteLength > 0) {
                        pendingChunks.push(value.slice());
                        appendNextChunk();
                    }
                }
            } catch (error) {
                console.error("Unable to read streamed video data.", error);
                if (mediaSource.readyState === "open") {
                    try {
                        mediaSource.endOfStream("network");
                    } catch (endOfStreamError) {
                        console.error("Unable to terminate failed video stream.", endOfStreamError);
                    }
                }
            } finally {
                reader.releaseLock();
            }
        })();
    }, { once: true });

    if (typeof element.load === "function") {
        element.load();
    }
}

window.ImageSource = {
    Set: async function(element, stream, mimeType) {
        const isVideo = (mimeType && mimeType.startsWith("video/"))
            || (element.tagName && element.tagName.toLowerCase() === "video");
        if (isVideo) {
            return setStreamingVideoSource(element, stream, mimeType);
        }

        return setObjectUrlSource(element, stream, mimeType);
    }
};
