namespace PassKit

/// <summary>
/// Information about a field.
/// These keys are used for all dictionaries that define a field.
/// </summary>
type Field =
    {
        /// <summary>
        /// Optional. Attributed value of the field.
        /// The value may contain HTML markup for links. Only the &lt;a&gt; tag and its href attribute are supported.
        /// For example, the following is key-value pair specifies a link with the text “Edit my profile”:
        /// "attributedValue": "&lt;a href='http://example.com/customers/123'&gt;Edit my profile&lt;/a&gt;"
        /// This key’s value overrides the text specified by the value key.
        /// Available in iOS 7.0.
        /// </summary>
        attributedValue: string option

        /// <summary>
        /// Optional. Format string for the alert text that is displayed when the pass is updated. The format string
        /// must contain the escape %@, which is replaced with the field’s new value. For example, “Gate changed to %@.”
        /// If you don’t specify a change message, the user isn’t notified when the field changes.
        /// </summary>
        changeMessage: string option

        /// <summary>
        /// Optional. Data detectors that are applied to the field’s value. Valid values are:
        /// PKDataDetectorTypePhoneNumber
        /// PKDataDetectorTypeLink
        /// PKDataDetectorTypeAddress
        /// PKDataDetectorTypeCalendarEvent
        /// The default value is all data detectors. Provide an empty array to use no data detectors.
        /// Data detectors are applied only to back fields.
        /// </summary>
        dataDetectorType: string array option

        /// <summary>
        /// Required. The key must be unique within the scope of the entire pass. For example, “departure-gate.”
        /// </summary>
        key: string

        /// <summary>
        /// Optional. Label text for the field.
        /// </summary>
        label: string option

        /// <summary>
        /// Optional. Alignment for the field’s contents. Must be one of the following values:
        /// PKTextAlignmentLeft
        /// PKTextAlignmentCenter
        /// PKTextAlignmentRight
        /// PKTextAlignmentNatural
        /// The default value is natural alignment, which aligns the text appropriately based on its script direction.
        /// This key is not allowed for primary fields or back fields.
        /// </summary>
        textAlignment: string option

        /// <summary>
        /// Required. Value of the field, for example, 42.
        /// </summary>
        value: string

        /// <summary>
        /// Style of date to display. Must be one of the styles: PKDateStyleNone, PKDateStyleShort, PKDateStyleMedium,
        /// PKDateStyleLong, PKDateStyleFull
        /// </summary>
        dateStyle: string option

        /// <summary>
        /// Optional. Always display the time and date in the given time zone, not in the user’s current time zone. The
        /// default value is false.
        /// The format for a date and time always requires a time zone, even if it will be ignored. For backward
        /// compatibility with iOS 6, provide an appropriate time zone, so that the information is displayed
        /// meaningfully even without ignoring time zones.
        /// This key does not affect how relevance is calculated.
        /// Available in iOS 7.0.
        /// </summary>
        ignoresTimeZone: bool option

        /// <summary>
        /// Optional. If true, the label’s value is displayed as a relative date; otherwise, it is displayed as an
        /// absolute date. The default value is false.
        /// This key does not affect how relevance is calculated.
        /// </summary>
        isRelative: bool option

        /// <summary>
        /// Style of time to display. Must be one of the styles: PKDateStyleNone, PKDateStyleShort, PKDateStyleMedium,
        /// PKDateStyleLong, PKDateStyleFull
        /// </summary>
        timeStyle: string option

        /// <summary>
        /// ISO 4217 currency code for the field’s value.
        /// </summary>
        currencyCode: string option

        /// <summary>
        /// Style of number to display. Must be one of the following values:
        /// PKNumberStyleDecimal
        /// PKNumberStylePercent
        /// PKNumberStyleScientific
        /// PKNumberStyleSpellOut
        /// Number styles have the same meaning as the Cocoa number formatter styles with corresponding names. For more
        /// information, see NSNumberFormatterStyle.
        /// </summary>
        numberStyle: string option
    }

namespace PassKit.LowerLevel

open PassKit

