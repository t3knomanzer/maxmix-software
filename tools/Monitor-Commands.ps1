$repoRoot = Split-Path $PSScriptRoot -Parent
$Host.UI.RawUI.WindowTitle = 'MAXMIX - comandi in tempo reale'
Write-Host 'MAXMIX - questa finestra mostra i comandi e i risultati delle prossime operazioni.' -ForegroundColor Cyan
Get-Content -LiteralPath (Join-Path $repoRoot '.tools\commands-live.log') -Tail 80 -Wait
