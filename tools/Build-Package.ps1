param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot
try {
    if (-not $SkipBuild) { & '.\tools\Build-Local.ps1' -Firmware -Tests }
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $msbuild = & $vswhere -latest -products '*' -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    & $msbuild Desktop\OfflineTools\MaxmixTools.csproj /p:Configuration=Release "/p:TargetFrameworkRootPath=$repoRoot\.tools\reference-assemblies\net48\build\" /v:minimal /nologo
    if ($LASTEXITCODE -ne 0) { throw 'Compilazione strumenti offline non riuscita.' }
    $stage = Join-Path $repoRoot '.tools\package-1.5.9'
    if (Test-Path $stage) {
        $resolved = [IO.Path]::GetFullPath($stage)
        if (-not $resolved.StartsWith((Join-Path $repoRoot '.tools\'), [StringComparison]::OrdinalIgnoreCase)) { throw 'Cartella staging non valida.' }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
    New-Item -ItemType Directory -Force $stage | Out-Null
    foreach ($folder in @('Tools','Firmware','Driver','Avrdude','Sources')) { New-Item -ItemType Directory -Path (Join-Path $stage $folder) | Out-Null }
    Get-ChildItem Desktop\Application\MaxMix\bin\Release -File | Where-Object { $_.Extension -in '.exe','.dll','.config' } | Copy-Item -Destination $stage
    Copy-Item Desktop\OfflineTools\bin\Release\MaxmixTools.exe (Join-Path $stage 'Tools')
    Copy-Item Embedded\MaxMix\.pio\build\nano\firmware.hex (Join-Path $stage 'Firmware\Maxmix-1.5.9-full-step.hex')
    Copy-Item Embedded\MaxMix\.pio\build\nano-halfstep\firmware.hex (Join-Path $stage 'Firmware\Maxmix-1.5.9-half-step.hex')
    Copy-Item Desktop\Resources\bin\Driver\* (Join-Path $stage 'Driver')
    $signature = Get-AuthenticodeSignature (Join-Path $stage 'Driver\ch341SER.CAT')
    if ($signature.Status -ne 'Valid') { throw 'Firma catalogo driver non valida.' }
    Copy-Item Desktop\Resources\bin\avrdude\* (Join-Path $stage 'Avrdude')
    Copy-Item LICENSE,LICENSES.md,NOTICE,AUTHORS $stage
    Copy-Item installer\LEGGIMI.txt $stage
    Copy-Item Desktop\Resources\icons\app.ico (Join-Path $stage 'Maxmix.ico')
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::Open((Join-Path $stage 'Sources\Maxmix-source-1.5.9.zip'), [IO.Compression.ZipArchiveMode]::Create)
    try {
        $files = @(git ls-files --cached --others --exclude-standard) | Sort-Object -Unique | Where-Object {
            $_ -match '^(Desktop/|Embedded/|Tests/|tools/|installer/|BuildSystem/|\.github/)' -or $_ -in @('README.md','README-LOCAL-BUILD.md','LICENSE','LICENSES.md','NOTICE','AUTHORS','.gitignore')
        }
        foreach ($file in $files) { if (Test-Path -LiteralPath $file -PathType Leaf) { [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $repoRoot $file), $file, [IO.Compression.CompressionLevel]::Optimal) | Out-Null } }
    } finally { $zip.Dispose() }
    $manifest = foreach ($file in Get-ChildItem $stage -Recurse -File) {
        @{ path = $file.FullName.Substring($stage.Length + 1); sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash }
    }
    @{ version = '1.5.9'; files = @($manifest) } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $stage 'bundle.json') -Encoding utf8
    $selfTest = Join-Path $repoRoot '.tools\offline-self-test.txt'
    $process = Start-Process -FilePath (Join-Path $stage 'Tools\MaxmixTools.exe') -ArgumentList "--self-test `"$selfTest`"" -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0) { throw 'Verifica del pacchetto offline non superata.' }
    Get-Content $selfTest
    $compiler = Join-Path $repoRoot '.tools\innosetup\ISCC.exe'
    if (-not (Test-Path $compiler)) { throw 'Installa il compilatore Inno Setup 6.7.4 in .tools\innosetup.' }
    New-Item -ItemType Directory -Force dist | Out-Null
    & $compiler "/DPayloadRoot=$stage" "/O$repoRoot\dist" installer\Maxmix.iss
    if ($LASTEXITCODE -ne 0) { throw 'Creazione installer non riuscita.' }
    Get-FileHash dist\Maxmix-1.5.9-Setup.exe -Algorithm SHA256 | Format-List Path,Hash
} finally { Pop-Location }
