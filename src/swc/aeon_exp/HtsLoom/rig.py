from enum import StrEnum
from typing import Literal, Dict, List, Optional, Union
from typing_extensions import Annotated, TypeAliasType
from swc.aeon_rigs.base import BaseSchema, Device
from swc.aeon_rigs.video import SpinnakerCamera
from swc.aeon_rigs.foraging import UndergroundFeeder
from swc.aeon_rigs.harp import HarpTimestampGeneratorGen3, HarpCameraControllerGen2
from pydantic import Field

class CameraName(StrEnum):
    NORTH = "CameraNorth"
    SOUTH = "CameraSouth"
    TOP = "CameraTop"
    EAST = "CameraEast"
    WEST = "CameraWest"
    PATCH1 = "CameraPatch1"
    PATCH2 = "CameraPatch2"
    PATCH3 = "CameraPatch3"
    PATCH4 = "CameraPatch4"
    PATCH5 = "CameraPatch5"
    PATCH6 = "CameraPatch6"
    LIGHT = "CameraLightMonitor"
    NEST = "CameraNest"

class FeederName(StrEnum):
    FEEDER1 = "Feeder1"
    FEEDER2 = "Feeder2"
    FEEDER3 = "Feeder3"
    FEEDER4 = "Feeder4"
    FEEDER5 = "Feeder5"
    FEEDER6 = "Feeder6"

class TriggerName(StrEnum):
    TRIGGER0 = "Trigger0"
    TRIGGER1 = "Trigger1"

class CameraControllerTrigger(BaseSchema):
    frequency: int = Field(
        default=50,
        description="The frequency of the camera TTL trigger.",
    )

class CameraController(HarpCameraControllerGen2):
    triggers: Dict[TriggerName,CameraControllerTrigger]

class WeightScale(Device):
    port_name: str = Field(examples=["COM"], description="The name of the device serial port.")
    filter_window: int = Field(default=40, description="Sliding window size of the weight linear regression filter.")
    weight_baseline_refactory_period : float = Field(default=5, description="The time between consecutive weight baseline when subject in center of arena in seconds.")

class Point(BaseSchema):
    x: int = Field(default=0, description="The X coordinate of the point")
    y: int = Field(default=0, description="The Y coordinate of the point")

class InArena(BaseSchema):
    center: Point = Field(description="The centerPoint of the arena in camera coordinates.")
    radius: int = Field(description="The radius of the arena in image coordinates.")

class Polygon(BaseSchema):
    """A polygon is defined by list of points connected linearly in sequence"""
    points: List[Point] = Field(default=[Point(x=0,y=0)], description="Points to make the polygon")

class RegionsTrackingParameters(BaseSchema):
    threshold : int = Field(default=100, description="Threshold for the blob tracking.")
    regions: List[Polygon] = Field(description="Regions for the tracking.")

class BlobTracking(BaseSchema):
    tracking_type: Literal["BlobTracking"] = "BlobTracking"
    regionTracking : Dict[str, RegionsTrackingParameters] = Field(description="The subject tracking in the arena.")

class ZoneActivity(BaseSchema):
    position : Point = Field(default=Point(x=0,y=0), description="Zone position")
    regions: List[Polygon] = Field(description="Regions for the Activity.")

class HeadTailTracking(BlobTracking):
    tracking_type: Literal["HeadTailTracking"] = "HeadTailTracking"
    velocity_threshold : int = Field(default = 10, description = "Velocity threshold, in pixels, used to infer direction of travel and therefore the head of the subject") # TODO: Update to generic (mm) vs camera (pixels) units
    buffer_length : int = Field(default = 10, description = "The length of the buffer history, in frames, on which to compute velocity")
    Zones: List[ZoneActivity] | None = Field(default=None, description="ZonesOfInterest")

Tracking = TypeAliasType ('Tracking', Annotated[Union[HeadTailTracking, BlobTracking], Field(discriminator="tracking_type")])

class Camera(SpinnakerCamera):
    trigger: TriggerName = Field(default=TriggerName.TRIGGER0, description="The name of the trigger.")
    # tracking:  Annotated[Union[HeadTailTracking, Tracking], Field(discriminator="tracking_type")] | None = 
    tracking: Tracking | None =  Field(default=None, description="Tracking Parameters.") 
    zones: List[ZoneActivity] | None = Field(default=None, description="ZonesOfInterest")


class LightCycle(BaseSchema):
    command_socket: str = Field(default=">tcp://localhost:4304", description="Specifies the endpoint to send commands to the Light Server.")
    event_socket: str = Field(default=">tcp://localhost:4303",description="Specifies the endpoint to send commands to the Light Server.")
    room_name: str = Field(default="Aeon3", description="The name of the room to monitor and control.")
    config_file_name: str = Field(default="lightcycle.config", description="The name of the CSV file describing the light model, where each row represents one whole minute and the red, cold white and warm white, light levels set for that minute.")

class Rig(BaseSchema):
    clock_synchronizer: HarpTimestampGeneratorGen3
    camera_synchronizer: CameraController
    cameras: Dict[CameraName, Camera]
    feeders: Dict[FeederName,UndergroundFeeder]
    nests: Dict[str, WeightScale] = Field(default=None,description="Weight scale parameters.")
    # activity_center: ActivityCenter = Field(description="Activity in the center of the arena.")
    light_cycle: LightCycle = Field(description="LightCycle components for the arena.")
    # head_tail_parameters: Dict[CameraName, HeadTailParameters] = Field(default = None, description="HeadTail parameters per camera")
