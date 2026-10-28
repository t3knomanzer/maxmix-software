param([switch]$Firmware, [switch]$Tests, [switch]$Preview)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot
try {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $msbuild = & $vswhere -latest -products '*' -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    if (-not $msbuild) { throw 'MSBuild non trovato. Installa Visual Studio Build Tools con gli strumenti desktop .NET.' }
    $refRoot = Join-Path $repoRoot '.tools\reference-assemblies\net48\build'
    if (-not (Test-Path (Join-Path $refRoot '.NETFramework\v4.8\mscorlib.dll'))) {
        New-Item -ItemType Directory -Force '.tools\reference-assemblies' | Out-Null
        Invoke-WebRequest 'https://api.nuget.org/v3-flatcontainer/microsoft.netframework.referenceassemblies.net48/1.0.3/microsoft.netframework.referenceassemblies.net48.1.0.3.nupkg' -OutFile '.tools\reference-assemblies\net48.zip'
        Expand-Archive '.tools\reference-assemblies\net48.zip' '.tools\reference-assemblies\net48' -Force
    }
    & $msbuild 'Desktop\Application\MaxMix.sln' /restore /p:RestorePackagesConfig=true /p:Configuration=Release "/p:TargetFrameworkRootPath=$refRoot\" /v:minimal /nologo
    if ($LASTEXITCODE -ne 0) { throw 'Compilazione desktop non riuscita.' }
    if ($Tests -or $Preview) {
        & $msbuild 'Tests\ConnectionRegression\ConnectionRegression.csproj' /p:Configuration=Release "/p:TargetFrameworkRootPath=$refRoot\" /v:minimal /nologo
        if ($LASTEXITCODE -ne 0) { throw 'Compilazione test non riuscita.' }
        if ($Tests) {
            & '.\bin\ConnectionRegression\ConnectionRegression.exe'
            if ($LASTEXITCODE -ne 0) { throw 'Test desktop non superati.' }
        }
        if ($Preview) {
            & '.\bin\ConnectionRegression\ConnectionRegression.exe' --preview
            if ($LASTEXITCODE -ne 0) { throw 'Verifica grafica non superata.' }
        }
    }
    if ($Firmware) {
        $pio = Join-Path $repoRoot '.tools\platformio\Scripts\platformio.exe'
        if (-not (Test-Path $pio)) {
            python -m venv '.tools\platformio'
            if ($LASTEXITCODE -ne 0) { throw 'Creazione ambiente Python non riuscita.' }
            & '.tools\platformio\Scripts\python.exe' -m pip install 'platformio==6.2.0'
            if ($LASTEXITCODE -ne 0) { throw 'Installazione PlatformIO non riuscita.' }
        }
        & $pio run --project-dir 'Embedded\MaxMix' -e nano -e nano-halfstep
        if ($LASTEXITCODE -ne 0) { throw 'Compilazione firmware non riuscita.' }
        if ($Tests) {
            $compiler = Join-Path $env:USERPROFILE '.platformio\packages\toolchain-gccmingw32\bin\g++.exe'
            if (-not (Test-Path $compiler)) {
                & $pio pkg install --global --tool platformio/toolchain-gccmingw32
                if ($LASTEXITCODE -ne 0) { throw 'Installazione compilatore test non riuscita.' }
            }
            New-Item -ItemType Directory -Force 'bin\FirmwareCommunication' | Out-Null
            & $compiler -static -std=c++11 -DARDUINO_AVR_NANO -ITests/FirmwareCommunication Tests/FirmwareCommunication/main.cpp Embedded/MaxMix/Communications.cpp -o bin/FirmwareCommunication/test.exe
            if ($LASTEXITCODE -ne 0) { throw 'Compilazione test firmware non riuscita.' }
            & '.\bin\FirmwareCommunication\test.exe'
            if ($LASTEXITCODE -ne 0) { throw 'Test firmware non superati.' }
            & $compiler -static -std=c++11 -DARDUINO_AVR_NANO -ITests/FirmwareCommunication Tests/FirmwareCommunication/lighting.cpp -o bin/FirmwareCommunication/lighting.exe
            if ($LASTEXITCODE -ne 0) { throw 'Compilazione test LED non riuscita.' }
            & '.\bin\FirmwareCommunication\lighting.exe'
            if ($LASTEXITCODE -ne 0) { throw 'Test LED non superati.' }
        }
    }
} finally { Pop-Location }
