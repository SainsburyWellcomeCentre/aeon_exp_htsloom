# Hardware checks on the rig

A record of the checks on the rig, and of the hypotheses they closed. The work ran
between 14 and 16 October 2026. Phases A to F need no reboot. The BIOS update of
16 October did.

**The machine has recorded hardware faults since June 2023.** The Windows kernel
bugchecked three times, and all three are in the hardware class. Two of them are
machine check exceptions, which the processor itself raises. Those records come from the
operating system and from the processor. They do not come from our software, and they
predate our crash series by three years. See **The machine's own crash records** below.

**What is still not established** is that every application crash since July 2026 has
this cause. That link is strong, but it is inference and not measurement.

## Checks

| Phase | Test | Status | Result |
|---|---|---|---|
| A | Windows event log, memory configuration | done | No fault found. The memory is **not ECC**, so the log proves nothing. |
| B | MemTest64, 8 hours | done | **616 loops, 0 errors.** Bad memory cells are unlikely. |
| C | Page heap on Bonsai | done | **Not usable.** 50 fps falls to 20 fps in both modes. |
| D | Replace the video encoder | done | OpenCV is out. Each camera uses its own ffmpeg process. **The crash followed.** |
| E | Identify the processor | done | **i9-13900K on microcode `0x11D`.** Three years behind every Intel mitigation. |
| H | Read the kernel bugcheck records | done | **Three bugchecks, all hardware.** Two are processor machine checks. |
| F | ffmpeg alone, without Bonsai | **repeat** | Started on 16 October, then stopped for the BIOS update. It did not run long enough to show anything. |
| G | MemTest86 from a USB stick | not run | Needs a reboot and a person at the machine. Low priority now. |

Two buffer lifetime hypotheses and a handle leak are also closed. We closed those by
reading the source, not by test.

## Actions

| Action | Status |
|---|---|
| Update the BIOS to a release with microcode `0x12B` or later | **done 16 Oct 2026.** F12b to F20, `0x11D` to `0x137` |
| Set Intel Default Settings | **done.** Performance profile, 253 W and 307 A |
| Keep XMP off | **done.** DDR5-4800 JEDEC at 1.10 V |
| Set AC BACK to Always On | **done.** The rig returns after a power cut |
| Stop Windows demoting Bonsai when it loses focus | **done 16 Oct 2026.** See **Windows settings changed on 16 October 2026** |
| Make the Bonsai priority survive a reboot | **done.** `CpuPriorityClass = 6`, verified after a reboot |
| Confirm the cameras hold 50 and 100 fps under the new power limit | **done.** Camera lag fell from 0.028 s to 0.018 s |
| Throttle the control panel displays | **done 16 Oct 2026.** Every display now samples at 20 Hz |
| Log memory and hardware sensors during a run | **done 16 Oct 2026.** See **Monitoring from 16 October 2026** |
| Run the rig overnight and watch for a crash | **in progress from 16 Oct 2026.** The median time to crash was about 4 hours |
| Repeat phase F with the new microcode | open |
| Exclude the recording folders from real-time scanning | open. The client is standalone, so this is settable locally. **Real-time scanning was switched off for the 16 Oct overnight run** |
| Open the Intel warranty claim | open |
| Reopen issue #56 | open |
| Put this document on a branch from `main` | open |

## The crash record

Fourteen crashes between 27 July and 15 October 2026. The values come from the minidump
exception records, except where the table says otherwise.

| Date, UTC | Process | Access | Fault address | Faulting module |
|---|---|---|---|---|
| 27 Jul 12:32 | Bonsai | WRITE | `0x7F813E04` | `opencv_ffmpeg2413_64.dll+0x505FEF` |
| 1 Sep 12:26 | Bonsai | EXECUTE | `0x0` | unmapped |
| 14 Sep 11:43 | Bonsai | EXECUTE | `0x0` | unmapped |
| 16 Sep 11:53 | Bonsai | EXECUTE | `0x0` | unmapped |
| 2 Oct 08:33 | Bonsai | CLR exception | | `KERNELBASE.dll` (stop time, separate fault) |
| 5 Oct 11:59 | Bonsai | READ | `0xFFFFFFFFFFFFFFFF` | `clr.dll+0x4863C` |
| 6 Oct 18:27 | Bonsai | WRITE | `0x468770B8` | `KERNELBASE.dll+0x2CF19` |
| 9 Oct 14:34 | Bonsai | heap corruption | | `ntdll.dll+0xFF349` |
| 12 Oct 22:22 | Bonsai | READ | `0xC3290008` | `clr.dll+0xB19F4` |
| 13 Oct 08:59 | Bonsai | EXECUTE | `0x0` | unmapped |
| 13 Oct 12:51 | Bonsai | READ | `0xFFFFFFFFFFFFFFFF` | `CONCRT140.dll+0x254FB` |
| 14 Oct 18:39 | **ffmpeg** | | | `ffmpeg.exe+0x58B9949` (event log only, no dump) |
| 15 Oct 11:50 | **ffmpeg** | WRITE | `0x0` | `ffmpeg.exe+0x5775E70` |
| 15 Oct 19:57 | Bonsai | EXECUTE | **`0x37F2F9D52068`** | unmapped |

**Eight different modules. Two different processes.**

### How to read the table

1. A logic error faults in the same place each time. These faults move between managed
   code, four native libraries, the heap manager and a second process.
2. On 9 October the Windows heap manager stated the problem directly. Its own metadata
   was damaged.
