using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DisposableCameraDrone : MonoBehaviour
{
    public Camera captureCamera;
    public float lifetime = 30f;

    AIInputCapture capture;
    bool registered;
    float dieAt;

    public void Activate(AIInputCapture input, float duration, Camera template)
    {
        capture = input;
        lifetime = Mathf.Max(0.1f, duration);
        dieAt = Time.time + lifetime;
        EnsureCamera(template);
        Register();
    }

    void Update()
    {
        if (Time.time >= dieAt)
            Destroy(gameObject);
    }

    void OnDestroy()
    {
        Unregister();
    }

    void EnsureCamera(Camera template)
    {
        if (captureCamera == null)
            captureCamera = GetComponentInChildren<Camera>(true);

        if (captureCamera == null)
        {
            GameObject go = new GameObject("DroneCaptureCamera");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, -0.05f, 0.35f);
            go.transform.localRotation = Quaternion.identity;
            captureCamera = go.AddComponent<Camera>();
            if (go.GetComponent<UniversalAdditionalCameraData>() == null)
                go.AddComponent<UniversalAdditionalCameraData>();
        }

        AudioListener listener = captureCamera.GetComponent<AudioListener>();
        if (listener != null)
            Destroy(listener);

        captureCamera.enabled = false;
        captureCamera.targetTexture = null;
        captureCamera.depth = -8f;
        captureCamera.nearClipPlane = 0.15f;
        captureCamera.farClipPlane = 1000f;
        captureCamera.fieldOfView = template != null ? template.fieldOfView : 60f;
        captureCamera.clearFlags = CameraClearFlags.Skybox;
        captureCamera.allowHDR = true;
        captureCamera.allowMSAA = true;
        if (template != null)
            captureCamera.cullingMask = template.cullingMask;
    }

    void Register()
    {
        if (registered || capture == null || captureCamera == null)
            return;

        capture.RegisterCamera(captureCamera);
        registered = true;
    }

    void Unregister()
    {
        if (!registered || capture == null || captureCamera == null)
            return;

        capture.UnregisterCamera(captureCamera);
        registered = false;
    }
}
