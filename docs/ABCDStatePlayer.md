# ABCDStatePlayer

> Reference notes on the `ABCDStatePlayer` Bonsai pattern, reconstructed from the
> sibling AEON foraging experiments. **This node is not currently part of
> `aeon_exp_htsloom`** — it lives in the foraging task-logic workflows. These notes
> are kept here for reference because the looming task uses the same underlying
> idea (a data-driven state machine) through its own primitives.

## What it is

`ABCDStatePlayer` is a **data-driven state-machine "player"** implemented as a
Bonsai `GroupWorkflow`. Rather than hard-coding task phases, it loads a set of
named states from a configuration file and then "plays" them one after another,
running each state's reactive logic until a transition condition fires and moves
to the next state. The "ABCD" in the name refers to the labelled states
(A → B → C → D …) the player steps through.

It appears inlined (as a `GroupWorkflow`, not a shared `.bonsai` file) inside the
task logic of:

- `aeon_exp_foragingABC/src/Extensions/TaskLogic.bonsai`
- `aeon_exp_OVC/src/Extensions/TaskLogic.bonsai`
- `phields_exp_prototype0/src/Extensions/FeedersTasklogic.bonsai`

## Structure

The group exposes a few internal subjects and a recursive player loop:

| Subject | Type | Role |
| --- | --- | --- |
| `States` | `Dictionary<string, ForagingState>` | All available states, keyed by name. Loaded from config. |
| `CurrentState` | `string` | Name of the state currently playing. Seeded from `StartState`. |
| `InStateWheelDisplacement` | grouped, timestamped `double` per feeder | Per-state input stream(s) the active state consumes. |
| `InStateWheelDeltaDisplacement` | grouped, timestamped `double` per feeder | As above, delta form. |
| `InStateResetWheelDisplacement` | grouped, timestamped `double` per feeder | As above, reset form. |

### How a run proceeds

1. **Load the rule set.** A `ReadAllText` reads a YAML rule file
   (e.g. `Rule1_AllActive.yaml`), `DeserializeFromYaml` parses it into a
   `ForagingController`, and its `States` member is pushed into the `States`
   subject. The controller's `StartState` seeds `CurrentState`.
2. **Play the current state.** A `Defer` / `CreateObservable` block
   (`StatePlayer` → `PlayState`) looks up the active state by name in the
   `States` dictionary and subscribes to that state's reactive logic
   (wheel displacement integration, reward rules, gain, reset triggers, etc.).
3. **Wait, then transition.** The state runs until its transition condition is
   met — a `StateSwitchDelay` (externalised property) and/or in-state events.
   The next state name is written back into `CurrentState`.
4. **Loop.** Because the player is built from `Defer` over `CurrentState`, writing
   a new value re-subscribes the player on the next state, so the machine
   advances A → B → C → D and so on until the run ends.

The transition/condition logic in the foraging version is task-specific
(feeders, wheel-displacement thresholds such as `MinimumDistThreshold`,
`InactivityTimeout`, reward rules). The reusable idea is the **outer shell**:
*load named states from config → play current state → on a condition, swap
`CurrentState` → repeat.*

## Two levels of state: metastate file vs. rule/state file

The configuration is a **nested, two-level state machine**, and the two file
types are easy to confuse. They are validated by two different JSON schemas
(`MetaController.json` and `ForagingController.json`) and play different roles:

| | **Metastate file** (outer) | **Rule / state file** (inner) |
| --- | --- | --- |
| Example | `ABC-MetaController.yaml` | `Rule1_AllActive.yaml`, `Rule2_WinSwitch.yaml`, … |
| Schema / type | `MetaController` → `MetaState` | `ForagingController` → `ForagingState` |
| Top-level keys | `startState`, `metaStates` | `startState`, `states` |
| A single entry is… | a **meta-state**: a pointer to one rule file **plus** the conditions to move to the next meta-state | a **foraging state**: the per-feeder behaviour the animal actually experiences |
| Drives transitions by | manual key event, elapsed time, reward count, or number of inner state transitions | the animal's behaviour (wheel distance / inactivity), via each patch's `nextState` |
| Time scale | whole blocks of a session (minutes to hours, e.g. `activationInterval: 06:00:00`) | moment-to-moment within a block |

### Metastate file (the "rule switcher")