3. Until 15 October every EXECUTE fault was at address 0. A null function pointer can be
   a logic error. On 15 October the processor jumped to `0x37F2F9D52068`, which is
   arbitrary garbage in no mapped region. A logic error does not produce that.
4. The two ffmpeg faults are at different offsets in the same binary.

This pattern is memory corruption. The cause can be software that writes out of bounds,
or it can be the hardware.

### The crash rate is increasing

| Period | Crashes |
|---|---|
| July 2026 | 1 |
| August 2026 | 0 |
| September 2026 | 3 |
| 1 to 15 October 2026 | **10** |

## The machine's own crash records

This section answers the objection that only our own software fails on this machine. The
witnesses here are the Windows kernel and the processor.

### Three kernel bugchecks, all hardware

Read from `C:\Windows\Minidump` and `C:\Windows\MEMORY.DMP`.

| Date | Bugcheck | Name |
|---|---|---|
| 16 Jun 2023 | `0x1A`, subcode `0x403` | MEMORY_MANAGEMENT |
| 17 Nov 2023 | `0x9C` | MACHINE_CHECK_EXCEPTION |
| 29 Jun 2024 | `0x9C` | MACHINE_CHECK_EXCEPTION |

**`MACHINE_CHECK_EXCEPTION` is the processor speaking.** The machine check architecture
in the processor raises it. Windows only records it. Both events carry the same
parameter 1, `0x80000001`.

### The 2023 bugcheck shows a flipped bit

Microsoft documents subcode `0x403` of bugcheck `0x1A` as follows:

> The page table and PFNs are out of sync. This is probably a hardware error, especially
> if parameters differ by exactly one bit.

The parameters of the 16 June 2023 bugcheck:

```
0xFFFFFF0000849308
0xFFFFFF0000849388
```

The difference is `0x80`. **Exactly one bit.** This matches the condition in the
documentation.

### Ten unexpected shutdowns since 2024

From System log events 41 and 6008.

| Unexpected shutdown | Note |
|---|---|
| 29 Jun 2024 | bugcheck `0x9C`, dump written |
| 17 Jul 2024 15:37 | |
| 15 Aug 2024 16:29 | |
| 15 Aug 2024 17:01 | three in 90 minutes |
| 15 Aug 2024 18:00 | |
| 17 Oct 2024 12:24 | |
| 22 Feb 2025 21:25:20 | **also AEON3 at 21:24:43. A power event.** |
| 2 Mar 2026 21:53:42 | **also AEON3 at 21:53:32. A power event.** |
| 30 May 2026 07:40:15 | **also AEON3 at 07:39:58. A power event.** |
| 15 Oct 2026 23:16 | |

**Three of the ten are not this machine's fault.** AEON3, a second rig in the same room,
went down within 10 to 37 seconds on those three dates. Two machines stopping together
is a power event in the building, not a processor.

That leaves **seven** unexplained shutdowns on AEON4, plus the three kernel bugchecks.

It also removes an earlier claim from this document. The corrected machine check of
2 March 2026 at 17:51 was followed by a shutdown at 21:53, and the two looked related.
AEON3 went down in the same second, so the shutdown was a power cut and the pairing was
a coincidence.

`MEMORY.DMP` is still dated 29 June 2024. Nothing has overwritten it. Crash dumping is
enabled, because `CrashDumpEnabled` is 7. So the nine shutdowns after June 2024 **never
reached the error path in the operating system**. The machine stopped executing.

### How to read this

1. The machine blue-screened in 2023, and the dumps name hardware.
2. It then reported machine check exceptions twice.
3. Since July 2024 it stops dead, with no dump and no blue screen.

That is a decline over three years, on a processor that has never received Intel's
microcode fix. Our crash series started in July 2026, at the end of that sequence.

## AEON3 has the same defect

Checked on 23 October 2026 with `CheckCpuMicrocode.ps1` and `CheckCrashHistory.ps1`.

| Item | AEON4 | **AEON3** |
|---|---|---|
| Processor | i9-13900K | **i9-14900K** |
| CPUID | family 6, model 183, stepping 1 | **identical** |
| Board | Gigabyte Z790 AORUS MASTER | **identical** |
| BIOS | F12b, Nov 2023, a beta | **F13g PI, Apr 2024, a beta** |
| Microcode | `0x11D` | **`0x123`** |
| Operating system | Windows 10 LTSC 19044 | **identical** |
| Memory | 128 GB | 64 GB |

`0x123` is below `0x12B`, so **AEON3 has also never received any Intel mitigation.** The
i9-14900K is the top bin of the affected generation, so it carries more voltage than the
13900K does.

### Its crash record

| Item | Value |
|---|---|
| Unexpected shutdowns | 8, of which 3 are the shared power events above |
| Bugchecks | **none recorded** |
| Kernel dumps on disk | **none**, although `CrashDumpEnabled = 7` |
| Application crashes | **60, all Bonsai, across 5 distinct faulting modules** |
| Most recent crash | 22 October 2026 |

**Sixty crashes.** AEON4 produced fourteen. The fault moves between five modules on
AEON3 and eight on AEON4, which is the same signature.

**AEON3 is the `aeon_exp_foragingABC` rig.** `src/ABC-Rig.yaml` in that repository gives
`roomName: AEON3`.

### Fixed on 23 October 2026

The same `Z790AORUSMASTER.F20` file works, because the board is the same.

```
BIOS       F13g PI  ->  F20
microcode  0x123    ->  0x137      verified on the machine
power      Intel Default Settings, Performance profile
memory     XMP off, multiplier on Auto
```

