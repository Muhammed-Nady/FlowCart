FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy solution and project files for layer caching
COPY Directory.Build.props nuget.config FlowCart.slnx ./
COPY src/BuildingBlocks/FlowCart.BuildingBlocks.Domain/FlowCart.BuildingBlocks.Domain.csproj src/BuildingBlocks/FlowCart.BuildingBlocks.Domain/
COPY src/BuildingBlocks/FlowCart.BuildingBlocks.Application/FlowCart.BuildingBlocks.Application.csproj src/BuildingBlocks/FlowCart.BuildingBlocks.Application/
COPY src/BuildingBlocks/FlowCart.BuildingBlocks.Infrastructure/FlowCart.BuildingBlocks.Infrastructure.csproj src/BuildingBlocks/FlowCart.BuildingBlocks.Infrastructure/
COPY src/Services/Ordering/Ordering.Domain/Ordering.Domain.csproj src/Services/Ordering/Ordering.Domain/
COPY src/Services/Ordering/Ordering.Application/Ordering.Application.csproj src/Services/Ordering/Ordering.Application/
COPY src/Services/Ordering/Ordering.Infrastructure/Ordering.Infrastructure.csproj src/Services/Ordering/Ordering.Infrastructure/
COPY src/Services/Ordering/Ordering.API/Ordering.API.csproj src/Services/Ordering/Ordering.API/

# Restore dependencies
RUN dotnet restore src/Services/Ordering/Ordering.API/Ordering.API.csproj

# Copy source code
COPY src/BuildingBlocks/ src/BuildingBlocks/
COPY src/Services/Ordering/ src/Services/Ordering/

# Build and publish
WORKDIR /app/src/Services/Ordering/Ordering.API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
USER $APP_UID

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Ordering.API.dll"]
