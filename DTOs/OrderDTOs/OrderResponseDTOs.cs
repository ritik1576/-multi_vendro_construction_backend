public class PlaceOrderResponseDto
{
    public long Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime PlacedAt { get; set; }
}

public class OrderDetailsDto
{
    public long Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public DateTime PlacedAt { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string DisplayStatus { get; set; } = string.Empty;
    public List<OrderItemDetailsDto> Items { get; set; } = new();
    public OrderAmountDto Amount { get; set; } = new();
    public DeliveryAddressDto DeliveryAddress { get; set; } = new();
    public PaymentMethodDto PaymentMethod { get; set; } = new();
    public List<string> Vendors { get; set; } = new();
    public OrderTrackingDto Tracking { get; set; } = new();
}

public class OrderItemDetailsDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalPrice { get; set; }
    public string UnitLabel { get; set; } = string.Empty;
}

public class OrderAmountDto
{
    public decimal ItemsSubtotal { get; set; }
    public decimal Delivery { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class DeliveryAddressDto
{
    public long AddressId { get; set; }
    public string ContactName { get; set; } = "Site Manager";
    public string AddressLine { get; set; } = "Construction Site, Zone A";
    public string CityStatePincode { get; set; } = "Mumbai, Maharashtra 400001";
    public string ContactPhone { get; set; } = "+91 9876543210";
}

public class PaymentMethodDto
{
    public string Method { get; set; } = "COD";
    public string Description { get; set; } = "Pay on delivery";
    public string PaymentStatus { get; set; } = string.Empty;
}

public class OrderTrackingDto
{
    public long OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public string CurrentStage { get; set; } = string.Empty;
    public List<TrackingStepDto> Steps { get; set; } = new();
}

public class TrackingStepDto
{
    public int Step { get; set; }
    public string Title { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class OrderWithItemsDto
{
    public long OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingCharge { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public DateTime PlacedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Customer
    public long CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    
    // Items
    public List<OrderItemWithVendorDto> OrderItems { get; set; } = new();
}

public class OrderItemWithVendorDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalPrice { get; set; }
    
    // Vendor Info
    public long? VendorId { get; set; }
    public string? VendorName { get; set; }
    public string? VendorStatus { get; set; }
}
