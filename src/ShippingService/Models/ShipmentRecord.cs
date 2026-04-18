namespace ShippingService.Models;

public sealed class ShipmentRecord
{
    public required string ShipmentId { get; init; }

    public required string OrderId { get; init; }

    public required string Status { get; init; }

    public required string TrackingNumber { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
