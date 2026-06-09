using System.ComponentModel.DataAnnotations;

namespace InframartAPI_New.DTOs
{
    public class CreatePaymentDto
    {
        [Required]
        public long Order_Id { get; set; }

        [Required]
        public long User_Id { get; set; }

        [Required]
        public string Payment_Status { get; set; } = "created";
    }
}