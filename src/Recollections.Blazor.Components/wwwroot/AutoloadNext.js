export function observe(element, component) {
    if ("IntersectionObserver" in window) {
        const observer = new IntersectionObserver(
            (entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        component.invokeMethodAsync("intersected");
                    }
                });
            },
            {
                root: null
            }
        );

        observer.observe(element);
    }
}