# TrackerPlus - 打包發行並部署至遠端主機
# 用法：
#   .\publish.ps1
#   .\publish.ps1 -RemotePath '\\192.168.1.10\D$\Apps\TrackerPlus'
#   .\publish.ps1 -RemotePath '\\server\share\TrackerPlus' -Zip
#   .\publish.ps1 -OutputDir '.\artifacts\publish' -PublishOnly
#   .\publish.ps1 -RemotePath '\\host\share\app' -PreserveRemoteConfig
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [string] $OutputDir = '',

    [string] $RemotePath = '',

    [switch] $Zip,
    [switch] $PublishOnly,
    [switch] $NoRestore,
    [switch] $PreserveRemoteConfig,
    [switch] $SelfContained,

    [string] $Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'

$repoRoot = $PSScriptRoot
$sln = Join-Path $repoRoot 'TrackerPlus.sln'
$webProject = Join-Path $repoRoot 'TrackerPlus.Web\TrackerPlus.Web.csproj'
$artifactsRoot = Join-Path $repoRoot 'artifacts'

if (-not (Test-Path $sln)) {
    Write-Error "Solution not found: $sln"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error 'dotnet CLI not found. Install .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0'
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $OutputDir = Join-Path $artifactsRoot "TrackerPlus-$stamp"
}

$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)

Write-Host '==> TrackerPlus publish' -ForegroundColor Cyan
Write-Host "    .NET SDK: $(dotnet --version)"
Write-Host "    Configuration: $Configuration"
Write-Host "    Output: $OutputDir"
if ($RemotePath) {
    Write-Host "    Remote: $RemotePath"
}
Write-Host ''

Set-Location $repoRoot

if (-not $NoRestore) {
    Write-Host '==> dotnet restore' -ForegroundColor Yellow
    dotnet restore $sln
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if (Test-Path $OutputDir) {
    Write-Host "==> Clean output folder: $OutputDir" -ForegroundColor Yellow
    Remove-Item -LiteralPath $OutputDir -Recurse -Force
}

Write-Host '==> dotnet publish' -ForegroundColor Yellow
$publishArgs = @(
    'publish', $webProject,
    '-c', $Configuration,
    '-o', $OutputDir,
    '--no-restore'
)

if ($SelfContained) {
    $publishArgs += @('-r', $Runtime, '--self-contained', 'true')
} else {
    $publishArgs += @('--self-contained', 'false')
}

dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ''
Write-Host "Publish succeeded: $OutputDir" -ForegroundColor Green

if ($Zip) {
    $zipPath = "$OutputDir.zip"
    if (Test-Path $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    Write-Host ''
    Write-Host "==> Create zip: $zipPath" -ForegroundColor Yellow
    Compress-Archive -Path (Join-Path $OutputDir '*') -DestinationPath $zipPath -CompressionLevel Optimal
    Write-Host "Zip created: $zipPath" -ForegroundColor Green
}

if ($PublishOnly -or [string]::IsNullOrWhiteSpace($RemotePath)) {
    if ([string]::IsNullOrWhiteSpace($RemotePath)) {
        Write-Host ''
        Write-Host 'Tip: use -RemotePath to copy to another host, e.g.' -ForegroundColor DarkGray
        Write-Host "  .\publish.ps1 -RemotePath '\\192.168.1.10\D`$\Apps\TrackerPlus'" -ForegroundColor DarkGray
    }
    exit 0
}

$remoteTarget = $RemotePath.TrimEnd('\', '/')
Write-Host ''
Write-Host "==> Deploy to remote: $remoteTarget" -ForegroundColor Yellow

if (-not (Test-Path $remoteTarget)) {
    Write-Host "Remote folder not found, creating: $remoteTarget" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $remoteTarget -Force | Out-Null
}

$robocopyArgs = @(
    $OutputDir,
    $remoteTarget,
    '/MIR',
    '/Z',
    '/FFT',
    '/R:3',
    '/W:5',
    '/NFL',
    '/NDL',
    '/NP'
)

if ($PreserveRemoteConfig) {
    $robocopyArgs += '/XF', 'appsettings.json', 'appsettings.Production.json', 'web.config'
    Write-Host '    Preserving remote appsettings.json / web.config' -ForegroundColor DarkGray
}

$robocopyLog = & robocopy @robocopyArgs
$robocopyExit = $LASTEXITCODE

# Robocopy: 0-7 = success, >= 8 = failure
if ($robocopyExit -ge 8) {
    Write-Error "Robocopy failed (exit code $robocopyExit)."
}

Write-Host ''
Write-Host "Deploy succeeded: $remoteTarget" -ForegroundColor Green
exit 0
