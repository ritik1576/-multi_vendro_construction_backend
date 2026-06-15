using System.Collections.Generic;
using System.Threading.Tasks;
using InframartAPI_New.Models;

namespace InframartAPI_New.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task<List<Notification>> GetUserNotificationsAsync(long userId, int pageNumber, int pageSize);
        Task<Notification?> GetNotificationByIdAsync(long id);
        Task<int> GetUnreadCountAsync(long userId);
        Task<bool> MarkAsReadAsync(long id);
        Task<bool> MarkAsUnreadAsync(long id);
        Task<bool> MarkAllAsReadAsync(long userId);
        Task<bool> DeleteNotificationAsync(long id);
        Task<bool> DeleteAllNotificationsAsync(long userId);
        Task<Notification> CreateNotificationAsync(Notification notification);
        Task CreateNotificationsBulkAsync(List<Notification> notifications);
        Task SaveChangesAsync();
    }
}
