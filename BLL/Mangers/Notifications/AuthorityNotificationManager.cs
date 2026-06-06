using BLL.DTO.Notification;
using Database;
using Microsoft.EntityFrameworkCore;

namespace BLL.Managers.Notification
{
    public interface IAuthorityNotificationManager
    {
        Task<List<NotificationResponse_Dto>> GetMyHistoryAsync(int authorityId);
        Task<bool> MarkAsReadAsync(int notificationId);
    }

    public class AuthorityNotificationManager : IAuthorityNotificationManager
    {
        private readonly Ai_Reports_Context _context;

        public AuthorityNotificationManager(Ai_Reports_Context context)
        {
            _context = context;
        }

        public async Task<List<NotificationResponse_Dto>> GetMyHistoryAsync(int authorityId)
        {
            return await _context.TbAuthorityNotification
                .Where(n => n.Authority_ID == authorityId)
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

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.TbAuthorityNotification
                .FindAsync(notificationId);

            if (notification == null) return false;

            notification.IsRead = true;
            return await _context.SaveChangesAsync() > 0;
        }
    }
}