from enum import StrEnum
from typing import Dict, List, Optional, Union
from swc.aeon_rigs.base import BaseSchema, Device
from swc.aeon_rigs.video import SpinnakerCamera
from swc.aeon_rigs.foraging import UndergroundFeeder
from swc.aeon_rigs.harp import HarpTimestampGeneratorGen3, HarpCameraControllerGen2
from pydantic import Field 

class Point(BaseSchema):
    x: int = Field(default=0, description="The X coordinate of the point")
    y: int = Field(default=0, description="The Y coordinate of the point")

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
    filter_window: int =Field(default=40, description="Sliding window size of the weight linear regression filter.")
    weight_baseline_refactory_period : float = Field(default=5, description="The time between consecutive weight baseline when subject in center of arena in seconds.")

class ActivityTracking(BaseSchema):
    threshold : int = Field(default=100, description="Threshold for the blob tracking.")
    regions: List[List[Point]] = Field(description="Region for the tracking.")

class InArena(BaseSchema):
    center: Point = Field(description="The centerPoint of the arena in camera coordinates.")
    radius: int = Field(description="The radius of the arena in image coordinates.")

class Tracking(BaseSchema):
    blob_tracking : Dict[str, ActivityTracking] = Field(description="The subject tracking in the arena.") 

class Camera(SpinnakerCamera):
    trigger: TriggerName = Field(default=TriggerName.TRIGGER0, description="The name of the trigger.")
    camera_tracking: Tracking | None =  Field(default=None, description="Tracking Parameters.")

class ActivityCenter(ActivityTracking):
    camera: CameraName = Field(description="Activity center camera")

class LightCycle(BaseSchema):
    commandSocket: str = Field(default=">tcp://localhost:4304", description="Specifies the endpoint to send commands to the Light Server.")
    eventSocket: str = Field(default=">tcp://localhost:4303",description="Specifies the endpoint to send commands to the Light Server.")
    roomName: str = Field(default="Aeon3", description="The name of the room to monitor and control.")
    configFileName: str = Field(default="lightcycle.config", description="The name of the CSV file describing the light model, where each row represents one whole minute and the red, cold white and warm white, light levels set for that minute.")

class Rig(BaseSchema):
    clock_synchronizer: HarpTimestampGeneratorGen3
    camera_synchronizer: CameraController
    cameras: Dict[CameraName, Camera]
    feeders: Dict[FeederName,UndergroundFeeder]
    nest: Dict[str, WeightScale] = Field(description="Weight scale parameters.")
    activityCenter: ActivityCenter = Field(description="Activity in the center of the arena.")
    lightCycle: LightCycle = Field(description="LightCycle components for the arena.")
