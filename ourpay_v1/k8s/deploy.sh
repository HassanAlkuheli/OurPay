#!/bin/bash

# Deploy to Kubernetes
echo "🚀 Deploying OurPay to Kubernetes..."

# Create namespace
echo "📦 Creating namespace..."
kubectl apply -f k8s/namespace.yaml

# Deploy databases and message queue
echo "🗄️ Deploying PostgreSQL..."
kubectl apply -f k8s/postgres.yaml

echo "📡 Deploying Redis..."
kubectl apply -f k8s/redis.yaml

echo "🐰 Deploying RabbitMQ..."
kubectl apply -f k8s/rabbitmq.yaml

# Wait for databases to be ready
echo "⏳ Waiting for databases to be ready..."
kubectl wait --for=condition=ready pod -l app=postgres -n ourpay --timeout=300s
kubectl wait --for=condition=ready pod -l app=redis -n ourpay --timeout=300s
kubectl wait --for=condition=ready pod -l app=rabbitmq -n ourpay --timeout=300s

# Deploy monitoring
echo "📊 Deploying monitoring stack..."
kubectl apply -f k8s/monitoring.yaml

# Deploy API
echo "🌐 Deploying Payment API..."
kubectl apply -f k8s/api.yaml

# Wait for API to be ready
echo "⏳ Waiting for API to be ready..."
kubectl wait --for=condition=ready pod -l app=paymentapi -n ourpay --timeout=300s

echo "✅ Deployment complete!"
echo ""
echo "🌐 Access points:"
echo "  - Payment API: http://localhost (via LoadBalancer)"
echo "  - Grafana: http://localhost:3000 (admin/admin123)"
echo "  - RabbitMQ Management: http://localhost:15672 (guest/guest)"
echo "  - Swagger UI: http://localhost/swagger"
echo ""
echo "📊 Check deployment status:"
echo "  kubectl get all -n ourpay"
echo ""
echo "🔍 View logs:"
echo "  kubectl logs -f deployment/paymentapi -n ourpay"
