namespace InframartAPI_New.Models
{
    public class PasswordResetToken
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiryTime { get; set; }
        public bool IsUsed { get; set; } = false;
    }
}