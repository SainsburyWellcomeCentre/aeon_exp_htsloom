# Bonsai crash diagnostics

Tools for finding out why `Bonsai.exe` died on the rig. They collect two things:

1. **A full-memory crash dump** (`.dmp`) of the process at the moment it crashed. Windows
   writes it automatically. No debugger is attached and nobody needs to be watching.
2. **A memory trace** (`.csv`) of the run before the crash, one row per minute. A slow leak
   or a gradual rise shows up here before the crash happens.

Together they answer the two questions a post-mortem needs: what was the process doing at
the instant it died (the dump), and was it drifting there for hours or did it fail suddenly
(the trace).

## What is in this folder

| File | Purpose | Needs admin? |
|---|---|---|
| `BonsaiCrashDiag.ps1` | The tool itself. Four modes: `-Setup`, `-Status`, `-Search`, `-Sample`. | `-Setup` only |
| `RunBonsaiDiag.cmd` | Double-click launcher. Asks for admin rights (UAC), then runs `-Setup -Status -Search`. | yes, it prompts |
| `Sample.cmd` | Double-click launcher for the memory sampler (`-Sample`). Keep it open for the whole run. | no |

## How the crash dumps are created

The dumps come from **Windows Error Reporting (WER) LocalDumps**, a built-in Windows
feature. Nothing inside Bonsai writes them, and no debugger is involved.

`-Setup` writes three values under this registry key:

```
HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\Bonsai.exe
    DumpFolder  REG_EXPAND_SZ  C:\CrashDumps
    DumpType    REG_DWORD      2          ; 2 = full dump (all process memory)
    DumpCount   REG_DWORD      10         ; keep at most 10; the oldest is overwritten
```

What happens when Bonsai crashes:

1. `Bonsai.exe` hits an unhandled error it cannot recover from. This means a native access
   violation (`0xC0000005`) or a fatal .NET error such as `ExecutionEngineException`
   (`0x80131506`) or `FailFast`. Normal .NET exceptions that Bonsai catches and shows in
   the GUI do **not** count.
2. The process hands control to WER. WER looks for a `LocalDumps\<exe name>` key and finds
   the `Bonsai.exe` key above.
3. WER starts `WerFault.exe`, which copies the whole process memory into
   `C:\CrashDumps\Bonsai.exe.<pid>.dmp` before the process is closed. With `DumpType = 2`
   this includes every thread's stack, the managed heap, all loaded native modules and the
   exception record. That is everything a debugger needs to replay the last instant.
4. WER writes an entry to the Application event log and lets the process exit.

What this means in practice:

