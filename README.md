<div align="center">

# 🛒 FlowCart

### Production-grade Microservices E-Commerce Platform

A fully containerized, cloud-ready e-commerce backend built with **.NET 9**, **Clean Architecture**, **YARP API Gateway**, **Entity Framework Core**, **SQL Server**, **Redis**, and **Docker Compose**.

[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat-square&logo=docker)](https://www.docker.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?style=flat-square&logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?style=flat-square&logo=redis)](https://redis.io/)
[![xUnit](https://img.shields.io/badge/Tests-60%20Passed-brightgreen?style=flat-square)](https://xunit.net/)

</div>

---

## Architecture

```
                        ┌──────────────────────┐
                        │    Client / Postman   │
                        └──────────┬───────────┘
                                   │ :5000
                                   ▼
                        ┌──────────────────────┐
                        │  API Gateway (YARP)  │
                        └───┬──────────────┬───┘
                            │              │
              ┌─────────────┘              └─────────────┐
              │                                          │
              ▼                                          ▼
  ┌───────────────────────┐              ┌───────────────────────┐
  │    Catalog Service    │◄────────────►│   Ordering Service    │
  │                       │  HTTP calls  │                       │
  │  • Products           │              │  • Checkout flow      │
  │  • Categories         │              │  • Order state machine │
  │  • Pricing            │              │  • Price validation   │
  └──────────┬────────────┘              └──────────┬────────────┘
             │                                      │
       ┌─────┴──────┐                               │
       ▼            ▼                               ▼
  ┌─────────┐  ┌─────────┐           ┌───────────────────────┐
  │  Redis  │  │  SQL DB │           │  Inventory Service    │
  │  Cache  │  │CatalogDb│           │                       │
  └─────────┘  └─────────┘           │  • Stock tracking     │
                                     │  • Atomic reserve /   │
                                     │    release            │
                                     └──────────┬────────────┘
                                                │
                                           ┌────┴────┐
                                           ▼         ▼
                                      ┌────────┐ ┌────────┐
                                      │ SQL DB │ │ SQL DB │
                                      │OrderDb │ │  InvDb │
                                      └────────┘ └────────┘
```

Each service follows strict **Clean Architecture** with four decoupled layers:

| Layer | Responsibility |
|---|---|
| `*.Domain` | Entities, value objects, domain events, domain exceptions. Zero external dependencies. |
| `*.Application` | CQRS handlers (MediatR), FluentValidation pipeline, DTOs, repository interfaces. |
| `*.Infrastructure` | EF Core DbContext, migrations, repository & Unit of Work implementations, Redis, Polly HTTP adapters. |
| `*.API` | ASP.NET Core controllers, exception middleware, health checks, DI configuration. |

---

## Services

| Service | External Port | Responsibility |
|---|---|---|
| **API Gateway** | `:5000` | YARP reverse proxy — single entry point for all client traffic |
| **Catalog Service** | Internal | Product catalog, categories, pricing, Redis Cache-Aside |
| **Ordering Service** | Internal | Checkout orchestration, order state machine (Pending → Confirmed → Canceled) |
| **Inventory Service** | Internal | Real-time warehouse stock, atomic reserve/release, optimistic concurrency |
| **SQL Server 2022** | `:1433` | Isolated database per service (`CatalogDb`, `OrderingDb`, `InventoryDb`) |
| **Redis 7** | `:6379` | Distributed cache for fast product reads |

---

## Tech Stack

| Category | Technology |
|---|---|
| Runtime | .NET 9, C# 13, ASP.NET Core |
| Gateway | YARP (Yet Another Reverse Proxy) |
| Database | Microsoft SQL Server 2022 + EF Core 9 |
| Caching | Redis 7 (`StackExchange.Redis`, `IDistributedCache`) |
| Resilience | `Microsoft.Extensions.Http.Resilience` — retries, exponential backoff, circuit breakers |
| Messaging | MediatR 12 (CQRS, domain event dispatching) |
| Validation | FluentValidation with automatic pipeline behavior |
| Testing | xUnit, Moq, FluentAssertions |
| Containers | Multi-stage Dockerfiles, Docker Compose v2 |

---

## Prerequisites

| Tool | Required | Notes |
|---|---|---|
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | ✅ Required | Includes Docker Compose v2 |
| [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) | Optional | Only needed to run tests or debug locally |
| [Postman](https://www.postman.com/) | Optional | A pre-built collection is included |

---

## Quick Start

> **The entire stack — microservices, gateway, SQL Server, and Redis — launches with one command.**

### 1. Clone the repository
```bash
git clone https://github.com/Muhammed-Nady/FlowCart.git
cd FlowCart
```

### 2. Start everything
```bash
docker compose up -d --build
```

Docker Compose starts services in dependency order using health checks:
1. **SQL Server** and **Redis** boot first and pass health probes.
2. **Catalog**, **Ordering**, and **Inventory** services start once infrastructure is healthy. EF Core migrations and seed data run automatically.
3. The **API Gateway** starts last and begins routing traffic.

### 3. Check container status
```bash
docker compose ps
```

All 6 containers should be `Up`:
```
NAME                         STATUS
flowcart-api-gateway         Up
flowcart-catalog-service     Up
flowcart-inventory-service   Up
flowcart-ordering-service    Up
flowcart-redis               Up (healthy)
flowcart-sqlserver           Up (healthy)
```

### 4. Confirm the gateway is live
```bash
curl http://localhost:5000/
```
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

### 5. Stop the stack
```bash
# Stop containers (data persisted)
docker compose down

# Stop containers and wipe all volumes
docker compose down -v
```

---

## Local Development (Without Docker)

Prefer to run services directly for debugging?

### 1. Spin up only infrastructure
```bash
docker compose up -d sqlserver redis
```

### 2. Run each service in a separate terminal
```bash
# Terminal 1
dotnet run --project src/Services/Catalog/Catalog.API

# Terminal 2
dotnet run --project src/Services/Inventory/Inventory.API

# Terminal 3
dotnet run --project src/Services/Ordering/Ordering.API

# Terminal 4
dotnet run --project src/ApiGateway/ApiGateway.API
```

> Migrations and seed data run automatically on startup via `Database.MigrateAsync()`.

---

## API Reference

All endpoints are accessed through the gateway at `http://localhost:5000`.

### Catalog

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/products` | List all products (paged) |
| `GET` | `/api/products/{id}` | Get product by ID (Redis Cache-Aside) |
| `POST` | `/api/products` | Create a new product |
| `PUT` | `/api/products/{id}/price` | Update a product's price |
| `GET` | `/api/categories` | List all categories |

### Inventory

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/inventory/products/{id}` | Get live stock levels for a product |
| `POST` | `/api/inventory/reserve` | Reserve stock (called by Ordering) |
| `POST` | `/api/inventory/release` | Release reserved stock on cancellation |

### Ordering

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/orders` | Create order — validates price + reserves stock |
| `GET` | `/api/orders` | List all orders (paged, filterable by status) |
| `GET` | `/api/orders/{id}` | Get order by ID |
| `GET` | `/api/orders/customer/{customerId}` | Get all orders for a customer |
| `PUT` | `/api/orders/{id}/confirm` | Confirm a pending order |
| `PUT` | `/api/orders/{id}/cancel` | Cancel an order and release reserved stock |

---

## Testing

### Postman Collection

A ready-to-use collection is included at the root of the repo.

1. Open **Postman** → **Import** → select `FlowCart.postman_collection.json`
2. The collection includes pre-wired variables:
   - `baseUrl` → `http://localhost:5000`
   - `sampleProductId` → pre-seeded laptop ID
   - `createdOrderId` → auto-populated after creating an order

### cURL Examples

**Place an order (full end-to-end checkout):**
```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "shippingAddress": {
      "street": "123 Tech Way", "city": "San Francisco",
      "state": "CA", "country": "USA", "zipCode": "94107"
    },
    "items": [{
      "productId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      "productName": "Enterprise Dev Laptop 16-inch",
      "expectedUnitPrice": 1999.99,
      "quantity": 1
    }]
  }'
```

**Confirm an order:**
```bash
curl -X PUT http://localhost:5000/api/orders/<ORDER_ID>/confirm
```

**Cancel an order (stock is automatically released):**
```bash
curl -X PUT http://localhost:5000/api/orders/<ORDER_ID>/cancel \
  -H "Content-Type: application/json" \
  -d '{"reason": "Customer changed mind"}'
```

---

## Unit Tests

```bash
dotnet test
```

| Project | Tests | Coverage |
|---|---|---|
| `FlowCart.BuildingBlocks.UnitTests` | 14 | Repository, Unit of Work, Result pattern, domain event dispatching |
| `Catalog.UnitTests` | 18 | Product domain rules, CQRS handlers, Redis Cache-Aside, validators |
| `Ordering.UnitTests` | 16 | Order state machine, Polly resilience, checkout orchestration |
| `Inventory.UnitTests` | 12 | Atomic stock reservation, optimistic concurrency, release logic |
| **Total** | **60** | **0 failures** |

---

## Solution Structure

```
FlowCart/
├── docker-compose.yml
├── FlowCart.postman_collection.json
├── README.md
│
├── src/
│   ├── ApiGateway/
│   │   └── ApiGateway.API/
│   │
│   ├── BuildingBlocks/                     # Shared abstractions
│   │   ├── FlowCart.BuildingBlocks.Domain/
│   │   ├── FlowCart.BuildingBlocks.Application/
│   │   └── FlowCart.BuildingBlocks.Infrastructure/
│   │
│   └── Services/
│       ├── Catalog/
│       │   ├── Catalog.Domain/
│       │   ├── Catalog.Application/
│       │   ├── Catalog.Infrastructure/
│       │   └── Catalog.API/
│       ├── Ordering/
│       │   ├── Ordering.Domain/
│       │   ├── Ordering.Application/
│       │   ├── Ordering.Infrastructure/
│       │   └── Ordering.API/
│       └── Inventory/
│           ├── Inventory.Domain/
│           ├── Inventory.Application/
│           ├── Inventory.Infrastructure/
│           └── Inventory.API/
│
└── tests/
    ├── BuildingBlocks/
    │   └── FlowCart.BuildingBlocks.UnitTests/
    └── Services/
        ├── Catalog.UnitTests/
        ├── Ordering.UnitTests/
        └── Inventory.UnitTests/
```
