using Grpc.Core;
using InventoryService.Repositories;
using Shared.Grpc.Inventory;

namespace InventoryService.Services;

public sealed class InventoryGrpcService(
    InMemoryInventoryStore inventoryStore,
    ILogger<InventoryGrpcService> logger) : InventoryProcessor.InventoryProcessorBase
{
    private readonly InMemoryInventoryStore _inventoryStore = inventoryStore;
    private readonly ILogger<InventoryGrpcService> _logger = logger;

    public override Task<ReserveInventoryReply> ReserveInventory(ReserveInventoryRequest request, ServerCallContext context)
    {
        var result = _inventoryStore.Reserve(
            request.OrderId,
            request.Items.Select(item => new RequestedInventoryItem(item.Sku, item.Quantity)).ToList());

        _logger.LogInformation("Inventory reservation for order {OrderId} returned {Success}.", request.OrderId, result.Success);

        var reply = new ReserveInventoryReply
        {
            Success = result.Success,
            ReservationId = result.ReservationId,
            Message = result.Message
        };

        reply.RemainingStock.AddRange(result.RemainingStock.Select(item => new InventoryStockLevel
        {
            Sku = item.Sku,
            AvailableQuantity = item.AvailableQuantity
        }));

        return Task.FromResult(reply);
    }

    public override Task<ReleaseInventoryReply> ReleaseInventory(ReleaseInventoryRequest request, ServerCallContext context)
    {
        var result = _inventoryStore.Release(request.ReservationId, request.OrderId);

        _logger.LogInformation("Inventory release for reservation {ReservationId} returned {Success}.", request.ReservationId, result.Success);

        var reply = new ReleaseInventoryReply
        {
            Success = result.Success,
            Message = result.Message
        };

        reply.CurrentStock.AddRange(result.CurrentStock.Select(item => new InventoryStockLevel
        {
            Sku = item.Sku,
            AvailableQuantity = item.AvailableQuantity
        }));

        return Task.FromResult(reply);
    }

    public override Task<GetInventorySnapshotReply> GetInventorySnapshot(GetInventorySnapshotRequest request, ServerCallContext context)
    {
        var snapshot = _inventoryStore.Snapshot();
        var reply = new GetInventorySnapshotReply();

        reply.Items.AddRange(snapshot.Select(item => new InventoryStockLevel
        {
            Sku = item.Sku,
            AvailableQuantity = item.AvailableQuantity
        }));

        return Task.FromResult(reply);
    }
}
