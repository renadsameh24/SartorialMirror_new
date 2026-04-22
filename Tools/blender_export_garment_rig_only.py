"""
Open blend, remove metarig Armature modifier from garment mesh, export only mesh + **rig** armature.
"""
from __future__ import annotations

import os
import sys

import bpy


def log(msg: str) -> None:
    print(f"[export_garment_rig] {msg}", flush=True)


BLEND = os.environ.get("BLEND", "").strip()
EXPORT_FBX = os.environ.get("EXPORT_FBX", "").strip()
GARMENT_MESH = os.environ.get("GARMENT_MESH", "VR4D030312_ShirtFlannelBWWoman").strip()
RIG_NAME = os.environ.get("RIG_NAME", "rig").strip()
META_NAME = os.environ.get("META_NAME", "metarig").strip()


def main() -> int:
    if not BLEND or not EXPORT_FBX:
        log("Set BLEND and EXPORT_FBX.")
        return 1

    bpy.ops.wm.open_mainfile(filepath=BLEND)
    rig = bpy.data.objects.get(RIG_NAME)
    meta = bpy.data.objects.get(META_NAME)
    shirt = bpy.data.objects.get(GARMENT_MESH)

    if not rig or rig.type != "ARMATURE":
        log(f"Missing armature '{RIG_NAME}'.")
        return 2
    if not shirt or shirt.type != "MESH":
        log(f"Missing mesh '{GARMENT_MESH}'.")
        return 2

    for mod in list(shirt.modifiers):
        if mod.type != "ARMATURE":
            continue
        obj = getattr(mod, "object", None)
        if obj is None:
            continue
        if obj == meta or obj.name == META_NAME:
            mod_name = mod.name
            shirt.modifiers.remove(mod)
            log(f"Removed modifier '{mod_name}' -> {META_NAME}")

    has_rig = any(
        m.type == "ARMATURE" and getattr(m, "object", None) == rig for m in shirt.modifiers
    )
    if not has_rig:
        m = shirt.modifiers.new(name="Armature", type="ARMATURE")
        m.object = rig
        m.use_vertex_groups = True

    view_layer = bpy.context.view_layer
    for o in view_layer.objects:
        o.select_set(False)
    shirt.select_set(True)
    rig.select_set(True)
    view_layer.objects.active = rig

    os.makedirs(os.path.dirname(EXPORT_FBX) or ".", exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=EXPORT_FBX,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        add_leaf_bones=False,
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        bake_anim=False,
        path_mode="AUTO",
        apply_scale_options="FBX_SCALE_ALL",
        global_scale=1.0,
        mesh_smooth_type="FACE",
        use_mesh_modifiers=True,
        use_armature_deform_only=True,
    )
    log(f"Exported {EXPORT_FBX}")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception:
        import traceback

        traceback.print_exc()
        sys.exit(3)
