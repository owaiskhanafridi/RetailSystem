using Grpc.Core;
using Shared.Grpc.Shipping;
using ShippingService.Repositories;

namespace ShippingService.Services;

public sealed class ShippingGrpcService(
    InMemoryShipmentStore shipmentStore) : ShippingProcessor.ShippingProcessorBase
{
    private readonly InMemoryShipmentStore _shipmentStore = shipmentStore;

    public override Task<GetShipmentReply> GetShipmentByOrder(GetShipmentByOrderRequest request, ServerCallContext context)
    {
        if (!_shipmentStore.TryGetByOrderId(request.OrderId, out var shipment) || shipment is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Shipment for order '{request.OrderId}' was not found."));
        }

        return Task.FromResult(new GetShipmentReply
        {
            ShipmentId = shipment.ShipmentId,
            OrderId = shipment.OrderId,
            Status = shipment.Status,
            TrackingNumber = shipment.TrackingNumber,
            CreatedAtUtc = shipment.CreatedAtUtc.ToString("O")
        });
    }
}
