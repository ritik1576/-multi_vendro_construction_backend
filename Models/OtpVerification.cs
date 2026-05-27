namespace InframartAPI_New.Models
{
    public class OtpVerification
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? Otp { get; set; }
        public DateTime Expiry { get; set; }
    }
}