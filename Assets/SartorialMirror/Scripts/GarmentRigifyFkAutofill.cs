using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wires <see cref="SpheresToBones_FKDriver"/> to Rigify-export DEF-* bones and the same J_* sphere
/// names used by the SMPL checkpoint. Add to the same GameObject as <see cref="SpheresToBones_FKDriver"/>
/// (or assign <see cref="fk"/>). Run once in Awake when <see cref="autoWireOnPlay"/> is true.
/// </summary>
[DefaultExecutionOrder(2000)]
public sealed class GarmentRigifyFkAutofill : MonoBehaviour
{
    public SpheresToBones_FKDriver fk;
    [Tooltip("Imported FBX root that contains the armature (Animator root or armature object).")]
    public Transform garmentArmatureRoot;

    [Tooltip("Same JointSpheresRoot / hierarchy as checkpoint (J_pelvis, J_l_shoulder, …).")]
    public Transform jointSpheresRoot;

    public bool autoWireOnPlay = true;

    [Tooltip("Mecanim often overwrites bone transforms each frame; SMPL FK wins in LateUpdate but a garment Animator can still fight this. Disable garment Animators so script FK matches the SMPL-style drive.")]
    public bool disableGarmentAnimators = true;

    static IEnumerable<string> BoneNameVariants(string canonical)
    {
        if (string.IsNullOrEmpty(canonical)) yield break;
        yield return canonical;
        yield return canonical.Replace('.', '_');
        yield return canonical.Replace(".", "");
    }

    static Transform FindBone(Transform root, string boneName)
    {
        if (!root) return null;
        foreach (var variant in BoneNameVariants(boneName))
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == variant)
                    return t;
            }
        }
        return null;
    }

    static Transform FindSphere(Transform root, string n) => FindBone(root, n);

    void Awake()
    {
        if (!autoWireOnPlay) return;
        if (!fk) fk = GetComponent<SpheresToBones_FKDriver>();
        if (!fk || !garmentArmatureRoot || !jointSpheresRoot)
        {
            Debug.LogWarning("[GarmentRigifyFkAutofill] Missing references; skipping autofill.", this);
            return;
        }

        if (disableGarmentAnimators)
        {
            foreach (var anim in garmentArmatureRoot.GetComponentsInChildren<Animator>(true))
            {
                if (anim && anim.enabled)
                {
                    anim.enabled = false;
                    Debug.Log("[GarmentRigifyFkAutofill] Disabled Animator on '" + anim.name + "' so FK can drive bones.", anim);
                }
            }
        }

        var spine = FindBone(garmentArmatureRoot, "DEF-spine");
        var pelvisS = FindSphere(jointSpheresRoot, "J_pelvis");
        if (spine && pelvisS)
        {
            fk.rootBone = spine;
            fk.rootSphere = pelvisS;
            fk.followRootPosition = true;
            fk.followRootRotation = false;
        }
        else
            Debug.LogWarning("[GarmentRigifyFkAutofill] Root follow not set (need DEF-spine + J_pelvis). Spine=" + (spine != null) + " J_pelvis=" + (pelvisS != null), this);

        fk.segments = new[]
        {
            BuildSeg(garmentArmatureRoot, jointSpheresRoot, "DEF-upper_arm.L", "DEF-forearm.L", "J_l_shoulder", "J_l_elbow"),
            BuildSeg(garmentArmatureRoot, jointSpheresRoot, "DEF-forearm.L", "DEF-hand.L", "J_l_elbow", "J_l_wrist"),
            BuildSeg(garmentArmatureRoot, jointSpheresRoot, "DEF-upper_arm.R", "DEF-forearm.R", "J_r_shoulder", "J_r_elbow"),
            BuildSeg(garmentArmatureRoot, jointSpheresRoot, "DEF-forearm.R", "DEF-hand.R", "J_r_elbow", "J_r_wrist"),
        };

        int ok = 0;
        foreach (var s in fk.segments)
        {
            if (s.bone && s.boneChild && s.sphere && s.sphereChild)
                ok++;
        }
        Debug.Log($"[GarmentRigifyFkAutofill] Arm segments wired: {ok}/4. (Same J_* spheres as SMPL; garment rig must match DEF-* names or FBX underscore variants.)", this);
        if (ok == 0)
            Debug.LogError("[GarmentRigifyFkAutofill] No valid arm segments — check bone names under garmentArmatureRoot in Hierarchy (Rigify DEF-* / Unity renames).", this);
    }

    static SpheresToBones_FKDriver.Segment BuildSeg(Transform arm, Transform spheres, string bn, string bc, string sn, string sc)
    {
        return new SpheresToBones_FKDriver.Segment
        {
            bone = FindBone(arm, bn),
            boneChild = FindBone(arm, bc),
            sphere = FindSphere(spheres, sn),
            sphereChild = FindSphere(spheres, sc),
            applyPositionToBone = false
        };
    }
}
