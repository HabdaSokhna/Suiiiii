using Database;
using Database.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CURD
{
    public interface ITbNotificationRepository
    {
        // Create
        Task<Notification> CreateAsync(Notification notification);
        Task<List<Notification>> CreateBulkAsync(List<Notification> notifications);

        // Read
        Task<IEnumerable<Notification>> GetAllAsync();
        Task<Notification?> GetByIdAsync(int id);
        Task<IEnumerable<Notification>> GetByCitizenIdAsync(int citizenId);
        Task<IEnumerable<Notification>> GetUnreadByCitizenIdAsync(int citizenId);
        Task<IEnumerable<Notification>> GetByReportIdAsync(int reportId);

        // Update
        Task<Notification> UpdateAsync(Notification notification);
        Task<bool> MarkAsReadAsync(int notificationId, int citizenId);
        Task<int> MarkAllAsReadAsync(int citizenId);

        // Delete
        Task<bool> DeleteAsync(int id, int citizenId);
        Task<int> DeleteOldNotificationsAsync(int days);

        // Helpers
        Task<bool> ExistsAsync(int id);
        Task<int> GetUnreadCountAsync(int citizenId);
    }

    public class TbNotificationRepository : ITbNotificationRepository
    {
        private readonly Ai_Reports_Context _context;

        public TbNotificationRepository(Ai_Reports_Context context)
        {
            _context = context;
        }

        // ===================================
        // Create
        // ===================================
        public async Task<Notification> CreateAsync(Notification notification)
        {
            notification.CreatedAt = DateTime.UtcNow;
            // تأكد أن خاصية IsRead موجودة في الـ Domain Model
            // notification.IsRead = false; 

            _context.TbNotification.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<List<Notification>> CreateBulkAsync(List<Notification> notifications)
        {
            foreach (var n in notifications)
            {
                n.CreatedAt = DateTime.UtcNow;
            }

            _context.TbNotification.AddRange(notifications);
            await _context.SaveChangesAsync();
            return notifications;
        }

        // ===================================
        // Read
        // ===================================
        public async Task<IEnumerable<Notification>> GetAllAsync()
        {
            return await _context.TbNotification
                .Include(n => n.Report)
                // Include للـ Navigation Property وليس الـ ID
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<Notification?> GetByIdAsync(int id)
        {
            return await _context.TbNotification
                .Include(n => n.Report)
                .FirstOrDefaultAsync(n => n.Notification_ID == id);
        }

        public async Task<IEnumerable<Notification>> GetByCitizenIdAsync(int citizenId)
        {
            return await _context.TbNotification
                .Where(n => n.Citizen_ID == citizenId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetUnreadByCitizenIdAsync(int citizenId)
        {
            // ملاحظة: تأكد من وجود حقل IsRead في جدول TbNotification
            return await _context.TbNotification
                .Where(n => n.Citizen_ID == citizenId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetByReportIdAsync(int reportId)
        {
            return await _context.TbNotification
                .Where(n => n.Report_ID == reportId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        // ===================================
        // Update
        // ===================================
        public async Task<Notification> UpdateAsync(Notification notification)
        {
            _context.TbNotification.Update(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int citizenId)
        {
            var notification = await _context.TbNotification
                .FirstOrDefaultAsync(n => n.Notification_ID == notificationId && n.Citizen_ID == citizenId);

            if (notification == null) return false;

            // notification.IsRead = true; // تفعيلها عند إضافة الحقل للداتابيز
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> MarkAllAsReadAsync(int citizenId)
        {
            var notifications = await _context.TbNotification
                .Where(n => n.Citizen_ID == citizenId)
                .ToListAsync();

            // foreach (var n in notifications) n.IsRead = true;

            await _context.SaveChangesAsync();
            return notifications.Count;
        }

        // ===================================
        // Delete
        // ===================================
        public async Task<bool> DeleteAsync(int id, int citizenId)
        {
            var notification = await _context.TbNotification
                .FirstOrDefaultAsync(n => n.Notification_ID == id && n.Citizen_ID == citizenId);

            if (notification == null) return false;

            _context.TbNotification.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> DeleteOldNotificationsAsync(int days)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var oldNotifications = await _context.TbNotification
                .Where(n => n.CreatedAt < cutoffDate)
                .ToListAsync();

            _context.TbNotification.RemoveRange(oldNotifications);
            await _context.SaveChangesAsync();
            return oldNotifications.Count;
        }

        // ===================================
        // Helpers
        // ===================================
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.TbNotification.AnyAsync(n => n.Notification_ID == id);
        }

        public async Task<int> GetUnreadCountAsync(int citizenId)
        {
            return await _context.TbNotification
                .CountAsync(n => n.Citizen_ID == citizenId);
        }
    }
}