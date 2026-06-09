using InframartAPI_New.Data;
using InframartAPI_New.DTOs.VendorDTOs;
using InframartAPI_New.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using MultiVendorAPI.Data;

namespace InframartAPI_New.Services
{
    public class VendorOrderService : IVendorOrderService
    {
        private readonly AppDbContext _authCtx;          // users, vendors (auth side)
        private readonly ApplicationDbContext _appCtx;   // orders, products, addresses

        public VendorOrderService(AppDbContext authCtx, ApplicationDbContext appCtx)
        {
            _authCtx = authCtx;
            _appCtx = appCtx;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /vendor/status?userId={id}
        // ─────────────────────────────────────────────────────────────────────
        public async Task<(bool success, string? error, VendorStatusDto? data)>
            GetVendorStatusAsync(long userId)
        {
            var user = await _authCtx.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return (false, "User not found", null);

            var vendor = await _authCtx.Vendors.FirstOrDefaultAsync(v => v.UserId == userId);
            if (vendor == null)
                return (false, "Vendor profile not found for this user", null);

            return (true, null, new VendorStatusDto
            {
                UserId      = user.Id,
                VendorId    = vendor.Id,
                ShopName    = vendor.ShopName,
                ShopSlug    = vendor.ShopSlug,
                Description = vendor.Description,
                Logo        = vendor.Logo,
                Banner      = vendor.Banner,
                GstNumber   = vendor.GstNumber,
                CommissionRate = vendor.CommissionRate,
                Status      = vendor.Status,
                CreatedAt   = vendor.CreatedAt,
                UpdatedAt   = vendor.UpdatedAt,
                Email       = user.Email,
                Phone       = user.Phone
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /vendor/orders
        // ─────────────────────────────────────────────────────────────────────
        public async Task<(bool success, string? error, List<VendorOrderListDto>? data)>
            GetVendorOrdersAsync(long vendorId)
        {
            // Find all order-items whose product belongs to this vendor
            var vendorProductIds = await _appCtx.Products
                .Where(p => p.VendorId == vendorId)
                .Select(p => p.Id)
                .ToListAsync();

            if (!vendorProductIds.Any())
                return (true, null, new List<VendorOrderListDto>());

            // Distinct order IDs that contain at least one vendor product
            var orderIds = await _appCtx.OrderItems
                .Where(oi => vendorProductIds.Contains(oi.ProductId))
                .Select(oi => oi.OrderId)
                .Distinct()
                .ToListAsync();

            var orders = await _appCtx.Orders
                .Where(o => orderIds.Contains(o.Id))
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.PlacedAt)
                .ToListAsync();

            // Product lookup for this vendor's items
            var products = await _appCtx.Products
                .Where(p => vendorProductIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            // User lookup (customers) — from auth context
            var customerIds = orders.Select(o => o.UserId).Distinct().ToList();
            var customers   = await _authCtx.Users
                .Where(u => customerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            var result = orders.Select(order =>
            {
                // Only items belonging to this vendor
                var vendorItems = order.OrderItems
                    .Where(oi => vendorProductIds.Contains(oi.ProductId))
                    .ToList();

                customers.TryGetValue(order.UserId, out var customer);

                return new VendorOrderListDto
                {
                    OrderId       = order.Id,
                    OrderNumber   = order.OrderNumber,
                    PlacedAt      = order.PlacedAt,
                    OrderStatus   = order.OrderStatus,
                    PaymentStatus = order.PaymentStatus,
                    Subtotal      = order.Subtotal,
                    ShippingCharge = order.ShippingCharge,
                    DiscountAmount = order.DiscountAmount,
                    TotalAmount   = order.TotalAmount,
                    TotalItems    = vendorItems.Count,
                    CustomerId    = order.UserId,
                    CustomerName  = customer?.FullName,
                    CustomerEmail = customer?.Email,
                    CustomerPhone = customer?.Phone,
                    Items = vendorItems.Select(oi =>
                    {
                        products.TryGetValue(oi.ProductId, out var prod);
                        return new VendorOrderItemSummaryDto
                        {
                            OrderItemId       = oi.Id,
                            ProductId         = oi.ProductId,
                            ProductName       = oi.ProductName,
                            ProductSku        = prod?.Sku,
                            ProductThumbnail  = prod?.Thumbnail,
                            Quantity          = oi.Quantity,
                            UnitPrice         = oi.Price,
                            TotalPrice        = oi.TotalPrice
                        };
                    }).ToList()
                };
            }).ToList();

            return (true, null, result);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /vendor/orders/{orderId}
        // ─────────────────────────────────────────────────────────────────────
        public async Task<(bool success, string? error, VendorOrderDetailDto? data)>
            GetVendorOrderDetailAsync(long vendorId, long orderId)
        {
            var vendorProductIds = await _appCtx.Products
                .Where(p => p.VendorId == vendorId)
                .Select(p => p.Id)
                .ToListAsync();

            var order = await _appCtx.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return (false, "Order not found", null);

            // Verify this order contains at least one vendor product
            var hasVendorItem = order.OrderItems.Any(oi => vendorProductIds.Contains(oi.ProductId));
            if (!hasVendorItem)
                return (false, "Order not found for this vendor", null);

            // Customer
            var customer = await _authCtx.Users.FirstOrDefaultAsync(u => u.Id == order.UserId);

            // Delivery address
            var address = await _appCtx.Addresses.FirstOrDefaultAsync(a => a.Id == order.AddressId);

            // Product details for vendor items only
            var vendorItemIds = order.OrderItems
                .Where(oi => vendorProductIds.Contains(oi.ProductId))
                .Select(oi => oi.ProductId)
                .ToList();

            var products = await _appCtx.Products
                .Include(p => p.Category)
                .Where(p => vendorItemIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var dto = new VendorOrderDetailDto
            {
                OrderId       = order.Id,
                OrderNumber   = order.OrderNumber,
                OrderStatus   = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,
                PlacedAt      = order.PlacedAt,
                CreatedAt     = order.CreatedAt,
                Subtotal      = order.Subtotal,
                ShippingCharge = order.ShippingCharge,
                DiscountAmount = order.DiscountAmount,
                TotalAmount   = order.TotalAmount,

                Customer = new VendorOrderCustomerDto
                {
                    UserId   = order.UserId,
                    FullName = customer?.FullName,
                    Email    = customer?.Email,
                    Phone    = customer?.Phone
                },

                DeliveryAddress = address == null ? new VendorOrderAddressDto() : new VendorOrderAddressDto
                {
                    AddressId    = address.Id,
                    FullName     = address.FullName,
                    Phone        = address.Phone,
                    AddressLine1 = address.AddressLine1,
                    AddressLine2 = address.AddressLine2,
                    City         = address.City,
                    State        = address.State,
                    Country      = address.Country,
                    PostalCode   = address.PostalCode
                },

                Items = order.OrderItems
                    .Where(oi => vendorProductIds.Contains(oi.ProductId))
                    .Select(oi =>
                    {
                        products.TryGetValue(oi.ProductId, out var prod);
                        return new VendorOrderItemDetailDto
                        {
                            OrderItemId = oi.Id,
                            Quantity    = oi.Quantity,
                            UnitPrice   = oi.Price,
                            TotalPrice  = oi.TotalPrice,
                            Product = new VendorProductInfoDto
                            {
                                ProductId        = oi.ProductId,
                                Name             = prod?.Name ?? oi.ProductName,
                                Slug             = prod?.Slug,
                                Sku              = prod?.Sku,
                                ShortDescription = prod?.ShortDescription,
                                Description      = prod?.Description,
                                Price            = prod?.Price,
                                DiscountPrice    = prod?.DiscountPrice,
                                Thumbnail        = prod?.Thumbnail,
                                Status           = prod?.Status,
                                InStock          = prod?.InStock,
                                StockQuantity    = prod?.Quantity,
                                CategoryName     = prod?.Category?.Name
                            }
                        };
                    }).ToList()
            };

            return (true, null, dto);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /vendor/orders/{orderId}/status
        // ─────────────────────────────────────────────────────────────────────
        public async Task<(bool success, string? error)>
            UpdateOrderStatusAsync(long vendorId, long orderId, UpdateOrderStatusDto dto)
        {
            var allowed = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
            if (!allowed.Contains(dto.OrderStatus, StringComparer.OrdinalIgnoreCase))
                return (false, $"Invalid status. Allowed: {string.Join(", ", allowed)}");

            var vendorProductIds = await _appCtx.Products
                .Where(p => p.VendorId == vendorId)
                .Select(p => p.Id)
                .ToListAsync();

            var order = await _appCtx.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return (false, "Order not found");

            if (!order.OrderItems.Any(oi => vendorProductIds.Contains(oi.ProductId)))
                return (false, "Order not found for this vendor");

            order.OrderStatus = dto.OrderStatus;
            await _appCtx.SaveChangesAsync();

            return (true, null);
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE /vendor/orders/{orderId}
        // ─────────────────────────────────────────────────────────────────────
        public async Task<(bool success, string? error)>
            DeleteOrderAsync(long vendorId, long orderId)
        {
            var vendorProductIds = await _appCtx.Products
                .Where(p => p.VendorId == vendorId)
                .Select(p => p.Id)
                .ToListAsync();

            var order = await _appCtx.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return (false, "Order not found");

            if (!order.OrderItems.Any(oi => vendorProductIds.Contains(oi.ProductId)))
                return (false, "Order not found for this vendor");

            _appCtx.Orders.Remove(order);
            await _appCtx.SaveChangesAsync();

            return (true, null);
        }
    }
}
