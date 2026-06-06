using System;
using System.Collections.Generic;
using System.Text;
using OtpNet;

namespace BLL.Service
{
    public class OtpService
    {
        
        public string GenerateSecretKey()
        {
            byte[] secretKey = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(secretKey);
        }
        public bool VerifyOtp(string userOtp, string base32Secret)
        {
            byte[] secretKey = Base32Encoding.ToBytes(base32Secret);
            var totp = new Totp(secretKey);

            return totp.VerifyTotp(
                totp: userOtp,
                timeStepMatched: out _,
                window: new VerificationWindow(previous: 1, future: 1)
            );
        }
    }
}
