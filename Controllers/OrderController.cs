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
        CreateOrder(CreateOrderDto dto)
    {
        var result =
            await _service.CreateOrderAsync(dto);

        return StatusCode(
            result.StatusCode,
            result);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult>
        GetOrdersByUserId(long userId)
    {
        var result =
            await _service.GetOrdersByUserIdAsync(userId);

        return StatusCode(
            result.StatusCode,
            result);
    }

}