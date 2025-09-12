@echo off
echo 🎛️ Starting Kubernetes Dashboard...
echo.
echo ✅ Dashboard is installed and ready!
echo.
echo 🌐 To access the Dashboard:
echo   1. The port-forward will start automatically
echo   2. Open: https://localhost:8443
echo   3. Select "Token" authentication method
echo   4. Paste the token from dashboard-token.txt
echo.
echo 🔑 Your access token:
type dashboard-token.txt
echo.
echo.
echo 🚀 Starting port-forward (press Ctrl+C to stop)...
echo ⚠️  You may see SSL certificate warnings - click "Advanced" and "Proceed"
echo.
kubectl -n kubernetes-dashboard port-forward svc/kubernetes-dashboard 8443:443
