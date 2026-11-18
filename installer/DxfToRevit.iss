; DxfToRevit installer — Inno Setup 7 script.
;
; Installs the DxfToRevit add-in for Autodesk Revit:
;   <Addins>\DxfToRevit.addin             add-in manifest (Revit scans this folder)
;   <Addins>\DxfToRevit\*.dll             add-in assemblies
;   <Addins>\DxfToRevit\Resources\*.png   ribbon icons (light and dark theme)
;   <Addins>\DxfToRevit\Resources\Families  optional folder for a round column family (.rfa)
; where <Addins> is
;   %APPDATA%\Autodesk\Revit\Addins\<RevitVersion>       "Install for me only" (no administrator rights)
;   %ProgramData%\Autodesk\Revit\Addins\<RevitVersion>   "Install for all users" (administrator rights)
;
; Build: scripts/build-installer.ps1, or compile this script directly after building the add-in:
;   dotnet build DxfToRevit.Addin -c Release -p:DeployAddin=false
;   ISCC installer\DxfToRevit.iss
;
; Overridable defines (ISCC /D<name>=<value>):
;   SourceDir     folder with the add-in build output
;   RevitVersion  target Revit version (must match the RevitAPI the add-in was built against)

#ifndef RevitVersion
  #define RevitVersion "2025"
#endif

#ifndef SourceDir
  #define SourceDir AddBackslash(SourcePath) + "..\DxfToRevit.Addin\bin\Release\net8.0-windows"
#endif

#define AppName        "DxfToRevit"
#define AppPublisher   "SonerAyar"
#define AppDescription "DXF wall and column import for Autodesk Revit"
#define AddinDll       SourceDir + "\DxfToRevit.Addin.dll"

#if !FileExists(AddinDll)
  #error Add-in build output not found. Build it first: dotnet build DxfToRevit.Addin -c Release -p:DeployAddin=false
#endif

; Version comes from the assembly (Version in Directory.Build.props), e.g. 2.0.0.0 -> 2.0.0.
#define FullVersion GetVersionNumbersString(AddinDll)
#define AppVersion  Copy(FullVersion, 1, RPos(".", FullVersion) - 1)

; Folder layout below {autoappdata} (user or common application data, by install mode).
#define AddinsDir "Autodesk\Revit\Addins\" + RevitVersion

