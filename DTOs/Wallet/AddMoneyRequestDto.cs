using System.ComponentModel.DataAnnotations;

namespace InframartAPI_New.DTOs
{
    public class AddMoneyRequestDto
    {
        [Required]
        [Range(0.01, 100000.00, ErrorMessage = "Deposit amount must be between 0.01 and 100,000.")]
        public decimal Amount { get; set; }
    }
}
