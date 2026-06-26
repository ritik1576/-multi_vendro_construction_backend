using MultiVendorAPI.Common;
using MultiVendorAPI.Models;
using MultiVendorAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using MultiVendorAPI.Services.Interfaces;

public class OrderServices : IOrderService
{
    private const string AssumedVendorName = "Tata Steel";
    private const decimal DeliveryCharge = 99;

    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly InframartAPI_New.Data.AppDbContext _appDbContext;
    private readonly MultiVendorAPI.Data.ApplicationDbContext _applicationDbContext;
    private readonly InframartAPI_New.Services.Interfaces.INotificationService _notificationService;
    private readonly ICouponService _couponService;
    private readonly InframartAPI_New.Services.Interfaces.IEmailNotificationService _emailNotificationService;

    public OrderServices(
        IOrderRepository orderRepository,
        ICartRepository cartRepository,
        InframartAPI_New.Data.AppDbContext appDbContext,
        MultiVendorAPI.Data.ApplicationDbContext applicationDbContext,
        InframartAPI_New.Services.Interfaces.INotificationService notificationService,
        ICouponService couponService,
        InframartAPI_New.Services.Interfaces.IEmailNotificationService emailNotificationService)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _appDbContext = appDbContext;
        _applicationDbContext = applicationDbContext;
        _notificationService = notificationService;
        _couponService = couponService;
        _emailNotificationService = emailNotificationService;
    }

    public async Task<ServiceResponse<PlaceOrderResponseDto>>
        CreateOrderAsync(CreateOrderDto dto)
    {
        Console.WriteLine($"[DEBUG CreateOrderAsync] Starting order placement. UserId: {dto.UserId}, PaymentMethod: '{dto.PaymentMethod}', Items Count: {dto.Items.Count}");

        if (dto.UserId <= 0 || dto.AddressId <= 0 || dto.Items.Count == 0)
        {
            Console.WriteLine("[DEBUG CreateOrderAsync] Invalid order data validation failed.");
            return ServiceResponse<PlaceOrderResponseDto>
                .FailureResponse("Invalid order data", 400);
        }

        var cart = await _cartRepository.GetByUserIdWithItemsAsync(dto.UserId);
        if (cart == null)
        {
            Console.WriteLine($"[DEBUG CreateOrderAsync] Cart not found for user {dto.UserId}");
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

            var availableStock = product.Quantity ?? 0;
            if (availableStock < item.Quantity)
            {
                return ServiceResponse<PlaceOrderResponseDto>
                    .FailureResponse(
                        $"Insufficient stock for '{product.Name}'. " +
                        $"Available: {availableStock}, Requested: {item.Quantity}",
                        400);
            }

            var price = product.DiscountPrice.HasValue && product.DiscountPrice.Value > 0
                ? product.DiscountPrice.Value
                : product.Price.GetValueOrDefault();
            subtotal += price * item.Quantity;
            products.Add(product);
        }

        var now = DateTime.Now;
        decimal discountAmount = 0;
        long? couponId = null;

        if (!string.IsNullOrWhiteSpace(dto.CouponCode))
        {
            var couponValidation = await _couponService.ValidateCouponAsync(dto.CouponCode, dto.UserId);
            if (!couponValidation.Valid || couponValidation.Coupon == null)
            {
                return ServiceResponse<PlaceOrderResponseDto>
                    .FailureResponse(couponValidation.Message ?? "Invalid coupon", 400);
            }
            couponId = couponValidation.Coupon.Id;
            discountAmount = await _couponService.CalculateDiscountAsync(couponValidation.Coupon, subtotal);
        }

        Order order = null!;
        bool isWalletPayment = string.Equals(dto.PaymentMethod?.Trim(), "Wallet", StringComparison.OrdinalIgnoreCase);
        Console.WriteLine($"[DEBUG CreateOrderAsync] isWalletPayment evaluated to: {isWalletPayment} (Input: '{dto.PaymentMethod}')");

        if (isWalletPayment)
        {
            decimal orderAmount = subtotal - discountAmount + DeliveryCharge;
            decimal commissionAmount = orderAmount * 0.10m;
            decimal vendorAmount = orderAmount - commissionAmount;

            Console.WriteLine($"[DEBUG CreateOrderAsync] Wallet calculation - subtotal: {subtotal}, discount: {discountAmount}, delivery: {DeliveryCharge}, orderAmount: {orderAmount}, commissionAmount: {commissionAmount}, vendorAmount: {vendorAmount}");

            var customerWallet = await _applicationDbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == dto.UserId);
            if (customerWallet == null)
            {
                Console.WriteLine($"[DEBUG CreateOrderAsync] Customer wallet NOT found for UserId: {dto.UserId}");
                return ServiceResponse<PlaceOrderResponseDto>.FailureResponse("Customer wallet not found.", 404);
            }
            Console.WriteLine($"[DEBUG CreateOrderAsync] Customer wallet found. Available Balance: {customerWallet.AvailableBalance}");

            if (customerWallet.AvailableBalance < orderAmount)
            {
                Console.WriteLine($"[DEBUG CreateOrderAsync] Insufficient balance. Required: {orderAmount}, Available: {customerWallet.AvailableBalance}");
                await _notificationService.CreateNotificationAsync(dto.UserId, "Wallet Payment Failed", "Wallet payment failed due to insufficient balance.", "payment");
                await _notificationService.CreateNotificationAsync(dto.UserId, "Order Payment Failed", "Payment failed for your order.", "payment");
                return ServiceResponse<PlaceOrderResponseDto>.FailureResponse("Insufficient Balance", 400);
            }

            var vendorId = products.FirstOrDefault(p => p.VendorId.HasValue)?.VendorId;
            if (vendorId == null)
            {
                Console.WriteLine("[DEBUG CreateOrderAsync] Vendor ID not found in ordered items.");
                return ServiceResponse<PlaceOrderResponseDto>.FailureResponse("Vendor not found for the ordered items.", 400);
            }
            var vendorObj = await _appDbContext.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId.Value);
            if (vendorObj == null || !vendorObj.UserId.HasValue)
            {
                Console.WriteLine($"[DEBUG CreateOrderAsync] Vendor object not found or UserId missing for VendorId: {vendorId}");
                return ServiceResponse<PlaceOrderResponseDto>.FailureResponse("Vendor user ID not found.", 400);
            }
            long vendorUserId = vendorObj.UserId.Value;

            var vendorWallet = await _applicationDbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == vendorUserId);
            if (vendorWallet == null)
            {
                Console.WriteLine($"[DEBUG CreateOrderAsync] Vendor wallet NOT found for VendorUserId: {vendorUserId}");
                return ServiceResponse<PlaceOrderResponseDto>.FailureResponse("Vendor wallet not found.", 400);
            }
            Console.WriteLine($"[DEBUG CreateOrderAsync] Vendor wallet found. Balance: {vendorWallet.AvailableBalance}");

            var adminUser = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Role == "admin");
            if (adminUser == null)
            {
                Console.WriteLine("[DEBUG CreateOrderAsync] Admin user not found.");
                return ServiceResponse<PlaceOrderResponseDto>.FailureResponse("Admin user not found.", 500);
            }
            var adminWallet = await _applicationDbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == adminUser.Id);
            if (adminWallet == null)
            {
                Console.WriteLine($"[DEBUG CreateOrderAsync] Admin wallet NOT found for AdminUserId: {adminUser.Id}");
                return ServiceResponse<PlaceOrderResponseDto>.FailureResponse("Admin wallet not found.", 500);
            }
            Console.WriteLine($"[DEBUG CreateOrderAsync] Admin wallet found. Balance: {adminWallet.AvailableBalance}");

            using var dbTransaction = await _applicationDbContext.Database.BeginTransactionAsync();
            try
            {
                Console.WriteLine("[DEBUG CreateOrderAsync] DB Transaction started.");
                var custBefore = customerWallet.AvailableBalance;
                customerWallet.AvailableBalance -= orderAmount;
                customerWallet.TotalDebits += orderAmount;
                _applicationDbContext.Wallets.Update(customerWallet);

                var vendBefore = vendorWallet.AvailableBalance;
                vendorWallet.AvailableBalance += vendorAmount;
                vendorWallet.TotalCredits += vendorAmount;
                _applicationDbContext.Wallets.Update(vendorWallet);

                var adminBefore = adminWallet.AvailableBalance;
                adminWallet.AvailableBalance += commissionAmount;
                adminWallet.TotalCredits += commissionAmount;
                _applicationDbContext.Wallets.Update(adminWallet);

                await _applicationDbContext.SaveChangesAsync();

                order = new Order
                {
                    UserId = dto.UserId,
                    AddressId = dto.AddressId,
                    Subtotal = subtotal,
                    DiscountAmount = discountAmount,
                    ShippingCharge = DeliveryCharge,
                    TotalAmount = orderAmount,
                    CouponId = couponId,
                    CouponCode = dto.CouponCode,
                    PaymentStatus = "paid",
                    OrderStatus = "pending",
                    PlacedAt = now,
                    CreatedAt = now,
                    SubtotalAmount = subtotal,
                    CommissionAmount = commissionAmount,
                    VendorAmount = vendorAmount,
                    FinalAmount = orderAmount
                };

                await _orderRepository.CreateOrderAsync(order);
                await _orderRepository.SaveChangesAsync();

                order.OrderNumber = FormatOrderNumber(order.Id);

                for (var i = 0; i < dto.Items.Count; i++)
                {
                    var item = dto.Items[i];
                    var product = products[i];
                    var price = product.DiscountPrice.HasValue && product.DiscountPrice.Value > 0
                        ? product.DiscountPrice.Value
                        : product.Price.GetValueOrDefault();

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

                    product.Quantity = (product.Quantity ?? 0) - item.Quantity;
                    if (product.Quantity <= 0)
                    {
                        product.Quantity = 0;
                        product.InStock = false;
                    }
                }

                if (couponId.HasValue)
                {
                    var couponUsage = new CouponUsage
                    {
                        CouponId = couponId.Value,
                        UserId = dto.UserId,
                        OrderId = order.Id,
                        UsedAt = now
                    };
                    await _applicationDbContext.CouponUsages.AddAsync(couponUsage);

                    var coupon = await _applicationDbContext.Coupons.FindAsync(couponId.Value);
                    if (coupon != null)
                    {
                        coupon.UsedCount = (coupon.UsedCount ?? 0) + 1;
                    }
                }

                var cartItems = cart.CartItems.ToList();
                await _cartRepository.RemoveCartItemsAsync(cartItems);

                var customerUser = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId);
                string customerName = customerUser?.FullName ?? "Customer";
                string vendorName = vendorObj.ShopName ?? "Tata Steel";

                var transactionId = "ORD" + System.Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();

                var custTxn = new InframartAPI_New.Models.WalletTransaction
                {
                    TransactionId = transactionId,
                    WalletId = customerWallet.Id,
                    TransactionType = InframartAPI_New.Models.TransactionType.Purchase,
                    Direction = InframartAPI_New.Models.TransactionDirection.Debit,
                    Amount = orderAmount,
                    BalanceBefore = custBefore,
                    BalanceAfter = customerWallet.AvailableBalance,
                    AvailableBefore = custBefore,
                    AvailableAfter = customerWallet.AvailableBalance,
                    LockedBefore = customerWallet.LockedBalance,
                    LockedAfter = customerWallet.LockedBalance,
                    Status = InframartAPI_New.Models.TransactionStatus.Success,
                    Title = vendorName,
                    Description = $"Order Payment - Order #{order.OrderNumber}",
                    ReferenceType = "Order",
                    ReferenceId = order.OrderNumber,
                    CreatedAt = now,
                    CreatedBy = "System"
                };

                var vendTxn = new InframartAPI_New.Models.WalletTransaction
                {
                    TransactionId = transactionId,
                    WalletId = vendorWallet.Id,
                    TransactionType = InframartAPI_New.Models.TransactionType.Purchase,
                    Direction = InframartAPI_New.Models.TransactionDirection.Credit,
                    Amount = vendorAmount,
                    BalanceBefore = vendBefore,
                    BalanceAfter = vendorWallet.AvailableBalance,
                    AvailableBefore = vendBefore,
                    AvailableAfter = vendorWallet.AvailableBalance,
                    LockedBefore = vendorWallet.LockedBalance,
                    LockedAfter = vendorWallet.LockedBalance,
                    Status = InframartAPI_New.Models.TransactionStatus.Success,
                    Title = customerName,
                    Description = $"Order Received Payment - Order #{order.OrderNumber}",
                    ReferenceType = "Order",
                    ReferenceId = order.OrderNumber,
                    CreatedAt = now,
                    CreatedBy = "System"
                };

                var adminTxn = new InframartAPI_New.Models.WalletTransaction
                {
                    TransactionId = transactionId,
                    WalletId = adminWallet.Id,
                    TransactionType = InframartAPI_New.Models.TransactionType.Purchase,
                    Direction = InframartAPI_New.Models.TransactionDirection.Credit,
                    Amount = commissionAmount,
                    BalanceBefore = adminBefore,
                    BalanceAfter = adminWallet.AvailableBalance,
                    AvailableBefore = adminBefore,
                    AvailableAfter = adminWallet.AvailableBalance,
                    LockedBefore = adminWallet.LockedBalance,
                    LockedAfter = adminWallet.LockedBalance,
                    Status = InframartAPI_New.Models.TransactionStatus.Success,
                    Title = $"Commission - Order #{order.OrderNumber}",
                    Description = "Marketplace Commission",
                    ReferenceType = "Order",
                    ReferenceId = order.OrderNumber,
                    CreatedAt = now,
                    CreatedBy = "System"
                };

                await _applicationDbContext.WalletTransactions.AddAsync(custTxn);
                await _applicationDbContext.WalletTransactions.AddAsync(vendTxn);
                await _applicationDbContext.WalletTransactions.AddAsync(adminTxn);

                await _applicationDbContext.SaveChangesAsync();

                await dbTransaction.CommitAsync();

                // Trigger Wallet Payment Success Notifications
                try
                {
                    // Customer
                    await _notificationService.CreateNotificationAsync(order.UserId, "Wallet Payment Successful", $"Wallet payment of {orderAmount} INR was successful for order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());
                    await _notificationService.CreateNotificationAsync(order.UserId, "Order Payment Successful", $"Payment of {orderAmount} INR was successful for order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());

                    // Vendor
                    await _notificationService.CreateNotificationAsync(vendorUserId, "Payment Received", $"Payment of {vendorAmount} INR received for order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());
                    await _notificationService.CreateNotificationAsync(vendorUserId, "Commission Deducted", $"Commission of {commissionAmount} INR deducted for order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());
                    await _notificationService.CreateNotificationAsync(vendorUserId, "Order Payment Received", $"Order payment of {vendorAmount} INR received for order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());

                    // Admin
                    if (adminUser != null)
                    {
                        await _notificationService.CreateNotificationAsync(adminUser.Id, "New Payment Received", $"Payment of {orderAmount} INR received for order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());
                        await _notificationService.CreateNotificationAsync(adminUser.Id, "Commission Received", $"Commission of {commissionAmount} INR received for order {order.OrderNumber}.", "payment", "Order", order.Id.ToString());
                    }
                }
                catch (Exception exVal)
                {
                    Console.WriteLine($"[DEBUG CreateOrderAsync] Notification trigger failed for wallet payment success: {exVal.Message}");
                }

                // Assign the local variable so that code after block can return correctly
                dto.UserId = order.UserId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG CreateOrderAsync] EXCEPTION CAUGHT during wallet transaction processing: {ex}");
                await dbTransaction.RollbackAsync();

                try
                {
                    await _notificationService.CreateNotificationAsync(dto.UserId, "Wallet Payment Failed", $"Wallet payment failed: {ex.Message}", "payment");
                    await _notificationService.CreateNotificationAsync(dto.UserId, "Order Payment Failed", "Payment failed for your order.", "payment");
                }
                catch {}

                return ServiceResponse<PlaceOrderResponseDto>.FailureResponse($"Failed to place order using Wallet: {ex.Message}", 500);
            }
        }
        else
        {
            decimal orderAmount = subtotal - discountAmount + DeliveryCharge;
            decimal commissionAmount = orderAmount * 0.10m;
            decimal vendorAmount = orderAmount - commissionAmount;

            order = new Order
            {
                UserId = dto.UserId,
                AddressId = dto.AddressId,
                Subtotal = subtotal,
                DiscountAmount = discountAmount,
                ShippingCharge = DeliveryCharge,
                TotalAmount = orderAmount,
                CouponId = couponId,
                CouponCode = dto.CouponCode,
                PaymentStatus = "pending",
                OrderStatus = "pending",
                PlacedAt = now,
                CreatedAt = now,
                SubtotalAmount = subtotal,
                CommissionAmount = commissionAmount,
                VendorAmount = vendorAmount,
                FinalAmount = orderAmount
            };

            await _orderRepository.CreateOrderAsync(order);
            await _orderRepository.SaveChangesAsync();

            order.OrderNumber = FormatOrderNumber(order.Id);

            for (var i = 0; i < dto.Items.Count; i++)
            {
                var item = dto.Items[i];
                var product = products[i];
                var price = product.DiscountPrice.HasValue && product.DiscountPrice.Value > 0
                    ? product.DiscountPrice.Value
                    : product.Price.GetValueOrDefault();

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

            if (couponId.HasValue)
            {
                var couponUsage = new CouponUsage
                {
                    CouponId = couponId.Value,
                    UserId = dto.UserId,
                    OrderId = order.Id,
                    UsedAt = now
                };
                await _applicationDbContext.CouponUsages.AddAsync(couponUsage);

                var coupon = await _applicationDbContext.Coupons.FindAsync(couponId.Value);
                if (coupon != null)
                {
                    coupon.UsedCount = (coupon.UsedCount ?? 0) + 1;
                }
            }

            // Clear cart items
            var cartItems = cart.CartItems.ToList();
            await _cartRepository.RemoveCartItemsAsync(cartItems);

            await _orderRepository.SaveChangesAsync();
        }

        // Trigger notifications
        try
        {
            // Customer notification
            await _notificationService.CreateNotificationAsync(order.UserId, "Order Placed", $"Your order {order.OrderNumber} has been placed successfully.", "order");

            // Send ORDER_CREATED email to customer
            try
            {
                var customerUser = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Id == order.UserId);
                if (customerUser != null && !string.IsNullOrEmpty(customerUser.Email))
                {
                    await _emailNotificationService.SendTemplateEmailAsync(
                        "ORDER_CREATED",
                        customerUser.Email,
                        new Dictionary<string, string>
                        {
                            { "customer_name", customerUser.FullName ?? "Customer" },
                            { "order_number", order.OrderNumber ?? $"INFR-LOCAL-{order.Id:000}" },
                            { "order_amount", order.TotalAmount.ToString("F2") }
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send order placed email: {ex.Message}");
            }

            // Vendors notification
            var vendorIds = products.Where(p => p.VendorId.HasValue).Select(p => p.VendorId!.Value).Distinct().ToList();
            if (vendorIds.Count > 0)
            {
                var vendorUserIds = await _appDbContext.Vendors
                    .Where(v => vendorIds.Contains(v.Id) && v.UserId.HasValue)
                    .Select(v => v.UserId!.Value)
                    .ToListAsync();

                foreach (var vendorUserId in vendorUserIds)
                {
                    await _notificationService.CreateNotificationAsync(vendorUserId, "New Order Received", $"You have received a new order {order.OrderNumber}.", "order");
                }
            }

            // Admins notification
            var adminUserIds = await _appDbContext.Users
                .Where(u => u.Role == "admin")
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var adminUserId in adminUserIds)
            {
                await _notificationService.CreateNotificationAsync(adminUserId, "New Order Placed", $"A new order {order.OrderNumber} has been placed.", "order");
            }
        }
        catch (Exception ex)
        {
            // We shouldn't fail the order if notifications fail
            Console.WriteLine($"Notification trigger failed for order placement: {ex.Message}");
        }

        var response = new PlaceOrderResponseDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            TotalAmount = order.TotalAmount,
            OrderStatus = order.OrderStatus,
            PaymentStatus = order.PaymentStatus,
            PlacedAt = InframartAPI_New.Helpers.TimezoneHelper.ConvertToIst(order.PlacedAt)
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
            CreatedAt = InframartAPI_New.Helpers.TimezoneHelper.ConvertToIst(order.PlacedAt == default ? order.CreatedAt : order.PlacedAt),
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

            try
            {
                await _notificationService.CreateNotificationAsync(order.UserId, "Order Cancelled", $"Your order {GetOrderNumber(order)} has been cancelled.", "order");

                // Send ORDER_CANCELLED email to customer
                try
                {
                    var customerUser = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Id == order.UserId);
                    if (customerUser != null && !string.IsNullOrEmpty(customerUser.Email))
                    {
                        await _emailNotificationService.SendTemplateEmailAsync(
                            "ORDER_CANCELLED",
                            customerUser.Email,
                            new Dictionary<string, string>
                            {
                                { "customer_name", customerUser.FullName ?? "Customer" },
                                { "order_number", GetOrderNumber(order) }
                            }
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send order cancelled email: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Notification failed on order cancellation: {ex.Message}");
            }
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
            PlacedAt = InframartAPI_New.Helpers.TimezoneHelper.ConvertToIst(order.PlacedAt == default ? order.CreatedAt : order.PlacedAt),
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
                PlacedAt = InframartAPI_New.Helpers.TimezoneHelper.ConvertToIst(order.PlacedAt == default ? order.CreatedAt : order.PlacedAt),
                CreatedAt = InframartAPI_New.Helpers.TimezoneHelper.ConvertToIst(order.CreatedAt),
                CustomerId = order.UserId,
                CustomerName = customer?.FullName,
                CustomerEmail = customer?.Email,
                CustomerPhone = customer?.Phone,
                OrderItems = order.OrderItems.Select(item =>
                {
                    products.TryGetValue(item.ProductId, out var product);
                    InframartAPI_New.Models.Vendor? vendor = null;
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
                        VendorStatus = vendor?.Status.ToString()
                    };
                }).ToList()
            };
        }).ToList();

        return ServiceResponse<List<OrderWithItemsDto>>
            .SuccessResponse(result, "All orders with items retrieved successfully");
    }
}