This is the **high-level schedule** — which rule is in force and when to swap to
another. Each meta-state names a `stateFile` (a rule file) and a list of
`transitions`:

```yaml
# ABC-MetaController.yaml      (schema: MetaController.json)
startState: State_1
metaStates:
  State_1:
    stateFile: Rule1_AllActive.yaml      # <-- points to an inner rule/state file
    transitions:
      - targetState: State_14
        activationEvent: Key1Event        # manual keypress
      - targetState: State_2
        activationEvent: Key2Event
      - targetState: State_2
        activationTransitions: 5000       # after 5000 inner state transitions
  State_10:
    stateFile: Rule10_ABC_524.yaml
    transitions:
      - targetState: State_11
        activationInterval: 06:00:00      # after 6 h in this meta-state
```

A `Transition` (schema `Transition`) can fire on any of:

- `activationEvent` — a manual `EventName` (e.g. `Key1Event`, `Key2Event`).
- `activationInterval` — elapsed time since the meta-state started (ISO-8601 duration).
- `activationRewards` — number of rewards delivered since meta-state start.
- `activationTransitions` — minimum number of *inner* foraging-state transitions since meta-state start.

### Rule / state file (the "foraging controller")

This is the **low-level task** that `ABCDStatePlayer` actually plays. It is a
`ForagingController`: a `states` dictionary where each `ForagingState` defines,
per feeder, the reward behaviour and which state to advance to next:

```yaml
# Rule1_AllActive.yaml         (schema: ForagingController.json)
startState: AllActive_State1
states:
  AllActive_State1:
    patchStates:
      Feeder1:
        behavior: Reward
        rewardRule:
          inactivityTimeout: 0.0
          distanceThreshold: 50.0
          nextState: AllActive_State2   # <-- inner transition, driven by behaviour
        led:
          active: false
      # … Feeder2–Feeder6 …
```

### How the two connect at runtime

The player watches `CurrentMetaState`, reads its `Item1.StateFile`, loads **that
rule file**, deserializes it to a `ForagingController`, and pushes its `states`
into the `States` subject (seeded from the rule's own `StartState`). So:

```
MetaController (metastate file)        ← outer machine: which rule, and when to switch
  └─ MetaState "State_1"
        stateFile: Rule1_AllActive.yaml
        └─ ForagingController (rule/state file)   ← inner machine: ABCDStatePlayer plays this
             ├─ ForagingState "AllActive_State1"
             └─ ForagingState "AllActive_State2"
```

In short: **the metastate file decides *which rule* is active and the high-level
schedule for changing rules; the rule/state file defines *the actual foraging
states* the player steps through while that rule is active.** `ABCDStatePlayer`
is the inner player; the meta-controller sits above it and reloads it with a new
rule file whenever a meta-state transition fires.

> Terminology note: the sketch in
> `aeon_exp_foragingABC/docs/example_sequence_of_three/` calls the rule files
> "Level 2" and the rule-switching/meta-controller definition "Level 3". That
> maps directly onto the implemented `ForagingController` (rule/state) and
> `MetaController` (metastate) files respectively.

### Externalised properties

- `StateSwitchDelay` — minimum dwell time before a state may hand over.
- `MinimumDistThreshold`, `Gain` — foraging-specific tuning knobs surfaced by the
  inner per-feeder logic.

## Relationship to `aeon_exp_htsloom`

The looming task does not include `ABCDStatePlayer`, but it implements the same
"named state, loaded from config, logged on change" concept with its own
extensions:

- `src/Extensions/LoadState.bonsai` / `SaveState.bonsai` — persist and restore task state.
- `src/Extensions/LogLoomAngleState.bonsai` / `LogLoomRegionState.bonsai` — log state transitions.
- `src/Extensions/TaskControl.bonsai` — task-level control flow.

If a multi-phase looming protocol ever needs the same A→B→C→D sequencing, the
`ABCDStatePlayer` shell (config-driven `States` dictionary + `CurrentState`
subject + `Defer`-based replay) is the pattern to copy from the foraging task
logic and adapt to loom states.

## Source of these notes

Reconstructed by reading
`aeon_exp_foragingABC/src/Extensions/TaskLogic.bonsai` (the `ABCDStatePlayer`
`GroupWorkflow`, starting around line 54). No prose documentation for this node
exists anywhere under `C:\Git`; the workflow definition is the only source.
