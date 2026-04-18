FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["RetailSystem.Microservices.sln", "."]
COPY ["src/BuildingBlocks/Shared.Grpc/Protos/inventory.proto", "src/BuildingBlocks/Shared.Grpc/Protos/"]
COPY ["src/BuildingBlocks/Shared.Grpc/Protos/order.proto", "src/BuildingBlocks/Shared.Grpc/Protos/"]
COPY ["src/BuildingBlocks/Shared.Grpc/Protos/payment.proto", "src/BuildingBlocks/Shared.Grpc/Protos/"]
COPY ["src/BuildingBlocks/Shared.Messaging/Shared.Messaging.csproj", "src/BuildingBlocks/Shared.Messaging/"]
COPY ["src/OrderService/OrderService.csproj", "src/OrderService/"]
RUN dotnet restore "src/OrderService/OrderService.csproj"

COPY . .
WORKDIR "/src/src/OrderService"
RUN dotnet publish "OrderService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderService.dll"]
