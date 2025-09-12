# Install Helm for Kubernetes Dashboard
Write-Host "🔧 Installing Helm for Kubernetes Dashboard..." -ForegroundColor Cyan

# Create temporary directory
$tempDir = "$env:TEMP\helm-install"
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

try {
    # Download Helm
    Write-Host "📦 Downloading Helm..." -ForegroundColor Yellow
    $helmUrl = "https://get.helm.sh/helm-v3.15.4-windows-amd64.zip"
    $helmZip = "$tempDir\helm.zip"
    Invoke-WebRequest -Uri $helmUrl -OutFile $helmZip

    # Extract Helm
    Write-Host "📂 Extracting Helm..." -ForegroundColor Yellow
    Expand-Archive -Path $helmZip -DestinationPath $tempDir -Force

    # Copy helm.exe to a location in PATH
    $helmExe = "$tempDir\windows-amd64\helm.exe"
    $programFiles = "${env:ProgramFiles}\Helm"
    
    # Create Helm directory
    New-Item -ItemType Directory -Path $programFiles -Force | Out-Null
    
    # Copy helm.exe
    Copy-Item $helmExe -Destination "$programFiles\helm.exe" -Force
    
    # Add to PATH (for current session)
    $env:PATH += ";$programFiles"
    
    # Add to system PATH permanently
    $currentPath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
    if ($currentPath -notlike "*$programFiles*") {
        [Environment]::SetEnvironmentVariable("PATH", "$currentPath;$programFiles", "Machine")
        Write-Host "✅ Added Helm to system PATH" -ForegroundColor Green
    }

    Write-Host "✅ Helm installed successfully!" -ForegroundColor Green
    Write-Host "🎛️ You can now run: k8s\setup-dashboard.bat" -ForegroundColor Cyan
    
    # Verify installation
    & "$programFiles\helm.exe" version
}
catch {
    Write-Host "❌ Error installing Helm: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "💡 Try using the simple setup instead: k8s\setup-dashboard-simple.bat" -ForegroundColor Yellow
}
finally {
    # Cleanup
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
