@echo off
setlocal
cd /d "%~dp0"

call "%~dp0Build.cmd"
if errorlevel 1 exit /b 1

set "TARGET=%LOCALAPPDATA%\Programs\LegacyAuraControl"
set "STARTMENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs"
set "DESKTOP=%USERPROFILE%\Desktop"

if not exist "%TARGET%" mkdir "%TARGET%"
copy /Y "LegacyAura.exe" "%TARGET%\LegacyAura.exe" >nul
copy /Y "assets\LegacyAura.ico" "%TARGET%\LegacyAura.ico" >nul
copy /Y "LICENSE" "%TARGET%\LICENSE.txt" >nul

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command ^
 "Add-Type -AssemblyName System.Drawing; " ^
 "$bmp=[System.Drawing.Bitmap]::FromFile(\'%TARGET%\\LegacyAura.png\'); " ^
 "$h=$bmp.GetHicon(); $ico=[System.Drawing.Icon]::FromHandle($h); " ^
 "$fs=[System.IO.File]::Open(\'%TARGET%\\LegacyAura.ico\',[System.IO.FileMode]::Create); $ico.Save($fs); $fs.Close(); $ico.Dispose(); $bmp.Dispose(); " ^
 "$w=New-Object -ComObject WScript.Shell; " ^
 "$s=$w.CreateShortcut([IO.Path]::Combine('%STARTMENU%','Legacy Aura Control.lnk')); " ^
 "$s.TargetPath='%TARGET%\LegacyAura.exe'; $s.WorkingDirectory='%TARGET%'; $s.IconLocation='%TARGET%\LegacyAura.ico'; $s.Save(); " ^
 "$d=$w.CreateShortcut([IO.Path]::Combine('%DESKTOP%','Legacy Aura Control.lnk')); " ^
 "$d.TargetPath='%TARGET%\LegacyAura.exe'; $d.WorkingDirectory='%TARGET%'; $d.IconLocation='%TARGET%\LegacyAura.ico'; $d.Save()"

start "" "%TARGET%\LegacyAura.exe"
