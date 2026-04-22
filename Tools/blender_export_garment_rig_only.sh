#!/usr/bin/env bash
set -euo pipefail
REPO="$(cd "$(dirname "$0")/.." && pwd)"
export BLEND="${BLEND:-$REPO/retarget_BASELINE_FINAL.blend}"
export EXPORT_FBX="${EXPORT_FBX:-$REPO/Assets/GarmentsExported/Garment_Rigify_Unity.fbx}"
BLENDER_CMD="${BLENDER:-}"
if [[ -z "$BLENDER_CMD" ]]; then
  command -v blender &>/dev/null && BLENDER_CMD="$(command -v blender)"
  [[ -x "/Applications/Blender.app/Contents/MacOS/Blender" ]] && BLENDER_CMD="/Applications/Blender.app/Contents/MacOS/Blender"
fi
mkdir -p "$(dirname "$EXPORT_FBX")"
exec "$BLENDER_CMD" --background --python "$REPO/Tools/blender_export_garment_rig_only.py"
