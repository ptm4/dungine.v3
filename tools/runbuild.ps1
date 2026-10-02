param([int]$Wait = 25, [string]$Out = "E:\Unity\Projects\dungine.v3\Assets\Captures\build_shot.png", [string]$Extra = "")
# Launches the built game windowed, captures its window with PrintWindow (works even when covered), then closes it.
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class W32 {
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hwnd, out RECT r);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@
$exe = "E:\Unity\Projects\dungine.v3\Builds\DungineII\DungineII.exe"
$log = "E:\Unity\Projects\dungine.v3\Builds\player_test.log"
$args = "-screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile `"$log`" $Extra"
$p = Start-Process -FilePath $exe -ArgumentList $args -PassThru
Start-Sleep -Seconds $Wait
$p.Refresh()
$h = $p.MainWindowHandle
if ($h -eq [IntPtr]::Zero) { Write-Output "no window"; } else {
  $r = New-Object W32+RECT
  [W32]::GetClientRect($h, [ref]$r) | Out-Null
  $w = [Math]::Max(1, $r.Right - $r.Left); $hh = [Math]::Max(1, $r.Bottom - $r.Top)
  $bmp = New-Object System.Drawing.Bitmap $w, $hh
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $hdc = $g.GetHdc()
  [W32]::PrintWindow($h, $hdc, 3) | Out-Null
  $g.ReleaseHdc($hdc); $g.Dispose()
  $bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
  Write-Output "captured ${w}x${hh}"
}
Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1
if (Test-Path $log) { Select-String -Path $log -Pattern "Exception|Error|error|Shader|shader" | Select-Object -First 30 | ForEach-Object { $_.Line } }
