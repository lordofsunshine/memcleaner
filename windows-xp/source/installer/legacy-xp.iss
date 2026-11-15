#define AppName "memcleaner"
#define AppExe "MemCleaner.exe"
#define AppVersion GetFileVersion("..\legacy\MemCleanerLegacy\bin\Release\MemCleaner.exe")

[Setup]
AppId={{C4A1F0D2-8E37-4B62-9F1C-7A5B3D2E9C41}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=lordofsunshine
AppCopyright=MIT License, lordofsunshine
VersionInfoVersion=1.0.0.0
VersionInfoCompany=lordofsunshine
VersionInfoDescription=memcleaner setup
DefaultDirName={pf}\memcleaner
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=..\dist
OutputBaseFilename=MemCleaner-Legacy-XP-Setup-{#AppVersion}
SetupIconFile=..\legacy\MemCleanerLegacy\Assets\app.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
Compression=lzma2/max
SolidCompression=yes
PrivilegesRequired=admin
MinVersion=5.1.2600
OnlyBelowVersion=6.0

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные ярлыки:"; Flags: unchecked

[Files]
Source: "..\legacy\MemCleanerLegacy\bin\Release\MemCleaner.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\legacy\MemCleanerLegacy\bin\Release\MemCleaner.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\Удалить {#AppName}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Запустить {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\reg.exe"; Parameters: "delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Run"" /v memcleaner /f"; Flags: runhidden; RunOnceId: "RemoveRun"
