using InframartAPI_New.Data;
using InframartAPI_New.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace InframartAPI_New.Helpers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class VendorDashboardAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user == null || !user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedObjectResult(new { success = false, message = "Unauthorized access." });
                return;
            }

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                context.Result = new UnauthorizedObjectResult(new { success = false, message = "Invalid user identity." });
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var vendor = await dbContext.Vendors.FirstOrDefaultAsync(v => v.UserId == userId);

            if (vendor == null)
            {
                context.Result = new ObjectResult(new { success = false, message = "Vendor profile not found." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            if (vendor.Status != VendorStatus.Approved || vendor.KycStatus != KycStatus.Approved)
            {
                context.Result = new ObjectResult(new { success = false, message = "Access denied. Your Vendor profile and KYC must be approved." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }
        }
    }
}
