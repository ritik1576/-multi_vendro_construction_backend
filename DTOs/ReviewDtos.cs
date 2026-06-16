using System;

namespace InframartAPI_New.DTOs
{
    public class CreateReviewDto
    {
        public long ProductId { get; set; }

        public int Rating { get; set; }

        public string Review { get; set; } = string.Empty;
    }

    public class ReviewResponseDto
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public long ProductId { get; set; }

        public int Rating { get; set; }

        public string Review { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