Also applied: crash dumps registered for `Bonsai.exe` and `ffmpeg.exe` to
`C:\CrashDumps`, Bonsai set to Above Normal through `CpuPriorityClass = 6`, Windows
scheduling set to Background services, power plan High performance.

Recorded in `SainsburyWellcomeCentre/aeon_exp_foragingABC` issue 170, now closed.

### Why AEON3 is the better experiment

AEON4 crashed 14 times in three months, so a quiet week there could be luck. **AEON3
crashed 60 times**, and the last one was the day before the fix.

If the count on AEON3 stays near 60 over the coming weeks, that is strong evidence that
the missing microcode was the cause. If it keeps climbing, that processor is already
damaged and needs replacing under the five year warranty.

Both machines now run microcode `0x137` for the first time since they were bought.

| Rig | Fixed | Crashes before |
|---|---|---|
| AEON4, htsloom | 16 Oct 2026 | 14 in three months, 8 faulting modules |
| AEON3, foragingABC | 23 Oct 2026 | 60, 5 faulting modules |

### How to repeat this reading

The bugcheck code sits at offset `0x38` of the `DUMP_HEADER64` structure, and the four
parameters at `0x40`, `0x48`, `0x50` and `0x58`. The header starts with the ASCII tags
`PAGE` and `DU64`. Any tool that reads those offsets gives the same answer.

## The machine

Read from the crash dumps and from the rig.

| Item | Value |
|---|---|
| Processor | **13th Gen Intel Core i9-13900K**, 24 cores, 32 threads |
| CPUID | family 6, model `0xB7`, stepping 1 (Raptor Lake-S) |
| Microcode | **`0x11D`** |
| Installed memory | **128 GB**, 4 slots of 32 GB |
| Memory modules | 4 x Corsair `CMT64GX5M2X5600C40`, running at **4800 MHz** |
| Memory error correction | **None. The memory is not ECC.** |
| Commit limit | 146.8 GB |
| Operating system | Windows 10, build 19044 |

## Phase A: the Windows event log

### Finding 1: two corrected machine check errors

```
19 Feb 2026 12:57:30   Id 19   MCABank 2   ApicId 70   MciAddr 0x18c07a50
 2 Mar 2026 17:51:26   Id 19   MCABank 2   ApicId 70   MciAddr 0x18c07a50
```

The **same physical address, the same bank and the same processor**, eleven days apart.
A repeated fault at one location is not random noise.

`MciStat = 0x8c0000c040020151` decodes to valid, no overflow, **corrected**, address
valid. The error code `0x151` with `MemHierarchyLvl 1` points at a level 1 cache or TLB
fault **inside the processor core**. Bank 2 on Intel parts is a core unit. The memory
controller reports in much higher banks, so this is not a DIMM error.

**This does not explain the crashes.** Both errors happened before the crash series
began on 27 July. Nothing was logged in between. A corrected error is one the hardware
repaired, so no data was damaged.

It does show that this machine produces machine check errors.

### Finding 2: the other events are not relevant

- **Id 2**, several dates, including at each boot. The raw data holds the text `MEI` and
  `CSME FW status`. This is the Intel Management Engine reporting its firmware state. It
  is not a memory error.
- **Id 1**, November 2023 and June 2024. Fatal. The section GUID marks them as PCI
  Express. They are two years old and predate the problem.

### Finding 3: the memory is not ECC, and this is decisive

`Win32_PhysicalMemoryArray.MemoryErrorCorrection` returns **3**, which means none.

Non-ECC memory cannot detect a flipped bit. Nothing is logged, because nothing notices.

So the silence in the WHEA log through the whole crash period says **nothing at all**
about the health of the memory. The one argument against bad memory has gone.

### Conclusion of phase A

1. The hardware is **not excluded**.
2. The event log **cannot** confirm or deny a memory fault on this machine.
3. The processor has a small, repeated defect at one cache address, last seen in March.
   Keep it in the record.

## Phase B: the memory test is clean

Run on 14 and 15 October 2026 with MemTest64, with Bonsai closed.

| Item | Value |
|---|---|
| Duration | 28803 seconds, 8.0 hours |
| Loops completed | **616** |
| Threads | 32 |
| Memory tested | Maximum, about 110 GB of 128 GB |
| **Errors** | **0** |

Each loop runs four algorithms: Stuck Address, Random Data, Move Data and Bitpattern.
The tool reported `Test finished with no errors detected`.

The modules are rated 5600 but run at 4800, so XMP is off and the memory runs at the
JEDEC default. **An overclock is not the cause.**

Note the configuration. `CMT64GX5M2X5600C40` is a kit of **two** 32 GB modules, so the
rig runs **two separate kits**. Vendors match modules within a kit, not across kits. It
is also two modules per channel, which DDR5 handles much worse than one.

**What this clears.** The memory cells are very probably good. 616 loops is a strong
pass.

**What it does not clear.**

- A Windows tester cannot reach the 10 to 18 GB that the operating system holds.
- It tests through the normal virtual mapping, so it may never touch the physical pages
  that Bonsai uses.
- It loads the memory from the processor only. The crashes happen with the cameras, the
  Harp devices, the network and the disk all moving data at once.
- Two modules per channel on DDR5 stresses the memory controller in the processor. This
  test does not reproduce that load.
- **It tests the DIMMs.** The WHEA errors in phase A point at the core, not the DIMMs.
  The clean pass and the WHEA errors do not contradict each other. They examined
  different hardware.