/// <summary>
/// Information about a location beacon. Available in iOS 7.0.
/// </summary>
type Beacon =
    {
        /// <summary>
        /// Optional. Major identifier of a Bluetooth Low Energy location beacon.
        /// </summary>
        major: uint16 option
        /// <summary>
        /// Optional. Minor identifier of a Bluetooth Low Energy location beacon.
        /// </summary>
        minor: uint16 option
        /// <summary>
        /// Required. Unique identifier of a Bluetooth Low Energy location beacon.
        /// </summary>
        proximityUuid: string
        /// <summary>
        /// Optional. Text displayed on the lock screen when the pass is currently relevant. For example, a description
        /// of the nearby location such as “Store nearby on 1st and Main.”
        /// </summary>
        relevantText: string
    }

type Location =
    {
        /// <summary>
        /// Optional. Altitude, in meters, of the location.
        /// </summary>
        altitude: double option
        /// <summary>
        /// Required. Latitude, in degrees, of the location.
        /// </summary>
        latitude: double
        /// <summary>
        /// Required. Longitude, in degrees, of the location.
        /// </summary>
        longitude: double
        /// <summary>
        /// Optional. Text displayed on the lock screen when the pass is currently relevant. For example, a description of
        /// the nearby location such as “Store nearby on 1st and Main.”
        /// </summary>
        relevantText: string option
    }

/// <summary>
/// Keys that define the structure of the pass.
/// These keys are used for all pass styles and partition the fields into the various parts of the pass.
/// </summary>
type PassStructure =
    {
        /// <summary>
        /// Optional. Additional fields to be displayed on the front of the pass.
        /// </summary>
        auxiliaryFields: Field array option
        /// <summary>
        /// Optional. Fields to be on the back of the pass.
        /// </summary>
        backFields: Field array option
        /// <summary>
        /// Optional. Fields to be displayed in the header on the front of the pass.
        /// Use header fields sparingly; unlike all other fields, they remain visible when a stack of passes are displayed.
        /// </summary>
        headerFields: Field array option
        /// <summary>
        /// Optional. Fields to be displayed prominently on the front of the pass.
        /// </summary>
        primaryFields: Field array option
        /// <summary>
        /// Optional. Fields to be displayed on the front of the pass.
        /// </summary>
        secondaryFields: Field array option
        /// <summary>
        /// Required for boarding passes; otherwise not allowed. Type of transit. Must be one of the following values:
        /// PKTransitTypeAir, PKTransitTypeBoat, PKTransitTypeBus, PKTransitTypeGeneric,PKTransitTypeTrain.
        /// </summary>
        transitType: string option
    }

/// <summary>
/// Information about a pass’s barcode.
/// </summary>
type Barcode =
    {
        /// <summary>
        /// Optional. Text displayed near the barcode. For example, a human-readable version of the barcode data in case the barcode doesn’t scan.
        /// </summary>
        altText: string option
        /// <summary>
        /// Required. Barcode format. For the barcode dictionary, you can use only the following values: PKBarcodeFormatQR,
        /// PKBarcodeFormatPDF417, or PKBarcodeFormatAztec. For dictionaries in the barcodes array, you may also use PKBarcodeFormatCode128.
        /// </summary>
        format: string
        /// <summary>
        /// Required. Message or payload to be displayed as a barcode.
        /// </summary>
        message: string
        /// <summary>
        /// Required. Text encoding that is used to convert the message from the string representation to a data
        /// representation to render the barcode. The value is typically iso-8859-1, but you may use another encoding that
        /// is supported by your barcode scanning infrastructure.
        /// </summary>
        messageEncoding: string
    }

/// <summary>
/// Information about the NFC payload passed to an Apple Pay terminal.
/// </summary>
type Nfc =
    {
        /// <summary>
        /// Required. The payload to be transmitted to the Apple Pay terminal. Must be 64 bytes or less. Messages longer
        /// than 64 bytes are truncated by the system.
        /// </summary>
        message: string
        /// <summary>
        /// Optional. The public encryption key used by the Value Added Services protocol. Use a Base64 encoded X.509
        /// SubjectPublicKeyInfo structure containing a ECDH public key for group P256.
        /// </summary>
        encryptionPublicKey: string option
    }

namespace PassKit.TopLevel

open System
open System.Collections.Generic
open System.IO
open System.Text
open Microsoft.FSharp.Core
open PassKit.LowerLevel
open PkPass.PassKit.Barcode
open PkPass.PassKit.Deserialization
open PkPass.PassKit.Field

