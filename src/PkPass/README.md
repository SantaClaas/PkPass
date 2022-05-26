
## Debug
If you have issues with debugging activate the service worker update on reload in developer tools to always get the latest version and not the cashed ones
## Resources
https://www.nuget.org/packages/ZXing.Net/
https://developer.apple.com/library/archive/documentation/UserExperience/Reference/PassKit_Bundle/Chapters/TopLevel.html#//apple_ref/doc/uid/TP40012026-CH2-SW1
https://developer.apple.com/documentation/walletpasses/pass
https://developer.apple.com/library/archive/documentation/UserExperience/Conceptual/PassKit_PG/Creating.html#//apple_ref/doc/uid/TP40012195-CH4
https://developer.apple.com/library/archive/documentation/UserExperience/Conceptual/PassKit_PG/index.html#//apple_ref/doc/uid/TP40012195-CH1-SW1
## TODOs
* Add [persistent storage (web.dev)](https://web.dev/persistent-storage/) when user opens pass and wants to keep it in app
* Add ways to allow opening pass files
  * Through share target (WIP)
  * Through file type handler registration (WIP)
  * Through vanilla file input
  * Through drag & drop
  * Through openShowFilePicker API
  * Through clipboard