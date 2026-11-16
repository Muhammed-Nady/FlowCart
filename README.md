# FlowCart - Microservices E-Commerce Platform

A production-grade, containerized e-commerce microservices application built with **.NET 9**, **Clean Architecture**, **YARP API Gateway**, **Entity Framework Core**, **SQL Server**, **Redis**, and **Docker Compose**.

---

## Architecture Overview

```
                      ┌─────────────────────────┐
                      │      Client / Postman   │
                      └────────────┬────────────┘
                                   │ HTTP :5000
                                   ▼
                      ┌─────────────────────────┐
                      │  ApiGateway (YARP)      │
                      └──────┬─────┬─────┬──────┘
                             │     │     │
         ┌───────────────────┘     │     └───────────────────┐
         ▼                         ▼                         ▼
┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐
│ Catalog.Service │◄──────┤ Ordering.Service├──────►│Inventory.Service│
│  (Port: 8080)   │       │  (Port: 8080)   │       │  (Port: 8080)   │
└────────┬────────┘       └────────┬────────┘       └────────┬────────┘
         │                         │                         │
    ┌────┴────┐                    │                    ┌────┴────┐
    ▼         ▼                    ▼                    ▼         ▼
┌───────┐ ┌───────┐           ┌─────────┐          ┌─────────┐
│ Redis │ │ SQL DB│           │ SQL DB  │          │ SQL DB  │
└───────┘ └───────┘           └─────────┘          └─────────┘
```

Each microservice is decoupled and implements strict **Clean Architecture** with four layers:
1. `*.Domain`: Pure business entities, value objects, domain events, domain exceptions (zero external dependencies).
2. `*.Application`: CQRS commands/queries via MediatR, FluentValidation pipeline behaviors, DTOs, and interface definitions.
3. `*.Infrastructure`: EF Core `DbContext`, migrations, generic repository & Unit of Work implementations, Redis caching, and resilient HTTP client adapters.
4. `*.API`: ASP.NET Core controllers, global exception handling middleware, health checks, and dependency injection setup.

---

## Services & Components

| Service | Port (Internal / External) | Responsibility |
|---|---|---|
| **ApiGateway** | `8080` / `5000` | YARP reverse proxy, centralized request routing, health aggregation |
| **Catalog.Service** | `8080` / Internal | Product catalog, categories, pricing, Redis Cache-Aside pattern |
| **Ordering.Service** | `8080` / Internal | Order state machine (Pending, Confirmed, Canceled), checkout orchestration via resilient Polly HTTP clients |
| **Inventory.Service** | `8080` / Internal | Warehouse stock tracking, atomic stock reservation/release, optimistic concurrency control |
| **SQL Server** | `1433` / `1433` | Isolated databases per microservice (`CatalogDb`, `OrderingDb`, `InventoryDb`) |
| **Redis** | `6379` / `6379` | Distributed cache for catalog lookups and fast product reads |

---

## Technology Stack

