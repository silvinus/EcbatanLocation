<#
.SYNOPSIS
    Régénère les notes de toutes les releases GitHub existantes en regroupant
    les PR par catégorie, d'après leurs labels actuels.

.DESCRIPTION
    /!\ L'API GitHub `generate-notes` lit .github/release.yml depuis le commit
    du tag concerné. Pour les tags créés AVANT l'ajout du fichier, la
    catégorisation ne peut donc pas être appliquée par l'API (liste plate).

    Ce script contourne la limite : pour chaque release, il récupère la liste
    des PR de la plage via l'API generate-notes (liste fiable, gérée par
    GitHub), lit les labels ACTUELS de chaque PR, puis reconstruit lui-même
    les notes catégorisées en miroir de .github/release.yml.

    Si une plage ne contient aucune PR (changements poussés en commits directs
    sur la branche), le script bascule sur un fallback qui catégorise les
    messages de commit d'après leur préfixe Conventional Commit (fix:, feat:…).

    Seul le texte de la release est modifié — les artefacts attachés et le tag
    ne sont pas touchés. Le script est idempotent : relançable sans risque.

.PARAMETER Repo
    Dépôt au format owner/name. Détecté automatiquement si omis.

.PARAMETER WhatIf
    Affiche les notes générées sans modifier les releases.

.EXAMPLE
    ./scripts/Regenerate-ReleaseNotes.ps1 -WhatIf
    ./scripts/Regenerate-ReleaseNotes.ps1
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Repo
)

$ErrorActionPreference = 'Stop'

# --- Catégories, en miroir de .github/release.yml (ordre = priorité) ---------
$categories = [ordered]@{
    '🐛 Corrections'     = @('bug', 'fix')
    '🚀 Fonctionnalités' = @('enhancement', 'feat', 'feature')
    '♻️ Refactoring'     = @('refactor')
    '📝 Documentation'   = @('documentation', 'docs')
    '🤖 Dépendances'     = @('dependencies')
    '🔧 CI / Build'      = @('ci')
    '🧹 Maintenance'     = @('chore')
}
$catchAllTitle = '🔄 Autres changements'
$excludeLabels = @('ignore-for-release')

# Préfixe Conventional Commit -> titre de catégorie (fallback sans PR)
$prefixToCat = @{
    fix = '🐛 Corrections'; bug = '🐛 Corrections'
    feat = '🚀 Fonctionnalités'; feature = '🚀 Fonctionnalités'; perf = '🚀 Fonctionnalités'
    refactor = '♻️ Refactoring'
    docs = '📝 Documentation'
    deps = '🤖 Dépendances'
    ci = '🔧 CI / Build'; build = '🔧 CI / Build'
    chore = '🧹 Maintenance'
}
# ---------------------------------------------------------------------------

if (-not $Repo) {
    $Repo = gh repo view --json nameWithOwner -q '.nameWithOwner'
}
Write-Host "Dépôt : $Repo" -ForegroundColor Cyan

# Labels de toutes les PR, en une seule requête (numéro -> liste de labels)
Write-Host "Chargement des labels de PR..." -ForegroundColor DarkGray
$prLabels = @{}
gh pr list --repo $Repo --state all --limit 500 --json number,labels |
    ConvertFrom-Json |
    ForEach-Object { $prLabels[[string]$_.number] = @($_.labels.name) }

function Get-CategoryFor {
    param([string]$PrNumber)
    $labels = $prLabels[$PrNumber]
    if (-not $labels) { return $catchAllTitle }
    if ($labels | Where-Object { $excludeLabels -contains $_ }) { return $null }  # exclu
    foreach ($cat in $categories.Keys) {
        if ($labels | Where-Object { $categories[$cat] -contains $_ }) { return $cat }
    }
    return $catchAllTitle
}

