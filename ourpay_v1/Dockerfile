# Build stage - Use standard image for faster build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy only project file first for better caching
COPY PaymentApi.csproj ./
RUN dotnet restore --no-cache

# Copy source code in optimized order
COPY Program.cs ./
COPY appsettings*.json ./
COPY Configuration/ ./Configuration/
COPY Controllers/ ./Controllers/
COPY Data/ ./Data/
COPY DTOs/ ./DTOs/
COPY Middleware/ ./Middleware/
COPY Models/ ./Models/
COPY Repositories/ ./Repositories/
COPY Services/ ./Services/

# Build with optimizations
RUN dotnet publish -c Release -o out --no-restore \
    -p:DebugType=None \
    -p:DebugSymbols=false

# Runtime stage - Use minimal Alpine image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app

# Install curl for health checks and create non-root user
RUN apk add --no-cache curl && \
    adduser -D -h /app appuser && \
    chown -R appuser:appuser /app

# Copy application with proper ownership
COPY --from=build --chown=appuser:appuser /app/out .

# Switch to non-root user
USER appuser

# Set optimized environment variables for smaller footprint
ENV ASPNETCORE_URLS=http://+:80 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
    DOTNET_USE_POLLING_FILE_WATCHER=1 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    COMPlus_EnableDiagnostics=0 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1

EXPOSE 80

# Lightweight health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "PaymentApi.dll"]
