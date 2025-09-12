# OurPay Payment API - AI Coding Agent Instructions

## 🏗️ Architecture Overview

This is a **Payment by Link API** built with .NET 8, featuring a multi-project ecosystem with load testing tools and monitoring UIs.

### Core Stack
- **Main API**: ASP.NET Core 8 with Entity Framework Core
- **Databases**: PostgreSQL (primary), SQLite (local development), Redis (caching)
- **Messaging**: RabbitMQ for webhooks and background processing
- **Auth**: JWT with refresh tokens, role-based access (merchant/customer/admin)
- **Infrastructure**: Docker Compose for local development

### Project Structure
```
ourpay_v1/               # Main API project
├── Controllers/         # API endpoints (Auth, Payments, Logs, Webhooks)
├── Services/           # Business logic (Payment, Auth, Cache, RabbitMQ, Webhook)
├── Middleware/         # Custom middleware (Rate limiting, Throughput, Error handling)
├── Configuration/      # Settings classes and AutoMapper profiles
├── LoadTesting/        # WPF load testing UI
├── TechStackUI/        # WPF tech stack monitoring UI
├── LoadTestDemo/       # Standalone API for testing
└── Tests/              # Unit and integration tests
```

## 🔧 Critical Development Workflows

### Running the API
```bash
# With Docker (recommended)
docker-compose up -d  # PostgreSQL + Redis + RabbitMQ

# Local development with SQLite
dotnet run --urls "http://localhost:5262"

# Use run-demo.bat for full stack demo
```

### Database Operations
- **PostgreSQL**: `dotnet ef database update` (requires running Docker services)
- **SQLite**: Auto-created at `payment_app.db` for local dev
- **Switching**: Modify `ConnectionStrings:DefaultConnection` in appsettings

### Load Testing Ecosystem
- `LoadTesting/` - WPF app for API load testing
- `TechStackUI/` - Real-time monitoring of Redis, RabbitMQ, SQLite
- `LoadTestDemo/` - Standalone API that processes high-volume payments
- Run `run-demo.bat` to start the complete ecosystem

## 🚀 Key Architectural Patterns

### Middleware Pipeline Order (Program.cs)
1. **ThroughputLimitMiddleware** - Controls concurrent requests and per-IP limits
2. **RateLimitMiddleware** - Redis-based rate limiting per user/endpoint
3. **ErrorHandlingMiddleware** - Global exception handling with structured logging
4. **JWT Authentication** - Bearer token validation

### Service Layer Architecture
- **PaymentService**: Core business logic, integrates with audit logging
- **CacheService**: Redis abstraction for rate limiting and session storage
- **RabbitMQService**: Async webhook delivery and background job queuing
- **WebhookBackgroundService**: Hosted service for processing webhook queues

### Configuration Pattern
All settings use strongly-typed classes in `Configuration/AppSettings.cs`:
```csharp
JwtSettings, RedisSettings, RateLimitSettings, PaymentSettings, RabbitMQSettings
```
Bind via `builder.Configuration.GetSection(TSettings.SectionName).Get<TSettings>()`

### Repository Pattern
- `Repositories/` contains EF Core repository implementations
- `IUserRepository`, `IPaymentRepository`, `IAuditLogRepository`
- Injected into services for data access abstraction

## 🎯 Domain-Specific Conventions

### API Endpoints Follow RESTful Pattern
- `POST /api/v1/auth/register` - User registration
- `POST /api/v1/auth/login` - Returns JWT + refresh token
- `POST /api/v1/payments` - Create payment link (merchant only)
- `POST /api/v1/payments/{id}/confirm` - Confirm payment (customer only)

### Error Handling Strategy
- Global middleware catches all exceptions
- Returns consistent `ApiResponse<T>` structure
- Logs with Serilog to console + file (`logs/payment-api-*.log`)
- Rate limit violations return HTTP 429 with retry headers

### Authentication Flow
1. Login returns `AccessToken` (15min) + `RefreshToken` (7 days)
2. All protected endpoints require Bearer token
3. Role-based authorization: `[Authorize(Roles = "merchant,admin")]`
4. Refresh tokens stored in Redis for revocation support

### Rate Limiting Implementation
Uses Redis counters with TTL:
- Payment creation: 10/minute per user
- Payment confirmation: 5/minute per user
- Login attempts: 5/minute per IP
- Keys: `rate_limit:{endpoint}:{user_id}` or `rate_limit:{endpoint}:{ip}`

## 🔍 Testing & Debugging

### Load Testing Pattern
1. Start main API: `dotnet run --urls "http://localhost:5262"`
2. Start LoadTestDemo API: `cd LoadTestDemo && dotnet run` (port 5262)
3. Launch TechStackUI: `cd TechStackUI && dotnet run`
4. Use LoadTesting WPF app for high-volume testing

### Monitoring Integration
- **TechStackUI** shows real-time Redis cache hits/misses, RabbitMQ queue depths, SQLite queries
- **LoadTesting** app simulates payment workflows with configurable throughput
- Both UIs connect to running APIs and display live metrics

### Common Issues
- **Port conflicts**: APIs default to 5262, check with `netstat -an | findstr 5262`
- **Database migrations**: Run `dotnet ef database update` in main project directory
- **Redis/RabbitMQ connectivity**: TechStackUI gracefully shows connection failures

## 📊 Performance Considerations

### Throughput Limits (ThroughputLimitMiddleware)
- Max concurrent requests: Configurable via `ThroughputLimitSettings`
- Per-IP connection limits prevent abuse
- Semaphore-based concurrency control

### Caching Strategy
- JWT blacklist in Redis for immediate token revocation
- Rate limiting counters with automatic TTL
- User session data for webhook callbacks

When modifying this codebase, always consider the multi-project ecosystem and ensure changes work across the load testing and monitoring tools.

always use lowest amount of code and files created
always prefer existing code over created new code
always choose the best location for new code
always use the instructions provided without change it
