using System;
using System.Security.Claims;
using System.Threading.Tasks;
using InframartAPI_New.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InframartAPI_New.Controllers
{
    [ApiController]
    [Route("notifications")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private long GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !long.TryParse(claim, out var userId))
            {
                throw new UnauthorizedAccessException("User identification token is invalid or missing.");
            }
            return userId;
        }

        // ================= GET ALL NOTIFICATIONS =================
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var response = await _notificationService.GetNotificationsAsync(userId, pageNumber, pageSize);
            
            if (!response.Success)
            {
                return StatusCode(response.StatusCode, new { message = response.Message });
            }

            return Ok(response.Data);
        }

        // ================= GET UNREAD COUNT =================
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            var response = await _notificationService.GetUnreadCountAsync(userId);

            if (!response.Success)
            {
                return StatusCode(response.StatusCode, new { message = response.Message });
            }

            return Ok(response.Data);
        }

        // ================= GET NOTIFICATION BY ID =================
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetNotificationById(long id)
        {
            var userId = GetCurrentUserId();
            var response = await _notificationService.GetNotificationByIdAsync(id, userId);

            if (!response.Success)
            {
                return StatusCode(response.StatusCode, new { message = response.Message });
            }

            return Ok(response.Data);
        }

        // ================= MARK AS READ =================
        [HttpPut("{id:long}/read")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            var userId = GetCurrentUserId();
            var response = await _notificationService.MarkAsReadAsync(id, userId);

            if (!response.Success)
            {
                return StatusCode(response.StatusCode, new { message = response.Message });
            }

            return Ok(new { success = true, message = response.Message });
        }

        // ================= MARK AS UNREAD =================
        [HttpPut("{id:long}/unread")]
        public async Task<IActionResult> MarkAsUnread(long id)
        {
            var userId = GetCurrentUserId();
            var response = await _notificationService.MarkAsUnreadAsync(id, userId);

            if (!response.Success)
            {
                return StatusCode(response.StatusCode, new { message = response.Message });
            }

            return Ok(new { success = true, message = response.Message });
        }

        // ================= MARK ALL AS READ =================
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            var response = await _notificationService.MarkAllAsReadAsync(userId);

            if (!response.Success)
            {
                return StatusCode(response.StatusCode, new { message = response.Message });
            }

            return Ok(new { success = true, message = response.Message });
        }

        // ================= DELETE NOTIFICATION =================
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteNotification(long id)
        {
            var userId = GetCurrentUserId();
            var response = await _notificationService.DeleteNotificationAsync(id, userId);

            if (!response.Success)
            {
                return StatusCode(response.StatusCode, new { message = response.Message });
            }

            return Ok(new { success = true, message = response.Message });
        }

        // ================= DELETE ALL NOTIFICATIONS =================
        [HttpDelete]
        public async Task<IActionResult> DeleteAllNotifications()
        {
            var userId = GetCurrentUserId();
            var response = await _notificationService.DeleteAllNotificationsAsync(userId);

            if (!response.Success)
            {
                return StatusCode(response.StatusCode, new { message = response.Message });
            }

            return Ok(new { success = true, message = response.Message });
        }
    }
}
