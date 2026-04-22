using System.Collections;
using UnityEngine;

/// <summary>
/// Same role as MediaPipe33_To_J17Mapper1: 33 MediaPipe spheres → J_* targets for SpheresToBones_FKDriver.
/// Optional synthesizeLowerBodyFromPelvis for garment-only upper-body focus (legs follow pelvis stub).
/// </summary>
public sealed class PoseLandmarksToJointSpheresFlexible : MonoBehaviour
{
    [Header("MediaPipe landmark spheres (33) root")]
    public Transform mediaPipeRoot;

    [Header("J_* sphere targets root (same layout as SMPL checkpoint)")]
    public Transform jointRoot;

    [Header("Tuning")]
    public bool mirrorX = false;
    [Range(0f, 1f)] public float lerp = 1f;
    public Vector3 globalOffset = Vector3.zero;

    [Tooltip("If true, J_l_hip … J_r_ankle are placed from pelvis with small offsets instead of raw leg landmarks.")]
    public bool synthesizeLowerBodyFromPelvis = false;

    const int L_SHOULDER = 11, R_SHOULDER = 12;
    const int L_ELBOW = 13, R_ELBOW = 14;
    const int L_WRIST = 15, R_WRIST = 16;
    const int L_HIP = 23, R_HIP = 24;
    const int L_KNEE = 25, R_KNEE = 26;
    const int L_ANKLE = 27, R_ANKLE = 28;
    const int NOSE = 0;
    const int L_EAR = 7, R_EAR = 8;

    Transform[] mp = new Transform[33];
    Transform J_pelvis, J_spine, J_neck, J_head;
    Transform J_l_shoulder, J_l_elbow, J_l_wrist;
    Transform J_r_shoulder, J_r_elbow, J_r_wrist;
    Transform J_l_hip, J_l_knee, J_l_ankle;
    Transform J_r_hip, J_r_knee, J_r_ankle;
    bool targetsReady;

    IEnumerator Start()
    {
        while (mediaPipeRoot == null || jointRoot == null)
            yield return null;
        while (mediaPipeRoot.childCount < 33)
            yield return null;
        CacheMp();
        CacheTargets();
    }

    void CacheMp()
    {
        for (int i = 0; i < 33; i++)
            mp[i] = mediaPipeRoot.GetChild(i);
    }

    void CacheTargets()
    {
        J_pelvis = FindDeep(jointRoot, "J_pelvis");
        J_spine = FindDeep(jointRoot, "J_spine");
        J_neck = FindDeep(jointRoot, "J_neck");
        J_head = FindDeep(jointRoot, "J_head");
        J_l_shoulder = FindDeep(jointRoot, "J_l_shoulder");
        J_l_elbow = FindDeep(jointRoot, "J_l_elbow");
        J_l_wrist = FindDeep(jointRoot, "J_l_wrist");
        J_r_shoulder = FindDeep(jointRoot, "J_r_shoulder");
        J_r_elbow = FindDeep(jointRoot, "J_r_elbow");
        J_r_wrist = FindDeep(jointRoot, "J_r_wrist");
        J_l_hip = FindDeep(jointRoot, "J_l_hip");
        J_l_knee = FindDeep(jointRoot, "J_l_knee");
        J_l_ankle = FindDeep(jointRoot, "J_l_ankle");
        J_r_hip = FindDeep(jointRoot, "J_r_hip");
        J_r_knee = FindDeep(jointRoot, "J_r_knee");
        J_r_ankle = FindDeep(jointRoot, "J_r_ankle");

        targetsReady = J_pelvis && J_spine && J_neck && J_head &&
                       J_l_shoulder && J_l_elbow && J_l_wrist &&
                       J_r_shoulder && J_r_elbow && J_r_wrist &&
                       J_l_hip && J_l_knee && J_l_ankle &&
                       J_r_hip && J_r_knee && J_r_ankle;
        if (!targetsReady)
            Debug.LogError("[PoseLandmarksToJointSpheresFlexible] Missing J_* under jointRoot.");
    }

