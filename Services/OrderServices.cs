using MultiVendorAPI.Common;
using MultiVendorAPI.Models;
using MultiVendorAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

public class OrderServices : IOrderService
{
    private const string AssumedVendorName = "Tata Steel";
    private const decimal DeliveryCharge = 99;

    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly InframartAPI_New.Data.AppDbContext _appDbContext;
    private readonly MultiVendorAPI.Data.ApplicationDbContext _applicationDbContext;

    public OrderServices(
        IOrderRepository orderRepository,
        ICartRepository cartRepository,
        InframartAPI_New.Data.AppDbContext appDbContext,
        MultiVendorAPI.Data.ApplicationDbContext applicationDbContext)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _appDbContext = appDbContext;
        _applicationDbContext = applicationDbContext;
    }

    public async Task<ServiceResponse<PlaceOrderResponseDto>>
        CreateOrderAsync(CreateOrderDto dto)
    {
        if (dto.UserId <= 0 || dto.AddressId <= 0 || dto.Items.Count == 0)
        {
            return ServiceResponse<PlaceOrderResponseDto>
                .FailureResponse("Invalid order data", 400);
        }

        var cart = await _cartRepository.GetByUserIdWithItemsAsync(dto.UserId);
        if (cart == null)
        {
            return ServiceResponse<PlaceOrderResponseDto>
                .FailureResponse("Cart not found for user", 404);
        }

        decimal subtotal = 0;
        var products = new List<Product>();

        foreach (var item in dto.Items)
        {
            if (item.Quantity <= 0)
            {
                return ServiceResponse<PlaceOrderResponseDto>
                    .FailureResponse("Item quantity must be greater than zero", 400);
            }

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == item.CartItemId);

            if (cartItem == null)
            {
                return ServiceResponse<PlaceOrderResponseDto>
                    .FailureResponse($"Cart item {item.CartItemId} not found in user's cart", 404);
            }

            var product = cartItem.Product;

            if (product == null)
            {
                product = await _orderRepository.GetProductByIdAsync(cartItem.ProductId);
            }

            if (product == null)
            {
                return ServiceResponse<PlaceOrderResponseDto>
                    .FailureResponse($"Product {cartItem.ProductId} not found", 404);
            }

            // ── Stock check ────────────────────────────────────────────────
            var availableStock = product.Quantity ?? 0;
            if (availableStock < item.Quantity)
            {
                return ServiceResponse<PlaceOrderResponseDto>
                    .FailureResponse(
                        $"Insufficient stock for '{product.Name}'. " +
                        $"Available: {availableStock}, Requested: {item.Quantity}",
                        400);
            }

            subtotal += product.Price.GetValueOrDefault() * item.Quantity;
            products.Add(product);
        }

        var now = DateTime.UtcNow;
        var order = new Order
        {
            UserId = dto.UserId,
            AddressId = dto.AddressId,
            Subtotal = subtotal,
            DiscountAmount = 0,
            ShippingCharge = DeliveryCharge,
            TotalAmount = subtotal + DeliveryCharge,
            PaymentStatus = "pending",
            OrderStatus = "pending",
            PlacedAt = now,
            CreatedAt = now
        };

        await _orderRepository.CreateOrderAsync(order);
        await _orderRepository.SaveChangesAsync();

        order.OrderNumber = FormatOrderNumber(order.Id);

        for (var i = 0; i < dto.Items.Count; i++)
        {
            var item = dto.Items[i];
            var product = products[i];
            var price = product.Price.GetValueOrDefault();

            await _orderRepository.CreateOrderItemAsync(new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = item.Quantity,
                ProductName = product.Name ?? string.Empty,
                Price = price,
                TotalPrice = price * item.Quantity,
                CreatedAt = now
            });

            // ── Deduct stock ───────────────────────────────────────────────
            product.Quantity = (product.Quantity ?? 0) - item.Quantity;
            if (product.Quantity <= 0)
            {
                product.Quantity = 0;
                product.InStock = false;
            }
        }

        await _orderRepository.SaveChangesAsync();

        var response = new PlaceOrderResponseDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            TotalAmount = order.TotalAmount,
            OrderStatus = order.OrderStatus,
            PaymentStatus = order.PaymentStatus,
            PlacedAt = order.PlacedAt
        };

        return ServiceResponse<PlaceOrderResponseDto>
            .SuccessResponse(response, "Order placed successfully", 201);
    }

    public async Task<ServiceResponse<List<OrderListDto>>> GetOrdersAsync(long? userId)
    {
        var orders = await _orderRepository.GetOrdersAsync(userId);

        var result = orders.Select(order => new OrderListDto
        {
            Id = order.Id,
            OrderNumber = GetOrderNumber(order),
            VendorName = AssumedVendorName,
            TotalAmount = order.TotalAmount,
            OrderStatus = order.OrderStatus,
            DisplayStatus = ToDisplayStatus(order.OrderStatus),
            CreatedAt = order.PlacedAt == default ? order.CreatedAt : order.PlacedAt,
            ItemCount = order.OrderItems.Count
        }).ToList();

        return ServiceResponse<List<OrderListDto>>
            .SuccessResponse(result, "Orders retrieved successfully");
    }

    public async Task<ServiceResponse<OrderDetailsDto>> GetOrderDetailsAsync(long orderId, long currentUserId, string userRole)
    {
        var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId);

        if (order == null)
        {
            return ServiceResponse<OrderDetailsDto>
                .FailureResponse("Order not found", 404);
        }

        if (userRole != "admin" && order.UserId != currentUserId)
        {
            return ServiceResponse<OrderDetailsDto>
                .FailureResponse("Access denied to this order", 403);
        }

        return ServiceResponse<OrderDetailsDto>
            .SuccessResponse(MapToDetails(order), "Order details retrieved successfully");
    }

    public async Task<ServiceResponse<OrderDetailsDto>> CancelOrderAsync(long orderId, long currentUserId, string userRole)
    {
        var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId);

        if (order == null)
        {
            return ServiceResponse<OrderDetailsDto>
                .FailureResponse("Order not found", 404);
        }

        if (userRole != "admin" && order.UserId != currentUserId)
        {
            return ServiceResponse<OrderDetailsDto>
                .FailureResponse("Access denied to cancel this order", 403);
        }

        if (order.OrderStatus == "delivered")
        {
            return ServiceResponse<OrderDetailsDto>
                .FailureResponse("Delivered orders cannot be cancelled", 400);
        }

        if (order.OrderStatus != "cancelled")
        {
            order.OrderStatus = "cancelled";
            order.PaymentStatus = order.PaymentStatus == "paid"
                ? "refunded"
                : order.PaymentStatus;

            await _orderRepository.SaveChangesAsync();
        }

        return ServiceResponse<OrderDetailsDto>
            .SuccessResponse(MapToDetails(order), "Order cancelled successfully");
    }

    public async Task<ServiceResponse<OrderTrackingDto>> GetOrderTrackingAsync(long orderId, long currentUserId, string userRole)
    {
        var order = await _orderRepository.GetOrderByIdWithItemsAsync(orderId);

        if (order == null)
        {
            return ServiceResponse<OrderTrackingDto>
                .FailureResponse("Order not found", 404);
        }

        if (userRole != "admin" && order.UserId != currentUserId)
        {
            return ServiceResponse<OrderTrackingDto>
                .FailureResponse("Access denied to this order tracking", 403);
        }

        return ServiceResponse<OrderTrackingDto>
            .SuccessResponse(MapToTracking(order), "Order tracking retrieved successfully");
    }

    private static OrderDetailsDto MapToDetails(Order order)
    {
        var orderNumber = GetOrderNumber(order);
        var subtotal = order.Subtotal != 0
            ? order.Subtotal
            : order.OrderItems.Sum(item => item.TotalPrice);

        return new OrderDetailsDto
        {
            Id = order.Id,
            OrderNumber = orderNumber,
            VendorName = AssumedVendorName,
            PlacedAt = order.PlacedAt == default ? order.CreatedAt : order.PlacedAt,
            OrderStatus = order.OrderStatus,
            DisplayStatus = ToDisplayStatus(order.OrderStatus),
            Items = order.OrderItems.Select(item => new OrderItemDetailsDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                VendorName = AssumedVendorName,
                Quantity = item.Quantity,
                Price = item.Price,
                TotalPrice = item.TotalPrice,
                UnitLabel = GetUnitLabel(item.ProductName)
            }).ToList(),
            Amount = new OrderAmountDto
            {
                ItemsSubtotal = subtotal,
                Delivery = order.ShippingCharge,
                DiscountAmount = order.DiscountAmount,
                TotalAmount = order.TotalAmount
            },
            DeliveryAddress = new DeliveryAddressDto
            {
                AddressId = order.AddressId
            },
            PaymentMethod = new PaymentMethodDto
            {
                PaymentStatus = order.PaymentStatus
            },
            Vendors = new List<string> { AssumedVendorName },
            Tracking = MapToTracking(order)
        };
    }

    private static OrderTrackingDto MapToTracking(Order order)
    {
        var currentStep = GetCurrentStep(order.OrderStatus);

        return new OrderTrackingDto
        {
            OrderId = order.Id,
            OrderNumber = GetOrderNumber(order),
            OrderStatus = order.OrderStatus,
            CurrentStage = GetStepTitle(currentStep),
            Steps = Enumerable.Range(1, 5)
                .Select(step => new TrackingStepDto
                {
                    Step = step,
                    Title = GetStepTitle(step),
                    State = GetStepState(step, currentStep, order.OrderStatus),
                    Description = GetStepDescription(step, currentStep, order.OrderStatus)
                })
                .ToList()
        };
    }

    private static string GetOrderNumber(Order order)
    {
        return string.IsNullOrWhiteSpace(order.OrderNumber)
            ? FormatOrderNumber(order.Id)
            : order.OrderNumber;
    }

    private static string FormatOrderNumber(long orderId)
    {
        return $"INFR-LOCAL-{orderId:000}";
    }

    private static string ToDisplayStatus(string? status)
    {
        return status switch
        {
            "pending" => "Vendor Confirmation Pending",
            "confirmed" => "Confirmed",
            "shipped" => "Packed",
            "delivered" => "Delivered",
            "cancelled" => "Cancelled",
            _ => "Vendor Confirmation Pending"
        };
    }

    private static int GetCurrentStep(string? status)
    {
        return status switch
        {
            "pending" => 1,
            "confirmed" => 2,
            "shipped" => 3,
            "delivered" => 5,
            "cancelled" => 1,
            _ => 1
        };
    }

    private static string GetStepTitle(int step)
    {
        return step switch
        {
            1 => "Order Placed",
            2 => "Confirmed",
            3 => "Packed",
            4 => "Out for Delivery",
            5 => "Delivered",
            _ => string.Empty
        };
    }

    private static string GetStepState(int step, int currentStep, string? status)
    {
        if (status == "cancelled")
        {
            return step == 1 ? "cancelled" : "upcoming";
        }

        if (step < currentStep)
        {
            return "completed";
        }

        return step == currentStep ? "current" : "upcoming";
    }

    private static string GetStepDescription(int step, int currentStep, string? status)
    {
        if (status == "cancelled" && step == 1)
        {
            return "Order cancelled";
        }

        if (step < currentStep)
        {
            return "Step completed";
        }

        return step == currentStep
            ? "Current processing stage"
            : "Upcoming step";
    }

    private static string GetUnitLabel(string productName)
    {
        var name = productName.ToLower();

        if (name.Contains("cement"))
        {
            return "bag";
        }

        if (name.Contains("sand"))
        {
            return "cubic feet";
        }

        return "piece";
    }

    public async Task<ServiceResponse<List<OrderWithItemsDto>>> GetAllOrdersWithItemsAsync()
    {
        var orders = await _orderRepository.GetOrdersAsync(null);

        var customerIds = orders.Select(o => o.UserId).Distinct().ToList();
        var productIds = orders.SelectMany(o => o.OrderItems).Select(oi => oi.ProductId).Distinct().ToList();

        var customers = await _appDbContext.Users
            .Where(u => customerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var products = await _applicationDbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var vendorIds = products.Values.Where(p => p.VendorId.HasValue).Select(p => p.VendorId!.Value).Distinct().ToList();
        
        var vendors = await _appDbContext.Vendors
            .Where(v => vendorIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id);

        var result = orders.Select(order =>
        {
            customers.TryGetValue(order.UserId, out var customer);

            return new OrderWithItemsDto
            {
                OrderId = order.Id,
                OrderNumber = GetOrderNumber(order),
                Subtotal = order.Subtotal,
                TotalAmount = order.TotalAmount,
                DiscountAmount = order.DiscountAmount,
                ShippingCharge = order.ShippingCharge,
                PaymentStatus = order.PaymentStatus,
                OrderStatus = order.OrderStatus,
                PlacedAt = order.PlacedAt == default ? order.CreatedAt : order.PlacedAt,
                CreatedAt = order.CreatedAt,
                CustomerId = order.UserId,
                CustomerName = customer?.FullName,
                CustomerEmail = customer?.Email,
                CustomerPhone = customer?.Phone,
                OrderItems = order.OrderItems.Select(item =>
                {
                    products.TryGetValue(item.ProductId, out var product);
                    global::Vendor? vendor = null;
                    if (product != null && product.VendorId.HasValue)
                    {
                        vendors.TryGetValue(product.VendorId.Value, out vendor);
                    }

                    return new OrderItemWithVendorDto
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        TotalPrice = item.TotalPrice,
                        VendorId = product?.VendorId,
                        VendorName = vendor?.ShopName,
                        VendorStatus = vendor?.Status
                    };
                }).ToList()
            };
        }).ToList();

        return ServiceResponse<List<OrderWithItemsDto>>
            .SuccessResponse(result, "All orders with items retrieved successfully");
    }
}
