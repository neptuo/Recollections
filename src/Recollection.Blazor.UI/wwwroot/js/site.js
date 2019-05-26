window.Recollection = {
    NavigateTo: function (href) {
        window.location.href = href;
        return true;
    },
    SaveToken: function (token) {
        if ("localStorage" in window) {
            if (token == null)
                window.localStorage.removeItem("token");
            else
                window.localStorage.setItem("token", token);
        }
    },
    LoadToken: function () {
        if ("localStorage" in window) {
            return window.localStorage.getItem("token");
        }

        return null;
    }
};