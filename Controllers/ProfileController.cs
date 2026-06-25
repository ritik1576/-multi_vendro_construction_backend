using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InframartAPI_New.Data;
using MultiVendorAPI.Data;
using InframartAPI_New.DTOs;
using InframartAPI_New.Models;
using MultiVendorAPI.Models;

namespace InframartAPI_New.Controllers
{
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _authContext;
        private readonly ApplicationDbContext _appContext;

        public ProfileController(AppDbContext authContext, ApplicationDbContext appContext)
        {
            _authContext = authContext;
            _appContext = appContext;
        }

        private long GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !long.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException();
            return userId;
        }

        [HttpGet("customer/profile")]
        [Authorize(Roles = "customer")]
        public async Task<IActionResult> GetCustomerProfile()
        {
            try
            {
                var userId = GetCurrentUserId();

                var user = await _authContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var defaultAddress = await _appContext.Addresses
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault);

                var wallet = await _appContext.Wallets
                    .FirstOrDefaultAsync(w => w.UserId == userId);

                var profile = new CustomerProfileResponseDto
                {
                    User = new UserDetailsDto
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email,
                        Phone = user.Phone,
                        Role = user.Role,
                        Status = user.Status
                    },
                    Customer = new CustomerDetailsDto
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email,
                        Phone = user.Phone
                    },
                    DefaultAddress = defaultAddress == null ? null : new AddressProfileDto
                    {
                        Id = defaultAddress.Id,
                        FullName = defaultAddress.FullName,
                        Phone = defaultAddress.Phone,
                        AddressLine1 = defaultAddress.AddressLine1,
                        AddressLine2 = defaultAddress.AddressLine2,
                        City = defaultAddress.City,
                        State = defaultAddress.State,
                        Country = defaultAddress.Country,
                        PostalCode = defaultAddress.PostalCode,
                        AddressType = defaultAddress.AddressType,
                        IsDefault = defaultAddress.IsDefault
                    },
                    Wallet = wallet == null ? null : new WalletSummaryDto
                    {
                        Id = wallet.Id,
                        AvailableBalance = wallet.AvailableBalance,
                        TotalCredits = wallet.TotalCredits,
                        TotalDebits = wallet.TotalDebits,
                        LockedBalance = wallet.LockedBalance
                    }
                };

                return Ok(profile);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpGet("vendor/profile")]
        [Authorize(Roles = "vendor")]
        public async Task<IActionResult> GetVendorProfile()
        {
            try
            {
                var userId = GetCurrentUserId();

                var user = await _authContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var vendor = await _authContext.Vendors.FirstOrDefaultAsync(v => v.UserId == userId);
                if (vendor == null)
                {
                    return NotFound(new { message = "Vendor details not found" });
                }

                var kyc = await _authContext.VendorKycs.FirstOrDefaultAsync(k => k.VendorId == vendor.Id);

                var wallet = await _appContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);

                var profile = new VendorProfileResponseDto
                {
                    User = new UserDetailsDto
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email,
                        Phone = user.Phone,
                        Role = user.Role,
                        Status = user.Status
                    },
                    Vendor = new VendorDetailsDto
                    {
                        Id = vendor.Id,
                        ShopName = vendor.ShopName,
                        ShopSlug = vendor.ShopSlug,
                        Description = vendor.Description,
                        Logo = vendor.Logo,
                        Banner = vendor.Banner,
                        CommissionRate = vendor.CommissionRate,
                        KycStatus = vendor.KycStatus.ToString(),
                        Status = vendor.Status.ToString()
                    },
                    Kyc = kyc == null ? null : new BusinessKycDetailsDto
                    {
                        Id = kyc.Id,
                        BusinessLegalName = kyc.BusinessLegalName,
                        BankAccountName = kyc.BankAccountName,
                        AadhaarDocumentUrl = kyc.AadhaarDocumentUrl,
                        GstNumber = kyc.GstNumber,
                        PanNumber = kyc.PanNumber,
                        BusinessAddress = kyc.BusinessAddress,
                        BankAccountNumber = kyc.BankAccountNumber,
                        IFSC_Code = kyc.IfscCode,
                        GstCertificateUrl = kyc.GstCertificateUrl,
                        PanCardUrl = kyc.PanCardUrl,
                        BankStatementUrl = kyc.BankStatementUrl,
                        KycStatus = kyc.Status.ToString(),
                        RejectionReason = kyc.RejectionReason,
                        SubmittedAt = kyc.SubmittedAt,
                        VerifiedAt = kyc.VerifiedAt
                    },
                    Wallet = wallet == null ? null : new WalletSummaryDto
                    {
                        Id = wallet.Id,
                        AvailableBalance = wallet.AvailableBalance,
                        TotalCredits = wallet.TotalCredits,
                        TotalDebits = wallet.TotalDebits,
                        LockedBalance = wallet.LockedBalance
                    },
                    CreatedAt = vendor.CreatedAt,
                    UpdatedAt = vendor.UpdatedAt
                };

                return Ok(profile);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
