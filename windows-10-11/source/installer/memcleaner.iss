#define AppName "memcleaner"
#define AppExe "MemCleaner.exe"
#define AppVersion GetFileVersion("..\publish\MemCleaner.exe")

[Setup]
AppId={{7E1B2C4D-5A6F-4C3B-9E2A-1D4F8A0B6C31}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=lordofsunshine
AppCopyright=MIT License, lordofsunshine
VersionInfoVersion=1.0.0.0
VersionInfoCompany=lordofsunshine
VersionInfoDescription=memcleaner setup
AppPublisherURL=https://github.com/lordofsunshine
DefaultDirName={autopf}\memcleaner
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=..\dist
OutputBaseFilename=MemCleaner-Setup-{#AppVersion}
SetupIconFile=..\src\MemCleaner\Assets\app.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные ярлыки:"; Flags: unchecked

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\Удалить {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Запустить {#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  TaskName: String;
  ResultCode: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    TaskName := 'memcleaner-' + Lowercase(GetUserNameString);
    Exec(ExpandConstant('{sys}\schtasks.exe'), '/Delete /TN "' + TaskName + '" /F', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;
