using System;
using System.Collections.Generic;

namespace BLL.Service
{
    public class OtpStore
    {
        // للـ OTP Codes
        private readonly Dictionary<string, (string Code, DateTime Expiry)> _store = new();

        // للـ Reset Password Tokens
        private readonly Dictionary<string, string> _tokens = new();

        // ✅ حفظ OTP
        public void Save(string email, string code)
        {
            _store[email] = (code, DateTime.UtcNow.AddMinutes(5));
        }

        // ✅ التحقق من OTP
        public bool Verify(string email, string code)
        {
            if (!_store.TryGetValue(email, out var stored))
                return false;

            if (DateTime.UtcNow > stored.Expiry)
            {
                _store.Remove(email);
                return false;
            }

            if (stored.Code != code)
                return false;

            _store.Remove(email);
            return true;
        }

        // ✅ حفظ Reset Token بعد التحقق من OTP
        public void SaveToken(string key, string token)
            => _tokens[key] = token;

        // ✅ جيب الـ Reset Token
        public string? GetToken(string key)
            => _tokens.TryGetValue(key, out var token) ? token : null;

        // ✅ امسح الـ Reset Token بعد الاستخدام
        public void RemoveToken(string key)
            => _tokens.Remove(key);
    }
}