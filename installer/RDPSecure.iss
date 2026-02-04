#define AppName "RDPSecure"
#define AppVersion "1.0.0"
#define AppPublisher "Jurgens92"
#define AppExeName "RDPSecure.exe"
#define AppServiceName "RDPSecure"

; ---------------------------------------------------------------------------
; General settings
; ---------------------------------------------------------------------------
[Info]
Name={#AppName}
Version={#AppVersion}
Publisher={#AppPublisher}

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupOffset: 8; Flags: unchecked
Name: "startmenupin"; Description: "Pin to Start Menu"; GroupOffset: 8; Flags: unchecked

; ---------------------------------------------------------------------------
; Directories
; ---------------------------------------------------------------------------
[Dirs]
Name: "{autopf}\{#AppName}"; Permissions: admin:adminsonly
Name: "{commonappdata}\{#AppName}"; Permissions: system:systemfull,admin:adminfull,users:userreadexec

; ---------------------------------------------------------------------------
; Files — sourced from the dotnet publish output produced by build.ps1
; ---------------------------------------------------------------------------
[Files]
; Main application files
Source: "..\publish\*"; DestDir: "{autopf}\{#AppName}"; Flags: recursesubdirs createemptydirs

; ---------------------------------------------------------------------------
; Icons / shortcuts
; ---------------------------------------------------------------------------
[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{autopf}\{#AppName}\{#AppExeName}"; IconFilename: "{autopf}\{#AppName}\{#AppName}.ico"; Comment: "Monitor and protect RDP connections"
Name: "{userdesktop}\{#AppName}"; Filename: "{autopf}\{#AppName}\{#AppExeName}"; IconFilename: "{autopf}\{#AppName}\{#AppName}.ico"; Tasks: desktopicon
Name: "{autoprograms}\{#AppName}\Uninstall {#AppName}"; Filename: "{uninstallexe}"; Parameters: "/SILENT"; Comment: "Remove {#AppName}"

; ---------------------------------------------------------------------------
; Pascal script — handles service registration on install and cleanup on uninstall
; ---------------------------------------------------------------------------
[Code]

// -----------------------------------------------------------------------
// Install: register the Windows service after files are in place
// -----------------------------------------------------------------------
procedure AfterInstall;
var
  RetVal: Longint;
begin
  // Install the service (auto-start)
  RetVal := Shell('sc.exe',
    'create {#AppServiceName} binPath= "{autopf}\{#AppName}\{#AppExeName}" start= auto DisplayName= "{#AppName}"',
    '', SW_HIDE, True);

  // Set the service description
  Shell('sc.exe',
    'description {#AppServiceName} "Monitors and protects RDP connections from brute force attacks"',
    '', SW_HIDE, True);

  // Start the service
  Shell('sc.exe',
    'start {#AppServiceName}',
    '', SW_HIDE, True);
end;

// -----------------------------------------------------------------------
// Uninstall: stop and delete the service before files are removed
// -----------------------------------------------------------------------
function NeedReboot: Boolean;
begin
  Result := False;
end;

procedure CurUninstallStepOnChange(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostDelete then begin
    // Stop the service
    Shell('sc.exe',
      'stop {#AppServiceName}',
      '', SW_HIDE, True);
    // Delete the service
    Shell('sc.exe',
      'delete {#AppServiceName}',
      '', SW_HIDE, True);
  end;
end;
