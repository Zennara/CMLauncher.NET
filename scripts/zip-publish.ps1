param(
    [Parameter(Mandatory = $true)][string]$Directory,
    [Parameter(Mandatory = $true)][string]$ZipName
)

$ErrorActionPreference = 'Stop'

$root = Resolve-Path -LiteralPath $Directory -ErrorAction Stop | Select-Object -ExpandProperty Path
if (-not (Test-Path -LiteralPath $root -PathType Container)) {
    Write-Error "Directory not found: $Directory"
    exit 1
}

$zipPath = Join-Path $root $ZipName
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }

$zipLeaf = Split-Path -Leaf $zipPath

Push-Location -LiteralPath $root
try {
    # Zip the full folder so the directory structure is preserved
    Compress-Archive -Path '*' -DestinationPath $zipPath -CompressionLevel Optimal -Force

    # Remove manifest.json from inside the zip
    Add-Type -AssemblyName System.IO.Compression.FileSystem | Out-Null
    $archive = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Update)
    try {
        $entries = @($archive.Entries)
        foreach ($e in $entries) {
            if ([System.IO.Path]::GetFileName($e.FullName) -eq 'manifest.json') {
                $e.Delete()
            }
        }
    }
    finally {
        $archive.Dispose()
    }

    # Clean the publish folder so it only contains manifest.json and the zip
    Get-ChildItem -LiteralPath . -Force | Where-Object { $_.Name -ne 'manifest.json' -and $_.Name -ne $zipLeaf } | Remove-Item -Recurse -Force
}
finally {
    Pop-Location
}

Write-Host "Created: $zipPath"
