<#
  CheckCpuMicrocode.ps1

  Reports the processor, the board, the BIOS and the loaded microcode, then says
  whether the machine needs a BIOS update for Intel's Vmin Shift Instability defect.

  The defect affects Intel 13th and 14th Generation Core desktop processors, which
  report CPUID model 183 (0xB7) or 191 (0xBF). Intel released microcode 0x12B in
  September 2024 as the final mitigation. A processor below that revision is still
  degrading. The damage is permanent, so the update prevents more of it and does not
  repair what is done.

  Needs no administrator rights. Reads only.

  Local:
      powershell -NoProfile -ExecutionPolicy Bypass -File CheckCpuMicrocode.ps1

  Remote:
      Invoke-Command -ComputerName RIG2 -FilePath .\CheckCpuMicrocode.ps1
#>

$ErrorActionPreference = 'SilentlyContinue'

# ---- processor -------------------------------------------------------------
$cpu = Get-CimInstance Win32_Processor | Select-Object -First 1

# Win32_Processor.Caption reads like "Intel64 Family 6 Model 183 Stepping 1"
$family = $null; $model = $null; $stepping = $null
if ($cpu.Caption -match 'Family (\d+) Model (\d+) Stepping (\d+)') {
    $family   = [int]$Matches[1]
    $model    = [int]$Matches[2]
    $stepping = [int]$Matches[3]
}

# ---- microcode -------------------------------------------------------------
$cpuKey = 'HKLM:\HARDWARE\DESCRIPTION\System\CentralProcessor\0'
$cur = $null; $prev = $null
$b = (Get-ItemProperty $cpuKey).'Update Revision'
if ($b -and $b.Length -ge 8) { $cur = [BitConverter]::ToUInt32($b, 4) }
$b2 = (Get-ItemProperty $cpuKey).'Previous Update Revision'
if ($b2 -and $b2.Length -ge 8) { $prev = [BitConverter]::ToUInt32($b2, 4) }

# ---- board and BIOS --------------------------------------------------------
$board = Get-CimInstance Win32_BaseBoard | Select-Object -First 1
$bios  = Get-CimInstance Win32_BIOS | Select-Object -First 1
$os    = Get-CimInstance Win32_OperatingSystem

Write-Host ''
Write-Host ("=" * 68)
Write-Host ("  {0}" -f $env:COMPUTERNAME)
Write-Host ("=" * 68)

'{0,-22} {1}' -f 'Processor',    $cpu.Name
'{0,-22} {1}'      -f 'CPUID',   ("family {0}  model {1} (0x{1:X})  stepping {2}" -f $family, $model, $stepping)
'{0,-22} {1} cores / {2} threads' -f 'Topology', $cpu.NumberOfCores, $cpu.NumberOfLogicalProcessors
if ($cur -ne $null) { '{0,-22} 0x{1:X}' -f 'Microcode', $cur } else { '{0,-22} unknown' -f 'Microcode' }
if ($prev -ne $null -and $prev -ne $cur) { '{0,-22} 0x{1:X}' -f '  loaded over', $prev }
''
'{0,-22} {1} {2} {3}' -f 'Board', $board.Manufacturer, $board.Product, $board.Version
'{0,-22} {1}' -f 'BIOS', $bios.SMBIOSBIOSVersion
'{0,-22} {1}' -f 'BIOS date', $(if ($bios.ReleaseDate) { ([datetime]$bios.ReleaseDate).ToString('yyyy-MM-dd') } else { 'unknown' })
'{0,-22} {1} build {2}' -f 'Operating system', $os.Caption, $os.BuildNumber
'{0,-22} {1:N0} GB' -f 'Memory', ((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB)
''

# ---- verdict ---------------------------------------------------------------
$affectedModels = @(183, 191)   # 0xB7 and 0xBF, Raptor Lake-S
$fixRevision    = 0x12B

if ($family -ne 6 -or $affectedModels -notcontains $model) {
    Write-Host 'VERDICT: not a 13th or 14th Generation Core desktop processor.' -ForegroundColor Green
    Write-Host '         Intel''s Vmin Shift Instability defect does not apply.'
}
elseif ($cur -eq $null) {
    Write-Host 'VERDICT: affected family, but the microcode revision could not be read.' -ForegroundColor Yellow
}
elseif ($cur -lt $fixRevision) {
    Write-Host ('VERDICT: NEEDS A BIOS UPDATE.  microcode 0x{0:X} is below 0x12B.' -f $cur) -ForegroundColor Red
    Write-Host ''
    Write-Host '  This processor has never received Intel''s mitigation. It requests'
    Write-Host '  excessive core voltage, which degrades the silicon permanently and'
    Write-Host '  progressively. The symptom is random access violations that move'
    Write-Host '  between programs and grow more frequent over months.'
    Write-Host ''
    Write-Host '  1. Install the newest BIOS from the board maker for this exact model'
    Write-Host '     and revision. Any release from late 2024 carries 0x12B or later.'
    Write-Host '  2. Load Intel Default Settings in the BIOS. The power limits matter'
    Write-Host '     as much as the microcode.'
    Write-Host '  3. Run this script again to confirm.'
    Write-Host ''
    Write-Host '  Intel extended the warranty on these processors to five years.'
}
else {
    Write-Host ('VERDICT: OK.  microcode 0x{0:X} includes Intel''s mitigation.' -f $cur) -ForegroundColor Green
    Write-Host '         Confirm Intel Default Settings is enabled in the BIOS as well.'
}
Write-Host ''
