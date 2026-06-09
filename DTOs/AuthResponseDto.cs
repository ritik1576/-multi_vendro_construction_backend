namespace InframartAPI_New.DTOs
{
    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }

        public string? Token { get; set; }

        public long UserId { get; set; }
        public long VendorId { get; set; }

        public string? Role { get; set; }

        public string? Status { get; set; }

        public string? ShopName { get; set; }
    }
} 