# LGTM Stack Test Script (PowerShell)
# This script validates the LGTM Stack implementation for comprehensive observability

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "LGTM Stack Integration Test" -ForegroundColor Cyan
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
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Yellow
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

Write-Host "Waiting for LGTM Stack services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "LGTM STACK ACCEPTANCE CRITERIA TESTS" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Test 1: Grafana Access
Write-Host "1. Grafana Dashboard Tests:" -ForegroundColor Yellow
Test-Endpoint "http://localhost:3000" 200 "Grafana UI is accessible"
Test-Endpoint "http://localhost:3000/api/datasources" 200 "Grafana datasources API is accessible"

# Test 2: Prometheus Metrics Collection
Write-Host ""
Write-Host "2. Prometheus Metrics Tests:" -ForegroundColor Yellow
Test-Endpoint "http://localhost:9090" 200 "Prometheus UI is accessible"
Test-Endpoint "http://localhost:9090/api/v1/targets" 200 "Prometheus targets API is accessible"

# Test 3: Application Metrics
Write-Host ""
Write-Host "3. Application Metrics Tests:" -ForegroundColor Yellow
Write-Host "Testing: Application metrics endpoint... " -NoNewline
try {
    $metricsResponse = Invoke-RestMethod -Uri "http://localhost:8080/metrics" -TimeoutSec 10
    if ($metricsResponse -match "http_request_duration_seconds") {
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

# Test 4: Loki Log Aggregation
Write-Host ""
Write-Host "4. Loki Log Aggregation Tests:" -ForegroundColor Yellow
Test-Endpoint "http://localhost:3100/ready" 200 "Loki is ready"
Test-Endpoint "http://localhost:3100/api/prom/label" 200 "Loki labels API is accessible"

# Test 5: Tempo Distributed Tracing
Write-Host ""
Write-Host "5. Tempo Distributed Tracing Tests:" -ForegroundColor Yellow
Test-Endpoint "http://localhost:3200/ready" 200 "Tempo is ready"
Test-Endpoint "http://localhost:3200/api/search/tags" 200 "Tempo search API is accessible"

# Test 6: Promtail Log Shipping
Write-Host ""
Write-Host "6. Promtail Log Shipping Tests:" -ForegroundColor Yellow
Write-Host "Testing: Promtail container is running... " -NoNewline
try {
    $promtailStatus = docker-compose ps | Select-String "paymentapi-promtail.*Up"
    if ($promtailStatus) {
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

# Test 7: OpenTelemetry Integration
Write-Host ""
Write-Host "7. OpenTelemetry Integration Tests:" -ForegroundColor Yellow
Write-Host "Testing: OpenTelemetry traces are being generated... " -NoNewline
try {
    # Generate some traffic to create traces
    Invoke-RestMethod -Uri "http://localhost:8080/health" -TimeoutSec 5 | Out-Null
    Invoke-RestMethod -Uri "http://localhost:8080/api/v1/auth/register" -Method POST -ContentType "application/json" -Body '{"username":"test","email":"test@test.com","password":"Test123!"}' -TimeoutSec 5 | Out-Null
    
    # Check if traces are being sent to Tempo
    Start-Sleep -Seconds 5
    $traceResponse = Invoke-RestMethod -Uri "http://localhost:3200/api/search/tags" -TimeoutSec 10
    if ($traceResponse) {
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

# Test 8: Grafana Datasources Configuration
Write-Host ""
Write-Host "8. Grafana Datasources Tests:" -ForegroundColor Yellow
Write-Host "Testing: Prometheus datasource configured... " -NoNewline
try {
    $datasourcesResponse = Invoke-RestMethod -Uri "http://localhost:3000/api/datasources" -TimeoutSec 10
    $prometheusDS = $datasourcesResponse | Where-Object { $_.type -eq "prometheus" }
    if ($prometheusDS) {
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

Write-Host "Testing: Loki datasource configured... " -NoNewline
try {
    $datasourcesResponse = Invoke-RestMethod -Uri "http://localhost:3000/api/datasources" -TimeoutSec 10
    $lokiDS = $datasourcesResponse | Where-Object { $_.type -eq "loki" }
    if ($lokiDS) {
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

Write-Host "Testing: Tempo datasource configured... " -NoNewline
try {
    $datasourcesResponse = Invoke-RestMethod -Uri "http://localhost:3000/api/datasources" -TimeoutSec 10
    $tempoDS = $datasourcesResponse | Where-Object { $_.type -eq "tempo" }
    if ($tempoDS) {
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
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "TEST RESULTS SUMMARY" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Tests Passed: $TestsPassed" -ForegroundColor Green
Write-Host "Tests Failed: $TestsFailed" -ForegroundColor Red
Write-Host "Total Tests: $($TestsPassed + $TestsFailed)"

if ($TestsFailed -eq 0) {
    Write-Host ""
    Write-Host "🎉 ALL LGTM STACK TESTS PASSED! 🎉" -ForegroundColor Green
    Write-Host "The LGTM Stack implementation meets all acceptance criteria." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "❌ SOME TESTS FAILED ❌" -ForegroundColor Red
    Write-Host "Please review the failed tests and fix the issues." -ForegroundColor Red
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "LGTM STACK ACCESS INFORMATION" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "📊 Grafana Dashboard: http://localhost:3000 (admin/admin)" -ForegroundColor White
Write-Host "📈 Prometheus Metrics: http://localhost:9090" -ForegroundColor White
Write-Host "📝 Loki Logs: http://localhost:3100" -ForegroundColor White
Write-Host "🔍 Tempo Traces: http://localhost:3200" -ForegroundColor White
Write-Host "📊 Application Metrics: http://localhost:8080/metrics" -ForegroundColor White
Write-Host "🌐 Application API: http://localhost:8080" -ForegroundColor White
Write-Host "📚 Swagger UI: http://localhost:8080/" -ForegroundColor White

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "GRAFANA EXPLORE QUERIES" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Prometheus Metrics:" -ForegroundColor Yellow
Write-Host "  - rate(http_request_duration_seconds_count[5m])" -ForegroundColor White
Write-Host "  - up" -ForegroundColor White
Write-Host "  - process_cpu_seconds_total" -ForegroundColor White

Write-Host ""
Write-Host "Loki Logs (LogQL):" -ForegroundColor Yellow
Write-Host "  - {container_name=~\".*paymentapi.*\"}" -ForegroundColor White
Write-Host "  - {job=\"containerlogs\"}" -ForegroundColor White

Write-Host ""
Write-Host "Tempo Traces:" -ForegroundColor Yellow
Write-Host "  - Search by service name: paymentapi" -ForegroundColor White
Write-Host "  - Search by operation: GET /health" -ForegroundColor White

Write-Host ""
Write-Host "To stop all services: docker-compose down" -ForegroundColor Yellow
Write-Host "To view logs: docker-compose logs -f" -ForegroundColor Yellow
Write-Host "To restart: docker-compose restart" -ForegroundColor Yellow
