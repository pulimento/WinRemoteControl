param([switch]$ApplyDefaultProfile, [switch]$Rebuild)

$ErrorActionPreference = 'Stop'

$debugDirectory = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../WinRemoteControl/bin/Debug/net10.0-windows'))
$executable = Join-Path $debugDirectory 'WinRemoteControl DEBUG.exe'
if (-not $Rebuild -and -not (Test-Path -LiteralPath $executable)) { throw 'Build Debug before running this script.' }

# Close the main window even when it is hidden in the tray. WM_CLOSE preserves
# the application's unsaved-edits prompt and MQTT shutdown; never force-kill it.
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class DebugWindowCloser {
    private delegate bool Callback(IntPtr window, IntPtr data);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Callback callback, IntPtr data);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int length);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    public static bool Close(int processId) {
        bool requested = false;
        EnumWindows((window, data) => {
            GetWindowThreadProcessId(window, out uint owner);
            if (owner != processId) return true;
            var title = new StringBuilder(256);
            GetWindowText(window, title, title.Capacity);
            if (title.ToString() != "WinRemoteControl" && title.ToString() != "WinRemoteControl DEBUG") return true;
            requested = PostMessage(window, 0x10, IntPtr.Zero, IntPtr.Zero);
            return false;
        }, IntPtr.Zero);
        return requested;
    }
}
'@

foreach ($process in Get-Process -Name 'WinRemoteControl', 'WinRemoteControl DEBUG' -ErrorAction SilentlyContinue) {
    if (-not [string]::Equals([IO.Path]::GetDirectoryName($process.Path), $debugDirectory, [StringComparison]::OrdinalIgnoreCase)) { continue }
    if (-not [DebugWindowCloser]::Close($process.Id) -or -not $process.WaitForExit(10000)) {
        throw 'The previous Debug instance is still running. Resolve any unsaved-edits prompt and close it before restarting.'
    }
}
if ($Rebuild) {
    dotnet build (Join-Path $PSScriptRoot '../WinRemoteControl/WinRemoteControl.csproj') -c Debug --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Debug build failed; the application was not restarted.' }
}
$arguments = @('--settings')
if ($ApplyDefaultProfile) { $arguments += '--apply-default-profile' }
Start-Process -FilePath $executable -WorkingDirectory $debugDirectory -ArgumentList $arguments -PassThru | Select-Object Id, ProcessName
