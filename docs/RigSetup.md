# Setting up the rig — `HtsLoomRig.yaml`

`HtsLoomRig.yaml` describes the **physical rig**: what hardware exists and the
parameters each device runs with. It's loaded once at startup as `RigConfiguration`
and is the thing you edit when hardware changes (a new camera serial, a feeder on a
different COM port, a new light schedule).

Field-by-field meanings are in the schema — hover any key in VS Code (the
`$schema=HtsLoom.json#/$defs/Rig` header). This doc is the *map* of the file and the
*things that actually trip people up*.

## The shape of the file

```
clockSynchronizer:   # the master Harp clock (timestamp source for everything)
inputExpander:       # Harp GPIO board (e.g. photodiode / digital inputs)
cameraSynchronizer:  # Harp board that emits the camera TTL triggers
cameras:             # every camera, keyed by name
feeders:             # every feeder, keyed by name
nests:               # weight scale(s)
lightCycle:          # the light-server connection + schedule
```

## The synchronisation chain (read this first)

Everything is time-locked through Harp hardware, and it's worth holding the chain in
your head:

- **`clockSynchronizer`** is the **master clock** — a Harp timestamp generator. Every
  device's data is stamped against it, which is what lets you align video, tracking,
  feeders and looms after the fact.
- **`cameraSynchronizer`** emits the **TTL pulses that fire the cameras**. Its
  `triggers` define named pulse groups (`Trigger0`, `Trigger1`) each with a
  `frequency` (Hz). Cameras don't free-run — each one names which trigger group drives
  it, so all cameras on the same trigger are frame-synchronous.
- **`inputExpander`** collects digital inputs (e.g. the photodiode signal used to
  verify loom onset).

The practical upshot: a camera's frame rate comes from its **trigger group's
frequency**, not from the camera block itself.

## Cameras

Keyed by **`CameraName`** (`CameraTop`, `CameraNest`, `CameraNorth/South/East/West`,
`CameraPatch1…6`, …). Two kinds of settings:

- **Imaging** — `serialNumber` (the FLIR Spinnaker serial; this is how the rig finds
  the physical camera), `exposureTime`, `gain`, `binning`, and `trigger` (which TTL
  group above).
- **Tracking (optional)** — only the cameras that track an animal carry a `tracking`
  block:
  - `threshold` — blob-detection brightness threshold;
  - `regions` — polygons (lists of points) bounding where to look;
  - head-tail tracking adds a `velocityThreshold` (used to infer which end is the head);
  - `zones` — polygon regions of interest the **task** references (e.g. the area that
    arms a loom). Zones defined here are what `HtsLoomTask.yaml`'s `zoneTriggers`
    point at by index.

> Mental split: **imaging settings make a good picture; tracking settings turn that
> picture into a mouse position; zones turn a position into task events.** A patch
> camera that's only for monitoring needs imaging only — no `tracking`/`zones`.

## Feeders

Keyed by **`FeederName`** (`Feeder1…4`, currently **one per screen**). Each:

- `portName` — the **COM port** of that feeder's Harp board (e.g. `COM20`). This is
  the most common thing to get wrong after re-cabling.
- `wheelRadius` — converts wheel rotation to linear distance (the unit the feeder
  task's `distanceThreshold` is measured in). **Sign encodes direction** — a negative
  radius flips which way counts as "forward."
- `pelletDeliveryTimeout` / `pelletDeliveryRetryCount` — how long to wait for delivery
  confirmation and how many times to retry.

The feeder *names* here are the contract with the task: a rule's `patchStates` and the
control panel both address feeders by these exact names.

## Nests & light cycle

- **`nests`** — weight scale(s), each with its Harp port and a
  `weightBaselineRefractoryPeriod` (how often to re-baseline weight while the subject
  is centred).
- **`lightCycle`** — connection to the Light Server: `commandSocket` / `eventSocket`
  (TCP endpoints), `roomName`, and `configFileName` (the `lightcycle.config` CSV, one
  row per minute giving red / cold-white / warm-white levels). The light *schedule*
  lives in that CSV, not here.

## Setup checklist

- [ ] Camera **serial numbers** match the physically installed cameras.
- [ ] Each camera's `trigger` points at a `cameraSynchronizer` group that exists, at
      the frame rate you want.
- [ ] Feeder **COM ports** match the wiring; `wheelRadius` sign gives "forward."
- [ ] Camera **names** and **feeder names** are values from the schema enums
      (`CameraName` / `FeederName`) — typos here fail validation in the editor.
- [ ] Tracking cameras have `tracking` + `zones`; monitor-only cameras don't.
- [ ] `lightCycle` sockets/room match the Light Server, and `lightcycle.config` exists.

## Worked example

An illustrative slice with one of each device kind. Region polygons are trimmed to a
few points for readability — real ones have many more (see the actual `HtsLoomRig.yaml`).

```yaml
# yaml-language-server: $schema=HtsLoom.json#/$defs/Rig
lightCycle:
  commandSocket: '>tcp://172.24.158.103:4304'   # Light Server endpoints
  eventSocket:   '>tcp://172.24.158.103:4303'
  roomName: AEON4
  configFileName: lightcycle.config             # per-minute light levels (CSV)

clockSynchronizer: { portName: COM5 }            # master Harp clock
inputExpander:     { portName: COM11 }           # photodiode / digital inputs
cameraSynchronizer:
  portName: COM6
  triggers:
    Trigger0: { frequency: 50 }                  # 50 Hz camera group
    Trigger1: { frequency: 100 }                 # 100 Hz camera group

cameras:
  CameraNorth:                                   # a TRACKING camera
    serialNumber: "23106245"                     # FLIR Spinnaker serial (finds the device)
    binning: 1
    exposureTime: 3000
    gain: 6
    trigger: Trigger0                            # frame rate comes from this group (50 Hz)
    tracking:
      trackingType: HeadTailTracking
      regionTracking:
        North: { threshold: 100, regions: [ { points: [ {x: 141, y: 355}, {x: 912, y: 139}, {x: 627, y: 910} ] } ] }
    zones:                                       # the task references these by index (zoneId)
      - position: { x: 741, y: 140 }
        regions: [ { points: [ {x: 243, y: 387}, {x: 972, y: 218}, {x: 1121, y: 600} ] } ]
  CameraPatch1:                                  # a MONITOR-ONLY camera (no tracking/zones)
    serialNumber: "23032823"
    binning: 2
    exposureTime: 3000
    gain: 0
    trigger: Trigger1

feeders:
  Feeder1: { portName: COM20, wheelRadius: -4, pelletDeliveryRetryCount: 2, pelletDeliveryTimeout: 1 }

nests:
  Nest: { portName: COM7, filterWindow: 40, weightBaselineRefractoryPeriod: 5 }
```

Note the two camera styles: `CameraNorth` tracks (so it has `tracking` + a `zones`
entry the task can arm looms from), while `CameraPatch1` is just a feed (imaging only).
That one `zones` entry is **zone 0 on `CameraNorth`** — exactly what a
`zoneTriggers` entry with `camera: CameraNorth, zoneId: 0` points at in the task.

