# LSE Trading Services

Production-ready .NET 8 trading service that records trades and serves current market values (average prices). The solution is layered, testable, and designed for extension with Redis, Kafka and container orchestration.

This README includes an end-to-end guide (local + containerized + Kubernetes) and maps features to the latest implementation.

---

## Contents
- Overview
- Architecture diagram
- API endpoints with version
- Project structure (latest)
- Design patterns & where applied
- Low level design (LLD)
- High level design (HLD)
- SOLID principles
- Configuration
- End-to-end: Local, Docker Compose, Kubernetes
- Dev/test commands & sample requests
- Operational suggestions & future improvements
- Contact

---

## Overview
The service records trades and provides current values (average price per ticker). Key capabilities:
- Write path: record trade (`POST /api/trades`) — persistent and publishes `TradeRecordedEvent`.
- Read path: get average price per ticker, bulk queries and all-tickers.
- Caching via `IDistributedCache` (decorator) with TTL from configuration.
- Pluggable event publishing (`IEventPublisher`) with Kafka implementation; noop fallback if Kafka not configured.
- EF Core repository with safe retry/transaction behavior for relational providers.

---

## Architecture (high level)
graph TD
  A["Brokers / Clients"] -->|JWT Token| B["API Gateway / Load Balancer (Kubernetes)"]
  B --> C["Web API (ASP.NET Core) - Stateless"]
  C --> D["Message Bus (Kafka)"]
  D --> E["Consumers / Stream Processors"]
  C --> H["Cache (Redis)"]
  C --> F["OLTP (Postgres / SQL Server)"]
  E --> G["Analytics / DW"]

---

## API Endpoints version - v1
Base: `/api/{version}/trades`

- `POST /api/{version}/trades`  
  Record trade. Body: `Trade` (DataAnnotation validation). Returns `202 Accepted`. Emits `TradeRecordedEvent` (publisher exceptions swallowed to keep write path resilient).

- `GET /api/{version}/trades/{ticker}`  
  Current average price for `ticker`. 200 OK with `ApiResponse<T>`.

- `POST /api/{version}/trades/range`  
  Bulk lookup. Body: `["VOD","HSBA","BP"]`. Returns dictionary: ticker → average price.

- `GET /api/{version}/trades`  
  All current values (all tickers).

Responses use `ApiResponse<T>`: `{ success, data, message, statusCode }`.

---

## Project structure (latest)
- `Lse.API` — Controllers, middleware, filters, Swagger, `appsettings.json`.
- `Lse.Application` — Service layer, `IEventPublisher` abstraction, `CachedTradeService` decorator, DI registration.
- `Lse.Domain` — Entities (`Trade`), constraints, `ITradeRepository` interface.
- `Lse.Infrastructure` — `LseDbContext`, EF Core repository, Kafka publisher, Redis & DI wiring.
- `Lse.Tests` — Unit and integration tests.

---

## Design patterns & where applied
- Repository Pattern — `Lse.Infrastructure\Repositories\TradeRepository.cs`.
- Dependency Injection & factory registration — `Lse.Application\DependencyInjection.cs`, `Lse.Infrastructure\DependencyInjection.cs`.
- Middleware Pattern — `Lse.API\Middleware\ExceptionHandlingMiddleware.cs`.
- Service Layer — `Lse.Application\Services\TradeService.cs`.
- Decorator Pattern — `Lse.Application\Services\CachedTradeService.cs` wraps `TradeService`.
- Observer/Event-driven — `TradeService` publishes `TradeRecordedEvent` via `IEventPublisher`. `KafkaEventPublisher` in `Lse.Infrastructure`.

---

## Low Level Design (LLD)
- `TradeService` validates `Trade`, persists with `ITradeRepository`, publishes `TradeRecordedEvent`.
- `CachedTradeService`:
  - Wraps `TradeService` and implements caching for:
    - `GetCurrentValueAsync`
    - `GetCurrentValuesAsync`
    - `GetAllCurrentValuesAsync`
  - Invalidates per-ticker and all keys on `RecordTradeAsync`.
  - TTL read from `IConfiguration` key `Cache:TradeValueTtlSeconds` (fallback 120s).
- `TradeRepository` uses EF Core execution strategy and transactional save for relational providers.

---

## Configuration (keys & behavior)
File: `Lse.API/appsettings.json` (or per-environment overrides)

