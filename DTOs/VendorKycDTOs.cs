using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace InframartAPI_New.DTOs
{
    public class KycSubmitDto
    {
        [Required]
        public long VendorId { get; set; }

        [Required]
        public string BusinessLegalName { get; set; } = string.Empty;

        [Required]
        public string BankAccountName { get; set; } = string.Empty;

        [Required]
        public IFormFile AadhaarPdf { get; set; }

        [Required]
        public string GstNumber { get; set; } = string.Empty;

        [Required]
        public string PanNumber { get; set; } = string.Empty;

        [Required]
        public string BusinessAddress { get; set; } = string.Empty;

        [Required]
        public string BankAccountNumber { get; set; } = string.Empty;

        [Required]
        public string IFSC { get; set; } = string.Empty;

        [Required]
        public IFormFile GstCertificateUpload { get; set; }

        [Required]
        public IFormFile PanCardUpload { get; set; }

        [Required]
        public IFormFile BankStatementUpload { get; set; }
    }

    public class KycStatusResponseDto
    {
        public string KycStatus { get; set; } = string.Empty;
        public string VendorStatus { get; set; } = string.Empty;
    }

    public class AdminKycDetailsDto
    {
        public long Id { get; set; }
        public long VendorId { get; set; }
        public string? ShopName { get; set; }
        public string? ShopSlug { get; set; }
        public string BusinessLegalName { get; set; } = string.Empty;
        public string BankAccountName { get; set; } = string.Empty;
        public string AadhaarDocumentUrl { get; set; } = string.Empty;
        public string GstNumber { get; set; } = string.Empty;
        public string PanNumber { get; set; } = string.Empty;
        public string BusinessAddress { get; set; } = string.Empty;
        public string BankAccountNumber { get; set; } = string.Empty;
        public string IfscCode { get; set; } = string.Empty;
        public string GstCertificateUrl { get; set; } = string.Empty;
        public string PanCardUrl { get; set; } = string.Empty;
        public string BankStatementUrl { get; set; } = string.Empty;
        public string KycStatus { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public long? VerifiedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Vendor details
        public string? VendorStatus { get; set; }
        public string? VendorEmail { get; set; }
        public string? VendorPhone { get; set; }
    }

    public class KycRejectDto
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
    }

    public class VendorRejectDto
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
    }
}
