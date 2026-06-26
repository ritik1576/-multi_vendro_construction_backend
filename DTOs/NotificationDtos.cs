using System;

namespace InframartAPI_New.DTOs
{
    public class NotificationDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public string? ReferenceType { get; set; }
        public string? ReferenceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UnreadCountDto
    {
        public int Count { get; set; }
    }

    public class CreateAnnouncementDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
