# SartorialMirror_new

Unity project extended from **SartorialMirrorProto_BACKUP** with **garment-only Rigify** support alongside the original SMPL pose pipeline.

## Blender source

- `retarget_BASELINE_FINAL.blend` — garment mesh **`VR4D030312_ShirtFlannelBWWoman`**, Rigify armature **`rig`** (use **`rig`**, not `metarig`). SMPL body/armature are ignored for garment export.

## Garment FBX export (Blender batch)

```bash
export BLEND="$(pwd)/retarget_BASELINE_FINAL.blend"
export EXPORT_FBX="$(pwd)/Assets/GarmentsExported/Garment_Rigify_Unity.fbx"
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/blender_export_garment_rig_only.py
```

The script removes the shirt’s Armature modifier that targets **`metarig`**, keeps **`rig`**, exports **only** the shirt mesh + **`rig`**.

## Pose pipeline (unchanged architecture)

1. **Python** `python_server/server_fastapi.py` — WebSocket + MediaPipe (needs `python_server/models/pose_landmarker_full.task`).
2. **`PoseReceiverWS`** — receives landmarks; optional webcam frames.
3. **`PoseRig3DSpheres`** on **`PoseRig`** — spawns 33 `J00`… landmark spheres.
4. **`MediaPipe33_To_J17Mapper1`** OR **`PoseLandmarksToJointSpheresFlexible`** — maps 33 → **`JointSpheresRoot`** children (`J_pelvis`, `J_l_shoulder`, …).
5. **`SpheresToBones_FKDriver`** — FK from sphere pairs to **bone** pairs (SMPL **or** garment `DEF-*` bones).

## Garment Unity wiring

1. Import / refresh `Assets/GarmentsExported/Garment_Rigify_Unity.fbx` (already exported in-repo).
2. Open **`Assets/SMPL_GarmentPoseCheckpoint.unity`** (duplicate of the working checkpoint).
3. Drag the garment FBX into the scene as an instance.
4. Create an empty **`GarmentPoseDriver`** (child of garment instance or separate):
   - Add **`SpheresToBones_FKDriver`**
   - Add **`GarmentRigifyFkAutofill`** — assign `garmentArmatureRoot` to the imported armature root that contains `DEF-spine`, `DEF-upper_arm.L`, …; assign `jointSpheresRoot` to the scene’s **`JointSpheresRoot`** (same as SMPL setup); enable **Auto Wire On Play**.
5. Add **`PoseLandmarksToJointSpheresFlexible`** next to **`MediaPipe33_To_J17Mapper1`** on **`MediaPipeToSMPLMapper`** (or sibling object). Wire `mediaPipeRoot` + `jointRoot` like the classic mapper (or leave blank and use **`GarmentOnlyPoseDirector`** to copy from classic).
6. Add **`GarmentOnlyPoseDirector`** on a runtime object (e.g. `SartorialMirrorRuntime` sibling):
   - `smplAvatarRoot` → `SMPL_neutral_rig_GOLDEN`
   - `garmentInstanceRoot` → your garment instance
   - `classicMapper` → `MediaPipe33_To_J17Mapper1`
   - `flexibleMapper` → `PoseLandmarksToJointSpheresFlexible`
   - `smplFkDriver` → FK on SMPL prefab
   - `garmentFkDriver` → FK on **GarmentPoseDriver**
   - Enable **`garmentOnly`** when you want shirt-only; tune **`synthesizeLowerBodyFromPelvis`** for cleaner legs on a shirt.

## Run

1. Start pose server (see original README in backup / same `python_server` here).
2. Unity: open **`SMPL_GarmentPoseCheckpoint.unity`**, press Play with **`garmentOnly`** on or off.

## Files added for garment path

| File | Purpose |
|------|---------|
| `PoseLandmarksToJointSpheresFlexible.cs` | Mapper like MediaPipe33 with optional synthetic lower body. |
| `GarmentRigifyFkAutofill.cs` | Auto-fills FK segments for Rigify `DEF-*` bones + J_* spheres. |
| `GarmentOnlyPoseDirector.cs` | Toggles SMPL vs garment + classic vs flexible mapper + FK drivers. |
| `Tools/blender_export_garment_rig_only.py` | Blender-only garment+rig export. |
| `Assets/GarmentsExported/Garment_Rigify_Unity.fbx` | Exported garment. |

Fine-tuning: bone names must match the FBX (`DEF-upper_arm.L`, …). If your Blender export renames bones, update **`GarmentRigifyFkAutofill`** search strings or assign **`SpheresToBones_FKDriver.segments`** manually in the Inspector.
