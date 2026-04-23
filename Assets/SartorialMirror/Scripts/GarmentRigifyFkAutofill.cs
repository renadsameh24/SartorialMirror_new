using System.Collections.Generic;
using System.Text;
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

    [Header("Root follow")]
    [Tooltip("Off by default: Rigify exports often don't have a stable pelvis/root bone like SMPL. Snapping DEF-spine to J_pelvis can collapse the torso. Enable only if you have a true root/pelvis bone to follow.")]
    public bool enableRootFollow = false;

    [Tooltip("Mecanim often overwrites bone transforms each frame; SMPL FK wins in LateUpdate but a garment Animator can still fight this. Disable garment Animators so script FK matches the SMPL-style drive.")]
    public bool disableGarmentAnimators = true;

    static string Norm(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    static Transform FindBone(Transform root, string boneName)
    {
        if (!root) return null;

        // Build a normalized lookup once per call (small rigs, called a few times only).
        var want = Norm(boneName);
        if (string.IsNullOrEmpty(want)) return null;

        Transform exact = null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (!t) continue;
            if (t.name == boneName) return t;
            if (Norm(t.name) == want) exact = t;
        }
        if (exact) return exact;

        // Heuristic: allow matches that END WITH the desired name (helps if importer prefixes names).
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (!t) continue;
            var n = Norm(t.name);
            if (n.EndsWith(want)) return t;
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

        // Root follow is optional; default OFF to avoid torso collapse on Rigify exports.
        fk.rootBone = null;
        fk.rootSphere = null;
        fk.followRootPosition = false;
        fk.followRootRotation = false;

        if (enableRootFollow)
        {
            // Prefer a true root/pelvis if present; fall back to spine only if you know it's safe.
            var rootCandidate = FindBone(garmentArmatureRoot, "root")
                               ?? FindBone(garmentArmatureRoot, "DEF-pelvis")
                               ?? FindBone(garmentArmatureRoot, "DEF-hips")
                               ?? FindBone(garmentArmatureRoot, "DEF-spine");

            var pelvisS = FindSphere(jointSpheresRoot, "J_pelvis");
            if (rootCandidate && pelvisS)
            {
                fk.rootBone = rootCandidate;
                fk.rootSphere = pelvisS;
                fk.followRootPosition = true;
                fk.followRootRotation = false;
                Debug.Log("[GarmentRigifyFkAutofill] Root follow enabled using bone '" + rootCandidate.name + "'.", this);
            }
            else
            {
                Debug.LogWarning("[GarmentRigifyFkAutofill] Root follow enabled but missing rootCandidate/J_pelvis. bone=" + (rootCandidate != null) + " J_pelvis=" + (pelvisS != null), this);
            }
        }

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
        {
            Debug.LogError("[GarmentRigifyFkAutofill] No valid arm segments — check garmentArmatureRoot points at the armature/bone hierarchy (not just the mesh), and verify bone names under it.", this);

            // Dump a small sample of transform names to help pick the right root + expected names.
            int shown = 0;
            var sb = new StringBuilder();
            sb.AppendLine("[GarmentRigifyFkAutofill] Sample transforms under garmentArmatureRoot:");
            foreach (var t in garmentArmatureRoot.GetComponentsInChildren<Transform>(true))
            {
                if (!t) continue;
                sb.Append(" - ").Append(t.name).AppendLine();
                if (++shown >= 40) break;
            }
            Debug.Log(sb.ToString(), this);

            Debug.LogWarning("[GarmentRigifyFkAutofill] Expected (normalized) names like: DEF-spine, DEF-upper_arm.L, DEF-forearm.L, DEF-hand.L (and .R). If your rig uses different names, we can map to them.", this);
        }
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
