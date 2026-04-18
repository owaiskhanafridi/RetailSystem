namespace Shared.Messaging.Contracts;

public sealed class OrderSubmittedIntegrationEvent
{
    public string OrderId { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;

    public string Currency { get; set; } = string.Empty;

    public string InventoryReservationId { get; set; } = string.Empty;

    public string PaymentId { get; set; } = string.Empty;

    public double TotalAmount { get; set; }

    public List<OrderSubmittedItem> Items { get; set; } = [];
}

public sealed class OrderSubmittedItem
{
    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public double UnitPrice { get; set; }
}
