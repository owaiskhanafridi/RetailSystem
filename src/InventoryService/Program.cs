using InventoryService.Repositories;
using InventoryService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddSingleton<InMemoryInventoryStore>();

var app = builder.Build();

app.MapGrpcService<InventoryGrpcService>();
app.MapGet("/", () => Results.Ok(new
{
    service = "InventoryService",
    communication = "gRPC"
}));

app.Run();
