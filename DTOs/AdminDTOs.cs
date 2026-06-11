using System;
using System.Collections.Generic;
using MultiVendorAPI.Models;

namespace InframartAPI_New.DTOs
{
    public class UserResponseDto
    {
        public long Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }
    }

    public class UserDetailsResponseDto
    {
        public UserResponseDto User { get; set; } = new();
        public List<AddressDto> Addresses { get; set; } = new();
        public List<AdminOrderResponseDto> Orders { get; set; } = new();
    }

    public class AddressDto
    {
        public long Id { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public string? AddressType { get; set; }
        public bool IsDefault { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class AdminOrderResponseDto
    {
        public long Id { get; set; }
        public string? OrderNumber { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingCharge { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public DateTime PlacedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public long UserId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
    }

    public class UpdateUserStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class AdminVendorDetailsDto
    {
        public long VendorId { get; set; }
        public long UserId { get; set; }
        public string? ShopName { get; set; }
        public string? ShopSlug { get; set; }
        public string? Description { get; set; }
        public string? Logo { get; set; }
        public string? Banner { get; set; }
        public string? GstNumber { get; set; }
        public decimal? CommissionRate { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? VendorName { get; set; }
        public string? VendorEmail { get; set; }
        public string? VendorPhone { get; set; }
    }
}
