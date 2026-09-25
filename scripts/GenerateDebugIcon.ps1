# Tint each original Release ICO frame without changing its geometry or alpha.
# Saturated colors become magenta; neutral border pixels remain unchanged.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$resourceDirectory = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../WinRemoteControl/Resources'))
$originalPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../WinRemoteControl/big_icon_5jt_icon.ico'))
$originalBytes = [IO.File]::ReadAllBytes($originalPath)
$count = [BitConverter]::ToUInt16($originalBytes, 4)
$frames = @()
for ($index = 0; $index -lt $count; $index++) {
    $entry = 6 + 16 * $index
    $width = if ($originalBytes[$entry] -eq 0) { 256 } else { [int]$originalBytes[$entry] }
    $height = if ($originalBytes[$entry + 1] -eq 0) { 256 } else { [int]$originalBytes[$entry + 1] }
    $length = [BitConverter]::ToInt32($originalBytes, $entry + 8)
    $dataOffset = [BitConverter]::ToInt32($originalBytes, $entry + 12)
    $originalIcon = $null
    if ($originalBytes[$dataOffset] -eq 137 -and $originalBytes[$dataOffset + 1] -eq 80) {
        $sourceStream = [IO.MemoryStream]::new($originalBytes, $dataOffset, $length)
        $bitmap = [Drawing.Bitmap]::new($sourceStream)
    } else {
        # A single-frame wrapper prevents GDI+ from silently selecting another size.
        $singleFrame = [byte[]]::new(22 + $length)
        ([byte[]]@(0,0,1,0,1,0)).CopyTo($singleFrame, 0)
        [Array]::Copy($originalBytes, $entry, $singleFrame, 6, 16)
        [BitConverter]::GetBytes([int]22).CopyTo($singleFrame, 18)
        [Array]::Copy($originalBytes, $dataOffset, $singleFrame, 22, $length)
        $sourceStream = [IO.MemoryStream]::new($singleFrame)
        $originalIcon = [Drawing.Icon]::new($sourceStream)
        $bitmap = $originalIcon.ToBitmap()
    }
    try {
        if ($bitmap.Width -ne $width -or $bitmap.Height -ne $height) { throw 'Source icon frame dimensions do not match its directory entry.' }
        for ($y = 0; $y -lt $bitmap.Height; $y++) {
            for ($x = 0; $x -lt $bitmap.Width; $x++) {
                $pixel = $bitmap.GetPixel($x, $y)
                $maximum = [Math]::Max([int]$pixel.R, [Math]::Max([int]$pixel.G, [int]$pixel.B))
                $minimum = [Math]::Min([int]$pixel.R, [Math]::Min([int]$pixel.G, [int]$pixel.B))
                if ($pixel.A -gt 0 -and $maximum - $minimum -gt 8) {
                    # HSV hue 300 degrees, with original saturation/value and alpha.
                    $bitmap.SetPixel($x, $y, [Drawing.Color]::FromArgb($pixel.A, $maximum, $minimum, $maximum))
                }
            }
        }
        $stream = [IO.MemoryStream]::new()
        try {
            $bitmap.Save($stream, [Drawing.Imaging.ImageFormat]::Png)
            $frames += @{ Width = $bitmap.Width; Height = $bitmap.Height; Bytes = $stream.ToArray() }
        } finally { $stream.Dispose() }
        if ($width -eq 256) { $bitmap.Save((Join-Path $resourceDirectory 'DebugIcon.png'), [Drawing.Imaging.ImageFormat]::Png) }
    } finally {
        $bitmap.Dispose()
        if ($originalIcon) { $originalIcon.Dispose() }
        $sourceStream.Dispose()
    }
}
$output = [IO.File]::Create((Join-Path $resourceDirectory 'DebugIcon.ico'))
$writer = [IO.BinaryWriter]::new($output)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
    $offset = 6 + 16 * $frames.Count
    foreach ($frame in $frames) {
        $writer.Write([byte]$(if ($frame.Width -eq 256) { 0 } else { $frame.Width }))
        $writer.Write([byte]$(if ($frame.Height -eq 256) { 0 } else { $frame.Height }))
        $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$frame.Bytes.Length); $writer.Write([uint32]$offset)
        $offset += $frame.Bytes.Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame.Bytes) }
} finally { $writer.Dispose(); $output.Dispose() }
