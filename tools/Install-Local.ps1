param([switch]$Start)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$source = Join-Path $repoRoot 'Desktop\Application\MaxMix\bin\Release'
if (-not (Test-Path (Join-Path $source 'MaxMix.exe'))) { throw 'Compila prima con tools\Build-Local.ps1.' }
$destination = Join-Path $env:LOCALAPPDATA 'Programs\Maxmix\1.5.9'
if (Get-Process MaxMix -ErrorAction SilentlyContinue) { throw 'Chiudi Maxmix con Esci prima di installare.' }
if (Test-Path $destination) {
    $backup = Join-Path $repoRoot ('backups\desktop-before-install-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    Copy-Item -LiteralPath $destination -Destination $backup -Recurse
}
New-Item -ItemType Directory -Force $destination | Out-Null
Get-ChildItem -LiteralPath $source -File | Where-Object { $_.Extension -in '.exe', '.dll', '.config' } |
    Copy-Item -Destination $destination -Force
$executable = Join-Path $destination 'MaxMix.exe'
$shell = New-Object -ComObject WScript.Shell
foreach ($folder in @([Environment]::GetFolderPath('Desktop'), (Join-Path ([Environment]::GetFolderPath('StartMenu')) 'Programs'))) {
    $link = $shell.CreateShortcut((Join-Path $folder 'Maxmix.lnk'))
    $link.TargetPath = $executable
    $link.WorkingDirectory = $destination
    $link.Description = 'Maxmix 1.5.9 - controller audio'
    $link.IconLocation = "$executable,0"
    $link.Save()
}
Write-Output "Installato: $executable"
if ($Start) { Start-Process -FilePath $executable -WorkingDirectory $destination }