/// <summary>
/// This record describes the JSON structure of the pass.json file and all the fields that can appear in it. It does not
/// describe a valid pass, just what can appear in the pass. An instance of this type will be passed to validation which
/// checks if the data is in the right format. This adheres to the rules of JSON more than to the rules of passkit pass
/// The structure and documentation is based upon
/// https://developer.apple.com/library/archive/documentation/UserExperience/Reference/PassKit_Bundle/Chapters/TopLevel.html
/// </summary>
type MaybeInvalidPass =
    {
        /// <summary>
        /// Required. Brief description of the pass, used by the iOS accessibility technologies.
        /// Don’t try to include all of the data on the pass in its description, just include enough detail to
        /// distinguish
        /// passes of the same type.
        /// </summary>
        description: string

        /// <summary>
        /// Required. Version of the file format. The value must be 1.
        /// </summary>
        formatVersion: int

        /// <summary>
        /// Required. Display name of the organization that originated and signed the pass.
        /// </summary>
        organizationName: string

        /// <summary>
        /// Required. Pass type identifier, as issued by Apple. The value must correspond with your signing certificate.
        /// </summary>
        passTypeIdentifier: string

        /// <summary>
        /// Required. Serial number that uniquely identifies the pass. No two passes with the same pass type identifier
        /// may have the same serial number.
        /// </summary>
        serialNumber: string

        /// <summary>
        /// Required. Team identifier of the organization that originated and signed the pass, as issued by Apple.
        /// </summary>
        teamIdentifier: string

        /// <summary>
        /// Optional. A URL to be passed to the associated app when launching it. The app receives this URL in the
        /// application:didFinishLaunchingWithOptions: and application:openURL:options: methods of its app delegate. If
        /// this key is present, the associatedStoreIdentifiers key must also be present.
        /// </summary>
        appLaunchUrl: string option

        /// <summary>
        /// Optional. A list of iTunes Store item identifiers for the associated apps. Only one item in the list is
        /// used—the first item identifier for an app compatible with the current device. If the app is not installed,
        /// the link opens the App Store and shows the app. If the app is already installed, the link launches the app.
        /// </summary>
        associatedStoreIdentifiers: int array option

        /// <summary>
        /// Optional. Custom information for companion apps. This data is not displayed to the user. For example, a pass
        /// for a cafe could include information about the user’s favorite drink and sandwich in a machine-readable form
        /// for the companion app to read, making it easy to place an order for “the usual” from the app.
        /// Available in iOS 7.0.
        /// </summary>
        userInfo: IReadOnlyDictionary<string, obj> option

        /// <summary>
        /// Optional. Date and time when the pass expires.
        /// The value must be a complete date with hours and minutes, and may optionally include seconds.
        /// Available in iOS 7.0.
        /// </summary>
        expirationDate: string option

        /// <summary>
        /// Optional. Indicates that the pass is void—for example, a one time use coupon that has been redeemed.
        /// The default value is false.
        /// Available in iOS 7.0.
        /// </summary>
        voided: bool option

        /// <summary>
        /// Optional. Beacons marking locations where the pass is relevant.
        /// For these dictionaries’ keys, see Beacon Dictionary Keys
        /// Available in iOS 7.0.
        /// </summary>
        beacons: Beacon array option

        /// <summary>
        /// Optional. Locations where the pass is relevant. For example, the location of your store. For these
        /// dictionaries’ keys, see Location Dictionary Keys.
        /// </summary>
        locations: Location array option

        /// <summary>
        /// Optional. Maximum distance in meters from a relevant latitude and longitude that the pass is relevant. This
        /// number is compared to the pass’s default distance and the smaller value is used. Available in iOS 7.0.
        /// </summary>
        maxDistance: int option

        /// <summary>
        /// Recommended for event tickets and boarding passes; otherwise optional. Date and time when the pass becomes
        /// relevant. For example, the start time of a movie.
        /// The value must be a complete date with hours and minutes, and may optionally include seconds.
        /// </summary>
        relevantDate: string option

        /// <summary>
        /// Information specific to a boarding pass.
        /// </summary>
        boardingPass: PassStructure option

        /// <summary>
        /// Information specific to a coupon.
        /// </summary>
        coupon: PassStructure option

        /// <summary>
        /// Information specific to an event ticket.
        /// </summary>
        eventTicket: PassStructure option

        /// <summary>
        /// Information specific to a generic pass.
        /// </summary>
        generic: PassStructure option

        /// <summary>
        /// Information specific to a store card.
        /// </summary>
        storeCard: PassStructure option

        /// <summary>
        /// Optional. Information specific to the pass’s barcode. For this dictionary’s keys, see Barcode Dictionary
        /// Keys.
        /// Note:Deprecated in iOS 9.0 and later; use barcodes instead.
        /// </summary>
        barcode: Barcode option

        /// <summary>
        /// Optional. Information specific to the pass’s barcode. The system uses the first valid barcode dictionary in
        /// the array. Additional dictionaries can be added as fallbacks. For this dictionary’s keys, see Barcode
        /// Dictionary Keys.
        /// Note: Available only in iOS 9.0 and later.
        /// </summary>
        barcodes: PassKit.LowerLevel.Barcode array option

        /// <summary>
        /// Optional. Background color of the pass, specified as an CSS-style RGB triple. For example, rgb(23, 187, 82).
        /// </summary>
        backgroundColor: string option

        /// <summary>
        /// Optional. Foreground color of the pass, specified as a CSS-style RGB triple. For example, rgb(10
        /// </summary>
        foregroundColor: string option

        /// <summary>
        /// Optional for event tickets and boarding passes; otherwise not allowed. Identifier used to group related
        /// passes. If a grouping identifier is specified, passes with the same style, pass type identifier, and
        /// grouping identifier are displayed as a group. Otherwise, passes are grouped automatically.
        /// Use this to group passes that are tightly related, such as the boarding passes for different connections of
        /// the same trip.
        /// Available in iOS 7.0.
        /// </summary>
        groupingIdentifier: string option

        /// <summary>
        /// Optional. Color of the label text, specified as a CSS-style RGB triple. For example, rgb(255, 255, 255).
        /// If omitted, the label color is determined automatically.
        /// </summary>
        labelColor: string option

        /// <summary>
        /// Optional. Text displayed next to the logo on the pass.
        /// </summary>
        logoText: string option

        /// <summary>
        /// Optional. If true, the strip image is displayed without a shine effect. The default value prior to iOS 7.0
        /// is false.
        /// In iOS 7.0, a shine effect is never applied, and this key is deprecated.
        /// </summary>
        suppressStripShine: bool option

        /// <summary>
        /// The authentication token to use with the web service. The token must be 16 characters or longer.
        /// </summary>
        authenticationToken: string option

        /// <summary>
        /// The URL of a web service that conforms to the API described in PassKit Web Service Reference.
        /// The web service must use the HTTPS protocol; the leading https:// is included in the value of this key.
        /// On devices configured for development, there is UI in Settings to allow HTTP web services.
        /// </summary>
        webServiceUrl: string option

        /// <summary>
        /// Optional. Information used for Value Added Service Protocol transactions. For this dictionary’s keys, see NFC Dictionary Keys.
        /// Available in iOS 9.0.
        /// </summary>
        nfc: Nfc option
    }


