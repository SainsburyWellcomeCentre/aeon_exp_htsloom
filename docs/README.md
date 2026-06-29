# Documentation

How the htsloom internals work and how to configure an experiment. These docs cover
**concepts and how the pieces fit together** — *field-level* details live in the
schemas: open any YAML in `../src/` and hover a key in VS Code (the
`# yaml-language-server: $schema=` header wires up validation + autocomplete).

## Setting up an experiment

There are **two config files** — *hardware* and *protocol*:

1. **[RigSetup.md](RigSetup.md)** — the **hardware** (`HtsLoomRig.yaml`): the Harp
   synchronisation chain, cameras + tracking/zones, feeders (COM ports, wheel radius),
   nests, light cycle. Edit when hardware changes.
2. **[TaskSetup.md](TaskSetup.md)** — the **protocol** (`HtsLoomTask.yaml`): how animal
   position/keys raise events (`zoneTriggers`), what looms those events present per
   screen, and the feeder reward task. Edit to change the experiment.
   - **[FeederTask.md](FeederTask.md)** — a focused guide to the **`feederTask` section
     of that same `HtsLoomTask.yaml`** (not a separate file): the feeder reward state
     machine — two-level meta-controller → rule design, and how to shape behaviour
     (hold-steady / win-switch / escalation).

The through-line across both files is the shared **event bus** (`EventName`s like
`ZoneTrigger1`, `Key1Event`): zones and keys raise events; looms and the feeder task
react to them.

## Examples

- **[examples/](examples/)** — ready-to-copy feeder rule files (`Escalation.yaml`,
  `RewardSwitch.yaml`) with diagrams. Patterns adapted from `aeon_exp_foragingABC`
  (Adrian Roggenbach) and reformatted to the htsloom schema; see the folder's README
  for attribution.

## Deeper dives

- **[ABCDStatePlayer.md](ABCDStatePlayer.md)** — technical reconstruction of the
  underlying Bonsai state-player (subjects, the `Defer`/replay loop, metastate-vs-rule
  file structure). Read this when you need to understand or **modify the workflow
  itself**, not just configure it.

## See also

- **Config files** in `../src/`: `HtsLoomRig.yaml` (hardware), `HtsLoomTask.yaml`
  (task + `feederTask` meta-controller), `Rule*.yaml` (feeder rules). Each has a
  `$schema` header for live editor validation.
- **Reference repos** (same patterns, more examples):
  `aeon_exp_foragingABC` (improved state-player + worked rule/meta examples under its
  own `docs/`) and `phields_exp_prototype0` (how the player integrates into a
  loom-style task).

> Keep this index updated when you add or move a doc.
