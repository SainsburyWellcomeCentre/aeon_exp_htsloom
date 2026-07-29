<#
  Bonsai rig crash diagnostics for aeon_exp_htsloom.

  Crash under investigation (2026-07-24): Bonsai.exe terminates ~3h into a run with
  ExecutionEngineException (exit 0x80131506) + access violation (0xc0000005) inside
  clr.dll. That signature = the .NET runtime hit corrupted memory (GC heap) and
  self-killed. It is NOT a .bonsai workflow-logic bug -- managed code throws catchable
  exceptions, it cannot corrupt the GC heap. Root cause is native/unmanaged interop
  accumulating over ~3h. Prime suspects: the Aeon.Acquisition 0.6.0 vs Aeon.Video 0.7.0
  version mismatch, the FLIR Spinnaker SDK, or the video encoder.

  Goal of this script: capture a full-heap dump of the NEXT crash and a memory trace of
  the run leading up to it, so we can tell native-leak vs managed-leak and name the
  corrupting component.

  Modes (combine freely; -Setup needs an elevated shell):
    -Setup    Register WER LocalDumps so the next crash writes a full-heap dump.
    -Status   Show current WER LocalDumps config + any dumps already captured.
    -Search   Hunt for existing Bonsai dumps / WER archives on C:.
    -Sample   Loop forever: append process + managed-heap memory to a CSV every
              -IntervalSec seconds (Ctrl+C to stop). No admin needed. Start this in
              its own window when you kick off the rig run.

  Examples:
    powershell -ExecutionPolicy Bypass -File BonsaiCrashDiag.ps1 -Setup -Status -Search
    powershell -ExecutionPolicy Bypass -File BonsaiCrashDiag.ps1 -Sample
#>
param(
  [switch]$Setup,
  [switch]$Status,
  [switch]$Search,
  [switch]$Sample,
  [string]$ProcessName = "Bonsai",
  [string]$DumpFolder  = "C:\CrashDumps",
  [int]   $IntervalSec = 60
)

$ErrorActionPreference = "Stop"
$werKey   = "HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\$ProcessName.exe"
$werKeyPS = "HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\$ProcessName.exe"

function Test-Admin {
  $id = [System.Security.Principal.WindowsIdentity]::GetCurrent()
  (New-Object System.Security.Principal.WindowsPrincipal($id)).IsInRole(
    [System.Security.Principal.WindowsBuiltinRole]::Administrator)
}

function Invoke-Setup {
  if (-not (Test-Admin)) {
    Write-Warning "-Setup needs an elevated shell (writes under HKLM). Re-run via RunBonsaiDiag.cmd or an admin PowerShell."
    return
  }
  Write-Host "Registering WER LocalDumps for $ProcessName.exe -> $DumpFolder ..." -ForegroundColor Cyan
  # reg.exe auto-creates the intermediate keys (Windows Error Reporting\LocalDumps\<exe>).
  & reg add $werKey /v DumpFolder /t REG_EXPAND_SZ /d "$DumpFolder" /f | Out-Null
  & reg add $werKey /v DumpType   /t REG_DWORD     /d 2            /f | Out-Null   # 2 = full heap
  & reg add $werKey /v DumpCount  /t REG_DWORD     /d 10           /f | Out-Null
  New-Item -ItemType Directory -Path $DumpFolder -Force | Out-Null
  Write-Host "Done. The next $ProcessName crash will drop a full-heap .dmp into $DumpFolder." -ForegroundColor Green
}

function Invoke-Status {
  Write-Host "`n== WER LocalDumps config ==" -ForegroundColor Cyan
  if (Test-Path $werKeyPS) {
    Get-ItemProperty $werKeyPS | Format-List DumpFolder, DumpType, DumpCount
  } else {
    Write-Host "(not configured yet -- run -Setup elevated)" -ForegroundColor Yellow
  }

  Write-Host "`n== Dumps in $DumpFolder ==" -ForegroundColor Cyan
  if (Test-Path $DumpFolder) {
    $d = Get-ChildItem $DumpFolder -Filter *.dmp -File -EA SilentlyContinue | Sort-Object LastWriteTime
    if ($d) { $d | Format-Table Name, @{n="MB";e={[math]::Round($_.Length/1MB,1)}}, LastWriteTime -AutoSize }
    else    { Write-Host "(none yet)" -ForegroundColor Yellow }
  } else { Write-Host "(folder does not exist yet)" -ForegroundColor Yellow }

  Write-Host "`n== $ProcessName running now? ==" -ForegroundColor Cyan
  $p = Get-Process $ProcessName -EA SilentlyContinue
  if ($p) { $p | Format-Table Id, @{n="PrivateMB";e={[math]::Round($_.PrivateMemorySize64/1MB)}}, HandleCount, @{n="Threads";e={$_.Threads.Count}} -AutoSize }
  else    { Write-Host "(not running)" -ForegroundColor Yellow }
}

