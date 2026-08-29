$nvi2 = 'C:\Program Files\NVIDIA Corporation\Installer2\InstallerCore\NVI2.DLL'
$pkgs = @(
    'Display.NvApp', 'Canvas', 'FrameViewSdk', 'Display.NView', 'Display.PhysX',
    'CUDAToolkit_12.9', 'CUDAToolkit_12.3',
    'CUDADevelopment_11.8', 'CUDADocument_11.8', 'nsight_nvtx_11.8',
    'CUDARuntimes_11.8', 'visual_studio_integration_11.8'
)
foreach ($p in $pkgs) {
    if (Test-Path $nvi2) {
        Start-Process 'C:\WINDOWS\SysWOW64\RunDll32.EXE' -ArgumentList "`"$nvi2`",UninstallPackage $p" -Wait -NoNewWindow -ErrorAction SilentlyContinue
    }
}
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce' -Name 'FanWidgetCudaInstall' -Value 'powershell.exe -NoProfile -ExecutionPolicy Bypass -File "c:\SOURCES\WIDGET_VENTILATEUR\post-reboot-install-cuda.ps1"'
