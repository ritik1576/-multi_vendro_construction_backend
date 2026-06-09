namespace InframartAPI_New.Models
{
    public class Order
    {
        public long Id { get; set; }

        public long? User_Id { get; set; }

        public long? Address_Id { get; set; }

        public long? Coupon_Id { get; set; }

        public string? Order_Number { get; set; }

        public decimal? Subtotal { get; set; }

        public decimal? Discount_Amount { get; set; }

        public decimal? Shipping_Charge { get; set; }

        public decimal? Total_Amount { get; set; }

        public string? Payment_Status { get; set; }

        public string? Order_Status { get; set; }

        public DateTime? Placed_At { get; set; }

        public DateTime? Created_At { get; set; }
    }
}
