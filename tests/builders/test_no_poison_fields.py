"""Regression: builder payloads must not contain fields Onshape rejects.

Current Onshape API revisions reject three things that older builder code
emitted on nearly every mutating feature, each producing an opaque HTTP 400
(BTWeirdStringValueException at parameters[0]) that looks nothing like its
cause:

- ``"libraryRelationType": "NONE"`` on any BTMParameter
- ``"parameterName": ""`` on any BTMParameter
- ``"queryStatement": None`` (JSON null) inside any query — queryStatement is
  an abstract BTPStatement; null and "" both fail

This test walks the full JSON of every builder's build() output and fails on
any occurrence, so the fields cannot creep back in through a new builder or a
copy-pasted parameter block.
"""

from typing import Any

from onshape_mcp.builders.boolean import BooleanBuilder, BooleanType
from onshape_mcp.builders.chamfer import ChamferBuilder
from onshape_mcp.builders.extrude import ExtrudeBuilder, ExtrudeType
from onshape_mcp.builders.fillet import FilletBuilder
from onshape_mcp.builders.offset_plane import OffsetPlaneBuilder
from onshape_mcp.builders.pattern import (
    CircularPatternBuilder,
    LinearPatternBuilder,
)
from onshape_mcp.builders.revolve import RevolveBuilder
from onshape_mcp.builders.shell import ShellBuilder
from onshape_mcp.builders.thicken import ThickenBuilder


def _assert_clean(node: Any, path: str = "$") -> None:
    if isinstance(node, dict):
        assert "libraryRelationType" not in node, f"libraryRelationType at {path}"
        if "parameterName" in node:
            assert node["parameterName"] != "", f'parameterName: "" at {path}'
        if "queryStatement" in node:
            assert node["queryStatement"] is not None, f"null queryStatement at {path}"
        for key, value in node.items():
            _assert_clean(value, f"{path}.{key}")
    elif isinstance(node, list):
        for i, item in enumerate(node):
            _assert_clean(item, f"{path}[{i}]")


def _built_payloads():
    boolean = BooleanBuilder(boolean_type=BooleanType.SUBTRACT)
    boolean.add_tool_body("t1")
    boolean.add_target_body("g1")

    chamfer = ChamferBuilder()
    chamfer.add_edge("e1")

    fillet = FilletBuilder()
    fillet.add_edge("e1")

    shell = ShellBuilder()
    shell.add_face("f1")
    shell.set_thickness(1.0)

    thicken = ThickenBuilder(name="Thicken", sketch_feature_id="sk1")
    thicken.set_thickness(2.0)

    linear = LinearPatternBuilder(direction_edge_id="e1")
    linear.add_feature("feat1")

    circular = CircularPatternBuilder(axis_edge_id="e1")
    circular.add_feature("feat1")

    return {
        "extrude": ExtrudeBuilder(
            sketch_feature_id="sk1", operation_type=ExtrudeType.NEW
        ).build(),
        "revolve": RevolveBuilder(sketch_feature_id="sk1").build(),
        "boolean": boolean.build(),
        "chamfer": chamfer.build(),
        "fillet": fillet.build(),
        "shell": shell.build(),
        "thicken": thicken.build(),
        "offset_plane": OffsetPlaneBuilder(reference_id="p1").build(),
        "linear_pattern": linear.build(),
        "circular_pattern": circular.build(),
    }


def test_no_rejected_fields_in_any_builder_payload():
    for name, payload in _built_payloads().items():
        _assert_clean(payload, path=name)