module Validation =
    open System.Text.Json
    open System.Text.Json.Serialization

    /// <summary>
    /// Describes the errors that can occur during pass deserialization
    /// </summary>
    type DeserializationError =
        /// <summary>
        /// The provided stream was null
        /// </summary>
        | NoStream of ArgumentNullException
        /// <summary>
        /// The JSON is invalid, the pass type is not compatible with the JSON, or there is remaining data in the Stream.
        /// </summary>
        | DataRemaining of JsonException
        /// <summary>
        /// There is no compatible JsonConverter for the pass type or its serializable members.
        /// </summary>
        | NotSupported of NotSupportedException
        /// <summary>
        /// An unexpected exception was thrown during deserialization
        /// </summary>
        | UnexpectedError of Exception

    /// <summary>
    /// Deserializes an UTF-8 JSON stream to an invalid pass that contains all the fields of the pass JSON but might not
    /// be valid
    /// </summary>
    /// <param name="passJsonFile"></param>
    let deserialize (passJsonFile: Stream) : Result<MaybeInvalidPass, DeserializationError> =
        let options = JsonSerializerOptions()
        options.Converters.Add(JsonFSharpConverter())

        try
            let passFields: MaybeInvalidPass = JsonSerializer.Deserialize(passJsonFile, options)
            Ok passFields
        with
        | :? ArgumentNullException as exception' -> exception' |> NoStream |> Error
        | :? JsonException as exception' -> exception' |> DataRemaining |> Error
        | :? NotSupportedException as exception' -> exception' |> NotSupported |> Error
        | exception' -> exception' |> UnexpectedError |> Error

[<Measure>]
type FormatVersion

