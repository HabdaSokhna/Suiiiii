using Database.Domain;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SIRS_API.Services
{
    public interface ITokenService
    {
        string GenerateToken(ApplicationUser user, IList<string> roles);
    }

    public class CreateToken : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly SymmetricSecurityKey _key;

        public CreateToken(IConfiguration config)
        {
            _config = config;

            // جلب المفتاح من الإعدادات والتأكد من وجوده
            var secretKey = _config["JWT:Key"]
                ?? throw new InvalidOperationException("🚨 JWT Key is missing in appsettings.json!");

            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        }

        public string GenerateToken(ApplicationUser user, IList<string> roles)
        {
            // 1. تعريف الـ Claims بالأسماء القياسية (Standard Names)
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("uid", user.Id) // معرف المستخدم مخصص
            };

            // 2. إضافة الأدوار (Roles) بأسماء صافية تتوافق مع إعدادات Program.cs
            foreach (var role in roles)
            {
                claims.Add(new Claim("role", role));
            }

            // 3. إعداد بيانات التوقيع (Signing Credentials)
            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256Signature);

            // 4. وصف التوكن (Token Descriptor)
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(1), // صلاحية ليوم واحد
                SigningCredentials = creds,
                Issuer = _config["JWT:Issuer"],
                Audience = _config["JWT:Audience"]
            };

            // 5. إنشاء التوكن وتحويله لنص (String)
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}