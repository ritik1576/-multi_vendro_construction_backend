using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InframartAPI_New.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        public long Id { get; set; }

        [Column("full_name")]
        public string? FullName { get; set; }

        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Phone { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }

        [Column("is_suspended")]
        public bool IsSuspended { get; set; }

        [Column("suspended_at")]
        public DateTime? SuspendedAt { get; set; }

        [Column("suspension_reason")]
        public string? SuspensionReason { get; set; }
    }
}