- **A dump is only written for a real crash.** A hang, a clean exit, or closing the Bonsai
  window leaves nothing. For a hang, take a dump by hand (see
  [Taking a dump by hand](#taking-a-dump-by-hand)).
- **Full dumps are large**: about the size of the process, so 1.5 to 2 GB for a 4-camera
  run. `DumpCount = 10` limits the folder to about 20 GB. Make sure `C:` has the space.
- The key is under `HKLM`, so it applies to every user on the machine and to every process
  named `Bonsai.exe` (both the bootstrapper and the real `Packages\...\Bonsai.exe` it
  starts). This is why `-Setup` needs an admin shell.
- The setting is **machine-wide and permanent**. It survives reboots and stays active until
  the key is deleted (see [Turning it off](#turning-it-off)).

## Setting it up on the rig

Do this once per machine:

1. Copy this `diagnostics/` folder to the rig. It only needs Windows PowerShell 5.1, which
   every Windows install has.
2. Double-click **`RunBonsaiDiag.cmd`** and accept the UAC prompt. It registers the WER key,
   creates `C:\CrashDumps`, prints the resulting settings, and searches `C:` for any dumps
   left by earlier crashes.
3. Check that the output shows `DumpType : 2` and `DumpFolder : C:\CrashDumps`.

The `.cmd` files call `powershell` with `-ExecutionPolicy Bypass` because the rig's default
policy refuses unsigned scripts. It applies to that one call only and does not change the
machine's policy.

To check the settings later without registering again, run this from any normal
(non-admin) PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File BonsaiCrashDiag.ps1 -Status
```

## Every run: start the memory sampler

WER only captures the crash instant. To see the hours before it, start the sampler in its
own window when you start acquisition and leave it open until the run ends:

- Double-click **`Sample.cmd`** (no admin needed), or run
  `powershell -ExecutionPolicy Bypass -File BonsaiCrashDiag.ps1 -Sample -IntervalSec 60`.

It adds one row per Bonsai process per minute to `C:\CrashDumps\bonsai_mem.csv`:

```
timestamp,pid,privateBytes,workingSet,managedHeapBytes,handleCount,threadCount
```

- `privateBytes`: all memory the process has committed, managed and native.
- `managedHeapBytes`: only the .NET GC heap. It comes from the `.NET CLR Memory`
  performance counter, matched to the PID. It is blank if the counter is not available.
- `handleCount` and `threadCount`: kernel handles and OS threads.

There are normally **two** `Bonsai.exe` processes: the bootstrapper and the real one. Filter
by `pid` when plotting. The one with the large `privateBytes` is the rig.

How to read the trace:

| Pattern | Meaning |
|---|---|
| `privateBytes` rises, `managedHeapBytes` stays flat | **Native leak**: camera SDK, video encoder, unmanaged interop. |
| `managedHeapBytes` rises | **Managed leak**: something in the workflow is buffering without limit. |
| `handleCount` rises | Leaked native handles: files, serial ports, events. |
| Everything flat, then a crash | Not a leak. A race, memory corruption, or a bad input that appears after N hours. |

## After a crash: what to collect

Copy all of these to the dev machine:

1. **`C:\CrashDumps\Bonsai.exe.<pid>.dmp`**, the dump. Match `<pid>` to the last rows of the
   CSV to be sure it is the right process.
2. **`C:\CrashDumps\bonsai_mem.csv`**, the memory trace.
3. **Application event log** entries around the crash time. Use Event Viewer
   (*Windows Logs → Application*) or:
   ```powershell
   Get-WinEvent -LogName Application -MaxEvents 200 |
     Where-Object { $_.ProviderName -in "Application Error",".NET Runtime","Windows Error Reporting" } |
     Format-List TimeCreated, Id, ProviderName, Message
   ```
   - Event **1026** (.NET Runtime): the managed exception type and the stack at the failure.
   - Event **1000** (Application Error): the native module and offset that faulted.
   - Event **1001** (Windows Error Reporting): confirms which `.dmp` file WER wrote.
4. The Bonsai console or log output, if it was visible.
5. The **git commit** the rig was running, and the YAML configs in `src/`.

`-Search` (which `RunBonsaiDiag.cmd` also runs) lists the Bonsai entries under
`%ProgramData%\Microsoft\Windows\WER\ReportArchive`. These are WER's own per-crash report
folders. They are useful if a dump is missing.

## Analysing a dump

The `.dmp` is a standard Windows minidump. Any of these tools can open it:

- **Visual Studio**: *File → Open → File…*, choose the `.dmp`, then *Debug with Mixed*. This
  is the easiest way to read managed and native stacks side by side.
- **WinDbg**: `!analyze -v` gives the automatic verdict. Then `.loadby sos clr` followed by
  `!clrstack`, `!threads` and `!dumpheap -stat` for the managed side. `.exr -1` shows the
  raw native exception record (access type and fault address). That record is what tells a
  read/write access violation apart from a jump to address 0.
- **ClrMD** (the `Microsoft.Diagnostics.Runtime` NuGet package): for scripted analysis in a
  small .NET console app. Useful when you need to ask the same question of several dumps.

To resolve symbols, the dump needs the **same Bonsai and package binaries** the rig was
running. The rig cannot restore NuGet packages, so its `.bonsai/Packages` folder is a copy of
the dev machine's. Keep the two in step and the dev copy will match the dump.

## Taking a dump by hand

WER does not fire for a **hang** (Bonsai is alive but frozen). Take a dump manually instead:

- **Task Manager**: *Details* tab, right-click the large `Bonsai.exe`, choose *Create dump
  file*. It writes a full dump to `%TEMP%\Bonsai.DMP` and shows you the path.
- **procdump** (Sysinternals): `procdump -ma <pid> C:\CrashDumps\Bonsai_hang.dmp`.
  `-ma` means full memory, the same as `DumpType = 2`.

Both are safe on a running process. It pauses for a few seconds while memory is copied.

## Turning it off

WER LocalDumps stays registered until you remove it. From an admin shell:

```powershell
reg delete "HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\Bonsai.exe" /f
```

The dumps in `C:\CrashDumps` are not deleted automatically. Remove them by hand once they
have been analysed.

## What this tooling has found so far

Kept here so the next person does not investigate a solved problem again.

- **July 2026, crash after about 3 hours. Resolved.** The full dump showed a *write* access
  violation inside `opencv_ffmpeg2413_64.dll` (the encoder bundled with Bonsai.Vision) on a
  video-writer thread. The managed heap was small and flat, so this was native encoder
  corruption, not a leak. Fixed by moving downstream work off the Spinnaker acquisition
  thread (`rx:ObserveOn` after `SpinnakerCapture` in `Cameras.bonsai`), so frames no
  longer back up in the driver.
- **September 2026, three crashes with one identical signature (issue #56).** A different
  fault: a jump to address 0 from inside `System.Reactive.Producer.SubscribeRaw`, while the
  runtime was building a delegate. The CPU context showed `RIP = 0` and `RAX = 0`, with the
  same return-address chain in all three dumps. The encoder threads were idle, the heap was
  healthy and camera lag was flat. The cause of the exposure was found in the control panel:
  the per-lane distance display subscribed a new inner workflow for every wheel sample, about
  500 times per second on a Harp serial thread. The control panel now subscribes once per
  state instead.
- The header comment in `BonsaiCrashDiag.ps1` still lists the first July guesses (an Aeon
  package version mismatch). The first dump ruled that out: every `Aeon.*` module on the rig
  is the same version, 0.7.0.
