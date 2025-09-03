@echo off
setlocal enabledelayedexpansion
set APP=253430
set DEPOT=253431
set USER=fireking120

:: Create a file to track which folders are from beta manifests
set "beta_folders=%temp%\beta_folders.txt"
if exist "%beta_folders%" del "%beta_folders%"

:: Mapping of manifest_id -> version (for later JSON patch)
set "manifest_version_map=%temp%\manifest_version_map.txt"
if exist "%manifest_version_map%" del "%manifest_version_map%"

for /f "tokens=* delims=" %%M in (manifests.txt) do (
    set "manifest=%%M"
    
    :: Check if manifest ID ends with "beta" (case-insensitive)
    if /i "!manifest:~-4!" == "beta" (
        :: Strip "beta" from the manifest ID
        set "clean_manifest=!manifest:~0,-4!"
        echo Downloading manifest !clean_manifest! from beta branch...
        DepotDownloader.exe -app %APP% -depot %DEPOT% -manifest !clean_manifest! -username %USER% -remember-password -branch beta -dir "versions\%%M"
        :: Record that this folder is from a beta manifest
        echo %%M >> "%beta_folders%"
    ) else (
        echo Downloading manifest %%M from default branch...
        DepotDownloader.exe -app %APP% -depot %DEPOT% -manifest %%M -username %USER% -remember-password -dir "versions\%%M"
    )
)

echo.
echo All downloads finished! Starting folder renaming process...

:: Change to versions directory
cd versions

:: Create a temporary file to track version counts
set "temp_file=%temp%\version_counts.txt"
if exist "%temp_file%" del "%temp_file%"

:: First pass - collect all versions and count duplicates
for /d %%F in (*) do (
    echo Processing folder: %%F
    
    :: Look for executable files (.exe, .dll) in the folder
    set "version="
    set "found_file="
    
    :: Check for .exe files first
    for %%E in ("%%F\*.exe") do (
        if exist "%%E" (
            set "found_file=%%E"
            call :get_version "!found_file!"
        )
    )
    
    :: If no .exe found, check for .dll files
    if not defined found_file (
        for %%D in ("%%F\*.dll") do (
            if exist "%%D" (
                set "found_file=%%D"
                call :get_version "!found_file!"
            )
        )
    )
    
    if defined found_file (
        echo Found assembly file: !found_file!
        echo Found version: !version!
        
        if defined version (
            :: Count occurrences of this version
            set "count=0"
            if exist "%temp_file%" (
                for /f "tokens=1,2 delims=:" %%A in (%temp_file%) do (
                    if "%%A"=="!version!" set "count=%%B"
                )
            )
            
            :: Increment count
            set /a count+=1
            
            :: Update the count file
            set "found=false"
            if exist "%temp_file%" (
                for /f "tokens=1,2 delims=:" %%A in (%temp_file%) do (
                    if "%%A"=="!version!" set "found=true"
                )
            )
            
            if "!found!"=="false" (
                echo !version!:!count! >> "%temp_file%"
            ) else (
                :: Replace the line with updated count
                set "temp_file2=%temp%\version_counts_temp.txt"
                if exist "!temp_file2!" del "!temp_file2!"
                for /f "tokens=1,2 delims=:" %%A in (%temp_file%) do (
                    if "%%A"=="!version!" (
                        echo %%A:!count! >> "!temp_file2!"
                    ) else (
                        echo %%A:%%B >> "!temp_file2!"
                    )
                )
                move /y "!temp_file2!" "%temp_file%" >nul
            )
        ) else (
            echo No version found for %%F
        )
    ) else (
        echo No executable files found in %%F
    )
)

:: Second pass - rename folders with proper b numbering (separate counters for beta vs normal)
set "rename_log=%temp%\rename_log.txt"
if exist "%rename_log%" del "%rename_log%"

