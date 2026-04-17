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
        ShowFromElement: function (trigger, contentElement) {
            Bootstrap.Popover._hideActive();

            var originalTitle = trigger.getAttribute("title");
            if (originalTitle) {
                trigger.removeAttribute("title");
            }

            var popover = new bootstrap.Popover(trigger, {
                html: true,
                sanitize: false,
                content: contentElement.innerHTML,
                placement: "top",
                trigger: "manual",
                customClass: "entry-popover"
            });

            popover.show();

            var dismissHandler = function (e) {
                var tip = popover.tip;
                if (tip && !tip.contains(e.target) && !trigger.contains(e.target)) {
                    document.removeEventListener("pointerdown", dismissHandler);
                    popover.dispose();
                    if (originalTitle) {
                        trigger.setAttribute("title", originalTitle);
                    }
                    Bootstrap.Popover._active = null;
                }
            };

            Bootstrap.Popover._active = { popover: popover, dismiss: dismissHandler, trigger: trigger, originalTitle: originalTitle };

            setTimeout(function () {
                document.addEventListener("pointerdown", dismissHandler);
            }, 0);
        },
        _active: null,
        _hideActive: function () {
            if (Bootstrap.Popover._active) {
                document.removeEventListener("pointerdown", Bootstrap.Popover._active.dismiss);
                Bootstrap.Popover._active.popover.dispose();
                if (Bootstrap.Popover._active.originalTitle) {
                    Bootstrap.Popover._active.trigger.setAttribute("title", Bootstrap.Popover._active.originalTitle);
                }
                Bootstrap.Popover._active = null;
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

window.Recollections.Notifications = {
    isSupported: function () {
        return "serviceWorker" in navigator && "PushManager" in window && typeof window.Notification !== "undefined";
    },
    getPermission: function () {
        if (typeof window.Notification === "undefined") {
            return "unsupported";
        }

        return window.Notification.permission;
    },
    getTimeZone: function () {
        return Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC";
    },
    getSubscription: async function () {
        if (!window.Recollections.Notifications.isSupported()) {
            return null;
        }

        const registration = await navigator.serviceWorker.ready;
        const subscription = await registration.pushManager.getSubscription();
        return mapSubscription(subscription);
    },
    subscribe: async function (publicKey) {
        if (!publicKey) {
            throw new Error("Missing public push key.");
        }

        if (!window.Recollections.Notifications.isSupported()) {
            throw new Error("Push notifications are not supported in this browser.");
        }

        let permission = window.Notification.permission;
        if (permission !== "granted") {
            permission = await window.Notification.requestPermission();
        }

        if (permission !== "granted") {
            throw new Error("Notification permission was not granted.");
        }

        const registration = await navigator.serviceWorker.ready;
        let subscription = await registration.pushManager.getSubscription();
        if (subscription === null) {
            subscription = await registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlBase64ToUint8Array(publicKey)
            });
        }

        return mapSubscription(subscription);
    },
    unsubscribe: async function () {
        if (!window.Recollections.Notifications.isSupported()) {
            return null;
        }

        const registration = await navigator.serviceWorker.ready;
        const subscription = await registration.pushManager.getSubscription();
        const mapped = mapSubscription(subscription);
        if (subscription !== null) {
            await subscription.unsubscribe();
        }

        return mapped;
    }
};

function mapSubscription(subscription) {
    if (subscription === null) {
        return null;
    }

    const json = subscription.toJSON();
    return {
        endpoint: json.endpoint,
        p256dh: json.keys?.p256dh,
        auth: json.keys?.auth
    };
}

function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const normalized = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const raw = window.atob(normalized);
    const output = new Uint8Array(raw.length);
    for (let i = 0; i < raw.length; i++) {
        output[i] = raw.charCodeAt(i);
    }

    return output;
}

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
    FromUrlAsync: function (name, url) {
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
        return Downloader.FromUrlAsync(name, url);
    }
};

