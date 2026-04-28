import swc.aeon.io.reader as _reader
from swc.aeon.schema import core as _stream
from swc.aeon.schema.streams import Device, Stream, StreamGroup
from dotmap import DotMap



class Photodiode(Stream):
    """Photodiode data from an InputExpander (4 inputs: pd0..pd3)."""

    def __init__(self, pattern):
        super().__init__(_reader.Harp(f"{pattern}_90_*", columns=["pd0", "pd1", "pd2", "pd3"]))

class HeadTail(Stream):
    """Head-tail tracking data logged at HARP register 201.

    Columns (all float32):
        id                          : subject / blob index
        centroid_x, centroid_y      : body centroid in image pixels
        head_x, head_y              : head position
        tail_x, tail_y              : tail position
        velocity_x, velocity_y      : instantaneous velocity (pixels/frame)
        heading                     : heading angle in radians
        blob_centroid_x/y           : connected-component centroid
        orientation                 : blob major-axis orientation (radians)
        major_axis_length           : blob major axis (pixels)
        minor_axis_length           : blob minor axis (pixels)
        area                        : blob area (pixels²)
    """

    def __init__(self, pattern):
        super().__init__(_reader.Harp(f"{pattern}_201_*", columns=[
            "id",
            "centroid_x", "centroid_y",
            "head_x", "head_y",
            "tail_x", "tail_y",
            "velocity_x", "velocity_y",
            "heading",
            "blob_centroid_x", "blob_centroid_y",
            "orientation", "major_axis_length", "minor_axis_length", "area",
        ]))

class LoomRegionState(Stream):

    def __init__(self, pattern):
        super().__init__(_reader.Harp(f"{pattern}_202_*", columns=[
            "blob_id", "zone_id", "in_zone"
        ]))

class LoomAngleState(Stream):

    def __init__(self, pattern):
        super().__init__(_reader.Harp(f"{pattern}_203_*", columns=[
            "blob_id", "zone_id", "angle"
        ]))

class BeamBreak(Stream):

    def __init__(self, pattern):
        super().__init__(_reader.BitmaskEvent(f"{pattern}_32_*", 0x22, "PelletDetected"))

class DeliverPellet(Stream):
    """Pellet-delivery command events from an underground feeder (register 35)."""

    def __init__(self, pattern):
        super().__init__(_reader.BitmaskEvent(f"{pattern}_35_*", 0x01, "TriggerPellet"))


class TrackingCamera(StreamGroup):
    """Video metadata + HeadTail tracking (used for CameraTop)."""

    def __init__(self, pattern):
        super().__init__(pattern, _stream.Video, HeadTail, LoomRegionState, LoomAngleState)


class Feeder(StreamGroup):
    """Beam-break and pellet-delivery events for one underground feeder."""

    def __init__(self, pattern):
        super().__init__(pattern, BeamBreak, DeliverPellet)

# Dataset schema
# Device names must match the LogName parameter used in the Bonsai workflow.

htsloom = DotMap([
    Device("Metadata",           _stream.Metadata),
    # Top-down tracking camera
    Device("CameraTop",          TrackingCamera),
    # Side cameras facing looming screens
    Device("CameraNorth",        TrackingCamera),
    Device("CameraSouth",        TrackingCamera),
    Device("CameraEast",         TrackingCamera),
    Device("CameraWest",         TrackingCamera),
    # Nest and patch cameras (video only)
    Device("CameraNest",         _stream.Video),
    Device("CameraLightMonitor", _stream.Video),
    Device("CameraPatch1",       _stream.Video),
    Device("CameraPatch2",       _stream.Video),
    Device("CameraPatch3",       _stream.Video),
    Device("CameraPatch4",       _stream.Video),
    Device("CameraPatch5",       _stream.Video),
    Device("CameraPatch6",       _stream.Video),
    # Underground feeders
    Device("Feeder1",            Feeder),
    Device("Feeder2",            Feeder),
    Device("Feeder3",            Feeder),
    Device("Feeder4",            Feeder),
    Device("Feeder5",            Feeder),
    Device("Feeder6",            Feeder),
    Device("Environment",        _stream.Environment, _stream.MessageLog),
    Device("InputExpander",      Photodiode)
])