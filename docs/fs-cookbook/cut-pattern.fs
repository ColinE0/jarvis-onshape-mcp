// Cut (REMOVE-feature) patterns via opPattern.
//
// Broken alternative: create_linear_pattern / create_circular_pattern with a
// REMOVE extrude as the seed. Those tools build PART (body) patterns of the
// bodies the seed feature CREATED — and a cut creates no bodies, so there is
// nothing to replicate. True FEATURE patterns exist in the UI but their JSON
// form (instanceFunction, a BTMParameterFeatureList) is not supported by the
// builder layer.
//
// This recipe replicates the cut by patterning the TOOL geometry instead:
// re-extrude the cutting profile as separate tool bodies, opPattern them,
// then opBoolean SUBTRACT the whole row out of the target in one shot.
//
// Alternatively, when the seed is a NEW body you want repeated (posts,
// knuckles, bosses), the plain create_linear_pattern tool now works — this
// recipe is only needed for cuts.
//
// VERIFIED: opPattern(PatternType.PART) with qCreatedBy(makeId(<seedFeatureId>),
// EntityType.BODY) and explicit transforms, plus booleanBodies SUBTRACT,
// confirmed live 2026-06-30 (hinged-box dogfood build: knuckle row + pin bore).

annotation { "Feature Type Name" : "Cut pattern" }
export const cutPattern = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Sketch region to cut", "Filter" : EntityType.FACE && SketchObject.YES }
        definition.profile is Query;
        annotation { "Name" : "Target bodies", "Filter" : EntityType.BODY && BodyType.SOLID }
        definition.targets is Query;
        annotation { "Name" : "Cut depth" }
        isLength(definition.depth, LENGTH_BOUNDS);
        annotation { "Name" : "Instance spacing" }
        isLength(definition.spacing, LENGTH_BOUNDS);
        annotation { "Name" : "Instance count" }
        isInteger(definition.count, POSITIVE_COUNT_BOUNDS);
    }
    {
        // 1. Extrude the profile as standalone tool bodies (NEW, not REMOVE).
        opExtrude(context, id + "tool", {
            "entities" : definition.profile,
            "direction" : evOwnerSketchPlane(context, {"entity" : definition.profile}).normal * -1,
            "endBound" : BoundingType.BLIND,
            "endDepth" : definition.depth
        });

        // 2. Pattern the tool body along X. Swap the vector for other axes;
        //    instanceNames must be unique non-empty strings.
        var transforms = [];
        var names = [];
        for (var i = 1; i < definition.count; i += 1)
        {
            transforms = append(transforms,
                transform(vector(definition.spacing * i, 0 * millimeter, 0 * millimeter)));
            names = append(names, "inst" ~ i);
        }
        if (size(transforms) > 0)
        {
            opPattern(context, id + "pat", {
                "patternType" : PatternType.PART,
                "entities" : qCreatedBy(id + "tool", EntityType.BODY),
                "transforms" : transforms,
                "instanceNames" : names
            });
        }

        // 3. Subtract the whole tool row from the targets in one boolean.
        opBoolean(context, id + "cut", {
            "tools" : qUnion([
                qCreatedBy(id + "tool", EntityType.BODY),
                qCreatedBy(id + "pat", EntityType.BODY)
            ]),
            "targets" : definition.targets,
            "operationType" : BooleanOperationType.SUBTRACTION
        });
    });

// Worked example (5mm holes every 10mm, 4x, through a 6mm plate):
//   profile  -> qSketchRegion(id + "<holeSketchFeatureId>", true)
//   targets  -> qAllModifiableSolidBodies() or specific body query
//   depth    -> 6 * millimeter
//   spacing  -> 10 * millimeter
//   count    -> 4
