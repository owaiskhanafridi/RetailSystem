using Grpc.Core;
using PaymentService.Repositories;
using Shared.Grpc.Payment;

namespace PaymentService.Services;

public sealed class PaymentGrpcService(
    InMemoryPaymentStore paymentStore,
    ILogger<PaymentGrpcService> logger) : PaymentProcessor.PaymentProcessorBase
{
    private readonly InMemoryPaymentStore _paymentStore = paymentStore;
    private readonly ILogger<PaymentGrpcService> _logger = logger;

    public override Task<ProcessPaymentReply> ProcessPayment(ProcessPaymentRequest request, ServerCallContext context)
    {
        var decision = _paymentStore.Process(
            request.OrderId,
            request.CustomerId,
            request.Amount,
            string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency,
            request.PaymentMethod);

        _logger.LogInformation("Payment attempt for order {OrderId} returned {Status}.", request.OrderId, decision.Status);

        return Task.FromResult(new ProcessPaymentReply
        {
            Success = decision.Success,
            PaymentId = decision.PaymentId,
            Status = decision.Status,
            Message = decision.Message
        });
    }
}
