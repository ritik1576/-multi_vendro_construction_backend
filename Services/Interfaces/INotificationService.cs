using System.Collections.Generic;
using System.Threading.Tasks;
using MultiVendorAPI.Common;
using InframartAPI_New.DTOs;

namespace InframartAPI_New.Services.Interfaces
{
    public interface INotificationService
    {
        Task<ServiceResponse<List<NotificationDto>>> GetNotificationsAsync(long userId, int pageNumber, int pageSize);
        Task<ServiceResponse<NotificationDto>> GetNotificationByIdAsync(long id, long userId);
        Task<ServiceResponse<UnreadCountDto>> GetUnreadCountAsync(long userId);
        Task<ServiceResponse<bool>> MarkAsReadAsync(long id, long userId);
        Task<ServiceResponse<bool>> MarkAsUnreadAsync(long id, long userId);
        Task<ServiceResponse<bool>> MarkAllAsReadAsync(long userId);
        Task<ServiceResponse<bool>> DeleteNotificationAsync(long id, long userId);
        Task<ServiceResponse<bool>> DeleteAllNotificationsAsync(long userId);

        // Internal methods for business triggers
        Task CreateNotificationAsync(long userId, string title, string message, string type);
        Task CreateNotificationsBulkAsync(List<long> userIds, string title, string message, string type);
    }
}
