<#
.SYNOPSIS
    Régénère les notes de toutes les releases GitHub existantes en appliquant
    la configuration .github/release.yml (catégorisation par labels).

.DESCRIPTION
    L'API GitHub `generate-notes` lit .github/release.yml depuis la branche
    par défaut. Ce fichier doit donc être présent sur `main` AVANT de lancer
    ce script.

    Pour chaque tag (du plus ancien au plus récent), le script régénère les
    notes en passant le tag précédent comme borne, puis réécrit le corps de
    la release. Seul le texte est modifié — les artefacts attachés ne sont
    pas touchés. Le script est idempotent : relançable sans risque.

.PARAMETER Repo
    Dépôt au format owner/name. Détecté automatiquement si omis.

.PARAMETER WhatIf
    Affiche les notes générées sans modifier les releases.

.EXAMPLE
    ./scripts/Regenerate-ReleaseNotes.ps1
    ./scripts/Regenerate-ReleaseNotes.ps1 -WhatIf
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Repo
)

$ErrorActionPreference = 'Stop'

if (-not $Repo) {
    $Repo = gh repo view --json nameWithOwner -q '.nameWithOwner'
}
Write-Host "Dépôt : $Repo" -ForegroundColor Cyan

# Tags du plus ancien au plus récent (tri sémantique sur la version sans le 'v')
$tags = gh release list --repo $Repo --limit 200 --json tagName -q '.[].tagName' |
        Sort-Object { [version]($_ -replace '^v') }

if (-not $tags) {
    Write-Warning "Aucune release trouvée."
    return
}

$prev = $null
foreach ($tag in $tags) {
    $apiArgs = @(
        "repos/$Repo/releases/generate-notes",
        "-f", "tag_name=$tag"
    )
    if ($prev) { $apiArgs += @("-f", "previous_tag_name=$prev") }

    $notes = gh api @apiArgs -q '.body'

    if ($PSCmdlet.ShouldProcess($tag, "Réécrire les notes de release")) {
        $notes | gh release edit $tag --repo $Repo --notes-file -
        Write-Host "✓ $tag régénéré" -ForegroundColor Green
    }
    else {
        Write-Host "---- $tag (aperçu) ----" -ForegroundColor Yellow
        Write-Host $notes
    }

    $prev = $tag
}
