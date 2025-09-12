@echo off
REM Deploy to Kubernetes (Windows)
echo 🚀 Deploying OurPay to Kubernetes...

REM Create namespace
echo 📦 Creating namespace...
kubectl apply -f k8s/namespace.yaml --validate=false

REM Deploy databases and message queue
echo 🗄️ Deploying PostgreSQL...
kubectl apply -f k8s/postgres.yaml --validate=false

echo 📡 Deploying Redis...
kubectl apply -f k8s/redis.yaml --validate=false

echo 🐰 Deploying RabbitMQ...
kubectl apply -f k8s/rabbitmq.yaml --validate=false

REM Wait for databases to be ready
echo ⏳ Waiting for databases to be ready...
kubectl wait --for=condition=ready pod -l app=postgres -n ourpay --timeout=300s
kubectl wait --for=condition=ready pod -l app=redis -n ourpay --timeout=300s
kubectl wait --for=condition=ready pod -l app=rabbitmq -n ourpay --timeout=300s

REM Deploy monitoring
echo 📊 Deploying monitoring stack...
kubectl apply -f k8s/monitoring.yaml --validate=false

REM Deploy API
echo 🌐 Deploying Payment API...
kubectl apply -f k8s/api.yaml --validate=false

REM Wait for API to be ready
echo ⏳ Waiting for API to be ready...
kubectl wait --for=condition=ready pod -l app=paymentapi -n ourpay --timeout=300s

echo ✅ Deployment complete!
echo.
echo 🌐 Access points:
echo   - Payment API: http://localhost (via LoadBalancer)
echo   - Grafana: http://localhost:3000 (admin/admin123)
echo   - RabbitMQ Management: http://localhost:15672 (guest/guest)
echo   - Swagger UI: http://localhost/swagger
echo.
echo 📊 Check deployment status:
echo   kubectl get all -n ourpay
echo.
echo 🔍 View logs:
echo   kubectl logs -f deployment/paymentapi -n ourpay
pause
