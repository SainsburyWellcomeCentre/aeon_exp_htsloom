# Documentation

These docs explain how htsloom works and how to configure an experiment. They cover the
concepts and how the parts fit together. Field-level details are in the schemas: open any
YAML file in `../src/` and hover a key in VS Code. The `# yaml-language-server: $schema=`
header at the top of each file gives you validation and autocomplete.

## Setting up an experiment

An experiment uses two config files: one for the hardware and one for the protocol.

1. **[RigSetup.md](RigSetup.md)** describes the hardware file, `HtsLoomRig.yaml`: the Harp
   synchronisation chain, the cameras with their tracking and zones, the feeders (COM ports,
   wheel radius), the nests and the light cycle. Edit this file when the hardware changes.
2. **[TaskSetup.md](TaskSetup.md)** describes the protocol file, `HtsLoomTask.yaml`: how
   animal position and key presses raise events (`zoneTriggers`), which looms those events
   show on each screen, and the feeder reward task. Edit this file to change the experiment.
   - **[FeederTask.md](FeederTask.md)** is a focused guide to the `feederTask` section of
     `HtsLoomTask.yaml`. It is not a separate file. It explains the feeder reward state
     machine: the two-level meta-controller, how to design rules, and how to shape
     behaviour (hold steady, win-switch, escalation).

Both files share one event bus. Events have names such as `ZoneTrigger1` and `Key1Event`.
Zones and keys raise events; looms and the feeder task react to them.

## Examples

- **[examples/](examples/)** has feeder rule files you can copy (`Escalation.yaml`,
  `RewardSwitch.yaml`), with diagrams. The patterns come from `aeon_exp_foragingABC`
  (Adrian Roggenbach), reformatted to the htsloom schema. See that folder's README for
  attribution.

## Deeper dives

- **[ABCDStatePlayer.md](ABCDStatePlayer.md)** is a technical description of the Bonsai
  state player itself: its subjects, the `Defer`/replay loop, and how meta-states and rule
  files are structured. Read it when you need to change the workflow, not just configure it.

## Diagnostics

- **[../diagnostics/README.md](../diagnostics/README.md)** explains how to capture rig
  crashes: how Windows writes a full-memory `.dmp` file when `Bonsai.exe` crashes, how to
  set that up on the rig, how to run the memory sampler, and what to collect for a
  post-mortem.

## See also

- **Config files** in `../src/`: `HtsLoomRig.yaml` (hardware), `HtsLoomTask.yaml` (task,
  including the `feederTask` meta-controller) and `Rule*.yaml` (feeder rules). Each has a
  `$schema` header so the editor validates it as you type.
- **Reference repos** that use the same patterns and have more examples:
  `aeon_exp_foragingABC` (the improved state player, with worked rule and meta examples in
  its own `docs/`) and `phields_exp_prototype0` (how the player fits into a loom-style task).

> Keep this index up to date when you add or move a doc.
