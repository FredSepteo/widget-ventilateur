# Relance installation CUDA 546.12 apres reboot
$ErrorActionPreference = 'Continue'
$log = 'C:\NVIDIA\install-relaunch.log'
New-Item -ItemType Directory -Force -Path 'C:\NVIDIA' | Out-Null
function Log($m) { "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') $m" | Tee-Object -FilePath $log -Append }

Log '=== Relance post-reboot ==='

Get-Process -ErrorAction SilentlyContinue | Where-Object {
    $_.Name -match 'nvidia|nvcontainer|nvapp|NVDisplay|cuda'
} | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

$p100 = 'PCI\VEN_10DE&DEV_15F8&SUBSYS_118F10DE&REV_A1\4&184F91E2&0&00E4'
$rtx  = 'PCI\VEN_10DE&DEV_2882&SUBSYS_172619DA&REV_A1\4&F3D7BCB&0&0008'
Enable-PnpDevice -InstanceId $p100 -Confirm:$false -ErrorAction SilentlyContinue
Enable-PnpDevice -InstanceId $rtx  -Confirm:$false -ErrorAction SilentlyContinue
Log 'GPU reactives'

Get-PnpDevice | Where-Object { $_.InstanceId -match '15F8|2882' } | ForEach-Object {
    Log "Avant install: $($_.FriendlyName) -> $($_.Status) $($_.Problem)"
}

$cuda = "$env:TEMP\cuda_12.3.2_546.12_windows.exe"
if (-not (Test-Path $cuda)) {
    Log 'Telechargement...'
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri 'https://developer.download.nvidia.com/compute/cuda/12.3.2/local_installers/cuda_12.3.2_546.12_windows.exe' -OutFile $cuda -UseBasicParsing
}

Log 'Installation silencieuse CUDA 546.12 (-s, pilote inclus)...'
$p = Start-Process -FilePath $cuda -ArgumentList '-s' -Wait -PassThru
Log "Code sortie: $($p.ExitCode)"

Start-Sleep -Seconds 8
Get-PnpDevice | Where-Object { $_.InstanceId -match '15F8|2882' } | ForEach-Object {
    Log "Apres install: $($_.FriendlyName) -> $($_.Status) $($_.Problem)"
}
nvidia-smi 2>&1 | ForEach-Object { Log $_ }

Log 'Redemarrage dans 20 secondes'
shutdown /r /t 20 /c 'Finalisation pilote NVIDIA CUDA 546.12'
