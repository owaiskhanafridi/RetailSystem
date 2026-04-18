namespace OrderService.Models;

public sealed class OrderRecord
{
    public required string OrderId { get; init; }

    public required string CustomerId { get; init; }

    public required string Status { get; set; }

    public required string Currency { get; init; }

    public required double TotalAmount { get; init; }

    public string InventoryReservationId { get; init; } = string.Empty;

    public string PaymentId { get; init; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public List<OrderItemRecord> Items { get; init; } = [];
}

public sealed class OrderItemRecord
{
    public required string Sku { get; init; }

    public required int Quantity { get; init; }

    public required double UnitPrice { get; init; }
}