Important keys:
- `Cache:TradeValueTtlSeconds` (int seconds, default 120)
- `Redis:ConnectionString` (empty → in-memory distributed cache fallback)
- `Kafka:BootstrapServers` (empty → noop publisher)
- `Kafka:Topic:TradeRecorded` (default `trades.recorded`)
- `ConnectionStrings:LseDatabase` (optional; if empty EF uses in-memory provider for dev)

Behavior:
- `CachedTradeService` reads TTL from configuration if not provided explicitly.
- `Lse.Infrastructure.DependencyInjection` registers Redis when `Redis:ConnectionString` present; otherwise `IDistributedMemoryCache`.
- Kafka publisher registered only when `Kafka:BootstrapServers` is configured.

---

## End-to-end

### 1) Local dev (fast)
Prereqs: .NET 8 SDK

- Optional: start Redis/Kafka/Postgres if you want full integration; otherwise default in-memory fallbacks let API run standalone.

Run API:
- From repo root:
  - dotnet restore
  - dotnet build
  - dotnet run --project Lse.API

Open Swagger (Development): `https://localhost:55157/swagger/index.html` (see `Lse.API/Properties/launchSettings.json`)

### 2) Docker Compose (recommended for integration)
Create `docker-compose.yml` (example below) that brings up Redis, Kafka (via Bitnami/Confluent) and Postgres. Then run API container pointing `ASPNETCORE_ENVIRONMENT=Development` and mount config.

Sample minimal `docker-compose.yml` (adapt versions / settings as needed):
version: '3.8' services: zookeeper: image: bitnami/zookeeper:latest environment: - ALLOW_ANONYMOUS_LOGIN=yes
kafka: image: bitnami/kafka:latest environment: - KAFKA_BROKER_ID=1 - KAFKA_ZOOKEEPER_CONNECT=zookeeper:2181 - ALLOW_PLAINTEXT_LISTENER=yes - KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://kafka:9092 depends_on: - zookeeper
redis: image: redis:7 ports: - "6379:6379"
postgres: image: postgres:15 environment: - POSTGRES_USER=postgres - POSTGRES_PASSWORD=postgres - POSTGRES_DB=lse ports: - "5432:5432"


After compose is up:
- Update `Lse.API/appsettings.Development.json` with connection strings:
  - `Redis:ConnectionString`: `localhost:6379`
  - `Kafka:BootstrapServers`: `localhost:9092`
  - `ConnectionStrings:LseDatabase`: Postgres connection
- Run API as container or locally.

### 3) Kubernetes (basic)
- Build container image and push to registry.
- Deploy `Deployment` + `Service` and configure `ConfigMap`/`Secret` for `appsettings`.
- Example HPA (high-level; adapt labels/names):

apiVersion: apps/v1 kind: Deployment metadata: name: lse-trading-api spec: replicas: 3 selector: matchLabels: app: lse-trading-api template: metadata: labels: app: lse-trading-api spec: containers: - name: api image: <registry>/lse-trading-api:latest ports: - containerPort: 80 env: - name: ASPNETCORE_ENVIRONMENT value: "Production"

HPA example (v2 autoscaling):

---

## Run consumer / background processing
The repository has a sample `TradeRecordedConsumer` in `Lse.Infrastructure\Kafka`. For production, register that consumer as an `IHostedService` (or an independent consumer deployment) and run it against the Kafka cluster configured by `Kafka:BootstrapServers`.

---

## Dev & test commands
- Run unit tests:
  - `dotnet test`
- Run API locally:
  - `dotnet run --project Lse.API`
- Build docker image:
  - `docker build -t lse-trading-api:local -f Lse.API/Dockerfile .` (create Dockerfile to publish and run)


---

## Operational suggestions
- Use API Gateway (Apigee/Kong/Azure APIM) to enforce JWT and rate limits.
- Use Redis for caching; tune `Cache:TradeValueTtlSeconds` to trade-off freshness vs. load.
- Use Kafka for fan-out and analytics; make publisher robust with retries/backoff and DLQ (dead-letter topic).
- Add OpenTelemetry traces and expose Prometheus metrics for autoscaling and observability.
- Harden: secrets in Kubernetes `Secrets`, connection strings in `ConfigMap` with restricted access.

---

## Future improvements
- Implement full CQRS with separate read store (materialized views) for very large datasets.
- Add robust Kafka retry/backoff, circuit-breakers and dead-lettering.
- Move consumer into a scaled `IHostedService` deployment and register as part of helm charts.
- Add authentication & authorization, RBAC and tenant isolation if required.

---

## Contact
For issues or questions open an issue in the repo or contact: `appsbalaji01@gmail.com`
