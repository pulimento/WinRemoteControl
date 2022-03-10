; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

;Remember that, when we're using /Dxx parameter to run the compiler, it emulates "#define", not variables!
;All of this mess of #ifndef are because the compiler parameters defines it, but if it's already defined, it does not work

#ifndef MyAppName
#define MyAppName "WinRemoteControl"
#endif
#ifndef MyAppVersion
#define MyAppVersion "1.0.0"
#endif
#ifndef MyAppPublisher
#define MyAppPublisher "pulimento"
#endif
#ifndef MyAppURL
#define MyAppURL "https://github.com/pulimento"
#endif
#ifndef MyAppExeName
#define MyAppExeName "WinRemoteControl.exe"
#endif
#ifndef MyOutputDir
#define MyOutputDir "WinRemoteControl\Installer\Output"
#endif
#ifndef MyOutputBaseFileName
#define MyOutputBaseFileName "WinRemoteControl_Setup"
#endif
#ifndef MySourceDir
#define MySourceDir "WinRemoteControl\WinRemoteControl\bin\Release\net5.0-windows\win-x64\publish"
#endif
#ifndef MySetupIconFile
#define MySetupIconFile "WinRemoteControl\WinRemoteControl\Resources\big_icon_5jt_icon.ico"
#endif

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{FAD7BDDC-E0BA-403A-AB66-0E2F08F4B4EA}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={userappdata}\{#MyAppName}
DisableProgramGroupPage=yes
; Remove the following line to run in administrative install mode (install for all users.)
PrivilegesRequired=lowest
OutputDir={#MyOutputDir}
OutputBaseFilename={#MyOutputBaseFilename}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
SourceDir={#MySourceDir}
SetupIconFile={#MySetupIconFile}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}";
;was in the previous line - Flags: unchecked

[Files]
Source: "{#MySourceDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MySourceDir}\*"; Excludes: "settings.json"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

