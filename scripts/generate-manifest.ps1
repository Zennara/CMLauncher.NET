param(
    [Parameter(Mandatory = $true)][string]$Directory,
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Output = $null,
    [string[]]$Include = @('*')
)

# Resolve and validate directory
$root = Resolve-Path -LiteralPath $Directory -ErrorAction Stop | Select-Object -ExpandProperty Path
if (-not (Test-Path -LiteralPath $root -PathType Container)) {
    Write-Error "Directory not found: $Directory"
    exit 1
}

if (-not $Output) { $Output = Join-Path $root 'manifest.json' }

function Test-MatchPattern {
    param([string]$Path, [string[]]$Patterns)
    foreach ($p in $Patterns) {
        if ($Path -like $p) { return $true }
    }
    return $false
}

$files = Get-ChildItem -LiteralPath $root -Recurse -File | ForEach-Object {
    # Build a forward-slashed relative path
    $abs = (Resolve-Path -LiteralPath $_.FullName).Path
    $rel = $abs.Substring($root.Length).TrimStart('\','/')
    $relForward = $rel -replace '\\','/'

    [PSCustomObject]@{
        Full = $_.FullName
        Rel  = $relForward
    }
}

# Filter with include patterns (patterns are applied to the relative path, forward slashes)
if ($Include -and ($Include.Count -gt 0) -and -not ($Include.Count -eq 1 -and $Include[0] -eq '*')) {
    $files = $files | Where-Object { Test-MatchPattern -Path $_.Rel -Patterns $Include }
}

# Sort for deterministic output
$files = $files | Sort-Object Rel

# Compute SHA256 hashes
$manifestFiles = @()
foreach ($f in $files) {
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $f.Full).Hash.ToLowerInvariant()
    $manifestFiles += [PSCustomObject]@{ path = $f.Rel; hash = $hash }
}

$manifest = [PSCustomObject]@{
    version = $Version
    files   = $manifestFiles
}

# Write JSON
$json = $manifest | ConvertTo-Json -Depth 5
$json | Out-File -LiteralPath $Output -Encoding UTF8

Write-Host "Manifest written to: $Output"
