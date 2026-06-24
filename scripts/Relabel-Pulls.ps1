<#
.SYNOPSIS
    (Re)labellise les pull requests existantes d'après leur type de changement,
    pour que la catégorisation des release notes (.github/release.yml) s'applique
    aussi rétroactivement.

.DESCRIPTION
    Le type est déduit, par ordre de priorité :
      1. du préfixe Conventional Commit du titre de PR  (ex. "feat: ...", "fix(scope)!: ...")
      2. à défaut, du préfixe de la branche source        (ex. "feat/...", "fix/...")

    Mapping type -> label (aligné sur .github/release.yml) :
      feat, feature        -> feat
      fix, hotfix          -> fix
      refactor             -> refactor
      docs                 -> documentation
      chore                -> chore
      ci, build            -> ci

    Les PR Dependabot et celles déjà correctement labellisées sont ignorées
    (le label n'est ajouté que s'il manque). Script idempotent.

.PARAMETER Repo
    Dépôt au format owner/name. Détecté automatiquement si omis.

.PARAMETER State
    État des PR à traiter : merged (défaut), closed, open, all.

.PARAMETER WhatIf
    Affiche les actions sans rien modifier.

.EXAMPLE
    ./scripts/Relabel-Pulls.ps1 -WhatIf
    ./scripts/Relabel-Pulls.ps1
    ./scripts/Relabel-Pulls.ps1 -State all
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Repo,
    [ValidateSet('merged', 'closed', 'open', 'all')]
    [string]$State = 'merged'
)

$ErrorActionPreference = 'Stop'

# Forcer l'UTF-8 pour les échanges avec gh (titres de PR accentués).
$utf8 = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = $utf8
try { [Console]::OutputEncoding = $utf8 } catch {}
try { [Console]::InputEncoding = $utf8 } catch {}

if (-not $Repo) {
    $Repo = gh repo view --json nameWithOwner -q '.nameWithOwner'
}
Write-Host "Dépôt : $Repo (état: $State)" -ForegroundColor Cyan

# type Conventional Commit -> label du dépôt
$typeToLabel = @{
    feat     = 'feat'
    feature  = 'feat'
    fix      = 'fix'
    hotfix   = 'fix'
    refactor = 'refactor'
    docs     = 'documentation'
    chore    = 'chore'
    ci       = 'ci'
    build    = 'ci'
    deps     = 'dependencies'
}

function Resolve-Type {
    param([string]$Title, [string]$Branch)

    # 1. préfixe Conventional Commit du titre : type(scope)!: ...
    if ($Title -match '^\s*([a-zA-Z]+)(\([^)]*\))?!?:') {
        $t = $Matches[1].ToLower()
        if ($typeToLabel.ContainsKey($t)) { return $typeToLabel[$t] }
    }
    # 2. préfixe de branche : type/...
    if ($Branch -match '^([a-zA-Z]+)/') {
        $t = $Matches[1].ToLower()
        if ($typeToLabel.ContainsKey($t)) { return $typeToLabel[$t] }
    }
    return $null
}

$jsonFields = 'number,title,headRefName,labels,author'
$prs =
    if ($State -eq 'all') {
        gh pr list --repo $Repo --state all --limit 500 --json $jsonFields | ConvertFrom-Json
    }
    elseif ($State -eq 'merged') {
        # 'merged' n'est pas un --state valide : on filtre les PR fermées effectivement mergées
        gh pr list --repo $Repo --state closed --limit 500 `
            --json "$jsonFields,mergedAt" |
            ConvertFrom-Json | Where-Object { $_.mergedAt }
    }
    else {
        gh pr list --repo $Repo --state $State --limit 500 --json $jsonFields | ConvertFrom-Json
    }

if (-not $prs) { Write-Warning "Aucune PR trouvée."; return }

$applied = 0; $skipped = 0
foreach ($pr in $prs) {
    if ($pr.author.login -like 'dependabot*') {
        $skipped++; continue
    }

    $label = Resolve-Type -Title $pr.title -Branch $pr.headRefName
    if (-not $label) {
        Write-Host "?  #$($pr.number) type indéterminé — $($pr.title)" -ForegroundColor DarkGray
        $skipped++; continue
    }

    $existing = @($pr.labels.name)
    if ($existing -contains $label) {
        $skipped++; continue
    }

    if ($PSCmdlet.ShouldProcess("PR #$($pr.number)", "Ajouter le label '$label'")) {
        gh pr edit $pr.number --repo $Repo --add-label $label | Out-Null
        Write-Host "✓ #$($pr.number) +$label — $($pr.title)" -ForegroundColor Green
    }
    else {
        Write-Host "→ #$($pr.number) +$label — $($pr.title)" -ForegroundColor Yellow
    }
    $applied++
}

Write-Host ""
Write-Host "Terminé : $applied PR à (re)labelliser, $skipped ignorées." -ForegroundColor Cyan
if ($WhatIfPreference) {
    Write-Host "Mode aperçu — relancez sans -WhatIf pour appliquer." -ForegroundColor Yellow
}
else {
    Write-Host "Pensez ensuite à régénérer les notes : ./scripts/Regenerate-ReleaseNotes.ps1" -ForegroundColor Yellow
}
