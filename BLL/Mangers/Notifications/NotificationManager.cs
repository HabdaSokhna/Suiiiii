using BLL.DTO.Notification;
using Database;
using Microsoft.EntityFrameworkCore;

namespace BLL.Managers.Notification
{
    public interface INotificationManager
    {
        Task<List<NotificationResponse_Dto>> GetMyHistoryAsync(string userId);
        Task<bool> MarkAsReadAsync(int notificationId);
    }

    public class NotificationManager : INotificationManager
    {
        private readonly Ai_Reports_Context _context;

        public NotificationManager(Ai_Reports_Context context)
        {
            _context = context;
        }

        public async Task<List<NotificationResponse_Dto>> GetMyHistoryAsync(string userId)
        {
            try
            {
                var citizenId = await _context.TbCitizen
                    .Where(c => c.ApplicationUserId == userId)
                    .Select(c => c.Citizen_ID)
                    .FirstOrDefaultAsync();

                if (citizenId == 0) return new List<NotificationResponse_Dto>(); // ✅ مش null

                return await _context.TbNotification
                    .Where(n => n.Citizen_ID == citizenId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(20)
                    .Select(n => new NotificationResponse_Dto
                    {
                        Id = n.Notification_ID.ToString(),
                        Title = n.Title,
                        Message = n.Message,
                        Type = n.Type,
                        CreatedAt = n.CreatedAt,
                        IsRead = n.IsRead
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetMyHistoryAsync Error: {ex.Message}");
                return new List<NotificationResponse_Dto>();
            }
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            try
            {
                var notification = await _context.TbNotification.FindAsync(notificationId);
                if (notification == null) return false;

                notification.IsRead = true;
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MarkAsReadAsync Error: {ex.Message}");
                return false;
            }
        }
    }
}