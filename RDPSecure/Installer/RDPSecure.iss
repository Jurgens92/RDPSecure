; RDPSecure Installer Script
#define MyAppName "RDP Secure"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "RDP Secure"
#define MyAppURL "https://github.com/jurgens92/RDPSecure"
#define MyAppExeName "RDPSecure.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
AppId={{YOUR-UNIQUE-GUID-HERE}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=Installer
OutputBaseFilename=RDPSecure_Setup
SetupIconFile=RDPSecure.ico
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startupicon"; Description: "Start with Windows"; GroupDescription: "Windows Startup"

[Files]
; Main application files
Source: "bin\Release\net8.0-windows\RDPSecure.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows\rdpsecure.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\RDPSecure.ico"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\RDPSecure.ico"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\RDPSecure.ico"; Tasks: startupicon

[Run]
; Install and start the Windows service
Filename: "{sys}\sc.exe"; Parameters: "create RDPSecure binPath= ""{app}\{#MyAppExeName}"" start= auto displayname= ""RDP Secure Service"""; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "description RDPSecure ""Monitors and protects RDP connections from brute force attacks"""; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "start RDPSecure"; Flags: runhidden
; Run application after installation
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Stop and remove the Windows service
Filename: "{sys}\sc.exe"; Parameters: "stop RDPSecure"; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "delete RDPSecure"; Flags: runhidden

[Code]
