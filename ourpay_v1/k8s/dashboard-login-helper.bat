@echo off
echo 🔐 Kubernetes Dashboard Login Helper
echo ===================================
echo.

echo 📋 LOGIN INSTRUCTIONS:
echo.
echo 1. Go to: https://localhost:8443
echo 2. Select "Token" as authentication method
echo 3. Copy the token below and paste it into the dashboard
echo.

echo 🔑 AUTHENTICATION TOKEN (24-hour expiration):
echo ================================================================
type k8s\dashboard-token.txt
echo.
echo ================================================================
echo.

echo 💡 TROUBLESHOOTING:
echo - If login fails, the token might be expired
echo - Run: kubectl -n kubernetes-dashboard create token admin-user --duration=24h
echo - Copy the new token to the dashboard
echo.

echo 🌐 Opening dashboard in browser...
start https://localhost:8443
echo.

pause