    void LateUpdate()
    {
        if (!targetsReady || mediaPipeRoot == null || jointRoot == null || mediaPipeRoot.childCount < 33)
            return;
        if (mp[0] == null) CacheMp();

        Vector3 Ls = Pos(mp[L_SHOULDER]);
        Vector3 Rs = Pos(mp[R_SHOULDER]);
        Vector3 Le = Pos(mp[L_ELBOW]);
        Vector3 Re = Pos(mp[R_ELBOW]);
        Vector3 Lw = Pos(mp[L_WRIST]);
        Vector3 Rw = Pos(mp[R_WRIST]);

        Vector3 Lh = Pos(mp[L_HIP]);
        Vector3 Rh = Pos(mp[R_HIP]);
        Vector3 Lk = Pos(mp[L_KNEE]);
        Vector3 Rk = Pos(mp[R_KNEE]);
        Vector3 La = Pos(mp[L_ANKLE]);
        Vector3 Ra = Pos(mp[R_ANKLE]);

        Vector3 pelvis = (Lh + Rh) * 0.5f;
        Vector3 shoulderCenter = (Ls + Rs) * 0.5f;
        Vector3 spine = (pelvis + shoulderCenter) * 0.5f;
        Vector3 neck = shoulderCenter;

        Vector3 headBase = neck;
        if (mp[L_EAR] != null && mp[R_EAR] != null)
            headBase = (Pos(mp[L_EAR]) + Pos(mp[R_EAR])) * 0.5f;
        else if (mp[NOSE] != null)
            headBase = Pos(mp[NOSE]);

        Vector3 up = neck - pelvis;
        if (up.sqrMagnitude < 1e-6f) up = Vector3.up;
        Vector3 head = Vector3.Lerp(neck, headBase, 0.7f) + up.normalized * 0.10f;

        Set(J_pelvis, pelvis);
        Set(J_spine, spine);
        Set(J_neck, neck);
        Set(J_head, head);
        Set(J_l_shoulder, Ls);
        Set(J_l_elbow, Le);
        Set(J_l_wrist, Lw);
        Set(J_r_shoulder, Rs);
        Set(J_r_elbow, Re);
        Set(J_r_wrist, Rw);

        if (synthesizeLowerBodyFromPelvis)
        {
            Vector3 right = Vector3.Cross(up, Vector3.forward).normalized;
            if (right.sqrMagnitude < 1e-6f) right = Vector3.right;
            float w = 0.12f;
            Set(J_l_hip, pelvis - right * w * 0.5f);
            Set(J_r_hip, pelvis + right * w * 0.5f);
            Set(J_l_knee, pelvis - right * w * 0.5f + Vector3.down * 0.35f);
            Set(J_r_knee, pelvis + right * w * 0.5f + Vector3.down * 0.35f);
            Set(J_l_ankle, pelvis - right * w * 0.5f + Vector3.down * 0.70f);
            Set(J_r_ankle, pelvis + right * w * 0.5f + Vector3.down * 0.70f);
        }
        else
        {
            Set(J_l_hip, Lh);
            Set(J_l_knee, Lk);
            Set(J_l_ankle, La);
            Set(J_r_hip, Rh);
            Set(J_r_knee, Rk);
            Set(J_r_ankle, Ra);
        }
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var f = FindDeep(root.GetChild(i), name);
            if (f) return f;
        }
        return null;
    }

    Vector3 Pos(Transform t)
    {
        Vector3 p = t.position + globalOffset;
        if (mirrorX) p.x = -p.x;
        return p;
    }

    void Set(Transform target, Vector3 p)
    {
        if (!target) return;
        if (lerp >= 0.999f) target.position = p;
        else target.position = Vector3.Lerp(target.position, p, Mathf.Clamp01(lerp));
    }
}
