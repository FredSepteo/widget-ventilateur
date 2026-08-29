# Post-redemarrage : installer pilote CUDA 546.12 (RTX 4060 + P100)
$ErrorActionPreference = 'Continue'
$log = 'C:\NVIDIA\post-reboot-install.log'
New-Item -ItemType Directory -Force -Path 'C:\NVIDIA' | Out-Null
function Log($m) { "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') $m" | Tee-Object -FilePath $log -Append }

Log '=== Installation post-reboot ==='
Start-Sleep -Seconds 15

$nvi2 = 'C:\Program Files\NVIDIA Corporation\Installer2\InstallerCore\NVI2.DLL'
if (Test-Path $nvi2) {
    foreach ($pkg in @('Display.NvApp','Canvas','FrameViewSdk','Display.NView','Display.PhysX','Display.Driver','HDAudio.Driver','CUDAToolkit_12.9','CUDAToolkit_12.3')) {
        Log "Desinstalle $pkg"
        Start-Process 'C:\WINDOWS\SysWOW64\RunDll32.EXE' -ArgumentList "`"$nvi2`",UninstallPackage $pkg" -Wait -NoNewWindow -ErrorAction SilentlyContinue
    }
}

$cuda = "$env:TEMP\cuda_12.3.2_546.12_windows.exe"
if (-not (Test-Path $cuda)) {
    Log 'Telechargement CUDA 546.12...'
    Invoke-WebRequest -Uri 'https://developer.download.nvidia.com/compute/cuda/12.3.2/local_installers/cuda_12.3.2_546.12_windows.exe' -OutFile $cuda -UseBasicParsing
}

Log 'Install CUDA 546.12 (pilote inclus, toolkit desactive)...'
$args = '-s','compiler_12.3=0','cuda_12.3=0','documentation_12.3=0','cudadev_12.3=0','cudadevrt_12.3=0','nv_profiler_12.3=0','visual_studio_integration_12.3=0','nsight_nvtx_12.3=0','nsight_systems_12.3=0','nsight_compute_12.3=0','Display.NView=0','Display.NvApp=0','Display.PhysX=0','FrameViewSdk=0','CUDADemo_12.3=0'
$p = Start-Process -FilePath $cuda -ArgumentList $args -Wait -PassThru
Log "Code sortie: $($p.ExitCode)"

$p100 = 'PCI\VEN_10DE&DEV_15F8&SUBSYS_118F10DE&REV_A1\4&184F91E2&0&00E4'
$rtx  = 'PCI\VEN_10DE&DEV_2882&SUBSYS_172619DA&REV_A1\4&F3D7BCB&0&0008'
Enable-PnpDevice -InstanceId $p100 -Confirm:$false -ErrorAction SilentlyContinue
Enable-PnpDevice -InstanceId $rtx -Confirm:$false -ErrorAction SilentlyContinue

Get-PnpDevice -Class Display | Where-Object FriendlyName -match 'NVIDIA' | ForEach-Object { Log "$($_.FriendlyName) -> $($_.Status)" }
nvidia-smi 2>&1 | ForEach-Object { Log $_ }

Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce' -Name 'FanWidgetCudaInstall' -ErrorAction SilentlyContinue
Log '=== Termine ==='
