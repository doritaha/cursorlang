# Installs CursorLang for the current user: builds it locally, enables autostart and launches it.
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$base = 'https://raw.githubusercontent.com/doritaha/cursorlang/main'
$dir = Join-Path $env:LOCALAPPDATA 'CursorLang'
$src = Join-Path $dir 'CursorLang.cs'
$exe = Join-Path $dir 'CursorLang.exe'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'

New-Item -ItemType Directory -Force $dir | Out-Null
Get-Process CursorLang -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500
Invoke-WebRequest -UseBasicParsing "$base/windows/CursorLang.cs" -OutFile $src
& $csc /nologo /codepage:65001 /target:winexe /optimize+ "/out:$exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll $src
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'CursorLang' -Value "`"$exe`""
Start-Process $exe
Write-Host 'CursorLang installed and running.'
