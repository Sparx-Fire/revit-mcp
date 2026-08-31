; mcp-servers-for-revit 安装脚本 - Inno Setup
; 编译：ISCC.exe plans\mcp-servers-for-revit-安装脚本.iss
; 前提：先运行 build.ps1（输出到 build\ 目录）
#define MyAppName "mcp-servers-for-revit"
#define MyAppPublisher "Revit MCP"
#define MyAppVersion "1.0.0"
; ───────────────── Revit 版本 ─────────────────
#if !defined(RevitVersionParam)
#define RevitVersionParam "R26"
#endif
#if !defined(RevitYear)
#define RevitYear "2026"
#endif
; ──────────────────────────────────────────────
[Setup]
AppId={{D3E7F21A-5B98-4C6E-9A0F-1C8B4E2F7D3A}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
; 安装到 %APPDATA%\Autodesk\Revit\Addins\{year}
DefaultDirName={userappdata}\Autodesk\Revit\Addins\{#RevitYear}
DisableDirPage=yes
PrivilegesRequired=lowest
OutputDir=..\dist
OutputBaseFilename={#MyAppName}_{#RevitVersionParam}_{#RevitYear}_Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

; ========== RestartManager 修复 ==========
; CloseApplications=no   → 完全不调用 RestartManager API（不执行 RmGetList），
;                          从根源避免中文 ISL 里 '%s' 格式化崩溃
; RestartApplications=no → 安装完成后不自动重启被关闭的程序
CloseApplications=no
RestartApplications=no

[Languages]
; 暂不启用中文：chinesesimp + compiler:Default.isl 会加载损坏的中文消息表
; 如需中文向导，放入官方适配 6.2.0 的 ChineseSimplified.isl 后取消注释：
;Name: "chinesesimp"; MessagesFile: "ChineseSimplified.isl"

[Files]
; 单入口：整个 build\ 目录逐层复制到安装目录
;   build\mcp-servers-for-revit.addin → {app}\
;   build\revit_mcp_plugin\*          → {app}\revit_mcp_plugin\
Source: "..\build\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Dirs]
Name:"{app}"; Permissions: everyone-full
Name:"{app}\revit_mcp_plugin"; Permissions: everyone-full

[UninstallDelete]
Type: filesandordirs; Name: "{app}\revit_mcp_plugin"
Type: files; Name: "{app}\mcp-servers-for-revit.addin"

[Code]
// 安装初始化阶段静默结束 node.exe，释放 runtime\node.exe 文件锁
// 注意：会结束本机【所有】Node.js 进程（MCP 服务器即运行于 node.exe），
//       若有其他 Node 应用正在运行，请先自行保存
function InitializeSetup(): Boolean;
var
  ResCode: Integer;
begin
  Exec('taskkill', '/F /IM node.exe /T', '', SW_HIDE, ewWaitUntilTerminated, ResCode);
  Result := True;
end;
