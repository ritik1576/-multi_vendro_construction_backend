namespace InframartAPI_New.DTOs
{
    public class ForgetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}