## Phase C: page heap cannot run on this rig

Run on 16 October 2026. `gflags` is not installed on the rig, and it writes only two
registry values, so we set them directly:

```
HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\Bonsai.exe
    GlobalFlag      0x02000000     FLG_HEAP_PAGE_ALLOCS
    PageHeapFlags   0x3 full, then 0x8 light
```

| Mode | Frame rate |
|---|---|
| Full page heap, `0x3` | **20 fps**, against 50 expected |
| Light page heap, `0x8` | **20 fps** |
| Removed | **50 fps** |

Removing the key restored 50 fps, so page heap is certainly the cause.

**Why it is too expensive.** Thirteen cameras at 50 frames per second allocate and free
about **650 multi-megabyte buffers every second**. Page heap touches every one of them.
Aiming it at the OpenCV libraries alone would not help, because OpenCV allocates those
buffers.

**What we measured while it ran.** Memory was not the problem.

| Item | Value |
|---|---|
| Bonsai private memory | 4984 MB, against about 2000 MB normally |
| Free memory | 93.3 GB of 128 GB |
| Threads | 287 |
| Handles | 204,629 |

Sampled again three minutes later: 4970 MB, 283 threads, 204,620 handles. **Flat.** So
there is no handle leak and no memory growth.

It also ran for more than two minutes under full page heap without a fault. Full page
heap faults at the instant of an out of bounds write, so nothing wrote past the end of a
native block in that window. The fault is rare, not continuous.

**The registry key was removed.** Nothing needs undoing on the rig.

## Phase D: the encoder was replaced, and the crash followed

Branch `aa-dev/ffmpeg-all-cameras` moves every camera from the OpenCV `cv:VideoWriter`
to an external `ffmpeg` process. The CPU cameras use `libx264` at `-preset fast -crf 16`.
The four looming cameras use `h264_nvenc`. This removes OpenCV from the Bonsai process
completely.

ffmpeg then crashed on 14 and 15 October.

### The session of 14 October proves the workflow was healthy

Session `2026-10-14T172425Z`, 1 hour 15 minutes, from 17:24:25 to 18:39:47 UTC.

| Check | Result |
|---|---|
| Chunk rotation | Chunk 1 ends at 17:59:59.990720. Chunk 2 starts at 18:00:00.000704. All 13 streams. |
| Frame rate | Exactly 50.00 fps, and 100.00 fps on the patch cameras. No dropped frames. |
| Video against log | Every finalised AVI holds exactly as many frames as its CSV holds rows. |
| Encoder backlog | None. |
| Files finalised | 12 of 13. **Only `CameraTop` is unfinalised.** |

ffmpeg patches the RIFF size and writes an `idx1` index only when it closes a file
properly. `CameraTop_2026-10-14T18-00-00.avi` still holds the placeholder size
`0xFFFFFFFF`, has no index, and its last frame is cut 1718 bytes short. **That process
died in the middle of a write.** It wrote 119,186 frames of the 119,362 that Bonsai
sent.

So the chunk rotation, the `rx:Switch` teardown and the encoders are all correct. The
workflow stopped because one ffmpeg process died under it.

### What this phase shows

We have now changed the control panel, the Harp firmware and the whole encoder. The
encoder change replaced the library, the codec and the process boundary at once. The
crash followed every change.

## Phase E: the processor

| Item | Value |
|---|---|
| Processor | 13th Gen Intel Core i9-13900K |
| Microcode | **`0x11D`** |

Intel published three microcode updates for this part:

| Microcode | Date | What it changed |
|---|---|---|
| `0x125` | June 2024 | the eTVB boost algorithm |
| `0x129` | August 2024 | caps voltage requests above 1.55 V |
| **`0x12B`** | **September 2024** | **the Vmin shift root cause** |

**The rig had none of them.** The published defect is a microcode algorithm that
requests too much core voltage, which degrades the silicon permanently and
progressively. Intel extended the warranty on boxed 13th and 14th Gen desktop
processors to five years.

The i9-13900K is the top-bin part and is among the most affected.

**This is a fact about the machine, not a diagnosis.** It is consistent with the crash
record, the increasing crash rate and the two core machine checks in February and March.
It is not evidence that the processor causes our crashes.

### The board confirms the whole history

The GIGABYTE Z790 AORUS MASTER rev 1.0 BIOS list states each step in its own words.

| Version | Date | Release note |
|---|---|---|
| **F11** | 19 Sep 2023 | "Follow Intel to upgrade Microcode CPUID ver. **0x11D**" |
| F12b | about Nov 2023 | the installed BIOS. A beta between F11 and F12, still on `0x11D` |
| **F15** | 27 Sep 2024 | "Introduce the **Intel Default Settings** and enabled as default" and "Introduce microcode **0x12B** as the final mitigation of 13/14th Gen desktop processor Vmin Shift Instability issue" |
| F16f | 27 May 2025 | microcode **0x12F**, "further improves system conditions that can potentially contribute to Vmin Shift Instability" |
| **F20** | 29 Sep 2026 | `M32B0671_00000137`, microcode **0x137** |

GIGABYTE also records what Intel fixed and when:

> 0x12B encompasses 0x125 and 0x129 microcode updates, and addresses elevated voltage
> requests by the processor during idle and/or light activity periods.
> microcode 0x129 (August 2024) addresses high voltages requested by the processor.
> microcode 0x125 (June 2024) addresses eTVB algorithm issue.

