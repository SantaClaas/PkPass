module PkPass.PassKit.Images


//TODO support loading from url or other sources. Might add additional dataUrl type to shift responsibility of knowing file type from consumer to producer
type Image = Base64 of string
type Icon = Icon of Image
type BackgroundImage = BackgroundImage of Image
type Logo = Logo of Image
type Thumbnail = Thumbnail of Image

// Pass package is the loaded contents of a zip pass archive folder
// Images supported by pass type

type StripImage = StripImage of Image
type FooterImage = FooterImage of Image

/// <summary>
/// Describes the images common in all passes
/// </summary>
type CommonImages = CommonImages of Logo * Icon

[<RequireQualifiedAccess>]
type EventTicketImageOption =
    // https://developer.apple.com/library/archive/documentation/UserExperience/Conceptual/PassKit_PG/Creating.html#//apple_ref/doc/uid/TP40012195-CH4-SW4
    // If you specify a strip image, do not specify a background image or a thumbnail
    | StripImage of StripImage
    // background and or thumbnail might be optional
    | Other of BackgroundImage * Thumbnail
    
type BoardingPassImages = BoardingPassImages of CommonImages * FooterImage
type CouponImages = CouponImages of CommonImages * StripImage
type EventTicketImages = EventTicketImages of CommonImages * EventTicketImageOption
type GenericPassImages = GenericPassImages of CommonImages * Thumbnail
type StoreCardImages = StoreCardImages of CommonImages * StripImage 