// Might be overdoing it with types here
type PassTypeIdentifier = PassTypeIdentifier of string
type Description = Description of LocalizableString
type OrganizationName = OrganizationName of LocalizableString
type SerialNumber = SerialNumber of string
type TeamIdentifier = TeamIdentifier of string
type AppLaunchUrl = AppLaunchUrl of Uri

[<Measure>]
type StoreIdentifier

type AssociatedApp =
    {
        /// <summary>
        /// Optional. A URL to be passed to the associated app when launching it.
        /// The app receives this URL in the application:didFinishLaunchingWithOptions: and application:openURL:options: methods of its app delegate.
        /// If this key is present, the associatedStoreIdentifiers key must also be present.
        /// </summary>
        launchUrl: Uri option
        /// <summary>
        /// Optional. A list of iTunes Store item identifiers for the associated apps.
        /// Only one item in the list is used—the first item identifier for an app compatible with the current device. If the app is not installed, the link opens the App Store and shows the app. If the app is already installed, the link launches the app.
        /// </summary>
        storeIdentifiers: int<StoreIdentifier> array
    }


type AssociatedAppError =
    /// <summary>
    /// Could not create uri from launch url
    /// </summary>
    | InvalidLaunchUrl of string
    /// <summary>
    /// If the app launch url is present the store identifiers must be present too
    /// </summary>
    | StoreIdentifiersMissing

type CompanionApp = CompanionApp of userInfo: IReadOnlyDictionary<string, obj>

type ExpirationError = CouldNotParseExpirationDate of invalidDateString: string

type Expiration =
    {
        /// <summary>
        /// Optional. Date and time when the pass expires.
        /// The value must be a complete date with hours and minutes, and may optionally include seconds.
        /// Available in iOS 7.0.
        /// </summary>
        date: DateTimeOffset option

        /// <summary>
        /// Optional. Indicates that the pass is void—for example, a one time use coupon that has been redeemed. The default value is false.
        /// Available in iOS 7.0.
        /// </summary>
        isVoided: bool option
    }

type RelevanceError = CouldNotParseRelevanceDate of invalidDateString: string

type Relevance =
    { beacons: Beacon array option
      locations: Location array option
      maxDistance: int option
      relevantDate: DateTimeOffset option }

type PassStyle =
    | BoardingPass
    | Coupon
    | EventTicket
    | Generic
    | StoreCard

type GroupingIdentifier = GroupingIdentifier of string

type VisualAppearanceError = UnexpectedValue of string

type VisualAppearance =
    // We only support iOS 9 upwards passes
    { barcodes: Barcode array option
      backgroundColor: CssColor option
      foregroundColor: CssColor option
      groupingIdentifier: GroupingIdentifier option
      labelColor: CssColor option
      logoText: LocalizableString option
      isStripShineSuppressed: bool option }

type AuthenticationToken = AuthenticationToken of string

type WebServiceError =
    /// <summary>
    /// Web service authentication tokens must be 16 characters or longer
    /// </summary>
    | AuthenticationTokenTooShort
    /// <summary>
    /// Uri scheme has to be HTTPS (it might be HTTP or something else which is unlikely)
    /// </summary>
    | UrlIsNotHttps
    | CouldNotParseWebServiceUrl
    | NoWebServiceUrl
    | NoWebService

type WebService =
    { authenticationToken: AuthenticationToken
      url: Uri }

type EncryptionPublicKey = EncryptionPublicKey of string
type NfcError = NoMessage

type Nfc =
    { message: string
      encryptionPublicKey: EncryptionPublicKey option }

type ValidPass =
    { description: Description
      formatVersion: int<FormatVersion>
      organizationName: OrganizationName
      passTypeIdentifier: PassTypeIdentifier
      serialNumber: SerialNumber
      teamIdentifier: TeamIdentifier
      associatedApp: AssociatedApp option
      companionApp: CompanionApp option
      expiration: Expiration option
      relevance: Relevance option
      style: PassStyle
      visualAppearance: VisualAppearance
      webService: WebService
      nfc: Nfc option }

