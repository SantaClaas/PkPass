function scaleChildren(event) {
    const list = event.target;
    const children = list.children;
    const height = list.clientHeight;
    const scrollHeight = list.scrollHeight - height;
    const scrollTop = list.scrollTop;
    const percent = scrollTop / scrollHeight;
    const count = children.length;
    // 👇 example how we shrink based on position and scroll percentage with never going above 100%
    // let positionPercent = 1 / 5
    // console.log([1,2,3,4,5].map(n => (100 - Math.floor(50 * (1 - n/5) * percent))))
    for (let index = 0; index < count; index++) {
        const child = children[index];
        const position = index + 1;
        const positionPercent = position / count;
        const value = (100 - Math.floor(50 * (1 - positionPercent) * percent));

        child.style.setProperty("--scale", `${value}%`);
    }
}

export function scaleOnScroll(listElement) {
    listElement.addEventListener("scroll", scaleChildren);
}