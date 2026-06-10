# ============================================================
# Script de déploiement (version PowerShell pour Windows)
# Usage : .\deployement\deploy.ps1 -Host "203.0.113.42" [-User "root"]
# ============================================================

param(
    [Parameter(Mandatory=$true)]
    [string]$RemoteHost,
    [string]$User = "root"
)

$ErrorActionPreference = "Stop"

$AppDir = "/var/www/ecbatan-location"
$PublishDir = ".\publish"
$Project = "src\EcbatanLocation.Web\EcbatanLocation.Web.csproj"

Write-Host "==> Compilation en Release..." -ForegroundColor Cyan
dotnet publish $Project `
    -c Release `
    -o $PublishDir `
    --self-contained false `
    -r linux-x64

Write-Host "==> Copie de la config de production..." -ForegroundColor Cyan
Copy-Item "deployement\appsettings.Production.json" -Destination $PublishDir

Write-Host "==> Envoi des fichiers sur le serveur..." -ForegroundColor Cyan
scp -r "$PublishDir\*" "${User}@${RemoteHost}:${AppDir}/"

Write-Host "==> Correction des permissions..." -ForegroundColor Cyan
ssh "${User}@${RemoteHost}" "chown -R planning:planning ${AppDir}"

Write-Host "==> Redémarrage de l'application..." -ForegroundColor Cyan
ssh "${User}@${RemoteHost}" "systemctl restart ecbatan-location"

Write-Host "==> Vérification du statut..." -ForegroundColor Cyan
ssh "${User}@${RemoteHost}" "sleep 2 && systemctl is-active ecbatan-location"

Write-Host ""
Write-Host "==> Déploiement terminé avec succès !" -ForegroundColor Green

# Nettoyage local
Remove-Item -Recurse -Force $PublishDir
