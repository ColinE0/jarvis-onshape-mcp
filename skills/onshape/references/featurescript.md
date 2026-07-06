# FeatureScript custom feature — template and call sequence

Referenced from SKILL.md ("When to write a FeatureScript custom feature").
Check `docs/fs-cookbook/` in the plugin repo FIRST — adapting a proven
recipe beats authoring from scratch.

## Minimal template

Copy, adapt, pass as `featureScript` to `write_featurescript_feature`:

```
FeatureScript 2909;
import(path : "onshape/std/geometry.fs", version : "2909.0");

annotation { "Feature Type Name" : "My Custom Feature" }
export const myCustomFeature = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Length" }
        isLength(definition.length, LENGTH_BOUNDS);
        // Add more parameters here: isAngle, isReal, isInteger, isBoolean, etc.
    }
    {
        // Body of the feature -- call op* primitives from onshape/std.
        // opPlane(context, id + "plane1", { "plane" : plane(vector(0,0,1)*definition.length, vector(0,0,1)) });
        // opExtrude(context, id + "ext1", { "entities": ..., "direction": ..., "endBound": ..., "endBoundEntity": ... });
    });
```

## Call from the MCP layer

```
write_featurescript_feature(
  documentId, workspaceId, elementId,
  feature_type="myCustomFeature",
  feature_name="Instance name",
  feature_script="<the FS source above>",
  parameters=[{"id": "length", "type": "quantity", "value": "15 mm"}],
)
```

The orchestrator creates a Feature Studio element, uploads the source,
pulls the microversion, and instantiates via BTMFeature-134 with the
correct `e<eid>::m<mv>` namespace. You get back a `FeatureApplyResult`
with the usual `{status, feature_id, error_message, hints}` contract —
regen errors in your FS body propagate through exactly the same way as
starter-feature errors.
