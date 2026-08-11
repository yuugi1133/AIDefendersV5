using UnityEngine;
using System;
using System.IO;
using UnityEngine.Networking;
using System.Collections;

public class AIInputCapture : MonoBehaviour
{
    public Camera cctvCamera;
    public RenderTexture renderTexture;

    public float CaptureDelay = 0.1f; //1초당 찰영 속도

    Texture2D screenTexture;


    public Action<DetectionResponse> OnDetectionReceived; //이벤트:

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

        yield return www.SendWebRequest();  //파이썬 서버로 전송

        if (www.result == UnityWebRequest.Result.Success)   //그리고 결과를 받아 처리
        {
            string json = www.downloadHandler.text;     
            Debug.Log(json);    //json 결과 출력

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
        else
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