# Entrées catégorisées issues des commits directs d'une plage (fallback sans PR).
# Retourne une table ordonnée catégorie -> liste de lignes "* ...".
function Get-CommitCategories {
    param([string]$Prev, [string]$Tag)

    if (-not $Prev) { return $null }   # pas de borne basse : on ne tente pas

    $raw = gh api "repos/$Repo/compare/$Prev...$Tag" `
        -q '.commits[] | "\(.sha[0:7])\t\(.commit.message | split("\n")[0])"' 2>$null
    if (-not $raw) { return $null }

    $result = [ordered]@{}
    foreach ($entry in @($raw)) {
        $sha, $subject = $entry -split "`t", 2
        if (-not $subject) { continue }
        if ($subject -match '^Merge ') { continue }   # commits de merge ignorés

        $cat = $catchAllTitle
        if ($subject -match '^\s*([a-zA-Z]+)(\([^)]*\))?!?:') {
            $p = $Matches[1].ToLower()
            if ($prefixToCat.ContainsKey($p)) { $cat = $prefixToCat[$p] }
        }
        if (-not $result.Contains($cat)) {
            $result[$cat] = New-Object System.Collections.Generic.List[string]
        }
        $result[$cat].Add("* $subject ($sha)")
    }
    if ($result.Count -eq 0) { return $null }
    return $result
}

function Build-Notes {
    param([string]$FlatBody, [string]$Prev, [string]$Tag)

    $prLines = [ordered]@{}           # catégorie -> liste de lignes "* ..."
    $tail = New-Object System.Collections.Generic.List[string]  # New Contributors, Full Changelog...
    $mode = 'prs'

    foreach ($line in ($FlatBody -split "`r?`n")) {
        # Bascule en mode "tail" : section additionnelle (New Contributors) ou Full Changelog
        if (($line -match '^##\s' -and $line -notmatch "What's Changed") -or $line -match 'Full Changelog') {
            $mode = 'tail'
        }

        if ($mode -eq 'tail') {
            $tail.Add($line)
            continue
        }

        # mode "prs" : on ne retient que les lignes de PR ; on ignore l'entête et les lignes vides
        if ($line -match '^\s*\*\s' -and $line -match 'pull/(\d+)') {
            $num = $Matches[1]
            $cat = Get-CategoryFor -PrNumber $num
            if (-not $cat) { continue }                # PR exclue
            if (-not $prLines.Contains($cat)) {
                $prLines[$cat] = New-Object System.Collections.Generic.List[string]
            }
            $prLines[$cat].Add($line)
        }
    }

    # Nettoyage : retirer d'éventuelles lignes vides en tête de tail
    while ($tail.Count -gt 0 -and [string]::IsNullOrWhiteSpace($tail[0])) { $tail.RemoveAt(0) }

    # Fallback : aucune PR dans la plage -> on catégorise les commits directs
    if ($prLines.Count -eq 0) {
        $commitCats = Get-CommitCategories -Prev $Prev -Tag $Tag
        if ($commitCats) { $prLines = $commitCats }
    }

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine("## What's Changed")
    $emitted = $false
    $orderedTitles = @($categories.Keys) + $catchAllTitle
    foreach ($title in $orderedTitles) {
        if ($prLines.Contains($title)) {
            [void]$sb.AppendLine("### $title")
            foreach ($l in $prLines[$title]) { [void]$sb.AppendLine($l) }
            $emitted = $true
        }
    }
    if (-not $emitted) { return $FlatBody }   # rien à catégoriser : on garde l'original
    if ($tail.Count -gt 0) {
        [void]$sb.AppendLine()
        foreach ($l in $tail) { [void]$sb.AppendLine($l) }
    }
    return $sb.ToString().TrimEnd()
}

# Tags du plus ancien au plus récent (tri sémantique sur la version sans le 'v')
$tags = gh release list --repo $Repo --limit 200 --json tagName -q '.[].tagName' |
        Sort-Object { [version]($_ -replace '^v') }

if (-not $tags) { Write-Warning "Aucune release trouvée."; return }

$prev = $null
foreach ($tag in $tags) {
    $apiArgs = @("repos/$Repo/releases/generate-notes", "-f", "tag_name=$tag")
    if ($prev) { $apiArgs += @("-f", "previous_tag_name=$prev") }

    $flat = (gh api @apiArgs -q '.body') -join "`n"
    $notes = Build-Notes -FlatBody $flat -Prev $prev -Tag $tag

    if ($PSCmdlet.ShouldProcess($tag, "Réécrire les notes de release")) {
        $notes | gh release edit $tag --repo $Repo --notes-file -
        Write-Host "✓ $tag régénéré" -ForegroundColor Green
    }
    else {
        Write-Host "---- $tag (aperçu) ----" -ForegroundColor Yellow
        Write-Host $notes
        Write-Host ""
    }

    $prev = $tag
}

if ($WhatIfPreference) {
    Write-Host "Mode aperçu — relancez sans -WhatIf pour appliquer." -ForegroundColor Yellow
}
