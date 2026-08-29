@echo off
:: Lance le nettoyage NVIDIA + pilote dual GPU en administrateur
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process powershell -Verb RunAs -Wait -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File \"%~dp0install-pilote-dual-gpu.ps1\"'"
echo.
echo Termine. Consultez %TEMP%\nvidia-cleanup-install.log
echo Un redemarrage est recommande.
pause
