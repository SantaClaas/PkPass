namespace PkPass.Components

open System.Threading.Tasks
open Bolero
open Bolero.Html
open Microsoft.AspNetCore.Components
open Microsoft.JSInterop

module Elements =
    let boardingPass () =
        article {
            attr.``class``
                "origin-top transition-transform scale-[var(--scale)] bg-gray-100 rounded-lg aspect-[1/1.62] overflow-hidden flex flex-col justify-between mb-10 shadow-xl border-2 border-blue-300"

            section {
                // Header fields
                header {
                    attr.``class`` "bg-gray-200 h-12 px-2 pt-2 pb-1 w-full flex justify-between items-center"

                    div {
                        attr.``class`` "bg-gray-300 w-full h-7 align-middle"

                        p {
                            attr.``class`` "align-middle"
                            "Logo"
                        }
                    }

                    div {
                        attr.``class`` "bg-gray-300 w-full text-center h-7 align-middle"

                        p {
                            attr.``class`` "align-middle"
                            "Boarding pass"
                        }
                    }

                    div {
                        attr.``class`` "bg-gray-300 w-full text-end h-7 align-middle"

                        p {
                            attr.``class`` "align-middle"
                            "Header fields"
                        }
                    }
                }
                // Primary fields
                article {
                    attr.``class`` "bg-gray-200 rounded flex justify-between h-20 px-2 py-1"
                    div { attr.``class`` "aspect-video bg-gray-300 rounded" }
                    div { attr.``class`` "aspect-video bg-gray-300 rounded" }
                }

                // Auxiliary fields
                article {
                    attr.``class`` "bg-gray-200 h-10 px-2 py-1"
                    div { attr.``class`` "bg-gray-300 bg-gray-200 w-full h-full rounded" }
                }

                article {
                    attr.``class`` "bg-gray-200 h-10 px-2 py-1"
                    div { attr.``class`` "bg-gray-300 bg-gray-200 w-full h-full rounded" }
                }
            }

            section {
                article {
                    attr.``class`` "bg-gray-200 h-8 px-2 py-1"
                    div { attr.``class`` "bg-gray-300 bg-gray-200 w-full h-full rounded" }
                }

                article {
                    attr.``class`` "bg-gray-200 px-2 pb-2 pt-1"

                    div {
                        attr.``class`` "bg-gray-300 bg-gray-200 rounded p-3"
                        div { attr.``class`` "aspect-square w-60 bg-gray-400 m-auto rounded-xl" }
                    }
                }
            }
        }
