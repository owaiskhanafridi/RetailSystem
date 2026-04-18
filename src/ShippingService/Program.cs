using MassTransit;
using ShippingService.Consumers;
using ShippingService.Repositories;
using ShippingService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddSingleton<InMemoryShipmentStore>();

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<OrderSubmittedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        // Shipping consumes events asynchronously so temporary shipping failures do not reject accepted orders.
        cfg.Host(GetRequiredSetting(builder.Configuration, "RabbitMq:Host"), "/", host =>
        {
            host.Username(GetRequiredSetting(builder.Configuration, "RabbitMq:Username"));
            host.Password(GetRequiredSetting(builder.Configuration, "RabbitMq:Password"));
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.MapGrpcService<ShippingGrpcService>();
app.MapGet("/", () => Results.Ok(new
{
    service = "ShippingService",
    communication = "RabbitMQ consumer with gRPC query endpoint"
}));

app.Run();

static string GetRequiredSetting(IConfiguration configuration, string key) =>
    configuration[key] ?? throw new InvalidOperationException($"Missing configuration value '{key}'.");
