using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InframartAPI_New.Models
{
    [Table("vendor_kyc")]
    public class VendorKyc
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("vendor_id")]
        public long VendorId { get; set; }

        [Column("business_legal_name")]
        public string BusinessLegalName { get; set; } = string.Empty;

        [Column("bank_account_name")]
        public string BankAccountName { get; set; } = string.Empty;

        [Column("aadhaar_document_url")]
        public string AadhaarDocumentUrl { get; set; } = string.Empty;

        [Column("gst_number")]
        public string GstNumber { get; set; } = string.Empty;

        [Column("pan_number")]
        public string PanNumber { get; set; } = string.Empty;

        [Column("business_address")]
        public string BusinessAddress { get; set; } = string.Empty;

        [Column("bank_account_number")]
        public string BankAccountNumber { get; set; } = string.Empty;

        [Column("ifsc_code")]
        public string IfscCode { get; set; } = string.Empty;

        [Column("gst_certificate_url")]
        public string GstCertificateUrl { get; set; } = string.Empty;

        [Column("pan_card_url")]
        public string PanCardUrl { get; set; } = string.Empty;

        [Column("bank_statement_url")]
        public string BankStatementUrl { get; set; } = string.Empty;

        [Column("status")]
        public KycStatus Status { get; set; } = KycStatus.NotSubmitted;

        [Column("rejection_reason")]
        public string? RejectionReason { get; set; }

        [Column("submitted_at")]
        public DateTime? SubmittedAt { get; set; }

        [Column("verified_at")]
        public DateTime? VerifiedAt { get; set; }

        [Column("verified_by")]
        public long? VerifiedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
