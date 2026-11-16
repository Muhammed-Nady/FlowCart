FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy solution and project files for layer caching
COPY Directory.Build.props nuget.config FlowCart.slnx ./
COPY src/BuildingBlocks/FlowCart.BuildingBlocks.Domain/FlowCart.BuildingBlocks.Domain.csproj src/BuildingBlocks/FlowCart.BuildingBlocks.Domain/
COPY src/BuildingBlocks/FlowCart.BuildingBlocks.Application/FlowCart.BuildingBlocks.Application.csproj src/BuildingBlocks/FlowCart.BuildingBlocks.Application/
COPY src/BuildingBlocks/FlowCart.BuildingBlocks.Infrastructure/FlowCart.BuildingBlocks.Infrastructure.csproj src/BuildingBlocks/FlowCart.BuildingBlocks.Infrastructure/
COPY src/Services/Inventory/Inventory.Domain/Inventory.Domain.csproj src/Services/Inventory/Inventory.Domain/
COPY src/Services/Inventory/Inventory.Application/Inventory.Application.csproj src/Services/Inventory/Inventory.Application/
COPY src/Services/Inventory/Inventory.Infrastructure/Inventory.Infrastructure.csproj src/Services/Inventory/Inventory.Infrastructure/
COPY src/Services/Inventory/Inventory.API/Inventory.API.csproj src/Services/Inventory/Inventory.API/

# Restore dependencies
RUN dotnet restore src/Services/Inventory/Inventory.API/Inventory.API.csproj

# Copy source code
COPY src/BuildingBlocks/ src/BuildingBlocks/
COPY src/Services/Inventory/ src/Services/Inventory/

# Build and publish
WORKDIR /app/src/Services/Inventory/Inventory.API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
USER $APP_UID

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Inventory.API.dll"]
