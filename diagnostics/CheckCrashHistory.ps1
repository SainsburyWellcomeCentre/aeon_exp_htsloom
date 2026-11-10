<#
  CheckCrashHistory.ps1

  Reports how often a machine has stopped unexpectedly, and whether the operating
  system or the processor recorded a reason. Run it with CheckCpuMicrocode.ps1 to
  tell an affected processor that is already damaged from one caught early.

  What each record means:

    Event 41 and 6008    the machine went down without a clean shutdown. With no
                         bugcheck beside it, the machine stopped executing rather
                         than blue screening, which is a harder failure.
    Event 1001           a bugcheck, with its code. 0x9C MACHINE_CHECK_EXCEPTION
                         and 0x124 WHEA_UNCORRECTABLE_ERROR come from the processor
                         itself. 0x1A subcode 0x403 is a documented memory fault.
    WHEA-Logger          corrected and uncorrected hardware errors.
    Event 1000           an application crashed. Faults that move between different
                         programs point at the machine, not at one program.

  Needs no administrator rights for the logs. Reads only.

  Local:
      powershell -NoProfile -ExecutionPolicy Bypass -File CheckCrashHistory.ps1

  Remote:
      Invoke-Command -ComputerName RIG2 -FilePath .\CheckCrashHistory.ps1
#>

$ErrorActionPreference = 'SilentlyContinue'

$BUG = @{
  0x1A  = 'MEMORY_MANAGEMENT';          0x9C  = 'MACHINE_CHECK_EXCEPTION'
  0x124 = 'WHEA_UNCORRECTABLE_ERROR';   0x101 = 'CLOCK_WATCHDOG_TIMEOUT'
  0x12B = 'FAULTY_HARDWARE_CORRUPTED_PAGE'; 0x109 = 'CRITICAL_STRUCTURE_CORRUPTION'
  0x1E  = 'KMODE_EXCEPTION_NOT_HANDLED';    0x3B  = 'SYSTEM_SERVICE_EXCEPTION'
  0x50  = 'PAGE_FAULT_IN_NONPAGED_AREA';    0x7F  = 'UNEXPECTED_KERNEL_MODE_TRAP'
  0xD1  = 'DRIVER_IRQL_NOT_LESS_OR_EQUAL';  0x133 = 'DPC_WATCHDOG_VIOLATION'
}
$HARDWARE = @(0x1A, 0x9C, 0x124, 0x101, 0x12B, 0x109, 0x7F)

Write-Host ''
Write-Host ('=' * 72)
Write-Host ("  {0}" -f $env:COMPUTERNAME)
Write-Host ('=' * 72)

$os = Get-CimInstance Win32_OperatingSystem
'{0,-24} {1}' -f 'Last boot', $os.LastBootUpTime
'{0,-24} {1:N1} days' -f 'Uptime', ((Get-Date) - $os.LastBootUpTime).TotalDays
''

# ---- unexpected shutdowns --------------------------------------------------
Write-Host '--- Unexpected shutdowns (events 41 and 6008)'
$down = Get-WinEvent -FilterHashtable @{LogName='System'; Id=6008} -MaxEvents 40 |
        Select-Object TimeCreated, @{n='Detail';e={($_.Message -split "`r?`n")[0]}}
if ($down) { $down | Format-Table -AutoSize | Out-String -Width 160 | Write-Host }
else { Write-Host '    none found' }

# ---- bugchecks -------------------------------------------------------------
Write-Host '--- Bugchecks (event 1001)'
$bc = Get-WinEvent -FilterHashtable @{LogName='System'; Id=1001;
        ProviderName='Microsoft-Windows-WER-SystemErrorReporting'} -MaxEvents 40
if ($bc) {
  foreach ($e in $bc) {
    $code = $null
    if ($e.Message -match 'bugcheck was: 0x([0-9a-fA-F]+)') { $code = [Convert]::ToInt32($Matches[1], 16) }
    $name = if ($code -ne $null -and $BUG.ContainsKey($code)) { $BUG[$code] } else { 'unknown' }
    $flag = if ($code -ne $null -and $HARDWARE -contains $code) { '   <-- HARDWARE CLASS' } else { '' }
    '    {0}   0x{1:X}  {2}{3}' -f $e.TimeCreated, $code, $name, $flag
  }
} else { Write-Host '    none found' }
''

# ---- WHEA ------------------------------------------------------------------
Write-Host '--- WHEA hardware error log'
$whea = Get-WinEvent -FilterHashtable @{LogName='System';
          ProviderName='Microsoft-Windows-WHEA-Logger'} -MaxEvents 40 |
        Select-Object TimeCreated, Id, LevelDisplayName
if ($whea) { $whea | Format-Table -AutoSize | Out-String -Width 160 | Write-Host }
else { Write-Host '    none found' }

# ---- kernel dumps ----------------------------------------------------------
Write-Host '--- Kernel dumps on disk'
$mini = Get-ChildItem 'C:\Windows\Minidump' -Filter *.dmp |
        Select-Object Name, @{n='MB';e={[math]::Round($_.Length/1MB,1)}}, LastWriteTime
if ($mini) { $mini | Format-Table -AutoSize | Out-String -Width 160 | Write-Host }
else { Write-Host '    no minidumps' }
$full = Get-Item 'C:\Windows\MEMORY.DMP'
if ($full) { '    MEMORY.DMP  {0:N2} GB  {1}' -f ($full.Length/1GB), $full.LastWriteTime }
else { Write-Host '    no MEMORY.DMP' }

$cc = Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\CrashControl'
'    CrashDumpEnabled = {0}   (0 none, 1 complete, 2 kernel, 3 minidump, 7 automatic)' -f $cc.CrashDumpEnabled
''

# ---- application crashes ---------------------------------------------------
Write-Host '--- Application crashes (event 1000), grouped'
$app = Get-WinEvent -FilterHashtable @{LogName='Application'; Id=1000} -MaxEvents 200 |
       ForEach-Object {
         $n = if ($_.Message -match 'Faulting application name: ([^,]+)') { $Matches[1] } else { '?' }
         $m = if ($_.Message -match 'Faulting module name: ([^,]+)')      { $Matches[1] } else { '?' }
         [pscustomobject]@{ App = $n; Module = $m; When = $_.TimeCreated }
       }
if ($app) {
  $app | Group-Object App | Sort-Object Count -Descending | Select-Object -First 10 |
    ForEach-Object {
      '    {0,-34} {1,4}  last {2}' -f $_.Name, $_.Count, ($_.Group | Sort-Object When -Descending | Select-Object -First 1).When
    }
  $mods = ($app | Select-Object -ExpandProperty Module -Unique).Count
  ''
  '    {0} distinct faulting modules across {1} crashes' -f $mods, $app.Count
  if ($mods -ge 4) {
    Write-Host '    A fault that moves between many modules points at the machine, not a program.' -ForegroundColor Yellow
  }
} else { Write-Host '    none found' }

''
Write-Host '--- Summary'
$nDown = ($down | Measure-Object).Count
$nBug  = ($bc   | Measure-Object).Count
'    {0,-34} {1}' -f 'unexpected shutdowns', $nDown
'    {0,-34} {1}' -f 'bugchecks with a recorded code', $nBug
if ($nDown -gt $nBug) {
  Write-Host ''
  Write-Host ('    {0} shutdowns left no bugcheck. The machine stopped executing' -f ($nDown - $nBug)) -ForegroundColor Yellow
  Write-Host '    rather than blue screening, which is a harder kind of failure.'
}
Write-Host ''