* **Runtime & Framework:** .NET 9 (C# 13), ASP.NET Core
* **Reverse Proxy / Gateway:** YARP (Yet Another Reverse Proxy)
* **Data Access & ORM:** Microsoft SQL Server 2022, Entity Framework Core 9
* **Caching:** Redis 7 (`StackExchange.Redis`, `IDistributedCache`)
* **Resilience & Fault Tolerance:** `Microsoft.Extensions.Http.Resilience` (Polly: retries, exponential backoff, circuit breakers)
* **Validation:** FluentValidation with automatic MediatR pipeline behavior
* **Testing:** xUnit, Moq, FluentAssertions
* **Containerization:** Multi-stage Dockerfiles, Docker Compose

---

## Prerequisites

Before running the application, make sure you have installed:
* [Docker Desktop](https://www.docker.com/products/docker-desktop/) (with Docker Compose v2)
* [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) *(optional, only required for running tests or building locally outside Docker)*
* [Postman](https://www.postman.com/) or `curl` *(for testing endpoints)*

---

## Quick Start (Running with Docker Compose)

The entire system—including all microservices, API Gateway, SQL Server, and Redis—can be launched with a single command.

### 1. Clone & Navigate to Repository
```bash
git clone https://github.com/<your-username>/FlowCart.git
cd FlowCart
```

### 2. Build and Start All Containers
```bash
docker compose up -d --build
```

Docker Compose will start the services in proper sequence using health check conditions:
1. `flowcart-sqlserver` and `flowcart-redis` start first and await health probes.
2. `catalog-service`, `ordering-service`, and `inventory-service` start once databases and cache are healthy, automatically executing EF Core migrations and initial seeding.
3. `api-gateway` starts and begins routing traffic.

### 3. Verify Container Status
```bash
docker compose ps
```

All 6 containers should show `Up` / `healthy`:
```
NAME                         IMAGE                        STATUS
flowcart-api-gateway         flowcart-api-gateway         Up
flowcart-catalog-service     flowcart-catalog-service     Up
flowcart-inventory-service   flowcart-inventory-service   Up
flowcart-ordering-service    flowcart-ordering-service    Up
flowcart-redis               redis:7-alpine               Up (healthy)
flowcart-sqlserver           mcr.microsoft.com/mssql/...  Up (healthy)
```

### 4. Verify API Gateway Health
Open your browser or run:
```bash
curl http://localhost:5000/
```
Response:
```json
{
  "service": "FlowCart API Gateway",
  "status": "Online",
  "routes": {
    "catalog": "/api/products",
    "ordering": "/api/orders",
    "inventory": "/api/inventory"
  }
}
```

### 5. Stop Containers
To stop and remove containers:
```bash
docker compose down
```

To stop containers and wipe database volumes:
```bash
docker compose down -v
```

---

## Running Locally for Development (Outside Docker)

If you prefer to debug and run services directly through `dotnet run` or Visual Studio / VS Code:

### 1. Start Infrastructure Only (SQL Server & Redis)
```bash
docker compose up -d sqlserver redis
```

### 2. Run Database Migrations
Migrations run automatically on service startup, or you can apply them manually via EF Core CLI:
```bash
dotnet ef database update --project src/Services/Catalog/Catalog.Infrastructure --startup-project src/Services/Catalog/Catalog.API
dotnet ef database update --project src/Services/Ordering/Ordering.Infrastructure --startup-project src/Services/Ordering/Ordering.API
dotnet ef database update --project src/Services/Inventory/Inventory.Infrastructure --startup-project src/Services/Inventory/Inventory.API
```

### 3. Run Microservices
Run each service in separate terminal tabs:
```bash
# Terminal 1 - Catalog Service
dotnet run --project src/Services/Catalog/Catalog.API

# Terminal 2 - Inventory Service
dotnet run --project src/Services/Inventory/Inventory.API

# Terminal 3 - Ordering Service
dotnet run --project src/Services/Ordering/Ordering.API

# Terminal 4 - API Gateway
dotnet run --project src/ApiGateway/ApiGateway.API
```

---

## Testing the Microservices

### Option A: Using Postman (Recommended)

A pre-configured Postman Collection is included at the root of the repository:
[FlowCart.postman_collection.json](FlowCart.postman_collection.json)

1. Open **Postman**.
2. Click **Import** (top left) and select `FlowCart.postman_collection.json`.
3. The collection is pre-configured with:
   * `baseUrl`: `http://localhost:5000`
   * `sampleProductId`: `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` (pre-seeded Enterprise Dev Laptop)
   * Automatic environment variable chaining (creating an order auto-sets `createdOrderId` for subsequent Confirm/Cancel requests).

---

### Option B: Using cURL or PowerShell

#### 1. Browse Catalog Products
```bash
curl -X GET http://localhost:5000/api/products?pageNumber=1&pageSize=10
```

#### 2. Get Product By ID (Populates & Reads Redis Cache)
```bash
curl -X GET http://localhost:5000/api/products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa
```

#### 3. Check Real-Time Warehouse Stock
```bash
curl -X GET http://localhost:5000/api/inventory/products/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa
```

#### 4. Place an Order (End-to-End Orchestration)
The Ordering Service validates price with the Catalog Service and reserves stock in the Inventory Service:
```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "shippingAddress": {
      "street": "123 Tech Way",
      "city": "San Francisco",
      "state": "CA",
      "country": "USA",
      "zipCode": "94107"
    },
    "items": [
      {
        "productId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        "productName": "Enterprise Dev Laptop 16-inch",
        "expectedUnitPrice": 1999.99,
        "quantity": 1
      }
    ]
  }'
```

#### 5. Confirm Order
```bash
curl -X PUT http://localhost:5000/api/orders/<ORDER_ID>/confirm
```

#### 6. Cancel Order (Stock is Automatically Released)
```bash
curl -X PUT http://localhost:5000/api/orders/<ORDER_ID>/cancel \
  -H "Content-Type: application/json" \
  -d '{"reason": "Customer changed mind"}'
```

---

## Running Automated Unit Tests

The solution contains comprehensive unit test suites covering domain logic, CQRS handlers, validation rules, and controllers with mocked dependencies.

Execute all unit tests across the solution:
```bash
dotnet test
```

### Test Coverage Summary
* `FlowCart.BuildingBlocks.UnitTests`: 14 tests (Repository, Unit of Work, Result pattern, Domain Event dispatching)
* `Catalog.UnitTests`: 18 tests (Product/Category entities, CQRS handlers, Redis Cache-Aside, FluentValidation)
* `Ordering.UnitTests`: 16 tests (Order aggregate state transitions, Polly HTTP resilience, checkout orchestration)
* `Inventory.UnitTests`: 12 tests (Atomic stock reservation, optimistic concurrency, release logic)
* **Total:** 60 tests passed (0 failures).

---

## Solution Structure

```
FlowCart/
├── docker-compose.yml              # Multi-container orchestration
├── nuget.config                    # Package source configuration
├── FlowCart.slnx                   # Solution configuration
├── FlowCart.postman_collection.json# Postman test collection
├── README.md                       # Documentation
├── src/
│   ├── ApiGateway/                 # YARP Reverse Proxy
│   │   └── ApiGateway.API/
│   ├── BuildingBlocks/             # Shared Kernel
│   │   ├── FlowCart.BuildingBlocks.Domain/
│   │   ├── FlowCart.BuildingBlocks.Application/
│   │   └── FlowCart.BuildingBlocks.Infrastructure/
│   └── Services/
│       ├── Catalog/                # Catalog & Pricing Microservice
│       │   ├── Catalog.Domain/
│       │   ├── Catalog.Application/
│       │   ├── Catalog.Infrastructure/
│       │   └── Catalog.API/
│       ├── Ordering/               # Ordering & Checkout Microservice
│       │   ├── Ordering.Domain/
│       │   ├── Ordering.Application/
│       │   ├── Ordering.Infrastructure/
│       │   └── Ordering.API/
│       └── Inventory/              # Inventory & Warehouse Microservice
│           ├── Inventory.Domain/
│           ├── Inventory.Application/
│           ├── Inventory.Infrastructure/
│           └── Inventory.API/
└── tests/
    ├── BuildingBlocks/FlowCart.BuildingBlocks.UnitTests/
    └── Services/
        ├── Catalog.UnitTests/
        ├── Ordering.UnitTests/
        └── Inventory.UnitTests/
```

---

## License

This project is licensed under the MIT License.
