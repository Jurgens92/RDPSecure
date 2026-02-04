#define AppName "RDPSecure"
#define AppVersion "1.0.0"
#define AppPublisher "Jurgens92"
#define AppExeName "RDPSecure.exe"
#define AppServiceName "RDPSecure"

; ---------------------------------------------------------------------------
; Setup directives
; ---------------------------------------------------------------------------
[Setup]
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
; Unique ID — keeps upgrades clean (same ID = replace previous install)
AppId={{B3A2C1D4-5E6F-7890-ABCD-EF1234567890}}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputBaseFileName={#AppName}-{#AppVersion}
OutputDir=.\Output
SetupIcon=.\publish\{#AppName}.ico
UninstallDisplayIcon={app}\{#AppName}.ico
; Only support 64-bit Windows (matches the win-x64 publish target)
ArchitecturesSupported=x64
ArchitecturesAllowed=x64

; ---------------------------------------------------------------------------
; Optional tasks shown on the install page
; ---------------------------------------------------------------------------
[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupOffset: 8; Flags: unchecked

; ---------------------------------------------------------------------------
; Directories
; ---------------------------------------------------------------------------
[Dirs]
; Data directory — SYSTEM and Admins get full control here;
; the service's EnsureDirectoryPermissions() adds user-level ACLs on first run.
Name: "{commonappdata}\{#AppName}"; Permissions: system:systemfull,admin:adminfull

; ---------------------------------------------------------------------------
; Files — sourced from the dotnet publish output produced by build.ps1
; ---------------------------------------------------------------------------
[Files]
Source: ".\publish\*"; DestDir: "{app}"; Flags: recursesubdirs createemptydirs

; ---------------------------------------------------------------------------
; Icons / shortcuts
; ---------------------------------------------------------------------------
[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppName}.ico"; Comment: "Monitor and protect RDP connections"
Name: "{userdesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppName}.ico"; Tasks: desktopicon

; ---------------------------------------------------------------------------
; Post-install — register and start the Windows service
; ---------------------------------------------------------------------------
[Run]
Filename: "sc.exe"; Parameters: "create {#AppServiceName} binPath= ""{app}\{#AppExeName}"" start= auto DisplayName= ""{#AppName}"""; Description: "Registering {#AppName} service..."
Filename: "sc.exe"; Parameters: "description {#AppServiceName} ""Monitors and protects RDP connections from brute force attacks"""; Description: "Configuring {#AppName} service..."
Filename: "sc.exe"; Parameters: "start {#AppServiceName}"; Description: "Starting {#AppName} service..."

; ---------------------------------------------------------------------------
; Pre-uninstall — stop and remove the service before files are deleted
; ---------------------------------------------------------------------------
[UninstallRun]
Filename: "sc.exe"; Parameters: "stop {#AppServiceName}"; Description: "Stopping {#AppName} service..."
Filename: "sc.exe"; Parameters: "delete {#AppServiceName}"; Description: "Removing {#AppName} service..."