module PassValidation =
    type ValidationError =
        /// <summary>
        /// The format version of the field has a version number that is not 1. The only supported version is 1.
        /// At the time of writing there is only version 1. There is still the possibility of new versions having been
        /// released. In that case this error can be seen as a not yet supported error and support needs to be added if
        /// support for other versions is required.
        /// </summary>
        | InvalidVersion of invalidVersionNumber: int
        | InvalidAssociatedApp of AssociatedAppError
        | ExpirationError of ExpirationError
        | RelevanceError of RelevanceError
        /// <summary>
        /// No pass style was provided can not determine the type / style of pass (boarding pass, coupon, generic, ...)
        /// </summary>
        | NoPassStyle
        /// <summary>
        /// Multiple pass styles were provided. Can not determine pass style. Only one pass style is allowed.
        /// </summary>
        | MultiplePassStyles
        | VisualAppearanceError of VisualAppearanceError
        | VisualAppearanceErrors of VisualAppearanceError array
        | WebServiceError of WebServiceError
        | DescriptionMissing
        | FormatVersionMissing


    /// <summary>
    /// Creates a valid pass from an invalid pass. Fails if possibly invalid pass is not valid
    /// </summary>
    /// <param name="invalidPass"></param>
    let validate (invalidPass: MaybeInvalidPass) : Result<ValidPass, ValidationError> =
        match invalidPass with
        | { formatVersion = version } when version <> 1 -> version |> InvalidVersion |> Error
        | _ ->
            let associatedAppResult =
                match invalidPass.appLaunchUrl, invalidPass.associatedStoreIdentifiers with
                | None, None -> Ok None
                | Some url, Some identifiers ->
                    match Uri.TryCreate(url, UriKind.Absolute) with
                    | true, uri ->
                        { launchUrl = Some uri
                          storeIdentifiers = identifiers |> Array.map LanguagePrimitives.Int32WithMeasure }
                        |> Some
                        |> Ok
                    | false, _ -> url |> InvalidLaunchUrl |> Error
                | Some _, None -> AssociatedAppError.StoreIdentifiersMissing |> Error
                // Not sure if this is logically valid state but the docs don't say it isn't but also don't say it is
                | None, Some identifiers ->
                    { launchUrl = None
                      storeIdentifiers = identifiers |> Array.map LanguagePrimitives.Int32WithMeasure }
                    |> Some
                    |> Ok

            let expiration =
                match invalidPass.expirationDate with
                | Some dateString ->
                    match DateTimeOffset.TryParse dateString with
                    | true, date ->
                        { date = Some date
                          isVoided = invalidPass.voided }
                        |> Some
                        |> Ok
                    | false, _ -> dateString |> CouldNotParseExpirationDate |> Error
                | None -> Ok None

            let relevance =
                match invalidPass.beacons, invalidPass.locations, invalidPass.maxDistance, invalidPass.relevantDate with
                | None, None, None, None -> Ok None
                | beacons, locations, maxDistance, relevantDate ->
                    match relevantDate with
                    | Some dateString ->
                        match DateTimeOffset.TryParse dateString with
                        | true, date ->
                            { relevantDate = Some date
                              beacons = beacons
                              locations = locations
                              maxDistance = maxDistance }
                            |> Some
                            |> Ok
                        | false, _ -> CouldNotParseRelevanceDate dateString |> Error
                    | None ->
                        { relevantDate = None
                          beacons = beacons
                          locations = locations
                          maxDistance = maxDistance }
                        |> Some
                        |> Ok

            //TODO add fields
            let style =
                match
                    invalidPass.boardingPass,
                    invalidPass.coupon,
                    invalidPass.eventTicket,
                    invalidPass.generic,
                    invalidPass.storeCard
                with
                | Some boardingPass, None, None, None, None -> BoardingPass |> Ok
                | None, Some coupon, None, None, None -> Coupon |> Ok
                | None, None, Some eventTicket, None, None -> EventTicket |> Ok
                | None, None, None, Some generic, None -> Generic |> Ok
                | None, None, None, None, Some storeCard -> StoreCard |> Ok
                | None, None, None, None, None -> NoPassStyle |> Error
                | _ -> MultiplePassStyles |> Error


            let barcodes =
                invalidPass.barcodes
                |> Option.map (fun codes ->
                    codes
                    |> Array.map (fun code ->
                        match code.format with
                        | "PKBarcodeFormatQR" -> Ok BarcodeFormat.Qr
                        | "PKBarcodeFormatPDF417" -> Ok BarcodeFormat.Pdf417
                        | "PKBarcodeFormatAztec" -> Ok BarcodeFormat.Aztec
                        | "PKBarcodeFormatCode128" -> Ok BarcodeFormat.Code128
                        | unknown -> unknown |> UnexpectedValue |> Error
                        |> Result.map (fun format ->
                            { format = format
                              message = code.message
                              messageEncoding = Encoding.GetEncoding code.messageEncoding
                              alternateText = Option.map AlternateText code.altText })))

            // We assume string is valid CSS color since I don't want to write a CSS color string validator
            let backgroundColor = invalidPass.backgroundColor |> Option.map CssColor
            let foregroundColor = invalidPass.foregroundColor |> Option.map CssColor

            let groupingIdentifier =
                invalidPass.groupingIdentifier |> Option.map GroupingIdentifier

            let labelColor = invalidPass.labelColor |> Option.map CssColor
            let logoText = invalidPass.logoText |> Option.map LocalizableString

            associatedAppResult
            |> Result.mapError InvalidAssociatedApp
            |> Result.bind (fun associatedApp ->
                expiration
                |> Result.mapError ExpirationError
                |> Result.map (fun expiration ->
                    {| expiration = expiration
                       associatedApp = associatedApp |}))
            |> Result.bind (fun state ->
                relevance
                |> Result.mapError RelevanceError
                |> Result.map (fun relevance -> {| state with relevance = relevance |}))
            |> Result.bind (fun state -> style |> Result.map (fun style -> {| state with style = style |}))
            |> Result.bind (fun state ->
                match barcodes with
                | None ->
                    Ok
                        {| state with
                            visualAppearance =
                                { barcodes = None
                                  backgroundColor = backgroundColor
                                  foregroundColor = foregroundColor
                                  groupingIdentifier = groupingIdentifier
                                  labelColor = labelColor
                                  logoText = logoText
                                  isStripShineSuppressed = invalidPass.suppressStripShine } |}
                | Some codeResults ->
                    match Result.partition codeResults with
                    | barcodes, [] ->
                        Ok
                            {| state with
                                visualAppearance =
                                    { barcodes = barcodes |> List.toArray |> Some
                                      backgroundColor = backgroundColor
                                      foregroundColor = foregroundColor
                                      groupingIdentifier = groupingIdentifier
                                      labelColor = labelColor
                                      logoText = logoText
                                      isStripShineSuppressed = invalidPass.suppressStripShine } |}

                    | _, errors -> errors |> List.toArray |> VisualAppearanceErrors |> Error)
            |> Result.bind (fun state ->
                match invalidPass.authenticationToken, invalidPass.webServiceUrl with
                | None, None -> WebServiceError.NoWebService |> WebServiceError |> Error
                | Some token, _ when token |> String.length < 16 ->
                    WebServiceError.AuthenticationTokenTooShort |> WebServiceError |> Error
                | Some _, None -> WebServiceError.NoWebServiceUrl |> WebServiceError |> Error
                | Some token, Some webServiceUrl ->
                    match Uri.TryCreate(webServiceUrl, UriKind.Absolute) with
                    | false, _ -> WebServiceError.CouldNotParseWebServiceUrl |> WebServiceError |> Error
                    | true, url when url.Scheme <> Uri.UriSchemeHttps ->
                        WebServiceError.UrlIsNotHttps |> WebServiceError |> Error
                    | true, url ->
                        Ok
                            { description = LocalizableString invalidPass.description |> Description
                              formatVersion = 1<FormatVersion>
                              organizationName = LocalizableString invalidPass.description |> OrganizationName
                              passTypeIdentifier = PassTypeIdentifier invalidPass.passTypeIdentifier
                              serialNumber = SerialNumber invalidPass.serialNumber
                              teamIdentifier = TeamIdentifier invalidPass.teamIdentifier
                              associatedApp = state.associatedApp
                              companionApp = invalidPass.userInfo |> Option.map CompanionApp
                              expiration = state.expiration
                              relevance = state.relevance
                              style = state.style
                              visualAppearance = state.visualAppearance
                              webService =
                                { authenticationToken = AuthenticationToken token
                                  url = url }
                              nfc =
                                invalidPass.nfc
                                |> Option.map (fun nfc ->
                                    { message = nfc.message
                                      encryptionPublicKey = nfc.encryptionPublicKey |> Option.map EncryptionPublicKey }) }
                //Ok state //{| state with webService = { authenticationToken = AuthenticationToken token; url = url } |}
                | None, Some _ -> WebServiceError.UrlIsNotHttps |> WebServiceError |> Error)