The rig ran the `0x11D` generation for three years. It missed every mitigation.

### Corrected on 16 October 2026

```
BIOS       F12b  ->  F20 (29 September 2026)
microcode  0x11D ->  0x137
```

`M32B0671` decodes to CPUID family 6, model `0xB7`, stepping 1, which is this exact
processor.

Settings applied at the same time:

| Setting | Value | Reason |
|---|---|---|
| Intel Default Settings | **Performance** | 253 W with a 307 A current limit. The board previously ran with no limit, so the P-cores held 5.0 to 5.2 GHz under full AVX2 load |
| XMP | **Disabled** | four modules, two kits. It also keeps an overclock out of the warranty claim |
| Memory multiplier | Auto, **DDR5-4800** at 1.10 V | the JEDEC profile, not XMP |
| AC BACK | Always On | the rig returns by itself after a power cut |

The flash needed one recovery. The first attempt left the machine dead, and the board
reset itself to defaults. DualBIOS did its job.

**Verified after a reboot on 16 October 2026.** Every setting survived.

```
F20   microcode 0x137   Win32PrioritySeparation=24   CpuPriorityClass=6   High performance
```

Read it again at any time with:

```powershell
'{0}  microcode 0x{1:X}  Win32PrioritySeparation={2}  CpuPriorityClass={3}' -f `
  (Get-CimInstance Win32_BIOS).SMBIOSBIOSVersion,
  [BitConverter]::ToUInt32((Get-ItemProperty 'HKLM:\HARDWARE\DESCRIPTION\System\CentralProcessor\0').'Update Revision',4),
  (Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl').Win32PrioritySeparation,
  (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\Bonsai.exe\PerfOptions').CpuPriorityClass
powercfg /getactivescheme
```

**What this does and does not do.** It stops further degradation. It does not repair
damage already done. Phase F now decides whether the processor is usable.

## Windows settings changed on 16 October 2026

**This is not the crash.** It is a separate problem, recorded here so that a later
comparison of runs knows what changed and when.

### The symptom

The camera lag grows while Bonsai is not the foreground window, or while the operator
does something else such as listing a folder. **The lag recovers when Bonsai regains
focus.** The behaviour predates the BIOS update, so the power limit is not the cause.

### The cause

Windows demotes a background application in two ways, and both reverse with focus.

1. The default scheduling policy gives the foreground window a priority boost and a
   longer time slice.
2. The processor is hybrid, with 8 P-cores and 16 much slower E-cores. Windows pushes
   background threads onto the E-cores. Intel Thread Director guides this properly only
   on Windows 11. **This rig runs Windows 10 LTSC 21H2**, which handles it crudely.

### The change

```powershell
Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl' `
  -Name Win32PrioritySeparation -Value 24
powercfg /setactive SCHEME_MIN
powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN 100
powercfg /setactive SCHEME_CURRENT
```

| Setting | From | To | Effect |
|---|---|---|---|
| `Win32PrioritySeparation` | `0x26`, Programs | **24, Background services** | long fixed time slices, no foreground boost |
| Power plan | Balanced | **High performance** | no core parking |
| Minimum processor state | varies | **100%** | the clock does not drop when the load looks idle |

Writing `Win32PrioritySeparation` to the registry does **not** apply it to the running
kernel. Either reboot, or apply it through `SystemPropertiesPerformance.exe`, Advanced
tab, Processor scheduling.

### Bonsai at Above Normal priority: the change that worked

Set Bonsai to **AboveNormal**, which is priority 10 against Normal's 8.

```powershell
Get-Process Bonsai | ForEach-Object { $_.PriorityClass = 'AboveNormal' }
```

**Do not use High.** We tried it first and it hangs the workflow.

Bonsai writes raw frames into named pipes and the ffmpeg processes read them. Windows
does not pass a High priority class to child processes, so a High parent creates a
**Normal** child:

```
Bonsai   High    priority 13
ffmpeg   Normal  priority 8    <- cannot get the processor
```

Bonsai then starves the very processes it waits on. The pipe fills, the write blocks, and
the workflow hangs. The camera lag also grew continuously, with the window in focus or
not. Above Normal keeps Bonsai ahead of the background demotion without starving
anything.

**Do not raise ffmpeg to match.** The encoders are replaced at every hourly chunk
rotation, so any priority set by hand disappears within the hour.

### What this measured

`CameraNorth_Lag`, in seconds. The last 3000 frames of a run, which is about one minute.

| | 14 Oct 2026, before any change | 16 Oct 2026, after |
|---|---|---|
| median | 0.028 s | **0.018 s** |
| p95 | 0.062 s | **0.043 s** |
| max | 0.263 s | **0.093 s** |

About 35% lower. The 253 W power limit from Intel Default Settings cost nothing
measurable, even though the processor no longer reaches 5.2 GHz.

### Making it persist

Setting the priority with `Get-Process` applies to one process instance only, so every
restart of Bonsai returns to Normal. Set it by executable name instead. **This works, and
it survives a reboot.**

From an **administrator** PowerShell, once:

```powershell
$k = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\Bonsai.exe\PerfOptions'
New-Item -Path $k -Force | Out-Null
New-ItemProperty -Path $k -Name CpuPriorityClass -Value 6 -PropertyType DWord -Force | Out-Null
(Get-ItemProperty $k).CpuPriorityClass
```

`CpuPriorityClass` values: 1 Idle, 2 Normal, 3 High, 4 RealTime, 5 Below Normal,
**6 Above Normal**.

The shell must be elevated, or the command writes nothing and reports no error. It only
affects processes started after the change, so restart Bonsai to pick it up.

To remove it:

```powershell
Remove-Item 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\Bonsai.exe\PerfOptions' -Recurse -Force
```

This is the same `Image File Execution Options` key that phase C used for page heap. Only
`PerfOptions` is touched.

Confirm with:

```powershell
Get-Process Bonsai, ffmpeg | Select-Object Name, Id, PriorityClass,
  @{n='MB';e={[int]($_.WS/1MB)}} | Sort-Object Name, Id | Format-Table -AutoSize
```

Bonsai runs as two processes. The real one holds about 2 GB and 300 threads. Both should
read `AboveNormal`, and **every ffmpeg should read `Normal`**.

### The rig has about 20% of throughput margin

The lag answers this directly. Read in 30-second steps, the 14 October session shows a
single disturbance, not a warm-up.

```
sec   0 to 210    0.030 s     healthy
sec       240     1.487       something starts
sec       270     8.111
sec       300    13.856
sec       330    20.140       peak
sec       360    16.960       draining
sec       390     6.050
sec       420     0.043       recovered
sec     450 on    0.030 s     healthy for the next 35 minutes
```

The run was fine for four minutes, lost ground for about ninety seconds, then caught up.

**These episodes happen at any point in a run, not near the start.** The operator sees
them at different moments, and they follow activity on the machine rather than elapsed
time. An earlier note in this document called this a warm-up and blamed the .NET thread
pool. Both were wrong.

**What the slope measures.** The lag grew about 20 seconds over 90 seconds, so the
pipeline ran at roughly 78% of real time during the episode. It then drained the same
backlog at about the same rate. The rig therefore holds **about 20% of spare
throughput**, and anything that takes more than that makes the lag climb.

That matches the processor measurement. In steady state the machine runs at 78 to 84%
with about six cores idle, Bonsai taking 12.5 cores and the ten encoders 9.8 between
them.

**The behaviour is benign.** It always recovers, and the pipeline buffers rather than
drops. Every AVI frame count in that session equals its CSV row count, straight through
the 23-second episode.

### Lag is queued frames, and memory measures it

Every minidump records the **peak** working set as well as the current one. The gap
between them is the largest queue that run ever held.

| Dump | current WS | **peak WS** | excursion | implied worst lag |
|---|---|---|---|---|
| 11 dumps, Jul to 13 Oct | 1.1 to 1.5 GB | 1.7 to 3.1 GB | 0.5 to 1.6 GB | 2 to 5 s |
| **13160, 15 Oct 19:57** | 1.35 GB | **7.56 GB** | **6.21 GB** | **about 21 s** |

The arithmetic is direct. Four tracking cameras at 1440 x 1080 monochrome and 50 frames
per second hold 1.48 MB per frame, so one second of lag queues 0.29 GB across the four.

```
 5 s of lag  ->  1.45 GB
10 s of lag  ->  2.90 GB
20 s of lag  ->  5.79 GB
23 s of lag  ->  6.66 GB
```

The 15 October dump shows a **6.21 GB** excursion, against **6.66 GB** predicted for the
23-second episode measured in the lag log. That is within 7%.

**So the lag number and the memory number are the same thing.** The frames wait in
memory, and the peak working set records how deep the queue became.

**This gives a free diagnostic.** The peak working set of any dump tells you the worst
lag episode of that run, with no lag log needed. It also shows that every run so far has
had multi-second episodes, not only the ones we watched.

**It also has a cost.** A six gigabyte excursion is six gigabytes of image buffers
allocated and released, which means heavy garbage collector and large object heap churn.
On a machine with a suspected memory fault, that is more exposure, not less.

**Worth running.** `Sample.cmd` in this folder logs private bytes and working set once a
minute to `C:\CrashDumps\bonsai_mem.csv`. It was not running during these sessions. With
it on, every lag episode appears as a memory spike, continuously and without a crash.

**What consumes the margin.** Anything else that runs on the machine.

- Interactive use. Opening windows reproduces it, and a Remote Desktop session costs more
  again.
- Background scanning. `WithSecure` runs on this rig, and the recording path takes about
  6 GB per hour of video writes. Real-time scanning of that is expensive.

**Recommended.** Exclude the recording folders and the two executables from real-time
scanning, and leave the rig alone while it records.

**What was done on 16 October 2026.** The client is F-Secure Client Security Premium
15.30, running in **standalone mode**, so its settings are local and no IT request is
needed. The client also reports `Check for updates failed: Cannot connect to server`, so
its definitions are stale and cannot refresh.

**Real-time scanning was switched off** for the overnight run. A permanent exclusion
list is the better answer, and it is available locally.

Two things follow for anyone reading that run.

1. The run is **not comparable** with earlier runs on scanning alone.
2. The F-Secure option is *turn off temporarily*. It restores itself after a period or
   at a restart. If it came back during the night, the logs carry a step change at that
   moment, and the run has two different conditions in it. Check the client in the
   morning and record which.

Scheduled scanning was already off, so a periodic scan is not a factor either way.

### What we changed to buy margin back

**The control panel now updates at 20 Hz.** `ControlPanel.bonsai` gained nine
`rx:SampleInterval` nodes, each set to `PT0.05S`. They cover the image displays, the
heatmap, the tracking events and the four rolling graphs.

The visualizers previously updated at the camera rate, which is 50 fps on the looming
cameras and 100 fps on the patch cameras. **They run on the user interface thread**,
which is a single thread. It therefore sets a limit however idle the rest of the machine
is, and it matched the symptom better than anything else we measured: six processor cores
idle, the graphics processor at 13%, and the lag still climbing.

ZedGraph rolling graphs are expensive, so the four of them were the largest part of that
load.

**Still available if more margin is needed**, in order of cost:

1. `-preset veryfast` in place of `fast` on the CPU cameras. It roughly halves the
   encoder cost.
2. An explicit `-threads` limit per encoder. Each ffmpeg takes up to 48 threads by
   default, so ten of them put about 540 threads on 32 logical processors.
3. Move more cameras to `h264_nvenc`. `CameraTop` is the most expensive at 2.56 cores,
   and the encoder block on the graphics processor sits at 10%.

All three change the workflow and need a test run.

### Monitoring from 16 October 2026

Three records on one timeline, all at the same 5-second interval.

| Record | Written by | Holds |
|---|---|---|
| `CameraNorth_Lag` and the other `_Lag` streams | Bonsai, per frame | the delay, in seconds |
| `C:\CrashDumps\bonsai_mem.csv` | `BonsaiCrashDiag.ps1 -Sample -IntervalSec 5` | private bytes, working set, handles, threads |
| HWiNFO64 sensor log | HWiNFO64, logging to CSV | core VID, package power, core clocks, temperatures, throttle flags |

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File "C:\ProjectAeon\aeon_exp_htsloom\diagnostics\BonsaiCrashDiag.ps1" -Sample -IntervalSec 5
```

**The default 60 seconds is too coarse.** A lag episode ramps over about 90 seconds, so
60 seconds gives three points. Five seconds gives eighteen, which shows the slope. The
cost is about 17,000 rows and 1 MB per day.

**Switch off SMBus and SPD access in HWiNFO**, under Settings, Safety. Its low-level
polling is linked to instability on some boards, and this is a stability test. The memory
module sensors are not needed.

The HWiNFO log answers a question nothing else can: whether microcode `0x137` and the
253 W limit actually changed the voltage the processor requests.

Nothing else needs a logger. Windows records WHEA errors and bugchecks by itself, and
crash dumps are registered for `Bonsai.exe` and `ffmpeg.exe`.

### What it does not affect

The data. The 14 October session logged exactly 50.00 and 100.00 fps, and every AVI frame
count equals its CSV row count. The pipeline **buffers** the delay rather than dropping
frames, so this lag costs memory, not samples.

### Still available if the lag returns

Each ffmpeg defaults to about 1.5 threads per logical core, so up to 48 threads each.
Thirteen encoders therefore create hundreds of runnable threads on 32 logical
processors. An explicit `-threads` limit, or moving more cameras to `h264_nvenc`, would
cut the contention. Both change the workflow, so both need testing.

## Tests that closed other explanations

### Memory exhaustion is ruled out

Every minidump carries a `SystemMemoryInfo` stream that records the system memory state
at the instant of the crash. We read all fourteen.

| Item | Range across all crashes |
|---|---|
| Installed memory | 128 GB |
| Commit limit | 146.8 GB |
| Committed | 24.7 to 61.4 GB |
| Available | 72.5 to 108.5 GB |
| **Free commit headroom** | **85.4 to 122.1 GB** |

The ffmpeg process that crashed on 15 October held 0.20 GB private and 0.16 GB working
set.

So the null write in ffmpeg is **not** a failed allocation caused by memory pressure.
No allocation on this machine had any reason to fail.

### A single flipped bit is ruled out, for the 15 October fault

A single-bit memory error in a stored code pointer would move the target a power of two
away from a valid address. We tested all 64 single-bit changes of `0x37F2F9D52068`
against the process memory map in the dump.

Executable memory in that process was 281.3 MB, of 1916.3 MB committed. That is a very
small target, so the test is sharp.

**No single-bit change lands in an executable region.**

The READ faults do produce hits, but the address space around them is densely committed,
so those hits are chance and prove nothing.

**This does not clear the processor.** A degraded core more often produces a wrong
computed result or a stale cache line. Either gives arbitrary garbage, which is what the
table shows.

## Two buffer lifetime hypotheses, both closed

The idea was that an image buffer is released while handles to it are still held, on the
producer side. It is the right class of fault, and the raw Bonsai type really is unsafe
in that way, but this rig is protected.

- `Bonsai.Spinnaker.ImageEventListener` **deep copies** the pixels with `CV.Copy` into a
  new `IplImage`, notifies the observer, and only then calls `image.Release()`.
- `Bonsai.Spinnaker.SpinnakerChunkData` holds a **reference** to `ManagedChunkData` and
  reads `FrameID` and `Timestamp` lazily. After `Release()` those reads would touch freed
  memory. **Any workflow that puts an `ObserveOn` after the raw
  `Bonsai.Spinnaker.SpinnakerCapture` has a real use after free.** This is worth
  reporting upstream.
- This rig does not do that. `Aeon.Video.SpinnakerCapture.Generate` reads those two
  values on the acquisition thread, before the release, and stores them in
  `VideoChunkData`, which is a **struct of plain longs**.
- The garbage collector cannot free an `IplImage` during a native call either.
  `IplImage` derives from `SafeHandle`, so the marshaller holds a reference across every
  P/Invoke.

## Phase F: ffmpeg alone

This test was designed to answer one question. Does anything outside our own pipeline
fail on this machine? The kernel bugcheck records answered that question first, so the
test now has a smaller job. **It measures whether the fault is reproducible on demand.**
That matters for the rig schedule, and a warranty claim is stronger with it.

Run ffmpeg alone. No Bonsai, no cameras, no pipe. Same binary, same settings, synthetic
input.

```powershell
1..9 | ForEach-Object {
  Start-Process -NoNewWindow -FilePath 'C:\ProjectAeon\aeon_exp_htsloom\src\Extensions\ffmpeg.exe' `
    -ArgumentList '-nostats','-loglevel','warning',
                  '-f','lavfi','-i','testsrc2=size=1440x1080:rate=50',
                  '-t','86400','-vf','format=yuv420p',
                  '-c:v','libx264','-preset','fast','-crf','16','-f','null','-'
}
```

Nine encoders at the `CameraTop` resolution and settings. They write nothing to disk.
This matches the production CPU load without touching our code.

Register crash dumps for ffmpeg first, so a crash records itself:

```powershell
$k = 'HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\ffmpeg.exe'
New-Item -Path $k -Force | Out-Null
New-ItemProperty -Path $k -Name DumpFolder -Value 'C:\CrashDumps' -PropertyType ExpandString -Force | Out-Null
New-ItemProperty -Path $k -Name DumpType   -Value 2 -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path $k -Name DumpCount  -Value 3 -PropertyType DWord -Force | Out-Null
```

Then read the result:

```powershell
Get-ChildItem C:\CrashDumps -Filter ffmpeg*.dmp
Get-WinEvent -LogName Application -MaxEvents 50 | Where-Object Id -eq 1000
```

| Outcome | What it means |
|---|---|
| ffmpeg crashes | The fault reproduces on demand, on a workload we did not write. |
| No crash in 24 hours | The fault is rare at this load. It does not clear the machine. |

**Run it for hours, not minutes.** The dumps record how long Bonsai ran before each
crash. The times come from the `MiscInfo` stream of each dump.

```
2 h 25 m   2 h 49 m   3 h 17 m   3 h 51 m   4 h 23 m   4 h 52 m
6 h 17 m   7 h 28 m   8 h 46 m   24 h 10 m   26 h 46 m   64 h 16 m
```

The median is near four hours, and the spread is wide. A quiet ten minutes proves
nothing. Leave the test overnight.

A heartbeat file gives the survival time even when the machine stops without a dump:

```powershell
"START $(Get-Date -Format o)" | Out-File C:\CrashDumps\stress.log -Append
while ($true) {
  "ALIVE $(Get-Date -Format o)" | Out-File C:\CrashDumps\stress.log -Append
  Start-Sleep 20
}
```

This test is better than Prime95 here. It is a realistic load, not an AVX torture test,
so it adds no unusual wear, and it isolates one variable.

## The microcode fix: DONE on 16 October 2026

See **Corrected on 16 October 2026** under phase E. The rig now runs BIOS F20 with
microcode `0x137` and Intel Default Settings at the Performance profile.

Check the state at any time with:

```powershell
Get-CimInstance Win32_BIOS | Select-Object SMBIOSBIOSVersion, ReleaseDate
$b = (Get-ItemProperty 'HKLM:\HARDWARE\DESCRIPTION\System\CentralProcessor\0').'Update Revision'
'microcode 0x{0:X}' -f [BitConverter]::ToUInt32($b,4)
```

**Open the Intel warranty claim.** The evidence is complete: three kernel bugchecks, two
of them processor machine checks, two corrected WHEA entries, ten unexpected shutdowns
and fourteen application crashes. The warranty on boxed 13th Gen desktop processors runs
five years. The claim is stronger because the memory never ran XMP.

**If crashes continue with correct microcode**, the silicon is already damaged and the
processor must be replaced. Before concluding that, try two single-variable steps:

1. Intel Default Settings, **Baseline** profile. 125 W against 253 W. It may cost frame
   rate, so check the cameras hold 50 and 100 fps.
2. Memory multiplier down one step, to 4000 and then 3600. The reset moved the memory
   from about DDR5-4000 up to DDR5-4800, so that changed at the same time as the
   microcode.

## Other work still open

**Phase G, needs a reboot and a person at the machine.** MemTest86 from a USB stick. It
tests all 128 GB, including the memory that Windows holds. Run four passes or more.

**If a test finds an error.** The rig has 4 slots. Bisect with two modules at a time.

**Page heap on a reduced workflow** stays possible. With two cameras instead of thirteen
the frame rate would hold. The risk is that fewer cameras may not reproduce a fault that
takes hours at full load.

**Single-variable software tests, if the machine is cleared.**

1. **Per-camera writer instances.** Branch `aa-dev/single-instance-encoders` replaces one
   dynamic `SelectMany` over camera groups with explicit per-camera blocks.
2. **Revert Bonsai.Core and Bonsai.Vision to 2.9.0.** Those moved from 2.9.0 to 2.9.1 on
   13 July 2026, two weeks before the first crash. `OpenCV.Net` stayed at 3.4.2 and
   `Aeon.Video` at 0.7.0 throughout.

## Correction to the earlier analysis

In September I concluded that the execute at zero fault came from the control panel,
which subscribed once per wheel sample, and that the runtime failed to resolve a method
pointer under that load. PR #60 removed that work. The fault returned on 5, 13 and 15
October with the same caller offset.

A corrupted pointer explains the fault better, and it explains the other sites as well.
The per-element subscribe was real and worth removing. It was not the cause.

**Keep hypotheses out of the findings.** That mistake is why this document separates what
we measured from what we believe.
