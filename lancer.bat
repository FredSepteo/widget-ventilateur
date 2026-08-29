@echo off
setlocal
cd /d "%~dp0"

reg query "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO" >nul 2>&1
if errorlevel 1 (
    reg query "HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO" >nul 2>&1
    if errorlevel 1 (
        echo PawnIO absent — installation automatique...
        call "%~dp0install-pawnio.bat"
    )
)

echo Compilation du widget...
dotnet build FanWidget.sln -c Release
if errorlevel 1 (
    echo Echec de la compilation.
    pause
    exit /b 1
)

set "EXE=FanWidget\bin\Release\net9.0-windows\FanWidget.exe"

if not exist "%EXE%" (
    echo Executable introuvable : %EXE%
    pause
    exit /b 1
)

echo Fermeture de l'ancienne instance si presente...
taskkill /F /IM FanWidget.exe >nul 2>&1

echo Mise a jour de la tache planifiee de demarrage...
schtasks /Create /TN "FanWidget" /TR "%CD%\%EXE%" /SC ONLOGON /RL HIGHEST /F >nul 2>&1

echo Lancement de FanWidget (droits administrateur requis)...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%CD%\%EXE%' -Verb RunAs"

endlocal