for /d %%F in (*) do (
    set "current_folder=%%F"
    :: Look for executable files (.exe, .dll) in the folder
    set "version="
    set "found_file="
    
    :: Determine if this is a beta folder by suffix (more reliable)
    set "is_beta=false"
    if /i "!current_folder:~-4!"=="beta" set "is_beta=true"
    
    :: Fallback to the recorded list if needed
    if "!is_beta!"=="false" if exist "%beta_folders%" (
        for /f "usebackq tokens=* delims=" %%B in ("%beta_folders%") do (
            if /i "%%B"=="!current_folder!" set "is_beta=true"
        )
    )
    
    :: Check for .exe files first
    for %%E in ("!current_folder!\*.exe") do (
        if exist "%%E" (
            set "found_file=%%E"
            call :get_version "!found_file!"
        )
    )
    
    :: If no .exe found, check for .dll files
    if not defined found_file (
        for %%D in ("!current_folder!\*.dll") do (
            if exist "%%D" (
                set "found_file=%%D"
                call :get_version "!found_file!"
            )
        )
    )
    
    if defined found_file (
        if defined version (
            :: Build a per-branch key for separate counters
            if "!is_beta!"=="true" (
                set "version_key=!version!|beta"
            ) else (
                set "version_key=!version!|stable"
            )
            
            :: Check how many times we've used this version (per-branch) so far
            set "usage_count=0"
            if exist "%rename_log%" (
                for /f "tokens=1,2 delims=:" %%A in (%rename_log%) do (
                    if "%%A"=="!version_key!" set "usage_count=%%B"
                )
            )
            
            :: Increment usage count
            set /a usage_count+=1
            
            :: Determine new folder name based on whether it was beta
            if "!is_beta!"=="true" (
                if !usage_count! equ 1 (
                    set "new_name=!version!-beta"
                ) else (
                    set "new_name=!version!b!usage_count!-beta"
                )
            ) else (
                if !usage_count! equ 1 (
                    set "new_name=!version!"
                ) else (
                    set "new_name=!version!b!usage_count!"
                )
            )
            
            :: Check if target folder already exists (rare edge)
            set "counter=!usage_count!"
            set "final_name=!new_name!"
            :check_exists_loop
            if exist "!final_name!" (
                set /a counter+=1
                if "!is_beta!"=="true" (
                    set "final_name=!version!b!counter!-beta"
                ) else (
                    set "final_name=!version!b!counter!"
                )
                goto check_exists_loop
            )
            
            echo Renaming "!current_folder!" to "!final_name!"
            ren "!current_folder!" "!final_name!"
            if errorlevel 1 (
                echo Error renaming folder !current_folder! - target may already exist or folder may be in use
            ) else (
                echo Successfully renamed !current_folder! to !final_name!
            )
            
            :: Update the rename log with per-branch key
            set "found_in_log=false"
            if exist "%rename_log%" (
                for /f "tokens=1,2 delims=:" %%A in (%rename_log%) do (
                    if "%%A"=="!version_key!" set "found_in_log=true"
                )
            )
            
            if "!found_in_log!"=="false" (
                echo !version_key!:!usage_count! >> "%rename_log%"
            ) else (
                :: Update existing entry
                set "temp_log=%temp%\rename_log_temp.txt"
                if exist "!temp_log!" del "!temp_log!"
                for /f "tokens=1,2 delims=:" %%A in (%rename_log%) do (
                    if "%%A"=="!version_key!" (
                        echo %%A:!usage_count! >> "!temp_log!"
                    ) else (
                        echo %%A:%%B >> "!temp_log!"
                    )
                )
                move /y "!temp_log!" "%rename_log%" >nul
            )

            :: Record mapping manifest_id -> version (keep full final name, including -beta and b{num})
            set "manifest_id=!current_folder!"
            if /i "!is_beta!"=="true" set "manifest_id=!manifest_id:~0,-4!"
            set "version_for_json=!final_name!"
            echo !manifest_id!:!version_for_json!>> "%manifest_version_map%"
        ) else (
            echo Skipping !current_folder! - no version found
        )
    )
)

