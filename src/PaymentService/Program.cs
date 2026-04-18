using PaymentService.Repositories;
using PaymentService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddSingleton<InMemoryPaymentStore>();

var app = builder.Build();

app.MapGrpcService<PaymentGrpcService>();
app.MapGet("/", () => Results.Ok(new
{
    service = "PaymentService",
    communication = "gRPC"
}));

Console.WriteLine("Starting PaymentService...");
app.Run();
