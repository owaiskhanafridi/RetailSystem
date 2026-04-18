using MassTransit;
using Shared.Messaging.Contracts;
using ShippingService.Repositories;

namespace ShippingService.Consumers;

public sealed class OrderSubmittedConsumer(
    InMemoryShipmentStore shipmentStore,
    ILogger<OrderSubmittedConsumer> logger) : IConsumer<OrderSubmittedIntegrationEvent>
{
    private readonly InMemoryShipmentStore _shipmentStore = shipmentStore;
    private readonly ILogger<OrderSubmittedConsumer> _logger = logger;

    public Task Consume(ConsumeContext<OrderSubmittedIntegrationEvent> context)
    {
        var shipment = _shipmentStore.UpsertFromOrder(context.Message);

        _logger.LogInformation(
            "Shipment {ShipmentId} prepared for order {OrderId}.",
            shipment.ShipmentId,
            context.Message.OrderId);

        return Task.CompletedTask;
    }
}
