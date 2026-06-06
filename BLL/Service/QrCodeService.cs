using QRCoder;


namespace BLL.Service
{
    public class QrCodeService
    {
        
        public string GenerateQrCodeBase64(
            string userEmail,
            string base32Secret,
            string appName = "MyApp")
        {
            
            string uri = $"otpauth://totp/{appName}:{userEmail}"
                        + $"?secret={base32Secret}&issuer={appName}";

            using var gen = new QRCodeGenerator();
            using var data = gen.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
            using var qr = new PngByteQRCode(data);

            byte[] bytes = qr.GetGraphic(10);
            return "data:image/png;base64,"
                   + Convert.ToBase64String(bytes);
        }
    }
}
