window['updateAvailable']
    .then(isAvailable => {
        if (isAvailable) {
            alert("Update!");
        }
    });