using System.Collections.Generic;
using System.Threading.Tasks;
using InframartAPI_New.DTOs;
using InframartAPI_New.Models;
using MultiVendorAPI.Common;

namespace InframartAPI_New.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<ServiceResponse<object>> CreatePaymentAsync(CreatePaymentDto request, long userId);
        Task<ServiceResponse<object>> VerifyPaymentAsync(VerifyPaymentDto request, long userId);
        Task<ServiceResponse<List<Payment>>> PaymentHistoryAsync();
        Task<ServiceResponse<object>> RefundPaymentAsync(RefundDto request);
    }
}
