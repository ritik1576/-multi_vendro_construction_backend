using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InframartAPI_New.DTOs;
using InframartAPI_New.Models;
using InframartAPI_New.Repositories.Interfaces;
using InframartAPI_New.Services.Interfaces;
using Microsoft.Extensions.Logging;
using MultiVendorAPI.Common;

namespace InframartAPI_New.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository notificationRepository,
            ILogger<NotificationService> _logger)
        {
            _notificationRepository = notificationRepository;
            this._logger = _logger;
        }

        public async Task<ServiceResponse<List<NotificationDto>>> GetNotificationsAsync(long userId, int pageNumber, int pageSize)
        {
            try
            {
                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0) pageSize = 10;

                var notifications = await _notificationRepository.GetUserNotificationsAsync(userId, pageNumber, pageSize);
                
                var dtos = notifications.Select(n => new NotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                }).ToList();

                return ServiceResponse<List<NotificationDto>>.SuccessResponse(dtos, "Notifications retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve notifications for User: {UserId}", userId);
                return ServiceResponse<List<NotificationDto>>.FailureResponse("An error occurred while retrieving notifications.", 500);
            }
        }

        public async Task<ServiceResponse<NotificationDto>> GetNotificationByIdAsync(long id, long userId)
        {
            try
            {
                var notification = await _notificationRepository.GetNotificationByIdAsync(id);
                if (notification == null)
                {
                    return ServiceResponse<NotificationDto>.FailureResponse("Notification not found.", 404);
                }

                if (notification.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} unauthorized access attempt to Notification {NotificationId}", userId, id);
                    return ServiceResponse<NotificationDto>.FailureResponse("Access denied. You do not own this notification.", 403);
                }

                var dto = new NotificationDto
                {
                    Id = notification.Id,
                    UserId = notification.UserId,
                    Title = notification.Title,
                    Message = notification.Message,
                    Type = notification.Type,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt
                };

                return ServiceResponse<NotificationDto>.SuccessResponse(dto, "Notification retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve notification {NotificationId} for User: {UserId}", id, userId);
                return ServiceResponse<NotificationDto>.FailureResponse("An error occurred while retrieving the notification.", 500);
            }
        }

        public async Task<ServiceResponse<UnreadCountDto>> GetUnreadCountAsync(long userId)
        {
            try
            {
                var count = await _notificationRepository.GetUnreadCountAsync(userId);
                return ServiceResponse<UnreadCountDto>.SuccessResponse(new UnreadCountDto { Count = count }, "Unread count retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve unread notification count for User: {UserId}", userId);
                return ServiceResponse<UnreadCountDto>.FailureResponse("An error occurred while retrieving unread count.", 500);
            }
        }

        public async Task<ServiceResponse<bool>> MarkAsReadAsync(long id, long userId)
        {
            try
            {
                var notification = await _notificationRepository.GetNotificationByIdAsync(id);
                if (notification == null)
                {
                    return ServiceResponse<bool>.FailureResponse("Notification not found.", 404);
                }

                if (notification.UserId != userId)
                {
                    return ServiceResponse<bool>.FailureResponse("Access denied.", 403);
                }

                var success = await _notificationRepository.MarkAsReadAsync(id);
                await _notificationRepository.SaveChangesAsync();

                return ServiceResponse<bool>.SuccessResponse(success, "Notification marked as read successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark notification {NotificationId} as read for User: {UserId}", id, userId);
                return ServiceResponse<bool>.FailureResponse("An error occurred while marking notification as read.", 500);
            }
        }

        public async Task<ServiceResponse<bool>> MarkAsUnreadAsync(long id, long userId)
        {
            try
            {
                var notification = await _notificationRepository.GetNotificationByIdAsync(id);
                if (notification == null)
                {
                    return ServiceResponse<bool>.FailureResponse("Notification not found.", 404);
                }

                if (notification.UserId != userId)
                {
                    return ServiceResponse<bool>.FailureResponse("Access denied.", 403);
                }

                var success = await _notificationRepository.MarkAsUnreadAsync(id);
                await _notificationRepository.SaveChangesAsync();

                return ServiceResponse<bool>.SuccessResponse(success, "Notification marked as unread successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark notification {NotificationId} as unread for User: {UserId}", id, userId);
                return ServiceResponse<bool>.FailureResponse("An error occurred while marking notification as unread.", 500);
            }
        }

        public async Task<ServiceResponse<bool>> MarkAllAsReadAsync(long userId)
        {
            try
            {
                var success = await _notificationRepository.MarkAllAsReadAsync(userId);
                await _notificationRepository.SaveChangesAsync();

                return ServiceResponse<bool>.SuccessResponse(success, "All notifications marked as read successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark all notifications as read for User: {UserId}", userId);
                return ServiceResponse<bool>.FailureResponse("An error occurred while marking all notifications as read.", 500);
            }
        }

        public async Task<ServiceResponse<bool>> DeleteNotificationAsync(long id, long userId)
        {
            try
            {
                var notification = await _notificationRepository.GetNotificationByIdAsync(id);
                if (notification == null)
                {
                    return ServiceResponse<bool>.FailureResponse("Notification not found.", 404);
                }

                if (notification.UserId != userId)
                {
                    return ServiceResponse<bool>.FailureResponse("Access denied.", 403);
                }

                var success = await _notificationRepository.DeleteNotificationAsync(id);
                await _notificationRepository.SaveChangesAsync();

                return ServiceResponse<bool>.SuccessResponse(success, "Notification deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete notification {NotificationId} for User: {UserId}", id, userId);
                return ServiceResponse<bool>.FailureResponse("An error occurred while deleting the notification.", 500);
            }
        }

        public async Task<ServiceResponse<bool>> DeleteAllNotificationsAsync(long userId)
        {
            try
            {
                var success = await _notificationRepository.DeleteAllNotificationsAsync(userId);
                await _notificationRepository.SaveChangesAsync();

                return ServiceResponse<bool>.SuccessResponse(success, "All notifications deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete all notifications for User: {UserId}", userId);
                return ServiceResponse<bool>.FailureResponse("An error occurred while deleting all notifications.", 500);
            }
        }

        public async Task CreateNotificationAsync(long userId, string title, string message, string type)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type.ToLower(),
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationRepository.CreateNotificationAsync(notification);
                await _notificationRepository.SaveChangesAsync();
                _logger.LogInformation("Successfully created notification for User: {UserId}, Type: {Type}", userId, type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create internal notification for User: {UserId}, Title: {Title}, Type: {Type}", userId, title, type);
            }
        }

        public async Task CreateNotificationsBulkAsync(List<long> userIds, string title, string message, string type)
        {
            try
            {
                if (userIds == null || userIds.Count == 0) return;

                var notifications = userIds.Select(userId => new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type.ToLower(),
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                await _notificationRepository.CreateNotificationsBulkAsync(notifications);
                await _notificationRepository.SaveChangesAsync();
                _logger.LogInformation("Successfully created bulk notifications for {Count} users, Type: {Type}", userIds.Count, type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create bulk notifications. Title: {Title}, Type: {Type}", title, type);
            }
        }
    }
}
