using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Networking;
using System;
using System.Collections;

public class AIInputCapture : MonoBehaviour
{
    [FormerlySerializedAs("cctvCamera")]
    public Camera[] cameras;
    public RenderTexture renderTexture;

    public float CaptureDelay = 0.1f;

    [Header("Debug")]
    public bool logInferenceTime = true;

    Texture2D screenTexture;
    int nextIndex;

    public Action<DetectionResponse, Camera> OnDetectionReceived;

    void Awake()
    {
        EnsureLegacyCamera();
    }

    void Start()
    {
        EnsureLegacyCamera();
        InvokeRepeating(nameof(CaptureAndSend), 0f, CaptureDelay);

        if (renderTexture != null)
        {
            screenTexture = new Texture2D(
                renderTexture.width,
                renderTexture.height,
                TextureFormat.RGB24,
                false
            );
        }
    }

    public void RegisterCamera(Camera camera)
    {
        if (camera == null)
            return;

        EnsureLegacyCamera();
        if (cameras == null)
            cameras = Array.Empty<Camera>();

        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] == camera)
                return;
        }

        Array.Resize(ref cameras, cameras.Length + 1);
        cameras[cameras.Length - 1] = camera;
    }

    public void UnregisterCamera(Camera camera)
    {
        if (camera == null || cameras == null || cameras.Length == 0)
            return;

        int write = 0;
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] == null || cameras[i] == camera)
                continue;

            cameras[write] = cameras[i];
            write++;
        }

        if (write == cameras.Length)
            return;

        Array.Resize(ref cameras, write);
        if (write > 0)
            nextIndex %= write;
        else
            nextIndex = 0;
    }

    void CaptureAndSend()
    {
        if (renderTexture == null)
            return;

        Camera camera = NextCamera();
        if (camera == null)
            return;

        if (screenTexture == null)
        {
            screenTexture = new Texture2D(
                renderTexture.width,
                renderTexture.height,
                TextureFormat.RGB24,
                false
            );
        }

        RenderTexture previousTarget = camera.targetTexture;
        bool wasEnabled = camera.enabled;
        camera.enabled = true;
        camera.targetTexture = renderTexture;
        camera.Render();

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;
        screenTexture.ReadPixels(
            new Rect(0, 0, renderTexture.width, renderTexture.height),
            0,
            0
        );
        screenTexture.Apply();
        RenderTexture.active = currentRT;
        camera.targetTexture = previousTarget;
        camera.enabled = wasEnabled;

        byte[] bytes = screenTexture.EncodeToJPG(70);
        StartCoroutine(SendImage(bytes, camera));
    }

    IEnumerator SendImage(byte[] imageBytes, Camera sourceCamera)
    {
        WWWForm form = new WWWForm();
        form.AddBinaryData(
            "file",
            imageBytes,
            "capture.png",
            "image/png"
        );

        UnityWebRequest www = UnityWebRequest.Post(
            "http://127.0.0.1:8000/upload",
            form
        );

        float sentAt = Time.realtimeSinceStartup;
        yield return www.SendWebRequest();
        float elapsedMs = (Time.realtimeSinceStartup - sentAt) * 1000f;

        if (logInferenceTime)
        {
            if (www.result == UnityWebRequest.Result.Success)
                Debug.Log($"CCTV 추론 왕복 {elapsedMs:0.0} ms");
            else
                Debug.LogWarning($"CCTV 추론 실패 {elapsedMs:0.0} ms  {www.error}");
        }

        if (www.result == UnityWebRequest.Result.Success)
        {
            string json = www.downloadHandler.text;
            DetectionResponse response = JsonUtility.FromJson<DetectionResponse>(json);
            if (response != null)
                OnDetectionReceived?.Invoke(response, sourceCamera);
        }
        else if (!logInferenceTime)
        {
            Debug.LogError(www.error);
        }
    }

    Camera NextCamera()
    {
        if (cameras == null || cameras.Length == 0)
            return null;

        int count = cameras.Length;
        for (int i = 0; i < count; i++)
        {
            int index = (nextIndex + i) % count;
            Camera camera = cameras[index];
            if (camera == null || !camera.gameObject.activeInHierarchy)
                continue;

            nextIndex = (index + 1) % count;
            return camera;
        }

        return null;
    }

    void EnsureLegacyCamera()
    {
        if (cameras != null && cameras.Length > 0)
            return;

        Camera self = GetComponent<Camera>();
        if (self != null)
            cameras = new[] { self };
    }
}

[System.Serializable]
public class Detection
{
    public int class_id;
    public float confidence;

    public float center_x;
    public float center_y;

    public float width;
    public float height;
}

[System.Serializable]
public class DetectionResponse
{
    public Detection[] detections;
}
