using Grpc.Core;
using Shared.Grpc.Ordering;

namespace OrderService.Services;

public sealed class OrderGrpcService(
    OrderApplicationService orderApplicationService) : OrderProcessor.OrderProcessorBase
{
    private readonly OrderApplicationService _orderApplicationService = orderApplicationService;

    public override Task<CreateOrderReply> CreateOrder(CreateOrderRequest request, ServerCallContext context) =>
        _orderApplicationService.CreateOrderAsync(request, context.CancellationToken);

    public override Task<GetOrderReply> GetOrder(GetOrderRequest request, ServerCallContext context) =>
        Task.FromResult(_orderApplicationService.GetOrder(request.OrderId));
}
