@echo off
setlocal
cd /d "%~dp0"

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if not exist "%CSC%" (
  echo ERROR: .NET Framework C# compiler was not found.
  pause
  exit /b 1
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command ^
 "Add-Type -AssemblyName System.Drawing; " ^
 "$bmp=[System.Drawing.Bitmap]::FromFile('assets\LegacyAura.png'); " ^
 "$h=$bmp.GetHicon(); $ico=[System.Drawing.Icon]::FromHandle($h); " ^
 "$fs=[System.IO.File]::Open('assets\LegacyAura.build.ico',[System.IO.FileMode]::Create); " ^
 "$ico.Save($fs); $fs.Close(); $ico.Dispose(); $bmp.Dispose()"
if errorlevel 1 (
  echo ERROR: Could not generate the application icon.
  pause
  exit /b 1
)

"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ ^
 /win32icon:"assets\LegacyAura.build.ico" ^
 /out:"LegacyAura.exe" ^
 /reference:System.dll ^
 /reference:System.Core.dll ^
 /reference:System.Drawing.dll ^
 /reference:System.Windows.Forms.dll ^
 "src\LegacyAura.cs"

if errorlevel 1 (
  echo Build failed.
  pause
  exit /b 1
)

echo Build successful: LegacyAura.exe
