export function createObserver(scrollContainer, observerable) {
    
    function intersectionCallback(nodes) {
        for (let node of nodes) {
            const intersectionEvent = new CustomEvent("intersect", {
                bubbles: true,
                detail: { isIntersecting: node.isIntersecting},
            });
            node.target.dispatchEvent(intersectionEvent);
        }
    }

    // observerable.value.addEventListener("intersect", console.log);
    const observer = new IntersectionObserver(intersectionCallback, {
        root: scrollContainer.value,
        // Only trigger the event when it overlaps completely
        threshold: 1
    });

    observer.observe(observerable.value);
    return observer;
}