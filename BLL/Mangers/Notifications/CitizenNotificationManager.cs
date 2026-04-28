using CURD;
using Database;
using Microsoft.Extensions.Logging;
using FirebaseAdmin.Messaging;
using System;
using System.Threading.Tasks;

namespace BLL.Managers.Notifications
{
    public interface ICitizenNotificationManager
    {
        Task<bool> FillAndSendAsync(int citizenId, string type, string? status = null);
    }

    public class CitizenNotificationManager : ICitizenNotificationManager
    {
        private readonly Ai_Reports_Context _context;
        private readonly ICitizenRepository _citizenRepo;
        private readonly ILogger<CitizenNotificationManager> _logger;

        public CitizenNotificationManager(
            Ai_Reports_Context context,
            ICitizenRepository citizenRepo,
            ILogger<CitizenNotificationManager> logger)
        {
            _context = context;
            _citizenRepo = citizenRepo;
            _logger = logger;
        }

        public async Task<bool> FillAndSendAsync(int citizenId, string type, string? status = null)
        {
            try
            {
                var (title, messageBody) = type switch
                {
                    "Login" => ("تنبيه أمان", "تم تسجيل دخول جديد إلى حسابك."),
                    "Register" => ("مرحباً بك", "تم إنشاء حسابك بنجاح في نظام SIRS."),
                    "CreateReport" => ("تأكيد استلام بلاغ", "تم استلام بلاغك بنجاح وهو قيد المراجعة."),
                    "StatusUpdate" => ("تحديث بلاغ", $"تم تغيير حالة بلاغك إلى: {status}"),
                    _ => ("إشعار جديد", "يوجد تحديث جديد في حسابك.")
                };

                // ✅ جيب الـ token قبل ما تعمل أي حاجة تانية
                var deviceToken = await _citizenRepo.GetTokenByIdAsync(citizenId);

                // ✅ حفظ في الداتابيز
                var notification = new Database.Domain.Notification
                {
                    Citizen_ID = citizenId,
                    Title = title,
                    Message = messageBody,
                    Type = type,
                    CreatedAt = DateTime.UtcNow.AddHours(2),
                    IsRead = false
                };

                _context.TbNotification.Add(notification);
                await _context.SaveChangesAsync();

                // ✅ بعت الـ Firebase بس لو الـ token صح
                if (!string.IsNullOrEmpty(deviceToken) && deviceToken.Length > 100)
                {
                    try
                    {
                        var fcmMessage = new Message()
                        {
                            Token = deviceToken,
                            Notification = new FirebaseAdmin.Messaging.Notification()
                            {
                                Title = title,
                                Body = messageBody
                            }
                        };

                        string response = await FirebaseMessaging.DefaultInstance.SendAsync(fcmMessage);
                        _logger.LogInformation($"✅ Firebase Success: {response}");
                    }
                    catch (FirebaseMessagingException ex)
                    {
                        // مش بيعمل throw — بس بيلوج
                        _logger.LogWarning($"⚠️ Firebase Error: {ex.Message}");
                    }
                }
                else
                {
                    _logger.LogWarning($"⚠️ Invalid or missing DeviceToken for Citizen {citizenId}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Notification Error: {ex.Message}");
                return false;
            }
        }
    }
}