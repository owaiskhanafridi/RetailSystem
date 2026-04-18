using Grpc.Core;
using MassTransit;
using OrderService.Models;
using OrderService.Repositories;
using Shared.Grpc.Inventory;
using Shared.Grpc.Ordering;
using Shared.Grpc.Payment;
using Shared.Messaging.Contracts;

namespace OrderService.Services;

public sealed class OrderGrpcService(
    InventoryProcessor.InventoryProcessorClient inventoryClient,
    PaymentProcessor.PaymentProcessorClient paymentClient,
    InMemoryOrderStore orderStore,
    IPublishEndpoint publishEndpoint,
    ILogger<OrderGrpcService> logger) : OrderProcessor.OrderProcessorBase
{
    private readonly InventoryProcessor.InventoryProcessorClient _inventoryClient = inventoryClient;
    private readonly PaymentProcessor.PaymentProcessorClient _paymentClient = paymentClient;
    private readonly InMemoryOrderStore _orderStore = orderStore;
    private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;
    private readonly ILogger<OrderGrpcService> _logger = logger;

    public override async Task<CreateOrderReply> CreateOrder(CreateOrderRequest request, ServerCallContext context)
    {
        if (request.Items.Count == 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "At least one order item is required."));
        }

        if (string.IsNullOrWhiteSpace(request.CustomerId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "CustomerId is required."));
        }

        var orderId = string.IsNullOrWhiteSpace(request.OrderId) ? $"ord-{Guid.NewGuid():N}" : request.OrderId;
        var totalAmount = request.Items.Sum(item => item.UnitPrice * item.Quantity);

        // Inventory and payment stay on gRPC because checkout needs an immediate success/failure decision.
        var inventoryReply = await _inventoryClient.ReserveInventoryAsync(
            new ReserveInventoryRequest
            {
                OrderId = orderId,
                Items =
                {
                    request.Items.Select(item => new InventoryItemReservation
                    {
                        Sku = item.Sku,
                        Quantity = item.Quantity
                    })
                }
            },
            cancellationToken: context.CancellationToken);

        if (!inventoryReply.Success)
        {
            var rejectedOrder = CreateOrderRecord(orderId, request, totalAmount, "InventoryRejected", inventoryReply.ReservationId, string.Empty);
            _orderStore.Upsert(rejectedOrder);

            return new CreateOrderReply
            {
                OrderId = orderId,
                Status = rejectedOrder.Status,
                Message = inventoryReply.Message,
                InventoryReservationId = inventoryReply.ReservationId,
                TotalAmount = totalAmount
            };
        }

        var paymentReply = await _paymentClient.ProcessPaymentAsync(
            new ProcessPaymentRequest
            {
                OrderId = orderId,
                CustomerId = request.CustomerId,
                Amount = totalAmount,
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency,
                PaymentMethod = request.PaymentMethod
            },
            cancellationToken: context.CancellationToken);

        if (!paymentReply.Success)
        {
            await _inventoryClient.ReleaseInventoryAsync(
                new ReleaseInventoryRequest
                {
                    OrderId = orderId,
                    ReservationId = inventoryReply.ReservationId
                },
                cancellationToken: context.CancellationToken);

            var rejectedOrder = CreateOrderRecord(orderId, request, totalAmount, "PaymentRejected", string.Empty, paymentReply.PaymentId);
            _orderStore.Upsert(rejectedOrder);

            return new CreateOrderReply
            {
                OrderId = orderId,
                Status = rejectedOrder.Status,
                Message = paymentReply.Message,
                PaymentId = paymentReply.PaymentId,
                TotalAmount = totalAmount
            };
        }

        var acceptedOrder = CreateOrderRecord(orderId, request, totalAmount, "Accepted", inventoryReply.ReservationId, paymentReply.PaymentId);
        _orderStore.Upsert(acceptedOrder);

        // Shipping is asynchronous because it is important, but not part of the customer-facing checkout latency budget.
        await _publishEndpoint.Publish(new OrderSubmittedIntegrationEvent
        {
            OrderId = acceptedOrder.OrderId,
            CustomerId = acceptedOrder.CustomerId,
            Currency = acceptedOrder.Currency,
            InventoryReservationId = acceptedOrder.InventoryReservationId,
            PaymentId = acceptedOrder.PaymentId,
            TotalAmount = acceptedOrder.TotalAmount,
            Items = acceptedOrder.Items.Select(item => new OrderSubmittedItem
            {
                Sku = item.Sku,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        }, context.CancellationToken);

        _logger.LogInformation("Order {OrderId} accepted and published for fulfillment.", acceptedOrder.OrderId);

        return new CreateOrderReply
        {
            OrderId = acceptedOrder.OrderId,
            Status = acceptedOrder.Status,
            Message = "Order accepted and published for shipping.",
            InventoryReservationId = acceptedOrder.InventoryReservationId,
            PaymentId = acceptedOrder.PaymentId,
            TotalAmount = acceptedOrder.TotalAmount
        };
    }

    public override Task<GetOrderReply> GetOrder(GetOrderRequest request, ServerCallContext context)
    {
        if (!_orderStore.TryGet(request.OrderId, out var order) || order is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Order '{request.OrderId}' was not found."));
        }

        var reply = new GetOrderReply
        {
            OrderId = order.OrderId,
            CustomerId = order.CustomerId,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            InventoryReservationId = order.InventoryReservationId,
            PaymentId = order.PaymentId
        };

        reply.Items.AddRange(order.Items.Select(item => new OrderItem
        {
            Sku = item.Sku,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        }));

        return Task.FromResult(reply);
    }

    private static OrderRecord CreateOrderRecord(
        string orderId,
        CreateOrderRequest request,
        double totalAmount,
        string status,
        string inventoryReservationId,
        string paymentId) =>
        new()
        {
            OrderId = orderId,
            CustomerId = request.CustomerId,
            Status = status,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency,
            TotalAmount = totalAmount,
            InventoryReservationId = inventoryReservationId,
            PaymentId = paymentId,
            Items = request.Items.Select(item => new OrderItemRecord
            {
                Sku = item.Sku,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };
}
