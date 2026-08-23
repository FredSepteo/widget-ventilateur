@echo off
setlocal
cd /d "%~dp0"

set "SCAN=FanWidget.Tools\bin\Release\net9.0-windows\FanWidget.Tools.exe"
set "OUT=hardware-scan.txt"

if not exist "%SCAN%" (
    echo Compilation de l'outil de scan...
    dotnet build FanWidget.Tools\FanWidget.Tools.csproj -c Release
    if errorlevel 1 (
        echo Echec compilation.
        pause
        exit /b 1
    )
)

echo Scan materiel (admin requis)...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%SCAN%' -ArgumentList '%CD%\%OUT%' -Verb RunAs -Wait"

if exist "%OUT%" (
    echo.
    echo Rapport genere : %OUT%
    type "%OUT%"
) else (
    echo Scan annule ou echoue. Acceptez l'elevation UAC.
)

pause
endlocal
