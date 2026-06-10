namespace InframartAPI_New.DTOs.VendorDTOs
{
    // ─── Vendor Status ────────────────────────────────────────────────────────
    public class VendorStatusDto
    {
        public long UserId { get; set; }
        public long VendorId { get; set; }
        public string? ShopName { get; set; }
        public string? ShopSlug { get; set; }
        public string? Description { get; set; }
        public string? Logo { get; set; }
        public string? Banner { get; set; }
        public string? GstNumber { get; set; }
        public decimal? CommissionRate { get; set; }
        public string? Status { get; set; }          // pending | active | suspended
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // joined user info
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    // ─── Order List (summary rows) ────────────────────────────────────────────
    public class VendorOrderListDto
    {
        public long OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public DateTime PlacedAt { get; set; }
        public string? OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ShippingCharge { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }

        // customer info
        public long CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }

        public List<VendorOrderItemSummaryDto> Items { get; set; } = new();
    }

    public class VendorOrderItemSummaryDto
    {
        public long OrderItemId { get; set; }
        public long ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductSku { get; set; }
        public string? ProductThumbnail { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    // ─── Order Detail (full info) ─────────────────────────────────────────────
    public class VendorOrderDetailDto
    {
        public long OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public string? OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime PlacedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        // amounts
        public decimal Subtotal { get; set; }
        public decimal ShippingCharge { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }

        // customer
        public VendorOrderCustomerDto Customer { get; set; } = new();

        // delivery address
        public VendorOrderAddressDto DeliveryAddress { get; set; } = new();

        // items with full product info
        public List<VendorOrderItemDetailDto> Items { get; set; } = new();
    }

    public class VendorOrderCustomerDto
    {
        public long UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    public class VendorOrderAddressDto
    {
        public long AddressId { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
    }

    public class VendorOrderItemDetailDto
    {
        public long OrderItemId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }

        // full product info
        public VendorProductInfoDto Product { get; set; } = new();
    }

    public class VendorProductInfoDto
    {
        public long ProductId { get; set; }
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public string? Sku { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? Thumbnail { get; set; }
        public string? Status { get; set; }
        public bool? InStock { get; set; }
        public int? StockQuantity { get; set; }
        public string? CategoryName { get; set; }
    }

    // ─── Update Order Status ──────────────────────────────────────────────────
    public class UpdateOrderStatusDto
    {
        /// <summary>
        /// Allowed: pending | confirmed | shipped | delivered | cancelled
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }

    // ─── Dashboard ────────────────────────────────────────────────────────────
    public class VendorDashboardDto
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ConfirmedOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalProducts { get; set; }
    }
}
