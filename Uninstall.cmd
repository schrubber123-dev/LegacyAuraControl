@echo off
setlocal
set "TARGET=%LOCALAPPDATA%\Programs\LegacyAuraControl"
set "STARTMENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs"
set "DESKTOP=%USERPROFILE%\Desktop"
set "STARTUP=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup"

taskkill /IM LegacyAura.exe /F >nul 2>&1
del /Q "%STARTMENU%\Legacy Aura Control.lnk" >nul 2>&1
del /Q "%DESKTOP%\Legacy Aura Control.lnk" >nul 2>&1
del /Q "%STARTUP%\LegacyAuraControl.cmd" >nul 2>&1
rmdir /S /Q "%TARGET%" >nul 2>&1

echo Legacy Aura Control wurde deinstalliert.
pause
