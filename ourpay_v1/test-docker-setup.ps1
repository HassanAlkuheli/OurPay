# Payment API Docker Setup Test Script (PowerShell)
# This script validates all the acceptance criteria for the Docker containerization

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Payment API Docker Setup Test" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Test counter
$TestsPassed = 0
$TestsFailed = 0

# Function to run a test
function Test-Command {
    param(
        [string]$TestName,
        [scriptblock]$Command
    )
    
    Write-Host "Testing: $TestName... " -NoNewline
    
    try {
        $result = & $Command
        Write-Host "PASS" -ForegroundColor Green
        $script:TestsPassed++
    } catch {
        Write-Host "FAIL" -ForegroundColor Red
        $script:TestsFailed++
    }
}

# Function to check HTTP endpoint
function Test-Endpoint {
    param(
        [string]$Url,
        [int]$ExpectedStatusCode,
        [string]$TestName
    )
    
    Write-Host "Testing: $TestName... " -NoNewline
    
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 10
        if ($response.StatusCode -eq $ExpectedStatusCode) {
            Write-Host "PASS (HTTP $($response.StatusCode))" -ForegroundColor Green
            $script:TestsPassed++
        } else {
            Write-Host "FAIL (HTTP $($response.StatusCode), expected $ExpectedStatusCode)" -ForegroundColor Red
            $script:TestsFailed++
        }
    } catch {
        Write-Host "FAIL (Error: $($_.Exception.Message))" -ForegroundColor Red
        $script:TestsFailed++
    }
}

Write-Host "Starting Docker Compose services..." -ForegroundColor Yellow
docker-compose up -d

Write-Host "Waiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "ACCEPTANCE CRITERIA TESTS" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Test 1: All containers are running
Write-Host "1. Container Status Tests:" -ForegroundColor Yellow
Test-Command "All containers are running" { docker-compose ps | Select-String "Up" | Measure-Object | Select-Object -ExpandProperty Count }
Test-Command "PostgreSQL container is healthy" { docker-compose ps | Select-String "paymentapi-db.*healthy" | Measure-Object | Select-Object -ExpandProperty Count }
Test-Command "Redis container is healthy" { docker-compose ps | Select-String "paymentapi-redis.*healthy" | Measure-Object | Select-Object -ExpandProperty Count }
Test-Command "RabbitMQ container is healthy" { docker-compose ps | Select-String "paymentapi-rabbitmq.*healthy" | Measure-Object | Select-Object -ExpandProperty Count }
Test-Command "Payment API container is healthy" { docker-compose ps | Select-String "paymentapi-app.*healthy" | Measure-Object | Select-Object -ExpandProperty Count }
Test-Command "Nginx container is running" { docker-compose ps | Select-String "paymentapi-nginx.*Up" | Measure-Object | Select-Object -ExpandProperty Count }

Write-Host ""
Write-Host "2. HTTP Endpoint Tests:" -ForegroundColor Yellow
Test-Endpoint "http://localhost:8080/health" 200 "Health endpoint is accessible"
Test-Endpoint "http://localhost:8080/" 200 "Swagger UI is accessible at root"
Test-Endpoint "http://localhost:15672" 200 "RabbitMQ Management UI is accessible"

Write-Host ""
Write-Host "3. Service Integration Tests:" -ForegroundColor Yellow
Write-Host "Testing: Database connectivity... " -NoNewline
try {
    $healthResponse = Invoke-RestMethod -Uri "http://localhost:8080/health" -TimeoutSec 10
    if ($healthResponse.services.database -eq "Healthy") {
        Write-Host "PASS" -ForegroundColor Green
        $TestsPassed++
    } else {
        Write-Host "FAIL" -ForegroundColor Red
        $TestsFailed++
    }
} catch {
    Write-Host "FAIL" -ForegroundColor Red
    $TestsFailed++
}

Write-Host "Testing: Redis connectivity... " -NoNewline
try {
    $healthResponse = Invoke-RestMethod -Uri "http://localhost:8080/health" -TimeoutSec 10
    if ($healthResponse.services.redis -eq "Healthy") {
        Write-Host "PASS" -ForegroundColor Green
        $TestsPassed++
    } else {
        Write-Host "FAIL" -ForegroundColor Red
        $TestsFailed++
    }
} catch {
    Write-Host "FAIL" -ForegroundColor Red
    $TestsFailed++
}

Write-Host "Testing: RabbitMQ connectivity... " -NoNewline
try {
    $healthResponse = Invoke-RestMethod -Uri "http://localhost:8080/health" -TimeoutSec 10
    if ($healthResponse.services.rabbitmq -eq "Healthy") {
        Write-Host "PASS" -ForegroundColor Green
        $TestsPassed++
    } else {
        Write-Host "FAIL" -ForegroundColor Red
        $TestsFailed++
    }
} catch {
    Write-Host "FAIL" -ForegroundColor Red
    $TestsFailed++
}

Write-Host ""
Write-Host "4. Data Persistence Test:" -ForegroundColor Yellow
Write-Host "Testing: Database data persistence... " -NoNewline
try {
    $volumes = docker volume ls --filter "name=ourpay_v1" --format "{{.Name}}"
    if ($volumes -contains "ourpay_v1_pgdata") {
        Write-Host "PASS" -ForegroundColor Green
        $TestsPassed++
    } else {
        Write-Host "FAIL" -ForegroundColor Red
        $TestsFailed++
    }
} catch {
    Write-Host "FAIL" -ForegroundColor Red
    $TestsFailed++
}

Write-Host ""
Write-Host "5. Port Accessibility Tests:" -ForegroundColor Yellow
Test-Endpoint "http://localhost:8080" 200 "Application accessible via port 8080"
Test-Endpoint "http://localhost:15672" 200 "RabbitMQ Management port 15672 is accessible"

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "TEST RESULTS SUMMARY" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Tests Passed: $TestsPassed" -ForegroundColor Green
Write-Host "Tests Failed: $TestsFailed" -ForegroundColor Red
Write-Host "Total Tests: $($TestsPassed + $TestsFailed)"

if ($TestsFailed -eq 0) {
    Write-Host ""
    Write-Host "ALL TESTS PASSED!" -ForegroundColor Green
    Write-Host "The Docker containerization setup meets all acceptance criteria." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "SOME TESTS FAILED" -ForegroundColor Red
    Write-Host "Please review the failed tests and fix the issues." -ForegroundColor Red
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "SERVICE ACCESS INFORMATION" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Application: http://localhost:8080" -ForegroundColor White
Write-Host "Swagger UI: http://localhost:8080/" -ForegroundColor White
Write-Host "Health Check: http://localhost:8080/health" -ForegroundColor White
Write-Host "RabbitMQ Management: http://localhost:15672 (guest/guest)" -ForegroundColor White
Write-Host "PostgreSQL: localhost:5432 (postgres/postgres)" -ForegroundColor White
Write-Host "Redis: localhost:6379" -ForegroundColor White
Write-Host "RabbitMQ AMQP: localhost:5672" -ForegroundColor White

Write-Host ""
Write-Host "To stop all services: docker-compose down" -ForegroundColor Yellow
Write-Host "To view logs: docker-compose logs -f" -ForegroundColor Yellow
Write-Host "To restart: docker-compose restart" -ForegroundColor Yellow