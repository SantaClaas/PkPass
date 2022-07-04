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

const isFilePickerSupported = window.hasOwnProperty('showOpenFilePicker');

async function registerClick(button, fallbackInput, dotnetCallbackReference) {
    const cacheInputFiles = async () => {
        const files = Array.from(fallbackInput.files);
        await cacheFiles(files);
        await dotnetCallbackReference.invokeMethodAsync("PassesChanged");
        // Reset input so the event triggers when the same file is added
        fallbackInput.value = null;
    }

    fallbackInput.addEventListener("change", () => cacheInputFiles().catch(console.error))

    const requestFilesOrClickInput = async () => {
        if (!isFilePickerSupported) {
            // The change event will be triggered which will cause the input change handler that caches the files
            fallbackInput.click();
            return;
        }

        const fileHandles = await window.showOpenFilePicker(options);
        const getFilesPromises = fileHandles.map(handle => handle.getFile());
        const files = await Promise.all(getFilesPromises);
        await cacheFiles(files);
        await dotnetCallbackReference.invokeMethodAsync("PassesChanged");
    }

    button.addEventListener("click", () => requestFilesOrClickInput().catch(console.error));
}

console.info("File handling loaded");