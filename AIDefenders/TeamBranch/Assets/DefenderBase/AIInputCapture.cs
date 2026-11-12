using UnityEngine;
using System;
using System.IO;
using UnityEngine.Networking;
using System.Collections;

public class AIInputCapture : MonoBehaviour
{
    public Camera cctvCamera;
    public RenderTexture renderTexture;

    public float CaptureDelay = 0.1f;

    [Header("Debug")]
    public bool logInferenceTime = true;

    Texture2D screenTexture;


    public Action<DetectionResponse> OnDetectionReceived; //�̺�Ʈ:

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InvokeRepeating(nameof(CaptureAndSend), 0f, CaptureDelay);

        screenTexture = new Texture2D(
            renderTexture.width,
            renderTexture.height,
            TextureFormat.RGB24,
            false
        );
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.P))
        //{
        //    CaptureImage();
        //}
    }

    void CaptureAndSend()
    {
        RenderTexture currentRT = RenderTexture.active;

        RenderTexture.active = renderTexture;

        cctvCamera.Render();

        screenTexture.ReadPixels(
            new Rect(0, 0, renderTexture.width, renderTexture.height),
            0,
            0
        );

        screenTexture.Apply();

        RenderTexture.active = currentRT;

        byte[] bytes = screenTexture.EncodeToJPG(70);

        StartCoroutine(SendImage(bytes));
    }

    /*
    public void CaptureImage()
    {
        RenderTexture currentRT = RenderTexture.active;

        RenderTexture.active = renderTexture;

        cctvCamera.Render();

        screenTexture.ReadPixels(
            new Rect(0, 0, renderTexture.width, renderTexture.height),
            0,
            0
        );

        screenTexture.Apply();

        RenderTexture.active = currentRT;

        byte[] bytes = screenTexture.EncodeToPNG();

        StartCoroutine(SendImage(bytes));
    }
    */

    IEnumerator SendImage(byte[] imageBytes)
    {
        WWWForm form = new WWWForm();

        form.AddBinaryData(
            "file",
            imageBytes,
            "capture.png",
            "image/png"
        );

        UnityWebRequest www =
            UnityWebRequest.Post(
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
            Debug.Log(json);    //json ��� ���

            DetectionResponse response = JsonUtility.FromJson<DetectionResponse>(json);
            if (response != null)
            {
                foreach (var detection in response.detections)
                {
                    Debug.Log(
                        $"Class:{detection.class_id} " +
                        $"Conf:{detection.confidence:F2} " +
                        $"Center:({detection.center_x:F0}, {detection.center_y:F0})"
                    );
                }

                OnDetectionReceived?.Invoke(response);
            }
        }
        else if (!logInferenceTime)
        {
            Debug.LogError(www.error);
        }
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