::  Cleanup temporary files
if exist "%temp_file%" del "%temp_file%"
if exist "%rename_log%" del "%rename_log%"
if exist "%beta_folders%" del "%beta_folders%"

:: Return to original directory
cd ..

:: Build versions.json mapping: { version: { manifest: id, branch: public|beta }, ... }
echo Creating versions.json...
set "psfile=%temp%\gen_versions_%RANDOM%.ps1"
if exist "%psfile%" del "%psfile%"
(
    echo $versionsDir = Join-Path (Get-Location) 'versions'
    echo $map = @{
    echo Get-ChildItem -Directory -LiteralPath $versionsDir ^| ForEach-Object {
    echo ^    $folder = $_.Name
    echo ^    $version = $folder
    echo ^    if ($version -match '-beta$') { $branch = 'beta' } else { $branch = 'public' }
    echo ^    $manifestId = $null
    echo ^    $manifestFiles = Get-ChildItem -Path $_.FullName -Filter '*.manifest' -File -Recurse -ErrorAction SilentlyContinue
    echo ^    foreach ($mf in $manifestFiles) { if ($mf.BaseName -match '^[0-9]+_([0-9]+)$') { $manifestId = $matches[1]; break } }
    echo ^    $map[$version] = @{ manifest = $manifestId; branch = $branch }
    echo }
    echo $json = $map ^| ConvertTo-Json -Depth 3
    echo Set-Content -LiteralPath 'versions.json' -Value $json -Encoding UTF8
) > "%psfile%"

powershell -NoProfile -ExecutionPolicy Bypass -File "%psfile%"
del "%psfile%"

:: Patch data\cmz-manifests.json: set version for matching manifest_id
set "json_path=%~dp0..\data\cmz-manifests.json"
set "psfile2=%temp%\patch_cmz_%RANDOM%.ps1"
if exist "%psfile2%" del "%psfile2%"
(
    echo $mapPath = "$env:TEMP\manifest_version_map.txt"
    echo if (Test-Path $mapPath) {
    echo ^    $pairs = Get-Content -LiteralPath $mapPath
    echo ^    $map = @{
    echo ^    foreach ($line in $pairs) {
    echo ^        if ($line -match '^([0-9]+):(.+)$') { $map[$matches[1]] = $matches[2] }
    echo ^    }
    echo ^    $jsonPath = '%json_path%'
    echo ^    if (Test-Path $jsonPath) {
    echo ^        $content = Get-Content -Raw -LiteralPath $jsonPath
    echo ^        $arr = $content ^| ConvertFrom-Json
    echo ^        foreach ($item in $arr) {
    echo ^            $mid = [string]$item.manifest_id
    echo ^            if ($map.ContainsKey($mid)) { $item.version = $map[$mid] }
    echo ^        }
    echo ^        $out = $arr ^| ConvertTo-Json -Depth 10
    echo ^        Set-Content -LiteralPath $jsonPath -Value $out -Encoding UTF8
    echo ^    } else { Write-Host "JSON path not found: $jsonPath" }
    echo }
) > "%psfile2%"

powershell -NoProfile -ExecutionPolicy Bypass -File "%psfile2%"
del "%psfile2%"
if exist "%manifest_version_map%" del "%manifest_version_map%"

echo.
echo All downloads, folder renaming, and manifest.json updates complete!
pause
goto :eof

:get_version
set "file_path=%~1"
for /f "usebackq delims=" %%V in (`powershell -Command "try { $file = Get-Item '%file_path%'; $version = $file.VersionInfo.FileVersion; if ([string]::IsNullOrEmpty($version)) { $version = $file.VersionInfo.ProductVersion }; if ([string]::IsNullOrEmpty($version)) { $assembly = [System.Reflection.Assembly]::LoadFile($file.FullName); $assemblyVersion = $assembly.GetName().Version.ToString(); $version = $assemblyVersion }; Write-Output $version } catch { Write-Output 'ERROR' }"`) do (
    if "%%V" neq "ERROR" if "%%V" neq "" (
        set "version=%%V"
    )
)
goto :eofgoto :eof