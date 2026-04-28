using Database;
using Database.Domain;
using Microsoft.AspNetCore.Identity;
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Aliases لمنع التضارب بين موديل الداتابيز وموديل فايربيز
using DbNotification = Database.Domain.Notification;
using FirebaseNotification = FirebaseAdmin.Messaging.Notification;

namespace BLL.Service
{
    public interface ISystemNotificationService
    {
        Task SendNotificationAsync(int citizenId, string type);
    }

    public class NotificationService : ISystemNotificationService
    {
        private readonly Ai_Reports_Context _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationService(Ai_Reports_Context context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task SendNotificationAsync(int citizenId, string type)
        {
            // 1. تحديد نصوص الإشعارات بناءً على النوع (Type)
            var (title, message) = type switch
            {
                "ChangeEmail" => ("تحديث الحساب", "تم تغيير البريد الإلكتروني بنجاح."),
                "ChangePassword" => ("أمان الحساب", "تم تحديث كلمة المرور الخاصة بك بنجاح."),
                "UploadPhoto" => ("الملف الشخصي", "تم تحديث صورتك الشخصية بنجاح."),
                "CreateReport" => ("تأكيد استلام بلاغ", "تم استلام بلاغك بنجاح وهو قيد المراجعة."),
                _ => ("إشعار من النظام", "يوجد تحديث جديد في حسابك.")
            };

            // 2. حفظ الإشعار في قاعدة البيانات (جدول التنبيهات للأرشيف الداخلي)
            var dbNotif = new DbNotification
            {
                Citizen_ID = citizenId,
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow.AddHours(2), // توقيت مصر
                IsRead = false
            };

            try
            {
                _context.TbNotification.Add(dbNotif);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Database Error: {ex.Message}");
            }

            // 3. إرسال الإشعار اللحظي (Push Notification) عبر Firebase
            try
            {
                // نجلب بيانات المواطن للتأكد من وجوده والحصول على الـ ApplicationUserId (string)
                var citizen = await _context.TbCitizen
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Citizen_ID == citizenId);

                if (citizen != null && !string.IsNullOrEmpty(citizen.ApplicationUserId))
                {
                    // نجلب اليوزر من Identity باستخدام الـ ApplicationUserId
                    var user = await _userManager.FindByIdAsync(citizen.ApplicationUserId);

                    // التأكد من أن اليوزر موجود وعنده DeviceToken مسجل
                    if (user != null && !string.IsNullOrEmpty(user.DeviceToken))
                    {
                        var fcmMessage = new Message()
                        {
                            Token = user.DeviceToken,
                            Notification = new FirebaseNotification()
                            {
                                Title = title,
                                Body = message
                            },
                            Webpush = new WebpushConfig
                            {
                                // الرابط اللي هيفتحه لما يضغط على الإشعار
                                FcmOptions = new WebpushFcmOptions { Link = "https://localhost:7157" }
                            }
                        };

                        string response = await FirebaseMessaging.DefaultInstance.SendAsync(fcmMessage);
                        Console.WriteLine($"✅ Firebase Sent Successfully: {response}");
                    }
                    else
                    {
                        Console.WriteLine("⚠️ No DeviceToken found for this user in AspNetUsers.");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Citizen not found or ApplicationUserId is null.");
                }
            }
            catch (Exception ex)
            {
                // طباعة الخطأ في الـ Console فقط لضمان عدم توقف البرنامج
                Console.WriteLine($"❌ Firebase Cloud Messaging Error: {ex.Message}");
            }
        }
    }
}