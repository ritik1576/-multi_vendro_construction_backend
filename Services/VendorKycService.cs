using InframartAPI_New.Data;
using InframartAPI_New.DTOs;
using InframartAPI_New.Models;
using InframartAPI_New.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InframartAPI_New.Services
{
    public class VendorKycService : IVendorKycService
    {
        private readonly AppDbContext _context;
        private readonly IFileUploadService _fileUploadService;
        private readonly INotificationService _notificationService;
        private readonly IEmailNotificationService _emailNotificationService;

        public VendorKycService(
            AppDbContext context,
            IFileUploadService fileUploadService,
            INotificationService notificationService,
            IEmailNotificationService emailNotificationService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _notificationService = notificationService;
            _emailNotificationService = emailNotificationService;
        }

        public async Task<(bool success, string message)> SubmitKycAsync(KycSubmitDto dto)
        {
            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == dto.VendorId);
            if (vendor == null)
            {
                return (false, "Vendor profile not found.");
            }

            try
            {
                // Upload files
                var aadhaarUrl = await _fileUploadService.UploadKycDocumentAsync(dto.AadhaarPdf);
                var gstUrl = await _fileUploadService.UploadKycDocumentAsync(dto.GstCertificateUpload);
                var panUrl = await _fileUploadService.UploadKycDocumentAsync(dto.PanCardUpload);
                var bankUrl = await _fileUploadService.UploadKycDocumentAsync(dto.BankStatementUpload);

                var kyc = await _context.VendorKycs.FirstOrDefaultAsync(k => k.VendorId == vendor.Id);
                if (kyc == null)
                {
                    kyc = new VendorKyc
                    {
                        VendorId = vendor.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.VendorKycs.Add(kyc);
                }

                kyc.BusinessLegalName = dto.BusinessLegalName;
                kyc.BankAccountName = dto.BankAccountName;
                kyc.AadhaarDocumentUrl = aadhaarUrl;
                kyc.GstNumber = dto.GstNumber;
                kyc.PanNumber = dto.PanNumber;
                kyc.BusinessAddress = dto.BusinessAddress;
                kyc.BankAccountNumber = dto.BankAccountNumber;
                kyc.IfscCode = dto.IFSC;
                kyc.GstCertificateUrl = gstUrl;
                kyc.PanCardUrl = panUrl;
                kyc.BankStatementUrl = bankUrl;
                kyc.Status = KycStatus.UnderReview;
                kyc.RejectionReason = null;
                kyc.SubmittedAt = DateTime.UtcNow;
                kyc.UpdatedAt = DateTime.UtcNow;

                vendor.KycStatus = KycStatus.UnderReview;

                await _context.SaveChangesAsync();

                // Notify admin of submission
                try
                {
                    var adminUserIds = await _context.Users
                        .Where(u => u.Role == "admin")
                        .Select(u => u.Id)
                        .ToListAsync();

                    foreach (var adminId in adminUserIds)
                    {
                        await _notificationService.CreateNotificationAsync(
                            adminId, 
                            "KYC Submitted for Review", 
                            $"Vendor '{vendor.ShopName}' has submitted KYC documents for review.", 
                            "vendor"
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send notification to admins: {ex.Message}");
                }

                return (true, "KYC submitted successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to submit KYC: {ex.Message}");
            }
        }

        public async Task<(bool success, string? error, KycStatusResponseDto? data)> GetKycStatusAsync(long vendorId)
        {
            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId);
            if (vendor == null)
            {
                return (false, "Vendor profile not found.", null);
            }

            var data = new KycStatusResponseDto
            {
                KycStatus = vendor.KycStatus.ToString(),
                VendorStatus = vendor.Status.ToString()
            };

            return (true, null, data);
        }

        public async Task<(bool success, string? error, List<AdminKycDetailsDto>? data)> GetKycRequestsAsync(string? statusFilter)
        {
            KycStatus? targetStatus = null;
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                if (Enum.TryParse<KycStatus>(statusFilter, true, out var parsedStatus))
                {
                    targetStatus = parsedStatus;
                }
                else
                {
                    return (false, "Invalid status filter. Allowed values: Pending, UnderReview, Approved, Rejected.", null);
                }
            }

            var query = _context.VendorKycs.AsQueryable();
            if (targetStatus.HasValue)
            {
                query = query.Where(k => k.Status == targetStatus.Value);
            }

            var kycs = await query.ToListAsync();
            var vendorIds = kycs.Select(k => k.VendorId).Distinct().ToList();
            
            var vendors = await _context.Vendors
                .Where(v => vendorIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id);

            var userIds = vendors.Values.Where(v => v.UserId.HasValue).Select(v => v.UserId!.Value).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            var result = kycs.Select(k =>
            {
                vendors.TryGetValue(k.VendorId, out var vendor);
                User? user = null;
                if (vendor?.UserId != null)
                {
                    users.TryGetValue(vendor.UserId.Value, out user);
                }

                return new AdminKycDetailsDto
                {
                    Id = k.Id,
                    VendorId = k.VendorId,
                    ShopName = vendor?.ShopName,
                    ShopSlug = vendor?.ShopSlug,
                    BusinessLegalName = k.BusinessLegalName,
                    BankAccountName = k.BankAccountName,
                    AadhaarDocumentUrl = k.AadhaarDocumentUrl,
                    GstNumber = k.GstNumber,
                    PanNumber = k.PanNumber,
                    BusinessAddress = k.BusinessAddress,
                    BankAccountNumber = k.BankAccountNumber,
                    IfscCode = k.IfscCode,
                    GstCertificateUrl = k.GstCertificateUrl,
                    PanCardUrl = k.PanCardUrl,
                    BankStatementUrl = k.BankStatementUrl,
                    KycStatus = k.Status.ToString(),
                    RejectionReason = k.RejectionReason,
                    SubmittedAt = k.SubmittedAt,
                    VerifiedAt = k.VerifiedAt,
                    VerifiedBy = k.VerifiedBy,
                    CreatedAt = k.CreatedAt,
                    UpdatedAt = k.UpdatedAt,
                    VendorStatus = vendor?.Status.ToString(),
                    VendorEmail = user?.Email,
                    VendorPhone = user?.Phone
                };
            }).ToList();

            return (true, null, result);
        }

        public async Task<(bool success, string? error, AdminKycDetailsDto? data)> GetKycDetailsAsync(long vendorId)
        {
            var kyc = await _context.VendorKycs.FirstOrDefaultAsync(k => k.VendorId == vendorId);
            if (kyc == null)
            {
                return (false, "KYC records not found for this vendor.", null);
            }

            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId);
            User? user = null;
            if (vendor?.UserId != null)
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Id == vendor.UserId.Value);
            }

            var data = new AdminKycDetailsDto
            {
                Id = kyc.Id,
                VendorId = kyc.VendorId,
                ShopName = vendor?.ShopName,
                ShopSlug = vendor?.ShopSlug,
                BusinessLegalName = kyc.BusinessLegalName,
                BankAccountName = kyc.BankAccountName,
                AadhaarDocumentUrl = kyc.AadhaarDocumentUrl,
                GstNumber = kyc.GstNumber,
                PanNumber = kyc.PanNumber,
                BusinessAddress = kyc.BusinessAddress,
                BankAccountNumber = kyc.BankAccountNumber,
                IfscCode = kyc.IfscCode,
                GstCertificateUrl = kyc.GstCertificateUrl,
                PanCardUrl = kyc.PanCardUrl,
                BankStatementUrl = kyc.BankStatementUrl,
                KycStatus = kyc.Status.ToString(),
                RejectionReason = kyc.RejectionReason,
                SubmittedAt = kyc.SubmittedAt,
                VerifiedAt = kyc.VerifiedAt,
                VerifiedBy = kyc.VerifiedBy,
                CreatedAt = kyc.CreatedAt,
                UpdatedAt = kyc.UpdatedAt,
                VendorStatus = vendor?.Status.ToString(),
                VendorEmail = user?.Email,
                VendorPhone = user?.Phone
            };

            return (true, null, data);
        }

        public async Task<(bool success, string message)> ApproveKycAsync(long vendorId, long adminUserId)
        {
            var kyc = await _context.VendorKycs.FirstOrDefaultAsync(k => k.VendorId == vendorId);
            if (kyc == null)
            {
                return (false, "KYC details not found.");
            }

            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId);
            if (vendor == null)
            {
                return (false, "Vendor profile not found.");
            }

            kyc.Status = KycStatus.Approved;
            kyc.VerifiedAt = DateTime.UtcNow;
            kyc.VerifiedBy = adminUserId;
            kyc.UpdatedAt = DateTime.UtcNow;

            vendor.KycStatus = KycStatus.Approved;

            await _context.SaveChangesAsync();

            if (vendor.UserId.HasValue)
            {
                try
                {
                    await _notificationService.CreateNotificationAsync(
                        vendor.UserId.Value,
                        "KYC Approved",
                        "Your Vendor KYC documents have been successfully approved by the administrator.",
                        "vendor"
                    );

                    // Send KYC_APPROVED email
                    var vendorUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == vendor.UserId.Value);
                    if (vendorUser != null && !string.IsNullOrEmpty(vendorUser.Email))
                    {
                        await _emailNotificationService.SendTemplateEmailAsync(
                            "KYC_APPROVED",
                            vendorUser.Email,
                            new Dictionary<string, string>
                            {
                                { "VendorName", vendorUser.FullName ?? "Vendor" },
                                { "ApprovalDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") }
                            }
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to notify vendor of KYC approval: {ex.Message}");
                }
            }

            return (true, "KYC approved successfully.");
        }

        public async Task<(bool success, string message)> RejectKycAsync(long vendorId, string reason, long adminUserId)
        {
            var kyc = await _context.VendorKycs.FirstOrDefaultAsync(k => k.VendorId == vendorId);
            if (kyc == null)
            {
                return (false, "KYC details not found.");
            }

            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId);
            if (vendor == null)
            {
                return (false, "Vendor profile not found.");
            }

            kyc.Status = KycStatus.Rejected;
            kyc.RejectionReason = reason;
            kyc.VerifiedAt = DateTime.UtcNow;
            kyc.VerifiedBy = adminUserId;
            kyc.UpdatedAt = DateTime.UtcNow;

            vendor.KycStatus = KycStatus.Rejected;

            await _context.SaveChangesAsync();

            if (vendor.UserId.HasValue)
            {
                try
                {
                    await _notificationService.CreateNotificationAsync(
                        vendor.UserId.Value,
                        "KYC Rejected",
                        $"Your Vendor KYC has been rejected. Reason: {reason}",
                        "vendor"
                    );

                    // Send KYC_REJECTED email
                    var vendorUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == vendor.UserId.Value);
                    if (vendorUser != null && !string.IsNullOrEmpty(vendorUser.Email))
                    {
                        await _emailNotificationService.SendTemplateEmailAsync(
                            "KYC_REJECTED",
                            vendorUser.Email,
                            new Dictionary<string, string>
                            {
                                { "VendorName", vendorUser.FullName ?? "Vendor" },
                                { "RejectReason", reason },
                                { "ResubmitUrl", "https://inframart.com/vendor/kyc/resubmit" }
                            }
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to notify vendor of KYC rejection: {ex.Message}");
                }
            }

            return (true, "KYC rejected successfully.");
        }

        public async Task<(bool success, string message)> ApproveVendorAsync(long vendorId)
        {
            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId);
            if (vendor == null)
            {
                return (false, "Vendor profile not found.");
            }

            if (vendor.KycStatus != KycStatus.Approved)
            {
                return (false, "KYC Status must be Approved before approving the vendor profile.");
            }

            vendor.Status = VendorStatus.Approved;
            vendor.UpdatedAt = DateTime.UtcNow;

            if (vendor.UserId.HasValue)
            {
                var walletExists = await _context.Wallets.AnyAsync(w => w.UserId == vendor.UserId.Value && w.WalletType == WalletType.Vendor);
                if (!walletExists)
                {
                    var wallet = new Wallet
                    {
                        UserId = vendor.UserId.Value,
                        WalletType = WalletType.Vendor,
                        AvailableBalance = 0.00m,
                        LockedBalance = 0.00m,
                        TotalCredits = 0.00m,
                        TotalDebits = 0.00m,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        RowVersion = 1
                    };
                    _context.Wallets.Add(wallet);
                }
            }

            await _context.SaveChangesAsync();

            if (vendor.UserId.HasValue)
            {
                try
                {
                    await _notificationService.CreateNotificationAsync(
                        vendor.UserId.Value,
                        "Vendor Profile Approved",
                        "Your Vendor profile is now fully approved! You can now log in and access your dashboard.",
                        "vendor"
                    );

                    // Send VENDOR_APPROVED email
                    var vendorUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == vendor.UserId.Value);
                    if (vendorUser != null && !string.IsNullOrEmpty(vendorUser.Email))
                    {
                        await _emailNotificationService.SendTemplateEmailAsync(
                            "VENDOR_APPROVED",
                            vendorUser.Email,
                            new Dictionary<string, string>
                            {
                                { "VendorName", vendorUser.FullName ?? "Vendor" },
                                { "DashboardUrl", "https://inframart.com/vendor/dashboard" }
                            }
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to notify vendor of approval: {ex.Message}");
                }
            }

            return (true, "Vendor approved successfully.");
        }

        public async Task<(bool success, string message)> RejectVendorAsync(long vendorId, string reason)
        {
            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId);
            if (vendor == null)
            {
                return (false, "Vendor profile not found.");
            }

            vendor.Status = VendorStatus.Rejected;
            vendor.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (vendor.UserId.HasValue)
            {
                try
                {
                    await _notificationService.CreateNotificationAsync(
                        vendor.UserId.Value,
                        "Vendor Profile Rejected",
                        $"Your Vendor account application was rejected. Reason: {reason}",
                        "vendor"
                    );

                    // Send VENDOR_REJECTED email
                    var vendorUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == vendor.UserId.Value);
                    if (vendorUser != null && !string.IsNullOrEmpty(vendorUser.Email))
                    {
                        await _emailNotificationService.SendTemplateEmailAsync(
                            "VENDOR_REJECTED",
                            vendorUser.Email,
                            new Dictionary<string, string>
                            {
                                { "VendorName", vendorUser.FullName ?? "Vendor" },
                                { "RejectReason", reason },
                                { "ResubmitUrl", "https://inframart.com/vendor/kyc/resubmit" }
                            }
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to notify vendor of rejection: {ex.Message}");
                }
            }

            return (true, "Vendor rejected successfully.");
        }
    }
}
