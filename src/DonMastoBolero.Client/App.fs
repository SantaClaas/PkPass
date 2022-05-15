module PkPass.Client.App

open System
open Microsoft.AspNetCore.Components.Web
open Bolero
open Bolero.Html
open Bolero.Html.attr
open Elmish
open Microsoft.JSInterop
open Microsoft.AspNetCore.Components
open Microsoft.Extensions.Logging

module Command = Cmd

type AuthorizationCode = AuthorizationCode of string

type ClientId = ClientId of string
type ClientSecret = ClientSecret of string

type ClientCredential =
    | Id of ClientId
    | Secret of ClientSecret

type ClientCredentialPair = ClientCredentialPair of clientId: ClientId * clientSecret: ClientSecret

type CredentialsInput = 
    | Valid of ClientCredentialPair
    | Invalid of ClientCredentialPair
type Model =
    { clientId: ClientId option
      clientSecret: ClientSecret option
      authorizationCode: AuthorizationCode option }

let initializeModel authorizationCode =
    { clientId = None
      clientSecret = None
      authorizationCode = authorizationCode }

type SaveClientCredentialError =
    | SaveSecretError of exn
    | SaveIdError of exn

type LoadClientCredentialError =
    | LoadSecretError of exn
    | LoadIdError of exn

type ClientCredentialError =
    | SaveError of SaveClientCredentialError
    | LoadError of LoadClientCredentialError

type Error = CredentialError of ClientCredentialError

type Message =
    | SaveClientCredential of ClientCredential
    | SetClientCredential of ClientCredential option
    | LogError of Error

let flip f x y = f y x

let update (jsRuntime: IJSInProcessRuntime) (logger: ILogger) message model =
    match message with
    | LogError (CredentialError error) ->
        match error with
        | ClientCredentialError.SaveError saveError ->
            match saveError with
            | SaveClientCredentialError.SaveIdError exception' ->
                logger.LogError(exception', "An unexpected error occured while saving the client id")
            | SaveClientCredentialError.SaveSecretError exception' ->
                logger.LogError(exception', "An unexpected error occured while saving the client secret")
        | ClientCredentialError.LoadError loadError ->
            match loadError with
            | LoadClientCredentialError.LoadIdError exception' ->
                logger.LogError(exception', "And unexpected error occured while loading the client id")
            | LoadClientCredentialError.LoadSecretError exception' ->
                logger.LogError(exception', "And unexpected error occured while loading the client secret")

        model, Command.none
    | SetClientCredential credential ->
        match credential with
        | Some value ->
            match value with
            | ClientCredential.Id id -> { model with clientId = Some id }, Command.none
            | ClientCredential.Secret secret -> { model with clientSecret = Some secret }, Command.none
        | None -> model, Command.none
    | SaveClientCredential credential ->
        //let save runtime (key, value, error : exn -> SaveClientCredentialError) = (LocalStorage.setItem key value runtime), error
        let (pair, error) =
            match credential with
            | ClientCredential.Id (ClientId id) -> ("clientId", id), SaveClientCredentialError.SaveIdError
            | ClientCredential.Secret (ClientSecret secret) ->
                ("clientSecret", secret), SaveClientCredentialError.SaveSecretError

        let save (key, value) =
            LocalStorage.setItem key value jsRuntime

        let logError =
            error
            >> SaveError
            >> Error.CredentialError
            >> LogError

        model, Command.OfFunc.attempt save pair logError

let view (model: Model) dispatch =
    concat {
        comp<PageTitle> { "Application Registration" }

        main {

            Html.form {
                on.submit (fun _ -> Console.WriteLine "submitted")
                ``class`` "p-8 flex flex-col gap-2"

                let dispatchSave = SaveClientCredential >> dispatch
                let idValue = 
                  model.clientId
                    |> Option.map (function
                        | ClientId id -> id)

                ecomp<Components.OutlinedInput, _, _>
                    { value = idValue
                      label = "Client Id"
                      id = "clientId"
                      placeholder = None
                      assistiveText = Components.AssistiveText.None }
                    (Option.iter (
                        string
                        >> ClientId
                        >> ClientCredential.Id
                        >> dispatchSave
                    )) {
                    ``type`` "text"
                }

                let secretValue =
                    model.clientSecret
                    |> Option.map (function
                        | ClientSecret secret -> secret)

                ecomp<Components.OutlinedInput, _, _>
                    { value = secretValue
                      label = "Client Secret"
                      id = "clientSecret"
                      placeholder = None
                      assistiveText = Components.AssistiveText.None }
                    (Option.iter (
                        string
                        >> ClientSecret
                        >> ClientCredential.Secret
                        >> SaveClientCredential
                        >> dispatch
                    )) {
                    ``type`` "text"
                }

                div {
                    ``class`` "flex justify-end"

                    let svgString =
                        """
                <svg class="h-4.5 w-4.5" viewBox="0 0 24 24" fill="#000000">
                    <path d="M0 0h24v24H0z" fill="none"/>
                    <path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/>
                </svg>"""

                    let icon = rawHtml svgString

                    ecomp<Components.FilledButton, _, _> { label = "Submit"; icon = Some icon } (fun _ -> ()) {
                        attr.``type`` "submit"
                    }
                }
            }

        }
    }


let program (jsRuntime: IJSRuntime) logger authorizationCode =
    let runtime = jsRuntime :?> IJSInProcessRuntime
    let update = update runtime logger
    let loadClientCredential = runtime |> flip LocalStorage.getItem

    let loadClientId () =
        loadClientCredential "clientId"
        |> Option.map ClientId

    let loadClientSecret () =
        loadClientCredential "clientSecret"
        |> Option.map ClientSecret

    let credentialToMessage credential =
        Option.map credential >> SetClientCredential

    let idToMessage = credentialToMessage Id

    let logLoadError = LoadError >> CredentialError >> LogError

    let idErrorToMessage = LoadIdError >> logLoadError

    let secretToMessage = credentialToMessage Secret

    let secretErrorToMessage = LoadSecretError >> logLoadError

    let startCommand =
        Command.batch [ Command.OfFunc.either loadClientId () idToMessage idErrorToMessage
                        Command.OfFunc.either loadClientSecret () secretToMessage secretErrorToMessage ]

    Program.mkProgram (fun _ -> initializeModel authorizationCode, startCommand) update view

let indexOf (character: char) (string: char ReadOnlySpan) =
    match string.IndexOf(character) with
    | -1 -> None
    | value -> Some value



type App() =
    inherit ProgramComponent<Model, Message>()

    [<Inject>]
    member val Logger = Unchecked.defaultof<ILogger<App>> with get, set

    [<Inject>]
    member val NavigationManager = Unchecked.defaultof<NavigationManager> with get, set

    [<Parameter>]
    [<SupplyParameterFromQuery(Name = "code")>]
    member val AuthorizationCode = Unchecked.defaultof<string> with get, set

    override this.Program =

        let code =
            this.AuthorizationCode
            |> Option.ofObj
            |> Option.map AuthorizationCode


        program this.JSRuntime this.Logger code
