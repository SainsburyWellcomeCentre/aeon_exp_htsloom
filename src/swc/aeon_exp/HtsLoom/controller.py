from enum import StrEnum
from datetime import timedelta
from typing import Dict
from pydantic import Field
from swc.aeon.schema import BaseSchema
from swc.aeon_exp.htsloom.rig import FeederName


class PatchBehavior(StrEnum):
    REWARD = "Reward"
    INACTIVE = "Inactive"


class PatchRewardRule(BaseSchema):
    inactivity_timeout: float = Field(
        description="The amount of time, in seconds, after which the travelled distance will be reset if there is no motion."
    )
    distance_threshold: float = Field(
        description="The distance which must be travelled on the wheel to receive the next reward."
    )
    next_state: str = Field(
        description="The state which will be loaded after the next reward is delivered."
    )


class PatchLed(BaseSchema):
    active: bool = Field(
        default=False, description="Indicates whether the LED should turn ON whenever the wheel is active?"
    )
    off_delay: timedelta = Field(default=timedelta(0), description="The time to turn off LED after reward.")


class PatchState(BaseSchema):
    behavior: PatchBehavior = Field(
        default=PatchBehavior.REWARD, description="The running state of the patch."
    )
    reward_rule: PatchRewardRule = Field(description="Specifies the reward rule for the patch.")
    led: PatchLed = Field(description="Specifies the LED rule for the patch.")


class ForagingState(BaseSchema):
    patch_states: Dict[FeederName, PatchState] = Field(
        description="Specifies the state of every foraging patch in the habitat."
    )


class ForagingController(BaseSchema):
    start_state: str = Field(description="The name of the start state.")
    states: Dict[str, ForagingState] = Field(description="The set of all possible foraging states.")
