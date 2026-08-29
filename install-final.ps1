# Installation finale pilote CUDA 546.12 (RTX 4060 + P100) + redemarrage
$ErrorActionPreference = 'Continue'
$log = 'C:\NVIDIA\install-final.log'
New-Item -ItemType Directory -Force -Path 'C:\NVIDIA' | Out-Null
function Log($m) { "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') $m" | Tee-Object -FilePath $log -Append }

Log '=== Installation finale CUDA 546.12 ==='

Get-Process -ErrorAction SilentlyContinue | Where-Object {
    $_.Name -match 'nvidia|nvcontainer|nvapp|NVDisplay|setup|cuda'
} | ForEach-Object { Log "Stop $($_.Name)"; Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue }
Start-Sleep -Seconds 3

$p100 = 'PCI\VEN_10DE&DEV_15F8&SUBSYS_118F10DE&REV_A1\4&184F91E2&0&00E4'
$rtx  = 'PCI\VEN_10DE&DEV_2882&SUBSYS_172619DA&REV_A1\4&F3D7BCB&0&0008'
Enable-PnpDevice -InstanceId $p100 -Confirm:$false -ErrorAction SilentlyContinue
Enable-PnpDevice -InstanceId $rtx  -Confirm:$false -ErrorAction SilentlyContinue
Log 'Peripheriques GPU reactives'

$cuda = "$env:TEMP\cuda_12.3.2_546.12_windows.exe"
if (-not (Test-Path $cuda)) {
    Log 'Telechargement CUDA 546.12...'
    Invoke-WebRequest -Uri 'https://developer.download.nvidia.com/compute/cuda/12.3.2/local_installers/cuda_12.3.2_546.12_windows.exe' -OutFile $cuda -UseBasicParsing
}
Log "Installateur: $cuda ($([math]::Round((Get-Item $cuda).Length/1MB)) Mo)"

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
Log "Lancement: $installArgs"
$p = Start-Process -FilePath $cuda -ArgumentList $installArgs -Wait -PassThru
Log "Code sortie installateur: $($p.ExitCode)"

Start-Sleep -Seconds 5
Get-PnpDevice -Class Display -ErrorAction SilentlyContinue |
    Where-Object FriendlyName -match 'NVIDIA|vid' |
    ForEach-Object { Log "GPU: $($_.FriendlyName) -> $($_.Status) ($($_.Problem))" }

nvidia-smi 2>&1 | ForEach-Object { Log $_ }

Log 'Redemarrage dans 30 secondes...'
shutdown /r /t 30 /c 'Installation pilote NVIDIA CUDA 546.12 - redemarrage requis'
