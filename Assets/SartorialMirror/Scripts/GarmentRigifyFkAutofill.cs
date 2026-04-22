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

    static Transform FindBone(Transform root, string boneName)
    {
        if (!root) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == boneName)
                return t;
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
            Debug.LogWarning("[GarmentRigifyFkAutofill] Missing references; skipping autofill.");
            return;
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

        fk.segments = new[]
        {
            BuildSeg(garmentArmatureRoot, jointSpheresRoot, "DEF-upper_arm.L", "DEF-forearm.L", "J_l_shoulder", "J_l_elbow"),
            BuildSeg(garmentArmatureRoot, jointSpheresRoot, "DEF-forearm.L", "DEF-hand.L", "J_l_elbow", "J_l_wrist"),
            BuildSeg(garmentArmatureRoot, jointSpheresRoot, "DEF-upper_arm.R", "DEF-forearm.R", "J_r_shoulder", "J_r_elbow"),
            BuildSeg(garmentArmatureRoot, jointSpheresRoot, "DEF-forearm.R", "DEF-hand.R", "J_r_elbow", "J_r_wrist"),
        };
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
