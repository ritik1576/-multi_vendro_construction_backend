namespace InframartAPI_New.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public long? UserId { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? Token { get; set; }
    }
}