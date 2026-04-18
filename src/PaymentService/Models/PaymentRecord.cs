namespace PaymentService.Models;

public sealed class PaymentRecord
{
    public required string PaymentId { get; init; }

    public required string OrderId { get; init; }

    public required string CustomerId { get; init; }

    public required string Currency { get; init; }

    public required string PaymentMethod { get; init; }

    public required string Status { get; init; }

    public required double Amount { get; init; }

    public DateTimeOffset ProcessedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
