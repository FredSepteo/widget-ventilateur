@echo off
setlocal
cd /d "%~dp0"

echo Installation de PawnIO (pilote requis pour le controle des ventilateurs)...
echo.

where winget >nul 2>&1
if errorlevel 1 (
    echo winget introuvable. Telechargez PawnIO manuellement :
    echo https://github.com/namazso/PawnIO.Setup/releases/latest
    pause
    exit /b 1
)

winget install --id namazso.PawnIO --exact --silent --accept-package-agreements --accept-source-agreements

if errorlevel 1 (
    echo.
    echo Echec ou PawnIO deja installe. Verifiez dans "Applications installees".
) else (
    echo.
    echo PawnIO installe avec succes.
)

echo.
echo Un redemarrage peut etre necessaire si c'est la premiere installation.
pause
endlocal
