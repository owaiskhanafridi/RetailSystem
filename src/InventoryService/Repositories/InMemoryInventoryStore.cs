namespace InventoryService.Repositories;

public sealed class InMemoryInventoryStore
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, int> _stock = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LAPTOP-15"] = 25,
        ["PHONE-01"] = 40,
        ["MOUSE-USB"] = 120,
        ["KEYBOARD-MECH"] = 75
    };

    private readonly Dictionary<string, InventoryReservation> _reservations = new(StringComparer.OrdinalIgnoreCase);

    public InventoryReservationResult Reserve(string orderId, IReadOnlyCollection<RequestedInventoryItem> items)
    {
        lock (_syncRoot)
        {
            if (items.Count == 0)
            {
                return new InventoryReservationResult(false, string.Empty, "At least one inventory item is required.", GetSnapshot());
            }

            foreach (var item in items)
            {
                if (item.Quantity <= 0)
                {
                    return new InventoryReservationResult(false, string.Empty, $"Quantity for {item.Sku} must be greater than zero.", GetSnapshot());
                }

                if (!_stock.TryGetValue(item.Sku, out var available) || available < item.Quantity)
                {
                    return new InventoryReservationResult(false, string.Empty, $"Insufficient stock for {item.Sku}.", GetSnapshot());
                }
            }

            var reservationId = $"res-{Guid.NewGuid():N}";
            _reservations[reservationId] = new InventoryReservation(
                reservationId,
                orderId,
                items.Select(item => new RequestedInventoryItem(item.Sku, item.Quantity)).ToList());

            //Remove stocks from inventory
            foreach (var item in items)
            {
                _stock[item.Sku] -= item.Quantity;
            }

            return new InventoryReservationResult(true, reservationId, "Inventory reserved.", GetSnapshot());
        }
    }

    public InventoryReleaseResult Release(string reservationId, string orderId)
    {
        lock (_syncRoot)
        {
            if (!_reservations.TryGetValue(reservationId, out var reservation))
            {
                return new InventoryReleaseResult(false, $"Reservation '{reservationId}' was not found.", GetSnapshot());
            }

            if (!string.Equals(reservation.OrderId, orderId, StringComparison.OrdinalIgnoreCase))
            {
                return new InventoryReleaseResult(false, "Reservation does not belong to the supplied order.", GetSnapshot());
            }

            foreach (var item in reservation.Items)
            {
                _stock[item.Sku] = _stock.GetValueOrDefault(item.Sku) + item.Quantity;
            }

            _reservations.Remove(reservationId);
            return new InventoryReleaseResult(true, "Inventory released.", GetSnapshot());
        }
    }

    public IReadOnlyCollection<InventoryStockItem> Snapshot()
    {
        lock (_syncRoot)
        {
            return GetSnapshot();
        }
    }

    private List<InventoryStockItem> GetSnapshot() =>
        _stock
            .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select(item => new InventoryStockItem(item.Key, item.Value))
            .ToList();
}

public sealed record RequestedInventoryItem(string Sku, int Quantity);

public sealed record InventoryStockItem(string Sku, int AvailableQuantity);

public sealed record InventoryReservation(string ReservationId, string OrderId, IReadOnlyCollection<RequestedInventoryItem> Items);

public sealed record InventoryReservationResult(
    bool Success,
    string ReservationId,
    string Message,
    IReadOnlyCollection<InventoryStockItem> RemainingStock);

public sealed record InventoryReleaseResult(
    bool Success,
    string Message,
    IReadOnlyCollection<InventoryStockItem> CurrentStock);
