$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $repoRoot
$Host.UI.RawUI.WindowTitle = 'Maxmix - comandi e risultati in tempo reale'
Start-Transcript -Path (Join-Path $repoRoot '.tools\live-console.log') -Append | Out-Null
try {
    Write-Host 'MAXMIX 1.5.9 - verifica e installazione locale' -ForegroundColor Cyan
    Write-Host 'Il firmware sul dispositivo e gia aggiornato; qui vengono eseguiti i controlli finali e installata la app.'
    Write-Host "`nPS> .\tools\Build-Local.ps1 -Firmware -Tests" -ForegroundColor Yellow
    & '.\tools\Build-Local.ps1' -Firmware -Tests
    Write-Host "`nPS> .\tools\Install-Local.ps1 -Start" -ForegroundColor Yellow
    & '.\tools\Install-Local.ps1' -Start
    @{ status = 'installed'; timestamp = (Get-Date).ToString('o') } | ConvertTo-Json |
        Set-Content -LiteralPath '.tools\live-result.json' -Encoding utf8
    Write-Host "`nInstallazione completata. La app Maxmix resta aperta." -ForegroundColor Green
    Write-Host 'Log della app: %LOCALAPPDATA%\Maxmix\logs\info.log'
} catch {
    @{ status = 'failed'; error = $_.Exception.Message } | ConvertTo-Json |
        Set-Content -LiteralPath '.tools\live-result.json' -Encoding utf8
    Write-Host $_ -ForegroundColor Red
} finally { Stop-Transcript | Out-Null }
