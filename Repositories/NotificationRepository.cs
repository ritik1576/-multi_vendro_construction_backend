using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InframartAPI_New.Data;
using InframartAPI_New.Models;
using InframartAPI_New.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InframartAPI_New.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(long userId, int pageNumber, int pageSize)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Notification?> GetNotificationByIdAsync(long id)
        {
            return await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<int> GetUnreadCountAsync(long userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> MarkAsReadAsync(long id)
        {
            var notification = await GetNotificationByIdAsync(id);
            if (notification == null) return false;

            notification.IsRead = true;
            return true;
        }

        public async Task<bool> MarkAsUnreadAsync(long id)
        {
            var notification = await GetNotificationByIdAsync(id);
            if (notification == null) return false;

            notification.IsRead = false;
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(long userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unreadNotifications)
            {
                n.IsRead = true;
            }
            return true;
        }

        public async Task<bool> DeleteNotificationAsync(long id)
        {
            var notification = await GetNotificationByIdAsync(id);
            if (notification == null) return false;

            _context.Notifications.Remove(notification);
            return true;
        }

        public async Task<bool> DeleteAllNotificationsAsync(long userId)
        {
            var userNotifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .ToListAsync();

            _context.Notifications.RemoveRange(userNotifications);
            return true;
        }

        public async Task<Notification> CreateNotificationAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
            return notification;
        }

        public async Task CreateNotificationsBulkAsync(List<Notification> notifications)
        {
            if (notifications != null && notifications.Count > 0)
            {
                await _context.Notifications.AddRangeAsync(notifications);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
