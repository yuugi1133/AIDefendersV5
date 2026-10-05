using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody))]
public class CCTVDroneController : MonoBehaviour
{
    [Header("Flight")]
    public float acceleration = 22f;
    public float brake = 18f;
    public float maxSpeed = 16f;
    public float pullBackStrength = 40f;

    [Header("Look")]
    public float mouseSensitivity = 0.12f;
    public float minPitch = 15f;
    public float maxPitch = 80f;

    [Header("Zoom")]
    public float[] zoomFovs = { 60f, 40f, 25f };

    [Header("Refs")]
    public CCTVMoveLimit moveLimit;

    WaveDirector waveDirector;
    TurretPlatformPlacer buildPlacer;
    Rigidbody body;
    Renderer[] bodyRenderers;
    Camera[] zoomCameras;
    Vector3 homePosition;
    Quaternion homeRotation;
    float yaw;
    float pitch;
    int zoomLevel;
    bool flying;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.linearDamping = 0f;
        body.angularDamping = 0f;

        Collider hull = GetComponent<Collider>();
        if (hull != null)
            hull.isTrigger = true;

        bodyRenderers = GetComponents<Renderer>();
        zoomCameras = GetComponentsInChildren<Camera>(true);
        homePosition = transform.position;
        homeRotation = transform.rotation;

        Vector3 euler = transform.rotation.eulerAngles;
        yaw = euler.y;
        pitch = NormalizePitch(euler.x);
    }

    void Start()
    {
        waveDirector = FindFirstObjectByType<WaveDirector>();
        if (moveLimit == null)
            moveLimit = FindFirstObjectByType<CCTVMoveLimit>();
        if (buildPlacer == null)
            buildPlacer = FindFirstObjectByType<TurretPlatformPlacer>();

        if (waveDirector != null)
        {
            waveDirector.OnWaveStarted += HandleWaveStarted;
            waveDirector.OnPrepStarted += HandlePrepStarted;
            waveDirector.OnStageComplete += HandleStageComplete;
        }

        bool combat = waveDirector != null && waveDirector.Phase == WavePhase.Combat;
        SetFlying(combat);
    }

    void OnDestroy()
    {
        if (waveDirector == null)
            return;

        waveDirector.OnWaveStarted -= HandleWaveStarted;
        waveDirector.OnPrepStarted -= HandlePrepStarted;
        waveDirector.OnStageComplete -= HandleStageComplete;
    }

    void HandleWaveStarted(int waveIndex)
    {
        SetFlying(true);
    }

    void HandlePrepStarted(int waveIndex, float duration)
    {
        SetFlying(false);
    }

    void HandleStageComplete()
    {
        SetFlying(false);
    }

    void SetFlying(bool enabled)
    {
        flying = enabled;
        body.isKinematic = !enabled;
        body.linearVelocity = Vector3.zero;

        SetVisible(enabled);
        SetCursorLocked(enabled);

        if (!enabled)
        {
            transform.position = homePosition;
            transform.rotation = homeRotation;
            Vector3 euler = homeRotation.eulerAngles;
            yaw = euler.y;
            pitch = NormalizePitch(euler.x);
            zoomLevel = 0;
        }

        ApplyZoom();
    }

    void Update()
    {
        if (!flying)
            return;

        if (buildPlacer == null)
            buildPlacer = FindFirstObjectByType<TurretPlatformPlacer>();

        bool blockLook = buildPlacer != null && buildPlacer.BlocksPlayerLook;
        SetCursorLocked(!blockLook);

        if (blockLook)
            return;

        ReadLook(out float mouseX, out float mouseY);
        yaw += mouseX * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - mouseY * mouseSensitivity, minPitch, maxPitch);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        UpdateZoom();
    }

    void UpdateZoom()
    {
        float scroll = ReadScroll();
        if (scroll > 0.01f)
            zoomLevel++;
        else if (scroll < -0.01f)
            zoomLevel--;
        else
            return;

        int maxLevel = zoomFovs != null && zoomFovs.Length > 0 ? zoomFovs.Length - 1 : 0;
        zoomLevel = Mathf.Clamp(zoomLevel, 0, maxLevel);
        ApplyZoom();
    }

    void ApplyZoom()
    {
        if (zoomCameras == null || zoomCameras.Length == 0)
            return;

        float fov = 60f;
        if (zoomFovs != null && zoomFovs.Length > 0)
        {
            int index = Mathf.Clamp(zoomLevel, 0, zoomFovs.Length - 1);
            fov = zoomFovs[index];
        }

        for (int i = 0; i < zoomCameras.Length; i++)
        {
            if (zoomCameras[i] != null && !zoomCameras[i].orthographic)
                zoomCameras[i].fieldOfView = fov;
        }
    }

    void FixedUpdate()
    {
        if (!flying || body.isKinematic)
            return;

        Vector3 wish = ReadMoveWish();
        Vector3 velocity = body.linearVelocity;

        if (wish.sqrMagnitude > 0.0001f)
            velocity += wish.normalized * acceleration * Time.fixedDeltaTime;
        else
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, brake * Time.fixedDeltaTime);

        if (velocity.magnitude > maxSpeed)
            velocity = velocity.normalized * maxSpeed;

        velocity = ConstrainToLimit(velocity);
        body.linearVelocity = velocity;
    }

    Vector3 ReadMoveWish()
    {
        float x = 0f;
        float z = 0f;
        float y = 0f;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                x -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                z += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                z -= 1f;
            if (keyboard.spaceKey.isPressed)
                y += 1f;
            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
                y -= 1f;
        }
        else
