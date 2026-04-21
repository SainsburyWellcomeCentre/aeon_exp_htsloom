import json
from pathlib import Path
from swc.aeon.schema import Experiment as ExperimentBase, BaseSchema

from swc.aeon_exp.htsloom.rig import Rig
from swc.aeon_exp.htsloom.task import Task


class Experiment(ExperimentBase):
    def _join_pattern_prefix(self, pattern_prefix: str) -> str:
        """Rig is a root container — pass through child prefix unchanged."""
        return pattern_prefix

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
