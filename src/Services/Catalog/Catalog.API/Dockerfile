FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy solution and project files for layer caching
COPY Directory.Build.props nuget.config FlowCart.slnx ./
COPY src/BuildingBlocks/FlowCart.BuildingBlocks.Domain/FlowCart.BuildingBlocks.Domain.csproj src/BuildingBlocks/FlowCart.BuildingBlocks.Domain/
COPY src/BuildingBlocks/FlowCart.BuildingBlocks.Application/FlowCart.BuildingBlocks.Application.csproj src/BuildingBlocks/FlowCart.BuildingBlocks.Application/
COPY src/BuildingBlocks/FlowCart.BuildingBlocks.Infrastructure/FlowCart.BuildingBlocks.Infrastructure.csproj src/BuildingBlocks/FlowCart.BuildingBlocks.Infrastructure/
COPY src/Services/Catalog/Catalog.Domain/Catalog.Domain.csproj src/Services/Catalog/Catalog.Domain/
COPY src/Services/Catalog/Catalog.Application/Catalog.Application.csproj src/Services/Catalog/Catalog.Application/
COPY src/Services/Catalog/Catalog.Infrastructure/Catalog.Infrastructure.csproj src/Services/Catalog/Catalog.Infrastructure/
COPY src/Services/Catalog/Catalog.API/Catalog.API.csproj src/Services/Catalog/Catalog.API/

# Restore dependencies
RUN dotnet restore src/Services/Catalog/Catalog.API/Catalog.API.csproj

# Copy source code
COPY src/BuildingBlocks/ src/BuildingBlocks/
COPY src/Services/Catalog/ src/Services/Catalog/

# Build and publish
WORKDIR /app/src/Services/Catalog/Catalog.API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
USER $APP_UID

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Catalog.API.dll"]
