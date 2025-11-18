from enum import StrEnum
import json
from pathlib import Path
from pydantic import Field
from swc.aeon_rigs.base import BaseSchema


class FeederCommand(StrEnum):
    DELIVER_PELLET = "DeliverPellet"
    RESET_FEEDER = "ResetFeeder"

class CreateFeederCommand(BaseSchema):
    command: FeederCommand = Field(default=FeederCommand.DELIVER_PELLET, description="The command to send to the feeder device.")

def main():
    schema = CreateFeederCommand.model_json_schema(union_format="primitive_type_array")
    Path("Feeder.json").write_text(json.dumps(schema, indent=2))


if __name__ == "__main__":
    main()
