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

set "EXE_V17=FanWidget\bin\Release\net9.0-windows-v17\FanWidget.exe"
set "EXE_V16=FanWidget\bin\Release\net9.0-windows-v16\FanWidget.exe"
set "EXE_V15=FanWidget\bin\Release\net9.0-windows-v15\FanWidget.exe"
set "EXE_V14=FanWidget\bin\Release\net9.0-windows-v14\FanWidget.exe"
set "EXE_V13=FanWidget\bin\Release\net9.0-windows-v13\FanWidget.exe"
set "EXE_V12=FanWidget\bin\Release\net9.0-windows-v12\FanWidget.exe"
set "EXE_V11=FanWidget\bin\Release\net9.0-windows-v11\FanWidget.exe"
set "EXE_V10=FanWidget\bin\Release\net9.0-windows-v10\FanWidget.exe"
set "EXE_V9=FanWidget\bin\Release\net9.0-windows-v9\FanWidget.exe"
set "EXE_V8=FanWidget\bin\Release\net9.0-windows-v8\FanWidget.exe"
set "EXE_V7=FanWidget\bin\Release\net9.0-windows-v7\FanWidget.exe"
set "EXE_V6=FanWidget\bin\Release\net9.0-windows-v6\FanWidget.exe"
set "EXE_V5=FanWidget\bin\Release\net9.0-windows-v5\FanWidget.exe"
set "EXE_V4=FanWidget\bin\Release\net9.0-windows-v4\FanWidget.exe"
set "EXE_V3=FanWidget\bin\Release\net9.0-windows-v3\FanWidget.exe"
set "EXE_NEW=FanWidget\bin\Release\net9.0-windows-new\FanWidget.exe"
set "EXE=FanWidget\bin\Release\net9.0-windows\FanWidget.exe"

if exist "%EXE_V17%" set "EXE=%EXE_V17%"
if exist "%EXE_V16%" if not exist "%EXE_V17%" set "EXE=%EXE_V16%"
if exist "%EXE_V15%" if not exist "%EXE_V17%" if not exist "%EXE_V16%" set "EXE=%EXE_V15%"
if exist "%EXE_V14%" if not exist "%EXE_V17%" if not exist "%EXE_V16%" if not exist "%EXE_V15%" set "EXE=%EXE_V14%"
if exist "%EXE_V13%" if not exist "%EXE_V17%" if not exist "%EXE_V16%" if not exist "%EXE_V15%" if not exist "%EXE_V14%" set "EXE=%EXE_V13%"
if exist "%EXE_V12%" if not exist "%EXE_V17%" if not exist "%EXE_V16%" if not exist "%EXE_V15%" if not exist "%EXE_V14%" if not exist "%EXE_V13%" set "EXE=%EXE_V12%"
if exist "%EXE_V11%" if not exist "%EXE_V17%" if not exist "%EXE_V16%" if not exist "%EXE_V15%" if not exist "%EXE_V14%" if not exist "%EXE_V13%" if not exist "%EXE_V12%" set "EXE=%EXE_V11%"
if exist "%EXE_V10%" if not exist "%EXE_V17%" if not exist "%EXE_V16%" if not exist "%EXE_V15%" if not exist "%EXE_V14%" if not exist "%EXE_V13%" if not exist "%EXE_V12%" if not exist "%EXE_V11%" set "EXE=%EXE_V10%"
if exist "%EXE_V9%" if not exist "%EXE_V17%" if not exist "%EXE_V16%" if not exist "%EXE_V15%" if not exist "%EXE_V14%" if not exist "%EXE_V13%" if not exist "%EXE_V12%" if not exist "%EXE_V11%" if not exist "%EXE_V10%" set "EXE=%EXE_V9%"
if exist "%EXE_V8%" if not exist "%EXE_V10%" if not exist "%EXE_V9%" set "EXE=%EXE_V8%"
if exist "%EXE_V7%" if not exist "%EXE_V10%" if not exist "%EXE_V9%" if not exist "%EXE_V8%" set "EXE=%EXE_V7%"
if exist "%EXE_V6%" if not exist "%EXE_V10%" if not exist "%EXE_V9%" if not exist "%EXE_V8%" if not exist "%EXE_V7%" set "EXE=%EXE_V6%"
if exist "%EXE_V5%" if not exist "%EXE_V10%" if not exist "%EXE_V9%" if not exist "%EXE_V8%" if not exist "%EXE_V7%" if not exist "%EXE_V6%" set "EXE=%EXE_V5%"
if exist "%EXE_V4%" if not exist "%EXE_V10%" if not exist "%EXE_V9%" if not exist "%EXE_V8%" if not exist "%EXE_V7%" if not exist "%EXE_V6%" if not exist "%EXE_V5%" set "EXE=%EXE_V4%"
if exist "%EXE_V3%" if not exist "%EXE_V10%" if not exist "%EXE_V9%" if not exist "%EXE_V8%" if not exist "%EXE_V7%" if not exist "%EXE_V6%" if not exist "%EXE_V5%" if not exist "%EXE_V4%" set "EXE=%EXE_V3%"
if exist "%EXE_NEW%" if not exist "%EXE_V10%" if not exist "%EXE_V9%" if not exist "%EXE_V8%" if not exist "%EXE_V7%" if not exist "%EXE_V6%" if not exist "%EXE_V5%" if not exist "%EXE_V4%" if not exist "%EXE_V3%" set "EXE=%EXE_NEW%"

if not exist "%EXE%" (
    echo Compilation du widget...
    dotnet build FanWidget.sln -c Release
    if errorlevel 1 (
        echo Echec de la compilation.
        pause
        exit /b 1
    )
)

echo Fermeture de l'ancienne instance si presente...
taskkill /F /IM FanWidget.exe >nul 2>&1

echo Lancement de FanWidget (droits administrateur requis)...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%CD%\%EXE%' -Verb RunAs"

endlocal
