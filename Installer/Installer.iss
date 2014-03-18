#define MyAppName "todotxt.net"
#define MyAppPublisher "Hughesoft"
#define MyAppURL "http://www.todotxt.net"
#define MyAppExeName "todotxt.exe"
#define MyAppPath SourcePath + "ProgramFiles"
#define MyAppVer = GetFileVersion(MyAppPath + "\todotxt.exe")

[Setup]
AppId={{E3530941-304A-43B3-A7EF-EFAAE9E9EF5F}
AppName={#MyAppName}
AppVerName={#MyAppName} v{#MyAppVer}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\{#MyAppPublisher}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputBaseFilename=todotxt-setup-{#MyAppVer}
Compression=lzma
SolidCompression=yes

[Languages]
Name: english; MessagesFile: compiler:Default.isl

[Tasks]
Name: desktopicon; Description: {cm:CreateDesktopIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked

[Files]
Source: {#MyAppPath}\todotxt.exe; DestDir: {app}; Flags: ignoreversion
Source: {#MyAppPath}\*.dll; DestDir: {app}; Flags: ignoreversion
Source: {#MyAppPath}\todotxt.exe.config; DestDir: {app}; Flags: ignoreversion

[Icons]
Name: {group}\{#MyAppName}; Filename: {app}\{#MyAppExeName}
Name: {group}\{cm:UninstallProgram,{#MyAppName}}; Filename: {uninstallexe}
Name: {commondesktop}\{#MyAppName}; Filename: {app}\{#MyAppExeName}; Tasks: desktopicon

[Run]
Filename: {app}\{#MyAppExeName}; Description: {cm:LaunchProgram,{#MyAppName}}; Flags: nowait postinstall skipifsilent

[Code]
const


dotnetRedistURL = 'http://www.microsoft.com/en-us/download/details.aspx?id=17851';
dotnetRegKey = 'SOFTWARE\Microsoft\Net Framework Setup\NDP\v4.0';
version = '4.0';

function InitializeSetup(): Boolean;
var
    ErrorCode: Integer;
    NetFrameWorkInstalled : Boolean;
    InstallDotNetResponse : Boolean;
begin
	NetFrameWorkInstalled := RegKeyExists(HKLM,dotnetRegKey);
	if NetFrameWorkInstalled =true then
	   begin
		  Result := true;
	   end
	else
	   begin
		  InstallDotNetResponse := MsgBox('This setup requires version ' + version + ' of the .NET Framework. Please download and install the .NET Framework and run this setup again. Do you want to download the framework now?',mbConfirmation,MB_YESNO)= idYes;
		  if InstallDotNetResponse =false then
			begin
			  Result:=false;
			end
		  else
			begin
			  Result:=false;
			  ShellExec('open',dotnetRedistURL,'','',SW_SHOWNORMAL,ewNoWait,ErrorCode);
			end;
	   end;
	end;
