using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InframartAPI_New.Middlewares;

namespace MultiVendorAPI.Controllers;

[ApiController]
[Route("orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;

    public OrdersController(IOrderService service)
    {
        _service = service;
    }

    private (long userId, string role) GetCurrentUser()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(idClaim) || !long.TryParse(idClaim, out var userId) || string.IsNullOrEmpty(roleClaim))
            throw new UnauthorizedAccessException();
        return (userId, roleClaim);
    }

    [HttpPost]
    [Authorize(Roles = "customer")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var (userId, _) = GetCurrentUser();
        if (dto.UserId != userId)
        {
            throw new ForbiddenException("Cannot place an order for another user.");
        }

        var result = await _service.CreateOrderAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("all/{userId:long}")]
    [Authorize(Roles = "customer,admin")]
    public async Task<IActionResult> GetOrders(long userId)
    {
        var (currentUserId, role) = GetCurrentUser();
        if (role != "admin" && userId != currentUserId)
        {
            throw new ForbiddenException("Access denied to view these orders.");
        }

        var result = await _service.GetOrdersAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("all-with-items")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllOrdersWithItems()
    {
        var result = await _service.GetAllOrdersWithItemsAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:long}")]
    [Authorize(Roles = "customer,admin")]
    public async Task<IActionResult> GetOrderDetails(long id)
    {
        var (currentUserId, role) = GetCurrentUser();
        var result = await _service.GetOrderDetailsAsync(id, currentUserId, role);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:long}/cancel")]
    [Authorize(Roles = "customer,admin")]
    public async Task<IActionResult> CancelOrder(long id)
    {
        var (currentUserId, role) = GetCurrentUser();
        var result = await _service.CancelOrderAsync(id, currentUserId, role);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:long}/tracking")]
    [Authorize(Roles = "customer,admin")]
    public async Task<IActionResult> GetOrderTracking(long id)
    {
        var (currentUserId, role) = GetCurrentUser();
        var result = await _service.GetOrderTrackingAsync(id, currentUserId, role);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("create-online-payment")]
    [Authorize(Roles = "customer")]
    public async Task<IActionResult> CreateOnlinePayment([FromBody] InframartAPI_New.DTOs.CreateOnlinePaymentDto request)
    {
        var (userId, _) = GetCurrentUser();
        var result = await _service.CreateOnlinePaymentAsync(request, userId);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });
        return StatusCode(result.StatusCode, result.Data);
    }

    [HttpPost("verify-payment")]
    [Authorize(Roles = "customer")]
    public async Task<IActionResult> VerifyOnlinePayment([FromBody] InframartAPI_New.DTOs.VerifyOnlinePaymentDto request)
    {
        var (userId, _) = GetCurrentUser();
        var result = await _service.VerifyOnlinePaymentAsync(request, userId);
        if (!result.Success)
            return StatusCode(result.StatusCode, new { message = result.Message });
        return StatusCode(result.StatusCode, result.Data);
    }
}
