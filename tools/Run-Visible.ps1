param([Parameter(Mandatory=$true)][string]$ScriptPath)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$logPath = Join-Path $repoRoot '.tools\commands-live.log'
"`r`n[$(Get-Date -Format 'HH:mm:ss')] COMANDI:" | Tee-Object -FilePath $logPath -Append
Get-Content -LiteralPath $ScriptPath | Tee-Object -FilePath $logPath -Append
"--- RISULTATI ---" | Tee-Object -FilePath $logPath -Append
& $ScriptPath *>&1 | Tee-Object -FilePath $logPath -Append
