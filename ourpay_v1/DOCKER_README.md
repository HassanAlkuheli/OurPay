# Payment API - Docker Setup

This document provides instructions for running the Payment API using Docker and Docker Compose.

## Prerequisites

- Docker Desktop (Windows/Mac) or Docker Engine (Linux)
- Docker Compose v2.0+

## Quick Start

1. **Clone and navigate to the project:**
   ```bash
   cd ourpay_v1
   ```

2. **Start all services:**
   ```bash
   docker-compose up --build
   ```

3. **Access the application:**
   - API: http://localhost:8080
   - Swagger UI: http://localhost:8080/swagger
   - RabbitMQ Management: http://localhost:15672 (guest/guest)

## Services

The Docker Compose setup includes the following services:

| Service | Port | Description |
|---------|------|-------------|
| **paymentapi** | 80 (internal) | .NET 8.0 API application |
| **nginx** | 8080 | Reverse proxy and load balancer |
| **db** | 5432 | PostgreSQL 16 database |
| **redis** | 6379 | Redis 7 cache |
| **rabbitmq** | 5672, 15672 | RabbitMQ message broker with management UI |

## Environment Configuration

The application automatically detects the Docker environment and:
- Uses PostgreSQL instead of SQLite
- Enables Redis caching
- Enables RabbitMQ messaging
- Enables webhook background services
- Uses container service names for connections

## Data Persistence

Data is persisted using Docker volumes:
- `pgdata`: PostgreSQL database files
- `redisdata`: Redis data
- `rabbitmqdata`: RabbitMQ data

## Development Workflow

### Hot Reload (Development)
For development with hot reload, you can mount the source code:

```yaml
# Add to docker-compose.yml under paymentapi service
volumes:
  - ./logs:/app/logs
  - .:/app/src  # Mount source for hot reload
```

### Rebuilding After Code Changes
```bash
docker-compose up --build
```

### Viewing Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f paymentapi
```

### Database Management
```bash
# Connect to PostgreSQL
docker-compose exec db psql -U postgres -d paymentapi

# Run migrations (if needed)
docker-compose exec paymentapi dotnet ef database update
```

## Health Checks

The application includes comprehensive health checks:

- **Overall Health**: GET http://localhost:8080/health
- **Readiness**: GET http://localhost:8080/health/ready
- **Liveness**: GET http://localhost:8080/health/live

## Troubleshooting

### Common Issues

1. **Port conflicts**: Ensure ports 8080, 5432, 6379, 5672, and 15672 are available
2. **Permission issues**: On Linux/Mac, ensure Docker has proper permissions
3. **Memory issues**: Ensure Docker has sufficient memory allocated (4GB+ recommended)

### Reset Everything
```bash
# Stop and remove all containers, networks, and volumes
docker-compose down -v

# Remove all images
docker-compose down --rmi all

# Start fresh
docker-compose up --build
```

### Debugging
```bash
# Check service status
docker-compose ps

# Check service logs
docker-compose logs [service-name]

# Execute commands in running container
docker-compose exec paymentapi bash
docker-compose exec db psql -U postgres -d paymentapi
```

## Production Considerations

For production deployment:

1. **Security**: Change default passwords and secrets
2. **SSL/TLS**: Configure HTTPS termination at nginx
3. **Monitoring**: Add monitoring and logging aggregation
4. **Scaling**: Consider Kubernetes for orchestration
5. **Backup**: Implement database backup strategies

## API Endpoints

Once running, the following endpoints are available:

- **Authentication**: POST /auth/login, POST /auth/register
- **Payments**: GET /payments, POST /payments, GET /payments/{id}
- **Webhooks**: POST /webhooks
- **Health**: GET /health, GET /health/ready, GET /health/live
- **Swagger**: GET /swagger

## Testing the Setup

1. **Health Check**: Visit http://localhost:8080/health
2. **Database**: Check that database connection shows "Healthy"
3. **Redis**: Check that Redis connection shows "Healthy"  
4. **RabbitMQ**: Check that RabbitMQ connection shows "Healthy"
5. **API**: Try creating a user via POST /auth/register
6. **RabbitMQ UI**: Visit http://localhost:15672 to see message queues

## Support

For issues or questions:
1. Check the logs: `docker-compose logs -f`
2. Verify all services are healthy: `docker-compose ps`
3. Check the health endpoint: http://localhost:8080/health
