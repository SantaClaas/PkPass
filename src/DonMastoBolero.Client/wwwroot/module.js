
export function createObjectUrl(file) {
    return URL.createObjectURL(file);
}
export function getAttribute(object, attribute) { return object[attribute]; }

export function getLoadedFile(index) {

    return loadedPasses[index];
}
