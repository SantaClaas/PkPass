const cacheFiles = async fileHandles => {
    const cacheOpenPromise = caches.open("files");
    const getFilesPromises = fileHandles.map(handle => handle.getFile());
    const files = await Promise.all(getFilesPromises);
    const cache = await cacheOpenPromise;
    const putFilesInCachePromises = files.map(file => {
        const path = `/files/${file.name}`;
        const request = new Request(path, {method: "GET"});
        const response = new Response(file, {status: 200, statusText: "OK"});
        return cache.put(request, response);
    });
    await Promise.all(putFilesInCachePromises);
}
if ('launchQueue' in window && 'files' in LaunchParams.prototype) {
    console.info("File Handling API is supported! 🥳🎉");
    /* 
     * This will receive files through the OS file handler registration. 
     * Share target file sharing is received by service worker 
     */
    launchQueue.setConsumer((launchParameters) => {
        // Nothing to do when the queue is empty
        if (!launchParameters.files.length) {
            console.info("File handling queue is empty. Don't need to handle files")

            return;
        }

        cacheFiles(launchParameters.files)
            .catch(console.error);
        //TODO notify app or navigate to open page for one pass 
        // .then(() => window.open("/open/" + file.name, "_self"))

    })
} else {
    console.info("Fole Handling API is not supported 😔. Maybe it is only in canary or dev chromium browser versions yet?");
}

const getFilesUsingInput = input =>
    new Promise((resolve) => {
        const handleChange = (event) => {
            input.removeEventListener("change", handleChange);
            resolve(event.target.files);
        }

        input.addEventListener("change", handleChange);
        input.click();
    });

const getFilesFromUser = async fallBackInput => {
    if (!window.hasOwnProperty('showOpenFilePicker')) {
        return await getFilesUsingInput(fallBackInput);
    }

    const options = {
        types: [
            {
                description: 'Pass files',
                accept: {
                    'application/vnd.apple.pkpass': ['.pkpass'],
                    'application/vnd.apple.pkpasses': ['.pkpasses']
                }
            }
        ]
    };
    return window.showOpenFilePicker(options);
}

async function getAndCacheFilesFromUser(fallBackInput) {
// const getAndCacheFilesFromUser = async fallBackInput => {
    const files = await getFilesFromUser(fallBackInput);
    await cacheFiles(files);
}

console.info("File handling loaded");