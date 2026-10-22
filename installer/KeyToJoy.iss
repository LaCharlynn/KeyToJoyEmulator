; KeyToJoyEmulator - instalador Inno Setup
; Usa exatamente os executaveis da referencia aprovada:
;   completo\ (referencia local do design aprovado)
; O Configurador dessa referencia tem SHA-256
;   A0609F8E34CFF1CDC7C09BF49C6B679EC27DE725B0864E6133C60472436F0F59
; Instala por usuario, sem exigir administrador.
; O driver ViGEmBus NAO vai no pacote e precisa ser instalado separadamente.

#define MyAppName "KeyToJoyEmulator"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "KeyToJoyEmulator"
#define MyAppConfigurador "KeyToJoyEmulator-Configurador.exe"

[Setup]
AppId={{FFD86EEF-47AA-40A6-A6C8-C7139B841539}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\dist\installer
OutputBaseFilename=KeyToJoyEmulator-Setup-{#MyAppVersion}
SetupIconFile=..\my-version-design-novo\src\recursos\KeyToJoyEmulator.ico
UninstallDisplayIcon={app}\{#MyAppConfigurador}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=force
RestartApplications=no
VersionInfoVersion=1.0.0.0
VersionInfoDescription=Instalador do KeyToJoyEmulator
VersionInfoProductName=KeyToJoyEmulator
InfoBeforeFile=..\dist\portable\KeyToJoyEmulator\LEIA-ME.txt

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho do Configurador na Área de Trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked

[Files]
Source: "..\dist\portable\KeyToJoyEmulator\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "devices.json,smartthings_tokens.json,*.log,*.bak"

[Icons]
Name: "{group}\KeyToJoyEmulator - Configurador"; Filename: "{app}\{#MyAppConfigurador}"; WorkingDir: "{app}"
Name: "{group}\Desinstalar KeyToJoyEmulator"; Filename: "{uninstallexe}"
Name: "{autodesktop}\KeyToJoyEmulator - Configurador"; Filename: "{app}\{#MyAppConfigurador}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppConfigurador}"; Description: "Abrir o Configurador do KeyToJoyEmulator"; Flags: nowait postinstall skipifsilent
