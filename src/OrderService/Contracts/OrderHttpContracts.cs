namespace OrderService.Contracts;

public sealed class CreateOrderHttpRequest
{
    public string? OrderId { get; init; }

    public string? CustomerId { get; init; }

    public List<OrderItemHttpRequest> Items { get; init; } = [];

    public string? Currency { get; init; }

    public string? PaymentMethod { get; init; }
}

public sealed class OrderItemHttpRequest
{
    public string? Sku { get; init; }

    public int Quantity { get; init; }

    public double UnitPrice { get; init; }
}

public sealed record CreateOrderHttpResponse(
    string OrderId,
    string Status,
    string Message,
    string InventoryReservationId,
    string PaymentId,
    double TotalAmount);

public sealed record GetOrderHttpResponse(
    string OrderId,
    string CustomerId,
    string Status,
    IReadOnlyCollection<OrderItemHttpResponse> Items,
    double TotalAmount,
    string InventoryReservationId,
    string PaymentId);

public sealed record OrderItemHttpResponse(
    string Sku,
    int Quantity,
    double UnitPrice);