#endif
        {
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                x += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                x -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                z += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                z -= 1f;
            if (Input.GetKey(KeyCode.Space))
                y += 1f;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                y -= 1f;
        }

        Vector3 planar = Quaternion.Euler(0f, yaw, 0f) * new Vector3(x, 0f, z);
        planar.y = 0f;
        Vector3 wish = planar + Vector3.up * y;
        if (wish.sqrMagnitude > 1f)
            wish.Normalize();
        return wish;
    }

    void ReadLook(out float mouseX, out float mouseY)
    {
        mouseX = 0f;
        mouseY = 0f;

#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            mouseX = delta.x;
            mouseY = delta.y;
            return;
        }
#endif
        mouseX = Input.GetAxisRaw("Mouse X") * 20f;
        mouseY = Input.GetAxisRaw("Mouse Y") * 20f;
    }

    float ReadScroll()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null)
            return mouse.scroll.ReadValue().y;
#endif
        return Input.mouseScrollDelta.y;
    }

    Vector3 ConstrainToLimit(Vector3 velocity)
    {
        Collider volume = moveLimit != null ? moveLimit.Volume : null;
        if (volume == null)
            return velocity;

        Vector3 pos = body.position;
        if (IsInside(volume, pos))
            return velocity;

        Vector3 closest = volume.ClosestPoint(pos);
        Vector3 inward = closest - pos;
        if (inward.sqrMagnitude < 0.000001f)
            return velocity;

        Vector3 dir = inward.normalized;
        float outward = Vector3.Dot(velocity, -dir);
        if (outward > 0f)
            velocity += dir * outward;

        velocity += dir * pullBackStrength * Time.fixedDeltaTime;
        return velocity;
    }

    static bool IsInside(Collider volume, Vector3 worldPoint)
    {
        Vector3 closest = volume.ClosestPoint(worldPoint);
        return (closest - worldPoint).sqrMagnitude < 0.0001f;
    }

    static float NormalizePitch(float eulerX)
    {
        if (eulerX > 180f)
            eulerX -= 360f;
        return eulerX;
    }

    void SetVisible(bool visible)
    {
        if (bodyRenderers == null)
            return;

        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            if (bodyRenderers[i] != null)
                bodyRenderers[i].enabled = visible;
        }
    }

    void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
