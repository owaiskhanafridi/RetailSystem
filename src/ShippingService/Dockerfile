FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["RetailSystem.Microservices.sln", "."]
COPY ["src/BuildingBlocks/Shared.Grpc/Protos/shipping.proto", "src/BuildingBlocks/Shared.Grpc/Protos/"]
COPY ["src/BuildingBlocks/Shared.Messaging/Shared.Messaging.csproj", "src/BuildingBlocks/Shared.Messaging/"]
COPY ["src/ShippingService/ShippingService.csproj", "src/ShippingService/"]
RUN dotnet restore "src/ShippingService/ShippingService.csproj"

COPY . .
WORKDIR "/src/src/ShippingService"
RUN dotnet publish "ShippingService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ShippingService.dll"]
