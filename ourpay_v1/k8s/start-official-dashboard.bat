@echo off
echo 🚀 Starting Official Kubernetes Dashboard
echo =========================================
echo Following: https://kubernetes.io/docs/tasks/access-application-cluster/web-ui-dashboard/
echo.

echo 📋 Dashboard Information:
echo   URL: https://localhost:8443
echo   Token location: k8s\dashboard-token.txt
echo.

echo 🔑 Authentication Token:
type k8s\dashboard-token.txt
echo.
echo.

echo 📡 Starting port-forward...
echo Press Ctrl+C to stop the dashboard
echo.

kubectl -n kubernetes-dashboard port-forward svc/kubernetes-dashboard-kong-proxy 8443:443
