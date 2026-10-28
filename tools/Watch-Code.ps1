$repoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $repoRoot
$Host.UI.RawUI.WindowTitle = 'MAXMIX - modifiche al codice in tempo reale'
$Host.UI.RawUI.BufferSize = New-Object Management.Automation.Host.Size(150, 12000)
$snapshots = @{}
function Read-SourceFiles {
    @(git ls-files --cached --others --exclude-standard) | Where-Object {
        $_ -match '^(Desktop/|Embedded/|Tests/|tools/|installer/)' -and
        $_ -match '\.(cs|xaml|ino|h|cpp|ps1|iss|csproj|config|md)$'
    }
}
foreach ($file in Read-SourceFiles) {
    if (Test-Path -LiteralPath $file) { $snapshots[$file] = [IO.File]::ReadAllText((Join-Path $repoRoot $file)) }
}
Write-Host 'MAXMIX - modifiche ai file appena vengono salvate' -ForegroundColor Cyan
Write-Host 'Le righe verdi vengono aggiunte, quelle rosse vengono rimosse. Le modifiche arrivano a blocchi.'
Write-Host 'File gia aperto nel pannello: Embedded/MaxMix/LightingEffects.h'
while ($true) {
    foreach ($file in Read-SourceFiles) {
        try { $current = [IO.File]::ReadAllText((Join-Path $repoRoot $file)) } catch { continue }
        if ($snapshots.ContainsKey($file) -and $snapshots[$file] -ceq $current) { continue }
        Write-Host "`n[$(Get-Date -Format HH:mm:ss)] $file" -ForegroundColor Yellow
        $oldLines = if ($snapshots.ContainsKey($file)) { @($snapshots[$file] -split '\r?\n') } else { @() }
        $newLines = @($current -split '\r?\n')
        $prefix = 0
        while ($prefix -lt $oldLines.Count -and $prefix -lt $newLines.Count -and $oldLines[$prefix] -ceq $newLines[$prefix]) { $prefix++ }
        $suffix = 0
        while ($suffix -lt ($oldLines.Count - $prefix) -and $suffix -lt ($newLines.Count - $prefix) -and
               $oldLines[$oldLines.Count - 1 - $suffix] -ceq $newLines[$newLines.Count - 1 - $suffix]) { $suffix++ }
        Write-Host ('@@ dalla riga ' + ($prefix + 1) + ' @@')
        for ($i = $prefix; $i -lt $oldLines.Count - $suffix; $i++) { Write-Host ('- ' + $oldLines[$i]) -ForegroundColor Red }
        for ($i = $prefix; $i -lt $newLines.Count - $suffix; $i++) { Write-Host ('+ ' + $newLines[$i]) -ForegroundColor Green }
        $snapshots[$file] = $current
    }
    Start-Sleep -Milliseconds 750
}
