using System;

namespace InframartAPI_New.DTOs
{
    public class CustomerProfileResponseDto
    {
        public UserDetailsDto User { get; set; } = null!;
        public CustomerDetailsDto Customer { get; set; } = null!;
        public AddressProfileDto? DefaultAddress { get; set; }
        public WalletSummaryDto? Wallet { get; set; }
    }

    public class UserDetailsDto
    {
        public long Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }
    }

    public class CustomerDetailsDto
    {
        public long Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    public class AddressProfileDto
    {
        public long Id { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public string? AddressType { get; set; }
        public bool IsDefault { get; set; }
    }

    public class WalletSummaryDto
    {
        public long Id { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal TotalCredits { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal LockedBalance { get; set; }
    }

    public class VendorProfileResponseDto
    {
        public UserDetailsDto User { get; set; } = null!;
        public VendorDetailsDto Vendor { get; set; } = null!;
        public BusinessKycDetailsDto? Kyc { get; set; }
        public WalletSummaryDto? Wallet { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class VendorDetailsDto
    {
        public long Id { get; set; }
        public string? ShopName { get; set; }
        public string? ShopSlug { get; set; }
        public string? Description { get; set; }
        public string? Logo { get; set; }
        public string? Banner { get; set; }
        public decimal? CommissionRate { get; set; }
        public string KycStatus { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class BusinessKycDetailsDto
    {
        public long Id { get; set; }
        public string BusinessLegalName { get; set; } = string.Empty;
        public string BankAccountName { get; set; } = string.Empty;
        public string AadhaarDocumentUrl { get; set; } = string.Empty;
        public string GstNumber { get; set; } = string.Empty;
        public string PanNumber { get; set; } = string.Empty;
        public string BusinessAddress { get; set; } = string.Empty;
        public string BankAccountNumber { get; set; } = string.Empty;
        public string IFSC_Code { get; set; } = string.Empty;
        public string GstCertificateUrl { get; set; } = string.Empty;
        public string PanCardUrl { get; set; } = string.Empty;
        public string BankStatementUrl { get; set; } = string.Empty;
        public string KycStatus { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }
}
