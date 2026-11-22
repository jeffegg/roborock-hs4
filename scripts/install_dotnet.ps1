$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logPath = Join-Path $repoRoot "install_dotnet.log"
$installerScript = Join-Path $repoRoot "dotnet-install.ps1"
$dotnetDir = "C:\dotnet"

function Write-Log {
    param([string]$Message)
    $line = "[{0}] {1}" -f (Get-Date -Format s), $Message
    $line | Tee-Object -FilePath $logPath -Append
}

if (Test-Path $logPath) {
    Remove-Item $logPath -Force
}

Write-Log "Downloading dotnet install script"
Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $installerScript

Write-Log "Installing .NET SDK 8.0.100 to $dotnetDir"
& powershell -NoProfile -ExecutionPolicy Bypass -File $installerScript -Version 8.0.100 -InstallDir $dotnetDir

$dotnetExe = Join-Path $dotnetDir "dotnet.exe"
if (!(Test-Path $dotnetExe)) {
    throw "dotnet installation failed: $dotnetExe not found"
}

Write-Log "Installed dotnet at $dotnetExe"
& $dotnetExe --version | Tee-Object -FilePath $logPath -Append
exit 0