[Setup]
; AppId identifies the product for upgrades and uninstall; never change it between releases.
AppId={{68FDE657-F32F-474D-A211-CC80D72F74D8}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion} for Revit {#RevitVersion}
AppPublisher={#AppPublisher}
AppComments={#AppDescription}
VersionInfoVersion={#FullVersion}
VersionInfoDescription={#AppName} {#AppVersion} Setup
VersionInfoProductName={#AppName}

; Revit is 64-bit only: build a 64-bit installer (also implies x64compatible architectures and 64-bit install mode).
SetupArchitecture=x64
MinVersion=10.0

; Per-user install by default; the privileges dialog offers "for all users" (requires elevation).
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline

; The add-in location is fixed by Revit, so there is no directory or Start menu page.
DefaultDirName={autoappdata}\{#AddinsDir}\{#AppName}
DisableDirPage=yes
DisableProgramGroupPage=yes
UsePreviousAppDir=no
UninstallFilesDir={app}\Uninstall
UninstallDisplayName={#AppName} for Revit {#RevitVersion}
UninstallDisplayIcon={app}\Resources\DxfToRevit.ico

; Files in use (an upgrade while Revit is running) are handled by Restart Manager.
CloseApplications=yes
RestartApplications=no

WizardStyle=modern dynamic
SetupIconFile=..\DxfToRevit.Addin\Resources\DxfToRevit.ico
LicenseFile=..\LICENSE.txt
OutputDir=Output
OutputBaseFilename={#AppName}-{#AppVersion}-Revit{#RevitVersion}-Setup
Compression=lzma2/max
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
FinishedLabelNoIcons=Setup has finished installing [name].%n%nStart Autodesk Revit {#RevitVersion} and open the DxfToRevit tab to import a DXF drawing.

[CustomMessages]
RevitNotFound=Autodesk Revit {#RevitVersion} was not found on this computer (%1).%n%nThe add-in is only loaded by Revit {#RevitVersion}. Install it anyway?
DuplicateManifest=Another copy of the DxfToRevit add-in is registered here:%n%n%1%n%nRevit reports an error when the same add-in is registered twice (once for the current user and once for all users).%n%nRemove that registration? Its add-in files are left in place.
DuplicateManifestRemoveFailed=The other registration could not be removed:%n%n%1%n%nRemove it manually before starting Revit.

[InstallDelete]
; Ribbon icons from versions before the light/dark theme icons.
Type: files; Name: "{app}\Resources\About16.png"
Type: files; Name: "{app}\Resources\About32.png"
Type: files; Name: "{app}\Resources\DXF_Icon16.png"
Type: files; Name: "{app}\Resources\DXF_Icon32.png"
Type: files; Name: "{app}\Resources\Settings16.png"
Type: files; Name: "{app}\Resources\Settings32.png"

[Dirs]
; Optional user-provided round column family (see the Columns documentation); removed only when empty.
Name: "{app}\Resources\Families"

[Files]
; Add-in manifest: must sit directly in the Revit Addins folder.
Source: "{#SourceDir}\DxfToRevit.addin"; DestDir: "{autoappdata}\{#AddinsDir}"; Flags: ignoreversion

; Add-in assemblies and their dependency manifest.
Source: "{#SourceDir}\DxfToRevit.Addin.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\DxfToRevit.Addin.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\DxfToRevit.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\netDxf.netstandard.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\Microsoft.Extensions.DependencyInjection.Abstractions.dll"; DestDir: "{app}"; Flags: ignoreversion

; Ribbon icons and the application icon.
Source: "{#SourceDir}\Resources\*.png"; DestDir: "{app}\Resources"; Flags: ignoreversion
Source: "..\DxfToRevit.Addin\Resources\DxfToRevit.ico"; DestDir: "{app}\Resources"; Flags: ignoreversion

; License and third-party notices.
Source: "..\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\THIRD-PARTY-NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion

[Code]
const
  AddinsRelative = '{#AddinsDir}';
  ManifestName = 'DxfToRevit.addin';

{ Revit.exe of the target version in the default installation folder. }
function RevitExePath: String;
begin
  Result := ExpandConstant('{commonpf64}\Autodesk\Revit {#RevitVersion}\Revit.exe');
end;

{ Manifest registered for the other install scope (all users when installing for the current
  user, and vice versa). Revit refuses to load an add-in id that is registered twice. }
function OtherScopeManifestPath: String;
begin
  if IsAdminInstallMode then
    Result := ExpandConstant('{userappdata}\') + AddinsRelative + '\' + ManifestName
  else
    Result := ExpandConstant('{commonappdata}\') + AddinsRelative + '\' + ManifestName;
end;

{ Warns when Revit is not installed and offers to remove a duplicate registration. }
function InitializeSetup: Boolean;
var
  OtherManifest: String;
  Answer: Integer;
begin
  Result := True;

  if not FileExists(RevitExePath) then
  begin
    if SuppressibleMsgBox(FmtMessage(CustomMessage('RevitNotFound'), [RevitExePath]),
         mbConfirmation, MB_YESNO, IDYES) <> IDYES then
    begin
      Result := False;
      Exit;
    end;
  end;

  OtherManifest := OtherScopeManifestPath;
  if FileExists(OtherManifest) then
  begin
    Answer := SuppressibleMsgBox(FmtMessage(CustomMessage('DuplicateManifest'), [OtherManifest]),
      mbConfirmation, MB_YESNOCANCEL, IDNO);
    if Answer = IDCANCEL then
      Result := False
    else if (Answer = IDYES) and not DeleteFile(OtherManifest) then
      SuppressibleMsgBox(FmtMessage(CustomMessage('DuplicateManifestRemoveFailed'), [OtherManifest]),
        mbError, MB_OK, IDOK);
  end;
end;
