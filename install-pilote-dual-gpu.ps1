# Nettoyage NVIDIA (apps + pilote) puis installation pilote CUDA 546.12 (RTX 4060 + P100)
$ErrorActionPreference = 'Continue'
$log = "$env:TEMP\nvidia-cleanup-install.log"
function Log($msg) { $line = "$(Get-Date -Format 'HH:mm:ss') $msg"; $line | Out-File $log -Append; Write-Host $line }

Log '=== Debut ==='

# Arreter processus NVIDIA
Get-Process -ErrorAction SilentlyContinue | Where-Object {
    $_.Name -match 'nvidia|nvcontainer|nvapp|nvsphelper|nvdisplay|nvtray|NVIDIA'
} | ForEach-Object { Log "Stop $($_.Name)"; Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue }

Start-Sleep -Seconds 2

$nvi2 = 'C:\Program Files\NVIDIA Corporation\Installer2\InstallerCore\NVI2.DLL'
function Uninstall-NvPackage($pkg) {
    if (-not (Test-Path $nvi2)) { Log "NVI2 absent"; return }
    Log "Desinstalle $pkg"
    Start-Process -FilePath 'C:\WINDOWS\SysWOW64\RunDll32.EXE' `
        -ArgumentList "`"$nvi2`",UninstallPackage $pkg" -Wait -NoNewWindow
}

# Applications et composants (pas le pilote graphique pour l'instant)
$packages = @(
    'Display.NvApp', 'Canvas', 'FrameViewSdk', 'Display.NView', 'Display.PhysX',
    'CUDAToolkit_12.9', 'CUDAToolkit_12.3',
    'CUDADevelopment_11.8', 'CUDADocument_11.8', 'nsight_nvtx_11.8',
    'CUDARuntimes_11.8', 'visual_studio_integration_11.8'
)
foreach ($p in $packages) { Uninstall-NvPackage $p }

# Nsight (MSI)
$guids = @(
    '{C40134D1-C78B-4879-B3BC-A14F208A8B46}',
    '{000420DE-0A08-46D7-A941-E6120CB6D9CA}',
    '{9793590F-9325-4B93-A76F-6E8DC72C3B62}',
    '{99EEABF5-7586-4562-B2D1-008620242D09}',
    '{396E48B8-41BD-4CF4-A7E0-39EA7608A9C6}',
    '{B56D2F88-8865-40FD-B7AC-F074EE4D201D}'
)
foreach ($g in $guids) {
    Log "msiexec /X $g"
    Start-Process msiexec.exe -ArgumentList "/X$g /qn /norestart" -Wait -NoNewWindow
}

# Desinstaller pilote graphique actuel (581.15 DC)
Uninstall-NvPackage 'Display.Driver'
Uninstall-NvPackage 'HDAudio.Driver'

Start-Sleep -Seconds 3

# Telecharger CUDA 12.3.2 (pilote 546.12) si absent
$cudaUrl = 'https://developer.download.nvidia.com/compute/cuda/12.3.2/local_installers/cuda_12.3.2_546.12_windows.exe'
$cudaExe = "$env:TEMP\cuda_12.3.2_546.12_windows.exe"
if (-not (Test-Path $cudaExe)) {
    Log "Telechargement CUDA 12.3.2..."
    Invoke-WebRequest -Uri $cudaUrl -OutFile $cudaExe -UseBasicParsing
}

# Installation : pilote d'affichage uniquement
Log "Installation pilote 546.12 (driver seul)..."
$args = @(
    '-s',
    'nv_driver=1',
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
$p = Start-Process -FilePath $cudaExe -ArgumentList $args -Wait -PassThru
Log "CUDA install exit: $($p.ExitCode)"

Log '=== Fin - redemarrage recommande ==='
