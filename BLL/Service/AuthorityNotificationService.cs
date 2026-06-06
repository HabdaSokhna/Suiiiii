using Database;
using Database.Domain;
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using FirebaseNotification = FirebaseAdmin.Messaging.Notification;

namespace BLL.Service
{
    public interface IAuthorityNotificationService
    {
        Task SendAsync(int authorityId, string type, int? reportId = null);
    }

    public class AuthorityNotificationService : IAuthorityNotificationService
    {
        private readonly Ai_Reports_Context _context;

        public AuthorityNotificationService(Ai_Reports_Context context)
        {
            _context = context;
        }

        public async Task SendAsync(int authorityId, string type, int? reportId = null)
        {
            var (title, message) = type switch
            {
                "NewReport" => ("New Report", "A new report has been received and needs your review."),
                "UpdateReport" => ("Report Update", "An existing report has been updated."),
                "PendingReport" => ("Pending Report Reminder", "A report has been pending for over an hour and requires action."),
                _ => ("Notification", "There is a new update.")
            };

           
            try
            {
                _context.TbAuthorityNotification.Add(new AuthorityNotification
                {
                    Authority_ID = authorityId,
                    Title = title,
                    Message = message,
                    Type = type,
                    Report_ID = reportId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(2)
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DB Error: {ex.Message}");
            }

            // 2. Firebase Push
            try
            {
                var login = await _context.TbAuthority_Login
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.Authority_ID == authorityId);

                if (login != null && !string.IsNullOrEmpty(login.DeviceToken))
                {
                    var fcmMessage = new Message
                    {
                        Token = login.DeviceToken,
                        Notification = new FirebaseNotification
                        {
                            Title = title,
                            Body = message
                        }
                    };

                    var response = await FirebaseMessaging.DefaultInstance.SendAsync(fcmMessage);
                    Console.WriteLine($"✅ Firebase Sent: {response}");
                }
                else
                {
                    Console.WriteLine("⚠️ No DeviceToken for Authority.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Firebase Error: {ex.Message}");
            }
        }
    }
}