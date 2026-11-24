#ifndef ProductVersion
  #error ProductVersion fehlt
#endif
#ifndef SourceDirectory
  #error SourceDirectory fehlt
#endif
#ifndef OutputDirectory
  #error OutputDirectory fehlt
#endif
[Setup]
AppId={{679C2D50-1B23-45F4-A9B8-E3DB12F63689}
AppName=PlanetOne
AppVersion={#ProductVersion}
AppPublisher=PlanetOne
DefaultDirName={localappdata}\Programs\PlanetOne
DefaultGroupName=PlanetOne
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDirectory}
OutputBaseFilename=PlanetOne-{#ProductVersion}-Windows-x64-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\PlanetOne.exe
CreateUninstallRegKey=not IsTestInstall
CloseApplications=yes
[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
[Files]
Source: "{#SourceDirectory}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\PlanetOne"; Filename: "{app}\PlanetOne.exe"; Check: not IsTestInstall
[Run]
Filename: "{app}\PlanetOne.exe"; Description: "PlanetOne starten"; Flags: nowait postinstall skipifsilent
[Code]
function IsTestInstall: Boolean;
begin
  Result := ExpandConstant('{param:TESTINSTALL|0}') = '1';
end;
