#ifndef PayloadRoot
  #error PayloadRoot must point to the prepared offline bundle
#endif
[Setup]
AppId={{75096741-0829-481F-A752-967B0D3900F1}
AppName=Maxmix
AppVersion=1.5.9
AppVerName=Maxmix 1.5.9 - software, firmware e driver
AppPublisher=Maxmix - build locale
AppPublisherURL=https://maxmixproject.com/
DefaultDirName={localappdata}\Programs\Maxmix
DefaultGroupName=Maxmix
PrivilegesRequired=lowest
MinVersion=10.0.19041
ArchitecturesAllowed=x86 x64os
WizardStyle=modern dark
WizardSizePercent=115
Compression=lzma2
SolidCompression=yes
OutputBaseFilename=Maxmix-1.5.9-Setup
SetupIconFile={#PayloadRoot}\Maxmix.ico
UninstallDisplayIcon={app}\MaxMix.exe
CloseApplications=yes
RestartApplications=no
SourceDir={#PayloadRoot}
InfoBeforeFile={#PayloadRoot}\LEGGIMI.txt
VersionInfoDescription=Maxmix 1.5.9 - installer offline completo

[Languages]
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[Types]
Name: "full"; Description: "Completa (software, firmware e driver USB)"
Name: "custom"; Description: "Personalizzata"; Flags: iscustom

[Components]
Name: "app"; Description: "Applicazione Maxmix e dipendenze"; Types: full custom; Flags: fixed
Name: "firmware"; Description: "Aggiornamento firmware offline per Nano ATmega328P"; Types: full custom; Flags: fixed
Name: "driver"; Description: "Installa il driver USB CH340/CH341 (richiede conferma amministratore)"; Types: full

[Tasks]
Name: "desktopicon"; Description: "Crea un collegamento sul Desktop"; GroupDescription: "Collegamenti:"

[Files]
Source: "*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Maxmix"; Filename: "{app}\MaxMix.exe"
Name: "{group}\Aggiorna firmware Maxmix"; Filename: "{app}\Tools\MaxmixTools.exe"
Name: "{group}\Installa driver USB Maxmix"; Filename: "{app}\Tools\MaxmixTools.exe"; Parameters: "--install-driver"
Name: "{group}\Guida Maxmix"; Filename: "{app}\LEGGIMI.txt"
Name: "{userdesktop}\Maxmix"; Filename: "{app}\MaxMix.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\MaxMix.exe"; Description: "Apri Maxmix"; Flags: postinstall nowait skipifsilent
Filename: "{app}\Tools\MaxmixTools.exe"; Description: "Apri lo strumento di aggiornamento firmware"; Flags: postinstall nowait skipifsilent unchecked

[Code]
function InitializeSetup(): Boolean;
var Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040);
  if not Result then
    MsgBox('Maxmix richiede .NET Framework 4.8, incluso nelle versioni supportate di Windows 10 e Windows 11.', mbError, MB_OK);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var ResultCode: Integer;
begin
  if (CurStep = ssPostInstall) and WizardIsComponentSelected('driver') then begin
    if not Exec(ExpandConstant('{app}\Tools\MaxmixTools.exe'), '--install-driver', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      ResultCode := -1;
    if ResultCode <> 0 then begin
      Log('Installazione driver non completata: ' + IntToStr(ResultCode));
      if not WizardSilent then
        MsgBox('Il software e installato, ma il driver USB non e stato installato. Usa il collegamento Installa driver USB Maxmix e conferma la richiesta amministratore.', mbError, MB_OK);
    end;
  end;
end;
