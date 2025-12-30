window.Bootstrap = {
    Modal: {
        Show: function (container) {
            var $container = $(container);
            if (!$container.data("modal-initialized")) {
                var modal = new bootstrap.Modal(container, {});
                $container.data("modal", modal);

                $container.data("modal-initialized", true);
                
                $container.on('shown.bs.modal', function () {
                    if ($container.data("autofocus") === undefined) {
                        return;
                    }

                    var $select = $container.find("[data-select]");
                    if ($select.length > 0) {
                        $select[0].setSelectionRange(0, $select[0].value.length)
                    }

                    const autofocus = $container.find('[data-autofocus]');
                    let targetFocusElement;
                    if (autofocus.length > 0) {
                        targetFocusElement = autofocus.first();
                    } else {
                        targetFocusElement = $container.find("input").first();
                    }

                    targetFocusElement[0].scrollIntoView(true);
                    targetFocusElement.trigger('focus');
                });
            }

            $container.data("modal").show();
        },
        Hide: function (container) {
            $(container).data("modal")?.hide();
        },
        IsOpen: function (container) {
            return $(container).hasClass("show");
        },
        Dispose: function (container) {
            $(container).data("modal").dispose();
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
        return $(input).val();
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

window.InlineMarkdownEdit = {
    Initialize: function (interop, textArea, value) {
        $textArea = $(textArea);
        if ($textArea.data("easymde") != null) {
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

        $textArea.data("easymde", editor);

        if (value !== null) {
            InlineMarkdownEdit.SetValue(textArea, value);
        }
    },
    Destroy: function (textArea) {
        var editor = $(textArea).data("easymde");
        if (editor != null) {
            editor.toTextArea();
        }
    },
    SetValue: function (textArea, value) {
        if (value === null) {
            value = "";
        }

        var editor = $(textArea).data("easymde");
        if (editor != null) {
            editor.value(value);
        }
    },
    GetValue: function (textArea) {
        var editor = $(textArea).data("easymde");
        if (editor != null) {
            return editor.value();
        }
    }
};

window.InlineTextEdit = {
    Initialize: function (interop, input) {
        $(input).focus().keyup(function (e) {
            if (e.keyCode == 27) {
                $(this).blur();
                setTimeout(function () {
                    interop.invokeMethodAsync("TextEdit.OnCancel");
                }, 1);
            }
        });
    }
};

window.InlineDateEdit = {
    Initialize: function (input, format) {
        $(input).focus().datepicker({
            format: format.toLowerCase(),
            autoclose: true,
            todayHighlight: true,
            todayBtn: "linked"
        });
    },
    Destroy: function (input) {
        $(input).datepicker("destroy");
    },
    GetValue: function (input) {
        return $(input).val();
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