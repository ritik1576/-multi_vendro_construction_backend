using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InframartAPI_New.Models
{
    public class Vendor
    {
        [Key]
        public long Id { get; set; }

        public string? Name { get; set; }

        // FK
        public long UserId { get; set; }

        // Navigation
        [ForeignKey("UserId")]
        public required User User { get; set; }
    }
}