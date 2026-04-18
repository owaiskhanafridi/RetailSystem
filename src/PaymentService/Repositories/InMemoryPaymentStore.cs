using System.Collections.Concurrent;
using PaymentService.Models;

namespace PaymentService.Repositories;

public sealed class InMemoryPaymentStore
{
    private readonly ConcurrentDictionary<string, PaymentRecord> _payments = new(StringComparer.OrdinalIgnoreCase);

    public PaymentDecision Process(string orderId, string customerId, double amount, string currency, string paymentMethod)
    {
        if (amount <= 0)
        {
            return new PaymentDecision(false, string.Empty, "Rejected", "Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            return new PaymentDecision(false, string.Empty, "Rejected", "Payment method is required.");
        }

        if (paymentMethod.Contains("declined", StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentDecision(false, string.Empty, "Rejected", "Payment provider declined the transaction.");
        }

        var paymentId = $"pay-{Guid.NewGuid():N}";
        _payments[paymentId] = new PaymentRecord
        {
            PaymentId = paymentId,
            OrderId = orderId,
            CustomerId = customerId,
            Currency = currency,
            PaymentMethod = paymentMethod,
            Status = "Approved",
            Amount = amount
        };

        return new PaymentDecision(true, paymentId, "Approved", "Payment approved.");
    }
}

public sealed record PaymentDecision(bool Success, string PaymentId, string Status, string Message);
