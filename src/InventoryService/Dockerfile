FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["RetailSystem.Microservices.sln", "."]
COPY ["src/BuildingBlocks/Shared.Grpc/Protos/inventory.proto", "src/BuildingBlocks/Shared.Grpc/Protos/"]
COPY ["src/InventoryService/InventoryService.csproj", "src/InventoryService/"]
RUN dotnet restore "src/InventoryService/InventoryService.csproj"

COPY . .
WORKDIR "/src/src/InventoryService"
RUN dotnet publish "InventoryService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InventoryService.dll"]
