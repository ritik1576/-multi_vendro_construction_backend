using System.Text.Json.Serialization;

public class CreateOrderItemDto
{
    public long CartItemId { get; set; }

    [JsonPropertyName("cart_item_id")]
    public long CartItemIdSnakeCase { get => CartItemId; set => CartItemId = value; }

    public int Quantity { get; set; }
}