FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["RetailSystem.Microservices.sln", "."]
COPY ["src/BuildingBlocks/Shared.Grpc/Shared.Grpc.csproj", "src/BuildingBlocks/Shared.Grpc/"]
COPY ["src/PaymentService/PaymentService.csproj", "src/PaymentService/"]
RUN dotnet restore "src/PaymentService/PaymentService.csproj"

COPY . .
WORKDIR "/src/src/PaymentService"
RUN dotnet publish "PaymentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PaymentService.dll"]
