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
    }
}
