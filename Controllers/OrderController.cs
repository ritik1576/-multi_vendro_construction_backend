using Microsoft.AspNetCore.Mvc;


namespace MultiVendorAPI.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;

    public OrdersController(IOrderService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult>
        CreateOrder([FromBody] CreateOrderDto dto)
    {
        var result =
            await _service.CreateOrderAsync(dto);

        return StatusCode(
            result.StatusCode,
            result);
    }

    [HttpGet("all/{userId:long}")]
    public async Task<IActionResult> GetOrders(long userId)
    {
        var result = await _service.GetOrdersAsync(userId);

        return StatusCode(
            result.StatusCode,
            result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetOrderDetails(long id)
    {
        var result = await _service.GetOrderDetailsAsync(id);

        return StatusCode(
            result.StatusCode,
            result);
    }

    [HttpPut("{id:long}/cancel")]
    public async Task<IActionResult> CancelOrder(long id)
    {
        var result = await _service.CancelOrderAsync(id);

        return StatusCode(
            result.StatusCode,
            result);
    }

    [HttpGet("{id:long}/tracking")]
    public async Task<IActionResult> GetOrderTracking(long id)
    {
        var result = await _service.GetOrderTrackingAsync(id);

        return StatusCode(
            result.StatusCode,
            result);
    }

}
