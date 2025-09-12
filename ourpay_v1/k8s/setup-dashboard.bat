@echo off
echo 🎛️ Setting up Kubernetes Dashboard GUI (Official Method)...
echo.

REM Check if Helm is installed
helm version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Helm is not installed. Installing Helm first...
    echo.
    echo 📦 Installing Helm via PowerShell...
    powershell -Command "Invoke-WebRequest -Uri 'https://get.helm.sh/helm-v3.15.4-windows-amd64.zip' -OutFile 'helm.zip'; Expand-Archive -Path 'helm.zip' -DestinationPath '.'; Move-Item '.\windows-amd64\helm.exe' '.'; Remove-Item 'helm.zip' -Force; Remove-Item 'windows-amd64' -Recurse -Force"
    set PATH=%PATH%;%CD%
    echo ✅ Helm installed successfully!
    echo.
)

echo 🎛️ Installing Kubernetes Dashboard using Helm...
echo.

REM Add kubernetes-dashboard repository
echo 📦 Adding Kubernetes Dashboard Helm repository...
helm repo add kubernetes-dashboard https://kubernetes.github.io/dashboard/

REM Update Helm repositories
echo 🔄 Updating Helm repositories...
helm repo update

REM Deploy Kubernetes Dashboard
echo 🚀 Deploying Kubernetes Dashboard...
helm upgrade --install kubernetes-dashboard kubernetes-dashboard/kubernetes-dashboard --create-namespace --namespace kubernetes-dashboard

echo ⏳ Waiting for Dashboard to be ready...
timeout /t 30 /nobreak > nul

REM Create admin service account (from GitHub guide)
echo 👤 Creating admin service account...
kubectl apply -f k8s/dashboard-admin.yaml

echo 🔑 Creating access token...
kubectl -n kubernetes-dashboard create token admin-user > dashboard-token.txt

echo ✅ Dashboard setup complete!
echo.
echo 🎛️ To access Kubernetes Dashboard:
echo   1. Run: kubectl -n kubernetes-dashboard port-forward svc/kubernetes-dashboard-kong-proxy 8443:443
echo   2. Open: https://localhost:8443
echo   3. Use Bearer Token authentication with token from dashboard-token.txt
echo.
echo 🔑 Your access token is saved in: dashboard-token.txt
echo.
echo 🚀 Starting port-forward (Ctrl+C to stop)...
kubectl -n kubernetes-dashboard port-forward svc/kubernetes-dashboard-kong-proxy 8443:443
