using System.ComponentModel.DataAnnotations;

namespace InframartAPI_New.DTOs
{
    public class AddMoneyRequestDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }
    }
}
