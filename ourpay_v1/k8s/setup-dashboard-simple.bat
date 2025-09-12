@echo off
echo 🎛️ Setting up Kubernetes Dashboard GUI (Alternative Method - No Helm Required)...
echo.

REM Install Kubernetes Dashboard using kubectl (fallback method)
echo 📦 Installing Kubernetes Dashboard via kubectl...
kubectl apply -f https://raw.githubusercontent.com/kubernetes/dashboard/v2.7.0/aio/deploy/recommended.yaml

echo ⏳ Waiting for dashboard to be ready...
timeout /t 30 /nobreak > nul

REM Create admin service account
echo 👤 Creating admin service account...
kubectl apply -f k8s/dashboard-admin.yaml

echo ⏳ Waiting for service account to be created...
timeout /t 10 /nobreak > nul

REM Create access token
echo 🔑 Creating access token...
kubectl -n kubernetes-dashboard create token admin-user > dashboard-token.txt

echo ✅ Dashboard setup complete!
echo.
echo 🎛️ To access Kubernetes Dashboard:
echo   Method 1 (Recommended):
echo   1. Run: kubectl -n kubernetes-dashboard port-forward svc/kubernetes-dashboard 8443:443
echo   2. Open: https://localhost:8443
echo.
echo   Method 2 (Proxy):
echo   1. Run: kubectl proxy
echo   2. Open: http://localhost:8001/api/v1/namespaces/kubernetes-dashboard/services/https:kubernetes-dashboard:/proxy/
echo.
echo   🔑 Use Bearer Token authentication with token from dashboard-token.txt
echo.
echo 📄 Your access token is saved in: dashboard-token.txt
echo.
echo 🚀 Starting port-forward to Dashboard (Ctrl+C to stop)...
kubectl -n kubernetes-dashboard port-forward svc/kubernetes-dashboard 8443:443
