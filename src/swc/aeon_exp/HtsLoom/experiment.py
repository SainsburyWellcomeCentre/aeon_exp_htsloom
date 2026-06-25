import json
from pathlib import Path
from swc.aeon.schema import Experiment as ExperimentBase, BaseSchema

from swc.aeon_exp.htsloom.rig import Rig
from swc.aeon_exp.htsloom.task import Task
from swc.aeon_exp.htsloom.controller import ForagingController


class Experiment(ExperimentBase):
    def _join_pattern_prefix(self, pattern_prefix: str) -> str:
        """Override the BaseSchema hook so Experiment stays transparent.

        Experiment is a structural grouping container, so its name must not
        be injected into the flat data-file patterns (e.g. CameraNorth_201_*,
        not Experiment/CameraNorth_201_*). We pass the child prefix through
        unchanged instead of replacing it with the container name.
        """
        return pattern_prefix

    rig: Rig
    task: Task

class HtsLoom(BaseSchema):
    experiment: Experiment|None = None
    foraging_controller: ForagingController|None = None

def main():
    schema = HtsLoom.model_json_schema(union_format="primitive_type_array")
    schema.pop('properties')
    Path("HtsLoom.json").write_text(json.dumps(schema, indent=2))


if __name__ == "__main__":
    main()
