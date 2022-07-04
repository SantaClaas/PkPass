
const cacheFiles = async files => {
    if (files.length === 0) return;

    const cache = await caches.open("files");
    const putFilesInCachePromises = files.map(file => {
        const path = `/files/${file.name}`;
        const request = new Request(path, {method: "GET"});
        const requestOptions = {
            status: 200,
            statusText: "OK",
            headers: new Headers({
                // "attachment" is the the better fit than "inline" even though not 100% correct
                "Content-Disposition": `attachment; filename=${file.name}`
            })
        }
        const response = new Response(file, requestOptions);
        return cache.put(request, response);
    });
    await Promise.all(putFilesInCachePromises);
}

const cacheFileHandles = async fileHandles => {
    const getFilesPromises = fileHandles.map(handle => handle.getFile());
    const files = await Promise.all(getFilesPromises);
    await cacheFiles(files);
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
        
        cacheFileHandles(launchParameters.files)
            .catch(console.error);
        //TODO notify app or navigate to open page for one pass 
        // .then(() => window.open("/open/" + file.name, "_self"))
    })
} else {
    console.info("File Handling API is not supported 😔. Maybe it is only in canary or dev chromium browser versions yet?");
}

const getFilesUsingInput = input =>
    new Promise((resolve, reject) => {
        // Generous timeout of 5 minutes
        const timeOutInMilliseconds = 1000 * 60 * 5;
        const timeoutId = setTimeout(() => {
            reject("Timed out waiting for event to notify files have changed");
        }, timeOutInMilliseconds);
        
        const handleChange = (event) => {
            input.removeEventListener("change", handleChange);
            clearTimeout(timeoutId);
            const files = Array.from(event.target.files);
            resolve(files);
        }
        input.addEventListener("blur", () => console.info("blur"));
        input.addEventListener("change", handleChange);
        input.click();
    });

const getFilesFromUser = async fallBackInput => {
    if (!window.hasOwnProperty('showOpenFilePicker') || true) {
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
    const fileHandles = await window.showOpenFilePicker(options);
    const getFilesPromises = fileHandles.map(handle => handle.getFile());
    return await Promise.all(getFilesPromises);
}

async function getAndCacheFilesFromUser(fallBackInput) {
    const files = await getFilesFromUser(fallBackInput);
    if (files.length === 0) {
        console.info("User selected no files.");
        return;
    }
    
    await cacheFiles(files);
}

console.info("File handling loaded");