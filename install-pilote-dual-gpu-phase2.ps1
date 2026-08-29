# Phase 2 : desinstallation complete + pilote CUDA 546.12 uniquement
$ErrorActionPreference = 'Continue'
$log = "$env:TEMP\nvidia-cleanup-install.log"
function Log($msg) { $line = "$(Get-Date -Format 'HH:mm:ss') [P2] $msg"; $line | Out-File $log -Append; Write-Host $line }

Log '=== Phase 2 debut ==='

Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.Name -match 'nvidia|nvcontainer|nvapp|NVDisplay|setup|cuda' } | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

$nvi2 = 'C:\Program Files\NVIDIA Corporation\Installer2\InstallerCore\NVI2.DLL'
function Uninstall-NvPackage($pkg) {
    if (-not (Test-Path $nvi2)) { return }
    Log "Desinstalle $pkg"
    Start-Process -FilePath 'C:\WINDOWS\SysWOW64\RunDll32.EXE' `
        -ArgumentList "`"$nvi2`",UninstallPackage $pkg" -Wait -NoNewWindow -ErrorAction SilentlyContinue
}

$allPackages = @(
    'Display.NvApp', 'Canvas', 'FrameViewSdk', 'Display.NView', 'Display.PhysX',
    'CUDAToolkit_12.9', 'CUDAToolkit_12.3',
    'CUDADevelopment_11.8', 'CUDADocument_11.8', 'nsight_nvtx_11.8',
    'CUDARuntimes_11.8', 'visual_studio_integration_11.8',
    'Display.Driver', 'HDAudio.Driver'
)
foreach ($p in $allPackages) { Uninstall-NvPackage $p }

$cudaExe = "$env:TEMP\cuda_12.3.2_546.12_windows.exe"
if (-not (Test-Path $cudaExe)) {
    Log 'CUDA absent, telechargement...'
    Invoke-WebRequest -Uri 'https://developer.download.nvidia.com/compute/cuda/12.3.2/local_installers/cuda_12.3.2_546.12_windows.exe' -OutFile $cudaExe -UseBasicParsing
}

Log 'Installation pilote seul (546.12)...'
$installArgs = @(
    '-s',
    'compiler_12.3=0',
    'cuda_12.3=0',
    'documentation_12.3=0',
    'cudadev_12.3=0',
    'cudadevrt_12.3=0',
    'nv_profiler_12.3=0',
    'visual_studio_integration_12.3=0',
    'nsight_nvtx_12.3=0',
    'nsight_systems_12.3=0',
    'nsight_compute_12.3=0',
    'Display.NView=0',
    'Display.NvApp=0',
    'Display.PhysX=0',
    'FrameViewSdk=0',
    'CUDADemo_12.3=0'
)
$p = Start-Process -FilePath $cudaExe -ArgumentList $installArgs -Wait -PassThru
Log "CUDA exit: $($p.ExitCode)"

Start-Sleep -Seconds 5
Log 'Verification:'
Get-PnpDevice -Class Display | Where-Object FriendlyName -match 'NVIDIA' | ForEach-Object { Log "$($_.FriendlyName) -> $($_.Status)" }
nvidia-smi 2>&1 | ForEach-Object { Log $_ }

Log '=== Phase 2 fin - REDEMARRER ==='
