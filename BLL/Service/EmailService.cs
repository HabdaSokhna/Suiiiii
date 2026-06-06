using System;
using System.Collections.Generic;
using System.Text;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace BLL.Service
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOtpAsync(string toEmail, string otpCode)
        {
            var settings = _config.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(settings["Email"]));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "كود التحقق الخاص بك";
            message.Body = new TextPart("html")
            {
                Text = $@"
                <h2>كود التحقق</h2>
                <p>الكود الخاص بك هو:</p>
                <h1 style='color:blue;letter-spacing:8px'>{otpCode}</h1>
                <p>صالح لمدة 5 دقائق فقط</p>"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(settings["Host"], int.Parse(settings["Port"]), false);
            await smtp.AuthenticateAsync(settings["Email"], settings["Password"]);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}
