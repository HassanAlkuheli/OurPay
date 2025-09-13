# 💳 OurPay - Payment by Link API

<div align="center">

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-336791?style=for-the-badge&logo=postgresql&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-DC382D?style=for-the-badge&logo=redis&logoColor=white)
![Kubernetes](https://img.shields.io/badge/Kubernetes-326CE5?style=for-the-badge&logo=kubernetes&logoColor=white)

**Enterprise-grade Payment Processing API with Advanced Security & Scalability**

[🚀 Quick Start](#-quick-start) • [📚 Documentation](#-api-documentation) • [🏗️ Architecture](#-architecture) • [🔧 Configuration](#-configuration)

</div>

---

## 🎯 **Overview**

OurPay is a modern, scalable Payment by Link API built for enterprise environments. It provides secure payment processing with JWT authentication, role-based access control, and comprehensive monitoring capabilities.

### ✨ **Key Features**

🔐 **Security First**
- JWT Authentication with refresh tokens
- Role-based access control (Merchant/Customer/Admin)
- Redis-based rate limiting
- Comprehensive audit logging

⚡ **High Performance**
- Async/await throughout
- Redis caching layer
- Connection pooling
- Configurable throughput limits

🚀 **Production Ready**
- Docker containerization
- Kubernetes deployment
- Health checks
- Structured logging with Serilog

🔄 **Real-time Processing**
- RabbitMQ message queuing
- Webhook delivery system
- Background job processing

## 🏗️ **Tech Stack**

### **Backend**
```csharp
ASP.NET Core 8          // Modern web framework
Entity Framework Core   // ORM for database access
AutoMapper             // Object-to-object mapping
```

### **Databases**
```sql
PostgreSQL 15+         // Primary production database
SQLite                 // Development & testing
Redis 7+              // Caching & session management
```

### **Authentication & Security**
```csharp
JWT Bearer Tokens      // Stateless authentication
Role-based Access      // merchant/customer/admin
Rate Limiting         // Redis-backed protection
Audit Logging         // Complete action tracking
```

### **Message Queue & Background Processing**
```yaml
RabbitMQ:
  - Webhook delivery queues
  - Background job processing
  - Event-driven architecture
```

### **Infrastructure**
```yaml
Docker & Docker Compose    # Containerization
Kubernetes               # Container orchestration
Oracle Cloud            # Production deployment
Serilog                # Structured logging
```

## 🚀 **Quick Start**

### Prerequisites
- .NET 8 SDK
- Docker & Docker Compose
- PostgreSQL (or use Docker)

### 1️⃣ Clone & Setup
```bash
git clone https://github.com/HassanAlkuheli/paymentAPI.git
cd paymentAPI/ourpay_v1
```

### 2️⃣ Start Infrastructure
```bash
# Start PostgreSQL, Redis, and RabbitMQ
docker-compose up -d postgres redis rabbitmq

# Or use SQLite for quick testing (no Docker needed)
# Update ConnectionStrings:DefaultConnection in appsettings.json
```

### 3️⃣ Run the API
```bash
# Restore packages and run
dotnet restore
dotnet run --urls "http://localhost:5262"

# API will be available at:
# http://localhost:5262 (API)
# http://localhost:5262/swagger (Documentation)
```

### 4️⃣ Test the API
```bash
# Register a merchant
curl -X POST http://localhost:5262/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"merchant@test.com","password":"Test123!","fullName":"Test Merchant","role":"merchant"}'

# Login to get JWT token
curl -X POST http://localhost:5262/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"merchant@test.com","password":"Test123!"}'
```

## 📚 **API Documentation**

### 🔐 Authentication Endpoints

#### Register User
```http
POST /api/v1/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "fullName": "John Doe",
  "role": "merchant"  // or "customer"
}
```

#### Login
```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!"
}

Response:
{
  "success": true,
  "data": {
    "accessToken": "eyJ0eXAiOiJKV1Q...",
    "refreshToken": "refresh_token_here",
    "expiresIn": 900,
    "tokenType": "Bearer"
  }
}
```

#### Refresh Token
```http
POST /api/v1/auth/refresh
Content-Type: application/json

{
  "refreshToken": "your_refresh_token"
}
```

### 💳 Payment Endpoints

#### Create Payment Link (Merchant Only)
```http
POST /api/v1/payments
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "amount": 100.00,
  "currency": "USD",
  "description": "Product Purchase",
  "expiresInMinutes": 60
}

Response:
{
  "success": true,
  "data": {
    "paymentId": "guid-here",
    "paymentLink": "https://pay.ourpay.com/pay/guid-here",
    "amount": 100.00,
    "currency": "USD",
    "status": "pending",
    "expiresAt": "2024-01-01T12:00:00Z"
  }
}
```

#### Confirm Payment (Customer)
```http
POST /api/v1/payments/{paymentId}/confirm
Authorization: Bearer {jwt_token}

Response:
{
  "success": true,
  "data": {
    "paymentId": "guid-here",
    "status": "success",
    "processedAt": "2024-01-01T12:05:00Z",
    "transactionId": "txn_123456789"
  }
}
```

#### Get Payment Details
```http
GET /api/v1/payments/{paymentId}
Authorization: Bearer {jwt_token}
```

### 📊 Admin & Monitoring

#### Health Check
```http
GET /health

Response:
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "redis": "Healthy",
    "rabbitmq": "Healthy"
  }
}
```

#### Get Audit Logs (Admin Only)
```http
GET /api/v1/logs?page=1&pageSize=50
Authorization: Bearer {admin_jwt_token}
```

## 🏗️ **Architecture**

### **Middleware Pipeline**
```csharp
1. ThroughputLimitMiddleware    // Request concurrency control
2. RateLimitMiddleware          // Redis-based rate limiting  
3. ErrorHandlingMiddleware      // Global exception handling
4. JWT Authentication          // Bearer token validation
5. Authorization              // Role-based access control
```

### **Service Layer Pattern**
```csharp
PaymentService         // Core payment processing logic
AuthService           // Authentication & JWT management
CacheService          // Redis abstraction layer
RabbitMQService       // Message queue integration
WebhookService        // Async webhook delivery
AuditLogService       // Security & compliance logging
```

### **Database Schema**

#### Users Table
```sql
CREATE TABLE Users (
    UserId          UUID PRIMARY KEY,
    Name            VARCHAR(100) NOT NULL,
    Email           VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash    VARCHAR(255) NOT NULL,
    Role            VARCHAR(20) NOT NULL CHECK (Role IN ('merchant', 'customer', 'admin')),
    Balance         DECIMAL(18,2) DEFAULT 0.00,
    CreatedAt       TIMESTAMP DEFAULT NOW(),
    UpdatedAt       TIMESTAMP DEFAULT NOW()
);
```

#### Payments Table
```sql
CREATE TABLE Payments (
    PaymentId       UUID PRIMARY KEY,
    MerchantId      UUID NOT NULL REFERENCES Users(UserId),
    CustomerId      UUID REFERENCES Users(UserId),
    Amount          DECIMAL(18,2) NOT NULL,
    Currency        CHAR(3) NOT NULL DEFAULT 'USD',
    Status          VARCHAR(20) NOT NULL DEFAULT 'pending',
    Description     TEXT,
    ExpiresAt       TIMESTAMP NOT NULL,
    CreatedAt       TIMESTAMP DEFAULT NOW(),
    UpdatedAt       TIMESTAMP DEFAULT NOW()
);
```

## 🔧 **Configuration**

### **Environment Variables**
```bash
# Database
ConnectionStrings__DefaultConnection="Host=localhost;Database=paymentapi;Username=postgres;Password=password"

# Redis
RedisSettings__ConnectionString="localhost:6379"

# JWT
JwtSettings__SecretKey="your-super-secret-jwt-key-here"
JwtSettings__ExpiryMinutes=15
JwtSettings__RefreshTokenExpiryDays=7

# Rate Limiting
RateLimitSettings__PaymentCreation__RequestsPerMinute=10
RateLimitSettings__PaymentConfirmation__RequestsPerMinute=5

# RabbitMQ
RabbitMQSettings__HostName="localhost"
RabbitMQSettings__UserName="guest"
RabbitMQSettings__Password="guest"
```

### **Docker Deployment**
```yaml
# docker-compose.yml
version: '3.8'
services:
  paymentapi:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    depends_on:
      - postgres
      - redis
      - rabbitmq

  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: paymentapi
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: password
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "15672:15672"  # Management UI
```

### **Kubernetes Deployment**
```yaml
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: paymentapi
spec:
  replicas: 3
  selector:
    matchLabels:
      app: paymentapi
  template:
    metadata:
      labels:
        app: paymentapi
    spec:
      containers:
      - name: paymentapi
        image: paymentapi:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
```

## 🔒 **Security Features**

### **Rate Limiting**
- **Payment Creation**: 10 requests/minute per user
- **Payment Confirmation**: 5 requests/minute per user  
- **Login Attempts**: 5 requests/minute per IP
- **Global Throughput**: Configurable concurrent request limits

### **JWT Security**
- **Access Tokens**: 15-minute expiry (configurable)
- **Refresh Tokens**: 7-day expiry, stored in Redis
- **Token Revocation**: Immediate via Redis blacklist
- **Role-based Claims**: Automatic role enforcement

### **Data Protection**
- **Password Hashing**: BCrypt with configurable work factor
- **SQL Injection Prevention**: Parameterized queries via EF Core
- **Input Validation**: Model validation with custom attributes
- **Audit Trail**: Complete action logging for compliance

## 🚀 **Performance & Scaling**

### **Scalability Features**
```yaml
Horizontal Pod Autoscaling:
  CPU Threshold: 70%
  Min Replicas: 2
  Max Replicas: 50
  
Load Balancing:
  - Kubernetes Service (ClusterIP)
  - External LoadBalancer support
  - Session-less design (JWT stateless)
```

### **Performance Optimizations**
- **Connection Pooling**: Database and Redis
- **Async/Await**: Non-blocking I/O operations
- **Caching Strategy**: Redis for session data and rate limits
- **Background Processing**: RabbitMQ for webhook delivery

## 📈 **Monitoring & Observability**

### **Complete LGTM Stack**
```yaml
Grafana (3000):           # Unified dashboards & alerting
  - Logs correlation
  - Traces visualization  
  - Metrics monitoring
  - Continuous profiling

Prometheus (9090):        # Metrics collection
  - Application metrics
  - Infrastructure monitoring
  - Custom business metrics

Loki (3100):             # Log aggregation
  - Structured log storage
  - Log-to-trace correlation
  - Full-text search

Tempo (3200):            # Distributed tracing
  - ✅ FIXED: Single-node configuration resolves "empty ring" error
  - TraceQL query support
  - Trace-to-logs correlation
  - Trace-to-profiles correlation

Pyroscope (4040):        # 🆕 Continuous profiling
  - CPU profiling
  - Memory allocation tracking
  - Code-level performance analysis
  - Correlation with traces
```

### **🆕 Enhanced Observability Features**

**🔍 Distributed Tracing (Fixed)**
- Tempo now uses single-node configuration for development
- Eliminates "empty ring" errors in TraceQL queries
- Full request lifecycle tracking across services

**⚡ Continuous Profiling (New)**
- Real-time CPU and memory profiling with Pyroscope
- Code-level performance insights
- Integration with distributed traces
- Zero-overhead profiling in production

**📊 Unified Correlation**
- Jump from logs → traces → profiles seamlessly
- Service map visualization with performance data
- Root cause analysis with complete context

### **Health Checks**
```http
GET /health                    # Overall health
GET /health/ready             # Readiness probe  
GET /health/live              # Liveness probe
```

### **Logging**
```csharp
// Structured logging with Serilog
{
  "timestamp": "2024-01-01T12:00:00Z",
  "level": "Information",
  "messageTemplate": "Payment {PaymentId} created by user {UserId}",
  "properties": {
    "PaymentId": "guid-here",
    "UserId": "user-guid",
    "Amount": 100.00,
    "Currency": "USD"
  }
}
```

### **Metrics Available**
- Request throughput and latency
- Database connection pool usage
- Redis cache hit/miss ratios
- RabbitMQ queue depths
- JWT token validation rates
- **🆕 Continuous profiling metrics (CPU, memory allocation)**

### **Access Monitoring Services**
```bash
# After running docker-compose up -d
Grafana:    http://localhost:3000    (admin/admin)
Prometheus: http://localhost:9090
Loki:       http://localhost:3100
Tempo:      http://localhost:3200
Pyroscope:  http://localhost:4040    # 🆕 Continuous profiling UI
```

## 🧪 **Testing**

### **Run Tests**
```bash
# Unit tests
dotnet test Tests/

# Integration tests with TestContainers
dotnet test Tests/ --filter Category=Integration
```

### **Load Testing**
```bash
# Built-in load testing tools available
# Check TechStackUI/ and LoadTesting/ directories for monitoring tools
```

## 🤝 **Contributing**

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 **License**

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📞 **Support**

- **Email**: support@ourpay.com
- **Documentation**: [Wiki](https://github.com/HassanAlkuheli/paymentAPI/wiki)
- **Issues**: [GitHub Issues](https://github.com/HassanAlkuheli/paymentAPI/issues)

---

<div align="center">

**Built with ❤️ by the OurPay Team**

⭐ Star this repo if you find it helpful!

</div>
- `POST /api/v1/auth/login` - Login, return JWT + refresh token
- `POST /api/v1/auth/refresh` - Refresh access token
- `POST /api/v1/auth/revoke` - Revoke refresh token (logout)

### Payments
- `POST /api/v1/payments` - Create payment link (merchant only)
- `GET /api/v1/payments/{payment_id}` - Show payment info (public)
- `POST /api/v1/payments/{payment_id}/confirm` - Confirm payment (customer only)
- `POST /api/v1/payments/{payment_id}/cancel` - Cancel payment (merchant/admin)
- `GET /api/v1/payments` - List payments (merchant → own only, admin → all)

### Admin
- `GET /api/v1/logs/{payment_id}` - Get audit logs (admin only)

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- Docker & Docker Compose
- PostgreSQL (if running without Docker)
- Redis (if running without Docker)

### 1. Clone the Repository
```bash
git clone <repository-url>
cd payment-api-v1
```

### 2. Run with Docker Compose
```bash
# For development
docker-compose -f docker-compose.dev.yml up -d

# For production
docker-compose up -d
```

This will start:
- Payment API on `http://localhost:7000`
- PostgreSQL on port `5432`
- Redis on port `6379`

### 3. Run Locally (without Docker)

#### Setup Database
```bash
# Start PostgreSQL and Redis
docker run -d --name postgres -p 5432:5432 -e POSTGRES_DB=paymentapi -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres123 postgres:15-alpine
docker run -d --name redis -p 6379:6379 redis:7-alpine
```

#### Run the API
```bash
dotnet restore
dotnet run
```

API will be available at `https://localhost:7000` (HTTPS) or `http://localhost:5000` (HTTP)

### 4. Access Services
- **API Documentation**: `http://localhost:7000/swagger`
- **Logs**: Check `./logs/` directory
- **Database**: PostgreSQL on `localhost:5432`
- **Cache**: Redis on `localhost:6379`

## 🧪 Demo Data

The application seeds demo data on startup:

### Demo Accounts
| Role | Email | Password |
|------|--------|----------|
| Merchant | merchant@demo.com | Merchant123! |
| Customer | customer@demo.com | Customer123! |
| Admin | admin@demo.com | Admin123! |

### Initial Balances
- **Customer**: $1,000.00
- **Merchant**: $0.00
- **Admin**: $0.00

## 📝 API Usage Examples

### 1. Register a New User
```bash
curl -X POST "http://localhost:7000/api/v1/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "John Merchant",
    "email": "john@merchant.com",
    "password": "Password123!",
    "role": "merchant"
  }'
```

### 2. Login
```bash
curl -X POST "http://localhost:7000/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "merchant@demo.com",
    "password": "Merchant123!"
  }'
```

### 3. Create Payment Link (Merchant)
```bash
curl -X POST "http://localhost:7000/api/v1/payments" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 99.99,
    "currency": "USD",
    "expiresInMinutes": 60
  }'
```

### 4. View Payment (Public)
```bash
curl -X GET "http://localhost:7000/api/v1/payments/PAYMENT_ID"
```

### 5. Confirm Payment (Customer)
```bash
curl -X POST "http://localhost:7000/api/v1/payments/PAYMENT_ID/confirm" \
  -H "Authorization: Bearer CUSTOMER_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "idempotencyKey": "unique-key-123"
  }'
```

### 6. List Payments
```bash
# Merchant - own payments only
curl -X GET "http://localhost:7000/api/v1/payments?page=1&pageSize=10" \
  -H "Authorization: Bearer MERCHANT_ACCESS_TOKEN"

# Admin - all payments
curl -X GET "http://localhost:7000/api/v1/payments?page=1&pageSize=10" \
  -H "Authorization: Bearer ADMIN_ACCESS_TOKEN"
```

## 🔐 Security Features

- **JWT Authentication** with access (15min) and refresh tokens (7 days)
- **Role-based authorization** (Customer, Merchant, Admin)
- **Rate limiting** on sensitive endpoints
- **Password hashing** using ASP.NET Core Identity
- **HTTPS enforcement** in production
- **Secure headers** and CORS configuration
- **Input validation** and sanitization
- **SQL injection protection** via EF Core

## ⚡ Business Logic

- **Merchant** can only cancel their own payments
- **Customer** can only confirm payments if balance >= amount
- **Audit log** written for every create/confirm/cancel operation
- **Automatic expiration** of pending payments
- **ACID transactions** for payment confirmations
- **Idempotency** support for payment confirmations
- **Balance validation** before payment processing

## 📊 Monitoring & Logging

- **Structured logging** with Serilog
- **Console and file logging** with rotation
- **Request/response logging**
- **Performance monitoring**
- **Error tracking** with detailed stack traces
- **Audit trail** for all operations

## 🧪 Testing

### Run Unit Tests
```bash
dotnet test
```

### Test Coverage
The project includes unit tests for:
- Payment service business logic
- Authentication flows
- Validation scenarios
- Error handling

## 🐳 Docker Configuration

### Development
```bash
docker-compose -f docker-compose.dev.yml up -d
```

### Production
```bash
docker-compose up -d
```

### With Management Tools
```bash
# Include pgAdmin and Redis Commander
docker-compose --profile tools up -d
```

Access management tools:
- **pgAdmin**: `http://localhost:8080` (admin@example.com / admin123)
- **Redis Commander**: `http://localhost:8081`

## ⚙️ Configuration

Key configuration sections in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "PostgreSQL connection string"
  },
  "JwtSettings": {
    "SecretKey": "Your JWT secret key",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "RateLimitSettings": {
    "PaymentCreationPerMinute": 10,
    "PaymentConfirmationPerMinute": 5,
    "LoginAttemptsPerMinute": 5
  },
  "PaymentSettings": {
    "BaseUrl": "https://yourapi.com",
    "MinAmount": 0.01,
    "MaxAmount": 10000.00,
    "SupportedCurrencies": ["USD", "EUR", "GBP"]
  }
}
```

## 🔧 Development

### Project Structure
```
├── Controllers/          # API Controllers
├── Services/            # Business Logic Services
├── Repositories/        # Data Access Layer
├── Models/             # Entity Models
├── DTOs/               # Data Transfer Objects
├── Data/               # Entity Framework Context
├── Configuration/      # App Settings & Mapping
├── Middleware/         # Custom Middleware
├── Tests/              # Unit Tests
└── docker-compose.yml  # Docker Configuration
```

### Adding New Features
1. Create/update models in `Models/`
2. Add DTOs in `DTOs/`
3. Update mapping in `Configuration/MappingProfile.cs`
4. Implement repository methods in `Repositories/`
5. Add business logic in `Services/`
6. Create controller endpoints in `Controllers/`
7. Add unit tests in `Tests/`

## 📈 Performance Considerations

- **Connection pooling** for database connections
- **Redis caching** for frequently accessed data
- **Async/await** throughout for non-blocking operations
- **Pagination** for list endpoints
- **Indexed database columns** for fast lookups
- **Background services** for cleanup tasks

## 🛠️ Troubleshooting

### Common Issues

1. **Database Connection Issues**
   ```bash
   # Check if PostgreSQL is running
   docker ps | grep postgres
   
   # View logs
   docker logs payment-postgres
   ```

2. **Redis Connection Issues**
   ```bash
   # Check if Redis is running
   docker ps | grep redis
   
   # Test connection
   redis-cli ping
   ```

3. **JWT Token Issues**
   - Check `JwtSettings:SecretKey` is at least 32 characters
   - Verify token expiration times
   - Check system clock synchronization

4. **API Not Responding**
   ```bash
   # Check API logs
   docker logs payment-api
   
   # Check if ports are available
   netstat -tulpn | grep :7000
   ```

## 📜 License

This project is licensed under the MIT License.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

**Happy Coding! 🚀**
