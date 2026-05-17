# TrackerPlus - 編譯並啟動 Web 應用程式
# 用法：
#   .\run.ps1                    # Debug 編譯後執行（http://localhost:5136）
#   .\run.ps1 -Configuration Release
#   .\run.ps1 -BuildOnly         # 只編譯不啟動
#   .\run.ps1 -LaunchProfile https
#   .\run.ps1 -NoRestore
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [ValidateSet('http', 'https', 'IIS Express')]
    [string] $LaunchProfile = 'http',

    [switch] $BuildOnly,
    [switch] $NoRestore
)

$ErrorActionPreference = 'Stop'

$repoRoot = $PSScriptRoot
$sln = Join-Path $repoRoot 'TrackerPlus.sln'
$webProject = Join-Path $repoRoot 'TrackerPlus.Web\TrackerPlus.Web.csproj'

if (-not (Test-Path $sln)) {
    Write-Error "Solution not found: $sln"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error 'dotnet CLI not found. Install .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0'
}

$dotnetVersion = dotnet --version
Write-Host "==> .NET SDK: $dotnetVersion" -ForegroundColor Cyan
Write-Host "==> Configuration: $Configuration" -ForegroundColor Cyan
Set-Location $repoRoot

if (-not $NoRestore) {
    Write-Host ''
    Write-Host '==> dotnet restore' -ForegroundColor Yellow
    dotnet restore $sln
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host ''
Write-Host '==> dotnet build' -ForegroundColor Yellow
dotnet build $sln -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($BuildOnly) {
    Write-Host ''
    Write-Host 'Build succeeded (app not started).' -ForegroundColor Green
    exit 0
}

$env:ASPNETCORE_ENVIRONMENT = 'Development'

Write-Host ''
Write-Host "==> dotnet run (profile: $LaunchProfile)" -ForegroundColor Yellow
Write-Host '    Press Ctrl+C to stop'
if ($LaunchProfile -eq 'http') {
    Write-Host '    Admin login: http://localhost:5136/Admin/Account/Login' -ForegroundColor Green
} elseif ($LaunchProfile -eq 'https') {
    Write-Host '    Admin login: https://localhost:7266/Admin/Account/Login' -ForegroundColor Green
}
Write-Host ''

dotnet run --project $webProject -c $Configuration --no-build --launch-profile $LaunchProfile

exit $LASTEXITCODE
