; Inno Setup script for DesignSpoolerClientSimulator.
; Build with: iscc /DMyAppVersion="1.0.0" /DArch="x64" /DSourceDir="C:\path\to\publish" DesignSpoolerClientSimulator.iss
;
; MyAppVersion, Arch and SourceDir are expected to be passed in from the CI
; build (see .github/workflows/dotnet.yml); the fallbacks below only exist so
; the script can also be compiled locally without extra arguments.

#define MyAppName "DesignSpoolerClientSimulator"
#define MyAppExeName "DesignSpoolerClientSimulator.exe"
#define MyAppPublisher "DHCD"

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0-local"
#endif
#ifndef Arch
  #define Arch "x64"
#endif
#ifndef SourceDir
  #define SourceDir "..\bin\Release\net10.0\win-" + Arch + "\publish"
#endif

; Inno Setup 6.3+ architecture identifiers.
#if Arch == "arm64"
  #define InnoArch "arm64"
#else
  #define InnoArch "x64compatible"
#endif

[Setup]
AppId={{6E3F2C6F-6E8B-4A6D-9E4B-6C2C6A0D2E7A}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed={#InnoArch}
ArchitecturesInstallIn64BitMode={#InnoArch}
OutputDir=Output
OutputBaseFilename={#MyAppName}-{#Arch}-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Files]
Source: "{#SourceDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
