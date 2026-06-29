# Setting up the task — `HtsLoomTask.yaml`

`HtsLoomTask.yaml` describes the **experiment**: when stimuli fire, what they look
like, and the feeder reward task. It's loaded as `TaskConfiguration` and is what you
edit to change the *protocol* (as opposed to the hardware, which lives in
`HtsLoomRig.yaml`).

As always, per-field meanings are in the schema hover-hints
(`$schema=HtsLoom.json#/$defs/Task`); this doc is the map and the mental model.

## Three parts

```
zoneTriggers:   # WHEN to raise an event (animal position → event)
looms:          # WHAT stimulus to present, per screen (event → loom)
feederTask:     # the feeder reward state machine  (see FeederTask.md)
```

The first two are wired together by an **event bus**, and understanding that bus is
the whole game.

## Events are the glue

Nothing calls a loom directly. Instead, things **raise named events** (`EventName`s
like `ZoneTrigger1`, `Key1Event`, `TaskReward`), and other things **listen** for them.
Two sources raise events:

- **`zoneTriggers`** — raise events from *animal behaviour* (being in a zone, facing
  it, long enough…);
- **keys** — raise events from the *keyboard* (`Key1Event`…`Key4Event`, mapped in
  `InputKeys.bonsai`) for manual control.

And **looms listen**: each loom's `startTriggers`/`stopTriggers` are sets of those
same event names. So the chain is:

```
animal in zone  ──zoneTrigger──▶  ZoneTrigger1  ──┐
keypress        ───────────────▶  Key1Event     ──┼──▶ loom whose startTriggers contains it  ──▶ present
```

Get the **event names to line up** across `zoneTriggers.trigger`, a loom's
`startTriggers`/`stopTriggers`, and the keys, and the protocol wires itself.

## `zoneTriggers` — turning position into events

A list of conditions. Each entry says *"when an animal is here, behaving like this,
raise these events"*:

- `camera` + `zoneId` — **which zone**: the `zoneId`-th zone polygon defined on that
  camera in `HtsLoomRig.yaml` (so the rig defines *where*, the task defines *what
  happens there*);
- `angleThreshold` — the animal must be **facing** the zone within this angle (radians);
- `timeInRegion` — minimum **dwell** before it counts;
- `refractoryPeriod` — minimum gap between successive triggers (seconds), so one visit
  doesn't fire repeatedly;
- `triggerProbability` — random gating (0–1): even when all conditions hold, fire only
  this fraction of the time;
- `trigger` — the set of **events to raise** when all of the above pass.

> Read one entry as a sentence: *"on `camera`, in zone `zoneId`, facing within
> `angleThreshold`, for `timeInRegion`s, not within `refractoryPeriod` of the last one,
> with probability `triggerProbability` → raise `trigger`."*

## `looms` — what to present, per screen

Keyed by **`ScreenName`** (`ScreenNorth/South/East/West`), each holding a set of named
loom definitions. A loom is an expanding (looming) disc; its parameters fall into
three groups:

- **When** — `startTriggers` / `stopTriggers` (the events that start/interrupt it),
  `initialDelay`, `pulseDuration`, `interPulseInterval`, `numberOfPulses`;
- **How it grows** — `startSize` → `endSize`, `animationDuration` (time to expand),
  `timeOnSet` (how long it holds at full size);
- **Look** — `location` (`x`,`y` on the screen, 0–1), `loomingColor` (greyscale 0–1).

You can define **many looms per screen** with different names and triggers — e.g. a
behaviour-driven one started by `ZoneTrigger1` and a manual one started by `Key1Event`
— and they coexist; which plays depends on which event fires.

## `feederTask` — the reward state machine

The feeder reward task lives here too, but it's a topic of its own — see
**[FeederTask.md](FeederTask.md)** for the meta-controller → rule design. In short:
`feederTask` is the schedule of which feeder rule is active and when to switch.

## Setup checklist

- [ ] Every event in a loom's `startTriggers`/`stopTriggers` is actually **raised**
      somewhere (a `zoneTriggers.trigger` or a key) — an event nothing raises is a loom
      that never starts.
- [ ] `zoneTriggers` `camera` + `zoneId` reference a **zone that exists** on that camera
      in `HtsLoomRig.yaml`.
- [ ] Screen names and event names are values from the schema enums (`ScreenName`,
      `EventName`) — the editor flags typos.
- [ ] `feederTask` has at least one transition per meta-state (see FeederTask.md — a
      transition-less meta-state ends immediately).

## Worked example

*"When a mouse enters the East zone facing it, fire a loom on the East screen"* — the
event bus end to end (`feederTask` omitted here; see FeederTask.md and
[examples/](examples/)):

```yaml
# yaml-language-server: $schema=HtsLoom.json#/$defs/Task
zoneTriggers:
  - camera: CameraEast
    zoneId: 0                  # the 0th zone defined on CameraEast in HtsLoomRig.yaml
    angleThreshold: 1.5        # mouse must face the zone within ~1.5 rad
    timeInRegion: 0.5          # ...for at least 0.5 s
    refractoryPeriod: 300      # ...and not within 5 min of the last trigger
    triggerProbability: 0.2    # ...then, 20% of the time:
    trigger: [ ZoneTrigger1 ]  # raise this event

looms:
  ScreenEast:
    Loom200:
      startTriggers: [ ZoneTrigger1, Key1Event ]  # starts on the zone event OR a keypress
      stopTriggers:  [ Key1Event ]                 # a key interrupts it
      initialDelay: 0
      numberOfPulses: 1
      pulseDuration: 10
      interPulseInterval: 0.5
      location: { x: 0, y: 0 }                     # screen position (0-1)
      startSize: 0.1
      endSize: 2                                   # expands from 0.1 -> 2
      animationDuration: 0.34                      # over 0.34 s
      timeOnSet: 0.25                              # holds 0.25 s at full size
      loomingColor: 0                              # black disc (greyscale 0-1)
```

Trace the bus: the `zoneTriggers` entry raises **`ZoneTrigger1`** when its conditions
pass; `Loom200`'s `startTriggers` contains `ZoneTrigger1`, so it presents. Because
`Key1Event` is also in `startTriggers`, you can fire the same loom manually — and the
`camera: CameraEast, zoneId: 0` line only resolves if `CameraEast` actually defines a
zone 0 in the rig.

