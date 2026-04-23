using UnityEngine;

/// <summary>
/// WebGL kiosk mode:
/// - Hide Unity's webcam background UI (web renders camera instead).
/// - Force main camera to transparent clear (alpha = 0) so WebGL can overlay.
/// - Align the garment root using live J_* shoulder spheres.
/// Attach this once in your WebGL scene (e.g. on a "WebGLStage" GameObject).
/// </summary>
[DefaultExecutionOrder(-500)]
public sealed class WebGLGarmentStageController : MonoBehaviour
{
    [Header("Hide Unity webcam background (rendering only)")]
    public bool hideWebcamBackgroundInWebGL = true;
    public WebcamBackgroundUGUI webcamBackground;

    [Header("Transparent WebGL background")]
    public bool forceTransparentCameraClear = true;
    public Camera targetCamera;

    [Header("Garment alignment")]
    [Tooltip("Root transform of the garment instance to move/scale (NOT the skeleton bones).")]
    public Transform garmentRoot;

    [Tooltip("Optional explicit J_* sphere references (auto-found by name if left empty).")]
    public Transform jLeftShoulder;   // J_l_shoulder
    public Transform jRightShoulder;  // J_r_shoulder
    public Transform jNeck;           // J_neck (optional)
    public Transform jPelvis;         // J_pelvis (optional)

    [Header("Tuning")]
    [Tooltip("Desired garment width relative to shoulder distance.")]
    public float widthFromShoulders = 1.60f;
    [Tooltip("Vertical offset from shoulder midpoint, in shoulder-distance units.")]
    public float yOffsetFromShoulders = -0.15f;
    [Tooltip("Forward/back offset from shoulder midpoint, in shoulder-distance units.")]
    public float zOffsetFromShoulders = 0.00f;
    [Range(0f, 1f)] public float positionLerp = 0.35f;
    [Range(0f, 1f)] public float rotationLerp = 0.35f;
    [Range(0f, 1f)] public float scaleLerp = 0.25f;

    Vector3 _initialScale;
    bool _scaleInited;

    void Awake()
    {
        if (webcamBackground == null)
            webcamBackground = FindObjectOfType<WebcamBackgroundUGUI>(true);

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (garmentRoot == null)
        {
            // Best-effort: if your scene uses GarmentOnlyPoseDirector, use its garment root.
            var director = FindObjectOfType<GarmentOnlyPoseDirector>(true);
            if (director != null && director.garmentInstanceRoot != null)
                garmentRoot = director.garmentInstanceRoot.transform;
        }

        AutoFindJointsIfMissing();

        if (garmentRoot != null && !_scaleInited)
        {
            _initialScale = garmentRoot.localScale;
            _scaleInited = true;
        }
    }

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (hideWebcamBackgroundInWebGL && webcamBackground != null)
            webcamBackground.enabled = false;
#endif
        if (forceTransparentCameraClear && targetCamera != null)
        {
            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            var c = targetCamera.backgroundColor;
            c.a = 0f;
            targetCamera.backgroundColor = c;
        }
    }

    void LateUpdate()
    {
        if (garmentRoot == null) return;

        AutoFindJointsIfMissing();
        if (jLeftShoulder == null || jRightShoulder == null) return;

        var ls = jLeftShoulder.position;
        var rs = jRightShoulder.position;
        var shoulderMid = (ls + rs) * 0.5f;
        var shoulderVec = (rs - ls);
        var shoulderDist = shoulderVec.magnitude;
        if (shoulderDist < 0.0001f) return;

        // Position: follow shoulder midpoint with small offsets.
        var up = targetCamera != null ? targetCamera.transform.up : Vector3.up;
        var forward = targetCamera != null ? targetCamera.transform.forward : Vector3.forward;
        var goalPos =
            shoulderMid +
            up * (yOffsetFromShoulders * shoulderDist) +
            forward * (zOffsetFromShoulders * shoulderDist);

        garmentRoot.position = Vector3.Lerp(garmentRoot.position, goalPos, positionLerp);

        // Rotation: align garment "right" axis to shoulder line, keep facing camera.
        var right = shoulderVec.normalized;
        var face = targetCamera != null ? -targetCamera.transform.forward : Vector3.forward;
        var goalRot = Quaternion.LookRotation(face, up) * Quaternion.FromToRotation(Vector3.right, right);
        garmentRoot.rotation = Quaternion.Slerp(garmentRoot.rotation, goalRot, rotationLerp);

        // Scale: match garment width to shoulder distance.
        if (_scaleInited)
        {
            var desiredWidth = shoulderDist * widthFromShoulders;
            // Use X scale as the width driver, maintain proportions from initial scale.
            // If your garment root isn't uniform-scaled, set it uniform in the scene first.
            var baseX = Mathf.Max(0.0001f, _initialScale.x);
            var factor = desiredWidth / baseX;
            var goalScale = _initialScale * factor;
            garmentRoot.localScale = Vector3.Lerp(garmentRoot.localScale, goalScale, scaleLerp);
        }
    }

    void AutoFindJointsIfMissing()
    {
        if (jLeftShoulder == null) jLeftShoulder = FindByName("J_l_shoulder");
        if (jRightShoulder == null) jRightShoulder = FindByName("J_r_shoulder");
        if (jNeck == null) jNeck = FindByName("J_neck");
        if (jPelvis == null) jPelvis = FindByName("J_pelvis");
    }

    static Transform FindByName(string name)
    {
        var go = GameObject.Find(name);
        return go != null ? go.transform : null;
    }
}

