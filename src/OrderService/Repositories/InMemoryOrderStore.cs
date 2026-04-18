using System.Collections.Concurrent;
using OrderService.Models;

namespace OrderService.Repositories;

public sealed class InMemoryOrderStore
{
    private readonly ConcurrentDictionary<string, OrderRecord> _orders = new(StringComparer.OrdinalIgnoreCase);

    public void Upsert(OrderRecord order) => _orders[order.OrderId] = order;

    public bool TryGet(string orderId, out OrderRecord? order) => _orders.TryGetValue(orderId, out order);
}
