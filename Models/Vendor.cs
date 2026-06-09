using System.ComponentModel.DataAnnotations;

namespace InframartAPI_New.Models
{
    public class Vendor
    {
        [Key]
        public long Id { get; set; }

        public string? Name { get; set; }

        public string? Status { get; set; } = "pending"; 
        // pending / approved / rejected

        public long UserId { get; set; }

        public User User { get; set; } = null!;
    }
}