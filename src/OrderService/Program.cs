using MassTransit;
using OrderService.Repositories;
using OrderService.Services;
using Shared.Grpc.Inventory;
using Shared.Grpc.Payment;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddSingleton<InMemoryOrderStore>();

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

app.MapGrpcService<OrderGrpcService>();
app.MapGet("/", () => Results.Ok(new
{
    service = "OrderService",
    communication = "gRPC for checkout orchestration, RabbitMQ for downstream events"
}));

app.Run();

static string GetRequiredSetting(IConfiguration configuration, string key) =>
    configuration[key] ?? throw new InvalidOperationException($"Missing configuration value '{key}'.");
