module PkPass.PassKit.Barcode

open System
open QRCoder
open Images

let createQrCode (value : string) =
    use generator = new QRCodeGenerator()
    use data = generator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q)
    use code = new PngByteQRCode(data)
    code.GetGraphic(100)
    |> Convert.ToBase64String
    |> Image.Base64
    