window.ImageSource = {
    Set: async function(element, stream, mimeType) {
        const arrayBuffer = await stream.arrayBuffer();
        const blob = new Blob([arrayBuffer], {
            type: mimeType
        });
        const url = URL.createObjectURL(blob);
        element.onload = () => {
            URL.revokeObjectURL(url);
        }
        element.src = url;
    },
    SetFromUrl: async function (element, url, mimeType, bearerToken) {
        const headers = {};
        if (bearerToken) {
            headers["Authorization"] = "Bearer " + bearerToken;
        }

        let response;
        try {
            response = await fetch(url, { headers });
        } catch (e) {
            console.warn("ImageSource.SetFromUrl: fetch failed", url, e);
            return 0;
        }

        if (!response.ok) {
            return response.status;
        }

        const blob = await response.blob();
        const blobType = mimeType || blob.type || undefined;
        const typedBlob = blobType && blob.type !== blobType ? blob.slice(0, blob.size, blobType) : blob;
        const objectUrl = URL.createObjectURL(typedBlob);

        const revoke = () => URL.revokeObjectURL(objectUrl);
        const tag = element.tagName ? element.tagName.toLowerCase() : "";
        if (tag === "video") {
            element.addEventListener("loadeddata", revoke, { once: true });
            element.addEventListener("error", revoke, { once: true });
        } else {
            element.addEventListener("load", revoke, { once: true });
            element.addEventListener("error", revoke, { once: true });
        }

        element.src = objectUrl;
        return response.status;
    }
}

function escapeCssSelectorValue(value) {
    var stringValue = String(value);

    if (window.CSS && typeof window.CSS.escape === "function") {
        return window.CSS.escape(stringValue);
    }

    return stringValue.replace(/["\\]/g, "\\$&");
}

function canReplacePageHistoryState() {
    return window.history && typeof window.history.replaceState === "function";
}

function readPageHistoryUserState() {
    var historyState = window.history.state;
    var userState = {};
    var serializedUserState = historyState && typeof historyState === "object" ? historyState.userState : null;
    if (typeof serializedUserState === "string" && serializedUserState.length > 0) {
        try {
            userState = JSON.parse(serializedUserState);
        } catch {
            userState = {};
        }
    }

    if (userState && typeof userState === "object") {
        if (!userState.Map && typeof userState.Latitude === "number" && typeof userState.Longitude === "number") {
            userState = { Map: userState };
        } else if (!userState.Timeline && typeof userState.Offset === "number" && typeof userState.EntryId === "string") {
            userState = { Timeline: userState };
        }
    } else {
        userState = {};
    }

    return {
        historyState: historyState,
        userState: userState
    };
}

function updatePageHistoryUserState(update) {
    if (!canReplacePageHistoryState() || typeof update !== "function") {
        return;
    }

    var current = readPageHistoryUserState();
    update(current.userState);

    var nextHistoryState = current.historyState && typeof current.historyState === "object"
        ? Object.assign({}, current.historyState)
        : {};

    nextHistoryState.userState = JSON.stringify(current.userState);
    window.history.replaceState(nextHistoryState, "", window.location.href);
}

window.Timeline = {
    StorePosition: function (element) {
        if (!element) {
            return;
        }

        var entryId = element.getAttribute("data-entry-id");
        var offset = Number.parseInt(element.getAttribute("data-entry-offset"), 10);
        if (!entryId || Number.isNaN(offset)) {
            return;
        }

        updatePageHistoryUserState(function (userState) {
            userState.Timeline = {
                Offset: offset,
                EntryId: entryId
            };
        });
    },
    StorePositionOnKeyDown: function (element, event) {
        if (event && (event.key === "Enter" || event.key === " ")) {
            window.Timeline.StorePosition(element);
        }
    },
    ClearPosition: function () {
        updatePageHistoryUserState(function (userState) {
            delete userState.Timeline;
        });
    },
    ScrollToEntry: function (entryId) {
        var escapedEntryId = escapeCssSelectorValue(entryId);
        var element = document.querySelector('[data-entry-id="' + escapedEntryId + '"]');
        if (element) {
            element.scrollIntoView({ block: "center" });
        }
    }
};
