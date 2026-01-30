import json
from pathlib import Path
from swc.aeon_rigs.experiment import Experiment as ExperimentBase, BaseSchema

from swc.aeon_exp.HtsLoom.rig import Rig
from swc.aeon_exp.HtsLoom.task import Task


class Experiment(ExperimentBase):
    rig: Rig
    task: Task

class HtsLoom(BaseSchema):
    experiment: Experiment|None = None

def main():
    schema = HtsLoom.model_json_schema(union_format="primitive_type_array")
    schema.pop('properties')
    Path("HtsLoom.json").write_text(json.dumps(schema, indent=2))


if __name__ == "__main__":
    main()