function Invoke-Search {
  Write-Host "`n== Archived WER reports for $ProcessName ==" -ForegroundColor Cyan
  $arch = Join-Path $env:ProgramData "Microsoft\Windows\WER\ReportArchive"
  Get-ChildItem $arch -Directory -Filter "*$ProcessName*" -EA SilentlyContinue |
    Sort-Object LastWriteTime | Format-Table Name, LastWriteTime -AutoSize

  Write-Host "== Any $ProcessName dump on C: (this can take a minute) ==" -ForegroundColor Cyan
  $hits = Get-ChildItem C:\ -Recurse -Include "$ProcessName*.dmp","*.hdmp" -File -EA SilentlyContinue |
          Select-Object FullName, @{n="MB";e={[math]::Round($_.Length/1MB,1)}}, LastWriteTime
  if ($hits) { $hits | Format-Table -AutoSize } else { Write-Host "(no dump files found)" -ForegroundColor Yellow }
}

function Get-ManagedHeapByPid {
  # Best-effort map of PID -> managed-heap bytes. There can be several .NET CLR Memory
  # instances (Bonsai, Bonsai#1, ...); join them to PIDs via the "Process ID" counter.
  $map = @{}
  try {
    $ids   = (Get-Counter "\.NET CLR Memory(*)\Process ID" -EA Stop).CounterSamples
    $heaps = (Get-Counter "\.NET CLR Memory(*)\# Bytes in all Heaps" -EA Stop).CounterSamples
    $pidByInstance = @{}
    foreach ($s in $ids)   { $pidByInstance[$s.InstanceName] = [int]$s.CookedValue }
    foreach ($s in $heaps) {
      if ($pidByInstance.ContainsKey($s.InstanceName)) { $map[$pidByInstance[$s.InstanceName]] = [int64]$s.CookedValue }
    }
  } catch { }
  return $map
}

function Invoke-Sample {
  New-Item -ItemType Directory -Path $DumpFolder -Force | Out-Null
  $csv = Join-Path $DumpFolder "bonsai_mem.csv"
  if (-not (Test-Path $csv)) {
    "timestamp,pid,privateBytes,workingSet,managedHeapBytes,handleCount,threadCount" | Set-Content -Encoding utf8 $csv
  }
  Write-Host "Sampling $ProcessName every $IntervalSec s -> $csv  (Ctrl+C to stop)" -ForegroundColor Cyan
  Write-Host "One row PER process (there are usually 2 Bonsai processes; filter by pid later)." -ForegroundColor DarkGray
  Write-Host "Watch for: PrivateBytes climbing while managedHeapBytes stays flat = NATIVE leak (camera/video/version-mismatch)." -ForegroundColor DarkGray
  Write-Host "           managedHeapBytes climbing = a workflow buffering leak.  HandleCount climbing = leaked native handles." -ForegroundColor DarkGray
  while ($true) {
    $procs = @(Get-Process $ProcessName -EA SilentlyContinue)
    if ($procs.Count -gt 0) {
      $heapMap = Get-ManagedHeapByPid
      $ts = Get-Date -Format s
      foreach ($p in $procs) {
        $managed = if ($heapMap.ContainsKey($p.Id)) { $heapMap[$p.Id] } else { "" }
        $line = "{0},{1},{2},{3},{4},{5},{6}" -f `
          $ts, $p.Id, $p.PrivateMemorySize64, $p.WorkingSet64, $managed, $p.HandleCount, $p.Threads.Count
        Add-Content -Path $csv -Value $line
      }
    }
    Start-Sleep -Seconds $IntervalSec
  }
}

if (-not ($Setup -or $Status -or $Search -or $Sample)) {
  Write-Host "Bonsai crash diagnostics -- pick one or more modes:" -ForegroundColor Cyan
  Write-Host "  -Setup   register WER full-heap dump on next crash (needs admin)"
  Write-Host "  -Status  show WER config + captured dumps + live process"
  Write-Host "  -Search  find existing dumps / WER archives"
  Write-Host "  -Sample  log memory to $DumpFolder\bonsai_mem.csv every $IntervalSec s"
  Write-Host "`ne.g.  powershell -ExecutionPolicy Bypass -File BonsaiCrashDiag.ps1 -Setup -Status"
  return
}

if ($Setup)  { Invoke-Setup }
if ($Status) { Invoke-Status }
if ($Search) { Invoke-Search }
if ($Sample) { Invoke-Sample }
