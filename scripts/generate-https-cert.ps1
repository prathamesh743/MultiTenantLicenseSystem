param(
    [string]$OutputDirectory = "certs",
    [string]$Password = "changeit",
    [string]$CertificateName = "localhost"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet SDK is required to generate HTTPS certificates."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

$certPath = Join-Path $targetDir "$CertificateName.pfx"
dotnet dev-certs https -ep $certPath -p $Password | Out-Null

Write-Host "HTTPS certificate exported to: $certPath"
Write-Host "Password: $Password"
Write-Host "Use with: docker compose -f docker-compose.yml -f docker-compose.https.yml up --build"
