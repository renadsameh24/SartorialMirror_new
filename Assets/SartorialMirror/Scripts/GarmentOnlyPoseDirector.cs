using UnityEngine;

/// <summary>
/// Toggles SMPL vs garment Rigify follow while reusing the same MediaPipe → 33 spheres → J_* pipeline.
/// Assign references from your duplicated checkpoint scene (or the original scene).
/// </summary>
public sealed class GarmentOnlyPoseDirector : MonoBehaviour
{
    [Header("Mode")]
    [Tooltip("If true: hide SMPL avatar, show garment, use flexible mapper + garment FK. If false: stock SMPL path.")]
    public bool garmentOnly = true;

    [Header("Roots")]
    public GameObject smplAvatarRoot;
    public GameObject garmentInstanceRoot;

    [Header("Mappers (same jointRoot + mediaPipeRoot)")]
    public MediaPipe33_To_J17Mapper1 classicMapper;
    public PoseLandmarksToJointSpheresFlexible flexibleMapper;

    [Header("FK drivers")]
    public SpheresToBones_FKDriver smplFkDriver;
    public SpheresToBones_FKDriver garmentFkDriver;

    [Header("Flexible mapper option when garmentOnly")]
    public bool synthesizeLowerBodyFromPelvis = true;

    void Awake()
    {
        if (flexibleMapper && classicMapper)
        {
            flexibleMapper.mediaPipeRoot = classicMapper.mediaPipeRoot;
            flexibleMapper.jointRoot = classicMapper.jointRoot;
            flexibleMapper.lerp = classicMapper.lerp;
            flexibleMapper.mirrorX = classicMapper.mirrorX;
            flexibleMapper.globalOffset = classicMapper.globalOffset;
        }

        if (flexibleMapper)
            flexibleMapper.synthesizeLowerBodyFromPelvis = synthesizeLowerBodyFromPelvis;
        ApplyMode();
    }

    void OnValidate()
    {
        if (flexibleMapper)
            flexibleMapper.synthesizeLowerBodyFromPelvis = synthesizeLowerBodyFromPelvis;
    }

    public void ApplyMode()
    {
        if (smplAvatarRoot) smplAvatarRoot.SetActive(!garmentOnly);
        if (garmentInstanceRoot) garmentInstanceRoot.SetActive(garmentOnly);

        if (classicMapper) classicMapper.enabled = !garmentOnly;
        if (flexibleMapper) flexibleMapper.enabled = garmentOnly;

        if (smplFkDriver) smplFkDriver.enabled = !garmentOnly;
        if (garmentFkDriver) garmentFkDriver.enabled = garmentOnly;
    }
}
