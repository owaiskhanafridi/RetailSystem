using System.Collections.Concurrent;
using Shared.Messaging.Contracts;
using ShippingService.Models;

namespace ShippingService.Repositories;

public sealed class InMemoryShipmentStore
{
    private readonly ConcurrentDictionary<string, ShipmentRecord> _shipmentsByOrder = new(StringComparer.OrdinalIgnoreCase);

    public ShipmentRecord UpsertFromOrder(OrderSubmittedIntegrationEvent orderSubmitted)
    {
        return _shipmentsByOrder.AddOrUpdate(
            orderSubmitted.OrderId,
            _ => CreateShipment(orderSubmitted.OrderId),
            (_, existing) => existing);
    }

    public bool TryGetByOrderId(string orderId, out ShipmentRecord? shipment) =>
        _shipmentsByOrder.TryGetValue(orderId, out shipment);

    private static ShipmentRecord CreateShipment(string orderId) =>
        new()
        {
            ShipmentId = $"shp-{Guid.NewGuid():N}",
            OrderId = orderId,
            Status = "ReadyForDispatch",
            TrackingNumber = $"TRK-{Guid.NewGuid():N}"[..18].ToUpperInvariant()
        };
}
