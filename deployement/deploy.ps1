# ============================================================
# Script de déploiement depuis une GitHub Release (PowerShell)
# Usage : .\deployement\deploy.ps1 -RemoteHost "203.0.113.42" [-User "root"] [-Version "1.2.0"]
#
# Sans -Version : déploie la dernière release
# Avec -Version : déploie la version spécifiée
# ============================================================

param(
    [Parameter(Mandatory=$true)]
    [string]$RemoteHost,
    [string]$User = "root",
    [string]$Version
)

$ErrorActionPreference = "Stop"

$AppDir = "/var/www/ecbatan-location"
$Repo = "<votre-org>/ecbatan-location"

if (-not $Version) {
    Write-Host "==> Récupération de la dernière version..." -ForegroundColor Cyan
    $Version = (gh release view --repo $Repo --json tagName -q '.tagName') -replace '^v', ''
    Write-Host "    Dernière version : v${Version}"
} else {
    Write-Host "==> Version demandée : v${Version}" -ForegroundColor Cyan
}

$Archive = "ecbatan-location-${Version}-linux-x64.tar.gz"
$DownloadUrl = "https://github.com/${Repo}/releases/download/v${Version}/${Archive}"

Write-Host "==> Téléchargement de ${Archive}..." -ForegroundColor Cyan
$TmpDir = Join-Path ([System.IO.Path]::GetTempPath()) "ecbatan-deploy"
New-Item -ItemType Directory -Force $TmpDir | Out-Null
$LocalPath = Join-Path $TmpDir $Archive

try {
    Invoke-WebRequest -Uri $DownloadUrl -OutFile $LocalPath

    Write-Host "==> Envoi sur le serveur..." -ForegroundColor Cyan
    scp $LocalPath "${User}@${RemoteHost}:/tmp/${Archive}"

    Write-Host "==> Déploiement sur le serveur..." -ForegroundColor Cyan
    ssh "${User}@${RemoteHost}" @"
sudo systemctl stop ecbatan-location
sudo tar -xzf /tmp/${Archive} -C ${AppDir}/
sudo chown -R planning:planning ${AppDir}
sudo systemctl start ecbatan-location
rm /tmp/${Archive}
"@

    Write-Host "==> Vérification du statut..." -ForegroundColor Cyan
    ssh "${User}@${RemoteHost}" "sleep 2 && systemctl is-active ecbatan-location"

    Write-Host ""
    Write-Host "==> Déploiement v${Version} terminé avec succès !" -ForegroundColor Green
} finally {
    Remove-Item -Recurse -Force $TmpDir -ErrorAction SilentlyContinue
}
