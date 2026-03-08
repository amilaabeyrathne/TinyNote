# Build Docker images exactly as CDK/ECS does
# Run from project root (Scania folder)

param(
    [switch]$Api,
    [switch]$Frontend,
    [string]$Tag = "ecs"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

# CDK uses linux/amd64 for API; use for both to match Fargate
$platform = "linux/amd64"

if (-not $Api -and -not $Frontend) {
    $Api = $true
    $Frontend = $true
}

if ($Api) {
    Write-Host "Building API image (platform=$platform)..." -ForegroundColor Cyan
    docker build --platform $platform `
        -f "$root/TinyNoteBackEnd/TinyNote.Api/Dockerfile" `
        -t "tinynote-api:$Tag" `
        "$root/TinyNoteBackEnd"
    Write-Host "API image: tinynote-api:$Tag" -ForegroundColor Green
}

if ($Frontend) {
    Write-Host "Building Frontend image (platform=$platform)..." -ForegroundColor Cyan
    docker build --platform $platform `
        -f "$root/TinyNoteFrontEnd/TinyNote/Dockerfile" `
        -t "tinynote-frontend:$Tag" `
        "$root/TinyNoteFrontEnd/TinyNote"
    Write-Host "Frontend image: tinynote-frontend:$Tag" -ForegroundColor Green
}
