FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy solution and project files for layer caching
COPY Directory.Build.props nuget.config FlowCart.slnx ./
COPY src/ApiGateway/ApiGateway.API/ApiGateway.API.csproj src/ApiGateway/ApiGateway.API/

# Restore dependencies
RUN dotnet restore src/ApiGateway/ApiGateway.API/ApiGateway.API.csproj

# Copy source code
COPY src/ApiGateway/ src/ApiGateway/

# Build and publish
WORKDIR /app/src/ApiGateway/ApiGateway.API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
USER $APP_UID

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.API.dll"]
