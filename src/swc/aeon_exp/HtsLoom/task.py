import math
from datetime import timedelta
from enum import StrEnum
from pathlib import Path
from typing import Dict, List
from swc.aeon.io.reader import Csv
from swc.aeon.schema import BaseSchema, Dataset, data_reader
from swc.aeon_exp.htsloom.rig import CameraName, Point
from pydantic import Field
from pydantic.json_schema import SkipJsonSchema

class ScreenName(StrEnum):
    NORTH = "ScreenNorth"
    SOUTH = "ScreenSouth"
    EAST = "ScreenEast"
    WEST = "ScreenWest"

class EventName(StrEnum):
    NONE = "None"
    STIMULUS_ENDED = "StimulusEnded"
    KEY_1_EVENT = "Key1Event"
    KEY_2_EVENT = "Key2Event"
    KEY_3_EVENT = "Key3Event"
    KEY_4_EVENT = "Key4Event"
    TASK_STIM_START = "TaskStimStart"
    TASK_STIM_STOP = "TaskStimStop"
    TASK_REWARD = "TaskReward"
    ZONE_TRIGGER_1 = "ZoneTrigger1"
    ZONE_TRIGGER_2 = "ZoneTrigger2"
    ZONE_TRIGGER_3 = "ZoneTrigger3"
    ZONE_TRIGGER_4 = "ZoneTrigger4"
    ZONE_TRIGGER_5 = "ZoneTrigger5"

class ScreenPosition(BaseSchema):
    x: float = Field(default=0.5, ge=0, le=1, description="The X coordinate of the point")
    y: float = Field(default=0.5, ge=0, le=1, description="The Y coordinate of the point")

class ZoneTrigger(BaseSchema):
    camera: CameraName = Field(default=CameraName.NORTH, description="Camera to be used")
    zone_id: int = Field(description= "The index of the region to be used to trigger the loom")
    trigger_probability: float = Field(description= "Probability to present a loom if all conditions are achieved")
    angle_threshold: float = Field(ge= 0, le = math.pi, description= "Angle threshold that subject needs to be facing looming region")
    refractory_period: float = Field( description= "Minimum period of inactivity after a loom is presented in seconds")
    time_in_region: float = Field(description= "Minimum Period subject needs to be in loom region for a loom to be triggered")
    trigger: set[EventName] = Field(description= "The triggers that will be set when all conditions are met")

# class TriggerMapping(BaseSchema):
#     trigger:Dict[ScreenName, List]

class StimulusSettings(BaseSchema):
    stop_triggers: set[EventName] = Field(default={EventName.KEY_1_EVENT}, description= "The triggers that interrupt the loom presentation")
    start_triggers: set[EventName] = Field(default={EventName.KEY_1_EVENT}, description= "The triggers that stat the loom presentation")
    initial_delay: float = Field(description= "The delay before starting to present the stimulus")
    pulse_duration: float = Field(default= 10, description= "The time period that a stimulus is presented if it doesn't finishes by itself")
    inter_pulse_interval: float = Field(description= "Period of time between pulses when running a set of pulses in sequence")
    number_of_pulses: int = Field(description= "The quantity of stimulus to be presented each time it starts")
    # TODO: in the initial proposal there is a duration parameter that I don't fully understand why is needed
    # TODO: Revisit nomenculature for pulse people go to electricity and ttls

class LoomingPresentationParameters(StimulusSettings):
    location: ScreenPosition = Field(description= "The screen coordinates of the loom")
    start_size: float = Field(ge=0, description= "The initial size of the looming disc")
    end_size: float = Field(ge=0, description= "The end size of the looming disc")
    animation_duration: float = Field(default= 10, description= "The time period that each loom takes to reach full size")
    time_on_set: float = Field(default=0 , description= "The period that looming disc remains static on set after reaching the end size")
    looming_color: float = Field(default = 0.5, description= "The grayscale value of the looming disc to be presented")


class FeederABCTransition(BaseSchema):
    target_state: str = Field(description="The name of the target meta-state.")
    activation_interval: timedelta = Field(
        default=timedelta(0),
        description="The time from meta-state start until this transition is activated.",
    )
    activation_rewards: int = Field(
        default=0,
        description="The minimum number of delivered rewards from meta-state start for this transition to be activated.",
    )
    activation_event: EventName = Field(
        default=EventName.NONE,
        description="A manual event which must be triggered for this transition to be activated.",
    )
    activation_transitions: int = Field(
        default=0,
        description="The minimum number of foraging state transitions from meta-state start for this transition to be activated.",
    )


class FeederABCMetaState(BaseSchema):
    state_file: Path = Field(
        description="The path to the YML or JSON file describing the foraging state controller to use in this meta-state."
    )
    transitions: List[FeederABCTransition] = Field(description="The set of transitions from this meta-state.")


class FeederABCStateLog(Dataset):
    """Meta-state machine event log for the feeder ABC controller.

    On disk: ``StateLog/StateLog_{MetaState,States}_Events_*.csv``.
    """

    @data_reader
    def feeder_abc_metastate_events(self, pattern):
        return Csv(
            f"{pattern}_MetaState_Events_*",
            columns=[
                "target_state",
                "activation_interval",
                "activation_rewards",
                "activation_transitions",
                "activation_event",
            ],
        )

    @data_reader
    def state_events(self, pattern):
        return Csv(
            f"{pattern}_States_Events_*",
            columns=["feeder_abc_meta_state", "feeder_abc_next_state"],
        )


class FeederABCMetaController(Dataset):
    start_state: str = Field(description="The name of the starting meta-state.")
    meta_states: Dict[str, FeederABCMetaState] = Field(description="The set of meta-states in this meta controller.")
    state_log: SkipJsonSchema[FeederABCStateLog] = Field(default=FeederABCStateLog(), exclude=True)

    def _join_pattern_prefix(self, pattern_prefix: str) -> str:
        """Pass-through: the feeder meta-controller writes no data of its own.

        The ``state_log`` child lives directly in a top-level ``StateLog/``
        folder, so we preserve the accumulated prefix unchanged.
        """
        return pattern_prefix


class Task(BaseSchema):
    zone_triggers : List[ZoneTrigger] = Field(description="The zones that trigger events to be used by task control")
    looms: Dict[ScreenName, Dict[str ,LoomingPresentationParameters]] = Field(description="Dictionary with screen Id as a key for a dict of loom regions")
    feeder_task: FeederABCMetaController = Field(description="The feeder ABC task")

    def _join_pattern_prefix(self, pattern_prefix: str) -> str:
        """Pass-through: task-level data lives at session root, not under ``Task/``."""
        return pattern_prefix
