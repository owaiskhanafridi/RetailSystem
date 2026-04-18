# RetailSystem Microservices

This solution contains four independently deployable .NET 8 microservices in one Visual Studio solution:

| Service | Responsibility | Communication |
| --- | --- | --- |
| OrderService | Accepts orders and orchestrates checkout | gRPC + RabbitMQ |
| PaymentService | Processes payment decisions | gRPC |
| InventoryService | Reserves and releases stock | gRPC |
| ShippingService | Creates shipments after successful orders | RabbitMQ consumer + gRPC query |

## Why RabbitMQ

RabbitMQ fits the shipping workflow well because shipment creation is downstream work that should be retried independently instead of holding the order request open. The order path still uses gRPC for payment and inventory because checkout needs immediate answers from those services.

## Solution structure

```text
RetailSystem.Microservices.sln
src/
  BuildingBlocks/
    Shared.Grpc/
    Shared.Messaging/
  OrderService/
  PaymentService/
  InventoryService/
  ShippingService/
```

## Running locally

1. Start the full stack with Docker Compose:

   ```powershell
   docker compose up --build
   ```

2. Or run the services directly with `dotnet run` from each project. The development settings use these ports:

   - OrderService: `http://localhost:5101`
   - PaymentService: `http://localhost:5102`
   - InventoryService: `http://localhost:5103`
   - ShippingService: `http://localhost:5104`
   - RabbitMQ: `localhost:5672`

## Notes

- Each service has its own `Dockerfile`, so they can be built and deployed independently even though they live in one solution.
- The sample keeps persistence in memory to focus on the service boundaries, communication patterns, and deployment shape.
