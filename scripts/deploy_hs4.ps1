$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$logPath = Join-Path $repoRoot "deploy_hs4.log"

function Write-Log {
    param([string]$Message)
    $line = "[{0}] {1}" -f (Get-Date -Format s), $Message
    $line | Tee-Object -FilePath $logPath -Append
}

function Invoke-Checked {
    param(
        [string]$Label,
        [string]$FilePath,
        [string[]]$Arguments
    )

    Write-Log "$Label"
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Label failed with exit code $LASTEXITCODE"
    }
}

function Find-Git {
    $candidates = @(
        "C:\Program Files\Git\bin\git.exe",
        "C:\Program Files\Git\cmd\git.exe",
        "C:\Program Files (x86)\Git\bin\git.exe",
        "C:\Program Files (x86)\Git\cmd\git.exe"
    )

    foreach ($path in $candidates) {
        if (Test-Path $path) {
            return $path
        }
    }

    throw "Git executable not found in standard installation paths."
}

function Find-Dotnet {
    $candidates = @(
        "C:\Program Files\dotnet\dotnet.exe",
        "C:\dotnet\dotnet.exe"
    )

    foreach ($dotnet in $candidates) {
        if (Test-Path $dotnet) {
            return $dotnet
        }
    }

    throw "dotnet SDK not found in standard deployment paths."
}

function Find-HomeSeerRoot {
    $candidates = @(
        "C:\Program Files (x86)\HomeSeer HS4",
        "C:\Program Files\HomeSeer HS4",
        "C:\HomeSeer HS4"
    )

    foreach ($dir in $candidates) {
        if (Test-Path (Join-Path $dir "HS4.exe")) {
            return $dir
        }
    }

    throw "Unable to locate HomeSeer installation directory (HS4.exe not found in standard paths)."
}

Set-Location $repoRoot

if (Test-Path $logPath) {
    Remove-Item $logPath -Force
}

try {
    Write-Log "Starting deployment from $repoRoot"

    $git = Find-Git
    $dotnet = Find-Dotnet
    $hsRoot = Find-HomeSeerRoot

    Write-Log "Using git: $git"
    Write-Log "Using dotnet: $dotnet"
    Write-Log "Detected HomeSeer root: $hsRoot"

    Invoke-Checked -Label "Git fetch" -FilePath $git -Arguments @("fetch", "--all", "--prune")
    Invoke-Checked -Label "Git checkout main" -FilePath $git -Arguments @("checkout", "main")
    Invoke-Checked -Label "Git pull origin main" -FilePath $git -Arguments @("pull", "origin", "main")

    $projectPath = Join-Path $repoRoot "hs4-plugin\HSPI_RoborockBridge\HSPI_RoborockBridge.csproj"
    if (!(Test-Path $projectPath)) {
        throw "Project file not found: $projectPath"
    }

    Invoke-Checked -Label "Restoring plugin project" -FilePath $dotnet -Arguments @("restore", $projectPath)
    Invoke-Checked -Label "Building plugin project" -FilePath $dotnet -Arguments @("build", $projectPath, "-c", "Release", "--no-restore")

    $outputDir = Join-Path $repoRoot "hs4-plugin\HSPI_RoborockBridge\bin\Release\net48"
    if (!(Test-Path $outputDir)) {
        throw "Build output folder not found: $outputDir"
    }

    Write-Log "Copying plugin output to $hsRoot"
    Copy-Item (Join-Path $outputDir "*") -Destination $hsRoot -Recurse -Force

    Write-Log "Deployment complete"
    exit 0
}
catch {
    Write-Log ("Deployment failed: " + $_.Exception.Message)
    exit 1
}
