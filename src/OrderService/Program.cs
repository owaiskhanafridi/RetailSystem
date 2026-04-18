using Grpc.Core;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using OrderService.Contracts;
using OrderService.Repositories;
using OrderService.Services;
using Shared.Grpc.Inventory;
using Shared.Grpc.Ordering;
using Shared.Grpc.Payment;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200", "https://localhost:4200"];

builder.Services.AddGrpc();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddSingleton<InMemoryOrderStore>();
builder.Services.AddScoped<OrderApplicationService>();

builder.Services.AddGrpcClient<InventoryProcessor.InventoryProcessorClient>(options =>
{
    options.Address = new Uri(GetRequiredSetting(builder.Configuration, "GrpcClients:InventoryService"));
});

builder.Services.AddGrpcClient<PaymentProcessor.PaymentProcessorClient>(options =>
{
    options.Address = new Uri(GetRequiredSetting(builder.Configuration, "GrpcClients:PaymentService"));
});

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.UsingRabbitMq((context, cfg) =>
    {
        // RabbitMQ is used only for downstream work so order placement does not wait on shipping.
        cfg.Host(GetRequiredSetting(builder.Configuration, "RabbitMq:Host"), "/", host =>
        {
            host.Username(GetRequiredSetting(builder.Configuration, "RabbitMq:Username"));
            host.Password(GetRequiredSetting(builder.Configuration, "RabbitMq:Password"));
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.UseCors("AngularApp");

app.MapGrpcService<OrderGrpcService>();
app.MapPost("/api/orders", async ([FromBody] CreateOrderHttpRequest request, OrderApplicationService orderApplicationService, HttpContext httpContext) =>
{
    try
    {
        var grpcRequest = new CreateOrderRequest
        {
            OrderId = request.OrderId ?? string.Empty,
            CustomerId = request.CustomerId ?? string.Empty,
            Currency = request.Currency ?? string.Empty,
            PaymentMethod = request.PaymentMethod ?? string.Empty
        };

        grpcRequest.Items.AddRange(request.Items.Select(item => new OrderItem
        {
            Sku = item.Sku ?? string.Empty,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        }));

        var reply = await orderApplicationService.CreateOrderAsync(grpcRequest, httpContext.RequestAborted);
        var response = new CreateOrderHttpResponse(
            reply.OrderId,
            reply.Status,
            reply.Message,
            reply.InventoryReservationId,
            reply.PaymentId,
            reply.TotalAmount);

        return string.Equals(reply.Status, "Accepted", StringComparison.OrdinalIgnoreCase)
            ? Results.Created($"/api/orders/{reply.OrderId}", response)
            : Results.Ok(response);
    }
    catch (RpcException exception)
    {
        return MapRpcException(exception);
    }
}).RequireCors("AngularApp");

app.MapGet("/api/orders/{orderId}", (string orderId, OrderApplicationService orderApplicationService) =>
{
    try
    {
        var reply = orderApplicationService.GetOrder(orderId);
        var response = new GetOrderHttpResponse(
            reply.OrderId,
            reply.CustomerId,
            reply.Status,
            reply.Items.Select(item => new OrderItemHttpResponse(item.Sku, item.Quantity, item.UnitPrice)).ToArray(),
            reply.TotalAmount,
            reply.InventoryReservationId,
            reply.PaymentId);

        return Results.Ok(response);
    }
    catch (RpcException exception)
    {
        return MapRpcException(exception);
    }
}).RequireCors("AngularApp");

app.MapGet("/", () => Results.Ok(new
{
    service = "OrderService",
    communication = "gRPC for checkout orchestration, REST for frontend clients, RabbitMQ for downstream events"
}));

Console.WriteLine("Starting OrderService...");

app.Run();

static string GetRequiredSetting(IConfiguration configuration, string key) =>
    configuration[key] ?? throw new InvalidOperationException($"Missing configuration value '{key}'.");

static IResult MapRpcException(RpcException exception) =>
    exception.StatusCode switch
    {
        StatusCode.InvalidArgument => Results.BadRequest(new { message = exception.Status.Detail }),
        StatusCode.NotFound => Results.NotFound(new { message = exception.Status.Detail }),
        _ => Results.Problem(detail: exception.Status.Detail, statusCode: StatusCodes.Status500InternalServerError)
    };
