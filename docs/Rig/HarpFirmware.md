# Harp firmware for the HTS loom rig

Every board must run **Harp Core 1.15**. This is the current core firmware library,
`harp-tech/core.atxmega`, released on 21 October 2025. The device reports it as
`CoreVersion` in the Device Setup dialog.

Do not confuse this number with `harp-tech/protocol` v1.13.0. That is the written Harp
standard document. It uses a separate count.

## Installed versions

The operator flashed every device that the experiment uses on 5 and 6 October 2026.

All seven devices are verified. The operator read them in Device Setup on 6 October 2026,
at 09:33 to 09:39.

| COM | Role | Device | Firmware | Core | Hardware | Verified |
|---|---|---|---|---|---|---|
| COM5 | clockSynchronizer | ClockSynchronizer | 1.1 | **1.15** | 1.0 | 6 Oct 2026 |
| COM6 | cameraSynchronizer | CameraControllerGen2 | 1.2 | **1.15** | 1.2 | 6 Oct 2026 |
| COM11 | inputExpander | InputExpander | 2.4 | **1.15** | 1.2 | 6 Oct 2026 |
| COM20 | Feeder1 | OutputExpander | 2.3 | **1.15** | 1.2 (1) | 6 Oct 2026 |
| COM4 | Feeder2 | OutputExpander | 2.3 | **1.15** | 1.2 (1) | 6 Oct 2026 |
| COM3 | Feeder3 | OutputExpander | 2.3 | **1.15** | 1.2 (1) | 6 Oct 2026 |
| COM16 | Feeder4 | OutputExpander | 2.3 | **1.15** | 1.2 (1) | 6 Oct 2026 |

**Every device runs core 1.15.** The four feeders run the same firmware, 2.3, and the same
core. Feeder1 on COM20 ran two core versions behind the other three before this work. It
now matches them.

(1) **The hardware version changed in the report.** On 5 October all four feeder boards
reported hardware 1.0. After the flash they report hardware 1.2. The boards did not
change. The Harp firmware image sets this register, so the hardware 1.2 build makes the
device report 1.2. Treat the field as a property of the firmware, not as proof of the
board. Use the hardware 1.2 file for these boards from now on.

## Devices that were not flashed

| COM | Device | Reason |
|---|---|---|
| COM8 | SoundCard | **Disconnected from the rig.** The workflow never used it. No core 1.15 build exists. See `harp-tech/device.soundcard#30`. Flash it before you connect it again. |
| COM15, COM21 | RfidReader | The workflow does not use them. Flash them before you connect them. |
| COM7 | nest board | `HtsLoomRig.yaml` declares `nests.Nest` on this port, but no workflow opens it. `Extensions/Devices.bonsai` includes no weight scale, and no workflow reads the `nests` field. |

## Downloads

Flash each file with the **Bootloader >>** button in Device Setup. The `hw` part of the
file name must equal the HardwareVersion of the device.

| Device | Hardware | File |
|---|---|---|
| ClockSynchronizer | 1.0 | [fw1.1-harp1.15](https://github.com/harp-tech/device.clocksynchronizer/releases/download/fw1.1-harp1.15/ClockSynchronizer-fw1.1-harp1.15-hw1.0-ass0.hex) |
| CameraControllerGen2 | 1.2 | [fw1.2-harp1.15](https://github.com/harp-tech/device.cameracontrollergen2/releases/download/fw1.2-harp1.15/CameraControllerGen2-fw1.2-harp1.15-hw1.2-ass0.hex) |
| InputExpander | 1.2 | [fw2.4-harp1.15](https://github.com/harp-tech/device.inputexpander/releases/download/fw2.4-harp1.15/InputExpander-fw2.4-harp1.15-hw1.2-ass0.hex) |
| OutputExpander | 1.2 build, see note | [fw2.3-harp1.15](https://github.com/harp-tech/device.outputexpander/releases/download/fw2.3-harp1.15/OutputExpander-fw2.3-harp1.15-hw1.2-ass0.hex) |
| SoundCard | 1.1 | [fw2.2-harp1.13](https://github.com/harp-tech/device.soundcard/releases/download/fw2.2-harp1.13/SoundCard-fw2.2-harp1.13-hw1.1-ass0.hex) |
| RfidReader | 1.2 | [fw1.4-harp1.15](https://github.com/harp-tech/device.rfidreader/releases/download/fw1.4-harp1.15/RfidReader-fw1.4-harp1.15-hw1.2-ass0.hex) |

**OutputExpander note.** The four feeder boards reported hardware 1.0 before the flash.
The only published build is for hardware 1.2. That build is approved for these boards, and
the operator used it on 5 and 6 October 2026. The devices now report hardware 1.2.
Filipe Carvalho (`filcarv`), of the Open Ephys Production Site, gave the approval.

Release pages, if you need a different hardware version:

- https://github.com/harp-tech/device.clocksynchronizer/releases
- https://github.com/harp-tech/device.cameracontrollergen2/releases
- https://github.com/harp-tech/device.inputexpander/releases
- https://github.com/harp-tech/device.outputexpander/releases
- https://github.com/harp-tech/device.soundcard/releases
- https://github.com/harp-tech/device.rfidreader/releases

## Order, for the next time

1. Flash the four feeders.
2. Flash the CameraControllerGen2.
3. Flash the ClockSynchronizer last. It gives the clock to the whole rig.
4. Confirm each new version in Device Setup before you start the next device.
5. Do not start an experiment until you verify every device.

Issue #61 holds the version of every device before this work, and the Device Setup
screenshots from 5 October 2026.
